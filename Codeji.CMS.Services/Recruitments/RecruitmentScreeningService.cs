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

public sealed class RecruitmentScreeningService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentScreeningReview> reviews,
    IMongoDbRepository<ApplicantLogs> logs,
    IHttpContextAccessor context) : IRecruitmentScreeningService
{
    public async Task<Result<string>> Upsert(RecruitmentScreeningReviewCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        var reviewer = string.IsNullOrWhiteSpace(command.ReviewerUserId) ? actorUserId : command.ReviewerUserId.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(reviewer))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Company, reviewer, and idempotency key are required.");
        var decision = command.Decision?.Trim();
        if (decision is not (RecruitmentScreeningDecision.Pending or RecruitmentScreeningDecision.Pass or RecruitmentScreeningDecision.Hold or RecruitmentScreeningDecision.Reject))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Screening decision must be Pending, Pass, Hold, or Reject.");
        if (command.Score is < 0 or > 100 || command.Criteria is null || command.Criteria.Count == 0 || command.Criteria.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Screening requires at least one named criterion and a score from 0 to 100.");
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
            var entity = new RecruitmentScreeningReview { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = command.VacancyId, ReviewerUserId = reviewer, ResumeVersion = command.ResumeVersion?.Trim(), Score = command.Score, Decision = decision!, RejectionReason = command.RejectionReason?.Trim(), Evidence = command.Evidence?.Trim(), Criteria = command.Criteria.Select(Map).ToList(), LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            var saved = await reviews.AddOne(entity);
            if (!saved.Success) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Screening review could not be saved.", StatusCodes.Status409Conflict);
            await WriteLog(applicant, entity, actorUserId, null);
            return Ok(entity.ReviewId, "Screening review saved.");
        }
        var version = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Screening review was changed by another user.", StatusCodes.Status409Conflict);
        var previous = existing.Decision;
        existing.VacancyId = command.VacancyId; existing.ResumeVersion = command.ResumeVersion?.Trim(); existing.Score = command.Score; existing.Decision = decision!; existing.RejectionReason = command.RejectionReason?.Trim(); existing.Evidence = command.Evidence?.Trim(); existing.Criteria = command.Criteria.Select(Map).ToList(); existing.LastCommandIdempotencyKey = key; existing.Version = version + 1; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
        var updated = await reviews.Update(Builders<RecruitmentScreeningReview>.Filter.Where(x => x.CompanyId == companyId && x.ReviewId == existing.ReviewId && (x.Version == version || x.Version == 0)), existing);
        if (!updated.Success) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Screening review was changed by another user.", StatusCodes.Status409Conflict);
        await WriteLog(applicant, existing, actorUserId, previous);
        return Ok(existing.ReviewId, "Screening review updated.");
    }

    private async Task WriteLog(Applicant applicant, RecruitmentScreeningReview review, string actor, string? previous)
    {
        await logs.AddOne(new ApplicantLogs { CompanyId = applicant.CompanyId, ApplicantId = applicant.ApplicantId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobRole = applicant.VacancyId, UserId = actor, ActivityCategory = 0, Description = $"Screening {review.Decision.ToLowerInvariant()} with score {review.Score}." + (string.IsNullOrWhiteSpace(review.RejectionReason) ? string.Empty : $" Reason: {review.RejectionReason}"), EventType = "ScreeningReviewUpdated", PreviousStage = previous, CurrentStage = review.Decision, CreatedBy = actor, UpdatedBy = actor });
    }

    private static RecruitmentScreeningCriterion Map(RecruitmentScreeningCriterionCommand item) => new() { Name = item.Name.Trim(), Passed = item.Passed, Notes = item.Notes?.Trim() };
    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
    private static Result<string> Fail(string code, string message, int status = 400) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
}