using System.Security.Cryptography;
using System.Text;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace Codeji.CMS.Services.Employees;

/// <summary>Restricted former-employee portal. It never creates an HRMS identity or grants company-wide access.</summary>
public sealed class FormerEmployeePortalService(
    IMongoDbRepository<EmployeeExit> exits,
    IMongoDbRepository<FormerEmployeeAccessToken> tokens,
    IMongoDbRepository<EmployeeExitOutbox> outbox,
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<Company> companies,
    IRoleService roles,
    IHttpContextAccessor httpContextAccessor,
    IMiddlewareService mail,
    ILogger<FormerEmployeePortalService> logger) : IFormerEmployeePortalService
{
    private const int MaxInviteHours = 168;
    private const int MaxAttempts = 10;
    private const int OtpMinutes = 10;
    private const int SessionDays = 7;

    public async Task<Result<FormerEmployeePortalInviteDto>> CreateInvite(CreateFormerEmployeePortalInviteRequest request)
    {
        var companyId = CurrentContext.CompanyId(httpContextAccessor);
        var actorId = CurrentContext.UserId(httpContextAccessor);
        if (request is null || request.ValidForHours is < 1 or > MaxInviteHours || !await CanManage(companyId, actorId)) return Failed<FormerEmployeePortalInviteDto>("FORMER_PORTAL_INVITE_FORBIDDEN");
        var company = await companies.FirstOrDefault(x => x.CompanyId == companyId && x.Status && !x.IsDeleted);
        if (company?.FormerEmployeePortalEnabled != true) return Failed<FormerEmployeePortalInviteDto>("FORMER_PORTAL_COMPANY_DISABLED");

        // The portal is deliberately post-employment.  Invitations are issued after a completed
        // resignation/termination, not during a still-active notice-period workflow.
        var exit = await exits.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status));
        if (exit is null || exit.Version != request.Version || exit.PolicySnapshot?.EnableFormerEmployeePortal != true || string.IsNullOrWhiteSpace(exit.PostExitAccess?.PersonalEmail)) return Failed<FormerEmployeePortalInviteDto>("FORMER_PORTAL_NOT_READY");
        if (exit.PostExitAccess?.PersonalEmailVerified != true) return Failed<FormerEmployeePortalInviteDto>("FORMER_PORTAL_PERSONAL_EMAIL_NOT_VERIFIED");

        exit.PostExitAccess!.Enabled = true;
        exit.Version++; exit.UpdatedBy = actorId; exit.UpdatedDate = DateTime.UtcNow;
        exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FormerEmployeePortalInviteRotationStarted", ActorUserId = actorId });
        var updated = await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && !x.IsDeleted && EmployeeLifecycleStatuses.FormerPortalEligibleStatuses.Contains(x.Status)), exit);
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
            Body = $"<p>Your secure former-employee portal is ready.</p><p><a href=\"{portalUrl}\">Open the portal</a></p><p>This one-time link activates your first session. After that, sign in any time with a fresh email code while HR keeps your access enabled.</p>",
            EmailLogType = EnumsHelper.MailType.ResignationMail
        });
        if (!delivery.IsSent)
        {
            // A link that was not delivered must not silently leave the portal enabled.
            // Revoke the new token and close the case-level gate so an administrator can
            // correct the contact address and deliberately send a fresh invitation.
            var failedAt = DateTime.UtcNow;
            await tokens.GetCollection().UpdateOneAsync(
                Builders<FormerEmployeeAccessToken>.Filter.And(
                    Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.CompanyId, companyId),
                    Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.TokenHash, Hash(rawToken)),
                    Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.RevokedAtUtc, null)),
                Builders<FormerEmployeeAccessToken>.Update.Set(x => x.RevokedAtUtc, failedAt).Set(x => x.SessionRevokedAtUtc, failedAt));

            exit.PostExitAccess.Enabled = false;
            exit.Version++; exit.UpdatedBy = actorId; exit.UpdatedDate = failedAt;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FormerEmployeePortalInviteDeliveryFailed", ActorUserId = actorId });
        var rollback = await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == exit.Version - 1 && !x.IsDeleted && EmployeeLifecycleStatuses.FormerPortalEligibleStatuses.Contains(x.Status)), exit);
            return rollback.Success
                ? Failed<FormerEmployeePortalInviteDto>("FORMER_PORTAL_INVITE_EMAIL_DELIVERY_FAILED")
                : Failed<FormerEmployeePortalInviteDto>("FORMER_PORTAL_INVITE_DELIVERY_FAILED_REVIEW_REQUIRED");
        }
        await UpdateLifecyclePortalState(exit, EmployeeLifecycleStatuses.FormerPortalInvited, actorId, "PORTAL_INVITED");
        return new Result<FormerEmployeePortalInviteDto>
        {
            Success = true,
            // The one-time link is delivered only by email.  Never return a bearer token to an
            // administrator-facing browser response, logs, or notification payload.
            MethodResult = new FormerEmployeePortalInviteDto { ExpiresAtUtc = expiresAt }
        };
    }

    public async Task<Result<FormerEmployeePortalSessionDto>> RedeemInvite(FormerEmployeePortalTokenRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Token)) return Failed<FormerEmployeePortalSessionDto>("FORMER_PORTAL_TOKEN_INVALID");
        var now = DateTime.UtcNow;
        var rawSession = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var sessionExpiry = now.AddDays(SessionDays);
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

    public async Task<Result> RequestOtp(FormerEmployeePortalOtpRequest request)
    {
        // Keep the public response non-enumerating: a caller cannot learn whether this email has portal access.
        if (request is null || string.IsNullOrWhiteSpace(request.PersonalEmail)) return new Result { Success = true };
        var email = request.PersonalEmail.Trim().ToLowerInvariant();
        // Older post-exit records may predate contact-email normalization.  Match in memory
        // after the tenant-safe eligibility filter so a valid portal user is not silently
        // treated as unknown solely because their stored email has casing or whitespace.
        var candidate = (await exits.GetAll(x => !x.IsDeleted && EmployeeLifecycleStatuses.FormerPortalEligibleStatuses.Contains(x.Status) && x.PostExitAccess != null && x.PostExitAccess.Enabled && x.PostExitAccess.PersonalEmailVerified, withDefaultFilter: false))
            .Where(x => string.Equals(x.PostExitAccess?.PersonalEmail?.Trim(), email, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CompletedAtUtc).FirstOrDefault();
        if (candidate is null)
        {
            logger.LogWarning("Former-employee OTP requested for an email without an eligible portal case.");
            return new Result { Success = true };
        }
        var company = await companies.FirstOrDefault(x => x.CompanyId == candidate.CompanyId && x.Status && !x.IsDeleted, withDefaultFilter: false);
        if (company?.FormerEmployeePortalEnabled != true) return new Result { Success = true };

        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var now = DateTime.UtcNow;
        await tokens.GetCollection().UpdateManyAsync(
            Builders<FormerEmployeeAccessToken>.Filter.And(Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.CompanyId, candidate.CompanyId), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.EmployeeExitId, candidate.EmployeeExitId), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.OtpUsedAtUtc, null)),
            Builders<FormerEmployeeAccessToken>.Update.Set(x => x.OtpUsedAtUtc, now));
        var saved = await tokens.AddOne(new FormerEmployeeAccessToken { CompanyId = candidate.CompanyId, CreatedBy = "former-employee-otp", EmployeeExitId = candidate.EmployeeExitId, OtpHash = Hash($"{email.ToUpperInvariant()}:{otp}"), OtpExpiresAtUtc = now.AddMinutes(OtpMinutes) });
        if (!saved.Success) return new Result { Success = false, Message = "FORMER_PORTAL_OTP_CREATE_FAILED" };
        var delivery = await mail.EmailSendAndSaveWithResult(new EmpEmailLogs { CompanyId = candidate.CompanyId, UserTo = candidate.EmployeeUserId, UserFrom = "former-employee-otp", Email = email, Subject = "Your former-employee portal code", Body = $"<p>Your verification code is <strong>{otp}</strong>.</p><p>It expires in {OtpMinutes} minutes. Do not share this code.</p>", EmailLogType = EnumsHelper.MailType.ResignationMail });
        if (delivery.IsSent)
        {
            logger.LogInformation("Former-employee OTP accepted by the mail provider for employee exit {EmployeeExitId}.", candidate.EmployeeExitId);
            return new Result { Success = true };
        }

        logger.LogWarning("Former-employee OTP delivery was rejected for employee exit {EmployeeExitId}: {DeliveryError}", candidate.EmployeeExitId, delivery.Error);

        // Do not display a success banner or leave a usable OTP behind when the configured
        // mail provider rejects the message. A later resend starts a fresh code/delivery.
        await tokens.GetCollection().UpdateOneAsync(
            Builders<FormerEmployeeAccessToken>.Filter.And(
                Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.CompanyId, candidate.CompanyId),
                Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.EmployeeExitId, candidate.EmployeeExitId),
                Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.OtpHash, Hash($"{email.ToUpperInvariant()}:{otp}")),
                Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.OtpUsedAtUtc, null)),
            Builders<FormerEmployeeAccessToken>.Update.Set(x => x.OtpUsedAtUtc, DateTime.UtcNow));
        return new Result { Success = false, Message = "FORMER_PORTAL_OTP_EMAIL_DELIVERY_FAILED" };
    }

    public async Task<Result<FormerEmployeePortalSessionDto>> VerifyOtp(FormerEmployeePortalOtpVerifyRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PersonalEmail) || request.Otp?.Length != 6 || !request.Otp.All(char.IsDigit)) return Failed<FormerEmployeePortalSessionDto>("FORMER_PORTAL_OTP_INVALID");
        var email = request.PersonalEmail.Trim().ToLowerInvariant(); var now = DateTime.UtcNow; var rawSession = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var token = await tokens.GetCollection().FindOneAndUpdateAsync(
            Builders<FormerEmployeeAccessToken>.Filter.And(Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.OtpHash, Hash($"{email.ToUpperInvariant()}:{request.Otp}")), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.OtpUsedAtUtc, null), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.RevokedAtUtc, null), Builders<FormerEmployeeAccessToken>.Filter.Lte(x => x.OtpAttempts, MaxAttempts), Builders<FormerEmployeeAccessToken>.Filter.Gte(x => x.OtpExpiresAtUtc, now)),
            Builders<FormerEmployeeAccessToken>.Update.Set(x => x.OtpUsedAtUtc, now).Set(x => x.SessionTokenHash, Hash(rawSession)).Set(x => x.SessionExpiresAtUtc, now.AddDays(SessionDays)).Inc(x => x.OtpAttempts, 1), new FindOneAndUpdateOptions<FormerEmployeeAccessToken> { ReturnDocument = ReturnDocument.After });
        if (token is null) return Failed<FormerEmployeePortalSessionDto>("FORMER_PORTAL_OTP_INVALID_OR_EXPIRED");
        var exit = await exits.FirstOrDefault(x => x.CompanyId == token.CompanyId && x.EmployeeExitId == token.EmployeeExitId && !x.IsDeleted && x.PostExitAccess != null && x.PostExitAccess.Enabled && EmployeeLifecycleStatuses.FormerPortalEligibleStatuses.Contains(x.Status), withDefaultFilter: false);
        var company = exit is null ? null : await companies.FirstOrDefault(x => x.CompanyId == exit.CompanyId && x.Status && !x.IsDeleted, withDefaultFilter: false);
        if (exit is null || company?.FormerEmployeePortalEnabled != true || !EmploymentExitRules.CanUseFormerEmployeePortal(exit)) { await tokens.GetCollection().UpdateOneAsync(x => x.FormerEmployeeAccessTokenId == token.FormerEmployeeAccessTokenId, Builders<FormerEmployeeAccessToken>.Update.Set(x => x.SessionRevokedAtUtc, now)); return Failed<FormerEmployeePortalSessionDto>("FORMER_PORTAL_ACCESS_DISABLED"); }
        return new Result<FormerEmployeePortalSessionDto> { Success = true, MethodResult = new FormerEmployeePortalSessionDto { SessionToken = rawSession, ExpiresAtUtc = now.AddDays(SessionDays), Theme = company.AppTheme, CompanyName = company.CompanyName, CompanyLogo = company.CompanyLogo } };
    }

    public async Task<Result> Logout(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken)) return new Result { Success = true };
        var now = DateTime.UtcNow;
        // Logout is deliberately idempotent: local state can always be cleared even if a
        // previous click already invalidated the server-side session.
        await tokens.GetCollection().UpdateManyAsync(
            Builders<FormerEmployeeAccessToken>.Filter.And(
                Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.SessionTokenHash, Hash(sessionToken)),
                Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.SessionRevokedAtUtc, null)),
            Builders<FormerEmployeeAccessToken>.Update.Set(x => x.SessionRevokedAtUtc, now));
        return new Result { Success = true };
    }

    private async Task UpdateLifecyclePortalState(EmployeeExit exit, string portalStatus, string actorId, string reasonCode)
    {
        var employee = await employees.FirstOrDefault(x => x.CompanyId == exit.CompanyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted, withDefaultFilter: false);
        if (employee is null) return;
        employee.Lifecycle ??= new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(exit.ExitType) ?? EmployeeLifecycleStatuses.Terminated };
        var previous = employee.Lifecycle.FormerPortalStatus;
        if (previous == portalStatus) return;
        employee.Lifecycle.FormerPortalStatus = portalStatus;
        employee.Lifecycle.Version++;
        employee.Lifecycle.History.Add(new EmployeeLifecycleStatusHistory
        {
            Dimension = nameof(EmployeeLifecycleState.FormerPortalStatus), PreviousValue = previous, NewValue = portalStatus,
            ReasonCode = reasonCode, EffectiveFromUtc = DateTime.UtcNow, ChangedBy = actorId, ChangedAtUtc = DateTime.UtcNow
        });
        employee.UpdatedBy = actorId;
        employee.UpdatedDate = DateTime.UtcNow;
        await employees.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == exit.CompanyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted), employee);
    }

    public async Task<Result> SetAccess(SetFormerEmployeePortalAccessRequest request)
    {
        var companyId = CurrentContext.CompanyId(httpContextAccessor); var actorId = CurrentContext.UserId(httpContextAccessor);
        if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || !await CanManage(companyId, actorId)) return new Result { Success = false, Message = "FORMER_PORTAL_ACCESS_FORBIDDEN" };
        var exit = await exits.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status));
        if (exit is null || exit.Version != request.Version || exit.PostExitAccess?.PersonalEmailVerified != true) return new Result { Success = false, Message = "FORMER_PORTAL_ACCESS_NOT_READY" };
        exit.PostExitAccess.Enabled = request.Enabled; exit.Version++; exit.UpdatedBy = actorId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = request.Enabled ? "FormerEmployeePortalEnabled" : "FormerEmployeePortalDisabled", ActorUserId = actorId, Notes = request.Reason?.Trim() });
        var saved = await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && !x.IsDeleted), exit);
        if (saved.Success && !request.Enabled) await tokens.GetCollection().UpdateManyAsync(Builders<FormerEmployeeAccessToken>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.RevokedAtUtc == null), Builders<FormerEmployeeAccessToken>.Update.Set(x => x.RevokedAtUtc, DateTime.UtcNow).Set(x => x.SessionRevokedAtUtc, DateTime.UtcNow));
        return saved;
    }

    public async Task<Result> SetCompanyAccess(SetCompanyFormerEmployeePortalRequest request)
    {
        var companyId = CurrentContext.CompanyId(httpContextAccessor); var actorId = CurrentContext.UserId(httpContextAccessor);
        if (request is null || !await CanManage(companyId, actorId)) return new Result { Success = false, Message = "FORMER_PORTAL_COMPANY_ACCESS_FORBIDDEN" };
        var company = await companies.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);
        if (company is null) return new Result { Success = false, Message = "COMPANY_NOT_FOUND" };
        company.FormerEmployeePortalEnabled = request.Enabled; company.UpdatedBy = actorId; company.UpdatedDate = DateTime.UtcNow;
        var saved = await companies.Update(Builders<Company>.Filter.Where(x => x.CompanyId == companyId && !x.IsDeleted), company);
        if (saved.Success && !request.Enabled) await tokens.GetCollection().UpdateManyAsync(Builders<FormerEmployeeAccessToken>.Filter.Where(x => x.CompanyId == companyId && x.RevokedAtUtc == null), Builders<FormerEmployeeAccessToken>.Update.Set(x => x.RevokedAtUtc, DateTime.UtcNow).Set(x => x.SessionRevokedAtUtc, DateTime.UtcNow));
        return saved;
    }

    public async Task<Result> VerifyPostExitContact(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return new Result { Success = false, Message = "POST_EXIT_CONTACT_TOKEN_INVALID" };
        var tokenHash = Hash(token);
        var exit = await exits.FirstOrDefault(x => !x.IsDeleted && x.PostExitAccess != null && x.PostExitAccess.PersonalEmailVerificationExpiresAtUtc >= DateTime.UtcNow && x.PostExitAccess.PersonalEmailVerificationTokenHash == tokenHash, withDefaultFilter: false);
        if (exit is null)
        {
            var alreadyVerified = await exits.FirstOrDefault(x => !x.IsDeleted && x.PostExitAccess != null && x.PostExitAccess.PersonalEmailVerified && x.PostExitAccess.PersonalEmailVerifiedTokenHash == tokenHash, withDefaultFilter: false);
            if (alreadyVerified is not null)
                return new Result { Success = true, Message = "POST_EXIT_CONTACT_ALREADY_VERIFIED" };
            return new Result { Success = false, Message = "POST_EXIT_CONTACT_TOKEN_INVALID_OR_EXPIRED" };
        }
        exit.PostExitAccess!.PersonalEmailVerified = true; exit.PostExitAccess.PersonalEmailVerifiedAtUtc = DateTime.UtcNow; exit.PostExitAccess.PersonalEmailVerifiedTokenHash = tokenHash; exit.PostExitAccess.PersonalEmailVerificationTokenHash = null; exit.PostExitAccess.PersonalEmailVerificationExpiresAtUtc = null;
        exit.Version++; exit.UpdatedBy = "post-exit-contact-verification"; exit.UpdatedDate = DateTime.UtcNow;
        exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "PostExitContactVerified", ActorUserId = "post-exit-contact-verification" });
        return await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == exit.CompanyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == exit.Version - 1 && !x.IsDeleted), exit);
    }

    public async Task<Result> RevokeAccess(RevokeFormerEmployeePortalAccessRequest request)
    {
        var companyId = CurrentContext.CompanyId(httpContextAccessor);
        var actorId = CurrentContext.UserId(httpContextAccessor);
        if (request is null || string.IsNullOrWhiteSpace(request.Reason) || !await CanManage(companyId, actorId)) return new Result { Success = false, Message = "FORMER_PORTAL_REVOKE_FORBIDDEN" };
        var exit = await exits.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status));
        if (exit is null || exit.Version != request.Version || exit.PostExitAccess is null) return new Result { Success = false, Message = "FORMER_PORTAL_REVOKE_NOT_FOUND" };

        // Disable at the case level first; session resolution also checks this flag, so active
        // sessions are denied even if the subsequent token cleanup has to be retried.
        exit.PostExitAccess.Enabled = false; exit.Version++; exit.UpdatedBy = actorId; exit.UpdatedDate = DateTime.UtcNow;
        exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FormerEmployeePortalAccessRevoked", ActorUserId = actorId, Notes = request.Reason.Trim() });
        var saved = await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && !x.IsDeleted && EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status)), exit);
        if (!saved.Success) return saved;
        var now = DateTime.UtcNow;
        await tokens.GetCollection().UpdateManyAsync(
            Builders<FormerEmployeeAccessToken>.Filter.And(Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.CompanyId, companyId), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.EmployeeExitId, exit.EmployeeExitId), Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.RevokedAtUtc, null)),
            Builders<FormerEmployeeAccessToken>.Update.Set(x => x.RevokedAtUtc, now).Set(x => x.SessionRevokedAtUtc, now));
        await UpdateLifecyclePortalState(exit, EmployeeLifecycleStatuses.FormerPortalRevoked, actorId, "PORTAL_REVOKED");
        return new Result { Success = true };
    }

    public async Task<Result<FormerEmployeePortalCaseDto>> GetCase(string sessionToken)
    {
        var (token, exit) = await ResolveSession(sessionToken);
        if (token is null || exit is null) return Failed<FormerEmployeePortalCaseDto>("FORMER_PORTAL_SESSION_INVALID");
        var employee = await employees.FirstOrDefault(x => x.CompanyId == exit.CompanyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted, withDefaultFilter: false);
        return new Result<FormerEmployeePortalCaseDto>
        {
            Success = true,
            MethodResult = new FormerEmployeePortalCaseDto
            {
                CompanyName = (await companies.FirstOrDefault(x => x.CompanyId == exit.CompanyId && x.Status && !x.IsDeleted, withDefaultFilter: false))?.CompanyName,
                CompanyLogo = (await companies.FirstOrDefault(x => x.CompanyId == exit.CompanyId && x.Status && !x.IsDeleted, withDefaultFilter: false))?.CompanyLogo,
                EmployeeExitId = exit.EmployeeExitId, Status = exit.Status, LastWorkingDate = exit.LastWorkingDate,
                Profile = employee is null ? null : new FormerEmployeePortalProfileDto
                {
                    FullName = string.Join(" ", new[] { employee.FirstName, employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    EmployeeId = employee.EmployeeId,
                    JobRole = employee.JobRole,
                    Department = employee.Department,
                    DateOfJoining = DateTime.TryParse(employee.DateOfJoining, out var joiningDate) ? joiningDate.Date : null,
                    LastWorkingDate = exit.LastWorkingDate
                },
                Requests = exit.FormerEmployeeRequests.Select(x => new FormerEmployeePortalRequestDto { RequestId = x.RequestId, Type = x.Type, Status = x.Status, Details = x.Details, RequestedAtUtc = x.RequestedAtUtc, EvidenceId = x.EvidenceId, IssuedAtUtc = x.IssuedAtUtc }).ToList(),
                Documents = exit.Evidence.Where(x => x.Visibility == "EmployeeOnly" && x.Status == "Verified" && x.IssuedToFormerEmployeeAtUtc.HasValue && !string.IsNullOrWhiteSpace(x.StorageKey)).Select(x => new EmployeeExitEvidenceDto { EvidenceId = x.EvidenceId, FileName = x.FileName, ContentType = x.ContentType }).ToList()
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
        // Former-employee sessions are valid only after completion, where the exit is
        // intentionally inactive.  Do not require IsActive here or every valid portal
        // document request is rejected after its session has been resolved.
        var saved = await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == exit.CompanyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == exit.Version - 1 && !x.IsDeleted && EmployeeLifecycleStatuses.FormerPortalEligibleStatuses.Contains(x.Status)), exit);
        if (saved.Success) await QueueHrNotification(exit, "FormerEmployeeDocumentRequested", $"Document request: {request.Type}", $"A former employee requested {request.Type}.");
        return saved;
    }

    public async Task<Result<EmployeeExitEvidenceDto>> GetDocument(string sessionToken, string evidenceId)
    {
        var (_, exit) = await ResolveSession(sessionToken);
        var evidence = exit?.Evidence.FirstOrDefault(x => x.EvidenceId == evidenceId && x.Visibility == "EmployeeOnly" && x.Status == "Verified" && x.IssuedToFormerEmployeeAtUtc.HasValue && !string.IsNullOrWhiteSpace(x.StorageKey));
        if (evidence is null || exit is null) return Failed<EmployeeExitEvidenceDto>("FORMER_PORTAL_DOCUMENT_FORBIDDEN");
        if (!evidence.SeenByFormerEmployeeAtUtc.HasValue)
        {
            evidence.SeenByFormerEmployeeAtUtc = DateTime.UtcNow;
            exit.Version++; exit.UpdatedBy = "former-employee-portal"; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FormerEmployeeDocumentSeen", ActorUserId = "former-employee-portal", Notes = evidence.FileName });
            var saved = await exits.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == exit.CompanyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == exit.Version - 1 && !x.IsDeleted), exit);
            if (saved.Success) await QueueHrNotification(exit, "FormerEmployeeDocumentSeen", "Former employee viewed a document", $"The former employee viewed {evidence.FileName ?? "an issued document"}.");
        }
        return new Result<EmployeeExitEvidenceDto> { Success = true, MethodResult = new EmployeeExitEvidenceDto { EvidenceId = evidence.EvidenceId, StorageKey = evidence.StorageKey, FileName = evidence.FileName, ContentType = evidence.ContentType } };
    }

    private async Task QueueHrNotification(EmployeeExit exit, string eventType, string title, string body)
    {
        var recipients = await employees.GetAll(x => x.CompanyId == exit.CompanyId && x.Status && !x.IsDeleted, withDefaultFilter: false);
        foreach (var recipient in recipients)
            if (await CanManage(exit.CompanyId, recipient.UserId))
                await outbox.AddOne(new EmployeeExitOutbox { CompanyId = exit.CompanyId, CreatedBy = "former-employee-portal", EmployeeExitId = exit.EmployeeExitId, EventType = eventType, RecipientUserId = recipient.UserId, Title = title, Body = body, TargetId = $"employee-exit-{exit.EmployeeUserId}" });
    }

    private async Task<(FormerEmployeeAccessToken? Token, EmployeeExit? Exit)> ResolveSession(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken)) return (null, null);
        var now = DateTime.UtcNow;
        var token = await tokens.FirstOrDefault(x => x.SessionTokenHash == Hash(sessionToken) && x.SessionExpiresAtUtc >= now && x.SessionRevokedAtUtc == null && x.RevokedAtUtc == null, withDefaultFilter: false);
        if (token is null) return (null, null);
        // This is a post-employment portal: completed resignations/terminations remain
        // accessible, while pending, withdrawn, cancelled, and rejoined cases do not.
        var exit = await exits.FirstOrDefault(x => x.CompanyId == token.CompanyId && x.EmployeeExitId == token.EmployeeExitId && !x.IsDeleted && x.PostExitAccess != null && x.PostExitAccess.Enabled && EmployeeLifecycleStatuses.FormerPortalEligibleStatuses.Contains(x.Status), withDefaultFilter: false);
        if (exit is null) return (token, null);
        var company = await companies.FirstOrDefault(x => x.CompanyId == exit.CompanyId && x.Status && !x.IsDeleted, withDefaultFilter: false);
        return company?.FormerEmployeePortalEnabled == true && EmploymentExitRules.CanUseFormerEmployeePortal(exit) ? (token, exit) : (token, null);
    }

    private Task<bool> CanManage(string companyId, string actorId) => roles.VerifyUserAccess(AppModule.Offboarding, [Permission.PortalManage, Permission.Edit, Permission.Override], actorId, companyId);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static Result<T> Failed<T>(string message) => new() { Success = false, Message = message };
}
