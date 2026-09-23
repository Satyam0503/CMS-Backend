using System.Security.Cryptography;
using System.Text;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.WebUtilities;
using MongoDB.Driver;
using Microsoft.AspNetCore.Http;
using Codeji.CMS.Utility.middlewares;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentExternalActionService(
    IMongoDbRepository<RecruitmentExternalActionToken> tokens,
    IMongoDbRepository<RecruitmentEmailOutbox> outbox,
    IHttpContextAccessor context,
    IMongoDbRepository<Applicant>? applicants = null,
    IMongoDbRepository<RecruitmentOffer>? offers = null,
    IMongoDbRepository<RecruitmentInterview>? interviews = null) : IRecruitmentExternalActionService
{
    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ScheduleInterview", "RescheduleInterview", "CancelInterview", "AcceptOffer", "DeclineOffer", "RequestOfferChange", "Preboarding"
    };

    public async Task<Result<RecruitmentExternalActionTokenDto>> Issue(RecruitmentExternalActionIssueCommand command, string actor)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId)) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Company context is required.");
        var action = AllowedActions.FirstOrDefault(x => string.Equals(x, command.ActionType?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (action is null) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Unsupported recruitment action.");
        var applicantId = command.ApplicantId?.Trim();
        if (string.IsNullOrWhiteSpace(applicantId)) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Applicant is required.");
        var email = command.RecipientEmail.Trim().ToLowerInvariant();
        var minutes = Math.Clamp(command.ExpiresInMinutes, 5, 24 * 60);
        var issuanceKey = command.IssuanceKey.Trim();
        if (issuanceKey.Length < 12) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Issuance key must be at least 12 characters.");
        var applicant = applicants is null ? null : await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted);
        if (applicants is not null && applicant is null) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Applicant does not belong to this company.", 404);
        if (applicant is not null && !string.Equals(applicant.Email?.Trim(), email, StringComparison.OrdinalIgnoreCase)) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Recipient email does not match the candidate.");
        var vacancyId = command.VacancyId?.Trim();
        if (applicant is not null && !string.IsNullOrWhiteSpace(vacancyId) && !string.Equals(applicant.VacancyId, vacancyId, StringComparison.Ordinal)) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Vacancy does not belong to the candidate.", 404);
        var interviewId = command.InterviewId?.Trim();
        var offerId = command.OfferId?.Trim();
        if (action is "ScheduleInterview" or "RescheduleInterview" or "CancelInterview")
        {
            if (string.IsNullOrWhiteSpace(interviewId)) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Interview is required for this action.");
            var interview = interviews is null ? null : await interviews.FirstOrDefault(x => x.CompanyId == companyId && x.InterviewId == interviewId && x.ApplicantId == applicantId && !x.IsDeleted);
            if (interviews is not null && interview is null) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Interview does not belong to the candidate.", 404);
            if (interview is not null && command.ResourceVersion.HasValue && command.ResourceVersion.Value != interview.Version) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.ConcurrencyConflict, "Interview has changed. Refresh and issue a new link.", 409);
        }
        if (action is "AcceptOffer" or "DeclineOffer" or "RequestOfferChange")
        {
            if (string.IsNullOrWhiteSpace(offerId)) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Offer is required for this action.");
            var offer = offers is null ? null : await offers.FirstOrDefault(x => x.CompanyId == companyId && x.OfferId == offerId && x.ApplicantId == applicantId && !x.IsDeleted);
            if (offers is not null && offer is null) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Offer does not belong to the candidate.", 404);
            if (offer is not null && offer.Status is not (RecruitmentOfferStatus.Sent or RecruitmentOfferStatus.Approved)) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidTransition, "Only an active offer can receive an external action link.", 409);
            if (offer is not null && command.ResourceVersion.HasValue && command.ResourceVersion.Value != offer.Version) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.ConcurrencyConflict, "Offer has changed. Refresh and issue a new link.", 409);
        }
        var existing = await tokens.FirstOrDefault(x => x.CompanyId == companyId && x.IssuanceKey == issuanceKey && x.ActionType == action && x.ApplicantId == applicantId && !x.IsDeleted);
        if (existing is not null)
        {
            return Ok(ToDto(existing), "Existing action link reused safely.");
        }
        var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var entity = new RecruitmentExternalActionToken
        {
            CompanyId = companyId, ApplicantId = applicantId, VacancyId = vacancyId, InterviewId = interviewId, OfferId = offerId,
            ActionType = action, TokenHash = Hash(raw), RecipientEmailHash = Hash(email), ResourceVersion = command.ResourceVersion, ExpiresAtUtc = DateTime.UtcNow.AddMinutes(minutes), IssuanceKey = issuanceKey,
            CreatedBy = actor, UpdatedBy = actor
        };
        var added = await tokens.AddOne(entity);
        return added.Success ? Ok(new RecruitmentExternalActionTokenDto { ActionTokenId = entity.ActionTokenId, ActionType = entity.ActionType, ApplicantId = entity.ApplicantId, VacancyId = entity.VacancyId, InterviewId = entity.InterviewId, OfferId = entity.OfferId, ResourceVersion = entity.ResourceVersion, ExpiresAtUtc = entity.ExpiresAtUtc, RawToken = raw }, "Action link issued.") : Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.ConcurrencyConflict, "Action link could not be issued.", 409);
    }

    public async Task<Result<RecruitmentExternalActionTokenDto>> Validate(string token, bool consume)
    {
        if (string.IsNullOrWhiteSpace(token)) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "Action token is required.");
        var hash = Hash(token.Trim());
        var item = await tokens.FirstOrDefault(x => x.TokenHash == hash && !x.IsDeleted);
        if (item is null || item.RevokedAtUtc is not null || item.ExpiresAtUtc <= DateTime.UtcNow || item.ConsumedAtUtc is not null)
            return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.InvalidRequest, "This action link is invalid or expired.", 410);
        if (consume)
        {
            var consumed = await tokens.UpdateMany(Builders<RecruitmentExternalActionToken>.Filter.Where(x => x.ActionTokenId == item.ActionTokenId && x.TokenHash == hash && x.ConsumedAtUtc == null && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow), Builders<RecruitmentExternalActionToken>.Update.Set(x => x.ConsumedAtUtc, DateTime.UtcNow).Set(x => x.LastAttemptAtUtc, DateTime.UtcNow).Inc(x => x.AttemptCount, 1));
            if (!consumed.Success) return Fail<RecruitmentExternalActionTokenDto>(RecruitmentErrorCodes.ConcurrencyConflict, "This action link was already used.", 409);
        }
        return Ok(ToDto(item), "Action link validated.");
    }

    public async Task<Result<string>> Revoke(string actionTokenId, string actor)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId)) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Company context is required.");
        var updated = await tokens.UpdateMany(Builders<RecruitmentExternalActionToken>.Filter.Where(x => x.CompanyId == companyId && x.ActionTokenId == actionTokenId && !x.IsDeleted), Builders<RecruitmentExternalActionToken>.Update.Set(x => x.RevokedAtUtc, DateTime.UtcNow).Set(x => x.UpdatedBy, actor).Set(x => x.UpdatedDate, DateTime.UtcNow));
        return updated.Success ? Ok(actionTokenId, "Action link revoked.") : Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Action link was not found.", 404);
    }

    public async Task<Result<string>> QueueEmail(RecruitmentEmailQueueCommand command, string actor)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId)) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Company context is required.");
        var key = command.DedupeKey.Trim();
        if (key.Length < 8) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Dedupe key is required.");
        var existing = await outbox.FirstOrDefault(x => x.CompanyId == companyId && x.DedupeKey == key && !x.IsDeleted);
        if (existing is not null) return Ok(existing.OutboxId, "Email already queued.");
        var entity = new RecruitmentEmailOutbox { CompanyId = companyId, DedupeKey = key, EventType = command.EventType.Trim(), RecipientEmail = command.RecipientEmail.Trim(), RecipientUserId = command.RecipientUserId?.Trim(), Subject = command.Subject.Trim(), HtmlBody = command.HtmlBody, CreatedBy = actor, UpdatedBy = actor, AvailableAtUtc = DateTime.UtcNow };
        var added = await outbox.AddOne(entity);
        return added.Success ? Ok(entity.OutboxId, "Recruitment email queued.") : Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict, "Recruitment email could not be queued.", 409);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static RecruitmentExternalActionTokenDto ToDto(RecruitmentExternalActionToken x) => new() { CompanyId = x.CompanyId, ActionTokenId = x.ActionTokenId, ActionType = x.ActionType, ApplicantId = x.ApplicantId, VacancyId = x.VacancyId, InterviewId = x.InterviewId, OfferId = x.OfferId, ResourceVersion = x.ResourceVersion, ExpiresAtUtc = x.ExpiresAtUtc };
    private static Result<T> Fail<T>(string code, string message, int status = 400) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
    private static Result<T> Ok<T>(T value, string message) => new() { Success = true, StatusCode = 200, MethodResult = value, Data = value, Message = message };
    private static Result<string> Ok(string value, string message) => Ok<string>(value, message);
}







