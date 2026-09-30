using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class LegacySalaryMigrationMapperTests
{
    [Fact]
    public void Map_ConvertsLegacyFieldsToCanonicalComponents()
    {
        var result = LegacySalaryMigrationMapper.Map(new SalaryModel
        {
            SalaryId = Guid.Parse("11111111-1111-1111-1111-111111111111"), UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            CompanyId = "company-1", EmployeeId = "employee-1", BasicPay = 40000m, Hra = 16000m, OtherAllowances = 9000m,
            Lta = 2000m, Bonus = 5000m, HealthInsurance = 1000m, TotalDeductions = 2500m, GrossSalary = 67000m, Ctc = 804000m,
            EffectiveFrom = new DateTime(2026, 4, 1), Currency = "INR", PaymentFrequency = "Monthly", Status = true
        });

        Assert.Equal("LEGACY_MIGRATION", result.Source);
        Assert.Equal("APPROVED", result.ApprovalStatus);
        Assert.Equal(7, result.Components.Count);
        Assert.Equal(40000m, result.Components.Single(x => x.ComponentCode == "BASIC").Value);
        Assert.Equal("DEDUCTION", result.Components.Single(x => x.ComponentCode == "HEALTH_INSURANCE").Kind);
        Assert.Equal(2500m, result.Components.Single(x => x.ComponentCode == "LEGACY_TOTAL_DEDUCTIONS").Value);
    }

    [Fact]
    public void Map_RejectsMissingTenantOrEmployeeIdentity()
    {
        Assert.Throws<ArgumentException>(() => LegacySalaryMigrationMapper.Map(new SalaryModel { EmployeeId = "employee-1" }));
        Assert.Throws<ArgumentException>(() => LegacySalaryMigrationMapper.Map(new SalaryModel { CompanyId = "company-1" }));
    }
}

