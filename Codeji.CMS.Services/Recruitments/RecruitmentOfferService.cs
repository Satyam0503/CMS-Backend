using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
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
    IMongoDbRepository<JobVacancy>? vacancies = null) : IRecruitmentOfferService
{
    public async Task<Result<string>> Upsert(RecruitmentOfferCommand command, string actorUserId)
    {
        var result = new Result<string> { Success = false, StatusCode = StatusCodes.Status400BadRequest };
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(command.IdempotencyKey)) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Company and idempotency key are required.");
        if (!RecruitmentOfferRules.IsValidCompensation(command.Salary, command.Currency)) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Salary and currency are invalid.");
        if (command.ExpiresAtUtc.HasValue && command.ExpiresAtUtc.Value <= DateTime.UtcNow) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Offer expiry must be in the future.");
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && !x.IsDeleted);
        if (applicant is null) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Applicant was not found in the current company.");
        var idempotencyKey = command.IdempotencyKey.Trim();
        var existing = string.IsNullOrWhiteSpace(command.OfferId) ? await offers.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && x.LastCommandIdempotencyKey == idempotencyKey && !x.IsDeleted) : await offers.FirstOrDefault(x => x.CompanyId == companyId && x.OfferId == command.OfferId && !x.IsDeleted);
        if (existing is not null && string.Equals(existing.LastCommandIdempotencyKey, idempotencyKey, StringComparison.Ordinal)) return Ok(result, existing.OfferId, "Offer command already applied.");
        if (existing is null)
        {
            if (!string.Equals(command.Status, RecruitmentOfferStatus.Draft, StringComparison.OrdinalIgnoreCase)) return Fail(result, RecruitmentErrorCodes.InvalidTransition, "Create the offer as Draft first, then submit it for approval or send it.", StatusCodes.Status409Conflict);

var offer = new RecruitmentOffer { CompanyId = companyId, ApplicantId = command.ApplicantId, VacancyId = command.VacancyId, Salary = command.Salary, Currency = command.Currency.Trim().ToUpperInvariant(), StartDateUtc = command.StartDateUtc.HasValue ? DateTime.SpecifyKind(command.StartDateUtc.Value, DateTimeKind.Utc) : null, ExpiresAtUtc = command.ExpiresAtUtc.HasValue ? DateTime.SpecifyKind(command.ExpiresAtUtc.Value, DateTimeKind.Utc) : null, Status = RecruitmentOfferStatus.Draft, LastCommandIdempotencyKey = idempotencyKey, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            var saved = await offers.AddOne(offer);
            return saved.Success ? Ok(result, offer.OfferId, "Offer created.") : Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Offer could not be created.", StatusCodes.Status409Conflict);
        }
        if (!RecruitmentOfferRules.IsAllowedTransition(existing.Status, command.Status)) return Fail(result, RecruitmentErrorCodes.InvalidTransition, "Offer status transition is not allowed.", StatusCodes.Status409Conflict);
        if (string.Equals(command.Status, RecruitmentOfferStatus.Approved, StringComparison.OrdinalIgnoreCase))
        {
            JobVacancy? vacancy = vacancies is null ? null : await vacancies.FirstOrDefault(x => x.CompanyId == companyId && x.JobId == command.VacancyId && !x.IsDeleted);
            var policy = approvalPolicies is null ? null : await approvalPolicies.FirstOrDefault(x => x.CompanyId == companyId && x.VacancyId == command.VacancyId && !x.IsDeleted);
            if (policy is null && approvalPolicies is not null && vacancy is not null && (!string.IsNullOrWhiteSpace(vacancy.LegalEntityId) || !string.IsNullOrWhiteSpace(vacancy.DepartmentId) || !string.IsNullOrWhiteSpace(vacancy.Grade))) policy = await approvalPolicies.FirstOrDefault(x => x.CompanyId == companyId && string.IsNullOrEmpty(x.VacancyId) && x.LegalEntityId == vacancy.LegalEntityId && x.DepartmentId == vacancy.DepartmentId && x.Grade == vacancy.Grade && !x.IsDeleted);
            policy ??= approvalPolicies is null ? null : await approvalPolicies.FirstOrDefault(x => x.CompanyId == companyId && string.IsNullOrEmpty(x.VacancyId) && string.IsNullOrEmpty(x.LegalEntityId) && string.IsNullOrEmpty(x.DepartmentId) && string.IsNullOrEmpty(x.Grade) && !x.IsDeleted);
            var roleMatches = string.IsNullOrWhiteSpace(policy?.ApproverRoleId) || string.Equals(CurrentContext.UserRoleId(context), policy.ApproverRoleId, StringComparison.Ordinal);
            var delegateMatches = !string.IsNullOrWhiteSpace(policy?.DelegatedApproverUserId) && string.Equals(CurrentContext.UserId(context), policy.DelegatedApproverUserId, StringComparison.Ordinal) && (!policy!.DelegationExpiresAtUtc.HasValue || policy.DelegationExpiresAtUtc > DateTime.UtcNow);
            if ((!string.IsNullOrWhiteSpace(policy?.ApproverRoleId) || !string.IsNullOrWhiteSpace(policy?.DelegatedApproverUserId)) && !roleMatches && !delegateMatches) return Fail(result, RecruitmentErrorCodes.ApprovalRequired, "This offer must be approved by the configured approver role or active delegation.", StatusCodes.Status403Forbidden);
            var requiresReference = policy?.RequireApprovalReference ?? true;
            var thresholdApplies = policy is null || (string.Equals(policy.Currency, command.Currency.Trim(), StringComparison.OrdinalIgnoreCase) && command.Salary >= policy.ApprovalThreshold);
            if (requiresReference && thresholdApplies && string.IsNullOrWhiteSpace(command.ApprovalReference) && string.IsNullOrWhiteSpace(existing.ApprovalReference)) return Fail(result, RecruitmentErrorCodes.ApprovalRequired, "A shared approval reference is required before an offer can be approved.", StatusCodes.Status409Conflict);
        }
        var currentVersion = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != currentVersion) return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Offer was changed by another user.", StatusCodes.Status409Conflict);
        var previousStatus = existing.Status;
        if (!string.IsNullOrWhiteSpace(command.ApprovalReference)) existing.ApprovalReference = command.ApprovalReference.Trim();
        existing.Salary = command.Salary;
        existing.Currency = command.Currency.Trim().ToUpperInvariant();
        existing.StartDateUtc = command.StartDateUtc.HasValue ? DateTime.SpecifyKind(command.StartDateUtc.Value, DateTimeKind.Utc) : null;
        existing.ExpiresAtUtc = command.ExpiresAtUtc.HasValue ? DateTime.SpecifyKind(command.ExpiresAtUtc.Value, DateTimeKind.Utc) : null;
        existing.Status = command.Status.Trim();
        existing.LastCommandIdempotencyKey = idempotencyKey;
        existing.Version = currentVersion + 1;
        existing.UpdatedBy = actorUserId;
        existing.UpdatedDate = DateTime.UtcNow;
        var updated = await offers.Update(Builders<RecruitmentOffer>.Filter.Where(x => x.CompanyId == companyId && x.OfferId == existing.OfferId && (x.Version == currentVersion || x.Version == 0)), existing);
        if (!updated.Success) return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Offer was changed by another user.", StatusCodes.Status409Conflict);
        await applicantLogs.AddOne(new ApplicantLogs { CompanyId = companyId, ApplicantId = applicant.ApplicantId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobRole = applicant.VacancyId, UserId = actorUserId, ActivityCategory = 0, Description = $"Offer status changed from {previousStatus} to {existing.Status}.", EventType = "OfferStatusChanged", PreviousStage = previousStatus, CurrentStage = existing.Status, CreatedBy = actorUserId, UpdatedBy = actorUserId });
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

    private static Result<string> Ok(Result<string> result, string id, string message) { result.Success = true; result.StatusCode = StatusCodes.Status200OK; result.MethodResult = id; result.Message = message; return result; }
    private static Result<string> Fail(Result<string> result, string code, string message, int status = StatusCodes.Status400BadRequest) { result.ErrorCode = code; result.StatusCode = status; result.Message = message; return result; }
}





