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
        var existing = string.IsNullOrWhiteSpace(command.ReviewId)
            ? await reviews.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.ReviewerUserId == reviewer && x.LastCommandIdempotencyKey == key && !x.IsDeleted)
            : await reviews.FirstOrDefault(x => x.CompanyId == companyId && x.ReviewId == command.ReviewId && !x.IsDeleted);
        if (existing is not null && existing.LastCommandIdempotencyKey == key)
        {
            // Pending work is also processed by a hosted worker; replay gives an immediate recovery attempt.
            var synchronized = await ProcessPendingStageSync(existing, applicant, actorUserId);
            return synchronized
                ? Ok(existing.ReviewId, "Screening command already applied.")
                : Fail(RecruitmentErrorCodes.StageSyncPending, "Screening was saved, and its stage update is queued for recovery.", StatusCodes.Status503ServiceUnavailable);
        }
        if (applicant.ActivityType == ActivityType.Rejected)
            return Fail(RecruitmentErrorCodes.InvalidTransition, "Rejected applications cannot be screened.", StatusCodes.Status409Conflict);

        if (existing is null)
        {
            var targetStage = DetermineNextStage(applicant.ActivityType, decision!, config);
            var entity = new RecruitmentScreeningReview { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = command.VacancyId, ReviewerUserId = reviewer, ResumeVersion = command.ResumeVersion?.Trim(), Score = command.Score, Decision = decision!, RejectionReason = command.RejectionReason?.Trim(), Evidence = command.Evidence?.Trim(), Criteria = criteriaToSave.Select(Map).ToList(), LastCommandIdempotencyKey = key, StageSyncTargetStage = targetStage?.ToString(), StageSyncStatus = targetStage.HasValue ? "Pending" : "NotRequired", CreatedBy = actorUserId, UpdatedBy = actorUserId };
            if (string.IsNullOrWhiteSpace(entity.ReviewId))
                entity.ReviewId = Guid.NewGuid().ToString("N");
            var saved = await reviews.AddOne(entity);
            if (!saved.Success) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Screening review could not be saved.", StatusCodes.Status409Conflict);
            var synchronized = await ProcessPendingStageSync(entity, applicant, actorUserId);
            if (!synchronized)
            {
                await WriteLog(applicant, entity, actorUserId, null);
                return Fail(RecruitmentErrorCodes.StageSyncPending, "Screening was saved, and its stage update is queued for recovery.", StatusCodes.Status503ServiceUnavailable);
            }
            await WriteLog(applicant, entity, actorUserId, null);
            return Ok(entity.ReviewId, "Screening review saved.");
        }

        var version = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Screening review was changed by another user.", StatusCodes.Status409Conflict);
        var previous = existing.Decision;
        var updateTargetStage = DetermineNextStage(applicant.ActivityType, decision!, config);
        existing.VacancyId = command.VacancyId; existing.ResumeVersion = command.ResumeVersion?.Trim(); existing.Score = command.Score; existing.Decision = decision!; existing.RejectionReason = command.RejectionReason?.Trim(); existing.Evidence = command.Evidence?.Trim(); existing.Criteria = criteriaToSave.Select(Map).ToList(); existing.LastCommandIdempotencyKey = key; existing.Version = version + 1; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
        existing.StageSyncTargetStage = updateTargetStage?.ToString(); existing.StageSyncStatus = updateTargetStage.HasValue ? "Pending" : "NotRequired";
        var updated = await reviews.Update(Builders<RecruitmentScreeningReview>.Filter.Where(x => x.CompanyId == companyId && x.ReviewId == existing.ReviewId && (x.Version == version || x.Version == 0)), existing);
        if (!updated.Success) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Screening review was changed by another user.", StatusCodes.Status409Conflict);
        var stageSynchronized = await ProcessPendingStageSync(existing, applicant, actorUserId);
        if (!stageSynchronized)
        {
            await WriteLog(applicant, existing, actorUserId, previous);
            return Fail(RecruitmentErrorCodes.StageSyncPending, "Screening was saved, and its stage update is queued for recovery.", StatusCodes.Status503ServiceUnavailable);
        }
        await WriteLog(applicant, existing, actorUserId, previous);
        return Ok(existing.ReviewId, "Screening review updated.");
    }

    private async Task<bool> ProcessPendingStageSync(RecruitmentScreeningReview review, Applicant applicant, string actorUserId)
    {
        if (!string.Equals(review.StageSyncStatus, "Pending", StringComparison.OrdinalIgnoreCase) ||
            !Enum.TryParse<ActivityType>(review.StageSyncTargetStage, true, out var targetStage) || !Enum.IsDefined(targetStage)) return true;

        if (!await ApplyScreeningStage(companyId: review.CompanyId, applicant, targetStage, review.Decision, actorUserId)) return false;
        return await SetStageSyncStatus(review, "Resolved", actorUserId);
    }

    private async Task<bool> ApplyScreeningStage(string companyId, Applicant applicant, ActivityType targetStage, string decision, string actorUserId)
    {
        var previousApplicantStage = applicant.ActivityType;
        var nextStage = targetStage;

        var canonical = applications is null ? null : await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted);
        if (canonical is not null && !string.Equals(canonical.CurrentStage, nextStage.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            // Custom workflow stages are opaque here. Never overwrite one, or any
            // recognized later stage, with an automatic screening transition.
            if (!TryGetStageRank(canonical.CurrentStage, out var currentRank) || currentRank > StageRank(nextStage))
                return true;

            var fromStage = canonical.CurrentStage;
            var previousVersion = canonical.Version;
            canonical.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = fromStage, ToStage = nextStage.ToString(), ActorUserId = actorUserId, Reason = $"Screening decision: {decision}" });
            canonical.CurrentStage = nextStage.ToString();
            canonical.Version = (previousVersion <= 0 ? 1 : previousVersion) + 1;
            canonical.UpdatedBy = actorUserId;
            canonical.UpdatedDate = DateTime.UtcNow;
            var canonicalUpdated = await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == canonical.ApplicationId && !x.IsDeleted && (x.Version == previousVersion || x.Version == 0)), canonical);
            if (!canonicalUpdated.Success) return false;
        }

        if (applicant.ActivityType != nextStage)
        {
            var previousActivity = applicant.ActivityType;
            applicant.ActivityType = nextStage;
            applicant.UpdatedBy = actorUserId;
            applicant.UpdatedDate = DateTime.UtcNow;
            var applicantUpdated = await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted), applicant);
            if (!applicantUpdated.Success)
            {
                applicant.ActivityType = previousActivity;
                return false;
            }
        }

        if (applications is not null && canonical is null)
        {
            var added = await applications.AddOne(new RecruitmentJobApplication
            {
                CompanyId = companyId,
                ApplicantId = applicant.ApplicantId,
                VacancyId = applicant.VacancyId ?? string.Empty,
                CurrentStage = nextStage.ToString(),
                CreatedBy = actorUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = actorUserId,
                UpdatedDate = DateTime.UtcNow,
                StageHistory = previousApplicantStage == nextStage
                    ? []
                    : [new RecruitmentApplicationStageEvent { FromStage = previousApplicantStage.ToString(), ToStage = nextStage.ToString(), ActorUserId = actorUserId, Reason = $"Screening decision: {decision}" }]
            });
            if (!added.Success) return false;
        }

        return true;
    }

    private static ActivityType? DetermineNextStage(ActivityType currentStage, string decision, RecruitmentEnterpriseConfig? config)
    {
        if (!(config?.AutoAdvanceOnScreening ?? true)) return null;
        return decision switch
        {
            RecruitmentScreeningDecision.Pass when currentStage is ActivityType.New or ActivityType.InProgress or ActivityType.OnHold or ActivityType.Shortlisted => ActivityType.Shortlisted,
            RecruitmentScreeningDecision.Hold when currentStage is ActivityType.New or ActivityType.InProgress or ActivityType.Shortlisted or ActivityType.OnHold => ActivityType.OnHold,
            RecruitmentScreeningDecision.Reject when currentStage is not ActivityType.Selected => ActivityType.Rejected,
            _ => null
        };
    }

    public async Task<int> RecoverPending(string? reviewId = null, int batchSize = 50, CancellationToken cancellationToken = default)
    {
        var pending = string.IsNullOrWhiteSpace(reviewId)
            ? await reviews.GetCollection().Find(x => x.StageSyncStatus == "Pending" && !x.IsDeleted).SortBy(x => x.CreatedDate).Limit(Math.Clamp(batchSize, 1, 200)).ToListAsync(cancellationToken)
            : [await reviews.FirstOrDefault(x => x.ReviewId == reviewId && x.StageSyncStatus == "Pending" && !x.IsDeleted)];
        var recovered = 0;
        foreach (var review in pending.Where(x => x is not null))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var applicant = await applicants.FirstOrDefault(x => x.CompanyId == review!.CompanyId && x.ApplicantId == review.ApplicantId && !x.IsDeleted);
            var actor = review!.UpdatedBy ?? review.CreatedBy ?? "screening-recovery";
            if (applicant is null || string.Equals(applicant.ConsentStatus, "Anonymized", StringComparison.OrdinalIgnoreCase))
            {
                if (await SetStageSyncStatus(review, "Cancelled", actor)) recovered++;
                continue;
            }
            if (await ProcessPendingStageSync(review, applicant, actor)) recovered++;
        }
        return recovered;
    }

    private async Task<bool> SetStageSyncStatus(RecruitmentScreeningReview review, string status, string actorUserId)
    {
        var previousVersion = review.Version <= 0 ? 1 : review.Version;
        review.StageSyncStatus = status;
        // Queue bookkeeping is guarded by the current review version but does not
        // advance the reviewer-facing version for an unchanged evaluation.
        review.UpdatedBy = actorUserId;
        review.UpdatedDate = DateTime.UtcNow;
        var result = await reviews.Update(
            Builders<RecruitmentScreeningReview>.Filter.Where(x => x.CompanyId == review.CompanyId && x.ReviewId == review.ReviewId && x.StageSyncStatus == "Pending" && (x.Version == previousVersion || x.Version == 0)),
            review);
        return result?.Success == true;
    }

    private static int StageRank(ActivityType stage) => stage switch
    {
        ActivityType.New => 0,
        ActivityType.InProgress => 1,
        ActivityType.OnHold => 2,
        ActivityType.Shortlisted => 3,
        ActivityType.Selected => 4,
        ActivityType.Rejected => 5,
        _ => 1
    };

    private static bool TryGetStageRank(string? stage, out int rank)
    {
        if (Enum.TryParse<ActivityType>(stage, true, out var parsed))
        {
            rank = StageRank(parsed);
            return true;
        }
        rank = 0;
        return false;
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
