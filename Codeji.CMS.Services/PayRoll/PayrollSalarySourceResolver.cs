using Codeji.CMS.DTO.Salary;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public sealed record PayrollSalarySourceResolution(List<SalaryModel> Records, bool UsesCanonicalSource);

/// <summary>
/// Resolves the transitional canonical/legacy salary source without allowing a
/// stale legacy record to revive after the canonical salary timeline has begun.
/// </summary>
public static class PayrollSalarySourceResolver
{
    public static PayrollSalaryStructureSnapshot SnapshotLegacyRecord(SalaryModel salary) => new()
    {
        SalaryStructureId = salary.SalaryId.ToString("D"), Version = 0,
        EffectiveFrom = salary.EffectiveFrom, EffectiveTo = salary.EffectiveTo,
        Components = new List<PayrollComponentSnapshot>
        {
            new() { Code = "BASIC_PAY", Name = "Basic Pay", Kind = "EARNING", Value = salary.BasicPay, IsTaxable = true },
            new() { Code = "HRA", Name = "House Rent Allowance", Kind = "EARNING", Value = salary.Hra ?? 0m, IsTaxable = true, SortOrder = 1 },
            new() { Code = "LTA", Name = "Leave Travel Allowance", Kind = "EARNING", Value = salary.Lta ?? 0m, IsTaxable = true, SortOrder = 2 },
            new() { Code = "OTHER_ALLOWANCE", Name = "Other Allowance", Kind = "EARNING", Value = salary.OtherAllowances ?? 0m, IsTaxable = true, SortOrder = 3 },
            new() { Code = "BONUS", Name = "Bonus", Kind = "EARNING", Value = salary.Bonus ?? 0m, IsTaxable = true, SortOrder = 4 },
            new() { Code = "HEALTH_INSURANCE", Name = "Health Insurance", Kind = "DEDUCTION", Value = salary.HealthInsurance ?? 0m, SortOrder = 5 }
        }.Concat((salary.OtherDeductions ?? []).OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select((x, index) => new PayrollComponentSnapshot
            {
                Code = string.IsNullOrWhiteSpace(x.Code) ? $"LEGACY_DEDUCTION_{index + 1}" : $"SALARY_{x.Code.Trim().ToUpperInvariant()}",
                Name = string.IsNullOrWhiteSpace(x.Name) ? "Other Deduction" : x.Name,
                Kind = "DEDUCTION", Value = x.Amount, IsTaxable = false, SortOrder = index + 6
            })).ToList()
    };
    public static PayrollSalaryStructureSnapshotDto SnapshotLegacyRecordDto(SalaryModel salary)
    {
        var snapshot = SnapshotLegacyRecord(salary);
        return new PayrollSalaryStructureSnapshotDto
        {
            SalaryStructureId = snapshot.SalaryStructureId, Version = snapshot.Version,
            EffectiveFrom = snapshot.EffectiveFrom, EffectiveTo = snapshot.EffectiveTo,
            Components = snapshot.Components.Select(x => new PayrollComponentSnapshotDto
            {
                Code = x.Code, Name = x.Name, Kind = x.Kind, Value = x.Value,
                IsTaxable = x.IsTaxable, IsRecurring = x.IsRecurring, SortOrder = x.SortOrder
            }).ToList()
        };
    }
    // Maps legacy salary rows to immutable run snapshots.
    public static PayrollSalarySourceResolution Resolve(
        string companyId,
        string employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyCollection<EmployeeSalaryStructure> approvedActiveStructures,
        Func<IEnumerable<SalaryModel>> loadLegacyStructures)
    {
        var canonical = approvedActiveStructures
            .Where(x => x.CompanyId == companyId && x.EmployeeId == employeeId &&
                x.EffectiveFrom.Date <= periodEnd.Date &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= periodStart.Date))
            .OrderByDescending(x => x.EffectiveFrom.Date)
            .ThenByDescending(x => x.Version)
            .Select(EmployeeSalaryPayrollMapper.Map)
            .ToList();
        if (canonical.Count > 0) return new PayrollSalarySourceResolution(canonical, true);

        // Before the first approved canonical effective date, legacy rows remain
        // a compatibility source. Once that timeline has started, a gap or an
        // expired salary must not silently fall back to an older legacy salary.
        var canonicalTimelineHasStarted = approvedActiveStructures.Any(x =>
            x.CompanyId == companyId && x.EmployeeId == employeeId && x.EffectiveFrom.Date <= periodEnd.Date);
        if (canonicalTimelineHasStarted) return new PayrollSalarySourceResolution([], true);

        var legacy = loadLegacyStructures()
            .Where(x => x.CompanyId == companyId && x.EmployeeId == employeeId &&
                x.EffectiveFrom.Date <= periodEnd.Date &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= periodStart.Date))
            .OrderByDescending(x => x.EffectiveFrom.Date)
            .ToList();
        return new PayrollSalarySourceResolution(legacy, false);
    }
}
