using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public static class EmployeeSalaryPayrollMapper
{
    private static readonly HashSet<string> StandardCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "BASIC", "BASIC_PAY", "HRA", "LTA", "OTHER_ALLOWANCE", "SPECIAL_ALLOWANCE", "BONUS", "HEALTH_INSURANCE"
    };

    public static SalaryModel Map(EmployeeSalaryStructure structure)
    {
        if (!Guid.TryParse(structure.Id, out var salaryId))
            throw new InvalidOperationException($"Approved salary structure '{structure.Id}' for employee '{structure.EmployeeId}' has an invalid identifier.");

        var components = structure.Components.Select((x, index) => new PayrollComponentSnapshot
        {
            Code = x.ComponentCode, Name = x.ComponentName, Kind = x.Kind,
            CalculationType = x.CalculationType, BaseComponentCode = x.BaseComponentCode,
            Value = x.Percentage ?? x.Value, IsTaxable = x.IsTaxable, IsRecurring = x.IsRecurring,
            IsStatutory = x.IsStatutory, IsEmployerContribution = x.IsEmployerContribution, SortOrder = index
        }).ToList();
        var calculation = components.Count == 0
            ? new PayrollComponentCalculationOutcome { Success = true }
            : PayrollComponentCalculationEngine.Calculate(components);
        if (!calculation.Success)
            throw new InvalidOperationException(calculation.ErrorMessage ?? "Employee salary component formula is invalid.");
        decimal Value(string code) => calculation.Values.TryGetValue(code, out var amount) ? amount : 0m;
        var recurringEarnings = structure.Components
            .Where(x => string.Equals(x.Kind, "EARNING", StringComparison.OrdinalIgnoreCase) && x.IsRecurring && !x.IsEmployerContribution && !x.IsStatutory)
            .ToArray();
        decimal RecurringValue(string code) => recurringEarnings
            .Where(x => string.Equals(x.ComponentCode, code, StringComparison.OrdinalIgnoreCase))
            .Sum(x => Value(x.ComponentCode));
        var basic = RecurringValue("BASIC") + RecurringValue("BASIC_PAY");
        var hra = RecurringValue("HRA");
        var lta = RecurringValue("LTA");
        var bonus = RecurringValue("BONUS");
        var standardOther = RecurringValue("OTHER_ALLOWANCE") + RecurringValue("SPECIAL_ALLOWANCE");
        var customRecurringEarnings = recurringEarnings
            .Where(x => !StandardCodes.Contains(x.ComponentCode) || string.Equals(x.ComponentCode, "HEALTH_INSURANCE", StringComparison.OrdinalIgnoreCase))
            .Sum(x => Value(x.ComponentCode));
        var other = standardOther + customRecurringEarnings;
        var healthInsurance = structure.Components
            .Where(x => string.Equals(x.ComponentCode, "HEALTH_INSURANCE", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.Kind, "DEDUCTION", StringComparison.OrdinalIgnoreCase) && x.IsRecurring &&
                        !x.IsEmployerContribution && !x.IsStatutory)
            .Sum(x => Value(x.ComponentCode));
        var otherDeductions = structure.Components
            .Where(x => string.Equals(x.Kind, "DEDUCTION", StringComparison.OrdinalIgnoreCase) && x.IsRecurring && !x.IsEmployerContribution && !x.IsStatutory && !string.Equals(x.ComponentCode, "HEALTH_INSURANCE", StringComparison.OrdinalIgnoreCase))
            .Select(x => new SalaryDeductionComponent { Code = x.ComponentCode, Name = x.ComponentName, Amount = Value(x.ComponentCode) })
            .ToList();
        var otherEarnings = recurringEarnings
            .Where(x => !string.Equals(x.ComponentCode, "BASIC", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x.ComponentCode, "BASIC_PAY", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x.ComponentCode, "HRA", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x.ComponentCode, "LTA", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x.ComponentCode, "BONUS", StringComparison.OrdinalIgnoreCase))
            .Select(x => new SalaryEarningComponent { Code = x.ComponentCode, Name = x.ComponentName, Amount = Value(x.ComponentCode) })
            .Where(x => x.Amount > 0m)
            .ToList();
        var gross = recurringEarnings.Sum(x => Value(x.ComponentCode));
        var deductions = healthInsurance + otherDeductions.Sum(x => x.Amount);

        return new SalaryModel
        {
            SalaryId = salaryId,
            UserId = Guid.TryParse(structure.UserId, out var userId) ? userId : null,
            CompanyId = structure.CompanyId,
            EmployeeId = structure.EmployeeId,
            BasicPay = basic,
            Hra = hra,
            Lta = lta,
            OtherAllowances = other,
            Bonus = bonus,
            HealthInsurance = healthInsurance,
            OtherDeductions = otherDeductions,
            OtherEarnings = otherEarnings,
            TotalDeductions = deductions,
            GrossSalary = gross,
            NetSalary = Math.Max(0m, gross - deductions),
            Ctc = structure.AnnualCtc ?? gross * 12m,
            EffectiveFrom = structure.EffectiveFrom,
            EffectiveTo = structure.EffectiveTo,
            PaymentFrequency = structure.PaymentFrequency,
            Currency = structure.Currency,
            Status = string.Equals(structure.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)
        };
    }
}
