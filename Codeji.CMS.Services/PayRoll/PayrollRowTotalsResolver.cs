using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public readonly record struct PayrollRowTotals(decimal Gross, decimal Deductions, decimal Net);

/// <summary>
/// Resolves the employee take-home totals from the persisted payroll row shape.
/// Keep the legacy row as the input boundary until all payroll producers emit the
/// versioned canonical calculation result; this prevents run, sync, and payslip
/// paths from maintaining separate gross-to-net formulas in the meantime.
/// </summary>
public static class PayrollRowTotalsResolver
{
    public static PayrollRowTotals Resolve(EmpPayRoll payroll)
    {
        ArgumentNullException.ThrowIfNull(payroll);

        var gross = PayrollCalculationRules.RoundMoney(Math.Max(0m,
            payroll.BasicPay + payroll.Bonus +
            payroll.Allowance.HRA + payroll.Allowance.LTA + payroll.Allowance.OtherAllowance));

        var deductions = PayrollCalculationRules.RoundMoney(Math.Max(0m,
            payroll.Deduction.LossOfPay + payroll.Deduction.IncomeTax +
            payroll.Deduction.HealthInsurance + payroll.Deduction.EPF +
            payroll.Deduction.ESIC + payroll.Deduction.ProfessionalTax +
            PayrollDeductionLineTotals.AdditionalAmount(payroll.DeductionLines)));

        return new PayrollRowTotals(gross, deductions, PayrollCalculationRules.RoundMoney(gross - deductions));
    }
}
