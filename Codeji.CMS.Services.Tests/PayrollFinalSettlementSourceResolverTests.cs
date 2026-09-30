using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollFinalSettlementSourceResolverTests
{
    [Fact]
    public async Task Resolve_IsTenantScopedAndUsesCalculatedLineSnapshot()
    {
        var month = new DateTime(2026, 9, 1);
        var runs = Repository(
            new PayrollRun { Id = "run-c1", CompanyId = "company-1", PeriodStart = month, PeriodEnd = month.AddMonths(1).AddDays(-1), PayDate = month.AddMonths(1), RunType = "FINAL_SETTLEMENT", Status = "CALCULATED", CreatedAtUtc = month.AddDays(10) },
            new PayrollRun { Id = "run-c2", CompanyId = "company-2", PeriodStart = month, PeriodEnd = month.AddMonths(1).AddDays(-1), PayDate = month.AddMonths(1), RunType = "FINAL_SETTLEMENT", Status = "CALCULATED", CreatedAtUtc = month.AddDays(20) });
        var lines = Repository(
            new PayrollRunEmployee { Id = "line-c1", CompanyId = "company-1", PayrollRunId = "run-c1", EmployeeId = "emp-1", Status = "CALCULATED", GrossAmount = 2500.25m, DeductionAmount = 275.25m, NetAmount = 2225m },
            new PayrollRunEmployee { Id = "line-c2", CompanyId = "company-2", PayrollRunId = "run-c2", EmployeeId = "emp-1", Status = "CALCULATED", GrossAmount = 9999m, DeductionAmount = 1m, NetAmount = 9998m });

        var result = await new PayrollFinalSettlementSourceResolver(runs.Object, lines.Object).Resolve("company-1", "emp-1", month);

        Assert.NotNull(result);
        Assert.Equal("run-c1", result!.PayrollRunId);
        Assert.Equal("line-c1", result.PayrollRunEmployeeId);
        Assert.Equal(2500.25m, result.GrossAmount);
        Assert.Equal(275.25m, result.DeductionAmount);
        Assert.False(result.AlreadyProcessed);
    }

    [Fact]
    public async Task Resolve_MarksProcessedRunAsConsumedToPreventDoubleCounting()
    {
        var month = new DateTime(2026, 9, 1);
        var runs = Repository(new PayrollRun { Id = "run-paid", CompanyId = "company-1", PeriodStart = month, PeriodEnd = month.AddMonths(1).AddDays(-1), PayDate = month.AddMonths(1), RunType = "FINAL_SETTLEMENT", Status = "PAID" });
        var lines = Repository(new PayrollRunEmployee { Id = "line-paid", CompanyId = "company-1", PayrollRunId = "run-paid", EmployeeId = "emp-1", Status = "CALCULATED", GrossAmount = 1800m, DeductionAmount = 100m, NetAmount = 1700m });

        var result = await new PayrollFinalSettlementSourceResolver(runs.Object, lines.Object).Resolve("company-1", "emp-1", month);

        Assert.NotNull(result);
        Assert.True(result!.AlreadyProcessed);
    }

    [Fact]
    public async Task Resolve_ReturnsNullWhenNoEmployeeLineExists()
    {
        var month = new DateTime(2026, 9, 1);
        var runs = Repository(new PayrollRun { Id = "run-1", CompanyId = "company-1", PeriodStart = month, PeriodEnd = month.AddMonths(1).AddDays(-1), PayDate = month.AddMonths(1), RunType = "FINAL_SETTLEMENT", Status = "CALCULATED" });
        var lines = Repository(new PayrollRunEmployee { Id = "line-other", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "other", Status = "CALCULATED" });

        var result = await new PayrollFinalSettlementSourceResolver(runs.Object, lines.Object).Resolve("company-1", "emp-1", month);

        Assert.Null(result);
    }

    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(params TEntity[] items)
    {
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<TEntity>>(items.Where(predicate.Compile()).ToArray()));
        return repository;
    }
}