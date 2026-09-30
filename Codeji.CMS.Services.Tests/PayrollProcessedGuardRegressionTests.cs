using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.PayRoll;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollProcessedGuardRegressionTests
{
    [Fact]
    public async Task AddUpdatePayRoll_RejectsTerminalPaidStateEvenWhenProcessedFlagIsFalse()
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        var employee = new EmpUser { CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", FirstName = "Paid", LastName = "Employee", Email = "paid@example.com" };
        employeeRepository.Setup(repository => repository.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(employee);
        payrollRepository.Setup(repository => repository.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>> predicate, bool _) => predicate.Compile()(new EmpPayRoll
            {
                CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", PayMonth = new DateTime(2026, 8, 1), PaidDate = new DateTime(2026, 9, 1), BasicPay = 50000, PaidDays = 31,
                IsProcessed = false, PaymentStatus = "PAID", WorkflowStatus = "PAID"
            }));

        var service = new PayRollServices(
            payrollRepository.Object, employeeRepository.Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(), new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        var result = await service.AddUpdatePayRoll(new AddUpdatePayRollRequestDto
        {
            EmployeeId = "employee-1", BasicPay = 50000, PaidDays = 31,
            PaidDate = new DateTime(2026, 9, 1), PayMonth = new DateTime(2026, 8, 15)
        }, "company-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        payrollRepository.Verify(repository => repository.AddOne(It.IsAny<EmpPayRoll>()), Times.Never);
        payrollRepository.Verify(repository => repository.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()), Times.Never);
    }
    [Fact]
    public async Task AddUpdatePayRoll_RejectsWorkflowProcessedWhenLegacyFlagIsFalse()
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        var employee = new EmpUser { CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", FirstName = "Processed", LastName = "Employee", Email = "processed@example.com" };
        employeeRepository.Setup(repository => repository.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(employee);
        payrollRepository.Setup(repository => repository.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>> predicate, bool _) => predicate.Compile()(new EmpPayRoll
            {
                CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", PayMonth = new DateTime(2026, 8, 1), PaidDate = new DateTime(2026, 9, 1), BasicPay = 50000, PaidDays = 31,
                IsProcessed = false, PaymentStatus = "PENDING", WorkflowStatus = "PROCESSED"
            }));

        var service = new PayRollServices(
            payrollRepository.Object, employeeRepository.Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(), new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        var result = await service.AddUpdatePayRoll(new AddUpdatePayRollRequestDto
        {
            EmployeeId = "employee-1", BasicPay = 50000, PaidDays = 31,
            PaidDate = new DateTime(2026, 9, 1), PayMonth = new DateTime(2026, 8, 1)
        }, "company-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        payrollRepository.Verify(repository => repository.AddOne(It.IsAny<EmpPayRoll>()), Times.Never);
        payrollRepository.Verify(repository => repository.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()), Times.Never);
    }
    [Fact]
    public async Task AddUpdatePayRoll_AllowsDraftForDifferentEmployeeWhenAnotherEmployeeIsProcessed()
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        var targetEmployee = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-2",
            EmployeeId = "employee-2",
            FirstName = "Draft",
            LastName = "Employee",
            Email = "draft@example.com"
        };

        employeeRepository
            .Setup(repository => repository.FirstOrDefault(
                It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(targetEmployee);

        payrollRepository
            .Setup(repository => repository.Exist(
                It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(),
                It.IsAny<bool>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>> predicate, bool _) => predicate.Compile()(new EmpPayRoll
            {
                CompanyId = "company-1",
                UserId = "user-1",
                EmployeeId = "employee-1",
                PaidDays = 31,
                PaidDate = new DateTime(2026, 9, 1),
                BasicPay = 50000,
                PayMonth = new DateTime(2026, 8, 1),
                IsProcessed = true
            }));

        payrollRepository
            .Setup(repository => repository.AddOne(It.IsAny<EmpPayRoll>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true, StatusCode = StatusCodes.Status200OK });

        var service = new PayRollServices(
            payrollRepository.Object,
            employeeRepository.Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(),
            new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        var result = await service.AddUpdatePayRoll(new AddUpdatePayRollRequestDto
        {
            EmployeeId = "employee-2",
            BasicPay = 50000,
            PaidDays = 31,
            PaidDate = new DateTime(2026, 9, 1),
            PayMonth = new DateTime(2026, 8, 15)
        }, "company-1");

        Assert.True(result.Success);
        payrollRepository.Verify(repository => repository.AddOne(It.Is<EmpPayRoll>(payroll =>
            payroll.EmployeeId == "employee-2" && payroll.CompanyId == "company-1" && payroll.PayMonth == new DateTime(2026, 8, 1))), Times.Once);
    }
}





