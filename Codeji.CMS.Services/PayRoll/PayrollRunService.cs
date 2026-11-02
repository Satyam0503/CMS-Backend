using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Interfaces;
using Codeji.CMS.Services.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public interface IPayrollRunService
{
    Task<Result<PayrollRunDto>> Create(
        string companyId,
        string actorId,
        CreatePayrollRunRequestDto request
    );
    Task<List<PayrollRunDto>> List(string companyId);
    Task<Result<PayrollRunDto>> Get(string companyId, string id);
    Task<Result<List<PayrollRunEmployeeDto>>> GetEmployees(string companyId, string id);
}

public sealed class PayrollRunService(
    IMongoDbRepository<PayrollRun> runs,
    IMongoDbRepository<PayrollRunEmployee> runEmployees,
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<PayrollDivisorPolicy> policies,
    IMongoDbRepository<EmployeePayrollAssignment>? assignments = null,
    IMongoDbRepository<SalaryTemplate>? templates = null,
    IMongoDbRepository<PayrollPayGroup>? payGroups = null,
    IMongoDbRepository<PayrollPolicyProfile>? policyProfiles = null,
    IMongoDbRepository<EmployeeSalaryStructure>? employeeSalaryStructures = null,
    ISalaryRepository? legacySalaries = null,
    IPayrollReadinessService? readinessService = null
) : IPayrollRunService
{
    public async Task<Result<PayrollRunDto>> Create(
        string companyId,
        string actorId,
        CreatePayrollRunRequestDto request
    )
    {
        var start = new DateTime(request.PeriodStart.Year, request.PeriodStart.Month, 1);
        var end = new DateTime(
            request.PeriodEnd.Year,
            request.PeriodEnd.Month,
            DateTime.DaysInMonth(request.PeriodEnd.Year, request.PeriodEnd.Month)
        );
        if (
            request.PeriodStart.Date != start
            || request.PeriodEnd.Date != end
            || end < start
            || request.PayDate == default
        )
            return Fail(400, "Valid period boundaries and payday are required.");
        if (start.Year != end.Year || start.Month != end.Month)
            return Fail(
                400,
                "A payroll run must cover exactly one calendar month. Create a separate run for each month."
            );

        var type = (request.RunType ?? "REGULAR").Trim().ToUpperInvariant();
        var population = (request.PopulationType ?? "ALL_ELIGIBLE").Trim().ToUpperInvariant();
        if (
            type
            is not (
                "REGULAR"
                or "OFF_CYCLE"
                or "SUPPLEMENTAL"
                or "FINAL_SETTLEMENT"
                or "CORRECTION"
                or "REVERSAL"
            )
        )
            return Fail(400, "Unsupported payroll run type.");

        if (readinessService is not null)
        {
            var readiness = await readinessService.ValidateCompanyPayrollReadiness(companyId, start);
            if (!readiness.IsReady)
                return Fail(400, $"Company payroll setup is not ready for {start:MMMM yyyy}: {string.Join("; ", readiness.BlockingIssues)}");
        }
        var requestedEmployeeIds = request.EmployeeIds ?? [];
        if (population == "SELECTED" && requestedEmployeeIds.Count == 0)
            return Fail(400, "Selected population requires employees.");
        var selectedEmployeeIds = requestedEmployeeIds.Select(id => id?.Trim()).ToList();
        if (population == "SELECTED" && selectedEmployeeIds.Any(string.IsNullOrWhiteSpace))
            return Fail(400, "Selected employee IDs cannot be blank.");
        if (
            population == "SELECTED"
            && selectedEmployeeIds.Distinct(StringComparer.OrdinalIgnoreCase).Count()
                != selectedEmployeeIds.Count
        )
            return Fail(
                400,
                "Selected employee IDs contain duplicates. Remove duplicates and retry."
            );
        var requestFingerprint = CreateRequestFingerprint(
            start,
            end,
            request.PayDate.Date,
            type,
            population,
            request.DepartmentId,
            request.PayGroupId,
            selectedEmployeeIds
        );

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();
        if (idempotencyKey is not null)
        {
            var old = await runs.FirstOrDefault(x =>
                x.CompanyId == companyId && x.IdempotencyKey == idempotencyKey
            );
            if (old is not null)
            {
                if (old.Status == "CANCELLED")
                    return Fail(
                        409,
                        "This idempotency key belongs to a cancelled payroll run. Use a new key to create its replacement."
                    );
                return await MatchesCreateRequest(
                    companyId,
                    old,
                    start,
                    end,
                    request.PayDate.Date,
                    type,
                    population,
                    request.DepartmentId,
                    request.PayGroupId,
                    selectedEmployeeIds,
                    requestFingerprint
                )
                    ? Ok(old)
                    : Fail(
                        409,
                        "This idempotency key was already used for a different payroll request. Use a new key or reload the existing run."
                    );
            }
        }

        var existing = await runs.Exist(x =>
            x.CompanyId == companyId
            && x.PeriodStart == start
            && x.RunType == type
            && x.PopulationType == population
            && x.DepartmentId == request.DepartmentId
            && x.PayGroupId == request.PayGroupId
            && x.IsActive
            && x.Status != "CANCELLED"
        );
        if (existing)
            return Fail(409, "An equivalent payroll run already exists.");

        var requestedEmployees = (
            await employees.GetAll(x =>
                x.CompanyId == companyId
                && !x.IsDeleted
                && (requestedEmployeeIds.Count == 0 || selectedEmployeeIds.Contains(x.EmployeeId))
                && (
                    string.IsNullOrWhiteSpace(request.DepartmentId)
                    || x.Department == request.DepartmentId
                )
            )
        ).ToList();
        if (population == "SELECTED")
        {
            var matchedIds = requestedEmployees
                .Select(employee => employee.EmployeeId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (selectedEmployeeIds.Any(employeeId => !matchedIds.Contains(employeeId!)))
                return Fail(
                    409,
                    "One or more selected employees were not found in this company or do not match the run filters. Refresh the selection; no payroll run was created."
                );
        }
        var populationEmployees = requestedEmployees
            .Where(employee => IsEligibleForPayrollPeriod(employee, start, end))
            .ToList();
        if (
            population == "SELECTED"
            && requestedEmployees.Any(employee => !IsEligibleForPayrollPeriod(employee, start, end))
        )
            return Fail(
                409,
                "One or more selected employees are not eligible for payroll in this period. Check employment status and date of joining."
            );
        if (populationEmployees.Count == 0)
            return Fail(409, "No eligible employees were found for this payroll period.");

        var divisorPolicy = await policies.FirstOrDefault(x =>
            x.CompanyId == companyId
            && x.IsActive
            && x.EffectiveFrom <= end
            && (!x.EffectiveTo.HasValue || x.EffectiveTo >= start)
        );
        IEnumerable<PayrollPolicyProfile> configuredPolicy = policyProfiles is null
            ? Array.Empty<PayrollPolicyProfile>()
            : (await policyProfiles.GetAll(x => x.CompanyId == companyId && x.IsActive)).ToList();

        var run = new PayrollRun
        {
            CompanyId = companyId,
            PeriodStart = start,
            PeriodEnd = end,
            PayDate = request.PayDate.Date,
            IsActive = true,
            RunType = type,
            PopulationType = population,
            DepartmentId = request.DepartmentId,
            PayGroupId = request.PayGroupId,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = requestFingerprint,
            EmployeeCount = populationEmployees.Count,
            ReadyCount = populationEmployees.Count,
            CreatedBy = actorId,
            CreatedAtUtc = DateTime.UtcNow,
            Status = "DRAFT",
            ConfigurationSnapshot = new PayrollConfigurationSnapshot
            {
                DivisorPolicyId = divisorPolicy?.Id,
                DivisorPolicyVersion = divisorPolicy?.Version ?? 0,
                DivisorPolicy = divisorPolicy?.DivisorPolicy ?? "CALENDAR_DAYS",
                LopCalculationBase = divisorPolicy?.LopCalculationBase ?? "FIXED_GROSS",
                EpfMode = divisorPolicy?.EpfMode ?? "CAPPED_CEILING",
                EpfWageCeiling = divisorPolicy?.EpfWageCeiling ?? 15000m,
                EpfEmployeeRatePercent = divisorPolicy?.EpfEmployeeRatePercent ?? 12m,
                EpfEmployerRatePercent = divisorPolicy?.EpfEmployerRatePercent ?? 12m,
                EsicMode = divisorPolicy?.EsicMode ?? "STATUTORY",
                EsicWageCeiling = divisorPolicy?.EsicWageCeiling ?? 21000m,
                TdsMode = divisorPolicy?.TdsMode ?? "AUTO_NEW_REGIME",
                EsicEmployeeRatePercent = divisorPolicy?.EsicEmployeeRatePercent ?? 0.75m,
                EsicEmployerRatePercent = divisorPolicy?.EsicEmployerRatePercent ?? 3.25m,
                RequireAttendanceLock = divisorPolicy?.RequireAttendanceLock ?? true,
                CapturedAtUtc = DateTime.UtcNow,
            },
        };

        var assignmentRows = assignments is null
            ? []
            : (await assignments.GetAll(x => x.CompanyId == companyId)).ToList();
        var templateRows = templates is null
            ? []
            : (await templates.GetAll(x => x.CompanyId == companyId)).ToList();
        var payGroupRows = payGroups is null
            ? []
            : (await payGroups.GetAll(x => x.CompanyId == companyId)).ToList();
        var approvedSalaryRows = employeeSalaryStructures is null
            ? []
            : (
                await employeeSalaryStructures.GetAll(x =>
                    x.CompanyId == companyId
                    && x.ApprovalStatus == "APPROVED"
                    && x.Status == "ACTIVE"
                )
            ).ToList();

        var legacySalaryRowsByEmployee = new Dictionary<string, List<SalaryModel>>(
            StringComparer.Ordinal
        );
        if (legacySalaries is not null)
        {
            foreach (var employee in populationEmployees)
            {
                if (!Guid.TryParse(employee.UserId, out var userId))
                    continue;
                var history = await legacySalaries.GetSalaryHistoryAsync(companyId, userId);
                DateTime? joiningDate = EmploymentPeriod.GetJoiningDate(employee);
                var resolution = PayrollSalarySourceResolver.Resolve(
                    companyId,
                    employee.EmployeeId,
                    start,
                    end,
                    approvedSalaryRows,
                    () => history.Where(x => x.UserId == userId),
                    joiningDate
                );
                if (!resolution.UsesCanonicalSource)
                    legacySalaryRowsByEmployee[employee.EmployeeId] = resolution.Records;
            }
        }

        var policyResolutionByEmployee = new Dictionary<string, PayrollPeriodPolicyResolution>(
            StringComparer.Ordinal
        );
        foreach (var employee in populationEmployees)
        {
            var eligibleStart = start;
            DateTime? empJoining = EmploymentPeriod.GetJoiningDate(employee);
            if (empJoining.HasValue && empJoining.Value.Date > eligibleStart)
                eligibleStart = empJoining.Value.Date;
            var eligibleEnd = end;
            var lastWorkingDate = EmploymentPeriod.GetLastWorkingDate(employee);
            if (lastWorkingDate.HasValue && lastWorkingDate.Value.Date < eligibleEnd)
                eligibleEnd = lastWorkingDate.Value.Date;
            if (eligibleStart > eligibleEnd)
                continue;

            var employeeSalaryRows = approvedSalaryRows
                .Where(x => x.EmployeeId == employee.EmployeeId)
                .ToList();
            var periodPolicy = PayrollPolicyResolver.ResolveConsistentPeriod(
                companyId,
                employee.EmployeeId,
                eligibleStart,
                eligibleEnd,
                assignmentRows,
                employeeSalaryRows,
                configuredPolicy,
                payGroupRows,
                request.PayGroupId,
                empJoining
            );
            if (!periodPolicy.IsConsistent)
                return Fail(
                    409,
                    $"Employee {employee.EmployeeId} changes statutory policy on {periodPolicy.FirstConflictDate:yyyy-MM-dd}; split the payroll period or align the policy effective date before creating this run."
                );
            policyResolutionByEmployee[employee.EmployeeId] = periodPolicy;
        }

        var saved = await runs.AddOne(run);
        if (
            !saved.Success
            && IsUniqueIndexConflict(saved.Message, "ux_payrollrun_company_idempotency")
        )
        {
            var winningRun = idempotencyKey is null
                ? null
                : await runs.FirstOrDefault(x =>
                    x.CompanyId == companyId && x.IdempotencyKey == idempotencyKey
                );
            if (winningRun is not null && winningRun.Status != "CANCELLED")
                return await MatchesCreateRequest(
                    companyId,
                    winningRun,
                    start,
                    end,
                    request.PayDate.Date,
                    type,
                    population,
                    request.DepartmentId,
                    request.PayGroupId,
                    selectedEmployeeIds,
                    requestFingerprint
                )
                    ? Ok(winningRun)
                    : Fail(
                        409,
                        "This idempotency key was concurrently used for a different payroll request. Use a new key or reload the existing run."
                    );
            return Fail(
                409,
                "This payroll request was already submitted or cancelled. Refresh the payroll runs and use a new idempotency key if creating a replacement."
            );
        }
        if (
            !saved.Success
            && (
                IsUniqueIndexConflict(saved.Message, "ux_payrollrun_company_period_type_population")
                || IsUniqueIndexConflict(
                    saved.Message,
                    "ux_payrollrun_company_period_type_population_active"
                )
            )
        )
            return Fail(
                409,
                "An equivalent payroll run was created concurrently. Refresh payroll runs before trying again."
            );
        if (!saved.Success)
            return new Result<PayrollRunDto>
            {
                Success = false,
                StatusCode = saved.StatusCode,
                Message = saved.Message,
            };

        var lines = populationEmployees
            .Select(employee =>
            {
                var assignment = PayrollPolicyResolver.ResolveAssignment(
                    companyId,
                    employee.EmployeeId,
                    start,
                    assignmentRows
                );
                var employeeSalaryRows = approvedSalaryRows
                    .Where(x => x.EmployeeId == employee.EmployeeId)
                    .ToList();
                DateTime? empJoining = EmploymentPeriod.GetJoiningDate(employee);
                var salaryAtStart = EmployeeSalaryStructureResolver.Resolve(
                    companyId,
                    employee.EmployeeId,
                    start,
                    employeeSalaryRows,
                    approvedOnly: true,
                    joiningDate: empJoining
                );
                var overlappingSalaryRows = employeeSalaryRows
                    .Where(x =>
                        x.EffectiveFrom.Date <= end.Date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= start.Date)
                    )
                    .OrderBy(x => x.EffectiveFrom)
                    .ThenBy(x => x.Version)
                    .ToList();
                var salaryForSnapshot = salaryAtStart ?? overlappingSalaryRows.FirstOrDefault();
                legacySalaryRowsByEmployee.TryGetValue(employee.EmployeeId, out var legacyRows);
                var effectivePayGroupId =
                    assignment?.PayGroupId ?? salaryForSnapshot?.PayGroupId ?? request.PayGroupId;
                var resolvedPolicy = policyResolutionByEmployee.TryGetValue(
                    employee.EmployeeId,
                    out var periodPolicy
                )
                    ? periodPolicy.Profile
                    : PayrollPolicyResolver.ResolvePolicy(
                        companyId,
                        start,
                        assignment,
                        configuredPolicy,
                        payGroupRows,
                        effectivePayGroupId
                    );
                var templateId =
                    assignment?.SalaryTemplateId ?? salaryForSnapshot?.SalaryTemplateId;
                var template = templateId is null
                    ? null
                    : templateRows.FirstOrDefault(x =>
                        x.CompanyId == companyId
                        && x.Id == templateId
                        && x.IsActive
                        && x.EffectiveFrom.Date <= start.Date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= start.Date)
                    );
                var group = effectivePayGroupId is null
                    ? null
                    : payGroupRows.FirstOrDefault(x =>
                        x.CompanyId == companyId && x.Id == effectivePayGroupId
                    );
                return new PayrollRunEmployee
                {
                    CompanyId = companyId,
                    PayrollRunId = run.Id,
                    EmployeeId = employee.EmployeeId,
                    Status = "READY",
                    InclusionReason = "Population rule matched",
                    SalaryRecordId = salaryAtStart?.Id ?? salaryForSnapshot?.Id,
                    PayGroupId = effectivePayGroupId,
                    DepartmentId = employee.Department,
                    InputSnapshot = BuildSnapshot(
                        assignment,
                        template,
                        group,
                        resolvedPolicy,
                        salaryForSnapshot,
                        overlappingSalaryRows,
                        legacyRows ?? [],
                        divisorPolicy
                    ),
                };
            })
            .ToList();

        if (lines.Count > 0)
        {
            Result linePersistence;
            try
            {
                linePersistence = await runEmployees.AddMany(lines);
            }
            catch
            {
                linePersistence = new Result { Success = false, StatusCode = 500 };
            }

            if (!linePersistence.Success)
                return await CancelIncompleteRun(companyId, actorId, run, linePersistence);
        }
        return new Result<PayrollRunDto>
        {
            Success = true,
            StatusCode = 201,
            Message = "Payroll run created.",
            ResourceId = run.Id,
            MethodResult = Map(run),
        };
    }

    public async Task<List<PayrollRunDto>> List(string companyId) =>
        (await runs.GetAll(x => x.CompanyId == companyId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(Map)
            .ToList();

    public async Task<Result<PayrollRunDto>> Get(string companyId, string id)
    {
        var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        return run is null
            ? new Result<PayrollRunDto>
            {
                Success = false,
                StatusCode = 404,
                Message = "Payroll run was not found.",
            }
            : new Result<PayrollRunDto> { Success = true, MethodResult = Map(run) };
    }

    public async Task<Result<List<PayrollRunEmployeeDto>>> GetEmployees(string companyId, string id)
    {
        if (!await runs.Exist(x => x.CompanyId == companyId && x.Id == id))
            return new Result<List<PayrollRunEmployeeDto>>
            {
                Success = false,
                StatusCode = 404,
                Message = "Payroll run was not found.",
            };
        var rows = (
            await runEmployees.GetAll(x => x.CompanyId == companyId && x.PayrollRunId == id)
        )
            .Select(x => new PayrollRunEmployeeDto
            {
                EmployeeId = x.EmployeeId,
                Status = x.Status,
                InclusionReason = x.InclusionReason,
                ExclusionReason = x.ExclusionReason,
                ErrorMessage = x.ErrorMessage,
                InputSnapshot = new PayrollEmployeeInputSnapshotDto
                {
                    AssignmentId = x.InputSnapshot.AssignmentId,
                    AssignmentVersion = x.InputSnapshot.AssignmentVersion,
                    SalaryStructureId = x.InputSnapshot.SalaryStructureId,
                    SalaryStructureVersion = x.InputSnapshot.SalaryStructureVersion,
                    SalaryStructures = x
                        .InputSnapshot.SalaryStructures.Select(
                            s => new PayrollSalaryStructureSnapshotDto
                            {
                                SalaryStructureId = s.SalaryStructureId,
                                Version = s.Version,
                                PayGroupId = s.PayGroupId,
                                EffectiveFrom = s.EffectiveFrom,
                                EffectiveTo = s.EffectiveTo,
                                Components = s.Components.Select(MapComponent).ToList(),
                            }
                        )
                        .ToList(),
                    SalaryTemplateId = x.InputSnapshot.SalaryTemplateId,
                    SalaryTemplateVersion = x.InputSnapshot.SalaryTemplateVersion,
                    PayGroupId = x.InputSnapshot.PayGroupId,
                    PayGroupVersion = x.InputSnapshot.PayGroupVersion,
                    PolicyProfileId = x.InputSnapshot.PolicyProfileId,
                    PolicyProfileVersion = x.InputSnapshot.PolicyProfileVersion,
                    EpfMode = x.InputSnapshot.EpfMode,
                    EpfWageCeiling = x.InputSnapshot.EpfWageCeiling,
                    EpfEmployeeRatePercent = x.InputSnapshot.EpfEmployeeRatePercent,
                    EpfEmployerRatePercent = x.InputSnapshot.EpfEmployerRatePercent,
                    EsicMode = x.InputSnapshot.EsicMode,
                    EsicWageCeiling = x.InputSnapshot.EsicWageCeiling,
                    EsicEmployeeRatePercent = x.InputSnapshot.EsicEmployeeRatePercent,
                    EsicEmployerRatePercent = x.InputSnapshot.EsicEmployerRatePercent,
                    TdsMode = x.InputSnapshot.TdsMode,
                    RequireAttendanceLock = x.InputSnapshot.RequireAttendanceLock,
                    Components = x.InputSnapshot.Components.Select(MapComponent).ToList(),
                },
                StatutorySnapshot = new PayrollStatutorySnapshotDto
                {
                    PolicyProfileId = x.StatutorySnapshot.PolicyProfileId,
                    PolicyProfileVersion = x.StatutorySnapshot.PolicyProfileVersion,
                    EmployeeEpf = x.StatutorySnapshot.EmployeeEpf,
                    EmployerEpf = x.StatutorySnapshot.EmployerEpf,
                    EmployeeEsic = x.StatutorySnapshot.EmployeeEsic,
                    EmployerEsic = x.StatutorySnapshot.EmployerEsic,
                    IncomeTax = x.StatutorySnapshot.IncomeTax,
                    ProfessionalTax = x.StatutorySnapshot.ProfessionalTax,
                },
                AttendanceSnapshot = new PayrollAttendanceSnapshotDto
                {
                    SummaryId = x.AttendanceSnapshot.SummaryId,
                    SummaryVersion = x.AttendanceSnapshot.SummaryVersion,
                    IsApproved = x.AttendanceSnapshot.IsApproved,
                    IsLocked = x.AttendanceSnapshot.IsLocked,
                    ApprovedBy = x.AttendanceSnapshot.ApprovedBy,
                    ApprovedAt = x.AttendanceSnapshot.ApprovedAt,
                    LockedAt = x.AttendanceSnapshot.LockedAt,
                    LockedBy = x.AttendanceSnapshot.LockedBy,
                    ExpectedWorkingDays = x.AttendanceSnapshot.ExpectedWorkingDays,
                    EligibleWorkingDays = x.AttendanceSnapshot.EligibleWorkingDays,
                    PresentDays = x.AttendanceSnapshot.PresentDays,
                    WfhDays = x.AttendanceSnapshot.WfhDays,
                    PaidLeaveDays = x.AttendanceSnapshot.PaidLeaveDays,
                    UnpaidLeaveDays = x.AttendanceSnapshot.UnpaidLeaveDays,
                    HalfDays = x.AttendanceSnapshot.HalfDays,
                    AbsentDays = x.AttendanceSnapshot.AbsentDays,
                    PaidDays = x.AttendanceSnapshot.PaidDays,
                    UnpaidDays = x.AttendanceSnapshot.UnpaidDays,
                    LhdCount = x.AttendanceSnapshot.LhdCount,
                    EdCount = x.AttendanceSnapshot.EdCount,
                    CombinedLhdEdCount = x.AttendanceSnapshot.CombinedLhdEdCount,
                    LossOfPayDays = x.AttendanceSnapshot.LossOfPayDays,
                    LossOfPayAmount = x.AttendanceSnapshot.LossOfPayAmount,
                    Divisor = x.AttendanceSnapshot.Divisor,
                    DivisorPolicy = x.AttendanceSnapshot.DivisorPolicy,
                },
            })
            .ToList();
        return new Result<List<PayrollRunEmployeeDto>>
        {
            Success = true,
            MethodResult = rows,
            TotalRecords = rows.Count,
        };
    }

    private static PayrollEmployeeInputSnapshot BuildSnapshot(
        EmployeePayrollAssignment? assignment,
        SalaryTemplate? template,
        PayrollPayGroup? payGroup,
        PayrollPolicyProfile? policy,
        EmployeeSalaryStructure? employeeSalary,
        IReadOnlyCollection<EmployeeSalaryStructure> overlappingSalaries,
        IReadOnlyCollection<SalaryModel> legacySalaries,
        PayrollDivisorPolicy? companyPolicy
    )
    {
        var components =
            employeeSalary is not null
                ? employeeSalary
                    .Components.Where(x => x.IsRecurring)
                    .Select(
                        (x, index) =>
                            new PayrollComponentSnapshot
                            {
                                Code = x.ComponentCode,
                                Name = x.ComponentName,
                                Kind = x.Kind,
                                CalculationType = x.CalculationType,
                                BaseComponentCode = x.BaseComponentCode,
                                Value = x.Percentage ?? x.Value,
                                IsTaxable = x.IsTaxable,
                                IsRecurring = x.IsRecurring,
                                IsStatutory = x.IsStatutory,
                                IsEmployerContribution = x.IsEmployerContribution,
                                SortOrder = index,
                            }
                    )
                    .ToList()
            : template is null ? []
            : template.Components?.Count > 0
                ? template
                    .Components.OrderBy(x => x.SortOrder)
                    .Select(x => new PayrollComponentSnapshot
                    {
                        Code = x.Code,
                        Name = x.Name,
                        Kind = x.Kind,
                        CalculationType = x.CalculationType,
                        BaseComponentCode = x.BaseComponentCode,
                        Value = x.Value,
                        IsTaxable = x.IsTaxable,
                        SortOrder = x.SortOrder,
                    })
                    .ToList()
            : LegacyComponents(template);

        return new PayrollEmployeeInputSnapshot
        {
            AssignmentId = assignment?.Id,
            AssignmentVersion = assignment?.Version ?? 0,
            SalaryStructureId =
                employeeSalary?.Id ?? legacySalaries.FirstOrDefault()?.SalaryId.ToString("D"),
            SalaryStructureVersion = employeeSalary?.Version ?? 0,
            SalaryStructures =
                overlappingSalaries.Count > 0
                    ? overlappingSalaries
                        .Select(x => new PayrollSalaryStructureSnapshot
                        {
                            SalaryStructureId = x.Id,
                            Version = x.Version,
                            EffectiveFrom = x.EffectiveFrom,
                            PayGroupId = x.PayGroupId,
                            EffectiveTo = x.EffectiveTo,
                            Components = x
                                .Components.Select(
                                    (c, index) =>
                                        new PayrollComponentSnapshot
                                        {
                                            Code = c.ComponentCode,
                                            Name = c.ComponentName,
                                            Kind = c.Kind,
                                            CalculationType = c.CalculationType,
                                            BaseComponentCode = c.BaseComponentCode,
                                            Value = c.Percentage ?? c.Value,
                                            IsTaxable = c.IsTaxable,
                                            IsRecurring = c.IsRecurring,
                                            IsStatutory = c.IsStatutory,
                                            IsEmployerContribution = c.IsEmployerContribution,
                                            SortOrder = index,
                                        }
                                )
                                .ToList(),
                        })
                        .ToList()
                    : legacySalaries
                        .Select(PayrollSalarySourceResolver.SnapshotLegacyRecord)
                        .ToList(),
            SalaryTemplateId = template?.Id,
            SalaryTemplateVersion = template?.Version ?? 0,
            PayGroupId = assignment?.PayGroupId ?? payGroup?.Id,
            PayGroupVersion = payGroup?.Version ?? 0,
            PolicyProfileId = policy?.Id,
            PolicyProfileVersion = policy?.Version ?? 0,
            EpfMode = !string.IsNullOrWhiteSpace(employeeSalary?.EpfModeOverride)
                ? employeeSalary.EpfModeOverride
                : (policy?.EpfMode ?? companyPolicy?.EpfMode ?? "CAPPED_CEILING"),
            EpfWageCeiling = policy?.EpfWageCeiling ?? companyPolicy?.EpfWageCeiling ?? 15000m,
            EpfEmployeeRatePercent =
                policy?.EpfEmployeeRatePercent ?? companyPolicy?.EpfEmployeeRatePercent ?? 12m,
            EpfEmployerRatePercent =
                policy?.EpfEmployerRatePercent ?? companyPolicy?.EpfEmployerRatePercent ?? 12m,
            EsicMode = !string.IsNullOrWhiteSpace(employeeSalary?.EsicModeOverride)
                ? employeeSalary.EsicModeOverride
                : (policy?.EsicMode ?? companyPolicy?.EsicMode ?? "STATUTORY"),
            EsicWageCeiling = policy?.EsicWageCeiling ?? companyPolicy?.EsicWageCeiling ?? 21000m,
            EsicEmployeeRatePercent =
                policy?.EsicEmployeeRatePercent ?? companyPolicy?.EsicEmployeeRatePercent ?? 0.75m,
            EsicEmployerRatePercent =
                policy?.EsicEmployerRatePercent ?? companyPolicy?.EsicEmployerRatePercent ?? 3.25m,
            TdsMode = !string.IsNullOrWhiteSpace(employeeSalary?.TaxRegime)
                ? employeeSalary.TaxRegime
                : (policy?.TdsMode ?? companyPolicy?.TdsMode ?? "AUTO_NEW_REGIME"),
            RequireAttendanceLock =
                policy?.RequireAttendanceLock ?? companyPolicy?.RequireAttendanceLock,
            Components = components,
        };
    }

    private static PayrollComponentSnapshotDto MapComponent(PayrollComponentSnapshot c) =>
        new()
        {
            Code = c.Code,
            Name = c.Name,
            Kind = c.Kind,
            CalculationType = c.CalculationType,
            BaseComponentCode = c.BaseComponentCode,
            Value = c.Value,
            IsTaxable = c.IsTaxable,
            IsRecurring = c.IsRecurring,
            IsStatutory = c.IsStatutory,
            IsEmployerContribution = c.IsEmployerContribution,
            SortOrder = c.SortOrder,
        };

    private static List<PayrollComponentSnapshot> LegacyComponents(SalaryTemplate? template) =>
        [
            new()
            {
                Code = "BASIC_PAY",
                Name = "Basic Pay",
                Kind = "EARNING",
                Value = template?.BasicPay ?? 0,
                IsTaxable = true,
                SortOrder = 10,
            },
            new()
            {
                Code = "HRA",
                Name = "House Rent Allowance",
                Kind = "EARNING",
                Value = template?.Hra ?? 0,
                IsTaxable = true,
                SortOrder = 20,
            },
            new()
            {
                Code = "LTA",
                Name = "Leave Travel Allowance",
                Kind = "EARNING",
                Value = template?.Lta ?? 0,
                IsTaxable = true,
                SortOrder = 30,
            },
            new()
            {
                Code = "OTHER_ALLOWANCE",
                Name = "Other Allowance",
                Kind = "EARNING",
                Value = template?.OtherAllowance ?? 0,
                IsTaxable = true,
                SortOrder = 40,
            },
            new()
            {
                Code = "BONUS",
                Name = "Bonus",
                Kind = "EARNING",
                Value = template?.Bonus ?? 0,
                IsTaxable = true,
                SortOrder = 50,
            },
        ];

    private static PayrollRunDto Map(PayrollRun x) =>
        new()
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            PeriodStart = x.PeriodStart,
            PeriodEnd = x.PeriodEnd,
            PayDate = x.PayDate,
            RunType = x.RunType,
            ParentRunId = x.ParentRunId,
            Status = x.Status,
            PopulationType = x.PopulationType,
            DepartmentId = x.DepartmentId,
            PayGroupId = x.PayGroupId,
            IdempotencyKey = x.IdempotencyKey,
            EmployeeCount = x.EmployeeCount,
            ReadyCount = x.ReadyCount,
            BlockedCount = x.BlockedCount,
            SkippedCount = x.SkippedCount,
            GrossTotal = x.GrossTotal,
            DeductionTotal = x.DeductionTotal,
            NetTotal = x.NetTotal,
            LegacyPayrollRowsSynchronizedAtUtc = x.LegacyPayrollRowsSynchronizedAtUtc,
            CreatedBy = x.CreatedBy,
            CreatedAtUtc = x.CreatedAtUtc,
        };

    private async Task<Result<PayrollRunDto>> CancelIncompleteRun(
        string companyId,
        string actorId,
        PayrollRun run,
        Result linePersistence
    )
    {
        var employeeLineFilter =
            Builders<PayrollRunEmployee>.Filter.Eq(x => x.CompanyId, companyId)
            & Builders<PayrollRunEmployee>.Filter.Eq(x => x.PayrollRunId, run.Id);
        Result cleanup;
        try
        {
            cleanup = await runEmployees.DeleteAll(employeeLineFilter);
        }
        catch
        {
            cleanup = new Result { Success = false, StatusCode = 500 };
        }

        var expectedVersion = run.Version;
        run.Status = "CANCELLED";
        run.IsActive = false;
        run.Version++;
        run.AuditHistory.Add(
            new PayrollRunAuditEvent
            {
                Action = "RunCreationFailed",
                ActorId = actorId,
                OccurredAtUtc = DateTime.UtcNow,
                Notes = cleanup.Success
                    ? "Employee payroll lines could not be persisted; incomplete lines were removed and the draft run was cancelled."
                    : "Employee payroll lines could not be persisted or fully removed; the run was cancelled and partial lines may require cleanup.",
            }
        );

        var cancellation = await runs.Update(
            Builders<PayrollRun>.Filter.Eq(x => x.CompanyId, companyId)
                & Builders<PayrollRun>.Filter.Eq(x => x.Id, run.Id)
                & Builders<PayrollRun>.Filter.Eq(x => x.Status, "DRAFT")
                & Builders<PayrollRun>.Filter.Eq(x => x.Version, expectedVersion),
            run
        );
        if (!cancellation.Success)
            return Fail(
                500,
                $"Payroll run employee lines could not be saved and the run could not be safely cancelled. Run ID: {run.Id}."
            );

        if (!cleanup.Success)
            return Fail(
                500,
                $"Payroll run was cancelled, but partial employee lines could not be cleaned up. Contact a payroll administrator before retrying. Run ID: {run.Id}."
            );

        var statusCode = linePersistence.StatusCode >= 400 ? linePersistence.StatusCode : 500;
        return Fail(
            statusCode,
            $"Payroll run creation failed while saving employee lines. The draft was cancelled; refresh and retry with a new idempotency key. Run ID: {run.Id}."
        );
    }

    private static bool IsEligibleForPayrollPeriod(EmpUser employee, DateTime start, DateTime end)
    {
        if (
            !EmploymentPeriod.IsPayrollIncludedForMonth(employee, start)
            || !EmploymentPeriod.TryGetJoiningDate(employee, out var joiningDate)
            || joiningDate.Date > end.Date
        )
            return false;

        var lastWorkingDate = EmploymentPeriod.GetLastWorkingDate(employee);
        return !lastWorkingDate.HasValue || lastWorkingDate.Value.Date >= start.Date;
    }

    private async Task<bool> MatchesCreateRequest(
        string companyId,
        PayrollRun existing,
        DateTime start,
        DateTime end,
        DateTime payDate,
        string runType,
        string populationType,
        string? departmentId,
        string? payGroupId,
        IReadOnlyCollection<string?> selectedEmployeeIds,
        string requestFingerprint
    )
    {
        if (!string.IsNullOrWhiteSpace(existing.RequestFingerprint))
            return string.Equals(
                existing.RequestFingerprint,
                requestFingerprint,
                StringComparison.Ordinal
            );
        if (
            existing.PeriodStart.Date != start.Date
            || existing.PeriodEnd.Date != end.Date
            || existing.PayDate.Date != payDate.Date
            || !string.Equals(existing.RunType, runType, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                existing.PopulationType,
                populationType,
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(existing.DepartmentId, departmentId, StringComparison.Ordinal)
            || !string.Equals(existing.PayGroupId, payGroupId, StringComparison.Ordinal)
        )
            return false;

        if (!string.Equals(populationType, "SELECTED", StringComparison.OrdinalIgnoreCase))
            return true;

        var savedEmployeeIds = (
            await runEmployees.GetAll(x =>
                x.CompanyId == companyId && x.PayrollRunId == existing.Id
            )
        )
            .Select(x => x.EmployeeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return savedEmployeeIds.Count == existing.EmployeeCount
            && savedEmployeeIds.Count == selectedEmployeeIds.Count
            && selectedEmployeeIds.All(id => id is not null && savedEmployeeIds.Contains(id));
    }

    private static string CreateRequestFingerprint(
        DateTime start,
        DateTime end,
        DateTime payDate,
        string runType,
        string populationType,
        string? departmentId,
        string? payGroupId,
        IReadOnlyCollection<string?> selectedEmployeeIds
    )
    {
        var payload = JsonSerializer.Serialize(
            new
            {
                PeriodStart = start.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                PeriodEnd = end.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                PayDate = payDate.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                RunType = runType,
                PopulationType = populationType,
                DepartmentId = departmentId,
                PayGroupId = payGroupId,
                EmployeeIds = string.Equals(
                    populationType,
                    "SELECTED",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? selectedEmployeeIds
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                        .Select(id => id?.ToUpperInvariant())
                        .ToArray()
                    : Array.Empty<string?>(),
            }
        );
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static Result<PayrollRunDto> Ok(PayrollRun x) =>
        new()
        {
            Success = true,
            MethodResult = Map(x),
            ResourceId = x.Id,
            Message = "Existing payroll run returned.",
        };

    private static Result<PayrollRunDto> Fail(int statusCode, string message) =>
        new()
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
        };

    private static bool IsUniqueIndexConflict(string? message, string indexName) =>
        !string.IsNullOrEmpty(message)
        && message.Contains("E11000", StringComparison.Ordinal)
        && message.Contains(indexName, StringComparison.Ordinal);
}
