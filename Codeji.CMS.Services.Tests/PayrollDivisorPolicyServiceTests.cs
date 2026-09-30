using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollDivisorPolicyServiceTests
{
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
