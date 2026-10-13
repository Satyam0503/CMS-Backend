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

public sealed class PayRollProcessingWindowTests
{
    [Fact]
    public async Task ProcessPayrollMonth_RejectsCurrentMonthUntilItCloses()
    {
        var service = CreateService();
        var today = DateTime.UtcNow;

        var result = await service.ProcessPayrollMonth(new DateTime(today.Year, today.Month, 1), "company-1", "hr-user");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal("Payroll can only be processed after the payroll month has ended.", result.Message);
    }

    [Fact]
    public async Task ProcessPayrollMonth_RejectsPartialMonthProcessing()
    {
        var service = CreateService();

        var result = await service.ProcessPayrollMonth(DateTime.UtcNow.Date.AddMonths(-1), "company-1", "hr-user", ["employee-1"]);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("Payroll must be processed for all generated employees in the month.", result.Message);
    }

    private static PayRollServices CreateService() => new(
        new Mock<IMongoDbRepository<EmpPayRoll>>().Object,
        new Mock<IMongoDbRepository<EmpUser>>().Object,
        new Mock<IMongoDbRepository<Company>>().Object,
        new Mock<IMongoDbRepository<JobTitles>>().Object,
        new PdfService(),
        new Mock<IMiddlewareService>().Object,
        new Mock<IAttendancePayrollReadinessService>().Object);
}
