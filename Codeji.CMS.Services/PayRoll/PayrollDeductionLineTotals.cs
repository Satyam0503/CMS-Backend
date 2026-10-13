using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public static class PayrollDeductionLineTotals
{
    // LHD_ED_POLICY is an audit detail for the same amount already stored in LossOfPay.
    public static decimal AdditionalAmount(IEnumerable<PayrollDeductionLineDto>? lines) =>
        PayrollCalculationRules.RoundMoney((lines ?? [])
            .Where(x => !string.Equals(x.Code?.Trim(), "LHD_ED_POLICY", StringComparison.OrdinalIgnoreCase))
            .Sum(x => Math.Max(0m, x.Amount)));

    public static decimal AdditionalAmount(IEnumerable<PayrollDeductionLine>? lines) =>
        PayrollCalculationRules.RoundMoney((lines ?? [])
            .Where(x => !string.Equals(x.Code?.Trim(), "LHD_ED_POLICY", StringComparison.OrdinalIgnoreCase))
            .Sum(x => Math.Max(0m, x.Amount)));
}
