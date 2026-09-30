using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public static class LegacySalaryMigrationMapper
{
    public static EmployeeSalaryStructure Map(SalaryModel legacy)
    {
        if (string.IsNullOrWhiteSpace(legacy.CompanyId) || string.IsNullOrWhiteSpace(legacy.EmployeeId))
            throw new ArgumentException("Legacy salary must have company and employee identity.", nameof(legacy));

        var components = new List<EmployeeSalaryComponentValue>
        {
            Fixed("BASIC", "Basic Salary", legacy.BasicPay, true),
            Fixed("HRA", "House Rent Allowance", legacy.Hra ?? 0m, true),
            Fixed("LTA", "Leave Travel Allowance", legacy.Lta ?? 0m, true),
            Fixed("OTHER_ALLOWANCE", "Other Allowance", legacy.OtherAllowances ?? 0m, true),
            Fixed("BONUS", "Bonus", legacy.Bonus ?? 0m, true),
            Fixed("HEALTH_INSURANCE", "Health Insurance", legacy.HealthInsurance ?? 0m, false, "DEDUCTION")
        };
        if (legacy.TotalDeductions.HasValue)
            components.Add(Fixed("LEGACY_TOTAL_DEDUCTIONS", "Legacy Total Deductions", legacy.TotalDeductions.Value, false, "DEDUCTION"));

        return new EmployeeSalaryStructure
        {
            Id = legacy.SalaryId.ToString(), CompanyId = legacy.CompanyId, EmployeeId = legacy.EmployeeId, UserId = legacy.UserId.ToString(),
            Currency = string.IsNullOrWhiteSpace(legacy.Currency) ? "INR" : legacy.Currency.ToUpperInvariant(),
            PaymentFrequency = string.IsNullOrWhiteSpace(legacy.PaymentFrequency) ? "MONTHLY" : legacy.PaymentFrequency.ToUpperInvariant(),
            MonthlyGross = legacy.GrossSalary, AnnualCtc = legacy.Ctc, EffectiveFrom = legacy.EffectiveFrom.Date, EffectiveTo = legacy.EffectiveTo?.Date,
            Status = legacy.Status ? "ACTIVE" : "SUPERSEDED", ApprovalStatus = legacy.Status ? "APPROVED" : "SUPERSEDED", Source = "LEGACY_MIGRATION", Version = 1,
            CreatedAtUtc = legacy.CreatedAt, Components = components
        };
    }

    private static EmployeeSalaryComponentValue Fixed(string code, string name, decimal value, bool taxable, string kind = "EARNING") =>
        new() { ComponentCode = code, ComponentName = name, Kind = kind, CalculationType = "FIXED", Value = value, IsTaxable = taxable, IsRecurring = true };
}
