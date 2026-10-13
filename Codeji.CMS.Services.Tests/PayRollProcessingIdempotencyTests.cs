using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.PayRoll;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayRollProcessingIdempotencyTests
{
    private static readonly DateTime Month = new(2026, 8, 1);

    [Fact]
    public async Task ProcessPayrollMonth_TwoConcurrentCallers_WriteOnceAndBothSucceed()
    {
        var draft = Row("company-1", "employee-1");
        var otherTenant = Row("company-2", "employee-1");
        var repository = Repository([draft, otherTenant]);
        var readiness = new Mock<IAttendancePayrollReadinessService>();
        var gate = new TaskCompletionSource<AttendancePayrollReadiness>(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        readiness.Setup(x => x.EvaluateAsync("company-1", Month, It.IsAny<IReadOnlyCollection<string>?>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref arrivals) == 2)
                    gate.TrySetResult(new AttendancePayrollReadiness(true, []));
                return gate.Task;
            });
        var writes = 0;
        var sync = new object();
        repository.Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
            .Returns(() =>
            {
                lock (sync)
                {
                    if (draft.IsProcessed)
                        return Task.FromResult(new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest });
                    draft.IsProcessed = true;
                    writes++;
                    return Task.FromResult(new Result { Success = true, StatusCode = StatusCodes.Status200OK });
                }
            });
        var service = CreateService(repository, readiness.Object);

        var results = await Task.WhenAll(
            service.ProcessPayrollMonth(Month, "company-1", "hr-1"),
            service.ProcessPayrollMonth(Month, "company-1", "hr-2"));

        Assert.All(results, result => Assert.True(result.Success, result.Message));
        Assert.Equal(2, arrivals);
        Assert.Equal(1, writes);
        Assert.True(draft.IsProcessed);
        Assert.False(otherTenant.IsProcessed);
    }
    [Fact]
    public async Task ProcessPayrollMonth_WhenWorkflowAlreadyProcessed_DoesNotReprocessOrDuplicateAudit()
    {
        var payroll = Row("company-1", "employee-1");
        payroll.WorkflowStatus = "PROCESSED";
        payroll.IsProcessed = false;
        var repository = Repository([payroll]);
        var readiness = new Mock<IAttendancePayrollReadinessService>();
        var service = CreateService(repository, readiness.Object);

        var result = await service.ProcessPayrollMonth(Month, "company-1", "hr-1");

        Assert.True(result.Success, result.Message);
        Assert.Contains("already processed", result.Message, StringComparison.OrdinalIgnoreCase);
        repository.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Never);
        readiness.Verify(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>?>()), Times.Never);
    }
    [Fact]
    public async Task ProcessPayrollMonth_WhenAnotherRequestWins_ReturnsAlreadyProcessedWithoutSecondWrite()
    {
        var draft = Row("company-1", "employee-1");
        var otherTenant = Row("company-2", "employee-1");
        var repository = Repository([draft, otherTenant]);
        var writes = 0;
        repository.Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
            .Returns((FilterDefinition<EmpPayRoll> filter, UpdateDefinition<EmpPayRoll> _, bool __) =>
            {
                writes++;
                var rendered = filter.Render(new RenderArgs<EmpPayRoll>(
                    BsonSerializer.SerializerRegistry.GetSerializer<EmpPayRoll>(), BsonSerializer.SerializerRegistry)).ToJson();
                Assert.Contains("company-1", rendered);
                Assert.Contains("IsProcessed", rendered);
                Assert.Contains("WorkflowStatus", rendered);
                Assert.Contains("PaymentStatus", rendered);
                draft.IsProcessed = true; // The competing request committed first.
                return Task.FromResult(new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest });
            });

        var result = await CreateService(repository).ProcessPayrollMonth(Month, "company-1", "hr-1");

        Assert.True(result.Success, result.Message);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Contains("already processed", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, writes);
        Assert.False(otherTenant.IsProcessed);
    }

    [Fact]
    public async Task ProcessPayrollMonth_WhenMonthRemainsMixed_ReturnsConflict()
    {
        var first = Row("company-1", "employee-1");
        var second = Row("company-1", "employee-2");
        var repository = Repository([first, second]);
        repository.Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
            .Returns(() =>
            {
                first.IsProcessed = true;
                return Task.FromResult(new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest });
            });

        var result = await CreateService(repository).ProcessPayrollMonth(Month, "company-1", "hr-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.False(second.IsProcessed);
    }

    [Fact]
    public async Task ProcessPayrollMonth_WhenDatabaseFails_PreservesServerError()
    {
        var repository = Repository([Row("company-1", "employee-1")]);
        repository.Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Result { Success = false, StatusCode = StatusCodes.Status500InternalServerError, Message = "Mongo write failed" });

        var result = await CreateService(repository).ProcessPayrollMonth(Month, "company-1", "hr-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task ProcessPayrollMonth_WhenOnlySomeRowsWereUpdated_DoesNotReportSuccess()
    {
        var first = Row("company-1", "employee-1");
        var second = Row("company-1", "employee-2");
        var repository = Repository([first, second]);
        repository.Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
            .Returns(() =>
            {
                first.IsProcessed = true;
                return Task.FromResult(new Result { Success = true, StatusCode = StatusCodes.Status200OK });
            });

        var result = await CreateService(repository).ProcessPayrollMonth(Month, "company-1", "hr-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.False(second.IsProcessed);
    }
    [Fact]
    public async Task ProcessPayrollMonth_WhenFAndFSettlementPaidPayrollHasStalePendingApproval_DoesNotBlockRemainingRows()
    {
        var paidThroughSettlement = Row("company-1", "employee-1");
        paidThroughSettlement.IsProcessed = true;
        paidThroughSettlement.WorkflowStatus = "PAID";
        paidThroughSettlement.PaymentStatus = "PAID";
        paidThroughSettlement.ApprovalStatus = "PENDING";
        var draft = Row("company-1", "employee-2");
        var repository = Repository([paidThroughSettlement, draft]);
        var writes = 0;
        repository.Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
            .Returns(() =>
            {
                writes++;
                draft.IsProcessed = true;
                draft.WorkflowStatus = "PROCESSED";
                return Task.FromResult(new Result { Success = true, StatusCode = StatusCodes.Status200OK });
            });

        var result = await CreateService(repository).ProcessPayrollMonth(Month, "company-1", "hr-1");

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, writes);
        Assert.Equal("PAID", paidThroughSettlement.PaymentStatus);
        Assert.True(draft.IsProcessed);
    }


    [Fact]
    public async Task ProcessPayrollMonth_SequentialRetry_DoesNotWriteTwice()
    {
        var draft = Row("company-1", "employee-1");
        var repository = Repository([draft]);
        var writes = 0;
        repository.Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
            .Returns(() =>
            {
                writes++;
                draft.IsProcessed = true;
                return Task.FromResult(new Result { Success = true, StatusCode = StatusCodes.Status200OK });
            });
        var service = CreateService(repository);

        var first = await service.ProcessPayrollMonth(Month, "company-1", "hr-1");
        var retry = await service.ProcessPayrollMonth(Month, "company-1", "hr-2");

        Assert.True(first.Success, first.Message);
        Assert.True(retry.Success, retry.Message);
        Assert.Contains("already processed", retry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, writes);
    }
    private static Mock<IMongoDbRepository<EmpPayRoll>> Repository(IReadOnlyCollection<EmpPayRoll> rows)
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        repository.Setup(x => x.Get(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(),
                It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<EmpPayRoll, bool>> predicate,
                Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>? _, bool __) =>
                rows.Where(predicate.Compile()).AsQueryable());
        return repository;
    }

    private static PayRollServices CreateService(Mock<IMongoDbRepository<EmpPayRoll>> repository, IAttendancePayrollReadinessService? readinessService = null)
    {
        var readiness = new Mock<IAttendancePayrollReadinessService>();
        readiness.Setup(x => x.EvaluateAsync("company-1", Month, It.IsAny<IReadOnlyCollection<string>?>()))
            .ReturnsAsync(new AttendancePayrollReadiness(true, []));
        return new PayRollServices(repository.Object,
            new Mock<IMongoDbRepository<EmpUser>>().Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(), new Mock<IMiddlewareService>().Object, readinessService ?? readiness.Object);
    }

    private static EmpPayRoll Row(string companyId, string employeeId) => new()
    {
        CompanyId = companyId, UserId = employeeId, EmployeeId = employeeId,
        PayMonth = Month, PaidDate = Month.AddMonths(1), BasicPay = 50000,
        PaidDays = 31, IsProcessed = false
    };
}
