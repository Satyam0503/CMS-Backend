using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollRunCalculationServiceTests
{
    [Fact]
    public async Task SyncCalculatedRows_UsesImmutableComponentSnapshotAndRoundsTotals()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 1), Status = "DRAFT"
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1",
            InputSnapshot = new PayrollEmployeeInputSnapshot
            {
                Components =
                [
                    new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 1000.005m, SortOrder = 10 },
                    new() { Code = "HRA", Name = "HRA", Kind = "EARNING", CalculationType = "PERCENT_OF_COMPONENT", BaseComponentCode = "BASIC", Value = 50, SortOrder = 20 },
                    new() { Code = "PF", Name = "PF", Kind = "DEDUCTION", CalculationType = "PERCENT_OF_GROSS", Value = 10, SortOrder = 30 }
                ]
            }
        };
        var row = new EmpPayRoll
        {
            Id = "legacy-row", CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1),
            BasicPay = 1, PaidDays = 30
        };
        var runs = Repository(run);
        var lines = Repository(member);
        var payrolls = Repository(row);
        var service = new PayrollRunCalculationService(runs.Object, lines.Object, payrolls.Object);

        var result = await service.SyncCalculatedRows("company-1", "run-1", "actor-1");

        Assert.True(result.Success);
        Assert.Equal("CALCULATED", member.Status);
        Assert.Equal(1500.02m, member.GrossAmount);
        Assert.Equal(150m, member.DeductionAmount);
        Assert.Equal(1350.02m, member.NetAmount);
        Assert.Equal(1500.02m, run.GrossTotal);
        Assert.Equal(150m, run.DeductionTotal);
        Assert.Equal(1350.02m, run.NetTotal);
        Assert.Equal("CALCULATED", run.Status);
        lines.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRunEmployee>>(), member), Times.Once);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), run), Times.Once);
    }

    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(params TEntity[] items)
    {
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) =>
                Task.FromResult(items.FirstOrDefault(predicate.Compile())));
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<TEntity>>(items.Where(predicate.Compile()).ToArray()));
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<TEntity>>(), It.IsAny<TEntity>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        return repository;
    }
}
