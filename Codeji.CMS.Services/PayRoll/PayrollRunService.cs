using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public interface IPayrollRunService
{
    Task<Result<PayrollRunDto>> Create(string companyId, string actorId, CreatePayrollRunRequestDto request);
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
    IMongoDbRepository<PayrollPolicyProfile>? policyProfiles = null) : IPayrollRunService
{
    public async Task<Result<PayrollRunDto>> Create(string companyId, string actorId, CreatePayrollRunRequestDto request)
    {
        var start = new DateTime(request.PeriodStart.Year, request.PeriodStart.Month, 1);
        var end = new DateTime(request.PeriodEnd.Year, request.PeriodEnd.Month,
            DateTime.DaysInMonth(request.PeriodEnd.Year, request.PeriodEnd.Month));
        if (request.PeriodStart.Date != start || request.PeriodEnd.Date != end || end < start || request.PayDate == default)
            return Fail(400, "Valid period boundaries and payday are required.");

        var type = (request.RunType ?? "REGULAR").Trim().ToUpperInvariant();
        var population = (request.PopulationType ?? "ALL_ELIGIBLE").Trim().ToUpperInvariant();
        if (type is not ("REGULAR" or "OFF_CYCLE" or "SUPPLEMENTAL" or "FINAL_SETTLEMENT" or "CORRECTION" or "REVERSAL"))
            return Fail(400, "Unsupported payroll run type.");
        if (population == "SELECTED" && request.EmployeeIds.Count == 0)
            return Fail(400, "Selected population requires employees.");

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var old = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.IdempotencyKey == request.IdempotencyKey);
            if (old is not null) return Ok(old);
        }

        var existing = await runs.Exist(x => x.CompanyId == companyId && x.PeriodStart == start &&
            x.RunType == type && x.PopulationType == population && x.DepartmentId == request.DepartmentId &&
            x.PayGroupId == request.PayGroupId && x.Status != "CANCELLED");
        if (existing) return Fail(409, "An equivalent payroll run already exists.");

        var populationEmployees = (await employees.GetAll(x => x.CompanyId == companyId && !x.IsDeleted &&
            (request.EmployeeIds.Count == 0 || request.EmployeeIds.Contains(x.EmployeeId)) &&
            (string.IsNullOrWhiteSpace(request.DepartmentId) || x.Department == request.DepartmentId))).ToList();

        var divisorPolicy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.IsActive &&
            x.EffectiveFrom <= end && (!x.EffectiveTo.HasValue || x.EffectiveTo >= start));
        IEnumerable<PayrollPolicyProfile> configuredPolicy = policyProfiles is null
            ? Array.Empty<PayrollPolicyProfile>()
            : (await policyProfiles.GetAll(x => x.CompanyId == companyId && x.IsActive)).ToList();

        var run = new PayrollRun
        {
            CompanyId = companyId, PeriodStart = start, PeriodEnd = end, PayDate = request.PayDate.Date,
            RunType = type, PopulationType = population, DepartmentId = request.DepartmentId,
            PayGroupId = request.PayGroupId, IdempotencyKey = request.IdempotencyKey,
            EmployeeCount = populationEmployees.Count, ReadyCount = populationEmployees.Count,
            CreatedBy = actorId, CreatedAtUtc = DateTime.UtcNow, Status = "DRAFT",
            ConfigurationSnapshot = new PayrollConfigurationSnapshot
            {
                DivisorPolicyId = divisorPolicy?.Id,
                DivisorPolicyVersion = divisorPolicy?.Version ?? 0,
                DivisorPolicy = divisorPolicy?.DivisorPolicy ?? "CALENDAR_DAYS",
                LopCalculationBase = divisorPolicy?.LopCalculationBase ?? "FIXED_GROSS",
                EpfMode = divisorPolicy?.EpfMode ?? "CAPPED_CEILING",
                EpfWageCeiling = divisorPolicy?.EpfWageCeiling ?? 0,
                EsicMode = divisorPolicy?.EsicMode ?? "STATUTORY",
                TdsMode = divisorPolicy?.TdsMode ?? "AUTO_NEW_REGIME",
                RequireAttendanceLock = divisorPolicy?.RequireAttendanceLock ?? true,
                CapturedAtUtc = DateTime.UtcNow
            }
        };

        var saved = await runs.AddOne(run);
        if (!saved.Success) return new Result<PayrollRunDto> { Success = false, StatusCode = saved.StatusCode, Message = saved.Message };

        var assignmentRows = assignments is null ? [] : (await assignments.GetAll(x => x.CompanyId == companyId)).ToList();
        var templateRows = templates is null ? [] : (await templates.GetAll(x => x.CompanyId == companyId)).ToList();
        var payGroupRows = payGroups is null ? [] : (await payGroups.GetAll(x => x.CompanyId == companyId)).ToList();

        var lines = populationEmployees.Select(employee =>
        {
            var assignment = PayrollPolicyResolver.ResolveAssignment(companyId, employee.EmployeeId, start, assignmentRows);
            var resolvedPolicy = PayrollPolicyResolver.ResolvePolicy(companyId, start, assignment, configuredPolicy, payGroupRows);
            var template = assignment?.SalaryTemplateId is null
                ? null
                : templateRows.FirstOrDefault(x => x.CompanyId == companyId && x.Id == assignment.SalaryTemplateId &&
                    x.IsActive && x.EffectiveFrom.Date <= start.Date &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= start.Date));
            var group = assignment?.PayGroupId is null
                ? null
                : payGroupRows.FirstOrDefault(x => x.CompanyId == companyId && x.Id == assignment.PayGroupId);
            return new PayrollRunEmployee
            {
                CompanyId = companyId, PayrollRunId = run.Id, EmployeeId = employee.EmployeeId,
                Status = "READY", InclusionReason = "Population rule matched",
                PayGroupId = assignment?.PayGroupId ?? request.PayGroupId, DepartmentId = employee.Department,
                InputSnapshot = BuildSnapshot(assignment, template, group, resolvedPolicy)
            };
        }).ToList();

        if (lines.Count > 0) await runEmployees.AddMany(lines);
        return new Result<PayrollRunDto>
        {
            Success = true, StatusCode = 201, Message = "Payroll run created.",
            ResourceId = run.Id, MethodResult = Map(run)
        };
    }

    public async Task<List<PayrollRunDto>> List(string companyId) =>
        (await runs.GetAll(x => x.CompanyId == companyId)).OrderByDescending(x => x.CreatedAtUtc).Select(Map).ToList();

    public async Task<Result<PayrollRunDto>> Get(string companyId, string id)
    {
        var run = await runs.FirstOrDefault(x => x.CompanyId == companyId && x.Id == id);
        return run is null ? new Result<PayrollRunDto> { Success = false, StatusCode = 404, Message = "Payroll run was not found." } :
            new Result<PayrollRunDto> { Success = true, MethodResult = Map(run) };
    }

    public async Task<Result<List<PayrollRunEmployeeDto>>> GetEmployees(string companyId, string id)
    {
        if (!await runs.Exist(x => x.CompanyId == companyId && x.Id == id))
            return new Result<List<PayrollRunEmployeeDto>> { Success = false, StatusCode = 404, Message = "Payroll run was not found." };
        var rows = (await runEmployees.GetAll(x => x.CompanyId == companyId && x.PayrollRunId == id))
            .Select(x => new PayrollRunEmployeeDto
            {
                EmployeeId = x.EmployeeId, Status = x.Status, InclusionReason = x.InclusionReason,
                ExclusionReason = x.ExclusionReason, ErrorMessage = x.ErrorMessage,
                InputSnapshot = new PayrollEmployeeInputSnapshotDto
                {
                    AssignmentId = x.InputSnapshot.AssignmentId, AssignmentVersion = x.InputSnapshot.AssignmentVersion,
                    SalaryTemplateId = x.InputSnapshot.SalaryTemplateId, SalaryTemplateVersion = x.InputSnapshot.SalaryTemplateVersion,
                    PayGroupId = x.InputSnapshot.PayGroupId, PayGroupVersion = x.InputSnapshot.PayGroupVersion,
                    PolicyProfileId = x.InputSnapshot.PolicyProfileId, PolicyProfileVersion = x.InputSnapshot.PolicyProfileVersion,
                    Components = x.InputSnapshot.Components.Select(c => new PayrollComponentSnapshotDto
                    {
                        Code = c.Code, Name = c.Name, Kind = c.Kind, CalculationType = c.CalculationType,
                        BaseComponentCode = c.BaseComponentCode, Value = c.Value, IsTaxable = c.IsTaxable, SortOrder = c.SortOrder
                    }).ToList()
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
                    ProfessionalTax = x.StatutorySnapshot.ProfessionalTax
                }
            }).ToList();
        return new Result<List<PayrollRunEmployeeDto>> { Success = true, MethodResult = rows, TotalRecords = rows.Count };
    }

    private static PayrollEmployeeInputSnapshot BuildSnapshot(
        EmployeePayrollAssignment? assignment,
        SalaryTemplate? template,
        PayrollPayGroup? payGroup,
        PayrollPolicyProfile? policy)
    {
        var components = template is null
            ? []
            : template.Components?.Count > 0
            ? template.Components.OrderBy(x => x.SortOrder).Select(x => new PayrollComponentSnapshot
            {
                Code = x.Code, Name = x.Name, Kind = x.Kind, CalculationType = x.CalculationType,
                BaseComponentCode = x.BaseComponentCode, Value = x.Value, IsTaxable = x.IsTaxable, SortOrder = x.SortOrder
            }).ToList()
            : LegacyComponents(template);

        return new PayrollEmployeeInputSnapshot
        {
            AssignmentId = assignment?.Id, AssignmentVersion = assignment is null ? 0 : 1,
            SalaryTemplateId = template?.Id, SalaryTemplateVersion = template?.Version ?? 0,
            PayGroupId = assignment?.PayGroupId ?? payGroup?.Id, PayGroupVersion = payGroup?.Version ?? 0,
            PolicyProfileId = policy?.Id, PolicyProfileVersion = policy?.Version ?? 0,
            Components = components
        };
    }

    private static List<PayrollComponentSnapshot> LegacyComponents(SalaryTemplate? template) =>
    [
        new() { Code = "BASIC_PAY", Name = "Basic Pay", Kind = "EARNING", Value = template?.BasicPay ?? 0, IsTaxable = true, SortOrder = 10 },
        new() { Code = "HRA", Name = "House Rent Allowance", Kind = "EARNING", Value = template?.Hra ?? 0, IsTaxable = true, SortOrder = 20 },
        new() { Code = "LTA", Name = "Leave Travel Allowance", Kind = "EARNING", Value = template?.Lta ?? 0, IsTaxable = true, SortOrder = 30 },
        new() { Code = "OTHER_ALLOWANCE", Name = "Other Allowance", Kind = "EARNING", Value = template?.OtherAllowance ?? 0, IsTaxable = true, SortOrder = 40 },
        new() { Code = "BONUS", Name = "Bonus", Kind = "EARNING", Value = template?.Bonus ?? 0, IsTaxable = true, SortOrder = 50 }
    ];

    private static PayrollRunDto Map(PayrollRun x) => new()
    {
        Id = x.Id, CompanyId = x.CompanyId, PeriodStart = x.PeriodStart, PeriodEnd = x.PeriodEnd,
        PayDate = x.PayDate, RunType = x.RunType, Status = x.Status, PopulationType = x.PopulationType,
        DepartmentId = x.DepartmentId, PayGroupId = x.PayGroupId, IdempotencyKey = x.IdempotencyKey,
        EmployeeCount = x.EmployeeCount, ReadyCount = x.ReadyCount, BlockedCount = x.BlockedCount,
        SkippedCount = x.SkippedCount, GrossTotal = x.GrossTotal, DeductionTotal = x.DeductionTotal,
        NetTotal = x.NetTotal, CreatedBy = x.CreatedBy, CreatedAtUtc = x.CreatedAtUtc
    };

    private static Result<PayrollRunDto> Ok(PayrollRun x) =>
        new() { Success = true, MethodResult = Map(x), ResourceId = x.Id, Message = "Existing payroll run returned." };

    private static Result<PayrollRunDto> Fail(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };
}






