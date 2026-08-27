using System.Security.Cryptography;
using System.Text;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Employees;

/// <summary>Restricted former-employee portal. It never creates an HRMS identity or grants company-wide access.</summary>
public sealed class FormerEmployeePortalService(
    IMongoDbRepository<EmployeeExit> exits,
    IMongoDbRepository<FormerEmployeeAccessToken> tokens,
    IRoleService roles,
    IHttpContextAccessor httpContextAccessor,
    IMiddlewareService mail) : IFormerEmployeePortalService
{
    private const int MaxInviteHours = 168;
    private const int MaxAttempts = 10;

    public async Task<Result<FormerEmployeePortalInviteDto>> CreateInvite(CreateFormerEmployeePortalInviteRequest request)
    {
        var companyId = CurrentContext.CompanyId(httpContextAccessor);
        var actorId = CurrentContext.UserId(httpContextAccessor);
        if (request is null || request.ValidForHours is < 1 or > MaxInviteHours || !await CanManage(companyId, actorId)) return Failed<FormerEmployeePortalInviteDto>("FORMER_PORTAL_INVITE_FORBIDDEN");

        var exit = await exits.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
        if (exit is null || exit.Version != request.Version || exit.PolicySnapshot?.EnableFormerEmployeePortal != true || string.IsNullOrWhiteSpace(exit.PostExitAccess?.PersonalEmail)) return Failed<FormerEmployeePortalInviteDto>("FORMER_PORTAL_NOT_READY");

        exit.PostExitAccess!.Enabled = true;
        exit.Version++; exit.UpdatedBy = actorId; exit.UpdatedDate = DateTime.UtcNow;
        exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FormerEmployeePortalInviteRotationStarted", ActorUserId = actorId });
        var updated = await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        if (!updated.Success) return Failed<FormerEmployeePortalInviteDto>(updated.Message);

        // A replacement link must make every earlier link and session unusable before the
        // new link is issued. The optimistic case update above serializes concurrent rotations.
        var now = DateTime.UtcNow;
        await tokens.GetCollection().UpdateManyAsync(
            Builders<FormerEmployeeAccessToken>.Filter.And(Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.CompanyId, companyId), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.EmployeeExitId, exit.EmployeeExitId), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.RevokedAtUtc, null)),
            Builders<FormerEmployeeAccessToken>.Update.Set(x => x.RevokedAtUtc, now).Set(x => x.SessionRevokedAtUtc, now));

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var expiresAt = now.AddHours(request.ValidForHours);
        var savedToken = await tokens.AddOne(new FormerEmployeeAccessToken { CompanyId = companyId, CreatedBy = actorId, EmployeeExitId = exit.EmployeeExitId, TokenHash = Hash(rawToken), ExpiresAtUtc = expiresAt });
        if (!savedToken.Success) return Failed<FormerEmployeePortalInviteDto>(savedToken.Message);
        var portalUrl = $"{ConfigManager.AppSettings.AppUrl.TrimEnd('/')}/former-employee?token={Uri.EscapeDataString(rawToken)}";
        var delivery = await mail.EmailSendAndSaveWithResult(new EmpEmailLogs
        {
            CompanyId = companyId,
            UserTo = exit.EmployeeUserId,
            UserFrom = actorId,
            Email = exit.PostExitAccess.PersonalEmail!,
            Subject = "Your former-employee portal invitation",
            Body = $"<p>Your secure former-employee portal is ready.</p><p><a href=\"{portalUrl}\">Open the portal</a></p><p>This one-time link expires at {expiresAt:u}.</p>",
            EmailLogType = EnumsHelper.MailType.ResignationMail
        });
        return new Result<FormerEmployeePortalInviteDto>
        {
            Success = true,
            Message = delivery.IsSent ? null : "FORMER_PORTAL_INVITE_CREATED_EMAIL_DELIVERY_FAILED",
            MethodResult = new FormerEmployeePortalInviteDto { PortalToken = rawToken, ExpiresAtUtc = expiresAt }
        };
    }

    public async Task<Result<FormerEmployeePortalSessionDto>> RedeemInvite(FormerEmployeePortalTokenRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Token)) return Failed<FormerEmployeePortalSessionDto>("FORMER_PORTAL_TOKEN_INVALID");
        var now = DateTime.UtcNow;
        var rawSession = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var sessionExpiry = now.AddMinutes(30);
        var update = await tokens.GetCollection().FindOneAndUpdateAsync(
            Builders<FormerEmployeeAccessToken>.Filter.And(
                Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.TokenHash, Hash(request.Token)),
                Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.UsedAtUtc, null),
                Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.RevokedAtUtc, null),
                Builders<FormerEmployeeAccessToken>.Filter.Lte(x => x.Attempts, MaxAttempts),
                Builders<FormerEmployeeAccessToken>.Filter.Gte(x => x.ExpiresAtUtc, now)),
            Builders<FormerEmployeeAccessToken>.Update.Set(x => x.UsedAtUtc, now).Set(x => x.SessionTokenHash, Hash(rawSession)).Set(x => x.SessionExpiresAtUtc, sessionExpiry).Inc(x => x.Attempts, 1),
            new FindOneAndUpdateOptions<FormerEmployeeAccessToken> { ReturnDocument = ReturnDocument.After });
        return update is null ? Failed<FormerEmployeePortalSessionDto>("FORMER_PORTAL_TOKEN_INVALID_OR_EXPIRED") : new Result<FormerEmployeePortalSessionDto> { Success = true, MethodResult = new FormerEmployeePortalSessionDto { SessionToken = rawSession, ExpiresAtUtc = sessionExpiry } };
    }

    public async Task<Result> RevokeAccess(RevokeFormerEmployeePortalAccessRequest request)
    {
        var companyId = CurrentContext.CompanyId(httpContextAccessor);
        var actorId = CurrentContext.UserId(httpContextAccessor);
        if (request is null || string.IsNullOrWhiteSpace(request.Reason) || !await CanManage(companyId, actorId)) return new Result { Success = false, Message = "FORMER_PORTAL_REVOKE_FORBIDDEN" };
        var exit = await exits.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
        if (exit is null || exit.Version != request.Version || exit.PostExitAccess is null) return new Result { Success = false, Message = "FORMER_PORTAL_REVOKE_NOT_FOUND" };

        // Disable at the case level first; session resolution also checks this flag, so active
        // sessions are denied even if the subsequent token cleanup has to be retried.
        exit.PostExitAccess.Enabled = false; exit.Version++; exit.UpdatedBy = actorId; exit.UpdatedDate = DateTime.UtcNow;
        exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FormerEmployeePortalAccessRevoked", ActorUserId = actorId, Notes = request.Reason.Trim() });
        var saved = await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        if (!saved.Success) return saved;
        var now = DateTime.UtcNow;
        await tokens.GetCollection().UpdateManyAsync(
            Builders<FormerEmployeeAccessToken>.Filter.And(Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.CompanyId, companyId), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.EmployeeExitId, exit.EmployeeExitId), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.RevokedAtUtc, null)),
            Builders<FormerEmployeeAccessToken>.Update.Set(x => x.RevokedAtUtc, now).Set(x => x.SessionRevokedAtUtc, now));
        return new Result { Success = true };
    }

    public async Task<Result<FormerEmployeePortalCaseDto>> GetCase(string sessionToken)
    {
        var (token, exit) = await ResolveSession(sessionToken);
        if (token is null || exit is null) return Failed<FormerEmployeePortalCaseDto>("FORMER_PORTAL_SESSION_INVALID");
        return new Result<FormerEmployeePortalCaseDto>
        {
            Success = true,
            MethodResult = new FormerEmployeePortalCaseDto
            {
                EmployeeExitId = exit.EmployeeExitId, Status = exit.Status, LastWorkingDate = exit.LastWorkingDate,
                Requests = exit.FormerEmployeeRequests.Select(x => new FormerEmployeePortalRequestDto { RequestId = x.RequestId, Type = x.Type, Status = x.Status, Details = x.Details, RequestedAtUtc = x.RequestedAtUtc }).ToList(),
                Documents = exit.Evidence.Where(x => x.Visibility == "EmployeeOnly" && x.Status == "Verified" && !string.IsNullOrWhiteSpace(x.StorageKey)).Select(x => new EmployeeExitEvidenceDto { EvidenceId = x.EvidenceId, FileName = x.FileName, ContentType = x.ContentType }).ToList()
            }
        };
    }

    public async Task<Result> CreateRequest(string sessionToken, FormerEmployeePortalRequest request)
    {
        var (_, exit) = await ResolveSession(sessionToken);
        if (exit is null || request is null || string.IsNullOrWhiteSpace(request.Details) || request.Type is not ("RelievingLetter" or "ExperienceLetter" or "FnFStatement" or "TaxDocument" or "Correction" or "Other")) return new Result { Success = false, Message = "FORMER_PORTAL_REQUEST_INVALID" };
        exit.FormerEmployeeRequests.Add(new EmployeeExitFormerEmployeeRequest { Type = request.Type, Details = request.Details.Trim() });
        exit.Version++; exit.UpdatedBy = "former-employee-portal"; exit.UpdatedDate = DateTime.UtcNow;
        exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FormerEmployeePortalRequestCreated", ActorUserId = "former-employee-portal", Notes = request.Type });
        return await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == exit.CompanyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == exit.Version - 1 && x.IsActive), exit);
    }

    public async Task<Result<EmployeeExitEvidenceDto>> GetDocument(string sessionToken, string evidenceId)
    {
        var (_, exit) = await ResolveSession(sessionToken);
        var evidence = exit?.Evidence.FirstOrDefault(x => x.EvidenceId == evidenceId && x.Visibility == "EmployeeOnly" && x.Status == "Verified" && !string.IsNullOrWhiteSpace(x.StorageKey));
        return evidence is null ? Failed<EmployeeExitEvidenceDto>("FORMER_PORTAL_DOCUMENT_FORBIDDEN") : new Result<EmployeeExitEvidenceDto> { Success = true, MethodResult = new EmployeeExitEvidenceDto { EvidenceId = evidence.EvidenceId, StorageKey = evidence.StorageKey, FileName = evidence.FileName, ContentType = evidence.ContentType } };
    }

    private async Task<(FormerEmployeeAccessToken? Token, EmployeeExit? Exit)> ResolveSession(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken)) return (null, null);
        var now = DateTime.UtcNow;
        var token = await tokens.FirstOrDefault(x => x.SessionTokenHash == Hash(sessionToken) && x.SessionExpiresAtUtc >= now && x.SessionRevokedAtUtc == null && x.RevokedAtUtc == null);
        if (token is null) return (null, null);
        // This is a post-employment portal: completed resignations/terminations remain
        // accessible, while pending, withdrawn, cancelled, and rejoined cases do not.
        var exit = await exits.FirstOrDefault(x => x.CompanyId == token.CompanyId && x.EmployeeExitId == token.EmployeeExitId && !x.IsDeleted && x.PostExitAccess != null && x.PostExitAccess.Enabled && (x.Status == "Resigned" || x.Status == "Terminated"));
        return (token, exit);
    }

    private Task<bool> CanManage(string companyId, string actorId) => roles.VerifyUserAccess(AppModule.Offboarding, [Permission.Edit, Permission.Override], actorId, companyId);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static Result<T> Failed<T>(string message) => new() { Success = false, Message = message };
}
