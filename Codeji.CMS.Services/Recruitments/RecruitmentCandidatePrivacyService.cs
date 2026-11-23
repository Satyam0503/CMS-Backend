using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentCandidatePrivacyService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<ApplicantLogs> logs,
    IMongoDbRepository<RecruitmentEnterpriseConfig> configs,
    IHttpContextAccessor context,
    IRecruitmentCandidateDataRedactor redactor,
    IMongoDbRepository<RecruitmentHandoff>? handoffs = null) : IRecruitmentCandidatePrivacyService
{
    private static readonly HashSet<string> Actions = ["Grant", "Revoke", "Anonymize"];
    public async Task<Result<string>> Apply(RecruitmentCandidatePrivacyCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context); var key = command.IdempotencyKey?.Trim(); var action = command.Action?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(command.ApplicantId) || !Actions.Contains(action ?? string.Empty)) return Fail(400, "Company, idempotency key, and a valid privacy action are required.");
        if ((command.Reason?.Trim().Length ?? 0) > 500) return Fail(400, "Privacy action reasons are limited to 500 characters.");
        var reason = NormalizeReason(command.Reason);
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId.Trim() && !x.IsDeleted);
        if (applicant is null) return Fail(404, "Applicant was not found in the current company.");
        var commandFingerprint = Fingerprint(command, action!, reason);
        if (action == "Anonymize" && handoffs is not null &&
            await handoffs.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted) is not null)
            return Fail(409, "This candidate has completed hiring handoff. Candidate erasure is paused to protect employee and payroll records; use the company employee privacy process.");
        if (applicant.LastPrivacyCommandIdempotencyKey == key)
        {
            if (!string.Equals(applicant.LastPrivacyCommandFingerprint, commandFingerprint, StringComparison.Ordinal))
                return Fail(409, "This idempotency key was already used for a different privacy command. Submit the intended command with a new key.");
            if (applicant.ConsentStatus == "Anonymized" && applicant.AnonymizationCleanupPending)
            {
                var cleanup = await CompleteAnonymizationCleanup(applicant, companyId, actorUserId);
                if (!cleanup) return Fail(503, "The candidate was anonymized, but related records are still being redacted. Retry this privacy action shortly.");
            }
            if (!await EnsurePrivacyAuditLog(applicant, companyId, key!, action!, reason, actorUserId))
                return Fail(503, "Privacy state was saved, but its audit record could not be confirmed. Retry this privacy action shortly.");
            return Ok(applicant.ApplicantId, "Privacy command already applied.");
        }
        // Preserve retries issued by older versions while avoiding reuse of the shared command key for new privacy actions.
        if (string.IsNullOrEmpty(applicant.LastPrivacyCommandIdempotencyKey) && applicant.LastCommandIdempotencyKey == key &&
            ((action == "Anonymize" && applicant.ConsentStatus == "Anonymized" && applicant.AnonymizationCleanupPending) ||
             (action == "Grant" && applicant.ConsentStatus == "Granted") ||
             (action == "Revoke" && applicant.ConsentStatus == "Revoked")))
        {
            if (action == "Anonymize")
            {
                var cleanup = await CompleteAnonymizationCleanup(applicant, companyId, actorUserId);
                if (!cleanup) return Fail(503, "The candidate was anonymized, but related records are still being redacted. Retry this privacy action shortly.");
            }
            if (!await EnsurePrivacyAuditLog(applicant, companyId, key!, action!, reason, actorUserId))
                return Fail(503, "Privacy state was saved, but its audit record could not be confirmed. Retry this privacy action shortly.");
            return Ok(applicant.ApplicantId, "Privacy command already applied.");
        }
        if (applicant.ConsentStatus == "Anonymized") return Fail(409, "Anonymized candidate records are terminal and cannot be changed.");
        if (action == "Anonymize" && string.IsNullOrWhiteSpace(command.Reason)) return Fail(400, "A reason is required before anonymization.");
        string? resumePathToDelete = null;
        string? originalEmail = null;
        if (action == "Grant") { var config = await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted); var retentionDays = command.RetentionDays ?? config?.DefaultCandidateRetentionDays ?? 730; if (retentionDays is < 30 or > 3650) return Fail(400, "Candidate retention must be between 30 and 3650 days."); var nowUtc = DateTime.UtcNow; applicant.ConsentStatus = "Granted"; applicant.ConsentGrantedAtUtc = nowUtc; applicant.RetentionUntilUtc = nowUtc.AddDays(retentionDays); }
        else if (action == "Revoke") { applicant.ConsentStatus = "Revoked"; applicant.ConsentGrantedAtUtc = null; }
        else {
            originalEmail = applicant.Email;
            if (!string.IsNullOrWhiteSpace(applicant.ResumeUrl))
            {
                try { resumePathToDelete = RecruitmentResumeStorage.Resolve(applicant.ResumeUrl); }
                catch { }
            }
            PublicCareerApplicationRules.AnonymizeApplicant(applicant, DateTime.UtcNow);
        }
        applicant.LastPrivacyCommandIdempotencyKey = key;
        applicant.LastPrivacyCommandFingerprint = commandFingerprint;
        applicant.Version = Math.Max(1, applicant.Version) + 1; applicant.UpdatedBy = actorUserId; applicant.UpdatedDate = DateTime.UtcNow;
        var expectedVersion = Math.Max(1, applicant.Version) - 1;
        var updated = await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && (x.Version == expectedVersion || x.Version == 0)), applicant); if (!updated.Success) return Fail(409, "Applicant privacy state could not be saved.");
        if (action == "Anonymize" && resumePathToDelete is not null && File.Exists(resumePathToDelete))
        {
            try { File.Delete(resumePathToDelete); }
            catch { /* The orphan cleanup worker will remove unreferenced private files after its safety window. */ }
        }
        if (action == "Anonymize" && !await CompleteAnonymizationCleanup(applicant, companyId, actorUserId, originalEmail))
            return Fail(503, "The candidate was anonymized, but related records are still being redacted. Retry this privacy action shortly.");
        if (!await EnsurePrivacyAuditLog(applicant, companyId, key!, action!, reason, actorUserId))
            return Fail(503, "Privacy state was saved, but its audit record could not be confirmed. Retry this privacy action shortly.");
        return Ok(applicant.ApplicantId, $"Candidate privacy action {action} applied.");
    }

    private async Task<bool> EnsurePrivacyAuditLog(Applicant applicant, string companyId, string idempotencyKey, string action, string? reason, string actorUserId)
    {
        try
        {
            var existing = await logs.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId &&
                x.EventType == "CandidatePrivacyChanged" && x.IdempotencyKey == idempotencyKey && !x.IsDeleted);
            if (existing is not null) return true;
            var saved = await logs.AddOne(new ApplicantLogs
            {
                CompanyId = companyId,
                ApplicantId = applicant.ApplicantId,
                ApplicantName = "Privacy-controlled candidate",
                JobRole = applicant.VacancyId,
                UserId = actorUserId,
                ActivityCategory = 0,
                Description = string.IsNullOrEmpty(reason)
                    ? $"Candidate privacy action: {action}."
                    : $"Candidate privacy action: {action}. Reason: {reason}",
                EventType = "CandidatePrivacyChanged",
                CurrentStage = applicant.ConsentStatus,
                IdempotencyKey = idempotencyKey,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId
            });
            return saved?.Success == true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CompleteAnonymizationCleanup(Applicant applicant, string companyId, string actorUserId, string? originalEmail = null)
    {
        try
        {
            if (!RecruitmentResumeStorage.DeleteQuarantined(applicant.AnonymizationCleanupResumeQuarantineFileName))
                return false;
            await redactor.RedactAsync(companyId, applicant.ApplicantId, originalEmail ?? applicant.AnonymizationCleanupEmail, actorUserId, DateTime.UtcNow);
            int expectedVersion = applicant.Version;
            applicant.AnonymizationCleanupPending = false;
            applicant.AnonymizationCleanupEmail = null;
            applicant.AnonymizationCleanupResumeQuarantineFileName = null;
            applicant.AnonymizationCleanupCompletedAtUtc = DateTime.UtcNow;
            applicant.Version = Math.Max(1, expectedVersion) + 1;
            applicant.UpdatedBy = actorUserId;
            applicant.UpdatedDate = DateTime.UtcNow;
            var saved = await applicants.Update(
                Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId &&
                    x.ConsentStatus == "Anonymized" && (x.Version == expectedVersion || x.Version == 0)),
                applicant);
            return saved.Success;
        }
        catch
        {
            return false;
        }
    }
    private static Result<string> Ok(string id, string msg) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = msg };
    private static Result<string> Fail(int status, string msg) => new() { Success = false, StatusCode = status, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = msg };
    private static string Fingerprint(RecruitmentCandidatePrivacyCommand command, string action, string? reason)
    {
        var canonical = string.Join("\n", command.ApplicantId.Trim(), action.ToUpperInvariant(),
            command.RetentionDays?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            reason ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return null;
        var lineSafe = new string(reason.Trim().Select(character => char.IsControl(character) ? ' ' : character).ToArray());
        return string.Join(' ', lineSafe.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}


