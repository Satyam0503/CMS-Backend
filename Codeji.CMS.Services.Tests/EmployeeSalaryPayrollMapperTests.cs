using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeSalaryPayrollMapperTests
{
    [Fact]
    public void Calculates_salary_component_formulas_before_mapping_monthly_pay()
    {
        var structure = new EmployeeSalaryStructure
        {
            Id = "11111111-1111-1111-1111-111111111111", CompanyId = "company-1", EmployeeId = "employee-1", Status = "ACTIVE", ApprovalStatus = "APPROVED",
            EffectiveFrom = new DateTime(2026, 10, 1),
            Components =
            [
                Component("BASIC", "Basic", "EARNING", 30000m),
                new EmployeeSalaryComponentValue { ComponentCode = "HRA", ComponentName = "HRA", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "BASIC", Value = 40m },
                new EmployeeSalaryComponentValue { ComponentCode = "MEAL_ALLOWANCE", ComponentName = "Meal", Kind = "EARNING", CalculationType = "PERCENT_OF_GROSS", Value = 5m }
            ]
        };

        var payrollSalary = EmployeeSalaryPayrollMapper.Map(structure);

        Assert.Equal(30000m, payrollSalary.BasicPay);
        Assert.Equal(12000m, payrollSalary.Hra);
        Assert.Equal(2100m, payrollSalary.OtherAllowances);
        Assert.Equal(2100m, Assert.Single(payrollSalary.OtherEarnings).Amount);
        Assert.Equal("Meal", payrollSalary.OtherEarnings[0].Name);
        Assert.Equal(44100m, payrollSalary.GrossSalary);
        Assert.Null(payrollSalary.UserId);
    }

    [Fact]
    public void Maps_custom_recurring_earnings_and_employee_deductions_into_legacy_payroll_inputs()
    {
        var structure = new EmployeeSalaryStructure
        {
            Id = "22222222-2222-2222-2222-222222222222", CompanyId = "company-1", EmployeeId = "employee-1", Status = "ACTIVE", ApprovalStatus = "APPROVED",
            EffectiveFrom = new DateTime(2026, 10, 1), Currency = "INR", PaymentFrequency = "MONTHLY",
            Components =
            [
                Component("BASIC", "Basic", "EARNING", 30000m),
                Component("HRA", "HRA", "EARNING", 5000m),
                Component("MEAL_ALLOWANCE", "Meal Allowance", "EARNING", 2000m),
                Component("BONUS", "Bonus", "EARNING", 3000m, recurring: false),
                Component("HEALTH_INSURANCE", "Health Insurance", "DEDUCTION", 500m),
                Component("LOAN_REPAYMENT", "Loan Repayment", "DEDUCTION", 1000m),
                Component("EMPLOYER_EPF", "Employer PF", "EARNING", 3600m, employer: true),
                Component("TDS", "Income Tax", "DEDUCTION", 2500m, statutory: true)
            ]
        };

        var payrollSalary = EmployeeSalaryPayrollMapper.Map(structure);

        Assert.Equal(30000m, payrollSalary.BasicPay);
        Assert.Null(payrollSalary.UserId);
        Assert.Equal(2000m, payrollSalary.OtherAllowances);
        Assert.Equal(2000m, Assert.Single(payrollSalary.OtherEarnings).Amount);
        Assert.Equal("Meal Allowance", payrollSalary.OtherEarnings[0].Name);
        Assert.Equal(0m, payrollSalary.Bonus);
        Assert.Equal(37000m, payrollSalary.GrossSalary);
        Assert.Equal(500m, payrollSalary.HealthInsurance);
        Assert.Equal(1000m, payrollSalary.OtherDeductions.Single().Amount);
        Assert.Equal(1500m, payrollSalary.TotalDeductions);
        Assert.DoesNotContain(payrollSalary.OtherDeductions, x => x.Code is "EMPLOYER_EPF" or "TDS");
    }

    [Fact]
    public void Rejects_salary_structure_with_invalid_identifier_instead_of_inventing_a_new_id()
    {
        var structure = new EmployeeSalaryStructure
        {
            Id = "not-a-guid", CompanyId = "company-1", EmployeeId = "employee-1",
            Components = [Component("BASIC", "Basic", "EARNING", 30000)]
        };

        var exception = Assert.Throws<InvalidOperationException>(() => EmployeeSalaryPayrollMapper.Map(structure));

        Assert.Contains("invalid identifier", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("employee-1", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("EARNING", true, false, false, 30500, 0, 500)]
    [InlineData("DEDUCTION", true, false, false, 30000, 500, 0)]
    [InlineData("DEDUCTION", false, false, false, 30000, 0, 0)]
    [InlineData("EARNING", true, true, false, 30000, 0, 0)]
    [InlineData("DEDUCTION", true, false, true, 30000, 0, 0)]
    public void Health_insurance_respects_kind_recurrence_and_ownership(string kind, bool recurring, bool employer, bool statutory,
        int expectedGross, int expectedDeduction, int expectedOther)
    {
        var structure = new EmployeeSalaryStructure { Id = "33333333-3333-3333-3333-333333333333", CompanyId = "company-1", EmployeeId = "employee-1", Components =
        [Component("BASIC", "Basic", "EARNING", 30000), Component("HEALTH_INSURANCE", "Health", kind, 500, recurring, employer, statutory)] };
        var mapped = EmployeeSalaryPayrollMapper.Map(structure);
        Assert.Equal(expectedGross, mapped.GrossSalary);
        Assert.Equal(expectedDeduction, mapped.HealthInsurance);
        Assert.Equal(expectedDeduction, mapped.TotalDeductions);
        Assert.Equal(expectedOther, mapped.OtherAllowances);
        Assert.Equal(expectedGross - expectedDeduction, mapped.NetSalary);
    }

    [Theory]
    [InlineData(true, 33000, 3000)]
    [InlineData(false, 30000, 0)]
    public void Bonus_is_paid_monthly_only_when_recurring(bool recurring, int expectedGross, int expectedBonus)
    {
        var structure = new EmployeeSalaryStructure { Id = "44444444-4444-4444-4444-444444444444", CompanyId = "company-1", EmployeeId = "employee-1", Components =
        [Component("BASIC", "Basic", "EARNING", 30000), Component("BONUS", "Bonus", "EARNING", 3000, recurring),
         Component("ONE_TIME_RECOVERY", "Recovery", "DEDUCTION", 1500, recurring: false)] };
        var mapped = EmployeeSalaryPayrollMapper.Map(structure);
        Assert.Equal(expectedBonus, mapped.Bonus);
        Assert.Equal(expectedGross, mapped.GrossSalary);
        Assert.Empty(mapped.OtherDeductions);
        Assert.Equal(0m, mapped.TotalDeductions);
    }

    private static EmployeeSalaryComponentValue Component(string code, string name, string kind, decimal amount,
        bool recurring = true, bool employer = false, bool statutory = false) => new()
    {
        ComponentCode = code, ComponentName = name, Kind = kind, Value = amount, CalculationType = "FIXED",
        IsRecurring = recurring, IsEmployerContribution = employer, IsStatutory = statutory
    };
}
