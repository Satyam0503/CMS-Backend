using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollPaymentBatchServiceTests
{
    [Fact]
    public async Task Create_RejectsUnprocessedRun()
    {
        var run = Run("r", "c", "DRAFT");
        var service = new PayrollPaymentBatchService(Repo<PayrollPaymentBatch>().Object, Repo(run).Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Create("c", "r", "a");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Create_AllowsOnlyCompleteProcessedPopulation()
    {
        var run = Run("r", "c", "PROCESSED");
        run.EmployeeCount = 1;
        var row = new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "r", EmployeeId = "e", Status = "CALCULATED", NetAmount = 100.005m };
        var batches = Repo<PayrollPaymentBatch>();
        var service = new PayrollPaymentBatchService(batches.Object, Repo(run).Object, Repo(row).Object);

        var result = await service.Create("c", "r", "a");

        Assert.True(result.Success);
        Assert.Equal(100.01m, result.MethodResult!.TotalNetAmount);
        Assert.Equal(100.01m, result.MethodResult.Items.Single().NetAmount);
        batches.Verify(x => x.AddOne(It.IsAny<PayrollPaymentBatch>()), Times.Once);
    }

    [Fact]
    public async Task Submit_RequiresProviderReference()
    {
        var batch = new PayrollPaymentBatch { Id = "b", CompanyId = "c", PayrollRunId = "r", Status = "DRAFT" };
        var batches = Repo(batch);
        var service = new PayrollPaymentBatchService(batches.Object, Repo<PayrollRun>().Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Submit("c", "b", "a", new PaymentBatchRequestDto());

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        batches.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollPaymentBatch>>(), It.IsAny<PayrollPaymentBatch>()), Times.Never);
    }

    [Fact]
    public async Task Reconcile_RejectsRunThatIsNoLongerProcessedBeforeMutatingBatch()
    {
        var batch = new PayrollPaymentBatch { Id = "b", CompanyId = "c", PayrollRunId = "r", Status = "SUBMITTED" };
        var batches = Repo(batch);
        var service = new PayrollPaymentBatchService(batches.Object, Repo(Run("r", "c", "PAID")).Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Reconcile("c", "b", "a", new PaymentBatchRequestDto());

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        batches.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollPaymentBatch>>(), It.IsAny<PayrollPaymentBatch>()), Times.Never);
    }

    [Fact]
    public async Task Reconcile_MarksItemsAndRunPaid()
    {
        var batch = new PayrollPaymentBatch
        {
            Id = "b", CompanyId = "c", PayrollRunId = "r", Status = "SUBMITTED",
            Items = [new PayrollPaymentBatchItem { EmployeeId = "e", NetAmount = 100 }]
        };
        var run = Run("r", "c", "PROCESSED");
        var batches = Repo(batch);
        var runs = Repo(run);
        var service = new PayrollPaymentBatchService(batches.Object, runs.Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Reconcile("c", "b", "a", new PaymentBatchRequestDto { Note = "Bank confirmed" });

        Assert.True(result.Success);
        Assert.Equal("RECONCILED", batch.Status);
        Assert.Equal("RECONCILED", batch.Items.Single().Status);
        Assert.Equal("PAID", run.Status);
        runs.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollRun>>(), run), Times.Once);
    }

    private static PayrollRun Run(string id, string companyId, string status) => new()
    {
        Id = id, CompanyId = companyId, PeriodStart = DateTime.Today,
        PeriodEnd = DateTime.Today, PayDate = DateTime.Today, Status = status
    };

    private static Mock<IMongoDbRepository<TEntity>> Repo<TEntity>(params TEntity[] items)
    {
        var m = new Mock<IMongoDbRepository<TEntity>>();
        m.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _) => items.Any(p.Compile()));
        m.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _, bool __) => items.FirstOrDefault(p.Compile()));
        m.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> p, bool _, bool __) => items.Where(p.Compile()).ToArray());
        m.Setup(x => x.AddOne(It.IsAny<TEntity>())).ReturnsAsync(new Result { Success = true });
        m.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<TEntity>>(), It.IsAny<TEntity>()))
            .ReturnsAsync(new Result { Success = true });
        return m;
    }
}
