using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public static class EmployeeSalaryStructureResolver
{
    public static EmployeeSalaryStructure? Resolve(string companyId, string employeeId, DateTime date, IEnumerable<EmployeeSalaryStructure> records, bool approvedOnly = true)
    {
        var target = date.Date;
        return records
            .Where(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.EffectiveFrom.Date <= target && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= target))
            .Where(x => !approvedOnly || (x.ApprovalStatus == "APPROVED" && x.Status is "ACTIVE" or "APPROVED"))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.Version)
            .FirstOrDefault();
    }

    public static bool Overlaps(EmployeeSalaryStructure candidate, IEnumerable<EmployeeSalaryStructure> existing, string? excludedId = null) =>
        existing.Any(x => x.CompanyId == candidate.CompanyId && x.EmployeeId == candidate.EmployeeId && x.Id != excludedId && candidate.EffectiveFrom.Date <= (x.EffectiveTo?.Date ?? DateTime.MaxValue) && (candidate.EffectiveTo?.Date ?? DateTime.MaxValue) >= x.EffectiveFrom.Date);
}

