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
    public async Task AddUpdatePayRoll_RejectsNegativeNetWithoutWritingPayroll()
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        var service = new PayRollServices(
            payrollRepository.Object, employeeRepository.Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(), new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        var result = await service.AddUpdatePayRoll(new AddUpdatePayRollRequestDto
        {
            EmployeeId = "employee-1", BasicPay = 10000m, PaidDays = 30, PaidDate = new DateTime(2026, 9, 30),
            PayMonth = new DateTime(2026, 9, 1), IncomeTax = 2000m, EPF = 1000m,
            DeductionLines = [new PayrollDeductionLineDto { Code = "LOAN", Description = "Loan recovery", Amount = 8000m }]
        }, "company-1");

        Assert.False(result.Success);
        Assert.Equal("PAYROLL_NET_NEGATIVE", result.ErrorCode);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        employeeRepository.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        payrollRepository.Verify(x => x.AddOne(It.IsAny<EmpPayRoll>()), Times.Never);
        payrollRepository.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()), Times.Never);
    }

    [Fact]
    public async Task AddUpdatePayRoll_PersistsApprovedContributionRatesInCalculationSnapshot()
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        var employee = new EmpUser { CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", FirstName = "Payroll", LastName = "Employee", Email = "payroll@example.com" };
        EmpPayRoll? savedPayroll = null;
        employeeRepository.Setup(repository => repository.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(employee);
        payrollRepository.Setup(repository => repository.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(false);
        payrollRepository.Setup(repository => repository.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((EmpPayRoll?)null);
        payrollRepository.Setup(repository => repository.AddOne(It.IsAny<EmpPayRoll>()))
            .Callback<EmpPayRoll>(payroll => savedPayroll = payroll)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true, StatusCode = StatusCodes.Status200OK });

        var service = new PayRollServices(
            payrollRepository.Object, employeeRepository.Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(), new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        var result = await service.AddUpdatePayRoll(new AddUpdatePayRollRequestDto
        {
            EmployeeId = "employee-1", BasicPay = 50000m, PaidDays = 30,
            PaidDate = new DateTime(2026, 10, 31), PayMonth = new DateTime(2026, 10, 1),
            CalculationSnapshot = new PayrollCalculationSnapshotDto
            {
                PayrollMonth = new DateTime(2026, 10, 1),
                EpfEmployeeRatePercent = 10.25m,
                EpfEmployerRatePercent = 11.5m,
                EsicEmployeeRatePercent = 0.6m,
                EsicEmployerRatePercent = 3.1m,
                SalaryRevisions = [new() { SalaryStructureId = "salary-structure-42", Version = 7 }],
                SalaryStructures = [new()
                {
                    SalaryStructureId = "salary-structure-42", Version = 7,
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    Components = [new() { Code = "BASIC", Name = "Basic", Kind = "EARNING", Value = 50000m }]
                }]
            }
        }, "company-1");

        Assert.True(result.Success);
        Assert.NotNull(savedPayroll?.CalculationSnapshot);
        Assert.Equal(10.25m, savedPayroll.CalculationSnapshot.EpfEmployeeRatePercent);
        Assert.Equal(11.5m, savedPayroll.CalculationSnapshot.EpfEmployerRatePercent);
        Assert.Equal(0.6m, savedPayroll.CalculationSnapshot.EsicEmployeeRatePercent);
        Assert.Equal(3.1m, savedPayroll.CalculationSnapshot.EsicEmployerRatePercent);
        Assert.Collection(savedPayroll.CalculationSnapshot.SalaryRevisions,
            revision => { Assert.Equal("salary-structure-42", revision.SalaryStructureId); Assert.Equal(7, revision.Version); });
        var salarySnapshot = Assert.Single(savedPayroll.CalculationSnapshot.SalaryStructures);
        Assert.Equal("salary-structure-42", salarySnapshot.SalaryStructureId);
        Assert.Equal(50000m, Assert.Single(salarySnapshot.Components).Value);
    }

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





