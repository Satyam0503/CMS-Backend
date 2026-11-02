using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentOnboardingService(
    IMongoDbRepository<RecruitmentOnboardingCase> cases,
    IHttpContextAccessor context,
    IMongoDbRepository<EmpUser>? employees = null,
    IMongoDbRepository<Applicant>? applicants = null,
    IMongoDbRepository<JobVacancy>? vacancies = null) : IRecruitmentOnboardingService
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

    public async Task<Result<RecruitmentOnboardingQueueItem>> GetQueue(string? status, int pageNo, int pageSize)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId))
            return new Result<RecruitmentOnboardingQueueItem> { Success = false, StatusCode = StatusCodes.Status400BadRequest, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Company context is required." };
        var normalizedStatus = status?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedStatus) && !ValidStatuses.Contains(normalizedStatus))
            return new Result<RecruitmentOnboardingQueueItem> { Success = false, StatusCode = StatusCodes.Status400BadRequest, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Onboarding status is invalid." };
        pageNo = Math.Max(1, pageNo);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var casesForTenant = (await cases.GetAll(x => x.CompanyId == companyId && !x.IsDeleted && (string.IsNullOrWhiteSpace(normalizedStatus) || x.Status == normalizedStatus)))
            .OrderByDescending(x => x.UpdatedDate)
            .ToList();
        var page = casesForTenant.Skip((pageNo - 1) * pageSize).Take(pageSize).ToList();
        var applicantIds = page.Select(x => x.ApplicantId).Distinct().ToHashSet();
        var appList = applicants is not null && applicantIds.Count > 0
            ? (await applicants.GetAll(x => x.CompanyId == companyId && !x.IsDeleted && applicantIds.Contains(x.ApplicantId))).ToList()
            : [];
        var appMap = appList.ToDictionary(x => x.ApplicantId, x => x);
        var vacIds = appList.Select(x => x.VacancyId).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToHashSet();
        var vacMap = vacancies is not null && vacIds.Count > 0
            ? (await vacancies.GetAll(x => x.CompanyId == companyId && !x.IsDeleted && vacIds.Contains(x.JobId))).ToDictionary(x => x.JobId, x => x.Title)
            : new Dictionary<string, string>();
        var items = page.Select(item =>
        {
            var total = item.Tasks.Count;
            var completed = item.Tasks.Count(task => task.Status is "Completed" or "Skipped");
            var pending = item.Tasks.Count(task => task.Status is not ("Completed" or "Skipped"));
            var blocker = item.Tasks.FirstOrDefault(task => task.Status is not ("Completed" or "Skipped"));
            return new RecruitmentOnboardingQueueItem
            {
                CaseId = item.CaseId,
                ApplicantId = item.ApplicantId,
                ApplicantName = appMap.TryGetValue(item.ApplicantId, out var app) ? $"{app.FirstName} {app.LastName}".Trim() : null,
                Email = app?.Email,
                VacancyId = app?.VacancyId ?? string.Empty,
                VacancyName = vacMap.GetValueOrDefault(app?.VacancyId ?? string.Empty),
                EmployeeUserId = item.EmployeeUserId,
                Status = item.Status,
                Version = item.Version,
                TotalTasks = total,
                CompletedTasks = completed,
                RequiredPendingTasks = pending,
                CompletionPercent = total == 0 ? 0 : (int)Math.Round(completed * 100d / total),
                FirstBlockingTaskCode = blocker?.Code,
                FirstBlockingTaskTitle = blocker?.Title,
                UpdatedDate = item.UpdatedDate
            };
        }).ToList();
        return new Result<RecruitmentOnboardingQueueItem> { Success = true, StatusCode = StatusCodes.Status200OK, MethodResults = items, TotalRecords = casesForTenant.Count, Message = "Onboarding queue loaded." };
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
        {
            var replay = await ActivateEmployeeLifecycle(onboarding, companyId, actorUserId);
            return Ok(onboarding.CaseId, replay ?? "Onboarding command already applied.");
        }

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
        if (!updated.Success)
            return Fail(RecruitmentErrorCodes.ConcurrencyConflict, "Onboarding case was changed by another user.", StatusCodes.Status409Conflict);

        var activationMessage = await ActivateEmployeeLifecycle(onboarding, companyId, actorUserId);
        return Ok(onboarding.CaseId, activationMessage ?? "Onboarding case updated.");
    }

    public async Task<Result<string>> RetryActivation(string caseId, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(caseId) || string.IsNullOrWhiteSpace(actorUserId))
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Company, case, and actor are required.");
        var onboarding = await cases.FirstOrDefault(x => x.CompanyId == companyId && x.CaseId == caseId.Trim() && !x.IsDeleted);
        if (onboarding is null)
            return Fail(RecruitmentErrorCodes.InvalidRequest, "Onboarding case was not found in the current company.", StatusCodes.Status404NotFound);
        if (onboarding.Status != RecruitmentOnboardingStatus.Completed)
            return Fail(RecruitmentErrorCodes.InvalidTransition, "Only a completed onboarding case can activate an employee.", StatusCodes.Status409Conflict);
        var message = await ActivateEmployeeLifecycle(onboarding, companyId, actorUserId);
        return Ok(onboarding.CaseId, message ?? "Onboarding activation is pending until the linked employee is available.");
    }
    private async Task<string?> ActivateEmployeeLifecycle(RecruitmentOnboardingCase onboarding, string companyId, string actorUserId)
    {
        if (onboarding.Status != RecruitmentOnboardingStatus.Completed || employees is null || string.IsNullOrWhiteSpace(onboarding.EmployeeUserId))
            return null;

        var employee = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == onboarding.EmployeeUserId && !x.IsDeleted);
        if (employee is null)
            return "Onboarding case updated; employee lifecycle activation is pending because the linked employee was not found.";

        employee.Lifecycle ??= new EmployeeLifecycleState();        if (employee.Status && employee.EmploymentStatus == EmployeeLifecycleStatuses.Active
            && employee.Lifecycle?.EmploymentStatus == EmployeeLifecycleStatuses.Active
            && employee.Lifecycle?.AvailabilityStatus == EmployeeLifecycleStatuses.Working
            && employee.Lifecycle?.AccountStatus == EmployeeLifecycleStatuses.AccountActive
            && employee.Lifecycle?.PayrollStatus == EmployeeLifecycleStatuses.PayrollActive)
            return "Onboarding case and employee lifecycle are already active.";
        var now = DateTime.UtcNow;
        var previousVersion = employee.Lifecycle.Version;
        var changes = new (string Dimension, string Previous, string Next)[]
        {
            (nameof(EmployeeLifecycleState.EmploymentStatus), employee.Lifecycle.EmploymentStatus, EmployeeLifecycleStatuses.Active),
            (nameof(EmployeeLifecycleState.AvailabilityStatus), employee.Lifecycle.AvailabilityStatus, EmployeeLifecycleStatuses.Working),
            (nameof(EmployeeLifecycleState.AccountStatus), employee.Lifecycle.AccountStatus, EmployeeLifecycleStatuses.AccountActive),
            (nameof(EmployeeLifecycleState.PayrollStatus), employee.Lifecycle.PayrollStatus, EmployeeLifecycleStatuses.PayrollActive)
        };
        foreach (var change in changes.Where(x => !string.Equals(x.Previous, x.Next, StringComparison.OrdinalIgnoreCase)))
            employee.Lifecycle.History.Add(new EmployeeLifecycleStatusHistory { Dimension = change.Dimension, PreviousValue = change.Previous, NewValue = change.Next, ReasonCode = "RECRUITMENT_ONBOARDING_COMPLETED", Reason = "Recruitment onboarding completed", EffectiveFromUtc = now, ChangedBy = actorUserId, ChangedAtUtc = now });
        employee.Lifecycle.EmploymentStatus = EmployeeLifecycleStatuses.Active;
        employee.Lifecycle.AvailabilityStatus = EmployeeLifecycleStatuses.Working;
        employee.Lifecycle.AvailabilityEffectiveFrom = now;
        employee.Lifecycle.AccountStatus = EmployeeLifecycleStatuses.AccountActive;
        employee.Lifecycle.PayrollStatus = EmployeeLifecycleStatuses.PayrollActive;
        employee.Lifecycle.Version = Math.Max(0, previousVersion) + 1;
        employee.EmploymentStatus = EmployeeLifecycleStatuses.Active;
        employee.Status = true;
        employee.UpdatedBy = actorUserId;
        employee.UpdatedDate = now;
        var filter = Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted && (x.Lifecycle.Version == previousVersion || previousVersion == 0));
        var updated = await employees.Update(filter, employee);
        return updated.Success
            ? "Onboarding case and employee lifecycle activated."
            : "Onboarding case updated; employee lifecycle activation is pending and must be retried.";
    }
    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = StatusCodes.Status200OK, MethodResult = id, Message = message };
    private static Result<string> Fail(string code, string message, int status = StatusCodes.Status400BadRequest) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
}


