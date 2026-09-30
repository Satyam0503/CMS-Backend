using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollComponentCalculationEngineTests
{
    [Fact]
    public void CalculatesFixedComponentPercentagesAndRoundsEachLine()
    {
        var result = PayrollComponentCalculationEngine.Calculate(
        [
            new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000.005m, SortOrder = 10 },
            new() { Code = "HRA", Name = "HRA", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "BASIC", Value = 50, SortOrder = 20 },
            new() { Code = "PF", Name = "PF", Kind = "DEDUCTION", CalculationType = "PERCENT_OF_GROSS", Value = 10, SortOrder = 30 }
        ]);

        Assert.True(result.Success);
        Assert.Equal(1000.01m, result.Values["BASIC"]);
        Assert.Equal(500.01m, result.Values["HRA"]);
        Assert.Equal(150m, result.Values["PF"]);
        Assert.Equal(1500.02m, result.Gross);
        Assert.Equal(150m, result.Deductions);
        Assert.Equal(1350.02m, result.Net);
    }

    [Fact]
    public void PercentOfGrossUsesStableNonGrossEarningSubtotal()
    {
        var result = PayrollComponentCalculationEngine.Calculate(
        [
            new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000, SortOrder = 10 },
            new() { Code = "BONUS", Name = "Bonus", Kind = "EARNING", CalculationType = "PERCENT_OF_GROSS", Value = 10, SortOrder = 20 },
            new() { Code = "ALLOWANCE", Name = "Allowance", Kind = "EARNING", CalculationType = "PERCENT_OF_GROSS", Value = 5, SortOrder = 30 }
        ]);

        Assert.True(result.Success);
        Assert.Equal(100m, result.Values["BONUS"]);
        Assert.Equal(50m, result.Values["ALLOWANCE"]);
        Assert.Equal(1150m, result.Gross);
    }

    [Fact]
    public void RejectsMissingDependency()
    {
        var result = PayrollComponentCalculationEngine.Calculate(
        [
            new() { Code = "HRA", Name = "HRA", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "BASIC", Value = 40 }
        ]);

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_COMPONENT_BASE_MISSING", result.ErrorCode);
    }

    [Fact]
    public void RejectsCircularDependency()
    {
        var result = PayrollComponentCalculationEngine.Calculate(
        [
            new() { Code = "A", Name = "A", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "B", Value = 10 },
            new() { Code = "B", Name = "B", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "A", Value = 10 }
        ]);

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_COMPONENT_CYCLE", result.ErrorCode);
    }

    [Fact]
    public void RejectsDuplicateCodesCaseInsensitively()
    {
        var result = PayrollComponentCalculationEngine.Calculate(
        [
            new() { Code = "basic", Name = "Basic", Kind = "EARNING", Value = 100 },
            new() { Code = "BASIC", Name = "Duplicate", Kind = "EARNING", Value = 200 }
        ]);

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_COMPONENT_DUPLICATE", result.ErrorCode);
    }

    [Fact]
    public void RejectsNegativeValues()
    {
        var result = PayrollComponentCalculationEngine.Calculate(
        [
            new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = -1 }
        ]);

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_COMPONENT_VALUE_INVALID", result.ErrorCode);
    }
}
