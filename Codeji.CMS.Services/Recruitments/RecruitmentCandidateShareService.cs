using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentCandidateShareService(
    IMongoDbRepository<RecruitmentCandidateShare> shares,
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<EmpUser> employees,
    IHttpContextAccessor context) : IRecruitmentCandidateShareService
{
    public async Task<Result<string>> Grant(RecruitmentCandidateShareCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context); var key = command.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(actorUserId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(command.ApplicantId) || string.IsNullOrWhiteSpace(command.SharedWithUserId)) return Fail("Company, applicant, target user, actor, and idempotency key are required.");
        if (!string.Equals(command.Scope?.Trim(), "View", StringComparison.OrdinalIgnoreCase)) return Fail("Only View sharing is supported.");
        if (command.ExpiresAtUtc.HasValue && command.ExpiresAtUtc.Value <= DateTime.UtcNow) return Fail("Share expiry must be in the future.");
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId.Trim() && !x.IsDeleted);
        if (applicant is null) return Fail("Applicant was not found in the current company.", StatusCodes.Status404NotFound);
        var target = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == command.SharedWithUserId.Trim() && x.Status && !x.IsDeleted);
        if (target is null) return Fail("The target user is not active in the current company.", StatusCodes.Status404NotFound);
        var existing = await shares.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.SharedWithUserId == target.UserId && !x.IsDeleted);
        if (existing?.LastCommandIdempotencyKey == key) return Ok(existing.ShareId, "Candidate share already granted.");
        if (existing is null) { existing = new RecruitmentCandidateShare { CompanyId = companyId, ApplicantId = applicant.ApplicantId, SharedWithUserId = target.UserId, Scope = "View", CreatedBy = actorUserId }; }
        existing.Scope = "View"; existing.ExpiresAtUtc = command.ExpiresAtUtc; existing.RevokedAtUtc = null; existing.LastCommandIdempotencyKey = key; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
        var saved = string.IsNullOrWhiteSpace(existing.ShareId) ? await shares.AddOne(existing) : await shares.Update(Builders<RecruitmentCandidateShare>.Filter.Where(x => x.CompanyId == companyId && x.ShareId == existing.ShareId && !x.IsDeleted), existing);
        return saved.Success ? Ok(existing.ShareId, "Candidate access granted to the explicitly shared user.") : Fail("Candidate share could not be saved.", StatusCodes.Status409Conflict);
    }
    public async Task<Result<string>> Revoke(string shareId, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context); if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(shareId) || string.IsNullOrWhiteSpace(actorUserId)) return Fail("Company, share, and actor are required.");
        var existing = await shares.FirstOrDefault(x => x.CompanyId == companyId && x.ShareId == shareId.Trim() && !x.IsDeleted);
        if (existing is null) return Fail("Candidate share was not found in the current company.", StatusCodes.Status404NotFound);
        if (existing.RevokedAtUtc.HasValue) return Ok(existing.ShareId, "Candidate share was already revoked.");
        existing.RevokedAtUtc = DateTime.UtcNow; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
        var saved = await shares.Update(Builders<RecruitmentCandidateShare>.Filter.Where(x => x.CompanyId == companyId && x.ShareId == existing.ShareId && x.RevokedAtUtc == null && !x.IsDeleted), existing);
        return saved.Success ? Ok(existing.ShareId, "Candidate access revoked.") : Fail("Candidate share changed concurrently.", StatusCodes.Status409Conflict);
    }
    public async Task<Result<RecruitmentCandidateShareItem>> GetForApplicant(string applicantId)
    {
        var companyId = CurrentContext.CompanyId(context); if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(applicantId)) return new Result<RecruitmentCandidateShareItem> { Success = false, StatusCode = 400, Message = "Company and applicant are required." };
        var rows = (await shares.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId.Trim() && !x.IsDeleted && x.RevokedAtUtc == null && (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc > DateTime.UtcNow))).Select(x => new RecruitmentCandidateShareItem { ShareId = x.ShareId, ApplicantId = x.ApplicantId, SharedWithUserId = x.SharedWithUserId, Scope = x.Scope, ExpiresAtUtc = x.ExpiresAtUtc, RevokedAtUtc = x.RevokedAtUtc }).ToList();
        return new Result<RecruitmentCandidateShareItem> { Success = true, StatusCode = 200, MethodResults = rows, TotalRecords = rows.Count, Message = "Explicit candidate shares loaded." };
    }
    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
    private static Result<string> Fail(string message, int status = 400) => new() { Success = false, StatusCode = status, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = message };
}

