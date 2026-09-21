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
    IHttpContextAccessor context) : IRecruitmentHiringDecisionService
{
    private static readonly HashSet<string> Valid = [RecruitmentHiringDecisionStatus.Approve, RecruitmentHiringDecisionStatus.Hold, RecruitmentHiringDecisionStatus.Reject];
    public async Task<Result<string>> Upsert(RecruitmentHiringDecisionCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context); var key = command.IdempotencyKey?.Trim(); var decisionValue = command.Decision?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || !Valid.Contains(decisionValue ?? string.Empty)) return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Company, idempotency key, and a valid hiring decision are required.");
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId.Trim() && x.VacancyId == command.VacancyId.Trim() && !x.IsDeleted);
        if (applicant is null) return Fail(404, RecruitmentErrorCodes.InvalidRequest, "Applicant was not found in the current company.");
        var existing = string.IsNullOrWhiteSpace(command.DecisionId) ? await decisions.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted) : await decisions.FirstOrDefault(x => x.CompanyId == companyId && x.DecisionId == command.DecisionId && !x.IsDeleted);
        if (existing is not null && existing.LastCommandIdempotencyKey == key) return Ok(existing.DecisionId, "Hiring decision already applied.");
        if (string.Equals(decisionValue, RecruitmentHiringDecisionStatus.Approve, StringComparison.OrdinalIgnoreCase))
        {
            var openInterviews = (await interviews.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted && x.Status != RecruitmentInterviewStatus.Cancelled)).ToList();
            if (openInterviews.Any(x => x.Status != RecruitmentInterviewStatus.Completed || x.DebriefStatus != RecruitmentDebriefStatus.Complete)) return Fail(409, RecruitmentErrorCodes.InvalidTransition, "All scheduled interview rounds must be completed and debriefed before approval.");
        }
        if (existing is null)
        {
            existing = new RecruitmentHiringDecision { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, Decision = decisionValue!, Notes = command.Notes?.Trim(), LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            var added = await decisions.AddOne(existing); if (!added.Success) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision could not be saved.");
        }
        else
        {
            var version = existing.Version <= 0 ? 1 : existing.Version; if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision was changed by another user.");
            existing.Decision = decisionValue!; existing.Notes = command.Notes?.Trim(); existing.LastCommandIdempotencyKey = key; existing.Version = version + 1; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
            var updated = await decisions.Update(Builders<RecruitmentHiringDecision>.Filter.Where(x => x.CompanyId == companyId && x.DecisionId == existing.DecisionId && (x.Version == version || x.Version == 0)), existing); if (!updated.Success) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision was changed by another user.");
        }
        var target = decisionValue.Equals(RecruitmentHiringDecisionStatus.Approve, StringComparison.OrdinalIgnoreCase) ? ActivityType.Selected : decisionValue.Equals(RecruitmentHiringDecisionStatus.Reject, StringComparison.OrdinalIgnoreCase) ? ActivityType.Rejected : ActivityType.OnHold;
        var previous = applicant.ActivityType; if (previous != target) { applicant.ActivityType = target; applicant.UpdatedBy = actorUserId; applicant.UpdatedDate = DateTime.UtcNow; await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId), applicant); }
        await logs.AddOne(new ApplicantLogs { CompanyId = companyId, ApplicantId = applicant.ApplicantId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobRole = applicant.VacancyId, UserId = actorUserId, ActivityCategory = 0, Description = $"Hiring decision: {decisionValue}. {command.Notes?.Trim()}", EventType = "HiringDecisionRecorded", PreviousStage = previous.ToString(), CurrentStage = target.ToString(), CreatedBy = actorUserId, UpdatedBy = actorUserId });
        return Ok(existing.DecisionId, "Hiring decision saved.");
    }
    private static Result<string> Ok(string id, string msg) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = msg };
    private static Result<string> Fail(int status, string code, string msg) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = msg };
}
