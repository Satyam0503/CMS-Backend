using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollSalaryValueResolverTests
{
    [Fact]
    public void Approved_employee_structure_takes_precedence_over_assigned_template()
    {
        Assert.Equal(42000m, PayrollSalaryValueResolver.Resolve(true, 30000m, 42000m, 28000m));
    }

    [Fact]
    public void Template_is_fallback_when_employee_has_no_approved_structure()
    {
        Assert.Equal(30000m, PayrollSalaryValueResolver.Resolve(false, 30000m, 42000m, 28000m));
        Assert.Equal(28000m, PayrollSalaryValueResolver.Resolve(false, null, 42000m, 28000m));
    }
}
