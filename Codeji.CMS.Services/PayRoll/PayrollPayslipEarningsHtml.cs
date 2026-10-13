using System.Globalization;
using System.Net;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public static class PayrollPayslipEarningsHtml
{
    public static string RenderOtherAllowanceRows(EmpPayRoll payroll)
    {
        var lines = payroll.EarningLines.Where(x => x.Amount > 0m).ToArray();
        if (lines.Length == 0)
            lines = [new PayrollEarningLine { Code = "OTHER_ALLOWANCE", Description = "Other Allowance", Amount = payroll.Allowance.OtherAllowance }];

        return string.Join(Environment.NewLine, lines.Select(line =>
            $"<tr><td class=\"fw-bold\">{WebUtility.HtmlEncode(line.Description)}</td><td class=\"text-end\">₹ {line.Amount.ToString("N2", CultureInfo.InvariantCulture)}</td><td></td><td></td></tr>"));
    }

    public static string RenderOtherDeductionRows(EmpPayRoll payroll)
    {
        var lines = payroll.DeductionLines
            .Where(x => !string.Equals(x.Code?.Trim(), "LHD_ED_POLICY", StringComparison.OrdinalIgnoreCase) && x.Amount > 0m)
            .ToArray();
        if (lines.Length == 0)
        {
            var legacyTotal = PayrollDeductionLineTotals.AdditionalAmount(payroll.DeductionLines);
            lines = [new PayrollDeductionLine { Code = "OTHER_DEDUCTIONS", Description = "Other Salary Deductions", Amount = legacyTotal }];
        }

        return string.Join(Environment.NewLine, lines.Select(line =>
            $"<tr><td colspan=\"2\"></td><td class=\"fw-bold\">{WebUtility.HtmlEncode(line.Description)}</td><td class=\"text-end\">₹ {line.Amount.ToString("N2", CultureInfo.InvariantCulture)}</td></tr>"));
    }

    public static string GetEsicEmployeeLabel(EmpPayRoll payroll)
    {
        var rate = payroll.CalculationSnapshot?.EsicEmployeeRatePercent ?? 0.75m;
        return $"ESIC Employee ({rate.ToString("0.##", CultureInfo.InvariantCulture)}%)";
    }
}
