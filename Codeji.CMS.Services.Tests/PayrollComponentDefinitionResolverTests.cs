using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollComponentDefinitionResolverTests
{
    [Fact]
    public void Resolve_UsesLatestEffectiveVersionAndDoesNotFallBackAfterDisable()
    {
        var definitions = new[]
        {
            Definition("company-1", "BONUS", new DateTime(2026, 1, 1), true, 1),
            Definition("company-1", "BONUS", new DateTime(2026, 10, 1), false, 2),
            Definition("company-1", "BONUS", new DateTime(2026, 12, 1), true, 3),
            Definition("company-2", "BONUS", new DateTime(2026, 1, 1), false, 4),
        };

        Assert.Equal(1, PayrollComponentDefinitionResolver.Resolve(definitions, "company-1", "bonus", new DateTime(2026, 9, 30))?.Version);
        Assert.Null(PayrollComponentDefinitionResolver.Resolve(definitions, "company-1", "BONUS", new DateTime(2026, 10, 1)));
        Assert.Equal(3, PayrollComponentDefinitionResolver.Resolve(definitions, "company-1", "BONUS", new DateTime(2026, 12, 1))?.Version);
        Assert.Null(PayrollComponentDefinitionResolver.Resolve(definitions, "company-3", "BONUS", new DateTime(2026, 12, 1)));
    }

    [Fact]
    public void Resolve_DoesNotResurrectAnExpiredLatestVersion()
    {
        var definitions = new[]
        {
            Definition("company-1", "MEAL", new DateTime(2026, 1, 1), true, 1),
            new PayrollSalaryComponentDefinition
            {
                CompanyId = "company-1", Code = "MEAL", Name = "Meal",
                EffectiveFrom = new DateTime(2026, 4, 1), EffectiveTo = new DateTime(2026, 6, 30),
                IsActive = true, Version = 2
            }
        };

        Assert.Null(PayrollComponentDefinitionResolver.Resolve(definitions, "company-1", "MEAL", new DateTime(2026, 7, 1)));
    }

    private static PayrollSalaryComponentDefinition Definition(string companyId, string code, DateTime effectiveFrom, bool isActive, int version) => new()
    {
        CompanyId = companyId, Code = code, Name = code,
        EffectiveFrom = effectiveFrom, IsActive = isActive, Version = version
    };
}
