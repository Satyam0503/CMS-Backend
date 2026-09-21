using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentOnboardingService(
    IMongoDbRepository<RecruitmentOnboardingCase> cases,
    IHttpContextAccessor context) : IRecruitmentOnboardingService
{
    private static readonly HashSet<string> ValidStatuses = [
        RecruitmentOnboardingStatus.PendingActivation,
        RecruitmentOnboardingStatus.InProgress,
        RecruitmentOnboardingStatus.Completed];
    private static readonly HashSet<string> ValidTaskStatuses = ["Pending", "InProgress", "Completed", "Skipped"];

    public async Task<Result<RecruitmentOnboardingCase>> Get(string applicantId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(applicantId))
            return new Result<RecruitmentOnboardingCase> { Success = false, StatusCode = StatusCodes.Status400BadRequest, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Company and applicant are required." };
        var onboarding = await cases.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicantId.Trim() && !x.IsDeleted);
        return onboarding is null
            ? new Result<RecruitmentOnboardingCase> { Success = false, StatusCode = StatusCodes.Status404NotFound, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Onboarding case was not found." }
            : new Result<RecruitmentOnboardingCase> { Success = true, StatusCode = StatusCodes.Status200OK, MethodResult = onboarding };
    }

    public async Task<Result<string>> Update(RecruitmentOnboardingUpdateCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(command.CaseId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(actorUserId))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Company, case, actor, and idempotency key are required.");
        var status = command.Status?.Trim();
        if (!ValidStatuses.Contains(status ?? string.Empty))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Onboarding status is invalid.");
        if (command.Tasks is null || command.Tasks.Count == 0)
            return Fail(RecruitmentErrorCodes.InvalidRequest, "At least one onboarding task update is required.");
        if (command.Tasks.Any(x => string.IsNullOrWhiteSpace(x.Code) || !ValidTaskStatuses.Contains(x.Status?.Trim() ?? string.Empty)))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Each task must have a valid code and status.");

        var onboarding = await cases.FirstOrDefault(x => x.CompanyId == companyId && x.CaseId == command.CaseId.Trim() && !x.IsDeleted);
        if (onboarding is null)
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Onboarding case was not found in the current company.", StatusCodes.Status404NotFound);
        if (onboarding.LastCommandIdempotencyKey == key)
            return Ok(onboarding.CaseId, "Onboarding command already applied.");

        var version = onboarding.Version <= 0 ? 1 : onboarding.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version)
            return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Onboarding case was changed by another user.", StatusCodes.Status409Conflict);

        var requested = command.Tasks.ToDictionary(x => x.Code.Trim(), x => x.Status.Trim(), StringComparer.OrdinalIgnoreCase);
        var currentByCode = onboarding.Tasks.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var unknown = requested.Keys.Where(code => !currentByCode.ContainsKey(code)).ToList();
        if (unknown.Count > 0)
            return Fail(RecruitmentErrorCodes.InvalidRequest, $"Unknown onboarding task code: {unknown[0]}.");
        foreach (var task in onboarding.Tasks)
            if (requested.TryGetValue(task.Code, out var nextStatus)) task.Status = nextStatus;

        if (status == RecruitmentOnboardingStatus.Completed && onboarding.Tasks.Any(x => x.Status is not ("Completed" or "Skipped")))
            return Fail(RecruitmentErrorCodes.InvalidTransition, "All onboarding tasks must be completed or skipped before closing the case.", StatusCodes.Status409Conflict);
        if (status == RecruitmentOnboardingStatus.PendingActivation && onboarding.Tasks.Any(x => x.Status is not "Pending"))
            return Fail(RecruitmentErrorCodes.InvalidTransition, "PendingActivation cannot contain started or completed tasks.", StatusCodes.Status409Conflict);

        onboarding.Status = status!;
        onboarding.LastCommandIdempotencyKey = key;
        onboarding.Version = version + 1;
        onboarding.UpdatedBy = actorUserId;
        onboarding.UpdatedDate = DateTime.UtcNow;
        var updated = await cases.Update(Builders<RecruitmentOnboardingCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == onboarding.CaseId && (x.Version == version || x.Version == 0)), onboarding);
        return updated.Success
            ? Ok(onboarding.CaseId, "Onboarding case updated.")
            : Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Onboarding case was changed by another user.", StatusCodes.Status409Conflict);
    }

    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = StatusCodes.Status200OK, MethodResult = id, Message = message };
    private static Result<string> Fail(string code, string message, int status = StatusCodes.Status400BadRequest) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
}


