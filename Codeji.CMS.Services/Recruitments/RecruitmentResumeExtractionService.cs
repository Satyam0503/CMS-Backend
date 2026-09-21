using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentResumeExtractionService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentResumeExtractionReview> reviews,
    IMongoDbRepository<ApplicantLogs> logs,
    IHttpContextAccessor context) : IRecruitmentResumeExtractionService
{
    private static readonly HashSet<string> Decisions = [RecruitmentResumeExtractionDecision.Pending, RecruitmentResumeExtractionDecision.Approved, RecruitmentResumeExtractionDecision.Rejected];
    public async Task<Result<string>> Upsert(RecruitmentResumeExtractionReviewCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context); var key = command.IdempotencyKey?.Trim(); var decision = command.Decision?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || !Decisions.Contains(decision ?? string.Empty)) return Fail(400, "Company, idempotency key, and a valid extraction decision are required.");
        if (command.ExtractedFields is null || command.ExtractedFields.Count == 0 || command.ExtractedFields.Keys.Any(string.IsNullOrWhiteSpace)) return Fail(400, "At least one named extracted field is required.");
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId.Trim() && x.VacancyId == command.VacancyId.Trim() && !x.IsDeleted);
        if (applicant is null) return Fail(404, "Applicant was not found in the current company.");
        var existing = string.IsNullOrWhiteSpace(command.ReviewId) ? await reviews.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted) : await reviews.FirstOrDefault(x => x.CompanyId == companyId && x.ReviewId == command.ReviewId && !x.IsDeleted);
        if (existing is not null && existing.LastCommandIdempotencyKey == key) return Ok(existing.ReviewId, "Resume extraction review already applied.");
        if (existing is null)
        {
            existing = new RecruitmentResumeExtractionReview { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, ResumeVersion = command.ResumeVersion?.Trim(), ExtractedFields = command.ExtractedFields.ToDictionary(x => x.Key.Trim(), x => x.Value?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase), Decision = decision!, ReviewerUserId = actorUserId, Notes = command.Notes?.Trim(), LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            var added = await reviews.AddOne(existing); if (!added.Success) return Fail(409, "Resume extraction review could not be created.");
        }
        else
        {
            var version = existing.Version <= 0 ? 1 : existing.Version; if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(409, "Resume extraction review was changed by another user.");
            existing.ExtractedFields = command.ExtractedFields.ToDictionary(x => x.Key.Trim(), x => x.Value?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase); existing.ResumeVersion = command.ResumeVersion?.Trim(); existing.Decision = decision!; existing.ReviewerUserId = actorUserId; existing.Notes = command.Notes?.Trim(); existing.LastCommandIdempotencyKey = key; existing.Version = version + 1; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
            var updated = await reviews.Update(Builders<RecruitmentResumeExtractionReview>.Filter.Where(x => x.CompanyId == companyId && x.ReviewId == existing.ReviewId && (x.Version == version || x.Version == 0)), existing); if (!updated.Success) return Fail(409, "Resume extraction review was changed by another user.");
        }
        await logs.AddOne(new ApplicantLogs { CompanyId = companyId, ApplicantId = applicant.ApplicantId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobRole = applicant.VacancyId, UserId = actorUserId, ActivityCategory = 0, Description = $"Resume extraction review {decision switch { RecruitmentResumeExtractionDecision.Approved => "approved", RecruitmentResumeExtractionDecision.Rejected => "rejected", _ => "saved" }}; human approval is required before using extracted fields.", EventType = "ResumeExtractionReviewed", CurrentStage = decision, CreatedBy = actorUserId, UpdatedBy = actorUserId });
        return Ok(existing.ReviewId, "Resume extraction review saved.");
    }
    private static Result<string> Ok(string id, string msg) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = msg };
    private static Result<string> Fail(int status, string msg) => new() { Success = false, StatusCode = status, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = msg };
}

