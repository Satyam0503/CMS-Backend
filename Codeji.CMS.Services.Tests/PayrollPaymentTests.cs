using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollPaymentTests
{
    [Fact]
    public async Task RecordPayrollPayment_RejectsUnprocessedPayroll()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new EmpPayRoll { CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", PayMonth = new DateTime(2026, 8, 1), PaidDate = new DateTime(2026, 9, 1), BasicPay = 50000, PaidDays = 31, IsProcessed = false });
        var service = CreateService(repository);

        var result = await service.RecordPayrollPayment(new RecordPayrollPaymentRequestDto { PayrollId = "payroll-1", PaymentMethod = "MANUAL", PaymentReference = "REF-1" }, "company-1", "hr-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        repository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()), Times.Never);
    }

    [Fact]
    public async Task RecordPayrollPayment_RecordsPaymentForProcessedPayroll()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.IsProcessed = true;
        repository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(payroll);
        repository.Setup(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>())).Callback(() => { payroll.PaymentStatus = "CLOSED"; payroll.WorkflowStatus = "CLOSED"; }).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var result = await CreateService(repository).RecordPayrollPayment(new RecordPayrollPaymentRequestDto { PayrollId = "payroll-1", PaymentMethod = "manual", PaymentReference = " REF-9 " }, "company-1", "hr-1");

        Assert.True(result.Success);
        repository.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task RecordPayrollPayment_AcceptsWorkflowProcessedLegacyRowAndSynchronizesFlag()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.IsProcessed = false;
        payroll.WorkflowStatus = "PROCESSED";
        repository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(payroll);
        MongoDB.Driver.UpdateDefinition<EmpPayRoll>? capturedUpdate = null;
        repository.Setup(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
            .Callback<MongoDB.Driver.FilterDefinition<EmpPayRoll>, MongoDB.Driver.UpdateDefinition<EmpPayRoll>, bool>((_, update, _) => capturedUpdate = update)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var result = await CreateService(repository).RecordPayrollPayment(
            new RecordPayrollPaymentRequestDto { PayrollId = "payroll-1", PaymentMethod = "MANUAL", PaymentReference = "REF-STATE" },
            "company-1", "hr-1");

        Assert.True(result.Success);
        repository.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Once);
        var updateJson = capturedUpdate!.Render(new MongoDB.Driver.RenderArgs<EmpPayRoll>(MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<EmpPayRoll>(), MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry)).ToString();
        Assert.Contains("IsProcessed", updateJson);
    }
    [Fact]
    public async Task RecordPayrollPayment_RejectsAlreadyPaidPayroll()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.IsProcessed = true;
        payroll.PaymentStatus = "PAID";
        repository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(payroll);
        var result = await CreateService(repository).RecordPayrollPayment(new RecordPayrollPaymentRequestDto { PayrollId = "payroll-1", PaymentMethod = "MANUAL", PaymentReference = "REF-2" }, "company-1", "hr-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        repository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()), Times.Never);
    }

    [Fact]
    public async Task ClosePayrollMonth_RejectsMonthWithUnpaidRows()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.IsProcessed = true;
        payroll.PaymentStatus = "PENDING";
        repository.Setup(x => x.Get(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>())).Returns(new[] { payroll }.AsQueryable());

        var result = await CreateService(repository).ClosePayrollMonth(new DateTime(2026, 8, 1), "company-1", "hr-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        repository.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Never);
    }
    [Fact]
    public async Task RecordPayrollPayment_RejectsPayrollFromAnotherCompany()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var otherCompanyPayroll = BuildPayroll();
        otherCompanyPayroll.CompanyId = "company-2";
        repository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>> predicate, bool _, bool __) => predicate.Compile()(otherCompanyPayroll) ? otherCompanyPayroll : null);

        var result = await CreateService(repository).RecordPayrollPayment(new RecordPayrollPaymentRequestDto { PayrollId = "payroll-1", PaymentMethod = "MANUAL", PaymentReference = "REF-X" }, "company-1", "hr-1");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        repository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()), Times.Never);
    }
    [Fact]
    public async Task RecordPayrollPayment_RejectsClosedPayroll()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.IsProcessed = true;
        payroll.PaymentStatus = "CLOSED";
        repository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(payroll);
        var result = await CreateService(repository).RecordPayrollPayment(new RecordPayrollPaymentRequestDto { PayrollId = "payroll-1", PaymentMethod = "MANUAL", PaymentReference = "REF-CLOSED" }, "company-1", "hr-1");
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        repository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()), Times.Never);
    }
    [Fact]
    public async Task RecordPayrollPayment_ReturnsConflictWhenAtomicTransitionFindsNoPendingRow()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.IsProcessed = true;
        repository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(payroll);
        repository.Setup(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = false, StatusCode = 400 });

        var result = await CreateService(repository).RecordPayrollPayment(new RecordPayrollPaymentRequestDto { PayrollId = "payroll-1", PaymentMethod = "MANUAL", PaymentReference = "REF-RACE" }, "company-1", "hr-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("already recorded", result.Message, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task ClosePayrollMonth_ClosesFullyPaidMonthWithConditionalTransition()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.IsProcessed = true;
        payroll.PaymentStatus = "PAID";
        repository.Setup(x => x.Get(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>()))
            .Returns(new[] { payroll }.AsQueryable());
        repository.Setup(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
             .Callback(() => { payroll.PaymentStatus = "CLOSED"; payroll.WorkflowStatus = "CLOSED"; })
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var result = await CreateService(repository).ClosePayrollMonth(new DateTime(2026, 8, 1), "company-1", "hr-1");

        Assert.True(result.Success);
        repository.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Once);
    }
    [Fact]
    public async Task ClosePayrollMonth_ClosesLegacyLowercasePaidStatus()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.IsProcessed = true;
        payroll.PaymentStatus = "paid";
        repository.Setup(x => x.Get(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>())).Returns(new[] { payroll }.AsQueryable());
        repository.Setup(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>())).Callback(() => { payroll.PaymentStatus = "CLOSED"; payroll.WorkflowStatus = "CLOSED"; }).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var result = await CreateService(repository).ClosePayrollMonth(new DateTime(2026, 8, 1), "company-1", "hr-1");
        Assert.True(result.Success);
        repository.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Once);
    }
    [Fact]
    public async Task GetPayrollAuditHistory_ReturnsTenantScopedNewestFirstEvents()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.AuditHistory =
        [
            new PayrollAuditEvent { Action = "PayrollProcessed", ActorId = "hr-1", OccurredAtUtc = new DateTime(2026, 8, 20) },
            new PayrollAuditEvent { Action = "PaymentRecorded", ActorId = "hr-2", OccurredAtUtc = new DateTime(2026, 8, 21) }
        ];
        repository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>> predicate, bool _, bool __) => predicate.Compile()(payroll) ? payroll : null);

        var result = await CreateService(repository).GetPayrollAuditHistory("payroll-1", "company-1");

        Assert.True(result.Success);
        Assert.Equal(2, result.MethodResult!.Count);
        Assert.Equal("PaymentRecorded", result.MethodResult[0].Action);
    }

    [Fact]
    public async Task GetPayrollAuditHistory_RejectsPayrollFromAnotherCompany()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.CompanyId = "company-2";
        repository.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>> predicate, bool _, bool __) => predicate.Compile()(payroll) ? payroll : null);

        var result = await CreateService(repository).GetPayrollAuditHistory("payroll-1", "company-1");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }
    [Fact]
    public async Task ClosePayrollMonth_RejectsMonthWithoutGeneratedRows()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        repository.Setup(x => x.Get(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>()))
            .Returns(Array.Empty<EmpPayRoll>().AsQueryable());

        var result = await CreateService(repository).ClosePayrollMonth(new DateTime(2026, 8, 1), "company-1", "hr-1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        repository.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ClosePayrollMonth_IsIdempotentForAlreadyClosedRows()
    {
        var repository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var payroll = BuildPayroll();
        payroll.IsProcessed = true;
        payroll.PaymentStatus = "CLOSED";
        repository.Setup(x => x.Get(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>()))
            .Returns(new[] { payroll }.AsQueryable());

        var result = await CreateService(repository).ClosePayrollMonth(new DateTime(2026, 8, 1), "company-1", "hr-1");

        Assert.True(result.Success);
        repository.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Never);
    }
    private static EmpPayRoll BuildPayroll() => new()
    {
        Id = "payroll-1", CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
        PayMonth = new DateTime(2026, 8, 1), PaidDate = new DateTime(2026, 9, 1), BasicPay = 50000, PaidDays = 31
    };
    private static PayRollServices CreateService(Mock<IMongoDbRepository<EmpPayRoll>> payroll) => new(
        payroll.Object,
        new Mock<IMongoDbRepository<EmpUser>>().Object,
        new Mock<IMongoDbRepository<Company>>().Object,
        new Mock<IMongoDbRepository<JobTitles>>().Object,
        new PdfService(),
        new Mock<IMiddlewareService>().Object,
        new Mock<IAttendancePayrollReadinessService>().Object);
}









