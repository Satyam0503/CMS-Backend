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

public sealed class PayrollValidationTests
{
    [Fact]
    public async Task ValidatePayrollData_ReturnsStructuredErrorsWithoutWriting()
    {
        var payroll = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        employees.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<EmpUser>());
        payroll.Setup(x => x.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);

        var result = await CreateService(payroll, employees).ValidatePayrollData(new PayrollValidationRequestDto
        {
            PayMonth = new DateTime(2026, 8, 15),
            PayData = [new EmployeePayRollModel { EmployeeId = "missing", BasicPay = -1 }]
        }, "company-1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains(result.MethodResults, error => error.Contains("missing", StringComparison.OrdinalIgnoreCase));
        payroll.Verify(x => x.AddOne(It.IsAny<EmpPayRoll>()), Times.Never);
        payroll.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ValidatePayrollData_AllowsValidCompanyEmployeePayload()
    {
        var payroll = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        employees.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([new EmpUser { EmployeeId = "employee-1", CompanyId = "company-1", FirstName = "Test", LastName = "User", Email = "test@example.com" }]);
        payroll.Setup(x => x.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);

        var result = await CreateService(payroll, employees).ValidatePayrollData(new PayrollValidationRequestDto
        {
            PayMonth = new DateTime(2026, 8, 15),
            PayData = [new EmployeePayRollModel { EmployeeId = "employee-1", BasicPay = 100, PaidDays = 31, PaidDate = new DateTime(2026, 8, 31), HRA = 20, LossOfPayDays = 0, LossOfPay = 0, IncomeTax = 0 }]
        }, "company-1");

        Assert.True(result.Success);
        Assert.Empty(result.MethodResults);
        Assert.Equal("Payroll data is ready for upload.", result.Message);
    }

    private static PayRollServices CreateService(Mock<IMongoDbRepository<EmpPayRoll>> payroll, Mock<IMongoDbRepository<EmpUser>> employees) => new(
        payroll.Object,
        employees.Object,
        new Mock<IMongoDbRepository<Company>>().Object,
        new Mock<IMongoDbRepository<JobTitles>>().Object,
        new PdfService(),
        new Mock<IMiddlewareService>().Object,
        new Mock<IAttendancePayrollReadinessService>().Object);
}
