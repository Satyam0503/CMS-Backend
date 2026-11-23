using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Enums;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentCandidateActionService(
    IRecruitmentExternalActionService external,
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentOffer> offers,
    IMongoDbRepository<RecruitmentInterview> interviews,
    IMongoDbRepository<RecruitmentEmailOutbox> outbox,
    IMongoDbRepository<RecruitmentAutomationOutboxEvent>? automationOutbox = null,
    IMongoDbRepository<RecruitmentPreboardingCase>? preboardingCases = null,
    IMongoDbRepository<RecruitmentEnterpriseConfig>? configs = null,
    IMongoDbRepository<JobVacancy>? vacancies = null,
    IMongoDbRepository<RecruitmentJobApplication>? applications = null,
    IMongoDbRepository<ApplicantLogs>? applicantLogs = null) : IRecruitmentCandidateActionService
{
    public async Task<Result<RecruitmentCandidateOfferDetailsDto>> RecordOfferView(RecruitmentCandidateOfferViewCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Token)) return Fail<RecruitmentCandidateOfferDetailsDto>(RecruitmentErrorCodes.InvalidRequest, "A valid action link is required.");
        var token = await external.Validate(command.Token.Trim(), false);
        var item = token.MethodResult;
        var consumedReplay = false;
        if (!token.Success || item is null)
        {
            token = await external.ValidateForIdempotentReplay(command.Token.Trim());
            item = token.MethodResult;
            consumedReplay = token.Success && item is not null;
        }
        if (!token.Success || item is null || item.ActionType is not ("OfferResponse" or "AcceptOffer" or "DeclineOffer" or "RequestOfferChange") || string.IsNullOrWhiteSpace(item.OfferId)) return Fail<RecruitmentCandidateOfferDetailsDto>(RecruitmentErrorCodes.InvalidRequest, "This offer link is invalid or expired.", StatusCodes.Status410Gone);
        var offer = await offers.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.OfferId == item.OfferId && !x.IsDeleted);
        var isCommittedResponse = offer is not null &&
            offer.Status is RecruitmentOfferStatus.Accepted or RecruitmentOfferStatus.Declined or RecruitmentOfferStatus.ChangeRequested &&
            offer.RespondedAtUtc.HasValue && !string.IsNullOrWhiteSpace(offer.LastCommandIdempotencyKey) &&
            !string.IsNullOrWhiteSpace(offer.LastCandidateResponseFingerprint);
        var tokenMatchesOffer = item.ResourceVersion.HasValue &&
            (consumedReplay
                ? item.ResourceVersion.Value + 1 == offer?.Version && isCommittedResponse
                : item.ResourceVersion.Value == offer?.Version);
        if (offer is null || !tokenMatchesOffer)
            return Fail<RecruitmentCandidateOfferDetailsDto>(RecruitmentErrorCodes.ConcurrencyConflict, "This offer has changed. Ask the hiring team to send you an updated offer link.", StatusCodes.Status409Conflict);
        if (!consumedReplay && offer.Status == RecruitmentOfferStatus.Sent && offer.ViewedAtUtc is null)
            await offers.UpdateMany(Builders<RecruitmentOffer>.Filter.Where(x => x.CompanyId == item.CompanyId && x.OfferId == item.OfferId && x.ApplicantId == item.ApplicantId && x.Version == offer.Version && x.Status == RecruitmentOfferStatus.Sent && x.ViewedAtUtc == null && !x.IsDeleted), Builders<RecruitmentOffer>.Update.Set(x => x.ViewedAtUtc, DateTime.UtcNow));
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.ApplicantId == item.ApplicantId && !x.IsDeleted);
        if (consumedReplay && applicant is not null && applicant.ConsentStatus != "Anonymized" && offer.LastCandidateResponseMessage is not null)
        {
            var responseCommand = new RecruitmentCandidateOfferActionCommand
            {
                Token = command.Token.Trim(),
                Decision = offer.Status switch
                {
                    RecruitmentOfferStatus.Accepted => "Accepted",
                    RecruitmentOfferStatus.Declined => "Declined",
                    RecruitmentOfferStatus.ChangeRequested => "RequestOfferChange",
                    _ => string.Empty
                },
                Message = offer.LastCandidateResponseMessage,
                IdempotencyKey = offer.LastCommandIdempotencyKey ?? string.Empty,
                LegalConsent = offer.Status == RecruitmentOfferStatus.Accepted,
                SignerLegalName = offer.SignerLegalName,
                SignatureDataUrl = offer.SignatureDataUrl
            };
            var recovered = await ReplayOfferResponse(responseCommand, CandidateOfferResponseFingerprint(responseCommand), item);
            if (!recovered.Success)
                return Fail<RecruitmentCandidateOfferDetailsDto>(recovered.ErrorCode ?? RecruitmentErrorCodes.ConcurrencyConflict,
                    recovered.Message ?? "Your response is saved, but follow-up processing still needs to complete. Reload this page to retry safely.",
                    recovered.StatusCode is >= 400 and <= 599 ? recovered.StatusCode : StatusCodes.Status503ServiceUnavailable);
        }
        var vacancyTitle = string.Empty;
        if (vacancies != null && applicant != null && !string.IsNullOrWhiteSpace(applicant.VacancyId))
        {
            var vac = await vacancies.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.JobId == applicant.VacancyId && !x.IsDeleted);
            if (vac != null) vacancyTitle = vac.Title;
        }
        var dto = new RecruitmentCandidateOfferDetailsDto
        {
            OfferId = item.OfferId,
            Version = offer.Version,
            CandidateName = applicant != null ? $"{applicant.FirstName} {applicant.LastName}".Trim() : string.Empty,
            JobTitle = !string.IsNullOrWhiteSpace(vacancyTitle) ? vacancyTitle : (applicant?.CurrentJobTitle ?? string.Empty),
            Currency = offer?.Currency ?? "INR",
            Salary = offer?.Salary ?? 0,
            TotalCtc = offer?.TotalCtc ?? (offer?.Salary ?? 0),
            StartDateUtc = offer?.StartDateUtc,
            ExpiresAtUtc = offer?.ExpiresAtUtc,
            Status = offer?.Status ?? string.Empty,
            ActionType = item.ActionType,
            SignatureDataUrl = offer?.SignatureDataUrl,
            SignerLegalName = offer?.SignerLegalName,
            SignedAtUtc = offer?.SignedAtUtc
        };
        return Ok(dto, "Offer view recorded.");
    }
    public async Task<Result<string>> RespondToOffer(RecruitmentCandidateOfferActionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Token) || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length < 12)
            return Fail(RecruitmentErrorCodes.InvalidRequest, "A valid action link and idempotency key are required.");
        var responseFingerprint = CandidateOfferResponseFingerprint(command);
        var token = await external.Validate(command.Token.Trim(), false);
        if (!token.Success || token.MethodResult is null)
        {
            var replayToken = await external.ValidateForIdempotentReplay(command.Token.Trim());
            if (replayToken.Success && replayToken.MethodResult is not null)
                return await ReplayOfferResponse(command, responseFingerprint, replayToken.MethodResult);
            return Fail(RecruitmentErrorCodes.InvalidRequest, "This offer link is invalid or expired.", StatusCodes.Status410Gone);
        }
        if (string.IsNullOrWhiteSpace(token.MethodResult.CompanyId) || token.MethodResult.ActionType is not ("OfferResponse" or "AcceptOffer" or "DeclineOffer" or "RequestOfferChange"))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "This offer link is invalid or expired.", StatusCodes.Status410Gone);
        var decision = command.Decision.Trim();
        var requestedAction = token.MethodResult.ActionType;
        if (command.Message?.Length > 4000)
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Your response message must be 4,000 characters or fewer.");
        if ((requestedAction != "OfferResponse" && requestedAction == "AcceptOffer" && decision != "Accepted") ||
            (requestedAction == "DeclineOffer" && decision != "Declined") ||
            (requestedAction == "RequestOfferChange" && decision != "RequestOfferChange"))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "The decision does not match this action link.");
        if (decision == "RequestOfferChange" && string.IsNullOrWhiteSpace(command.Message))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "A message is required when requesting an offer change.");
        if (decision == "Accepted" && (!command.LegalConsent.GetValueOrDefault() ||
            string.IsNullOrWhiteSpace(command.SignerLegalName) || command.SignerLegalName.Trim().Length is < 2 or > 160 ||
            !RecruitmentOfferSignatureRules.IsValidPngDataUrl(command.SignatureDataUrl)))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Electronic acceptance requires legal consent, your full legal name, and a valid PNG signature.");
        var t = token.MethodResult;
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == t.CompanyId && x.ApplicantId == t.ApplicantId && !x.IsDeleted);
        if (applicant is null || applicant.ConsentStatus == "Anonymized" || string.IsNullOrWhiteSpace(t.OfferId)) return Fail(RecruitmentErrorCodes.InvalidRequest, "This offer link is no longer available.", StatusCodes.Status410Gone);
        var offer = await offers.FirstOrDefault(x => x.CompanyId == applicant.CompanyId && x.OfferId == t.OfferId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
        if (offer is null) return Fail(RecruitmentErrorCodes.InvalidTransition, "This offer is no longer awaiting a response.", StatusCodes.Status409Conflict);
        if (!t.ResourceVersion.HasValue || t.ResourceVersion.Value != offer.Version)
            return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "This offer has changed. Ask the hiring team to send you an updated offer link.", StatusCodes.Status409Conflict);
        if (offer.LastCommandIdempotencyKey == command.IdempotencyKey.Trim() && offer.Status is RecruitmentOfferStatus.Accepted or RecruitmentOfferStatus.Declined or RecruitmentOfferStatus.ChangeRequested)
            return Ok(offer.OfferId, $"Offer {offer.Status.ToLowerInvariant()} successfully.");
        if (offer.Status is not (RecruitmentOfferStatus.Sent or RecruitmentOfferStatus.Approved)) return Fail(RecruitmentErrorCodes.InvalidTransition, "This offer is no longer awaiting a response.", StatusCodes.Status409Conflict);
        if (offer.ExpiresAtUtc.HasValue && offer.ExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            await offers.UpdateMany(Builders<RecruitmentOffer>.Filter.Where(x => x.CompanyId == offer.CompanyId && x.OfferId == offer.OfferId && x.Version == offer.Version && x.Status == offer.Status && !x.IsDeleted), Builders<RecruitmentOffer>.Update.Set(x => x.Status, RecruitmentOfferStatus.Expired).Set(x => x.RespondedAtUtc, DateTime.UtcNow).Inc(x => x.Version, 1));
            return Fail(RecruitmentErrorCodes.InvalidTransition, "This offer has expired.", StatusCodes.Status409Conflict);
        }
        var next = decision switch
        {
            "Accepted" => RecruitmentOfferStatus.Accepted,
            "Declined" => RecruitmentOfferStatus.Declined,
            _ => RecruitmentOfferStatus.ChangeRequested
        };
        var updateBuilder = Builders<RecruitmentOffer>.Update
            .Set(x => x.Status, next)
            .Set(x => x.RespondedAtUtc, DateTime.UtcNow)
            .Set(x => x.LastCommandIdempotencyKey, command.IdempotencyKey.Trim())
            .Set(x => x.LastCandidateResponseFingerprint, responseFingerprint)
            .Set(x => x.LastCandidateResponseMessage, command.Message?.Trim() ?? string.Empty)
            .Inc(x => x.Version, 1);

        if (next == RecruitmentOfferStatus.Accepted)
        {
            updateBuilder = updateBuilder.Set(x => x.LastCandidateResponsePreviousStage, applicant.ActivityType.ToString());
            if (!string.IsNullOrWhiteSpace(command.SignatureDataUrl))
                updateBuilder = updateBuilder.Set(x => x.SignatureDataUrl, command.SignatureDataUrl.Trim());
            if (!string.IsNullOrWhiteSpace(command.SignerLegalName))
                updateBuilder = updateBuilder.Set(x => x.SignerLegalName, command.SignerLegalName.Trim());
            updateBuilder = updateBuilder.Set(x => x.SignedAtUtc, DateTime.UtcNow);
            if (!string.IsNullOrWhiteSpace(command.IpAddress))
                updateBuilder = updateBuilder.Set(x => x.SignerIpAddress, command.IpAddress.Trim());
            if (!string.IsNullOrWhiteSpace(command.UserAgent))
                updateBuilder = updateBuilder.Set(x => x.SignerUserAgent, command.UserAgent.Trim());
        }

        var updated = await offers.UpdateMany(
            Builders<RecruitmentOffer>.Filter.Where(x => x.CompanyId == offer.CompanyId && x.OfferId == offer.OfferId && x.Version == offer.Version && x.Status == offer.Status && !x.IsDeleted),
            updateBuilder);
        if (!updated.Success) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "The offer changed before your response was saved.", StatusCodes.Status409Conflict);
        var consumed = await external.Validate(command.Token.Trim(), true);
        if (!consumed.Success)
        {
            await offers.UpdateMany(Builders<RecruitmentOffer>.Filter.Where(x => x.CompanyId == offer.CompanyId && x.OfferId == offer.OfferId && x.Version == offer.Version + 1 && x.Status == next && x.LastCommandIdempotencyKey == command.IdempotencyKey.Trim()), Builders<RecruitmentOffer>.Update.Set(x => x.Status, offer.Status).Set(x => x.RespondedAtUtc, offer.RespondedAtUtc).Set(x => x.LastCommandIdempotencyKey, offer.LastCommandIdempotencyKey).Set(x => x.LastCandidateResponseFingerprint, offer.LastCandidateResponseFingerprint).Set(x => x.LastCandidateResponseMessage, offer.LastCandidateResponseMessage).Set(x => x.LastCandidateResponsePreviousStage, offer.LastCandidateResponsePreviousStage).Inc(x => x.Version, -1));
            return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "This action link was already used.", StatusCodes.Status409Conflict);
        }
        offer.Status = next;
        offer.Version += 1;
        offer.LastCommandIdempotencyKey = command.IdempotencyKey.Trim();
        offer.LastCandidateResponseFingerprint = responseFingerprint;
        offer.LastCandidateResponseMessage = command.Message?.Trim() ?? string.Empty;
        offer.RespondedAtUtc = DateTime.UtcNow;
        if (next == RecruitmentOfferStatus.Accepted) offer.LastCandidateResponsePreviousStage = applicant.ActivityType.ToString();
        if (!await EnsureOfferResponseEffects(offer, applicant, command))
            return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Your response was saved, but one or more follow-up records are still being prepared. Retry this same response link to finish safely.", StatusCodes.Status503ServiceUnavailable);
        return Ok(offer.OfferId, next == RecruitmentOfferStatus.ChangeRequested ? "Offer change request submitted." : $"Offer {next.ToLowerInvariant()} successfully.");
    }

    private async Task<Result<string>> ReplayOfferResponse(
        RecruitmentCandidateOfferActionCommand command,
        string responseFingerprint,
        RecruitmentExternalActionTokenDto token)
    {
        if (string.IsNullOrWhiteSpace(token.CompanyId) || string.IsNullOrWhiteSpace(token.ApplicantId) || string.IsNullOrWhiteSpace(token.OfferId) || !token.ResourceVersion.HasValue)
            return Fail(RecruitmentErrorCodes.InvalidRequest, "This offer link is invalid or expired.", StatusCodes.Status410Gone);
        if (!IsDecisionAllowed(token.ActionType, command.Decision.Trim()))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "The decision does not match this action link.");

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == token.CompanyId && x.ApplicantId == token.ApplicantId && !x.IsDeleted);
        if (applicant is null || applicant.ConsentStatus == "Anonymized")
            return Fail(RecruitmentErrorCodes.InvalidRequest, "This offer link is no longer available.", StatusCodes.Status410Gone);
        var offer = await offers.FirstOrDefault(x => x.CompanyId == token.CompanyId && x.OfferId == token.OfferId && x.ApplicantId == token.ApplicantId && !x.IsDeleted);
        var expectedStatus = command.Decision.Trim() switch
        {
            "Accepted" => RecruitmentOfferStatus.Accepted,
            "Declined" => RecruitmentOfferStatus.Declined,
            "RequestOfferChange" => RecruitmentOfferStatus.ChangeRequested,
            _ => string.Empty
        };
        var sameCommittedResponse = offer is not null &&
            offer.Version == token.ResourceVersion.Value + 1 &&
            offer.Status == expectedStatus &&
            offer.LastCommandIdempotencyKey == command.IdempotencyKey.Trim() &&
            offer.LastCandidateResponseFingerprint == responseFingerprint &&
            offer.RespondedAtUtc.HasValue;
        if (!sameCommittedResponse)
            return Fail(RecruitmentErrorCodes.IdempotencyConflict, "This offer link was already used for a different response.", StatusCodes.Status409Conflict);
        if (!await EnsureOfferResponseEffects(offer!, applicant, command))
            return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Your response is saved, but follow-up records are still being prepared. Retry this same response link shortly.", StatusCodes.Status503ServiceUnavailable);
        return Ok(offer!.OfferId, $"Offer {offer.Status.ToLowerInvariant()} successfully.");
    }

    private async Task<bool> EnsureOfferResponseEffects(RecruitmentOffer offer, Applicant applicant, RecruitmentCandidateOfferActionCommand command)
    {
        var key = command.IdempotencyKey.Trim();
        var next = offer.Status;
        var eventType = next == RecruitmentOfferStatus.ChangeRequested ? "OfferChangeRequested" : "OfferResponse";
        var bodyText = next == RecruitmentOfferStatus.ChangeRequested ? command.Message!.Trim() : $"Offer {next}";
        if (!await Queue(offer.CompanyId, $"offer-response:{offer.OfferId}:{key}", eventType, applicant.ApplicantId, applicant.Email, $"Offer {next}", $"<p>{System.Net.WebUtility.HtmlEncode(bodyText)}</p>"))
            return false;
        if (next != RecruitmentOfferStatus.Accepted) return true;
        if (applications is null || applicantLogs is null || automationOutbox is null) return false;

        var previousStage = offer.LastCandidateResponsePreviousStage ?? applicant.ActivityType.ToString();
        if (applicant.ActivityType != ActivityType.Selected &&
            string.Equals(applicant.ActivityType.ToString(), previousStage, StringComparison.OrdinalIgnoreCase))
        {
            applicant.ActivityType = ActivityType.Selected;
            applicant.UpdatedBy = "candidate-action";
            applicant.UpdatedDate = DateTime.UtcNow;
            if (!(await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == applicant.CompanyId && x.ApplicantId == applicant.ApplicantId), applicant)).Success)
                return false;
        }

        if (applications is not null &&
            (applicant.ActivityType == ActivityType.Selected ||
             string.Equals(applicant.ActivityType.ToString(), previousStage, StringComparison.OrdinalIgnoreCase)))
        {
            var canonical = await applications.FirstOrDefault(x => x.CompanyId == applicant.CompanyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
            if (canonical is null)
            {
                var newCanonical = new RecruitmentJobApplication
                {
                    CompanyId = applicant.CompanyId,
                    ApplicantId = applicant.ApplicantId,
                    VacancyId = applicant.VacancyId ?? string.Empty,
                    CurrentStage = nameof(ActivityType.Selected),
                    CreatedBy = "candidate-action",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = "candidate-action",
                    UpdatedDate = DateTime.UtcNow,
                    StageHistory =
                    [
                        new RecruitmentApplicationStageEvent
                        {
                            FromStage = previousStage,
                            ToStage = nameof(ActivityType.Selected),
                            ActorUserId = "candidate-action",
                            Reason = "Candidate accepted offer"
                        }
                    ]
                };
                if (!(await applications.AddOne(newCanonical)).Success &&
                    await applications.FirstOrDefault(x => x.CompanyId == applicant.CompanyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted) is null)
                    return false;
            }
            else if (!string.Equals(canonical.CurrentStage, nameof(ActivityType.Selected), StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(canonical.CurrentStage, previousStage, StringComparison.OrdinalIgnoreCase))
            {
                canonical.StageHistory.Add(new RecruitmentApplicationStageEvent
                {
                    FromStage = canonical.CurrentStage,
                    ToStage = nameof(ActivityType.Selected),
                    ActorUserId = "candidate-action",
                    Reason = "Candidate accepted offer"
                });
                canonical.CurrentStage = nameof(ActivityType.Selected);
                canonical.Version = (canonical.Version <= 0 ? 1 : canonical.Version) + 1;
                canonical.UpdatedBy = "candidate-action";
                canonical.UpdatedDate = DateTime.UtcNow;
                if (!(await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == applicant.CompanyId && x.ApplicationId == canonical.ApplicationId), canonical)).Success)
                    return false;
            }
        }

        if (await applicantLogs.FirstOrDefault(x => x.CompanyId == offer.CompanyId && x.ApplicantId == applicant.ApplicantId && x.EventType == "OfferAcceptedByCandidate" && x.UserId == "candidate-action" && !x.IsDeleted) is null)
        {
            var log = new ApplicantLogs
            {
                CompanyId = applicant.CompanyId,
                ApplicantId = applicant.ApplicantId,
                ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(),
                JobRole = applicant.VacancyId,
                UserId = "candidate-action",
                ActivityCategory = 0,
                Description = $"Candidate accepted offer ({offer.Currency} {offer.Salary:N2}). Status advanced to Selected.",
                EventType = "OfferAcceptedByCandidate",
                PreviousStage = previousStage,
                CurrentStage = nameof(ActivityType.Selected),
                CreatedBy = "candidate-action",
                UpdatedBy = "candidate-action"
            };
            if (!(await applicantLogs.AddOne(log)).Success &&
                await applicantLogs.FirstOrDefault(x => x.CompanyId == offer.CompanyId && x.ApplicantId == applicant.ApplicantId && x.EventType == "OfferAcceptedByCandidate" && x.UserId == "candidate-action" && !x.IsDeleted) is null)
                return false;
        }

        var config = configs is null ? null : await configs.FirstOrDefault(x => x.CompanyId == offer.CompanyId && !x.IsDeleted);
        var vacancy = vacancies is null ? null : await vacancies.FirstOrDefault(x => x.CompanyId == offer.CompanyId && x.JobId == (offer.VacancyId ?? applicant.VacancyId) && !x.IsDeleted);
        var requiresPreboarding = RecruitmentPreboardingPolicy.Requires(config, vacancy);
        if (requiresPreboarding)
        {
            if (preboardingCases is null) return false;
            var existingCase = await preboardingCases.FirstOrDefault(x => x.CompanyId == offer.CompanyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
            if (existingCase is null)
            {
                var codes = config?.PreboardingChecklist is { Count: > 0 } ? config.PreboardingChecklist : ["candidate-details", "documents", "verification"];
                var tasks = codes.Select(code => new RecruitmentPreboardingTask { Code = code, Title = code.Replace("-", " ") }).ToList();
                var preboarding = new RecruitmentPreboardingCase { CompanyId = offer.CompanyId, ApplicantId = applicant.ApplicantId, OfferId = offer.OfferId, Status = RecruitmentPreboardingStatus.PendingCandidateDetails, OwnerUserId = string.Empty, LastCommandIdempotencyKey = key, Tasks = tasks, CreatedBy = "candidate-action", UpdatedBy = "candidate-action" };
                if (!(await preboardingCases.AddOne(preboarding)).Success &&
                    await preboardingCases.FirstOrDefault(x => x.CompanyId == offer.CompanyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted) is null)
                    return false;
            }
        }

        if (automationOutbox is not null)
        {
            var handoffKey = $"offer-accepted:{offer.OfferId}:{key}";
            var existingHandoff = await automationOutbox.FirstOrDefault(x => x.CompanyId == offer.CompanyId && x.IdempotencyKey == handoffKey && !x.IsDeleted);
            if (existingHandoff is null)
            {
                var handoff = new RecruitmentAutomationOutboxEvent { CompanyId = offer.CompanyId, EventType = "OfferAccepted", ApplicantId = applicant.ApplicantId, IdempotencyKey = handoffKey, Payload = new Dictionary<string, string> { ["offerId"] = offer.OfferId, ["nextStep"] = requiresPreboarding ? "Preboarding" : "EmployeeHandoff" }, CreatedBy = "candidate-action", UpdatedBy = "candidate-action" };
                if (!(await automationOutbox.AddOne(handoff)).Success &&
                    await automationOutbox.FirstOrDefault(x => x.CompanyId == offer.CompanyId && x.IdempotencyKey == handoffKey && !x.IsDeleted) is null)
                    return false;
            }
        }
        return true;
    }

    private static bool IsDecisionAllowed(string actionType, string decision) =>
        actionType switch
        {
            "OfferResponse" => decision is "Accepted" or "Declined" or "RequestOfferChange",
            "AcceptOffer" => decision == "Accepted",
            "DeclineOffer" => decision == "Declined",
            "RequestOfferChange" => decision == "RequestOfferChange",
            _ => false
        };

    private static string CandidateOfferResponseFingerprint(RecruitmentCandidateOfferActionCommand command)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            Decision = command.Decision.Trim(),
            Message = command.Message?.Trim() ?? string.Empty,
            LegalConsent = command.LegalConsent == true,
            SignerLegalName = command.SignerLegalName?.Trim() ?? string.Empty
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public async Task<Result<string>> ConfirmInterview(RecruitmentCandidateInterviewActionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Token) || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length < 12)
            return Fail(RecruitmentErrorCodes.InvalidRequest, "A valid action link and idempotency key are required.");
        var token = await external.Validate(command.Token.Trim(), false);
        if (!token.Success || token.MethodResult is null || string.IsNullOrWhiteSpace(token.MethodResult.CompanyId) || token.MethodResult.ActionType is not ("ScheduleInterview" or "RescheduleInterview"))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "This interview link is invalid or expired.", StatusCodes.Status410Gone);
        var t = token.MethodResult;
        if (string.IsNullOrWhiteSpace(t.InterviewId)) return Fail(RecruitmentErrorCodes.InvalidRequest, "This interview link is incomplete.", StatusCodes.Status410Gone);
        var interview = await interviews.FirstOrDefault(x => x.InterviewId == t.InterviewId && x.CompanyId == t.CompanyId && x.ApplicantId == t.ApplicantId && !x.IsDeleted);
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == t.CompanyId && x.ApplicantId == t.ApplicantId && !x.IsDeleted);
        if (interview is null || applicant is null || applicant.ConsentStatus == "Anonymized") return Fail(RecruitmentErrorCodes.InvalidRequest, "This interview is no longer available.", StatusCodes.Status410Gone);
        var starts = command.StartsAtUtc.HasValue ? DateTime.SpecifyKind(command.StartsAtUtc.Value, DateTimeKind.Utc) : interview.StartsAtUtc;
        var ends = command.EndsAtUtc.HasValue ? DateTime.SpecifyKind(command.EndsAtUtc.Value, DateTimeKind.Utc) : interview.EndsAtUtc;
        if (ends <= starts) return Fail(RecruitmentErrorCodes.InvalidRequest, "Interview end must be after start.");
        var oldTimeZone = interview.TimeZoneId;
        var oldLocation = interview.Location;
        var oldMeetingUrl = interview.MeetingUrl;
        var oldStatus = interview.Status;
        var oldVersion = interview.Version;
        var key = command.IdempotencyKey.Trim();
        var updated = await interviews.UpdateMany(Builders<RecruitmentInterview>.Filter.Where(x => x.CompanyId == interview.CompanyId && x.InterviewId == interview.InterviewId && x.Version == oldVersion && !x.IsDeleted), Builders<RecruitmentInterview>.Update.Set(x => x.StartsAtUtc, starts).Set(x => x.EndsAtUtc, ends).Set(x => x.TimeZoneId, command.TimeZoneId?.Trim() ?? interview.TimeZoneId).Set(x => x.Location, command.Location?.Trim() ?? interview.Location).Set(x => x.MeetingUrl, command.MeetingUrl?.Trim() ?? interview.MeetingUrl).Set(x => x.Status, RecruitmentInterviewStatus.Confirmed).Set(x => x.LastCommandIdempotencyKey, key).Inc(x => x.Version, 1));
        if (!updated.Success) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "The interview changed before confirmation was saved.", StatusCodes.Status409Conflict);
        var consumed = await external.Validate(command.Token.Trim(), true);
        if (!consumed.Success)
        {
            await interviews.UpdateMany(Builders<RecruitmentInterview>.Filter.Where(x => x.CompanyId == interview.CompanyId && x.InterviewId == interview.InterviewId && x.Version == oldVersion + 1 && x.LastCommandIdempotencyKey == key && !x.IsDeleted), Builders<RecruitmentInterview>.Update.Set(x => x.StartsAtUtc, interview.StartsAtUtc).Set(x => x.EndsAtUtc, interview.EndsAtUtc).Set(x => x.TimeZoneId, oldTimeZone).Set(x => x.Location, oldLocation).Set(x => x.MeetingUrl, oldMeetingUrl).Set(x => x.Status, oldStatus).Set(x => x.LastCommandIdempotencyKey, interview.LastCommandIdempotencyKey).Set(x => x.Version, oldVersion));
            return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "This action link was already used.", StatusCodes.Status409Conflict);
        }
        await Queue(interview.CompanyId, $"interview-confirmed:{interview.InterviewId}:{key}", "InterviewConfirmed", applicant.ApplicantId, applicant.Email, "Interview confirmed", $"<p>Your interview has been confirmed for {starts:O}.</p>");
        return Ok(interview.InterviewId, "Interview confirmed successfully.");
    }
    private async Task<bool> Queue(string companyId, string dedupe, string type, string applicantId, string email, string subject, string html)
    {
        var existing = await outbox.FirstOrDefault(x => x.CompanyId == companyId && x.DedupeKey == dedupe && !x.IsDeleted);
        if (existing is not null) return existing.Status is not ("DeadLetter" or "Redacted" or "Cancelled");
        var added = await outbox.AddOne(new RecruitmentEmailOutbox { CompanyId = companyId, RelatedApplicantId = applicantId, DedupeKey = dedupe, EventType = type, RecipientEmail = email, Subject = subject, HtmlBody = html, AvailableAtUtc = DateTime.UtcNow, CreatedBy = "candidate-action", UpdatedBy = "candidate-action" });
        if (added.Success) return true;
        return await outbox.FirstOrDefault(x => x.CompanyId == companyId && x.DedupeKey == dedupe && !x.IsDeleted) is not null;
    }
    private static Result<string> Fail(string code, string message, int status = 400) => Fail<string>(code, message, status);
    private static Result<T> Fail<T>(string code, string message, int status = 400) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
    private static Result<string> Ok(string value, string message) => Ok<string>(value, message);
    private static Result<T> Ok<T>(T value, string message) => new() { Success = true, StatusCode = 200, Data = value, MethodResult = value, Message = message };
}





