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
    Task<Result> DeletePolicyDraft(string companyId, string policyId, int version);
    Task<Result> CancelPolicySubmission(string companyId, string actorUserId, string policyId, int version);
    Task<Result> RetirePolicy(string companyId, string actorUserId, string policyId, int version);
    Task<Result> SubmitPolicy(string companyId, string actorUserId, string policyId, int version);
    Task<Result> ApprovePolicy(string companyId, string actorUserId, string policyId, int version, bool isCompanyAdministrator = false);
    Task<Result> SaveTemplate(string companyId, string actorUserId, SalaryTemplateDto dto);
    Task<Result> RetireTemplate(string companyId, string actorUserId, string templateId, int version);
    Task<Result> DeleteTemplate(string companyId, string actorUserId, string templateId, int version);
    Task<Result> SeedStandardComponents(string companyId, string actorUserId);
}

public sealed class PayrollTemplateService(
    IMongoDbRepository<PayrollPolicyProfile> policies,
    IMongoDbRepository<SalaryTemplate> templates,
    IMongoDbRepository<PayrollPayGroup> payGroups,
    IMongoDbRepository<PayrollSalaryComponentDefinition>? componentDefinitions = null,
    IMongoDbRepository<EmpPayRoll>? payrolls = null,
    IMongoDbRepository<EmployeePayrollAssignment>? assignments = null) : IPayrollTemplateService
{
    private static readonly string ExemptMode = string.Concat('E', 'X', 'E', 'M', 'P', 'T');

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
            EffectiveTo = x.EffectiveTo, IsActive = x.IsActive,
            EpfEmployeeRatePercent = x.EpfEmployeeRatePercent, EpfEmployerRatePercent = x.EpfEmployerRatePercent,
            EsicEmployeeRatePercent = x.EsicEmployeeRatePercent, EsicEmployerRatePercent = x.EsicEmployerRatePercent
        }).ToList();

    public async Task<List<SalaryTemplateDto>> GetTemplates(string companyId) =>
        (await templates.GetAll(x => x.CompanyId == companyId))
        .OrderByDescending(x => x.EffectiveFrom).ThenBy(x => x.Name)
        .Select(x => new SalaryTemplateDto
        {
            Id = x.Id, Name = x.Name, PayGroupId = x.PayGroupId, DepartmentId = x.DepartmentId,
            EmployeeType = x.EmployeeType, GradeId = x.GradeId, EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo, BasicPay = x.BasicPay, Hra = x.Hra, Lta = x.Lta,
            OtherAllowance = x.OtherAllowance, Bonus = x.Bonus, IsActive = x.IsActive && !x.IsRetired, Version = x.Version,
            SourceTemplateId = x.SourceTemplateId, SourceTemplateVersion = x.SourceTemplateVersion,
            IsRetired = x.IsRetired, RetiredBy = x.RetiredBy, RetiredAtUtc = x.RetiredAtUtc,
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
        var versions = (await componentDefinitions.GetAll(x => x.CompanyId == companyId && x.Code == code)).ToList();
        PayrollSalaryComponentDefinition? previousVersion = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            previousVersion = versions.FirstOrDefault(x => x.Id == dto.Id);
            if (previousVersion is null)
                return new Result { Success = false, StatusCode = 404, Message = "Payroll component version was not found." };
            if (previousVersion.Version != dto.Version)
                return new Result { Success = false, StatusCode = 409, Message = "Payroll component changed; refresh and create a new version." };
            if (previousVersion.Version != versions.Max(x => x.Version))
                return new Result { Success = false, StatusCode = 409, Message = "This component version has been superseded. Refresh and revise the latest version." };
            if (!string.Equals(previousVersion.Code, code, StringComparison.OrdinalIgnoreCase))
                return new Result { Success = false, StatusCode = 409, Message = "A component code cannot be changed in place. Create a new component instead." };
        }
        else if (versions.Count > 0)
        {
            return new Result { Success = false, StatusCode = 409, Message = "This component code already has a version. Create its next effective-dated version from the existing row." };
        }

        DateTime? priorEndDate = null;
        var closesPriorVersion = previousVersion is not null &&
            (!previousVersion.EffectiveTo.HasValue || previousVersion.EffectiveTo.Value.Date >= dto.EffectiveFrom.Date);
        if (previousVersion is not null)
        {
            var latestEffectiveStart = versions.Max(x => x.EffectiveFrom.Date);
            if (dto.EffectiveFrom.Date <= latestEffectiveStart)
                return new Result { Success = false, StatusCode = 409, Message = "A component revision must start after the latest existing effective date. Backdated revisions require an audited correction." };
            if (dto.EffectiveTo.HasValue && versions.Any(x => x.Id != previousVersion.Id &&
                x.EffectiveFrom.Date > previousVersion.EffectiveFrom.Date && x.EffectiveFrom.Date <= dto.EffectiveTo.Value.Date))
                return new Result { Success = false, StatusCode = 409, Message = "The component revision overlaps a later version. Refresh component history and choose a non-overlapping date range." };
            if (closesPriorVersion) priorEndDate = previousVersion.EffectiveTo;
        }

        var entity = new PayrollSalaryComponentDefinition
        {
            // Definitions are append-only revisions. Never rewrite a component
            // that may already be referenced by templates or historical salaries.
            Id = string.Empty, CompanyId = companyId, Code = code, Name = dto.Name.Trim(),
            Kind = kind, CalculationType = calculationType, BaseComponentCode = dto.BaseComponentCode?.Trim().ToUpperInvariant(),
            IsTaxable = dto.IsTaxable, EffectiveFrom = dto.EffectiveFrom.Date, EffectiveTo = dto.EffectiveTo?.Date,
            IsActive = dto.IsActive, Version = versions.Count == 0 ? 1 : versions.Max(x => x.Version) + 1
        };
        if (!closesPriorVersion) return await componentDefinitions.AddOne(entity);

        // Close only the prior version's coverage window; its definition and existing
        // payroll snapshots remain unchanged. Scope the replacement by version so a
        // concurrent revision cannot also close this same record.
        var closedPrior = new PayrollSalaryComponentDefinition
        {
            Id = previousVersion!.Id, CompanyId = previousVersion.CompanyId, Code = previousVersion.Code,
            Name = previousVersion.Name, Kind = previousVersion.Kind, CalculationType = previousVersion.CalculationType,
            BaseComponentCode = previousVersion.BaseComponentCode, IsTaxable = previousVersion.IsTaxable,
            EffectiveFrom = previousVersion.EffectiveFrom, EffectiveTo = dto.EffectiveFrom.Date.AddDays(-1),
            IsActive = previousVersion.IsActive, Version = previousVersion.Version
        };
        var closeResult = await componentDefinitions.Update(
            Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.Id, previousVersion.Id) &
            Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.Version, previousVersion.Version) &
            Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.EffectiveTo, previousVersion.EffectiveTo), closedPrior);
        if (!closeResult.Success)
            return new Result { Success = false, StatusCode = 409, Message = "The previous component version changed and could not be closed. Refresh history and retry." };

        var addResult = await componentDefinitions.AddOne(entity);
        if (addResult.Success) return addResult;
        var afterFailedAdd = (await componentDefinitions.GetAll(x => x.CompanyId == companyId && x.Code == code)).ToList();
        if (afterFailedAdd.Any(x => x.Version == entity.Version))
            return new Result { Success = false, StatusCode = 409, Message = "Another administrator created this component revision concurrently. Refresh component history before retrying." };

        var restorePrior = new PayrollSalaryComponentDefinition
        {
            Id = previousVersion.Id, CompanyId = previousVersion.CompanyId, Code = previousVersion.Code,
            Name = previousVersion.Name, Kind = previousVersion.Kind, CalculationType = previousVersion.CalculationType,
            BaseComponentCode = previousVersion.BaseComponentCode, IsTaxable = previousVersion.IsTaxable,
            EffectiveFrom = previousVersion.EffectiveFrom, EffectiveTo = priorEndDate,
            IsActive = previousVersion.IsActive, Version = previousVersion.Version
        };
        var restoreResult = await componentDefinitions.Update(
            Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.Id, previousVersion.Id) &
            Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.Version, previousVersion.Version) &
            Builders<PayrollSalaryComponentDefinition>.Filter.Eq(x => x.EffectiveTo, closedPrior.EffectiveTo), restorePrior);
        if (!restoreResult.Success)
            return new Result { Success = false, StatusCode = 500, Message = "The new component version could not be saved and the prior date window could not be restored automatically. Payroll configuration requires administrator review." };
        return addResult;
    }

    public async Task<Result> SeedStandardComponents(string companyId, string actorUserId)
    {
        if (componentDefinitions is null)
            return new Result { Success = false, StatusCode = 503, Message = "Payroll component storage is unavailable." };

        var existing = (await componentDefinitions.GetAll(x => x.CompanyId == companyId)).ToList();
        var existingCodes = existing.Select(x => x.Code.Trim().ToUpperInvariant()).ToHashSet();

        var startOfYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
        var standards = new List<PayrollSalaryComponentDefinition>
        {
            new() { Id = string.Empty, CompanyId = companyId, Code = "HRA", Name = "House Rent Allowance", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = startOfYear, IsActive = true, Version = 1 },
            new() { Id = string.Empty, CompanyId = companyId, Code = "SPECIAL_ALLOWANCE", Name = "Special Allowance", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = startOfYear, IsActive = true, Version = 1 },
            new() { Id = string.Empty, CompanyId = companyId, Code = "LTA", Name = "Leave Travel Allowance", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = startOfYear, IsActive = true, Version = 1 },
            new() { Id = string.Empty, CompanyId = companyId, Code = "BONUS", Name = "Annual / Performance Bonus", Kind = "EARNING", CalculationType = "FIXED", IsTaxable = true, EffectiveFrom = startOfYear, IsActive = true, Version = 1 },
            new() { Id = string.Empty, CompanyId = companyId, Code = "HEALTH_INSURANCE", Name = "Group Health Insurance", Kind = "DEDUCTION", CalculationType = "FIXED", IsTaxable = false, EffectiveFrom = startOfYear, IsActive = true, Version = 1 }
        };

        var addedCount = 0;
        foreach (var standard in standards)
        {
            if (!existingCodes.Contains(standard.Code))
            {
                await componentDefinitions.AddOne(standard);
                addedCount++;
            }
        }

        return new Result
        {
            Success = true,
            StatusCode = 200,
            Message = addedCount > 0
                ? $"Successfully seeded {addedCount} standard Indian payroll components (HRA, Special Allowance, LTA, Bonus, Health Insurance)."
                : "Standard payroll components are already configured for this company."
        };
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
        var effectiveFrom = dto.EffectiveFrom.Date;
        var effectiveTo = dto.EffectiveTo?.Date;
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
            dto.EpfEmployeeRatePercent < 0 || dto.EpfEmployeeRatePercent > 100 ||
            dto.EpfEmployerRatePercent < 0 || dto.EpfEmployerRatePercent > 100 ||
            dto.EsicEmployeeRatePercent < 0 || dto.EsicEmployeeRatePercent > 100 ||
            dto.EsicEmployerRatePercent < 0 || dto.EsicEmployerRatePercent > 100 ||
            (!string.Equals(epfMode, ExemptMode, StringComparison.Ordinal) && (dto.EpfEmployeeRatePercent <= 0 || dto.EpfEmployerRatePercent <= 0 || dto.EpfWageCeiling <= 0)) ||
            (!string.Equals(esicMode, ExemptMode, StringComparison.Ordinal) && (dto.EsicEmployeeRatePercent <= 0 || dto.EsicEmployerRatePercent <= 0 || dto.EsicWageCeiling <= 0)) ||
            dto.IsActive && RequiresStatutoryReview(epfMode, esicMode, tdsMode) &&
            (string.IsNullOrWhiteSpace(dto.StatutoryJurisdiction) || string.IsNullOrWhiteSpace(dto.FinancialYear) || string.IsNullOrWhiteSpace(dto.StatutoryRuleVersion)))
            return new Result { Success = false, StatusCode = 400, Message = "Invalid payroll or statutory policy settings. Active statutory policies require jurisdiction, financial year, and rule version." };
        PayrollPolicyProfile? existingDraft = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            existingDraft = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == dto.Id);
            if (existingDraft is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll policy was not found." };
            if (existingDraft.Version != dto.Version) return new Result { Success = false, StatusCode = 409, Message = "Payroll policy changed; reload and retry." };
            if (existingDraft.ApprovalStatus == "APPROVED")
                return new Result { Success = false, StatusCode = 409, Message = "Submitted or approved payroll policy versions are immutable. Create a new effective-dated version for changes." };
        }
        // Profiles may overlap because an employee or pay group can intentionally use a
        // different approved profile from the company default. Effective dates select the
        // newest company default, while explicit assignments remain pinned to their profile.
        var duplicate = await policies.Exist(x => x.CompanyId == companyId && x.Name == dto.Name.Trim() &&
            x.EffectiveFrom == effectiveFrom && x.Id != dto.Id);
        if (duplicate) return new Result { Success = false, StatusCode = 409, Message = "A policy with this name and start date already exists. Choose a new effective date or name." };

        var entity = new PayrollPolicyProfile
        {
            Id = dto.Id ?? string.Empty, CompanyId = companyId, Name = dto.Name.Trim(), CreatedBy = existingDraft?.CreatedBy ?? actorUserId,
            Version = Math.Max(1, dto.Version), DivisorPolicy = divisorPolicy, Scope = scope,
            RequireAttendanceLock = dto.RequireAttendanceLock, EpfMode = epfMode, EpfWageCeiling = dto.EpfWageCeiling, EsicMode = esicMode, EsicWageCeiling = dto.EsicWageCeiling, TdsMode = tdsMode, StatutoryJurisdiction = dto.StatutoryJurisdiction.Trim().ToUpperInvariant(), FinancialYear = dto.FinancialYear.Trim(), StatutoryRuleVersion = dto.StatutoryRuleVersion.Trim(), ApprovalStatus = "DRAFT", EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo, IsActive = dto.IsActive,
            EpfEmployeeRatePercent = dto.EpfEmployeeRatePercent, EpfEmployerRatePercent = dto.EpfEmployerRatePercent,
            EsicEmployeeRatePercent = dto.EsicEmployeeRatePercent, EsicEmployerRatePercent = dto.EsicEmployerRatePercent
        };
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            entity.Version = existingDraft!.Version + 1;
            return await policies.Update(
                Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Id, dto.Id) &
                Builders<PayrollPolicyProfile>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Version, dto.Version), entity);
        }
        return await policies.AddOne(entity);
    }

    public async Task<Result> DeletePolicyDraft(string companyId, string policyId, int version)
    {
        var policy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == policyId);
        if (policy is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll policy was not found." };
        if (policy.Version != version) return new Result { Success = false, StatusCode = 409, Message = "Payroll policy changed; refresh and retry." };
        if (policy.ApprovalStatus != "DRAFT")
            return new Result { Success = false, StatusCode = 409, Message = "Only draft payroll policies can be deleted. Submitted or approved versions are retained for audit." };

        return await policies.Delete(
            Builders<PayrollPolicyProfile>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Id, policyId) &
            Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Version, version) &
            Builders<PayrollPolicyProfile>.Filter.Eq(x => x.ApprovalStatus, "DRAFT"));
    }

    public async Task<Result> CancelPolicySubmission(string companyId, string actorUserId, string policyId, int version)
    {
        var policy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == policyId);
        if (policy is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll policy was not found." };
        if (policy.Version != version) return new Result { Success = false, StatusCode = 409, Message = "Payroll policy changed; reload and retry." };
        if (policy.ApprovalStatus != "SUBMITTED") return new Result { Success = false, StatusCode = 409, Message = "Only submitted payroll policies can be withdrawn to draft." };
        policy.ApprovalStatus = "DRAFT";
        policy.SubmittedBy = null;
        policy.SubmittedAtUtc = null;
        policy.Version++;
        return await policies.Update(Builders<PayrollPolicyProfile>.Filter.Eq(x => x.CompanyId, companyId) & Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Id, policyId) & Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Version, version), policy);
    }

    public async Task<Result> RetirePolicy(string companyId, string actorUserId, string policyId, int version)
    {
        var policy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == policyId);
        if (policy is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll policy was not found." };
        if (policy.Version != version) return new Result { Success = false, StatusCode = 409, Message = "Payroll policy changed; refresh and retry." };
        if (!policy.IsActive) return new Result { Success = true, Message = "Payroll policy is already inactive." };

        policy.IsActive = false;
        if (policy.EffectiveFrom > DateTime.UtcNow.Date)
            policy.EffectiveTo = policy.EffectiveFrom;
        else if (!policy.EffectiveTo.HasValue || policy.EffectiveTo.Value > DateTime.UtcNow.Date)
            policy.EffectiveTo = DateTime.UtcNow.Date;
        policy.Version++;
        return await policies.Update(
            Builders<PayrollPolicyProfile>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Id, policyId) &
            Builders<PayrollPolicyProfile>.Filter.Eq(x => x.Version, version),
            policy);
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

    public async Task<Result> ApprovePolicy(string companyId, string actorUserId, string policyId, int version, bool isCompanyAdministrator = false)
    {
        var policy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == policyId);
        if (policy is null) return new Result { Success = false, StatusCode = 404, Message = "Payroll policy was not found." };
        if (policy.Version != version) return new Result { Success = false, StatusCode = 409, Message = "Payroll policy changed; reload and retry." };
        if (policy.ApprovalStatus != "SUBMITTED") return new Result { Success = false, StatusCode = 409, Message = "Payroll policy must be submitted before approval." };
        if (policy.IsActive && RequiresStatutoryReview(policy) && !HasStatutoryReviewMetadata(policy))
            return MissingStatutoryReviewMetadata();
        if (!isCompanyAdministrator &&
            ((!string.IsNullOrWhiteSpace(policy.CreatedBy) && string.Equals(policy.CreatedBy, actorUserId, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(policy.SubmittedBy) && string.Equals(policy.SubmittedBy, actorUserId, StringComparison.OrdinalIgnoreCase)))
           )
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
        if (!string.IsNullOrWhiteSpace(dto.Id) && dto.Version < 1)
            return new Result { Success = false, StatusCode = 409, Message = "Template version is required for an update. Refresh the template and retry." };
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            var existing = await templates.FirstOrDefault(x => x.Id == dto.Id && x.CompanyId == companyId);
            if (existing is null)
                return new Result { Success = false, StatusCode = 404, Message = "Salary template was not found in this company." };
            if (existing.Version != dto.Version)
                return new Result { Success = false, StatusCode = 409, Message = "The salary template changed while you were editing it. Refresh the template and reapply your changes." };
            return new Result { Success = false, StatusCode = 409, Message = "Saved salary templates are immutable. Create a separate revision so existing employee assignments and payroll history remain unchanged." };
        }
        SalaryTemplate? source = null;
        if (!string.IsNullOrWhiteSpace(dto.SourceTemplateId))
        {
            source = await templates.FirstOrDefault(x => x.Id == dto.SourceTemplateId && x.CompanyId == companyId);
            if (source is null)
                return new Result { Success = false, StatusCode = 404, Message = "The source salary template was not found in this company." };
            if (dto.SourceTemplateVersion != source.Version)
                return new Result { Success = false, StatusCode = 409, Message = "The source salary template changed. Refresh it before creating a revision." };
        }
        else if (dto.SourceTemplateVersion.HasValue)
            return new Result { Success = false, StatusCode = 400, Message = "A source template is required when a source version is supplied." };
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.EffectiveFrom == default ||
            (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return InvalidDates();
        if (new[] { dto.BasicPay, dto.Hra, dto.Lta, dto.OtherAllowance, dto.Bonus }.Any(value => value < 0) ||
            dto.Components.Any(component => component.Value < 0))
            return new Result { Success = false, StatusCode = 400, Message = "Salary template amounts and rates cannot be negative." };
        dto.PayGroupId = string.IsNullOrWhiteSpace(dto.PayGroupId) ? null : dto.PayGroupId.Trim();
        if (dto.PayGroupId is not null)
        {
            var group = await payGroups.FirstOrDefault(x => x.CompanyId == companyId && x.Id == dto.PayGroupId && x.IsActive);
            if (group is null)
                return new Result { Success = false, StatusCode = 404, Message = "Pay group was not found in this company or is inactive." };
            if (!string.Equals(group.Schedule, "MONTHLY", StringComparison.OrdinalIgnoreCase))
                return new Result { Success = false, StatusCode = 400, Message = "Only monthly pay groups can be used by salary templates." };
            if (group.EffectiveFrom.Date > dto.EffectiveFrom.Date ||
                (group.EffectiveTo.HasValue && (!dto.EffectiveTo.HasValue || dto.EffectiveTo.Value.Date > group.EffectiveTo.Value.Date)))
                return new Result { Success = false, StatusCode = 400, Message = "The pay group must be effective for the entire salary template date range." };
        }

        var components = new List<PayrollSalaryTemplateComponent>();
        if (dto.Components.Count > 0)
        {
            if (componentDefinitions is null)
                return new Result { Success = false, StatusCode = 503, Message = "Payroll component storage is unavailable." };
            var ids = dto.Components.Select(x => x.ComponentId).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToHashSet();
            if (ids.Count != dto.Components.Count)
                return new Result { Success = false, StatusCode = 400, Message = "Every template component must reference a component definition." };
            var companyDefinitions = (await componentDefinitions.GetAll(x => x.CompanyId == companyId)).ToList();
            var referencedDefinitions = companyDefinitions.Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id);
            if (referencedDefinitions.Count != ids.Count)
                return new Result { Success = false, StatusCode = 404, Message = "One or more salary components were not found." };
            if (referencedDefinitions.Values.Select(x => x.Code.Trim().ToUpperInvariant()).Distinct().Count() != dto.Components.Count)
                return new Result { Success = false, StatusCode = 409, Message = "A salary template cannot contain duplicate component codes." };

            foreach (var item in dto.Components)
            {
                var referenced = referencedDefinitions[item.ComponentId!];
                var current = PayrollComponentDefinitionResolver.Resolve(companyDefinitions, companyId, referenced.Code, dto.EffectiveFrom);
                if (current is null)
                    return new Result { Success = false, StatusCode = 400, Message = $"Payroll component '{referenced.Code}' is not enabled when this salary template starts." };
                if ((dto.EffectiveTo is null && current.EffectiveTo.HasValue) ||
                    (dto.EffectiveTo.HasValue && current.EffectiveTo.HasValue && current.EffectiveTo.Value.Date < dto.EffectiveTo.Value.Date))
                    return new Result { Success = false, StatusCode = 400, Message = $"Salary template dates extend beyond payroll component '{referenced.Code}'. End the template before the component version ends." };
                var nextVersion = companyDefinitions.FirstOrDefault(x =>
                    x.CompanyId == companyId && string.Equals(x.Code, referenced.Code, StringComparison.OrdinalIgnoreCase) &&
                    x.EffectiveFrom.Date > dto.EffectiveFrom.Date &&
                    (dto.EffectiveTo is null || x.EffectiveFrom.Date <= dto.EffectiveTo.Value.Date));
                if (nextVersion is not null)
                    return new Result { Success = false, StatusCode = 400, Message = $"Salary template overlaps a later version of payroll component '{referenced.Code}' effective {nextVersion.EffectiveFrom:yyyy-MM-dd}. Split the template at that date." };
            }

            components = dto.Components.Select((item, index) =>
            {
                var referenced = referencedDefinitions[item.ComponentId!];
                var definition = PayrollComponentDefinitionResolver.Resolve(companyDefinitions, companyId, referenced.Code, dto.EffectiveFrom)!;
                return new PayrollSalaryTemplateComponent
                {
                    ComponentId = definition.Id, Code = definition.Code, Name = definition.Name,
                    Kind = definition.Kind, CalculationType = definition.CalculationType,
                    BaseComponentCode = definition.BaseComponentCode, Value = item.Value,
                    IsTaxable = definition.IsTaxable, SortOrder = item.SortOrder == 0 ? index : item.SortOrder
                };
            }).OrderBy(x => x.SortOrder).ToList();
        }

        var calculation = PayrollComponentCalculationEngine.Calculate(
            new[] { ("BASIC", dto.BasicPay), ("HRA", dto.Hra), ("LTA", dto.Lta), ("OTHER_ALLOWANCE", dto.OtherAllowance), ("BONUS", dto.Bonus) }
                .Where(item => item.Item2.HasValue || !components.Any(component => string.Equals(component.Code, item.Item1, StringComparison.OrdinalIgnoreCase)))
                .Select(item => new PayrollComponentSnapshot { Code = item.Item1, Name = item.Item1, Value = item.Item2 ?? 0 })
                .Concat(components.Select(item => new PayrollComponentSnapshot { Code = item.Code, Name = item.Name, Kind = item.Kind,
                    CalculationType = item.CalculationType, BaseComponentCode = item.BaseComponentCode, Value = item.Value, SortOrder = item.SortOrder })));
        if (!calculation.Success)
            return new Result { Success = false, StatusCode = 400, Message = calculation.ErrorMessage, ErrorCode = calculation.ErrorCode };

        var entity = new SalaryTemplate
        {
            Id = dto.Id ?? string.Empty, CompanyId = companyId, Name = dto.Name.Trim(), PayGroupId = dto.PayGroupId,
            DepartmentId = dto.DepartmentId, EmployeeType = dto.EmployeeType, GradeId = dto.GradeId,
            EffectiveFrom = dto.EffectiveFrom.Date, EffectiveTo = dto.EffectiveTo?.Date, BasicPay = dto.BasicPay,
            Hra = dto.Hra, Lta = dto.Lta, OtherAllowance = dto.OtherAllowance, Bonus = dto.Bonus,
            Components = components, IsActive = dto.IsActive, Version = Math.Max(1, dto.Version)
        };
        entity.Version = 1;
        entity.SourceTemplateId = source?.Id;
        entity.SourceTemplateVersion = source?.Version;
        return await templates.AddOne(entity);
    }

    public async Task<Result> RetireTemplate(string companyId, string actorUserId, string templateId, int version)
    {
        var template = await templates.FirstOrDefault(x => x.CompanyId == companyId && x.Id == templateId);
        if (template is null)
            return new Result { Success = false, StatusCode = 404, Message = "Salary template was not found in this company." };
        if (version < 1 || template.Version != version)
            return new Result { Success = false, StatusCode = 409, Message = "Salary template changed; refresh it before retiring." };
        if (template.IsRetired)
            return new Result { Success = true, Message = "Salary template is already retired." };
        // Keep IsActive unchanged: existing assignments still resolve this immutable template.
        template.IsRetired = true;
        template.RetiredBy = actorUserId;
        template.RetiredAtUtc = DateTime.UtcNow;
        template.Version++;
        var versionFilter = version == 1
            ? Builders<SalaryTemplate>.Filter.Eq(x => x.Version, version) | Builders<SalaryTemplate>.Filter.Exists(x => x.Version, false)
            : Builders<SalaryTemplate>.Filter.Eq(x => x.Version, version);
        var result = await templates.Update(Builders<SalaryTemplate>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<SalaryTemplate>.Filter.Eq(x => x.Id, templateId) & versionFilter &
            Builders<SalaryTemplate>.Filter.Ne(x => x.IsRetired, true), template);
        return !result.Success && result.StatusCode == 400
            ? new Result { Success = false, StatusCode = 409, Message = "Salary template changed; refresh it before retiring." }
            : result;
    }

    public async Task<Result> DeleteTemplate(string companyId, string actorUserId, string templateId, int version)
    {
        var template = await templates.FirstOrDefault(x => x.CompanyId == companyId && x.Id == templateId);
        if (template is null)
            return new Result { Success = false, StatusCode = 404, Message = "Salary template was not found in this company." };
        if (assignments is not null)
        {
            var inUse = await assignments.Exist(x => x.CompanyId == companyId && x.SalaryTemplateId == templateId && !x.IsRejected && !x.IsCancelled);
            if (inUse)
                return new Result { Success = false, StatusCode = 409, Message = "This salary template cannot be deleted because employees are assigned to it. Retire it instead to preserve payroll history." };
        }
        return await templates.Delete(
            Builders<SalaryTemplate>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<SalaryTemplate>.Filter.Eq(x => x.Id, templateId) &
            Builders<SalaryTemplate>.Filter.Eq(x => x.Version, version));
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

