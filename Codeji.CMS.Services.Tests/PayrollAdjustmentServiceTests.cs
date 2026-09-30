using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollAdjustmentServiceTests
{
    [Fact]
    public async Task Create_RejectsDraftOriginalRun()
    {
        var run = Run("r", "c", "DRAFT");
        var service = new PayrollAdjustmentService(Repo<PayrollAdjustment>().Object, Repo(run).Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Create("c", "a", new CreatePayrollAdjustmentRequestDto
        {
            OriginalPayrollRunId = "r", EmployeeId = "e", Amount = 1, Reason = "late input"
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Create_RequiresEmployeeInOriginalRun()
    {
        var run = Run("r", "c", "PAID");
        var service = new PayrollAdjustmentService(Repo<PayrollAdjustment>().Object, Repo(run).Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Create("c", "a", new CreatePayrollAdjustmentRequestDto
        {
            OriginalPayrollRunId = "r", EmployeeId = "e", Amount = 1, Reason = "late input", Type = "ARREARS"
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Process_RequiresCalculatedCorrectionEmployee()
    {
        var adjustment = new PayrollAdjustment
        {
            Id = "a", CompanyId = "c", OriginalPayrollRunId = "original", EmployeeId = "e",
            Status = "APPROVED", Amount = 10, Reason = "correction"
        };
        var correction = Run("correction", "c", "CALCULATED"); correction.RunType = "CORRECTION";
        var blocked = new PayrollRunEmployee
        {
            CompanyId = "c", PayrollRunId = "correction", EmployeeId = "e", Status = "BLOCKED"
        };
        var service = new PayrollAdjustmentService(Repo(adjustment).Object, Repo(correction).Object, Repo(blocked).Object);

        var result = await service.Process("c", "actor", "a", new PayrollAdjustmentActionDto { CorrectionPayrollRunId = "correction" });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    private static PayrollRun Run(string id, string companyId, string status) => new()
    {
        Id = id, CompanyId = companyId, PeriodStart = DateTime.Today,
        PeriodEnd = DateTime.Today, PayDate = DateTime.Today, Status = status
    };

    private static Mock<IMongoDbRepository<TEntity>> Repo<TEntity>(params TEntity[] items)
    {
        var m = new Mock<IMongoDbRepository<TEntity>>();
        m.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _, bool __) => items.FirstOrDefault(p.Compile()));
        m.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _) => items.Any(p.Compile()));
        m.Setup(x => x.AddOne(It.IsAny<TEntity>())).ReturnsAsync(new Result { Success = true });
        m.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<TEntity>>(), It.IsAny<TEntity>()))
            .ReturnsAsync(new Result { Success = true });
        return m;
    }
}

