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
    }}