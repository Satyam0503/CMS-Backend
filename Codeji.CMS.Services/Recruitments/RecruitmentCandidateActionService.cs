using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Enums;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

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
        if (!token.Success || item is null || item.ActionType is not ("OfferResponse" or "AcceptOffer" or "DeclineOffer" or "RequestOfferChange") || string.IsNullOrWhiteSpace(item.OfferId)) return Fail<RecruitmentCandidateOfferDetailsDto>(RecruitmentErrorCodes.InvalidRequest, "This offer link is invalid or expired.", StatusCodes.Status410Gone);
        var updated = await offers.UpdateMany(Builders<RecruitmentOffer>.Filter.Where(x => x.CompanyId == item.CompanyId && x.OfferId == item.OfferId && x.ApplicantId == item.ApplicantId && x.Status == RecruitmentOfferStatus.Sent && x.ViewedAtUtc == null && !x.IsDeleted), Builders<RecruitmentOffer>.Update.Set(x => x.ViewedAtUtc, DateTime.UtcNow));
        var offer = await offers.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.OfferId == item.OfferId && !x.IsDeleted);
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.ApplicantId == item.ApplicantId && !x.IsDeleted);
        var vacancyTitle = string.Empty;
        if (vacancies != null && applicant != null && !string.IsNullOrWhiteSpace(applicant.VacancyId))
        {
            var vac = await vacancies.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.JobId == applicant.VacancyId && !x.IsDeleted);
            if (vac != null) vacancyTitle = vac.Title;
        }
        var dto = new RecruitmentCandidateOfferDetailsDto
        {
            OfferId = item.OfferId,
            CandidateName = applicant != null ? $"{applicant.FirstName} {applicant.LastName}".Trim() : string.Empty,
            JobTitle = !string.IsNullOrWhiteSpace(vacancyTitle) ? vacancyTitle : (applicant?.CurrentJobTitle ?? string.Empty),
            Currency = offer?.Currency ?? "INR",
            Salary = offer?.Salary ?? 0,
            TotalCtc = offer?.TotalCtc ?? (offer?.Salary ?? 0),
            StartDateUtc = offer?.StartDateUtc,
            ExpiresAtUtc = offer?.ExpiresAtUtc,
            Status = offer?.Status ?? string.Empty,
            ActionType = item.ActionType
        };
        return Ok(dto, "Offer view recorded.");
    }
    public async Task<Result<string>> RespondToOffer(RecruitmentCandidateOfferActionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Token) || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length < 12)
            return Fail(RecruitmentErrorCodes.InvalidRequest, "A valid action link and idempotency key are required.");
        var token = await external.Validate(command.Token.Trim(), false);
        if (!token.Success || token.MethodResult is null || string.IsNullOrWhiteSpace(token.MethodResult.CompanyId) || token.MethodResult.ActionType is not ("OfferResponse" or "AcceptOffer" or "DeclineOffer" or "RequestOfferChange"))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "This offer link is invalid or expired.", StatusCodes.Status410Gone);
        var decision = command.Decision.Trim();
        var requestedAction = token.MethodResult.ActionType;
        if ((requestedAction != "OfferResponse" && requestedAction == "AcceptOffer" && decision != "Accepted") ||
            (requestedAction == "DeclineOffer" && decision != "Declined") ||
            (requestedAction == "RequestOfferChange" && decision != "RequestOfferChange"))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "The decision does not match this action link.");
        if (decision == "RequestOfferChange" && string.IsNullOrWhiteSpace(command.Message))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "A message is required when requesting an offer change.");
        var t = token.MethodResult;
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == t.CompanyId && x.ApplicantId == t.ApplicantId && !x.IsDeleted);
        if (applicant is null || string.IsNullOrWhiteSpace(t.OfferId)) return Fail(RecruitmentErrorCodes.InvalidRequest, "This offer link is no longer available.", StatusCodes.Status410Gone);
        var offer = await offers.FirstOrDefault(x => x.CompanyId == applicant.CompanyId && x.OfferId == t.OfferId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
        if (offer is null) return Fail(RecruitmentErrorCodes.InvalidTransition, "This offer is no longer awaiting a response.", StatusCodes.Status409Conflict);
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
        var updated = await offers.UpdateMany(Builders<RecruitmentOffer>.Filter.Where(x => x.CompanyId == offer.CompanyId && x.OfferId == offer.OfferId && x.Version == offer.Version && x.Status == offer.Status && !x.IsDeleted), Builders<RecruitmentOffer>.Update.Set(x => x.Status, next).Set(x => x.RespondedAtUtc, DateTime.UtcNow).Set(x => x.LastCommandIdempotencyKey, command.IdempotencyKey.Trim()).Inc(x => x.Version, 1));
        if (!updated.Success) return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "The offer changed before your response was saved.", StatusCodes.Status409Conflict);
        var consumed = await external.Validate(command.Token.Trim(), true);
        if (!consumed.Success)
        {
            await offers.UpdateMany(Builders<RecruitmentOffer>.Filter.Where(x => x.CompanyId == offer.CompanyId && x.OfferId == offer.OfferId && x.Version == offer.Version + 1 && x.Status == next && x.LastCommandIdempotencyKey == command.IdempotencyKey.Trim()), Builders<RecruitmentOffer>.Update.Set(x => x.Status, offer.Status).Set(x => x.RespondedAtUtc, offer.RespondedAtUtc).Set(x => x.LastCommandIdempotencyKey, offer.LastCommandIdempotencyKey).Inc(x => x.Version, -1));
            return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "This action link was already used.", StatusCodes.Status409Conflict);
        }
        var eventType = next == RecruitmentOfferStatus.ChangeRequested ? "OfferChangeRequested" : "OfferResponse";
        var message = next == RecruitmentOfferStatus.ChangeRequested ? command.Message!.Trim() : $"Offer {next}";
        await Queue(offer.CompanyId, $"offer-response:{offer.OfferId}:{command.IdempotencyKey.Trim()}", eventType, applicant.Email, $"Offer {next}", $"<p>{System.Net.WebUtility.HtmlEncode(message)}</p>");
        if (next == RecruitmentOfferStatus.Accepted)
        {
            var prevStage = applicant.ActivityType;
            applicant.ActivityType = ActivityType.Selected;
            applicant.UpdatedBy = "candidate-action";
            applicant.UpdatedDate = DateTime.UtcNow;
            await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == applicant.CompanyId && x.ApplicantId == applicant.ApplicantId), applicant);

            if (applications is not null)
            {
                var canonical = await applications.FirstOrDefault(x => x.CompanyId == applicant.CompanyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
                if (canonical is not null)
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
                    await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == applicant.CompanyId && x.ApplicationId == canonical.ApplicationId), canonical);
                }
                else if (canonical is null)
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
                                FromStage = prevStage.ToString(),
                                ToStage = nameof(ActivityType.Selected),
                                ActorUserId = "candidate-action",
                                Reason = "Candidate accepted offer"
                            }
                        ]
                    };
                    await applications.AddOne(newCanonical);
                }
            }

            if (applicantLogs is not null)
            {
                await applicantLogs.AddOne(new ApplicantLogs
                {
                    CompanyId = applicant.CompanyId,
                    ApplicantId = applicant.ApplicantId,
                    ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(),
                    JobRole = applicant.VacancyId,
                    UserId = "candidate-action",
                    ActivityCategory = 0,
                    Description = $"Candidate accepted offer ({offer.Currency} {offer.Salary:N2}). Status advanced to Selected.",
                    EventType = "OfferAcceptedByCandidate",
                    PreviousStage = prevStage.ToString(),
                    CurrentStage = nameof(ActivityType.Selected),
                    CreatedBy = "candidate-action",
                    UpdatedBy = "candidate-action"
                });
            }
        }
        if (next == RecruitmentOfferStatus.Accepted && preboardingCases is not null)
        {
            var existingCase = await preboardingCases.FirstOrDefault(x => x.CompanyId == offer.CompanyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
            if (existingCase is null) { var config = configs is null ? null : await configs.FirstOrDefault(x => x.CompanyId == offer.CompanyId && !x.IsDeleted); var codes = config?.PreboardingChecklist is { Count: > 0 } ? config.PreboardingChecklist : ["candidate-details", "documents", "verification"]; var tasks = codes.Select(code => new RecruitmentPreboardingTask { Code = code, Title = code.Replace("-", " ") }).ToList(); await preboardingCases.AddOne(new RecruitmentPreboardingCase { CompanyId = offer.CompanyId, ApplicantId = applicant.ApplicantId, OfferId = offer.OfferId, Status = RecruitmentPreboardingStatus.PendingCandidateDetails, OwnerUserId = "", LastCommandIdempotencyKey = command.IdempotencyKey.Trim(), Tasks = tasks, CreatedBy = "candidate-action", UpdatedBy = "candidate-action" }); }
        }
        if (next == RecruitmentOfferStatus.Accepted && automationOutbox is not null)
        {
            var handoffKey = $"offer-accepted:{offer.OfferId}:{command.IdempotencyKey.Trim()}";
            var existingHandoff = await automationOutbox.FirstOrDefault(x => x.CompanyId == offer.CompanyId && x.IdempotencyKey == handoffKey && !x.IsDeleted);
            if (existingHandoff is null) await automationOutbox.AddOne(new RecruitmentAutomationOutboxEvent { CompanyId = offer.CompanyId, EventType = "OfferAccepted", ApplicantId = applicant.ApplicantId, IdempotencyKey = handoffKey, Payload = new Dictionary<string, string> { ["offerId"] = offer.OfferId, ["nextStep"] = "Preboarding" }, CreatedBy = "candidate-action", UpdatedBy = "candidate-action" });
        }
        return Ok(offer.OfferId, next == RecruitmentOfferStatus.ChangeRequested ? "Offer change request submitted." : $"Offer {next.ToLowerInvariant()} successfully.");
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
        if (interview is null || applicant is null) return Fail(RecruitmentErrorCodes.InvalidRequest, "This interview is no longer available.", StatusCodes.Status410Gone);
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
        await Queue(interview.CompanyId, $"interview-confirmed:{interview.InterviewId}:{key}", "InterviewConfirmed", applicant.Email, "Interview confirmed", $"<p>Your interview has been confirmed for {starts:O}.</p>");
        return Ok(interview.InterviewId, "Interview confirmed successfully.");
    }
    private async Task Queue(string companyId, string dedupe, string type, string email, string subject, string html)
    {
        var existing = await outbox.FirstOrDefault(x => x.CompanyId == companyId && x.DedupeKey == dedupe && !x.IsDeleted);
        if (existing is not null) return;
        await outbox.AddOne(new RecruitmentEmailOutbox { CompanyId = companyId, DedupeKey = dedupe, EventType = type, RecipientEmail = email, Subject = subject, HtmlBody = html, AvailableAtUtc = DateTime.UtcNow, CreatedBy = "candidate-action", UpdatedBy = "candidate-action" });
    }
    private static Result<string> Fail(string code, string message, int status = 400) => Fail<string>(code, message, status);
    private static Result<T> Fail<T>(string code, string message, int status = 400) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
    private static Result<string> Ok(string value, string message) => Ok<string>(value, message);
    private static Result<T> Ok<T>(T value, string message) => new() { Success = true, StatusCode = 200, MethodResult = value, Message = message };
}





