using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.PayRoll;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class PayrollRunStateSyncTests
{
    private const string TestCompanyId = "COMP-001";
    private const string TestEmployeeId = "EMP-001";
    private const string TestUserId = "user-001";
    private readonly DateTime TestMonth = new(2026, 10, 1);

    [Fact]
    public async Task AddUpdatePayRoll_WhenApprovedRunExists_ReturnsConflict()
    {
        // Arrange
        var empPayRollRepo = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var companyRepo = new Mock<IMongoDbRepository<Company>>();
        var jobTitlesRepo = new Mock<IMongoDbRepository<JobTitles>>();
        var middleware = new Mock<IMiddlewareService>();
        var readiness = new Mock<IAttendancePayrollReadinessService>();
        var runsRepo = new Mock<IMongoDbRepository<PayrollRun>>();
        var runEmpRepo = new Mock<IMongoDbRepository<PayrollRunEmployee>>();

        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new EmpUser
            {
                CompanyId = TestCompanyId,
                UserId = TestUserId,
                EmployeeId = TestEmployeeId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@company.com",
                Status = true
            });

        // Run is in APPROVED state
        var approvedRun = new PayrollRun
        {
            Id = "RUN-100",
            CompanyId = TestCompanyId,
            PeriodStart = TestMonth,
            PeriodEnd = new DateTime(2026, 10, 31),
            PayDate = new DateTime(2026, 10, 31),
            RunType = "REGULAR",
            Status = "APPROVED",
            IsActive = true
        };

        runsRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), false, true))
            .ReturnsAsync(approvedRun);

        var service = new PayRollServices(
            empPayRollRepo.Object,
            empRepo.Object,
            companyRepo.Object,
            jobTitlesRepo.Object,
            null!,
            middleware.Object,
            readiness.Object,
            runsRepo.Object,
            runEmpRepo.Object);

        var request = new AddUpdatePayRollRequestDto
        {
            EmployeeId = TestEmployeeId,
            PayMonth = TestMonth,
            BasicPay = 50000m,
            HRA = 10000m,
            OtherAllowance = 5000m,
            PaidDays = 30,
            PaidDate = new DateTime(2026, 10, 31)
        };

        // Act
        var result = await service.AddUpdatePayRoll(request, TestCompanyId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Contains("approved under Payroll Run RUN-100", result.Message);
    }

    [Fact]
    public async Task AddUpdatePayRoll_WhenDraftRunExists_SynchronizesRunTotals()
    {
        // Arrange
        var empPayRollRepo = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var companyRepo = new Mock<IMongoDbRepository<Company>>();
        var jobTitlesRepo = new Mock<IMongoDbRepository<JobTitles>>();
        var middleware = new Mock<IMiddlewareService>();
        var readiness = new Mock<IAttendancePayrollReadinessService>();
        var runsRepo = new Mock<IMongoDbRepository<PayrollRun>>();
        var runEmpRepo = new Mock<IMongoDbRepository<PayrollRunEmployee>>();

        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new EmpUser
            {
                CompanyId = TestCompanyId,
                UserId = TestUserId,
                EmployeeId = TestEmployeeId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@company.com",
                Status = true
            });

        // EmpPayRoll update succeeds
        var existingPayRoll = new EmpPayRoll
        {
            Id = "PAY-001",
            CompanyId = TestCompanyId,
            EmployeeId = TestEmployeeId,
            UserId = TestUserId,
            PayMonth = TestMonth,
            PaidDate = TestMonth.AddDays(30),
            PaidDays = 30,
            BasicPay = 40000m,
            Allowance = new Allowance(),
            Deduction = new Deduction()
        };

        empPayRollRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), false, true))
            .ReturnsAsync(existingPayRoll);
        empPayRollRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()))
            .ReturnsAsync(new Result { Success = true });

        // Run is in DRAFT state
        var draftRun = new PayrollRun
        {
            Id = "RUN-100",
            CompanyId = TestCompanyId,
            PeriodStart = TestMonth,
            PeriodEnd = new DateTime(2026, 10, 31),
            PayDate = new DateTime(2026, 10, 31),
            RunType = "REGULAR",
            Status = "DRAFT",
            IsActive = true,
            GrossTotal = 40000m,
            NetTotal = 40000m
        };

        runsRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), false, true))
            .ReturnsAsync(draftRun);

        var runEmployee = new PayrollRunEmployee
        {
            Id = "RE-001",
            CompanyId = TestCompanyId,
            PayrollRunId = "RUN-100",
            EmployeeId = TestEmployeeId,
            GrossAmount = 40000m,
            NetAmount = 40000m
        };

        runEmpRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRunEmployee, bool>>>(), false, true))
            .ReturnsAsync(runEmployee);
                runEmpRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollRunEmployee>>(), It.IsAny<PayrollRunEmployee>()))
            .ReturnsAsync(new Result { Success = true });
        runsRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()))
            .ReturnsAsync(new Result { Success = true });
        runEmpRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollRunEmployee, bool>>>(), false, true))
            .ReturnsAsync(new List<PayrollRunEmployee> { runEmployee });

        var service = new PayRollServices(
            empPayRollRepo.Object,
            empRepo.Object,
            companyRepo.Object,
            jobTitlesRepo.Object,
            null!,
            middleware.Object,
            readiness.Object,
            runsRepo.Object,
            runEmpRepo.Object);

        var request = new AddUpdatePayRollRequestDto
        {
            PayRollId = "PAY-001",
            EmployeeId = TestEmployeeId,
            PayMonth = TestMonth,
            BasicPay = 60000m,
            HRA = 10000m,
            OtherAllowance = 5000m,
            LossOfPay = 0m,
            IncomeTax = 5000m,
            PaidDays = 30,
            PaidDate = new DateTime(2026, 10, 31)
        };

        // Act
        var result = await service.AddUpdatePayRoll(request, TestCompanyId);

        // Assert
        Assert.True(result.Success);
        // Gross: 60k + 10k + 5k = 75k. Deduction: 5k. Net: 70k.
        Assert.Equal(75000m, runEmployee.GrossAmount);
        Assert.Equal(5000m, runEmployee.DeductionAmount);
        Assert.Equal(70000m, runEmployee.NetAmount);
        Assert.Equal(75000m, draftRun.GrossTotal);
        Assert.Equal(70000m, draftRun.NetTotal);
    }
}
