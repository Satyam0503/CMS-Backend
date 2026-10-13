using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollDeductionLineTotalsTests
{
    [Fact]
    public void Excludes_lop_audit_line_already_counted_in_loss_of_pay_but_counts_salary_deductions()
    {
        PayrollDeductionLineDto[] dtoLines =
        [
            new() { Code = "LHD_ED_POLICY", Description = "LOP audit", Amount = 250m },
            new() { Code = "SALARY_LOAN_REPAYMENT", Description = "Loan repayment", Amount = 1000m }
        ];
        PayrollDeductionLine[] entityLines =
        [
            new() { Code = "lhd_ed_policy", Description = "LOP audit", Amount = 250m },
            new() { Code = "SALARY_LOAN_REPAYMENT", Description = "Loan repayment", Amount = 1000m }
        ];

        Assert.Equal(1000m, PayrollDeductionLineTotals.AdditionalAmount(dtoLines));
        Assert.Equal(1000m, PayrollDeductionLineTotals.AdditionalAmount(entityLines));
    }
}
