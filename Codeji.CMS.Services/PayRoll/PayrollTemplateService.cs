using System.Text.RegularExpressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.PayRoll;

public interface IPayrollTemplateService
{
    Task<List<PayrollSalaryComponentDto>> GetComponents(string companyId);
    Task<List<PayrollPolicyProfileDto>> GetPolicies(string companyId);
    Task<List<SalaryTemplateDto>> GetTemplates(string companyId);
    Task<Result> SaveComponent(string companyId, string actorUserId, PayrollSalaryComponentDto dto);
    Task<Result> SavePolicy(string companyId, string actorUserId, PayrollPolicyProfileDto dto);
    Task<Result> SubmitPolicy(string companyId, string actorUserId, string policyId, int version);
    Task<Result> ApprovePolicy(string companyId, string actorUserId, string policyId, int version);
    Task<Result> SaveTemplate(string companyId, string actorUserId, SalaryTemplateDto dto);
}

public sealed class PayrollTemplateService(
    IMongoDbRepository<PayrollPolicyProfile> policies,
    IMongoDbRepository<SalaryTemplate> templates,
    IMongoDbRepository<PayrollPayGroup> payGroups,
    IMongoDbRepository<PayrollSalaryComponentDefinition>? componentDefinitions = null) : IPayrollTemplateService
{
    public async Task<List<PayrollSalaryComponentDto>> GetComponents(string companyId)
    {
        if (componentDefinitions is null) return [];
        return (await componentDefinitions.GetAll(x => x.CompanyId == companyId))
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.Code)
            .Select(Map).ToList();
    }

    public async Task<List<PayrollPolicyProfileDto>> GetPolicies(string companyId) =>
        (await policies.GetAll(x => x.CompanyId == companyId))
        .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Version)
        .Select(x => new PayrollPolicyProfileDto
        {
            Id = x.Id, Name = x.Name, Version = x.Version, DivisorPolicy = x.DivisorPolicy, Scope = x.Scope,
            RequireAttendanceLock = x.RequireAttendanceLock, EpfMode = x.EpfMode, EpfWageCeiling = x.EpfWageCeiling, EsicMode = x.EsicMode, EsicWageCeiling = x.EsicWageCeiling, TdsMode = x.TdsMode, StatutoryJurisdiction = x.StatutoryJurisdiction, FinancialYear = x.FinancialYear, StatutoryRuleVersion = x.StatutoryRuleVersion, ApprovalStatus = x.ApprovalStatus, CreatedBy = x.CreatedBy, SubmittedBy = x.SubmittedBy, SubmittedAtUtc = x.SubmittedAtUtc, ApprovedBy = x.ApprovedBy, ApprovedAtUtc = x.ApprovedAtUtc, EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo, IsActive = x.IsActive
        }).ToList();

    public async Task<List<SalaryTemplateDto>> GetTemplates(string companyId) =>
        (await templates.GetAll(x => x.CompanyId == companyId))
        .OrderByDescending(x => x.EffectiveFrom).ThenBy(x => x.Name)
        .Select(x => new SalaryTemplateDto
        {
            Id = x.Id, Name = x.Name, PayGroupId = x.PayGroupId, DepartmentId = x.DepartmentId,
            EmployeeType = x.EmployeeType, GradeId = x.GradeId, EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo, BasicPay = x.BasicPay, Hra = x.Hra, Lta = x.Lta,
            OtherAllowance = x.OtherAllowance, Bonus = x.Bonus, IsActive = x.IsActive, Version = x.Version,
            Components = x.Components.OrderBy(c => c.SortOrder).Select(c => new PayrollSalaryTemplateComponentDto
            {
                ComponentId = c.ComponentId, Code = c.Code, Name = c.Name, Kind = c.Kind,
                CalculationType = c.CalculationType, BaseComponentCode = c.BaseComponentCode,
                Value = c.Value, IsTaxable = c.IsTaxable, SortOrder = c.SortOrder
            }).ToList()
        }).ToList();

    public async Task<Result> SaveComponent(string companyId, string actorUserId, PayrollSalaryComponentDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var kind = dto.Kind.Trim().ToUpperInvariant();
        var calculationType = dto.CalculationType.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || !Regex.IsMatch(code, "^[A-Z][A-Z0-9_]{1,49}$") ||
            string.IsNullOrWhiteSpace(dto.Name) || dto.EffectiveFrom == default ||
            (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return InvalidDates("Component code, name and valid effective dates are required.");
        if (kind is not ("EARNING" or "DEDUCTION") ||
            calculationType is not ("FIXED" or "PERCENT_OF_GROSS" or "PERCENT_OF_COMPONENT"))
            return InvalidDates("Unsupported component kind or calculation type.");
        if (componentDefinitions is null) return new Result { Success = false, StatusCode = 503, Message = "Payroll component storage is unavailable." };
        if (await componentDefinitions.Exist(x => x.CompanyId == companyId && x.Code == code && x.Id != dto.Id && x.IsActive))
            return new Result { Success = false, StatusCode = 409, Message = "A payroll component with this code already exists." };

        var entity = new PayrollSalaryComponentDefinition
        {
            Id = dto.Id ?? string.Empty, CompanyId = companyId, Code = code, Name = dto.Name.Trim(),
            Kind = kind, CalculationType = calculationType, BaseComponentCode = dto.BaseComponentCode?.Trim().ToUpperInvariant(),
            IsTaxable = dto.IsTaxable, EffectiveFrom = dto.EffectiveFrom.Date, EffectiveTo = dto.EffectiveTo?.Date,
            IsActive = dto.IsActive, Version = Math.Max(1, dto.Version)
        };
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            entity.Version++;
            return await componentDefinitions.Update(
                Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.Id, dto.Id) &
                Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.CompanyId, companyId) &
                (dto.Version > 0 ? Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.Version, dto.Version) : Builders<PayrollSalaryComponentDefinition>.Filter.Empty), entity);
        }
        return await componentDefinitions.AddOne(entity);
    }

    public async Task<Result> SavePolicy(string companyId, string actorUserId, PayrollPolicyProfileDto dto)
    {
        var divisorPolicy = dto.DivisorPolicy.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.EffectiveFrom == default ||
            (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return InvalidDates();
        if (dto.EffectiveFrom.Day != 1 ||
            (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date !=
                new DateTime(dto.EffectiveTo.Value.Year, dto.EffectiveTo.Value.Month, DateTime.DaysInMonth(dto.EffectiveTo.Value.Year, dto.EffectiveTo.Value.Month))))
            return new Result { Success = false, StatusCode = 400, Message = "Payroll policy dates must cover whole months: start on the first day and end on the last day of a month." };
        var epfMode = dto.EpfMode.Trim().ToUpperInvariant();
        var esicMode = dto.EsicMode.Trim().ToUpperInvariant();
        var tdsMode = dto.TdsMode.Trim().ToUpperInvariant();
        var scope = dto.Scope.Trim().ToUpperInvariant();
        if (!new[] { "CALENDAR_DAYS", "WORKING_DAYS", "FIXED_30_DAYS" }.Contains(divisorPolicy) ||
            !new[] { "COMPANY_DEFAULT", "ASSIGNMENT_ONLY" }.Contains(scope) ||
            !new[] { "EXEMPT", "CAPPED_CEILING", "ACTUAL_BASIC_12" }.Contains(epfMode) ||
            !new[] { "EXEMPT", "STATUTORY" }.Contains(esicMode) ||
            !new[] { "AUTO_NEW_REGIME", "AUTO_OLD_REGIME", "MANUAL_ONLY" }.Contains(tdsMode) ||
            dto.EpfWageCeiling < 0 || dto.EsicWageCeiling < 0 ||
            dto.IsActive && RequiresStatutoryReview(epfMode, esicMode, tdsMode) &&
            (string.IsNullOrWhiteSpace(dto.StatutoryJurisdiction) || string.IsNullOrWhiteSpace(dto.FinancialYear) || string.IsNullOrWhiteSpace(dto.StatutoryRuleVersion)))
            return new Result { Success = false, StatusCode = 400, Message = "Invalid payroll or statutory policy settings. Active statutory policies require jurisdiction, financial year, and rule version." };
        PayrollPolicyProfile? existingDraft = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            existingDraft = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == dto.Id);
            if (existingDraft is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll policy was not found." };
            if (existingDraft.Version != dto.Version) return new Result { Success = false, StatusCode = 409, Message = "Payroll policy changed; reload and retry." };
            if (existingDraft.ApprovalStatus != "DRAFT")
                return new Result { Success = false, StatusCode = 409, Message = "Submitted or approved payroll policy versions are immutable. Create a new effective-dated version for changes." };
        }
        // Profiles may overlap because an employee or pay group can intentionally use a
        // different approved profile from the company default. Effective dates select the
        // newest company default, while explicit assignments remain pinned to their profile.
        var duplicate = await policies.Exist(x => x.CompanyId == companyId && x.Name == dto.Name.Trim() &&
            x.EffectiveFrom == dto.EffectiveFrom.Date && x.Id != dto.Id);
        if (duplicate) return new Result { Success = false, StatusCode = 409, Message = "A policy with this name and start date already exists. Choose a new effective date or name." };

        var entity = new PayrollPolicyProfile
        {
            Id = dto.Id ?? string.Empty, CompanyId = companyId, Name = dto.Name.Trim(), CreatedBy = existingDraft?.CreatedBy ?? actorUserId,
            Version = Math.Max(1, dto.Version), DivisorPolicy = divisorPolicy, Scope = scope,
            RequireAttendanceLock = dto.RequireAttendanceLock, EpfMode = epfMode, EpfWageCeiling = dto.EpfWageCeiling, EsicMode = esicMode, EsicWageCeiling = dto.EsicWageCeiling, TdsMode = tdsMode, StatutoryJurisdiction = dto.StatutoryJurisdiction.Trim().ToUpperInvariant(), FinancialYear = dto.FinancialYear.Trim(), StatutoryRuleVersion = dto.StatutoryRuleVersion.Trim(), ApprovalStatus = "DRAFT", EffectiveFrom = dto.EffectiveFrom.Date,
            EffectiveTo = dto.EffectiveTo?.Date, IsActive = dto.IsActive
        };
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            entity.Version = existingDraft!.Version + 1;
            return await policies.Update(
                Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Id, dto.Id) &
                Builders<PayrollPolicyProfile>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Version, dto.Version) &
                Builders<PayrollPolicyProfile>.Filter.Eq(x => x.ApprovalStatus, "DRAFT"), entity);
        }
        return await policies.AddOne(entity);
    }

    public async Task<Result> SubmitPolicy(string companyId, string actorUserId, string policyId, int version)
    {
        var policy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == policyId);
        if (policy is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll policy was not found." };
        if (policy.Version != version) return new Result { Success = false, StatusCode = 409, Message = "Payroll policy changed; reload and retry." };
        if (policy.IsActive && RequiresStatutoryReview(policy) && !HasStatutoryReviewMetadata(policy))
            return MissingStatutoryReviewMetadata();
        if (policy.ApprovalStatus is "SUBMITTED" or "APPROVED") return new Result { Success = false, StatusCode = 409, Message = "Payroll policy is already submitted or approved." };
        policy.ApprovalStatus = "SUBMITTED"; policy.SubmittedBy = actorUserId; policy.SubmittedAtUtc = DateTime.UtcNow;
        return await policies.Update(Builders<PayrollPolicyProfile>.Filter.Eq(x => x.CompanyId, companyId) & Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Id, policyId) & Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Version, version), policy);
    }

    public async Task<Result> ApprovePolicy(string companyId, string actorUserId, string policyId, int version)
    {
        var policy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == policyId);
        if (policy is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll policy was not found." };
        if (policy.Version != version) return new Result { Success = false, StatusCode = 409, Message = "Payroll policy changed; reload and retry." };
        if (policy.ApprovalStatus != "SUBMITTED") return new Result { Success = false, StatusCode = 409, Message = "Payroll policy must be submitted before approval." };
        if (policy.IsActive && RequiresStatutoryReview(policy) && !HasStatutoryReviewMetadata(policy))
            return MissingStatutoryReviewMetadata();
        if ((!string.IsNullOrWhiteSpace(policy.CreatedBy) && string.Equals(policy.CreatedBy, actorUserId, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(policy.SubmittedBy) && string.Equals(policy.SubmittedBy, actorUserId, StringComparison.OrdinalIgnoreCase)))
            return new Result { Success = false, StatusCode = 403, Message = "A payroll policy must be approved by someone other than its creator or submitter." };
        policy.ApprovalStatus = "APPROVED"; policy.ApprovedBy = actorUserId; policy.ApprovedAtUtc = DateTime.UtcNow; policy.Version++;
        return await policies.Update(Builders<PayrollPolicyProfile>.Filter.Eq(x => x.CompanyId, companyId) & Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Id, policyId) & Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Version, version) & Builders<PayrollPolicyProfile>.Filter.Eq(x => x.ApprovalStatus, "SUBMITTED"), policy);
    }

    private static bool RequiresStatutoryReview(string epfMode, string esicMode, string tdsMode) =>
        epfMode != "EXEMPT" || esicMode != "EXEMPT" || tdsMode != "MANUAL_ONLY";

    private static bool RequiresStatutoryReview(PayrollPolicyProfile policy) =>
        RequiresStatutoryReview(policy.EpfMode, policy.EsicMode, policy.TdsMode);

    private static bool HasStatutoryReviewMetadata(PayrollPolicyProfile policy) =>
        !string.IsNullOrWhiteSpace(policy.StatutoryJurisdiction) &&
        !string.IsNullOrWhiteSpace(policy.FinancialYear) &&
        !string.IsNullOrWhiteSpace(policy.StatutoryRuleVersion);

    private static Result MissingStatutoryReviewMetadata() => new()
    {
        Success = false,
        StatusCode = 400,
        Message = "Active statutory policies require jurisdiction, financial year, and rule version before submission or approval."
    };

    public async Task<Result> SaveTemplate(string companyId, string actorUserId, SalaryTemplateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.EffectiveFrom == default ||
            (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return InvalidDates();
        if (dto.PayGroupId is not null &&
            !await payGroups.Exist(x => x.CompanyId == companyId && x.Id == dto.PayGroupId && x.IsActive))
            return new Result { Success = false, StatusCode = 404, Message = "Pay Group was not found." };

        var components = new List<PayrollSalaryTemplateComponent>();
        if (dto.Components.Count > 0)
        {
            if (componentDefinitions is null)
                return new Result { Success = false, StatusCode = 503, Message = "Payroll component storage is unavailable." };
            var ids = dto.Components.Select(x => x.ComponentId).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToHashSet();
            if (ids.Count != dto.Components.Count)
                return new Result { Success = false, StatusCode = 400, Message = "Every template component must reference a component definition." };
            var definitions = (await componentDefinitions.GetAll(x => x.CompanyId == companyId && ids.Contains(x.Id) && x.IsActive)).ToDictionary(x => x.Id);
            if (definitions.Count != ids.Count)
                return new Result { Success = false, StatusCode = 404, Message = "One or more salary components were not found." };
            if (dto.Components.Select(x => x.Code.Trim().ToUpperInvariant()).Distinct().Count() != dto.Components.Count)
                return new Result { Success = false, StatusCode = 409, Message = "A salary template cannot contain duplicate component codes." };

            components = dto.Components.Select((item, index) =>
            {
                var definition = definitions[item.ComponentId!];
                return new PayrollSalaryTemplateComponent
                {
                    ComponentId = definition.Id, Code = definition.Code, Name = definition.Name,
                    Kind = definition.Kind, CalculationType = definition.CalculationType,
                    BaseComponentCode = definition.BaseComponentCode, Value = item.Value,
                    IsTaxable = definition.IsTaxable, SortOrder = item.SortOrder == 0 ? index : item.SortOrder
                };
            }).OrderBy(x => x.SortOrder).ToList();
        }

        var entity = new SalaryTemplate
        {
            Id = dto.Id ?? string.Empty, CompanyId = companyId, Name = dto.Name.Trim(), PayGroupId = dto.PayGroupId,
            DepartmentId = dto.DepartmentId, EmployeeType = dto.EmployeeType, GradeId = dto.GradeId,
            EffectiveFrom = dto.EffectiveFrom.Date, EffectiveTo = dto.EffectiveTo?.Date, BasicPay = dto.BasicPay,
            Hra = dto.Hra, Lta = dto.Lta, OtherAllowance = dto.OtherAllowance, Bonus = dto.Bonus,
            Components = components, IsActive = dto.IsActive, Version = Math.Max(1, dto.Version)
        };
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            entity.Version++;
            return await templates.Update(
                Builders<SalaryTemplate>.Filter.Eq(x => x.Id, dto.Id) &
                Builders<SalaryTemplate>.Filter.Eq(x => x.CompanyId, companyId) &
                (dto.Version > 0 ? Builders<SalaryTemplate>.Filter.Eq(x => x.Version, dto.Version) : Builders<SalaryTemplate>.Filter.Empty), entity);
        }
        return await templates.AddOne(entity);
    }

    private static PayrollSalaryComponentDto Map(PayrollSalaryComponentDefinition x) => new()
    {
        Id = x.Id, Code = x.Code, Name = x.Name, Kind = x.Kind, CalculationType = x.CalculationType,
        BaseComponentCode = x.BaseComponentCode, IsTaxable = x.IsTaxable, EffectiveFrom = x.EffectiveFrom,
        EffectiveTo = x.EffectiveTo, IsActive = x.IsActive, Version = x.Version
    };

    private static Result InvalidDates(string message = "Name and valid effective dates are required.") =>
        new() { Success = false, StatusCode = 400, Message = message };
}

