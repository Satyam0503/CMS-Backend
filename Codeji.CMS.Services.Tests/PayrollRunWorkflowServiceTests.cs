using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollRunWorkflowServiceTests
{
    [Theory]
    [InlineData("DRAFT")]
    [InlineData("BLOCKED")]
    public async Task Cancel_RetainsAuditReasonAndUsesOptimisticRunVersion(string startingStatus)
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = startingStatus, Version = 4
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        runs.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()))
            .ReturnsAsync(new Result { Success = true });
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Cancel("company-1", "run-1", "payroll-admin", "  Formula cannot be repaired  ");

        Assert.True(result.Success);
        Assert.Equal("CANCELLED", run.Status);
        Assert.False(run.IsActive);
        Assert.Equal(5, run.Version);
        var audit = Assert.Single(run.AuditHistory);
        Assert.Equal("RunCancelled", audit.Action);
        Assert.Equal("payroll-admin", audit.ActorId);
        Assert.Equal("Formula cannot be repaired", audit.Notes);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), run), Times.Once);
    }

    [Theory]
    [InlineData("CALCULATED")]
    [InlineData("APPROVED")]
    [InlineData("PROCESSED")]
    public async Task Cancel_RejectsRunOnceItHasBeenCalculated(string status)
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = status, Version = 2
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Cancel("company-1", "run-1", "payroll-admin", "Do not cancel this run");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(status, run.Status);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_RequiresAUsableReason()
    {
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Cancel("company-1", "run-1", "payroll-admin", "   ");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        runs.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_DoesNotRevealAnotherCompanyRun()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5), Status = "DRAFT"
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<PayrollRun, bool>> predicate, bool _, bool _) =>
                Task.FromResult(predicate.Compile()(run) ? run : null));
        var service = new PayrollRunWorkflowService(runs.Object, Mock.Of<IMongoDbRepository<PayrollRunEmployee>>());

        var result = await service.Cancel("company-2", "run-1", "payroll-admin", "Bad source setup");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task Validate_RejectsDraftWithoutCalculatedPayrollRows()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = "DRAFT", EmployeeCount = 1, ReadyCount = 1, Version = 1
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        var employees = new Mock<IMongoDbRepository<PayrollRunEmployee>>();
        employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollRunEmployee, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { new PayrollRunEmployee { CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1", Status = "READY" } });

        var result = await new PayrollRunWorkflowService(runs.Object, employees.Object).Validate("company-1", "run-1", "validator");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("DRAFT", run.Status);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task Approve_RejectsRunWhenStoredTotalsDoNotReconcileWithEmployeeRows()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = "CALCULATED", CreatedBy = "maker", ReviewedBy = "reviewer",
            ReviewedAtUtc = DateTime.UtcNow, EmployeeCount = 1, ReadyCount = 1,
            GrossTotal = 1000m, DeductionTotal = 100m, NetTotal = 900m, Version = 2
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        var employees = new Mock<IMongoDbRepository<PayrollRunEmployee>>();
        employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollRunEmployee, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[]
            {
                new PayrollRunEmployee
                {
                    CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1",
                    Status = "CALCULATED", GrossAmount = 900m, DeductionAmount = 100m, NetAmount = 800m
                }
            });

        var result = await new PayrollRunWorkflowService(runs.Object, employees.Object).Approve("company-1", "run-1", "approver");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("CALCULATED", run.Status);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }

    [Fact]
    public async Task Approve_RejectsRunCreatorEvenAfterReview()
    {
        var run = new PayrollRun
        {
            Id = "run-1", CompanyId = "company-1", PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30), PayDate = new DateTime(2026, 10, 5),
            Status = "CALCULATED", CreatedBy = "maker", ReviewedBy = "reviewer",
            ReviewedAtUtc = DateTime.UtcNow, EmployeeCount = 1, Version = 2
        };
        var runs = new Mock<IMongoDbRepository<PayrollRun>>();
        runs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        var employees = new Mock<IMongoDbRepository<PayrollRunEmployee>>();
        employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollRunEmployee, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<PayrollRunEmployee, bool>> _, bool _, bool _) => new[]
            {
                new PayrollRunEmployee { CompanyId = "company-1", PayrollRunId = "run-1", EmployeeId = "employee-1", Status = "CALCULATED" }
            });
        var service = new PayrollRunWorkflowService(runs.Object, employees.Object);

        var result = await service.Approve("company-1", "run-1", "maker");

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("CALCULATED", run.Status);
        runs.Verify(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()), Times.Never);
    }
}
