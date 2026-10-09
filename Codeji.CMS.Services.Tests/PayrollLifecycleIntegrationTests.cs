using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

/// <summary>
/// Exercises the payroll run lifecycle across calculation, review, workflow,
/// and payment services using shared in-memory repository state.
/// </summary>
public sealed class PayrollLifecycleIntegrationTests
{
    [Fact]
    public async Task Run_Calculates_Reviews_Approves_Processes_Reconciles_And_Closes()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1",
            PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30),
            PayDate = new DateTime(2026, 10, 1), Status = "DRAFT", EmployeeCount = 1
        };
        var member = new PayrollRunEmployee
        {
            Id = "line-1", CompanyId = "company-1", PayrollRunId = run.Id,
            EmployeeId = "employee-1", Status = "READY"
        };
        var payroll = new EmpPayRoll
        {
            Id = "payroll-1", CompanyId = "company-1", UserId = "user-1", EmployeeId = member.EmployeeId,
            PayMonth = new DateTime(2026, 9, 1), PaidDate = new DateTime(2026, 10, 1),
            BasicPay = 60000m, PaidDays = 30,
            Allowance = new Allowance { HRA = 12000m, OtherAllowance = 3000m },
            Deduction = new Deduction { LossOfPay = 5000m, IncomeTax = 3000m, EPF = 1800m }
        };

        var runs = InMemoryRepository.Create(run);
        var members = InMemoryRepository.Create(member);
        var payrollRows = InMemoryRepository.Create(payroll);
        var batches = InMemoryRepository.Create<PayrollPaymentBatch>();

        var calculation = new PayrollRunCalculationService(runs.Object, members.Object, payrollRows.Object);
        var review = new PayrollRunReviewService(runs.Object, members.Object);
        var workflow = new PayrollRunWorkflowService(runs.Object, members.Object);
        var payment = new PayrollPaymentBatchService(batches.Object, runs.Object, members.Object);

        var calculated = await calculation.SyncCalculatedRows("company-1", run.Id, "calculator");
        Assert.True(calculated.Success);
        Assert.Equal("CALCULATED", run.Status);
        Assert.Equal(75000m, run.GrossTotal);
        Assert.Equal(9800m, run.DeductionTotal);
        Assert.Equal(65200m, run.NetTotal);
        Assert.Equal("CALCULATED", member.Status);

        var reviewView = await review.Get("company-1", run.Id);
        Assert.True(reviewView.Success);
        Assert.True(reviewView.MethodResult!.CanApprove);
        Assert.False(reviewView.MethodResult.IsReviewed);
        Assert.True((await review.Record("company-1", run.Id, "reviewer", new PayrollRunReviewRequestDto { Comment = "Checked totals" })).Success);
        reviewView = await review.Get("company-1", run.Id);
        Assert.True(reviewView.MethodResult!.IsReviewed);
        Assert.True((await workflow.Approve("company-1", run.Id, "approver")).Success);
        Assert.True((await workflow.Process("company-1", run.Id, "processor")).Success);

        var createdBatch = await payment.Create("company-1", run.Id, "payment-operator");
        Assert.True(createdBatch.Success);
        Assert.Equal(65200m, createdBatch.MethodResult!.TotalNetAmount);
        Assert.True((await payment.Submit("company-1", createdBatch.MethodResult.Id, "payment-operator", new PaymentBatchRequestDto { ProviderReference = "bank-file-2026-09" })).Success);
        Assert.True((await payment.Reconcile("company-1", createdBatch.MethodResult.Id, "reconciler", new PaymentBatchRequestDto { Note = "Bank settlement confirmed", PaymentConfirmed = true })).Success);
        Assert.True((await workflow.Close("company-1", run.Id, "closer")).Success);

        Assert.Equal("CLOSED", run.Status);
        Assert.Equal("RECONCILED", createdBatch.MethodResult.Status);
        Assert.Equal("RECONCILED", createdBatch.MethodResult.Items.Single().Status);
        Assert.Equal("approver", run.ApprovedBy);
        Assert.NotNull(run.ProcessedAtUtc);
        Assert.NotNull(run.ClosedAtUtc);
        Assert.Contains(run.AuditHistory, item => item.Action == "RunCalculated");
        Assert.Contains(run.AuditHistory, item => item.Action == "RunPaid");
    }
}

internal static class InMemoryRepository
{
    public static Mock<IMongoDbRepository<TEntity>> Create<TEntity>(params TEntity[] initialItems)
    {
        var items = initialItems.ToList();
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _) => Task.FromResult(items.Any(predicate.Compile())));
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult(items.FirstOrDefault(predicate.Compile())));
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<TEntity>>(items.Where(predicate.Compile()).ToArray()));
        repository.Setup(x => x.AddOne(It.IsAny<TEntity>()))
            .Callback<TEntity>(item =>
            {
                var id = typeof(TEntity).GetProperty("Id");
                if (id?.CanWrite == true && string.IsNullOrWhiteSpace(id.GetValue(item)?.ToString()))
                    id.SetValue(item, "payment-batch-1");
                items.Add(item);
            })
            .ReturnsAsync(new Result { Success = true });
        repository.Setup(x => x.AddMany(It.IsAny<IEnumerable<TEntity>>()))
            .Callback<IEnumerable<TEntity>>(newItems => items.AddRange(newItems))
            .ReturnsAsync(new Result { Success = true });
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<TEntity>>(), It.IsAny<TEntity>()))
            .ReturnsAsync(new Result { Success = true });
        return repository;
    }
}

