using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollRunWorkflowServiceTests
{
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
