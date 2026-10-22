using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentOfferService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentOffer> offers,
    IMongoDbRepository<ApplicantLogs> applicantLogs,
    INotificationService notifications,
    IHttpContextAccessor context,
    IMongoDbRepository<RecruitmentOfferApprovalPolicy>? approvalPolicies = null,
    IMongoDbRepository<JobVacancy>? vacancies = null,
    IRecruitmentExternalActionService? externalActions = null,
    IMongoDbRepository<RecruitmentPreboardingCase>? preboardingCases = null,
    IMongoDbRepository<RecruitmentEnterpriseConfig>? configs = null,
    IMongoDbRepository<RecruitmentJobApplication>? applications = null) : IRecruitmentOfferService
{
    public async Task<Result<string>> Upsert(RecruitmentOfferCommand command, string actorUserId)
    {
        var result = new Result<string> { Success = false, StatusCode = StatusCodes.Status400BadRequest };
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Company and idempotency key are required.");
        if (!RecruitmentOfferRules.IsValidCompensation(command.Salary, command.Currency))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Salary and currency are invalid.");
        if (command.CompensationComponents is null || command.CompensationComponents.Count > 30 || command.CompensationComponents.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Value < 0) || command.CompensationComponents.GroupBy(x => x.Key.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Compensation components must have unique names, non-negative values, and no more than 30 entries.");
        if (command.ExpiresAtUtc.HasValue && command.ExpiresAtUtc.Value <= DateTime.UtcNow)
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Offer expiry must be in the future.");
        if (command.StartDateUtc.HasValue && command.StartDateUtc.Value.Date < DateTime.UtcNow.Date)
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Offer start date cannot be in the past.");
        if (command.ExpiresAtUtc.HasValue && command.StartDateUtc.HasValue && command.ExpiresAtUtc.Value > command.StartDateUtc.Value)
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Offer expiry must be on or before the offer start date.");

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && !x.IsDeleted);
        if (applicant is null)
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Applicant was not found in the current company.");
        if (string.Equals(command.Status, RecruitmentOfferStatus.Sent, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(applicant.Email))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "A candidate email address is required before sending an offer.");

        var idempotencyKey = command.IdempotencyKey.Trim();
        var existing = string.IsNullOrWhiteSpace(command.OfferId)
            ? await offers.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && x.LastCommandIdempotencyKey == idempotencyKey && !x.IsDeleted)
            : await offers.FirstOrDefault(x => x.CompanyId == companyId && x.OfferId == command.OfferId && !x.IsDeleted);

        // REC-BUG-02 Fix: If OfferId was not explicitly passed and idempotencyKey is for a new action, resolve the latest active offer for this applicant.
        if (existing is null && string.IsNullOrWhiteSpace(command.OfferId))
        {
            var applicantOffers = await offers.GetAll(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && !x.IsDeleted);
            existing = applicantOffers?.OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate).FirstOrDefault();
        }

        if (existing is not null && string.Equals(existing.LastCommandIdempotencyKey, idempotencyKey, StringComparison.Ordinal))
            return Ok(result, existing.OfferId, "Offer command already applied.");

        var config = configs is null ? null : await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);
        var fastTrackOfferAllowed = config is not null && (!config.OfferApproval || string.Equals(config.RecruitmentMode, "Simple", StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            if (!string.Equals(command.Status, RecruitmentOfferStatus.Draft, StringComparison.OrdinalIgnoreCase) && !fastTrackOfferAllowed)
                return Fail(result, RecruitmentErrorCodes.InvalidTransition, "Create the offer as Draft first, then submit it for approval or send it.", StatusCodes.Status409Conflict);

            var initialStatus = fastTrackOfferAllowed && !string.IsNullOrWhiteSpace(command.Status) ? command.Status.Trim() : RecruitmentOfferStatus.Draft;
            var offer = new RecruitmentOffer
            {
                CompanyId = companyId,
                ApplicantId = command.ApplicantId,
                VacancyId = command.VacancyId,
                Salary = command.Salary,
                CompensationComponents = command.CompensationComponents.ToDictionary(x => x.Key.Trim(), x => x.Value, StringComparer.OrdinalIgnoreCase),
                TotalCtc = command.Salary + command.CompensationComponents.Values.Sum(),
                Currency = command.Currency.Trim().ToUpperInvariant(),
                StartDateUtc = command.StartDateUtc.HasValue ? DateTime.SpecifyKind(command.StartDateUtc.Value, DateTimeKind.Utc) : null,
                ExpiresAtUtc = command.ExpiresAtUtc.HasValue ? DateTime.SpecifyKind(command.ExpiresAtUtc.Value, DateTimeKind.Utc) : null,
                ApprovalReference = command.ApprovalReference?.Trim(),
                Status = initialStatus,
                SentAtUtc = string.Equals(initialStatus, RecruitmentOfferStatus.Sent, StringComparison.OrdinalIgnoreCase) ? DateTime.UtcNow : null,
                LastCommandIdempotencyKey = idempotencyKey,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId
            };
            var saved = await offers.AddOne(offer);
            if (!saved.Success)
                return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Offer could not be created.", StatusCodes.Status409Conflict);

            await HandlePostOfferStatusEffects(companyId, applicant, offer, config, idempotencyKey, actorUserId);
            return Ok(result, offer.OfferId, "Offer created.");
        }

        var isAllowedTransition = RecruitmentOfferRules.IsAllowedTransition(existing.Status, command.Status)
            || (fastTrackOfferAllowed && existing.Status is RecruitmentOfferStatus.Draft or RecruitmentOfferStatus.PendingApproval or RecruitmentOfferStatus.Approved && command.Status is RecruitmentOfferStatus.Approved or RecruitmentOfferStatus.Sent or RecruitmentOfferStatus.Accepted)
            || (existing.Status is RecruitmentOfferStatus.Approved or RecruitmentOfferStatus.Sent && string.Equals(command.Status, RecruitmentOfferStatus.Accepted, StringComparison.OrdinalIgnoreCase));

        if (!isAllowedTransition)
            return Fail(result, RecruitmentErrorCodes.InvalidTransition, "Offer status transition is not allowed.", StatusCodes.Status409Conflict);

        if (string.Equals(command.Status, RecruitmentOfferStatus.Approved, StringComparison.OrdinalIgnoreCase) && (config is null || config.OfferApproval))
        {
            JobVacancy? vacancy = vacancies is null ? null : await vacancies.FirstOrDefault(x => x.CompanyId == companyId && x.JobId == command.VacancyId && !x.IsDeleted);
            var policy = approvalPolicies is null ? null : await approvalPolicies.FirstOrDefault(x => x.CompanyId == companyId && x.VacancyId == command.VacancyId && !x.IsDeleted);
            if (policy is null && approvalPolicies is not null && vacancy is not null && (!string.IsNullOrWhiteSpace(vacancy.LegalEntityId) || !string.IsNullOrWhiteSpace(vacancy.DepartmentId) || !string.IsNullOrWhiteSpace(vacancy.Grade)))
                policy = await approvalPolicies.FirstOrDefault(x => x.CompanyId == companyId && string.IsNullOrEmpty(x.VacancyId) && x.LegalEntityId == vacancy.LegalEntityId && x.DepartmentId == vacancy.DepartmentId && x.Grade == vacancy.Grade && !x.IsDeleted);
            policy ??= approvalPolicies is null ? null : await approvalPolicies.FirstOrDefault(x => x.CompanyId == companyId && string.IsNullOrEmpty(x.VacancyId) && string.IsNullOrEmpty(x.LegalEntityId) && string.IsNullOrEmpty(x.DepartmentId) && string.IsNullOrEmpty(x.Grade) && !x.IsDeleted);

            var roleMatches = string.IsNullOrWhiteSpace(policy?.ApproverRoleId) || string.Equals(CurrentContext.UserRoleId(context), policy.ApproverRoleId, StringComparison.Ordinal);
            var delegateMatches = !string.IsNullOrWhiteSpace(policy?.DelegatedApproverUserId) && string.Equals(CurrentContext.UserId(context), policy.DelegatedApproverUserId, StringComparison.Ordinal) && (!policy!.DelegationExpiresAtUtc.HasValue || policy.DelegationExpiresAtUtc > DateTime.UtcNow);
            if ((!string.IsNullOrWhiteSpace(policy?.ApproverRoleId) || !string.IsNullOrWhiteSpace(policy?.DelegatedApproverUserId)) && !roleMatches && !delegateMatches)
                return Fail(result, RecruitmentErrorCodes.ApprovalRequired, "This offer must be approved by the configured approver role or active delegation.", StatusCodes.Status403Forbidden);

            var requiresReference = policy?.RequireApprovalReference ?? true;
            var thresholdApplies = policy is null || (string.Equals(policy.Currency, command.Currency.Trim(), StringComparison.OrdinalIgnoreCase) && command.Salary >= policy.ApprovalThreshold);
            if (requiresReference && thresholdApplies && string.IsNullOrWhiteSpace(command.ApprovalReference) && string.IsNullOrWhiteSpace(existing.ApprovalReference))
                return Fail(result, RecruitmentErrorCodes.ApprovalRequired, "A shared approval reference is required before an offer can be approved.", StatusCodes.Status409Conflict);
        }

        var currentVersion = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != currentVersion)
            return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Offer was changed by another user.", StatusCodes.Status409Conflict);

        var previousStatus = existing.Status;
        if (!string.IsNullOrWhiteSpace(command.ApprovalReference)) existing.ApprovalReference = command.ApprovalReference.Trim();
        existing.Salary = command.Salary;
        existing.CompensationComponents = command.CompensationComponents.ToDictionary(x => x.Key.Trim(), x => x.Value, StringComparer.OrdinalIgnoreCase);
        existing.TotalCtc = command.Salary + command.CompensationComponents.Values.Sum();
        existing.Currency = command.Currency.Trim().ToUpperInvariant();
        existing.StartDateUtc = command.StartDateUtc.HasValue ? DateTime.SpecifyKind(command.StartDateUtc.Value, DateTimeKind.Utc) : null;
        existing.ExpiresAtUtc = command.ExpiresAtUtc.HasValue ? DateTime.SpecifyKind(command.ExpiresAtUtc.Value, DateTimeKind.Utc) : null;
        existing.Status = command.Status.Trim();
        if (string.Equals(existing.Status, RecruitmentOfferStatus.Sent, StringComparison.OrdinalIgnoreCase) && !string.Equals(previousStatus, RecruitmentOfferStatus.Sent, StringComparison.OrdinalIgnoreCase))
            existing.SentAtUtc = DateTime.UtcNow;
        if (string.Equals(existing.Status, RecruitmentOfferStatus.Accepted, StringComparison.OrdinalIgnoreCase) && !existing.RespondedAtUtc.HasValue)
            existing.RespondedAtUtc = DateTime.UtcNow;
        existing.LastCommandIdempotencyKey = idempotencyKey;
        existing.Version = currentVersion + 1;
        existing.UpdatedBy = actorUserId;
        existing.UpdatedDate = DateTime.UtcNow;

        var updated = await offers.Update(Builders<RecruitmentOffer>.Filter.Where(x => x.CompanyId == companyId && x.OfferId == existing.OfferId && (x.Version == currentVersion || x.Version == 0)), existing);
        if (!updated.Success)
            return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Offer was changed by another user.", StatusCodes.Status409Conflict);

        await applicantLogs.AddOne(new ApplicantLogs { CompanyId = companyId, ApplicantId = applicant.ApplicantId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobRole = applicant.VacancyId, UserId = actorUserId, ActivityCategory = 0, Description = $"Offer status changed from {previousStatus} to {existing.Status}.", EventType = "OfferStatusChanged", PreviousStage = previousStatus, CurrentStage = existing.Status, CreatedBy = actorUserId, UpdatedBy = actorUserId });

        await HandlePostOfferStatusEffects(companyId, applicant, existing, config, idempotencyKey, actorUserId);

        try
        {
            await notifications.SendNotificationToUser(actorUserId, new NotificationViewModel { UserNotificationId = existing.OfferId, Title = "Recruitment offer updated", Body = $"Offer for {applicant.FirstName} {applicant.LastName} is now {existing.Status}.", TargetId = existing.OfferId, SentDateTime = DateTime.UtcNow, SentBy = actorUserId, NotificationTypes = NotificationTypes.Notice });
        }
        catch
        {
            // Notification delivery is non-critical; the durable offer/audit write remains authoritative.
        }
        return Ok(result, existing.OfferId, "Offer updated.");
    }

    private async Task HandlePostOfferStatusEffects(
        string companyId,
        Applicant applicant,
        RecruitmentOffer offer,
        RecruitmentEnterpriseConfig? config,
        string idempotencyKey,
        string actorUserId)
    {
        try
        {
            if (string.Equals(offer.Status, RecruitmentOfferStatus.Sent, StringComparison.OrdinalIgnoreCase)
                && externalActions is not null
                && !string.IsNullOrWhiteSpace(applicant.Email)
                && (config?.CandidateEmailActions ?? true))
            {
                var issuanceKey = $"offer-sent:{offer.OfferId}:{idempotencyKey}";
                if (issuanceKey.Length < 12) issuanceKey = issuanceKey.PadRight(12, '0');
                var tokenResult = await externalActions.Issue(new RecruitmentExternalActionIssueCommand
                {
                    ActionType = "OfferResponse",
                    ApplicantId = applicant.ApplicantId,
                    VacancyId = applicant.VacancyId,
                    OfferId = offer.OfferId,
                    RecipientEmail = applicant.Email.Trim(),
                    ExpiresInMinutes = 24 * 60,
                    IssuanceKey = issuanceKey,
                    ResourceVersion = offer.Version
                }, actorUserId);

                var tokenParam = tokenResult.Success ? tokenResult.MethodResult?.RawToken : null;
                if (string.IsNullOrWhiteSpace(tokenParam) || !TryBuildCandidateOfferUrl(tokenParam, out var offerUrl))
                    return;
                await externalActions.QueueEmail(new RecruitmentEmailQueueCommand
                {
                    DedupeKey = $"offer-email:{offer.OfferId}:{idempotencyKey}",
                    EventType = "OfferSent",
                    RecipientEmail = applicant.Email.Trim(),
                    Subject = HtmlTemplate.Render(config?.OfferLetterSubjectTemplate ?? "Your offer for [JobTitle]", new { CandidateName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobTitle = applicant.VacancyId, Currency = offer.Currency, TotalCtc = offer.TotalCtc.ToString("N2") }),
                    HtmlBody = HtmlTemplate.Render(config?.OfferLetterBodyTemplate ?? "<p>Dear [CandidateName],</p><p>We are pleased to offer you the [JobTitle] position.</p><p>Compensation: [Currency] [TotalCtc]</p>", new { CandidateName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobTitle = applicant.VacancyId, Currency = offer.Currency, TotalCtc = offer.TotalCtc.ToString("N2") }) +
                               $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(offerUrl)}\">Review your offer</a></p><p>This secure link expires in 24 hours.</p>"
                }, actorUserId);
            }

            // REC-BUG-03 & REC-BUG-04 Fix: Ensure RecruitmentPreboardingCase exists when offer becomes Accepted (whether via candidate link or HR action)
            if (string.Equals(offer.Status, RecruitmentOfferStatus.Accepted, StringComparison.OrdinalIgnoreCase))
            {
                if (applicant.ActivityType != ActivityType.Selected)
                {
                    applicant.ActivityType = ActivityType.Selected;
                    applicant.UpdatedBy = actorUserId;
                    applicant.UpdatedDate = DateTime.UtcNow;
                    await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId), applicant);

                    if (applications is not null)
                    {
                        var canonical = await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
                        if (canonical is not null)
                        {
                            canonical.StageHistory.Add(new RecruitmentApplicationStageEvent
                            {
                                FromStage = canonical.CurrentStage,
                                ToStage = nameof(ActivityType.Selected),
                                ActorUserId = actorUserId,
                                Reason = "Offer accepted"
                            });
                            canonical.CurrentStage = nameof(ActivityType.Selected);
                            canonical.Version = (canonical.Version <= 0 ? 1 : canonical.Version) + 1;
                            canonical.UpdatedBy = actorUserId;
                            canonical.UpdatedDate = DateTime.UtcNow;
                            await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == canonical.ApplicationId), canonical);
                        }
                    }
                }
            }

            if (string.Equals(offer.Status, RecruitmentOfferStatus.Accepted, StringComparison.OrdinalIgnoreCase)
                && preboardingCases is not null)
            {
                var existingCase = await preboardingCases.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && !x.IsDeleted);
                if (existingCase is null)
                {
                    var codes = config?.PreboardingChecklist is { Count: > 0 } ? config.PreboardingChecklist : ["candidate-details", "documents", "verification"];
                    var autoComplete = config is not null && (!config.RequirePreboardingBeforeHandoff || string.Equals(config.RecruitmentMode, "Simple", StringComparison.OrdinalIgnoreCase));
                    var tasks = codes.Select(code => new RecruitmentPreboardingTask
                    {
                        Code = code,
                        Title = code.Replace("-", " "),
                        Status = autoComplete ? "Completed" : "Pending",
                        CompletedAtUtc = autoComplete ? DateTime.UtcNow : null
                    }).ToList();

                    await preboardingCases.AddOne(new RecruitmentPreboardingCase
                    {
                        CompanyId = companyId,
                        ApplicantId = applicant.ApplicantId,
                        OfferId = offer.OfferId,
                        Status = autoComplete ? "ReadyForHire" : RecruitmentPreboardingStatus.PendingCandidateDetails,
                        OwnerUserId = actorUserId,
                        LastCommandIdempotencyKey = idempotencyKey,
                        Tasks = tasks,
                        CreatedBy = actorUserId,
                        UpdatedBy = actorUserId
                    });
                }
            }
        }
        catch
        {
            // Secondary side-effects must not roll back the primary offer persistence.
        }
    }

    public async Task<Result<RecruitmentOffer?>> GetByApplicant(string applicantId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(applicantId))
            return new Result<RecruitmentOffer?> { Success = false, StatusCode = StatusCodes.Status400BadRequest, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Company context and applicant ID are required." };

        var list = await offers.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId.Trim() && !x.IsDeleted);
        var latest = list?.OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate).FirstOrDefault();
        return new Result<RecruitmentOffer?> { Success = true, StatusCode = StatusCodes.Status200OK, MethodResult = latest, Message = latest != null ? "Offer loaded." : "No offer found." };
    }

    private static bool TryBuildCandidateOfferUrl(string token, out string offerUrl)
    {
        offerUrl = string.Empty;
        var appUrl = ConfigManager.AppSettings?.AppUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(appUrl) || !Uri.TryCreate(appUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            return false;
        offerUrl = $"{appUrl}/candidate/offer?token={Uri.EscapeDataString(token)}";
        return true;
    }
    private static Result<string> Ok(Result<string> result, string id, string message) { result.Success = true; result.StatusCode = StatusCodes.Status200OK; result.MethodResult = id; result.Message = message; return result; }
    private static Result<string> Fail(Result<string> result, string code, string message, int status = StatusCodes.Status400BadRequest) { result.ErrorCode = code; result.StatusCode = status; result.Message = message; return result; }
}
