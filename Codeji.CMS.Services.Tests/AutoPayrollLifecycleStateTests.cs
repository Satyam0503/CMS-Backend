using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class AutoPayrollLifecycleStateTests
{
    [Fact]
    public async Task GeneratePayrollForMonth_RejectsLifecycleProcessedRowWhenLegacyFlagIsFalse()
    {
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var processedRow = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 8, 1), PaidDate = new DateTime(2026, 9, 1),
            BasicPay = 50000, PaidDays = 31, IsProcessed = false,
            WorkflowStatus = "PROCESSED"
        };
        payrollRepository.Setup(repository => repository.Exist(
                It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<EmpPayRoll, bool>> predicate, bool _) => predicate.Compile()(processedRow));
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        employeeRepository.Setup(repository => repository.GetAll(
                It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<EmpUser>());
        var divisorPolicies = new Mock<IPayrollDivisorPolicyService>();
        var service = new AutoPayrollServices(
            new Mock<IPayRollServices>().Object,
            employeeRepository.Object,
            payrollRepository.Object,
            new Mock<IMongoDbRepository<AttendanceModel>>().Object,
            new Mock<IMongoDbRepository<AttendanceDaySegment>>().Object,
            new Mock<IMongoDbRepository<SalaryModel>>().Object,
            new Mock<IMongoDbRepository<AttendanceStatusSetting>>().Object,
            new Mock<IMongoDbRepository<MonthlyAttendanceSummary>>().Object,
            new Mock<IMongoDbRepository<AttendancePayrollException>>().Object,
            divisorPolicies.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            new Mock<IMongoDbRepository<EmployeePayrollAssignment>>().Object,
            new Mock<IMongoDbRepository<PayrollPolicyProfile>>().Object);

        var result = await service.GeneratePayrollForMonthAsync("company-1", new DateTime(2026, 8, 1));

        Assert.False(result.Success);
        Assert.Contains("already processed", result.Message, StringComparison.OrdinalIgnoreCase);
        divisorPolicies.Verify(policy => policy.GetEffective(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GeneratePayrollForMonth_SkipsExcelUploadedRows_DuringBulkRun()
    {
        var august = new DateTime(2026, 8, 1);
        var payrollRepository = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var excelRow = new EmpPayRoll
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "emp-excel",
            PayMonth = august, PaidDate = new DateTime(2026, 8, 31),
            BasicPay = 45000, PaidDays = 31, IsProcessed = false,
            WorkflowStatus = "DRAFT", CalculationSource = "EXCEL_UPLOAD"
        };
        payrollRepository.Setup(r => r.Get(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<EmpPayRoll, bool>> predicate, Func<IQueryable<EmpPayRoll>, IOrderedQueryable<EmpPayRoll>> _, bool __) =>
                new[] { excelRow }.AsQueryable().Where(predicate.Compile()).AsQueryable());

        payrollRepository.Setup(r => r.Exist(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(false);

        var employee = new EmpUser
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "emp-excel",
            FirstName = "Excel", LastName = "User", Email = "excel@example.com", Status = true, DateOfJoining = "01 January 2026"
        };
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        employeeRepository.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { employee });

        var payRollServices = new Mock<IPayRollServices>();
        var divisorPolicies = new Mock<IPayrollDivisorPolicyService>();
        divisorPolicies.Setup(p => p.GetEffective(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new PayrollDivisorPolicyDto { DivisorPolicy = "CALENDAR_DAYS", RequireAttendanceLock = false });

        var policyProfiles = new Mock<IMongoDbRepository<PayrollPolicyProfile>>();
        policyProfiles.Setup(p => p.GetAll(It.IsAny<Expression<Func<PayrollPolicyProfile, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<PayrollPolicyProfile>());

        var assignments = new Mock<IMongoDbRepository<EmployeePayrollAssignment>>();
        assignments.Setup(p => p.GetAll(It.IsAny<Expression<Func<EmployeePayrollAssignment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<EmployeePayrollAssignment>());

        var service = new AutoPayrollServices(
            payRollServices.Object,
            employeeRepository.Object,
            payrollRepository.Object,
            new Mock<IMongoDbRepository<AttendanceModel>>().Object,
            new Mock<IMongoDbRepository<AttendanceDaySegment>>().Object,
            new Mock<IMongoDbRepository<SalaryModel>>().Object,
            new Mock<IMongoDbRepository<AttendanceStatusSetting>>().Object,
            new Mock<IMongoDbRepository<MonthlyAttendanceSummary>>().Object,
            new Mock<IMongoDbRepository<AttendancePayrollException>>().Object,
            divisorPolicies.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            assignments.Object,
            policyProfiles.Object);

        var result = await service.GeneratePayrollForMonthAsync("company-1", august, targetEmployeeIds: null);

        Assert.True(result.Success);
        // AddUpdatePayRoll should never be called because the excel-uploaded row was skipped and protected
        payRollServices.Verify(p => p.AddUpdatePayRoll(It.IsAny<AddUpdatePayRollRequestDto>(), It.IsAny<string>()), Times.Never);
    }
}