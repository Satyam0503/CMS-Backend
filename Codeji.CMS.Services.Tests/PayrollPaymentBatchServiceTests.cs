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
        run.EmployeeCount = 1; run.ReadyCount = 1; run.GrossTotal = 200m; run.DeductionTotal = 100m; run.NetTotal = 100.01m;
        var row = new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "r", EmployeeId = "e", Status = "CALCULATED", GrossAmount = 200m, DeductionAmount = 99.995m, NetAmount = 100.005m };
        var batches = Repo<PayrollPaymentBatch>();
        var service = new PayrollPaymentBatchService(batches.Object, Repo(run).Object, Repo(row).Object);

        var result = await service.Create("c", "r", "a");

        Assert.True(result.Success);
        Assert.Equal(100.01m, result.MethodResult!.TotalNetAmount);
        Assert.Equal(100.01m, result.MethodResult.Items.Single().NetAmount);
        batches.Verify(x => x.AddOne(It.IsAny<PayrollPaymentBatch>()), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsWinningBatchForConcurrentRequest()
    {
        var run = Run("r", "c", "PROCESSED");
        run.EmployeeCount = 1; run.ReadyCount = 1; run.GrossTotal = 200m; run.DeductionTotal = 100m; run.NetTotal = 100m;
        var row = new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "r", EmployeeId = "e", Status = "CALCULATED", GrossAmount = 200m, DeductionAmount = 100m, NetAmount = 100m };
        var winningBatch = new PayrollPaymentBatch { Id = "batch-winner", CompanyId = "c", PayrollRunId = "r", EmployeeCount = 1, TotalNetAmount = 100 };
        var batches = new Mock<IMongoDbRepository<PayrollPaymentBatch>>();
        batches.Setup(x => x.Exist(It.IsAny<Expression<Func<PayrollPaymentBatch, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);
        batches.Setup(x => x.AddOne(It.IsAny<PayrollPaymentBatch>())).ReturnsAsync(new Result { Success = false, StatusCode = 500, Message = "E11000 duplicate key index: ux_payrollpaymentbatch_company_run" });
        batches.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollPaymentBatch, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(winningBatch);
        var service = new PayrollPaymentBatchService(batches.Object, Repo(run).Object, Repo(row).Object);

        var result = await service.Create("c", "r", "a");

        Assert.True(result.Success);
        Assert.Equal("batch-winner", result.ResourceId);
    }

    [Fact]
    public async Task Create_AllowsSkippedEmployeesButPaysOnlyCalculatedRows()
    {
        var run = Run("r", "c", "PROCESSED");
        run.EmployeeCount = 2; run.ReadyCount = 1; run.SkippedCount = 1;
        run.GrossTotal = 200m; run.DeductionTotal = 50m; run.NetTotal = 150m;
        var rows = Repo(
            new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "r", EmployeeId = "paid", Status = "CALCULATED", GrossAmount = 200m, DeductionAmount = 50m, NetAmount = 150m },
            new PayrollRunEmployee { CompanyId = "c", PayrollRunId = "r", EmployeeId = "skipped", Status = "SKIPPED" });
        var batches = Repo<PayrollPaymentBatch>();
        var service = new PayrollPaymentBatchService(batches.Object, Repo(run).Object, rows.Object);

        var result = await service.Create("c", "r", "admin");

        Assert.True(result.Success);
        Assert.Equal(1, result.MethodResult!.EmployeeCount);
        Assert.Equal(150m, result.MethodResult.TotalNetAmount);
        Assert.Equal("paid", Assert.Single(result.MethodResult.Items).EmployeeId);
    }

    [Fact]
    public async Task Create_RejectsPaymentRowsThatNoLongerReconcileWithApprovedRun()
    {
        var run = Run("r", "c", "PROCESSED");
        run.EmployeeCount = 1; run.ReadyCount = 1;
        run.GrossTotal = 200m; run.DeductionTotal = 50m; run.NetTotal = 150m;
        var batches = Repo<PayrollPaymentBatch>();
        var rows = Repo(new PayrollRunEmployee
        {
            CompanyId = "c", PayrollRunId = "r", EmployeeId = "e", Status = "CALCULATED",
            GrossAmount = 200m, DeductionAmount = 50m, NetAmount = 149m
        });
        var service = new PayrollPaymentBatchService(batches.Object, Repo(run).Object, rows.Object);

        var result = await service.Create("c", "r", "admin");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        batches.Verify(x => x.AddOne(It.IsAny<PayrollPaymentBatch>()), Times.Never);
    }

    [Fact]
    public async Task Create_RejectsDuplicateEmployeePaymentRows()
    {
        var run = Run("r", "c", "PROCESSED");
        run.EmployeeCount = 2; run.ReadyCount = 2;
        run.GrossTotal = 200m; run.DeductionTotal = 0m; run.NetTotal = 200m;
        var row = new PayrollRunEmployee
        {
            CompanyId = "c", PayrollRunId = "r", EmployeeId = "e", Status = "CALCULATED",
            GrossAmount = 100m, DeductionAmount = 0m, NetAmount = 100m
        };
        var batches = Repo<PayrollPaymentBatch>();
        var service = new PayrollPaymentBatchService(batches.Object, Repo(run).Object, Repo(row, row).Object);

        var result = await service.Create("c", "r", "admin");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        batches.Verify(x => x.AddOne(It.IsAny<PayrollPaymentBatch>()), Times.Never);
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

        var result = await service.Reconcile("c", "b", "a", new PaymentBatchRequestDto { Note = "Verified", PaymentConfirmed = true });

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

        var result = await service.Reconcile("c", "b", "a", new PaymentBatchRequestDto { Note = "Bank confirmed", PaymentConfirmed = true });

        Assert.True(result.Success);
        Assert.Equal("RECONCILED", batch.Status);
        Assert.Equal("RECONCILED", batch.Items.Single().Status);
        Assert.Equal("PAID", run.Status);
        runs.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollRun>>(), run), Times.Once);
    }

    [Fact]
    public async Task Reconcile_RequiresServerSidePaymentConfirmation()
    {
        var batch = new PayrollPaymentBatch { Id = "b", CompanyId = "c", PayrollRunId = "r", Status = "SUBMITTED" };
        var batches = Repo(batch);
        var service = new PayrollPaymentBatchService(batches.Object, Repo(Run("r", "c", "PROCESSED")).Object, Repo<PayrollRunEmployee>().Object);

        var result = await service.Reconcile("c", "b", "a", new PaymentBatchRequestDto { Note = "Bank confirmed" });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("SUBMITTED", batch.Status);
        batches.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollPaymentBatch>>(), It.IsAny<PayrollPaymentBatch>()), Times.Never);
    }

    [Fact]
    public async Task Reconcile_ResumesAfterRunWriteFailureWithoutDuplicatingAudit()
    {
        var batch = new PayrollPaymentBatch { Id = "b", CompanyId = "c", PayrollRunId = "r", Status = "SUBMITTED" };
        var run = Run("r", "c", "PROCESSED");
        var batches = Repo(batch);
        var runs = Repo(run);
        runs.SetupSequence(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()))
            .ReturnsAsync(new Result { Success = false, StatusCode = 500 })
            .ReturnsAsync(new Result { Success = true });
        var service = new PayrollPaymentBatchService(batches.Object, runs.Object, Repo<PayrollRunEmployee>().Object);
        var request = new PaymentBatchRequestDto { Note = "Bank settlement confirmed", PaymentConfirmed = true };

        var interrupted = await service.Reconcile("c", "b", "a", request);
        Assert.False(interrupted.Success);
        Assert.Equal("RECONCILING", batch.Status);
        run.Status = "PROCESSED"; run.Version = 1; run.AuditHistory.Clear(); // Restore persisted state after the mocked failed write.

        var resumed = await service.Reconcile("c", "b", "retry-actor", request);

        Assert.True(resumed.Success);
        Assert.Equal("RECONCILED", batch.Status);
        Assert.Equal("PAID", run.Status);
        Assert.Single(run.AuditHistory, item => item.Action == "RunPaid" && item.Notes == "b");
    }

    [Fact]
    public async Task Reconcile_ResumesBatchFinalizationAfterRunWasPaid()
    {
        var batch = new PayrollPaymentBatch { Id = "b", CompanyId = "c", PayrollRunId = "r", Status = "SUBMITTED" };
        var run = Run("r", "c", "PROCESSED");
        var batches = Repo(batch);
        batches.SetupSequence(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<PayrollPaymentBatch>>(), It.IsAny<PayrollPaymentBatch>()))
            .ReturnsAsync(new Result { Success = true })
            .ReturnsAsync(new Result { Success = false, StatusCode = 500 })
            .ReturnsAsync(new Result { Success = true });
        var service = new PayrollPaymentBatchService(batches.Object, Repo(run).Object, Repo<PayrollRunEmployee>().Object);
        var request = new PaymentBatchRequestDto { Note = "Bank settlement confirmed", PaymentConfirmed = true };

        var interrupted = await service.Reconcile("c", "b", "a", request);
        Assert.False(interrupted.Success);
        Assert.Equal("PAID", run.Status);
        batch.Status = "RECONCILING"; batch.ReconciledBy = null; batch.ReconciledAtUtc = null;
        batch.Items = [new PayrollPaymentBatchItem { EmployeeId = "e", NetAmount = 10 }];

        var resumed = await service.Reconcile("c", "b", "retry-actor", request);

        Assert.True(resumed.Success);
        Assert.Equal("RECONCILED", batch.Status);
        Assert.Equal("RECONCILED", batch.Items.Single().Status);
        Assert.Single(run.AuditHistory, item => item.Action == "RunPaid" && item.Notes == "b");
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
