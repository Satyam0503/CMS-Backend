using System.Text.RegularExpressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentOfferNegotiationService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentOffer> offers,
    IMongoDbRepository<RecruitmentOfferVersion> versions,
    IMongoDbRepository<RecruitmentOfferCounterRequest> counters,
    IMongoDbRepository<ApplicantLogs> logs,
    IHttpContextAccessor context) : IRecruitmentOfferNegotiationService
{
    private static readonly HashSet<string> VersionStatuses = ["Draft", "PendingApproval", "Approved", "Sent", "RevisedSent"];
    private static readonly Regex CurrencyPattern = new("^[A-Z]{3}$", RegexOptions.Compiled);
    private const int MaximumNegotiationRounds = 5;

    public async Task<Result<string>> CreateVersion(RecruitmentOfferVersionCommand command, string actor)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || !ValidKey(command.IdempotencyKey)) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Company and an idempotency key of at least 12 characters are required.");
        if (!ValidMoney(command.Salary, command.Currency) || !VersionStatuses.Contains(command.Status.Trim())) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Salary, currency, and offer status are invalid.");
        if (command.ExpiresAtUtc.HasValue && command.ExpiresAtUtc.Value <= DateTime.UtcNow) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Offer expiry must be in the future.");
        if (command.StartDateUtc.HasValue && command.ExpiresAtUtc.HasValue && command.ExpiresAtUtc.Value <= command.StartDateUtc.Value) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Offer expiry must be after the start date.");
        var offer = await offers.FirstOrDefault(x => x.CompanyId == companyId && x.OfferId == command.OfferId.Trim() && !x.IsDeleted);
        if (offer is null) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Offer was not found in the current company.");
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == offer.ApplicantId && !x.IsDeleted);
        if (applicant is null) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Offer applicant was not found in the current company.");
        var existing = await versions.FirstOrDefault(x => x.CompanyId == companyId && x.OfferId == offer.OfferId && x.CreatedBy == actor && x.IdempotencyKey == command.IdempotencyKey.Trim() && !x.IsDeleted);
        if (existing is not null) return Ok(existing.OfferVersionId, "Offer version already created.");
        var latest = (await versions.GetAll(x => x.CompanyId == companyId && x.OfferId == offer.OfferId && !x.IsDeleted)).OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        var latestNumber = latest?.VersionNumber ?? 0;
        if (command.ExpectedVersionNumber.HasValue && command.ExpectedVersionNumber.Value != latestNumber) return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict, "Offer version changed; refresh before creating a new version.", 409);
        var version = new RecruitmentOfferVersion { CompanyId = companyId, OfferId = offer.OfferId, ApplicantId = offer.ApplicantId, VersionNumber = latestNumber + 1, Salary = command.Salary, Currency = command.Currency.Trim().ToUpperInvariant(), StartDateUtc = command.StartDateUtc, Status = command.Status.Trim(), VersionReason = command.VersionReason?.Trim() ?? "ManualRevision", IdempotencyKey = command.IdempotencyKey.Trim(), ApprovalReference = command.ApprovalReference?.Trim(), ExpiresAtUtc = command.ExpiresAtUtc, CreatedBy = actor, UpdatedBy = actor };
        var added = await versions.AddOne(version);
        return added.Success ? Ok(version.OfferVersionId, "Offer version created.") : Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict, "Offer version could not be created; retry with the same idempotency key.", 409);
    }

    public async Task<Result<string>> RequestCounter(RecruitmentOfferCounterCommand command, string actor)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || !ValidKey(command.IdempotencyKey)) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Company and an idempotency key of at least 12 characters are required.");
        if (!ValidMoney(command.RequestedSalary, command.RequestedCurrency)) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Requested salary and currency are invalid.");
        if (command.RequestedStartDateUtc.HasValue && command.RequestedStartDateUtc.Value <= DateTime.UtcNow) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Requested start date must be in the future.");
        var offer = await offers.FirstOrDefault(x => x.CompanyId == companyId && x.OfferId == command.OfferId.Trim() && !x.IsDeleted);
        if (offer is null || offer.Status is not (RecruitmentOfferStatus.Sent or RecruitmentOfferStatus.Approved)) return Fail<string>(RecruitmentErrorCodes.InvalidTransition, "Only an active sent offer can be negotiated.", 409);
        var duplicate = await counters.FirstOrDefault(x => x.CompanyId == companyId && x.OfferId == offer.OfferId && x.IdempotencyKey == command.IdempotencyKey.Trim() && !x.IsDeleted);
        if (duplicate is not null) return Ok(duplicate.CounterRequestId, "Counter request already submitted.");
        var all = (await counters.GetAll(x => x.CompanyId == companyId && x.OfferId == offer.OfferId && !x.IsDeleted)).ToList();
        if (all.Count(x => x.Status is "Pending" or "Approved") >= MaximumNegotiationRounds) return Fail<string>(RecruitmentErrorCodes.InvalidTransition, "The maximum negotiation rounds for this offer have been reached.", 409);
        var latest = (await versions.GetAll(x => x.CompanyId == companyId && x.OfferId == offer.OfferId && !x.IsDeleted)).OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        var baseVersion = latest?.VersionNumber ?? 0;
        var counter = new RecruitmentOfferCounterRequest { CompanyId = companyId, OfferId = offer.OfferId, ApplicantId = offer.ApplicantId, BaseVersionNumber = baseVersion, RequestedSalary = command.RequestedSalary, RequestedCurrency = command.RequestedCurrency.Trim().ToUpperInvariant(), RequestedStartDateUtc = command.RequestedStartDateUtc, Message = command.Message?.Trim(), NegotiationRound = all.Count + 1, IdempotencyKey = command.IdempotencyKey.Trim(), CreatedBy = actor, UpdatedBy = actor };
        var added = await counters.AddOne(counter);
        return added.Success ? Ok(counter.CounterRequestId, "Counter request submitted for review.") : Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict, "Counter request could not be submitted; retry with the same idempotency key.", 409);
    }

    public async Task<Result<string>> DecideCounter(RecruitmentOfferCounterDecisionCommand command, string actor)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || !ValidKey(command.IdempotencyKey)) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Company and an idempotency key of at least 12 characters are required.");
        var counter = await counters.FirstOrDefault(x => x.CompanyId == companyId && x.CounterRequestId == command.CounterRequestId.Trim() && !x.IsDeleted);
        if (counter is null) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Counter request was not found.");
        if (counter.Status != "Pending") return Ok(counter.CounterRequestId, "Counter request was already resolved.");
        if (command.ExpectedBaseVersionNumber.HasValue && command.ExpectedBaseVersionNumber.Value != counter.BaseVersionNumber) return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict, "The counter is based on a stale offer version.", 409);
        var decision = command.Decision.Trim();
        if (decision is not ("Approved" or "Rejected")) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Decision must be Approved or Rejected.");
        if (decision == "Approved" && (!command.ApprovedSalary.HasValue || !ValidMoney(command.ApprovedSalary.Value, command.ApprovedCurrency))) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Approved salary and currency are required and must be valid.");
        if (command.ApprovedStartDateUtc.HasValue && command.ApprovedStartDateUtc.Value <= DateTime.UtcNow) return Fail<string>(RecruitmentErrorCodes.InvalidRequest, "Approved start date must be in the future.");
        if (decision == "Approved")
        {
            var latest = (await versions.GetAll(x => x.CompanyId == companyId && x.OfferId == counter.OfferId && !x.IsDeleted)).OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            var version = new RecruitmentOfferVersion { CompanyId = companyId, OfferId = counter.OfferId, ApplicantId = counter.ApplicantId, VersionNumber = (latest?.VersionNumber ?? counter.BaseVersionNumber) + 1, Salary = command.ApprovedSalary!.Value, Currency = command.ApprovedCurrency!.Trim().ToUpperInvariant(), StartDateUtc = command.ApprovedStartDateUtc ?? counter.RequestedStartDateUtc, Status = "PendingApproval", VersionReason = "CounterApproved", IdempotencyKey = $"counter-decision:{command.IdempotencyKey.Trim()}", CreatedBy = actor, UpdatedBy = actor };
            var added = await versions.AddOne(version); if (!added.Success) return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict, "Approved offer version could not be created; the offer changed concurrently.", 409);
        }
        counter.Status = decision; counter.DecisionIdempotencyKey = command.IdempotencyKey.Trim(); counter.ResolutionNote = command.ResolutionNote?.Trim(); counter.ResolvedAtUtc = DateTime.UtcNow; counter.UpdatedBy = actor; counter.UpdatedDate = DateTime.UtcNow;
        var updated = await counters.Update(Builders<RecruitmentOfferCounterRequest>.Filter.Where(x => x.CompanyId == companyId && x.CounterRequestId == counter.CounterRequestId && x.Status == "Pending"), counter);
        if (!updated.Success) return Fail<string>(RecruitmentErrorCodes.ConcurrencyConflict, "Counter request was changed by another user.", 409);
        await logs.AddOne(new ApplicantLogs { CompanyId = companyId, ApplicantId = counter.ApplicantId, UserId = actor, EventType = "OfferCounterDecision", Description = $"Offer counter {decision.ToLowerInvariant()}.", CreatedBy = actor, UpdatedBy = actor });
        return Ok(counter.CounterRequestId, $"Counter request {decision.ToLowerInvariant()}.");
    }

    private static bool ValidKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Trim().Length >= 12;
    private static bool ValidMoney(decimal salary, string? currency) => salary > 0 && !string.IsNullOrWhiteSpace(currency) && CurrencyPattern.IsMatch(currency.Trim().ToUpperInvariant());
    private static Result<T> Fail<T>(string code, string message, int status = 400) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
}
