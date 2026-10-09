using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollDivisorPolicyServiceTests
{
    [Fact]
    public async Task Save_rejects_same_day_overlap_without_deactivating_existing_policy()
    {
        var sameDayActive = new PayrollDivisorPolicy
        {
            Id = "policy-current", CompanyId = "company-1", EffectiveFrom = new DateTime(2026, 9, 1),
            IsActive = true, Version = 1, DivisorPolicy = "CALENDAR_DAYS", LopCalculationBase = "FIXED_GROSS",
            EpfMode = "CAPPED_CEILING", EpfWageCeiling = 15000m, EpfEmployeeRatePercent = 12m, EpfEmployerRatePercent = 12m,
            EsicMode = "STATUTORY", EsicWageCeiling = 21000m, EsicEmployeeRatePercent = 0.75m, EsicEmployerRatePercent = 3.25m,
            TdsMode = "AUTO_NEW_REGIME"
        };
        var conflicting = new PayrollDivisorPolicy
        {
            Id = "policy-conflict", CompanyId = "company-1", EffectiveFrom = new DateTime(2026, 9, 1),
            EffectiveTo = new DateTime(2026, 9, 30), IsActive = true, Version = 2,
            DivisorPolicy = "CALENDAR_DAYS", LopCalculationBase = "FIXED_GROSS",
            EpfMode = "CAPPED_CEILING", EpfWageCeiling = 15000m, EpfEmployeeRatePercent = 12m, EpfEmployerRatePercent = 12m,
            EsicMode = "STATUTORY", EsicWageCeiling = 21000m, EsicEmployeeRatePercent = 0.75m, EsicEmployerRatePercent = 3.25m,
            TdsMode = "AUTO_NEW_REGIME"
        };
        var policies = new List<PayrollDivisorPolicy> { sameDayActive, conflicting };
        var repo = new Mock<IMongoDbRepository<PayrollDivisorPolicy>>();
        repo.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollDivisorPolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => policies);
        repo.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollDivisorPolicy>>(), It.IsAny<PayrollDivisorPolicy>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        repo.Setup(x => x.AddOne(It.IsAny<PayrollDivisorPolicy>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var service = new PayrollDivisorPolicyService(repo.Object);
        var result = await service.Save("company-1", "actor", new PayrollDivisorPolicyDto
        {
            DivisorPolicy = "FIXED_30_DAYS", EffectiveFrom = new DateTime(2026, 9, 1),
            EpfMode = "CAPPED_CEILING", EpfWageCeiling = 15000m,
            EsicMode = "STATUTORY", EsicWageCeiling = 21000m, TdsMode = "AUTO_NEW_REGIME"
        });

        Assert.False(result.Success);
        Assert.Contains("overlaps", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(sameDayActive.IsActive);
        repo.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollDivisorPolicy>>(), It.IsAny<PayrollDivisorPolicy>()), Times.Never);
        repo.Verify(x => x.AddOne(It.IsAny<PayrollDivisorPolicy>()), Times.Never);
    }

    [Fact]
    public async Task Save_restores_same_day_policy_when_new_version_insert_fails()
    {
        var existing = new PayrollDivisorPolicy
        {
            Id = "policy-current", CompanyId = "company-1", EffectiveFrom = new DateTime(2026, 9, 1),
            IsActive = true, Version = 1, DivisorPolicy = "CALENDAR_DAYS", LopCalculationBase = "FIXED_GROSS",
            EpfMode = "CAPPED_CEILING", EpfWageCeiling = 15000m, EpfEmployeeRatePercent = 12m, EpfEmployerRatePercent = 12m,
            EsicMode = "STATUTORY", EsicWageCeiling = 21000m, EsicEmployeeRatePercent = 0.75m, EsicEmployerRatePercent = 3.25m,
            TdsMode = "AUTO_NEW_REGIME"
        };
        var repo = new Mock<IMongoDbRepository<PayrollDivisorPolicy>>();
        var updateStates = new List<bool>();
        repo.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollDivisorPolicy, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(() => new List<PayrollDivisorPolicy> { existing });
        repo.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollDivisorPolicy>>(), It.IsAny<PayrollDivisorPolicy>()))
            .Callback<MongoDB.Driver.FilterDefinition<PayrollDivisorPolicy>, PayrollDivisorPolicy>((_, value) => updateStates.Add(value.IsActive))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        repo.Setup(x => x.AddOne(It.IsAny<PayrollDivisorPolicy>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = false, Message = "insert failed" });

        var service = new PayrollDivisorPolicyService(repo.Object);
        var result = await service.Save("company-1", "actor", new PayrollDivisorPolicyDto
        {
            DivisorPolicy = "FIXED_30_DAYS", EffectiveFrom = new DateTime(2026, 9, 1),
            EpfMode = "CAPPED_CEILING", EpfWageCeiling = 15000m, EpfEmployeeRatePercent = 12m, EpfEmployerRatePercent = 12m,
            EsicMode = "STATUTORY", EsicWageCeiling = 21000m, EsicEmployeeRatePercent = 0.75m, EsicEmployerRatePercent = 3.25m,
            TdsMode = "AUTO_NEW_REGIME"
        });

        Assert.False(result.Success);
        Assert.Equal("insert failed", result.Message);
        Assert.True(existing.IsActive);
        Assert.Equal(new[] { false, true }, updateStates);
        repo.Verify(x => x.AddOne(It.IsAny<PayrollDivisorPolicy>()), Times.Once);
    }

    [Fact]
    public async Task Save_rejects_unapproved_statutory_changes()
    {
        var repo = new Mock<IMongoDbRepository<PayrollDivisorPolicy>>();
        PayrollDivisorPolicy? saved = null;
        repo.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollDivisorPolicy, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(() => saved is null ? new List<PayrollDivisorPolicy>() : new List<PayrollDivisorPolicy> { saved });
        repo.Setup(x => x.AddOne(It.IsAny<PayrollDivisorPolicy>()))
            .Callback<PayrollDivisorPolicy>(value => saved = value)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var service = new PayrollDivisorPolicyService(repo.Object);
        var result = await service.Save("company-1", "actor", new PayrollDivisorPolicyDto
        {
            DivisorPolicy = "CALENDAR_DAYS", EffectiveFrom = new DateTime(2026, 9, 1),
            EsicMode = "STATUTORY", EsicWageCeiling = 24000m
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("effective-dated statutory profile", result.Message);
        Assert.Null(saved);
        var effective = await service.GetEffective("company-1", new DateTime(2026, 9, 15));
        Assert.Equal(21000m, effective.EsicWageCeiling);
    }

    [Fact]
    public async Task Save_allows_non_statutory_changes_when_statutory_values_are_unchanged()
    {
        var repo = new Mock<IMongoDbRepository<PayrollDivisorPolicy>>();
        PayrollDivisorPolicy? saved = null;
        repo.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollDivisorPolicy, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(() => saved is null ? new List<PayrollDivisorPolicy>() : new List<PayrollDivisorPolicy> { saved });
        repo.Setup(x => x.AddOne(It.IsAny<PayrollDivisorPolicy>()))
            .Callback<PayrollDivisorPolicy>(value => saved = value)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var service = new PayrollDivisorPolicyService(repo.Object);
        var result = await service.Save("company-1", "actor", new PayrollDivisorPolicyDto
        {
            DivisorPolicy = "FIXED_30_DAYS", EffectiveFrom = new DateTime(2026, 9, 1),
            EpfMode = "CAPPED_CEILING", EpfWageCeiling = 15000m,
            EsicMode = "STATUTORY", EsicWageCeiling = 21000m,
            TdsMode = "AUTO_NEW_REGIME", RequireAttendanceLock = false
        });

        Assert.True(result.Success);
        Assert.Equal("FIXED_30_DAYS", saved?.DivisorPolicy);
        Assert.False(saved?.RequireAttendanceLock);
    }
}
