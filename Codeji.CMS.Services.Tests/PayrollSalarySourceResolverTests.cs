using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollSalarySourceResolverTests
{
    [Fact]
    public void SnapshotLegacyRecord_PreservesCompensationValuesAndEffectiveWindow()
    {
        var from = new DateTime(2026, 9, 1);
        var to = new DateTime(2026, 9, 30);
        var salary = new SalaryModel
        {
            SalaryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), EffectiveFrom = from, EffectiveTo = to,
            BasicPay = 50000m, Hra = 10000m, Lta = 1000m, OtherAllowances = 2000m, Bonus = 3000m, HealthInsurance = 500m
        };

        var snapshot = PayrollSalarySourceResolver.SnapshotLegacyRecord(salary);

        Assert.Equal(salary.SalaryId.ToString("D"), snapshot.SalaryStructureId);
        Assert.Equal(0, snapshot.Version);
        Assert.Equal(from, snapshot.EffectiveFrom);
        Assert.Equal(to, snapshot.EffectiveTo);
        Assert.Equal(50000m, Assert.Single(snapshot.Components.Where(x => x.Code == "BASIC_PAY")).Value);
        Assert.Equal(500m, Assert.Single(snapshot.Components.Where(x => x.Code == "HEALTH_INSURANCE")).Value);
    }

    [Fact]
    public void Resolve_DoesNotReviveExpiredLegacySalaryAfterCanonicalTimelineStarted()
    {
        var canonical = Structure("company-1", "employee-1", new DateTime(2026, 4, 1), new DateTime(2026, 9, 30));
        var legacyLoads = 0;

        var result = PayrollSalarySourceResolver.Resolve(
            "company-1", "employee-1", new DateTime(2026, 10, 1), new DateTime(2026, 10, 31),
            [canonical], () => { legacyLoads++; return [Legacy("company-1", "employee-1", 90000m)]; });

        Assert.True(result.UsesCanonicalSource);
        Assert.Empty(result.Records);
        Assert.Equal(0, legacyLoads);
    }

    [Fact]
    public void Resolve_UsesLegacyForPeriodBeforeFirstFutureCanonicalSalary()
    {
        var futureCanonical = Structure("company-1", "employee-1", new DateTime(2026, 11, 1));
        var legacy = Legacy("company-1", "employee-1", 75000m);

        var result = PayrollSalarySourceResolver.Resolve(
            "company-1", "employee-1", new DateTime(2026, 10, 1), new DateTime(2026, 10, 31),
            [futureCanonical], () => [legacy]);

        Assert.False(result.UsesCanonicalSource);
        Assert.Same(legacy, Assert.Single(result.Records));
    }

    [Fact]
    public void Resolve_PrefersEffectiveCanonicalSalaryAndNeverLoadsLegacy()
    {
        var canonical = Structure("company-1", "employee-1", new DateTime(2026, 10, 1));
        var legacyLoads = 0;

        var result = PayrollSalarySourceResolver.Resolve(
            "company-1", "employee-1", new DateTime(2026, 10, 1), new DateTime(2026, 10, 31),
            [canonical], () => { legacyLoads++; return []; });

        Assert.True(result.UsesCanonicalSource);
        Assert.Equal(50000m, Assert.Single(result.Records).BasicPay);
        Assert.Equal(0, legacyLoads);
    }

    [Fact]
    public void Resolve_DoesNotTreatAnotherTenantCanonicalSalaryAsAuthority()
    {
        var otherTenantCanonical = Structure("company-2", "employee-1", new DateTime(2026, 4, 1));
        var legacy = Legacy("company-1", "employee-1", 60000m);

        var result = PayrollSalarySourceResolver.Resolve(
            "company-1", "employee-1", new DateTime(2026, 10, 1), new DateTime(2026, 10, 31),
            [otherTenantCanonical], () => [legacy]);

        Assert.False(result.UsesCanonicalSource);
        Assert.Same(legacy, Assert.Single(result.Records));
    }

    private static EmployeeSalaryStructure Structure(string companyId, string employeeId, DateTime from, DateTime? to = null) => new()
    {
        Id = "77777777-7777-7777-7777-777777777777",
        CompanyId = companyId, EmployeeId = employeeId, EffectiveFrom = from, EffectiveTo = to,
        Status = "ACTIVE", ApprovalStatus = "APPROVED", Version = 1,
        Components = [new EmployeeSalaryComponentValue { ComponentCode = "BASIC", ComponentName = "Basic", Value = 50000m }]
    };

    private static SalaryModel Legacy(string companyId, string employeeId, decimal basic) => new()
    {
        CompanyId = companyId, EmployeeId = employeeId, BasicPay = basic,
        EffectiveFrom = new DateTime(2025, 1, 1), PaymentFrequency = "MONTHLY", Currency = "INR"
    };
}
