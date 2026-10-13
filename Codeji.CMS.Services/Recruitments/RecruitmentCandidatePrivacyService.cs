using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentCandidatePrivacyService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<ApplicantLogs> logs,
    IMongoDbRepository<RecruitmentEnterpriseConfig> configs,
    IHttpContextAccessor context) : IRecruitmentCandidatePrivacyService
{
    private static readonly HashSet<string> Actions = ["Grant", "Revoke", "Anonymize"];
    public async Task<Result<string>> Apply(RecruitmentCandidatePrivacyCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context); var key = command.IdempotencyKey?.Trim(); var action = command.Action?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(command.ApplicantId) || !Actions.Contains(action ?? string.Empty)) return Fail(400, "Company, idempotency key, and a valid privacy action are required.");
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId.Trim() && !x.IsDeleted);
        if (applicant is null) return Fail(404, "Applicant was not found in the current company.");
        if (applicant.LastCommandIdempotencyKey == key) return Ok(applicant.ApplicantId, "Privacy command already applied.");
        if (applicant.ConsentStatus == "Anonymized") return Fail(409, "Anonymized candidate records are terminal and cannot be changed.");
        if (action == "Anonymize" && string.IsNullOrWhiteSpace(command.Reason)) return Fail(400, "A reason is required before anonymization.");
        if (action == "Grant") { var config = await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted); var retentionDays = command.RetentionDays ?? config?.DefaultCandidateRetentionDays ?? 730; if (retentionDays is < 30 or > 3650) return Fail(400, "Candidate retention must be between 30 and 3650 days."); var nowUtc = DateTime.UtcNow; applicant.ConsentStatus = "Granted"; applicant.ConsentGrantedAtUtc = nowUtc; applicant.RetentionUntilUtc = nowUtc.AddDays(retentionDays); }
        else if (action == "Revoke") { applicant.ConsentStatus = "Revoked"; applicant.ConsentGrantedAtUtc = null; }
        else { applicant.ConsentStatus = "Anonymized"; applicant.AnonymizedAtUtc = DateTime.UtcNow; applicant.Email = $"anonymized-{applicant.ApplicantId}@invalid.local"; applicant.Phone = "0000000000"; applicant.FirstName = "Anonymized"; applicant.LastName = "Candidate"; applicant.ResumeUrl = string.Empty; applicant.CoverLetter = null; applicant.LinkedInUrl = null; applicant.PortfolioUrl = null; }
        applicant.LastCommandIdempotencyKey = key; applicant.Version = Math.Max(1, applicant.Version) + 1; applicant.UpdatedBy = actorUserId; applicant.UpdatedDate = DateTime.UtcNow;
        var expectedVersion = Math.Max(1, applicant.Version) - 1;
        var updated = await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && (x.Version == expectedVersion || x.Version == 0)), applicant); if (!updated.Success) return Fail(409, "Applicant privacy state could not be saved.");
        await logs.AddOne(new ApplicantLogs { CompanyId = companyId, ApplicantId = applicant.ApplicantId, ApplicantName = "Privacy-controlled candidate", JobRole = applicant.VacancyId, UserId = actorUserId, ActivityCategory = 0, Description = $"Candidate privacy action: {action}. {command.Reason?.Trim()}", EventType = "CandidatePrivacyChanged", CurrentStage = applicant.ConsentStatus, CreatedBy = actorUserId, UpdatedBy = actorUserId });
        return Ok(applicant.ApplicantId, $"Candidate privacy action {action} applied.");
    }
    private static Result<string> Ok(string id, string msg) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = msg };
    private static Result<string> Fail(int status, string msg) => new() { Success = false, StatusCode = status, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = msg };
}


