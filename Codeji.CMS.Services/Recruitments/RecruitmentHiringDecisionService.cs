using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentHiringDecisionService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentInterview> interviews,
    IMongoDbRepository<RecruitmentHiringDecision> decisions,
    IMongoDbRepository<ApplicantLogs> logs,
    IHttpContextAccessor context,
    IMongoDbRepository<RecruitmentJobApplication>? applications = null,
    IMongoDbRepository<RecruitmentEnterpriseConfig>? configs = null) : IRecruitmentHiringDecisionService
{
    private static readonly HashSet<string> Valid = [RecruitmentHiringDecisionStatus.Approve, RecruitmentHiringDecisionStatus.Hold, RecruitmentHiringDecisionStatus.Reject];

    public async Task<Result<string>> Upsert(RecruitmentHiringDecisionCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        var decisionValue = command.Decision?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || !Valid.Contains(decisionValue ?? string.Empty))
            return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Company, idempotency key, and a valid hiring decision are required.");
        if (decisionValue is RecruitmentHiringDecisionStatus.Hold or RecruitmentHiringDecisionStatus.Reject && string.IsNullOrWhiteSpace(command.Notes))
            return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Decision notes are required for Hold or Reject outcomes.");

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId.Trim() && x.VacancyId == command.VacancyId.Trim() && !x.IsDeleted);
        if (applicant is null)
            return Fail(404, RecruitmentErrorCodes.InvalidRequest, "Applicant was not found in the current company.");

        var existing = string.IsNullOrWhiteSpace(command.DecisionId)
            ? await decisions.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted)
            : await decisions.FirstOrDefault(x => x.CompanyId == companyId && x.DecisionId == command.DecisionId && !x.IsDeleted);
        if (existing is not null && existing.LastCommandIdempotencyKey == key)
            return Ok(existing.DecisionId, "Hiring decision already applied.");

        var config = configs is null ? null : await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);
        var allowDirectFromEarlyStage = config is not null && (!config.ApplicationReview || string.Equals(config.RecruitmentMode, "Simple", StringComparison.OrdinalIgnoreCase));

        if (string.Equals(decisionValue, RecruitmentHiringDecisionStatus.Approve, StringComparison.OrdinalIgnoreCase)
            && applicant.ActivityType is not (ActivityType.Shortlisted or ActivityType.OnHold or ActivityType.Selected)
            && !(allowDirectFromEarlyStage && applicant.ActivityType is ActivityType.New or ActivityType.InProgress))
        {
            return Fail(409, RecruitmentErrorCodes.InvalidTransition, "The application must be shortlisted through screening before approval.");
        }

        if (string.Equals(decisionValue, RecruitmentHiringDecisionStatus.Approve, StringComparison.OrdinalIgnoreCase))
        {
            var openInterviews = (await interviews.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted && x.Status != RecruitmentInterviewStatus.Cancelled)).ToList();
            var requireInterviewGate = config is null || (config.RequireInterviewBeforeDecision && (config.InterviewRounds || config.Scorecards || !string.Equals(config.RecruitmentMode, "Simple", StringComparison.OrdinalIgnoreCase)));
            if (openInterviews.Count == 0 && requireInterviewGate)
                return Fail(409, RecruitmentErrorCodes.InvalidTransition, "At least one interview round must be completed and debriefed before approval.");
            if (openInterviews.Any(x => x.Status != RecruitmentInterviewStatus.Completed || x.DebriefStatus != RecruitmentDebriefStatus.Complete))
                return Fail(409, RecruitmentErrorCodes.InvalidTransition, "All scheduled interview rounds must be completed and debriefed before approval.");
        }

        if (existing is null)
        {
            existing = new RecruitmentHiringDecision { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, Decision = decisionValue!, Notes = command.Notes?.Trim(), LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            var added = await decisions.AddOne(existing);
            if (!added.Success) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision could not be saved.");
        }
        else
        {
            var version = existing.Version <= 0 ? 1 : existing.Version;
            if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version)
                return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision was changed by another user.");
            existing.Decision = decisionValue!;
            existing.Notes = command.Notes?.Trim();
            existing.LastCommandIdempotencyKey = key;
            existing.Version = version + 1;
            existing.UpdatedBy = actorUserId;
            existing.UpdatedDate = DateTime.UtcNow;
            var updated = await decisions.Update(Builders<RecruitmentHiringDecision>.Filter.Where(x => x.CompanyId == companyId && x.DecisionId == existing.DecisionId && (x.Version == version || x.Version == 0)), existing);
            if (!updated.Success) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision was changed by another user.");
        }

        var target = decisionValue.Equals(RecruitmentHiringDecisionStatus.Approve, StringComparison.OrdinalIgnoreCase)
            ? ActivityType.Selected
            : decisionValue.Equals(RecruitmentHiringDecisionStatus.Reject, StringComparison.OrdinalIgnoreCase)
                ? ActivityType.Rejected
                : ActivityType.OnHold;

        if (applications is not null)
        {
            var canonical = await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted);
            if (canonical is not null && !string.Equals(canonical.CurrentStage, target.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                canonical.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = canonical.CurrentStage, ToStage = target.ToString(), ActorUserId = actorUserId, Reason = $"Hiring decision: {decisionValue}" });
                canonical.CurrentStage = target.ToString();
                canonical.Version = (canonical.Version <= 0 ? 1 : canonical.Version) + 1;
                canonical.UpdatedBy = actorUserId;
                canonical.UpdatedDate = DateTime.UtcNow;
                await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == canonical.ApplicationId && !x.IsDeleted), canonical);
            }
        }

        var previous = applicant.ActivityType;
        if (previous != target)
        {
            applicant.ActivityType = target;
            applicant.UpdatedBy = actorUserId;
            applicant.UpdatedDate = DateTime.UtcNow;
            await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId), applicant);
        }

        await logs.AddOne(new ApplicantLogs { CompanyId = companyId, ApplicantId = applicant.ApplicantId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobRole = applicant.VacancyId, UserId = actorUserId, ActivityCategory = 0, Description = $"Hiring decision: {decisionValue}. {command.Notes?.Trim()}", EventType = "HiringDecisionRecorded", PreviousStage = previous.ToString(), CurrentStage = target.ToString(), CreatedBy = actorUserId, UpdatedBy = actorUserId });
        return Ok(existing.DecisionId, "Hiring decision saved.");
    }

    private static Result<string> Ok(string id, string msg) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = msg };
    private static Result<string> Fail(int status, string code, string msg) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = msg };
}
