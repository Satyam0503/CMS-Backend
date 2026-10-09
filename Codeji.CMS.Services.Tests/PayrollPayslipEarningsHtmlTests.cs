using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollPayslipEarningsHtmlTests
{
    [Fact]
    public void RendersNamedEarningsAndHtmlEncodesComponentNames()
    {
        var payroll = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 10, 1), PaidDate = new DateTime(2026, 10, 31),
            BasicPay = 30000m, PaidDays = 30,
            EarningLines = [new() { Code = "MEAL", Description = "Meal <script>", Amount = 1250.5m }]
        };

        var html = PayrollPayslipEarningsHtml.RenderOtherAllowanceRows(payroll);

        Assert.Contains("Meal &lt;script&gt;", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("1,250.50", html);
        Assert.DoesNotContain("Other Allowance", html);
    }

    [Fact]
    public void KeepsLegacyOtherAllowanceVisibleWhenNoBreakdownWasSaved()
    {
        var payroll = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 10, 1), PaidDate = new DateTime(2026, 10, 31),
            BasicPay = 30000m, PaidDays = 30,
            Allowance = new Allowance { OtherAllowance = 2000m }
        };

        var html = PayrollPayslipEarningsHtml.RenderOtherAllowanceRows(payroll);

        Assert.Contains("Other Allowance", html);
        Assert.Contains("2,000.00", html);
    }

    [Fact]
    public void EarningsBreakdownSurvivesBsonRoundTrip()
    {
        var payroll = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 10, 1), PaidDate = new DateTime(2026, 10, 31), BasicPay = 30000m, PaidDays = 30,
            EarningLines = [new() { Code = "MEAL", Description = "Meal allowance", Amount = 1200m }]
        };

        var restored = BsonSerializer.Deserialize<EmpPayRoll>(payroll.ToBson());

        Assert.Equal("MEAL", Assert.Single(restored.EarningLines).Code);
        Assert.Equal(1200m, restored.EarningLines[0].Amount);
    }

    [Fact]
    public void Renders_named_additional_deductions_and_excludes_lop_audit_duplicate()
    {
        var payroll = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 10, 1), PaidDate = new DateTime(2026, 10, 31),
            BasicPay = 30000m, PaidDays = 30,
            DeductionLines =
            [
                new() { Code = "LOAN", Description = "Loan <recovery>", Amount = 1500m },
                new() { Code = "LHD_ED_POLICY", Description = "LOP audit detail", Amount = 250m }
            ]
        };

        var html = PayrollPayslipEarningsHtml.RenderOtherDeductionRows(payroll);

        Assert.Contains("Loan &lt;recovery&gt;", html);
        Assert.Contains("1,500.00", html);
        Assert.DoesNotContain("LOP audit detail", html);
        Assert.DoesNotContain("250.00", html);
    }

    [Fact]
    public void Keeps_legacy_aggregate_deduction_row_when_no_breakdown_exists()
    {
        var payroll = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 10, 1), PaidDate = new DateTime(2026, 10, 31),
            BasicPay = 30000m, PaidDays = 30,
            DeductionLines = []
        };

        var html = PayrollPayslipEarningsHtml.RenderOtherDeductionRows(payroll);

        Assert.Contains("Other Salary Deductions", html);
        Assert.Contains("0.00", html);
    }

    [Theory]
    [InlineData(0.75, "ESIC Employee (0.75%)")]
    [InlineData(1.25, "ESIC Employee (1.25%)")]
    public void Uses_frozen_esic_employee_rate_in_payslip_label(decimal rate, string expected)
    {
        var payroll = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 10, 1), PaidDate = new DateTime(2026, 10, 31),
            BasicPay = 30000m, PaidDays = 30,
            CalculationSnapshot = new PayrollCalculationSnapshot { EsicEmployeeRatePercent = rate }
        };

        Assert.Equal(expected, PayrollPayslipEarningsHtml.GetEsicEmployeeLabel(payroll));
    }
}
