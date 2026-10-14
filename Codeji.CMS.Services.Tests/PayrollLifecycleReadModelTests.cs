using System.Linq.Expressions;
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

public sealed class PayrollLifecycleReadModelTests
{
    [Theory]
    [InlineData(true, "DRAFT", "PENDING", "PROCESSED")]
    [InlineData(false, "PROCESSED", "PENDING", "PROCESSED")]
    [InlineData(false, "DRAFT", "PAID", "PAID")]
    [InlineData(false, "DRAFT", "CLOSED", "CLOSED")]
    public async Task GetEmployeePayRoll_ProjectsEffectiveLifecycleAndTerminalFields(
        bool isProcessed, string workflowStatus, string paymentStatus, string expectedStatus)
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        var payroll = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 8, 1), PaidDate = new DateTime(2026, 9, 1),
            BasicPay = 50000, PaidDays = 31, IsProcessed = isProcessed,
            WorkflowStatus = workflowStatus, PaymentStatus = paymentStatus,
            PaymentReference = "BANK-REF", ClosedAt = new DateTime(2026, 9, 2), ClosedBy = "hr-1"
        };
        var employee = new EmpUser
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            FirstName = "Payroll", LastName = "User", Email = "payroll@example.com"
        };
        payrollRepository.Setup(repository => repository.Get(
                It.IsAny<Expression<Func<EmpPayRoll, bool>>>(),
                It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<EmpPayRoll, bool>> predicate,
                Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>> _, bool __) =>
                new[] { payroll, new EmpPayRoll { CompanyId = "company-2", UserId = "foreign-user", EmployeeId = "employee-1", PayMonth = payroll.PayMonth, PaidDate = payroll.PaidDate, BasicPay = 99999, PaidDays = 31, IsProcessed = true, WorkflowStatus = "PROCESSED" } }.AsQueryable().Where(predicate.Compile()).AsQueryable());
        employeeRepository.Setup(repository => repository.GetAll(
                It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<EmpUser, bool>> predicate, bool _, bool __) =>
                new[] { employee }.Where(predicate.Compile()));

        var service = new PayRollServices(
            payrollRepository.Object, employeeRepository.Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(), new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        var result = await service.GetEmployeePayRoll(
            new GetEmpPayRollRequestDto { PayMonth = new DateTime(2026, 8, 1) }, "company-1", processedOnly: true);

        var row = Assert.Single(result.MethodResults!);
        Assert.True(row.PayData.IsProcessed);
        Assert.Equal(expectedStatus, row.PayData.WorkflowStatus);
        Assert.Equal(paymentStatus, row.PayData.PaymentStatus);
        Assert.Equal("BANK-REF", row.PayData.PaymentReference);
        Assert.Equal("hr-1", row.PayData.ClosedBy);
        Assert.Equal(new DateTime(2026, 9, 2), row.PayData.ClosedAt);
    }

    [Fact]
    public async Task GetEmployeePayRoll_DoesNotJoinJobTitleFromAnotherTenant()
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        var jobTitleRepository = new Mock<IMongoDbRepository<JobTitles>>();
        var payroll = new EmpPayRoll { CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", PayMonth = new DateTime(2026, 8, 1), PaidDate = DateTime.UtcNow, BasicPay = 50000, PaidDays = 31, IsProcessed = true, WorkflowStatus = "PROCESSED" };
        var employee = new EmpUser { CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", FirstName = "Payroll", LastName = "User", Email = "payroll@example.com", JobRole = "job-shared" };
        payrollRepository.Setup(r => r.Get(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<EmpPayRoll, bool>> predicate, Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>> _, bool __) => new[] { payroll }.AsQueryable().Where(predicate.Compile()).AsQueryable());
        employeeRepository.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<EmpUser, bool>> predicate, bool _, bool __) => new[] { employee }.Where(predicate.Compile()));
        jobTitleRepository.Setup(r => r.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<JobTitles, bool>> predicate, bool _, bool __) => new[] { new JobTitles { CompanyId = "company-2", JobTitleId = "job-shared", Titles = [new() { Language = "en", Label = "Foreign title" }] } }.Where(predicate.Compile()));

        var service = new PayRollServices(payrollRepository.Object, employeeRepository.Object, new Mock<IMongoDbRepository<Company>>().Object, jobTitleRepository.Object, new PdfService(), new Mock<IMiddlewareService>().Object, new Mock<IAttendancePayrollReadinessService>().Object);
        var result = await service.GetEmployeePayRoll(new GetEmpPayRollRequestDto { PayMonth = payroll.PayMonth }, "company-1", processedOnly: true);

        var row = Assert.Single(result.MethodResults!);
        Assert.Null(row.JobTitle);
    }

    [Fact]
    public async Task GetEmployeePayRoll_ExcludesUncalculatedEmployeesWhoJoinedInFutureOrTerminatedPrior()
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        var jobTitleRepository = new Mock<IMongoDbRepository<JobTitles>>();

        var september = new DateTime(2026, 9, 1);
        var calculatedEmployee = new EmpUser
        {
            CompanyId = "company-1", UserId = "u-calc", EmployeeId = "emp-calc",
            FirstName = "Alice", LastName = "Smith", Email = "alice@example.com",
            DateOfJoining = "01 June 2026", Status = true
        };
        var futureJoiner = new EmpUser
        {
            CompanyId = "company-1", UserId = "u-future", EmployeeId = "emp-future",
            FirstName = "Vivek", LastName = "Singh", Email = "vivek@example.com",
            DateOfJoining = "12 October 2026", Status = true
        };
        var priorTerminated = new EmpUser
        {
            CompanyId = "company-1", UserId = "u-term", EmployeeId = "emp-term",
            FirstName = "Ramu", LastName = "Negi", Email = "ramu@example.com",
            DateOfJoining = "01 January 2026",
            ExitDate = "15 August 2026", LastWorkingDate = new DateTime(2026, 8, 15), Status = false
        };

        var calculatedPayroll = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "u-calc", EmployeeId = "emp-calc",
            PayMonth = september, PaidDate = DateTime.UtcNow, BasicPay = 50000, PaidDays = 30, IsProcessed = true, WorkflowStatus = "PROCESSED"
        };

        payrollRepository.Setup(r => r.Get(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<EmpPayRoll, bool>> predicate, Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>> _, bool __) =>
                new[] { calculatedPayroll }.AsQueryable().Where(predicate.Compile()).AsQueryable());

        employeeRepository.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<EmpUser, bool>> predicate, bool _, bool __) =>
                new[] { calculatedEmployee, futureJoiner, priorTerminated }.Where(predicate.Compile()));

        jobTitleRepository.Setup(r => r.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<JobTitles>());

        var service = new PayRollServices(
            payrollRepository.Object, employeeRepository.Object,
            new Mock<IMongoDbRepository<Company>>().Object, jobTitleRepository.Object,
            new PdfService(), new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        var result = await service.GetEmployeePayRoll(
            new GetEmpPayRollRequestDto { PayMonth = september }, "company-1", processedOnly: false);

        var returnedIds = result.MethodResults!.Select(r => r.EmployeeId).ToList();
        Assert.Single(returnedIds);
        Assert.Equal("emp-calc", returnedIds[0]);
        Assert.DoesNotContain("emp-future", returnedIds);
        Assert.DoesNotContain("emp-term", returnedIds);
    }

    [Fact]
    public async Task UploadPayrollData_StampsCalculationSourceExcelUploadAndAppendsAuditEvent()
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        var september = new DateTime(2026, 9, 1);
        var employee = new EmpUser
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "EMP101",
            FirstName = "Test", LastName = "User", Email = "test@example.com", Status = true
        };

        employeeRepository.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { employee });

        payrollRepository.Setup(r => r.Exist(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(false);

        EmpPayRoll? capturedPayroll = null;
        payrollRepository.Setup(r => r.AddOne(It.IsAny<EmpPayRoll>()))
            .Callback<EmpPayRoll>(p => capturedPayroll = p)
            .ReturnsAsync(new Domain.Models.Result { Success = true });

        var service = new PayRollServices(
            payrollRepository.Object, employeeRepository.Object,
            new Mock<IMongoDbRepository<Company>>().Object, new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(), new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        var payload = new EmplyeePayRollRequestDto
        {
            PayMonth = september,
            PayData = new List<EmployeePayRollModel>
            {
                new EmployeePayRollModel
                {
                    EmployeeId = "EMP101",
                    BasicPay = 45000,
                    PaidDays = 30,
                    PaidDate = DateTime.UtcNow,
                    HRA = 15000,
                    LossOfPay = 0,
                    LossOfPayDays = 0,
                    IncomeTax = 2000
                }
            }
        };

        var result = await service.UploadPayrollData(payload, "company-1", "admin-user");

        Assert.True(result.Success);
        Assert.NotNull(capturedPayroll);
        Assert.Equal("EXCEL_UPLOAD", capturedPayroll.CalculationSource);
        Assert.Equal("DRAFT", capturedPayroll.WorkflowStatus);
        var audit = Assert.Single(capturedPayroll.AuditHistory);
        Assert.Equal("PayrollUploaded", audit.Action);
        Assert.Equal("admin-user", audit.ActorId);
    }
}