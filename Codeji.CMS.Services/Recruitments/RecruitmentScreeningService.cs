using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentScreeningService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentScreeningReview> reviews,
    IMongoDbRepository<ApplicantLogs> logs,
    IHttpContextAccessor context,
    IMongoDbRepository<RecruitmentEnterpriseConfig>? configs = null,
    IMongoDbRepository<EmpUser>? employees = null,
    IMongoDbRepository<RecruitmentJobApplication>? applications = null,
    IMongoDbRepository<JobVacancy>? vacancies = null) : IRecruitmentScreeningService
{
    public async Task<Result<string>> Upsert(RecruitmentScreeningReviewCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        var reviewer = string.IsNullOrWhiteSpace(command.ReviewerUserId) ? actorUserId : command.ReviewerUserId.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(reviewer))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Company, reviewer, and idempotency key are required.");
        var decision = command.Decision?.Trim();
        if (employees is not null && !string.IsNullOrWhiteSpace(reviewer))
        {
            var activeReviewer = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == reviewer && x.Status && !x.IsDeleted);
            if (activeReviewer is null && string.IsNullOrWhiteSpace(CurrentContext.AdminUserId(context)) && !string.Equals(reviewer, actorUserId, StringComparison.OrdinalIgnoreCase))
                return Fail(RecruitmentErrorCodes.InvalidRequest, "Reviewer is not active in the current company.");
        }
        if (decision is not (RecruitmentScreeningDecision.Pending or RecruitmentScreeningDecision.Pass or RecruitmentScreeningDecision.Hold or RecruitmentScreeningDecision.Reject))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Screening decision must be Pending, Pass, Hold, or Reject.");
        if (command.Score is < 0 or > 100 || command.Criteria is null || command.Criteria.Count == 0 || command.Criteria.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Screening requires at least one named criterion and a score from 0 to 100.");

        var config = configs is null ? null : await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);

        JobVacancy? vacancy = null;
        if (vacancies is not null && !string.IsNullOrWhiteSpace(command.VacancyId))
        {
            vacancy = await vacancies.FirstOrDefault(x => x.CompanyId == companyId && x.JobId == command.VacancyId && !x.IsDeleted);
        }

        var allowCustomWorkflows = config is null || config.AllowJobWorkflowCustomization;

        // Active pass score: vacancy-specific threshold takes first precedence if customization allowed, followed by company config (only when enabled)
        bool isThresholdActive = false;
        int passScoreThreshold = 0;

        if (string.Equals(vacancy?.WorkflowMode, "Direct", StringComparison.OrdinalIgnoreCase))
        {
            // Direct hire vacancies fast-track without score gates
            isThresholdActive = false;
        }
        else if (allowCustomWorkflows && vacancy?.ScreeningPassScore is > 0)
        {
            // Role specifically configured a screening pass threshold
            passScoreThreshold = vacancy.ScreeningPassScore.Value;
            isThresholdActive = true;
        }
        else if (config is not null && !string.Equals(config.RecruitmentMode, "Simple", StringComparison.OrdinalIgnoreCase) && config.AutoAdvanceOnScreening && config.ScreeningPassScore > 0)
        {
            // Mid-market / Enterprise tenant with active screening gate policy
            passScoreThreshold = config.ScreeningPassScore;
            isThresholdActive = true;
        }

        if (isThresholdActive && passScoreThreshold > 0 && decision == RecruitmentScreeningDecision.Pass && command.Score < passScoreThreshold)
            return Fail(RecruitmentErrorCodes.InvalidRequest, $"Screening pass requires a score of at least {passScoreThreshold}.");

        // REC-BUG-06 Fix: When bulk screening sends "Bulk recruiter review", automatically expand it to match the company's configured ScreeningCriteria.
        var criteriaToSave = command.Criteria;
        var configuredCriteria = (allowCustomWorkflows && vacancy?.ScreeningCriteria is { Count: > 0 } vCriteria)
            ? vCriteria
            : config?.ScreeningCriteria;
        if (configuredCriteria is { Count: > 0 })
        {
            if (criteriaToSave.Count == 1 && string.Equals(criteriaToSave[0].Name.Trim(), "Bulk recruiter review", StringComparison.OrdinalIgnoreCase))
            {
                var template = criteriaToSave[0];
                criteriaToSave = configuredCriteria.Select(name => new RecruitmentScreeningCriterionCommand
                {
                    Name = name,
                    Passed = template.Passed,
                    Notes = template.Notes
                }).ToList();
            }
            else if (criteriaToSave.Any(x => !configuredCriteria.Contains(x.Name.Trim(), StringComparer.OrdinalIgnoreCase)))
            {
                return Fail(RecruitmentErrorCodes.InvalidRequest, "Screening criteria must match the company configuration.");
            }
        }

        if (decision == RecruitmentScreeningDecision.Reject && string.IsNullOrWhiteSpace(command.RejectionReason))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "A rejection reason is required.");
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && !x.IsDeleted);
        if (applicant is null) return Fail(RecruitmentErrorCodes.InvalidRequest, "Applicant was not found in the current company.");
        if (applicant.ActivityType == ActivityType.Rejected) return Fail(RecruitmentErrorCodes.InvalidTransition, "Rejected applications cannot be screened.", StatusCodes.Status409Conflict);

        var existing = string.IsNullOrWhiteSpace(command.ReviewId)
            ? await reviews.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.ReviewerUserId == reviewer && x.LastCommandIdempotencyKey == key && !x.IsDeleted)
            : await reviews.FirstOrDefault(x => x.CompanyId == companyId && x.ReviewId == command.ReviewId && !x.IsDeleted);
        if (existing is not null && existing.LastCommandIdempotencyKey == key) return Ok(existing.ReviewId, "Screening command already applied.");

        if (existing is null)
        {
            var entity = new RecruitmentScreeningReview { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = command.VacancyId, ReviewerUserId = reviewer, ResumeVersion = command.ResumeVersion?.Trim(), Score = command.Score, Decision = decision!, RejectionReason = command.RejectionReason?.Trim(), Evidence = command.Evidence?.Trim(), Criteria = criteriaToSave.Select(Map).ToList(), LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            if (string.IsNullOrWhiteSpace(entity.ReviewId))
                entity.ReviewId = Guid.NewGuid().ToString("N");
            var saved = await reviews.AddOne(entity);
            if (!saved.Success) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Screening review could not be saved.", StatusCodes.Status409Conflict);
            await AutoAdvanceApplicantStage(companyId, applicant, decision!, config, actorUserId);
            await WriteLog(applicant, entity, actorUserId, null);
            return Ok(entity.ReviewId, "Screening review saved.");
        }

        var version = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Screening review was changed by another user.", StatusCodes.Status409Conflict);
        var previous = existing.Decision;
        existing.VacancyId = command.VacancyId; existing.ResumeVersion = command.ResumeVersion?.Trim(); existing.Score = command.Score; existing.Decision = decision!; existing.RejectionReason = command.RejectionReason?.Trim(); existing.Evidence = command.Evidence?.Trim(); existing.Criteria = criteriaToSave.Select(Map).ToList(); existing.LastCommandIdempotencyKey = key; existing.Version = version + 1; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
        var updated = await reviews.Update(Builders<RecruitmentScreeningReview>.Filter.Where(x => x.CompanyId == companyId && x.ReviewId == existing.ReviewId && (x.Version == version || x.Version == 0)), existing);
        if (!updated.Success) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Screening review was changed by another user.", StatusCodes.Status409Conflict);
        await AutoAdvanceApplicantStage(companyId, applicant, decision!, config, actorUserId);
        await WriteLog(applicant, existing, actorUserId, previous);
        return Ok(existing.ReviewId, "Screening review updated.");
    }

    private async Task AutoAdvanceApplicantStage(string companyId, Applicant applicant, string decision, RecruitmentEnterpriseConfig? config, string actorUserId)
    {
        // REC-BUG-06 Fix: Automatically advance Applicant.ActivityType and canonical RecruitmentJobApplication.CurrentStage when AutoAdvanceOnScreening is enabled (default true)
        if (!(config?.AutoAdvanceOnScreening ?? true)) return;

        ActivityType? nextStage = decision switch
        {
            RecruitmentScreeningDecision.Pass when applicant.ActivityType is ActivityType.New or ActivityType.InProgress or ActivityType.OnHold => ActivityType.Shortlisted,
            RecruitmentScreeningDecision.Hold when applicant.ActivityType is ActivityType.New or ActivityType.InProgress or ActivityType.Shortlisted => ActivityType.OnHold,
            RecruitmentScreeningDecision.Reject when applicant.ActivityType is not ActivityType.Selected => ActivityType.Rejected,
            _ => null
        };

        if (!nextStage.HasValue || applicant.ActivityType == nextStage.Value) return;

        applicant.ActivityType = nextStage.Value;
        applicant.UpdatedBy = actorUserId;
        applicant.UpdatedDate = DateTime.UtcNow;
        await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId), applicant);

        if (applications is not null)
        {
            var canonical = await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted);
            if (canonical is not null && !string.Equals(canonical.CurrentStage, nextStage.Value.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                canonical.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = canonical.CurrentStage, ToStage = nextStage.Value.ToString(), ActorUserId = actorUserId, Reason = $"Screening decision: {decision}" });
                canonical.CurrentStage = nextStage.Value.ToString();
                canonical.Version = (canonical.Version <= 0 ? 1 : canonical.Version) + 1;
                canonical.UpdatedBy = actorUserId;
                canonical.UpdatedDate = DateTime.UtcNow;
                await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == canonical.ApplicationId && !x.IsDeleted), canonical);
            }
            else if (canonical is null)
            {
                var newCanonical = new RecruitmentJobApplication
                {
                    CompanyId = companyId,
                    ApplicantId = applicant.ApplicantId,
                    VacancyId = applicant.VacancyId ?? string.Empty,
                    CurrentStage = nextStage.Value.ToString(),
                    CreatedBy = actorUserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = actorUserId,
                    UpdatedDate = DateTime.UtcNow,
                    StageHistory =
                    [
                        new RecruitmentApplicationStageEvent
                        {
                            FromStage = applicant.ActivityType.ToString(),
                            ToStage = nextStage.Value.ToString(),
                            ActorUserId = actorUserId,
                            Reason = $"Screening decision: {decision}"
                        }
                    ]
                };
                await applications.AddOne(newCanonical);
            }
        }
    }

    public async Task<Result<RecruitmentScreeningBulkResult>> Bulk(RecruitmentScreeningBulkCommand command, string actorUserId)
    {
        if (command.Items is null || command.Items.Count == 0 || command.Items.Count > 100 || string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return new Result<RecruitmentScreeningBulkResult> { Success = false, StatusCode = 400, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Up to 100 screening items and an idempotency key are required." };
        var result = new RecruitmentScreeningBulkResult();
        foreach (var item in command.Items.Where(x => !string.IsNullOrWhiteSpace(x.ApplicantId) && !string.IsNullOrWhiteSpace(x.VacancyId)))
        {
            var response = await Upsert(new RecruitmentScreeningReviewCommand { ApplicantId = item.ApplicantId.Trim(), VacancyId = item.VacancyId.Trim(), Decision = command.Decision, Score = command.Score, Criteria = command.Criteria, Evidence = command.Evidence, RejectionReason = command.RejectionReason, IdempotencyKey = $"{command.IdempotencyKey.Trim()}:{item.ApplicantId.Trim()}:{item.VacancyId.Trim()}" }, actorUserId);
            if (response.Success) result.Succeeded++; else { result.Failed++; result.FailedApplicantIds.Add(item.ApplicantId.Trim()); }
        }
        return new Result<RecruitmentScreeningBulkResult> { Success = result.Failed == 0, StatusCode = result.Failed == 0 ? 200 : 207, ErrorCode = result.Failed == 0 ? null : RecruitmentErrorCodes.InvalidRequest, MethodResult = result, Data = result, TotalRecords = command.Items.Count, Message = $"Processed {result.Succeeded} screening item(s); {result.Failed} failed." };
    }

    private async Task WriteLog(Applicant applicant, RecruitmentScreeningReview review, string actor, string? previous)
    {
        await logs.AddOne(new ApplicantLogs { CompanyId = applicant.CompanyId, ApplicantId = applicant.ApplicantId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobRole = applicant.VacancyId, UserId = actor, ActivityCategory = 0, Description = $"Screening {review.Decision.ToLowerInvariant()} with score {review.Score}." + (string.IsNullOrWhiteSpace(review.RejectionReason) ? string.Empty : $" Reason: {review.RejectionReason}"), EventType = "ScreeningReviewUpdated", PreviousStage = previous, CurrentStage = review.Decision, CreatedBy = actor, UpdatedBy = actor });
    }

    private static RecruitmentScreeningCriterion Map(RecruitmentScreeningCriterionCommand item) => new() { Name = item.Name.Trim(), Passed = item.Passed, Notes = item.Notes?.Trim() };
    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
    private static Result<string> Fail(string code, string message, int status = 400) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
}