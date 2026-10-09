using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using Moq;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public sealed class AutoPayrollSalarySourceTests
{
    [Fact]
    public async Task GeneratePayroll_BlocksUnsupportedTdsModeBeforeChangingAnyPayrollRows()
    {
        var employee = new EmpUser
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", FirstName = "Test",
            LastName = "Employee", Email = "test@example.com", DateOfJoining = "2026-01-01", Status = true
        };
        var payrollRows = Repository<EmpPayRoll>();
        payrollRows.Setup(x => x.Exist(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);
        var payrollService = new Mock<IPayRollServices>();
        var divisor = new Mock<IPayrollDivisorPolicyService>();
        divisor.Setup(x => x.GetEffective("company-1", new DateTime(2026, 10, 1)))
            .ReturnsAsync(new PayrollDivisorPolicyDto { RequireAttendanceLock = false, TdsMode = "AUTO_NEW_REGIMEE" });
        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        var service = new AutoPayrollServices(
            payrollService.Object, Repository(employee).Object, payrollRows.Object,
            Repository<AttendanceModel>().Object, Repository<AttendanceDaySegment>().Object, Repository<SalaryModel>().Object,
            Repository<AttendanceStatusSetting>().Object, Repository<MonthlyAttendanceSummary>().Object,
            Repository<AttendancePayrollException>().Object, divisor.Object, workingCalendar.Object,
            Repository<EmployeePayrollAssignment>().Object, Repository<PayrollPolicyProfile>().Object,
            Repository<PayrollPayGroup>().Object, Repository<SalaryTemplate>().Object,
            Repository<EmployeeSalaryStructure>().Object);

        var result = await service.GeneratePayrollForMonthAsync("company-1", new DateTime(2026, 10, 1));

        Assert.False(result.Success);
        Assert.Equal(422, result.StatusCode);
        Assert.Equal("PAYROLL_VALIDATION_BLOCKED", result.ErrorCode);
        Assert.Contains(result.Errors, error => error.Field == "employee-1" && error.Message.Contains("unsupported", StringComparison.OrdinalIgnoreCase));
        payrollRows.Verify(x => x.AddOne(It.IsAny<EmpPayRoll>()), Times.Never);
        payrollRows.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()), Times.Never);
        payrollService.Verify(x => x.AddUpdatePayRoll(It.IsAny<AddUpdatePayRollRequestDto>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GeneratePayroll_BlocksNonTaxableCanonicalEarningWhenAutomaticTdsCannotModelItsExemption()
    {
        var employee = new EmpUser
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", FirstName = "Test",
            LastName = "Employee", Email = "test@example.com", DateOfJoining = "2026-01-01", Status = true
        };
        var salary = new EmployeeSalaryStructure
        {
            Id = "55555555-5555-5555-5555-555555555555", CompanyId = "company-1", EmployeeId = "employee-1",
            Version = 1, EffectiveFrom = new DateTime(2026, 10, 1), Status = "ACTIVE", ApprovalStatus = "APPROVED",
            Components =
            [
                new() { ComponentCode = "BASIC", ComponentName = "Basic", Value = 50000m, IsTaxable = true },
                new() { ComponentCode = "MEAL", ComponentName = "Meal allowance", Value = 3000m, IsTaxable = false }
            ]
        };
        var payrollRows = Repository<EmpPayRoll>();
        payrollRows.Setup(x => x.Exist(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);
        var payrollService = new Mock<IPayRollServices>();
        var divisor = new Mock<IPayrollDivisorPolicyService>();
        divisor.Setup(x => x.GetEffective("company-1", new DateTime(2026, 10, 1)))
            .ReturnsAsync(new PayrollDivisorPolicyDto { RequireAttendanceLock = false, TdsMode = "AUTO_NEW_REGIME" });
        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        var service = new AutoPayrollServices(
            payrollService.Object, Repository(employee).Object, payrollRows.Object,
            Repository<AttendanceModel>().Object, Repository<AttendanceDaySegment>().Object, Repository<SalaryModel>().Object,
            Repository<AttendanceStatusSetting>().Object, Repository<MonthlyAttendanceSummary>().Object,
            Repository<AttendancePayrollException>().Object, divisor.Object, workingCalendar.Object,
            Repository<EmployeePayrollAssignment>().Object, Repository<PayrollPolicyProfile>().Object,
            Repository<PayrollPayGroup>().Object, Repository<SalaryTemplate>().Object,
            Repository(salary).Object);

        var result = await service.GeneratePayrollForMonthAsync("company-1", new DateTime(2026, 10, 1));

        Assert.False(result.Success);
        Assert.Contains("MEAL", result.Message);
        Assert.Contains("exemption category", result.Message, StringComparison.OrdinalIgnoreCase);
        payrollRows.Verify(x => x.AddOne(It.IsAny<EmpPayRoll>()), Times.Never);
        payrollRows.Verify(x => x.Update(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<EmpPayRoll>()), Times.Never);
        payrollService.Verify(x => x.AddUpdatePayRoll(It.IsAny<AddUpdatePayRollRequestDto>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GeneratePayroll_PreservesManuallyEnteredTdsWhenRegeneratingManualOnlyDraft()
    {
        var employee = new EmpUser
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", FirstName = "Test",
            LastName = "Employee", Email = "test@example.com", DateOfJoining = "2026-01-01", Status = true
        };
        var salary = new EmployeeSalaryStructure
        {
            Id = "55555555-5555-5555-5555-555555555555", CompanyId = "company-1", EmployeeId = "employee-1",
            Version = 1, EffectiveFrom = new DateTime(2026, 10, 1), Status = "ACTIVE", ApprovalStatus = "APPROVED",
            Components = [new() { ComponentCode = "BASIC", ComponentName = "Basic", Value = 50000m, IsTaxable = true }]
        };
        var existingPayroll = new EmpPayRoll
        {
            Id = "payroll-1", CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1",
            PayMonth = new DateTime(2026, 10, 1), PaidDate = new DateTime(2026, 10, 31), BasicPay = 50000m,
            PaidDays = 31, CreatedAt = DateTime.UtcNow,
            Deduction = new Deduction { IncomeTax = 1234m },
            CalculationSnapshot = new PayrollCalculationSnapshot { TdsMode = "MANUAL_ONLY" }
        };
        var payrollRows = Repository(existingPayroll);
        AddUpdatePayRollRequestDto? generatedPayroll = null;
        var payrollService = new Mock<IPayRollServices>();
        payrollService.Setup(x => x.AddUpdatePayRoll(It.IsAny<AddUpdatePayRollRequestDto>(), "company-1"))
            .Callback<AddUpdatePayRollRequestDto, string>((request, _) => generatedPayroll = request)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var divisor = new Mock<IPayrollDivisorPolicyService>();
        divisor.Setup(x => x.GetEffective("company-1", new DateTime(2026, 10, 1)))
            .ReturnsAsync(new PayrollDivisorPolicyDto { RequireAttendanceLock = false, TdsMode = "MANUAL_ONLY" });
        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        workingCalendar.Setup(x => x.CountWorkingDaysAsync("company-1", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), It.IsAny<CancellationToken>())).ReturnsAsync(22);
        var service = new AutoPayrollServices(
            payrollService.Object, Repository(employee).Object, payrollRows.Object,
            Repository<AttendanceModel>().Object, Repository<AttendanceDaySegment>().Object, Repository<SalaryModel>().Object,
            Repository<AttendanceStatusSetting>().Object, Repository<MonthlyAttendanceSummary>().Object,
            Repository<AttendancePayrollException>().Object, divisor.Object, workingCalendar.Object,
            Repository<EmployeePayrollAssignment>().Object, Repository<PayrollPolicyProfile>().Object,
            Repository<PayrollPayGroup>().Object, Repository<SalaryTemplate>().Object,
            Repository(salary).Object);

        var result = await service.GeneratePayrollForMonthAsync("company-1", new DateTime(2026, 10, 1));

        Assert.True(result.Success, result.Message);
        Assert.Equal(1234m, generatedPayroll?.IncomeTax);
        Assert.Equal("payroll-1", generatedPayroll?.PayRollId);
    }

    [Fact]
    public async Task GeneratePayroll_PersistsSplitMonthSalaryRevisionsAndAppliesUnpaidLeaveAttendance()
    {
        var employee = new EmpUser
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", FirstName = "Test",
            LastName = "Employee", Email = "test@example.com", DateOfJoining = "2026-01-01", Status = true
        };
        var salaryV1 = new EmployeeSalaryStructure
        {
            Id = "55555555-5555-5555-5555-555555555555", CompanyId = "company-1", EmployeeId = "employee-1", Version = 1,
            EffectiveFrom = new DateTime(2026, 9, 1), EffectiveTo = new DateTime(2026, 9, 14),
            Status = "ACTIVE", ApprovalStatus = "APPROVED",
            Components = [new() { ComponentCode = "BASIC", ComponentName = "Basic", Value = 50000m, IsTaxable = true },
                new() { ComponentCode = "MEAL", ComponentName = "Meal Allowance", Kind = "EARNING", Value = 3000m, IsTaxable = true },
                new() { ComponentCode = "TRAVEL", ComponentName = "Travel Allowance", Kind = "EARNING", Value = 1000m, IsTaxable = true }]
        };
        var salaryV2 = new EmployeeSalaryStructure
        {
            Id = "66666666-6666-6666-6666-666666666666", CompanyId = "company-1", EmployeeId = "employee-1", Version = 2,
            EffectiveFrom = new DateTime(2026, 9, 15), Status = "ACTIVE", ApprovalStatus = "APPROVED",
            Components = [new() { ComponentCode = "BASIC", ComponentName = "Basic", Value = 60000m, IsTaxable = true },
                new() { ComponentCode = "MEAL", ComponentName = "Meal Allowance", Kind = "EARNING", Value = 6000m, IsTaxable = true },
                new() { ComponentCode = "TRAVEL", ComponentName = "Travel Allowance", Kind = "EARNING", Value = 2000m, IsTaxable = true }]
        };
        AddUpdatePayRollRequestDto? generatedPayroll = null;
        var payrollService = new Mock<IPayRollServices>();
        payrollService.Setup(x => x.AddUpdatePayRoll(It.IsAny<AddUpdatePayRollRequestDto>(), "company-1"))
            .Callback<AddUpdatePayRollRequestDto, string>((request, _) => generatedPayroll = request)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var divisor = new Mock<IPayrollDivisorPolicyService>();
        divisor.Setup(x => x.GetEffective("company-1", new DateTime(2026, 9, 1)))
            .ReturnsAsync(new PayrollDivisorPolicyDto { RequireAttendanceLock = false });
        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        workingCalendar.Setup(x => x.CountWorkingDaysAsync("company-1", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), It.IsAny<CancellationToken>())).ReturnsAsync(22);
        var service = new AutoPayrollServices(
            payrollService.Object, Repository(employee).Object, Repository<EmpPayRoll>().Object,
            Repository(
                new AttendanceModel { CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", Date = new DateTime(2026, 9, 29), Status = "PL", SourceType = "LEAVE", SourceId = "paid-leave-1", SourceVersion = 2 },
                new AttendanceModel { CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", Date = new DateTime(2026, 9, 30), Status = "UL", SourceType = "LEAVE", SourceId = "leave-1", SourceVersion = 3 }).Object,
            Repository<AttendanceDaySegment>().Object, Repository<SalaryModel>().Object,
            Repository(
                new AttendanceStatusSetting { CompanyId = "company-1", Code = "PL", Name = "Paid Leave", IsActive = true, PaidDayFraction = 1m, UnpaidDayFraction = 0m, IsAvailableForLeaveManagement = true },
                new AttendanceStatusSetting { CompanyId = "company-1", Code = "UL", Name = "Unpaid Leave", IsActive = true, PaidDayFraction = 0m, UnpaidDayFraction = 1m, IsAvailableForLeaveManagement = true }).Object,
            Repository<MonthlyAttendanceSummary>().Object,
            Repository<AttendancePayrollException>().Object, divisor.Object, workingCalendar.Object,
            Repository<EmployeePayrollAssignment>().Object, Repository<PayrollPolicyProfile>().Object,
            Repository<PayrollPayGroup>().Object, Repository<SalaryTemplate>().Object,
            Repository(salaryV1, salaryV2).Object);

        var result = await service.GeneratePayrollForMonthAsync("company-1", new DateTime(2026, 9, 1));

        Assert.True(result.Success, result.Message);
        Assert.NotNull(generatedPayroll?.CalculationSnapshot);
        Assert.Equal(1m, generatedPayroll.LossOfPayDays);
        Assert.Equal(29f, generatedPayroll.PaidDays);
        Assert.Equal(2048.89m, generatedPayroll.LossOfPay);
        Assert.Equal(6133.33m, generatedPayroll.OtherAllowance);
        Assert.Collection(generatedPayroll.EarningLines.OrderBy(x => x.Code),
            line => { Assert.Equal("MEAL", line.Code); Assert.Equal("Meal Allowance", line.Description); Assert.Equal(4600m, line.Amount); },
            line => { Assert.Equal("TRAVEL", line.Code); Assert.Equal("Travel Allowance", line.Description); Assert.Equal(1533.33m, line.Amount); });
        Assert.Equal(generatedPayroll.OtherAllowance, generatedPayroll.EarningLines.Sum(x => x.Amount));
        Assert.Collection(generatedPayroll.CalculationSnapshot.SalaryRevisions,
            revision => { Assert.Equal("55555555-5555-5555-5555-555555555555", revision.SalaryStructureId); Assert.Equal(1, revision.Version); },
            revision => { Assert.Equal("66666666-6666-6666-6666-666666666666", revision.SalaryStructureId); Assert.Equal(2, revision.Version); });
        Assert.Collection(generatedPayroll.CalculationSnapshot.SalaryStructures,
            structure => { Assert.Equal("55555555-5555-5555-5555-555555555555", structure.SalaryStructureId); Assert.Contains(structure.Components, component => component.Code == "BASIC" && component.Value == 50000m); },
            structure => { Assert.Equal("66666666-6666-6666-6666-666666666666", structure.SalaryStructureId); Assert.Contains(structure.Components, component => component.Code == "BASIC" && component.Value == 60000m); });
    }

    [Fact]
    public async Task GeneratePayroll_SkipsExpiredCanonicalSalaryWithoutQueryingStaleLegacySource()
    {
        var employee = new EmpUser
        {
            CompanyId = "company-1", UserId = "user-1", EmployeeId = "employee-1", FirstName = "Test",
            LastName = "Employee", Email = "test@example.com", DateOfJoining = "2026-01-01", Status = true
        };
        var expiredCanonical = new EmployeeSalaryStructure
        {
            CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 1, 1),
            EffectiveTo = new DateTime(2026, 8, 31), Status = "ACTIVE", ApprovalStatus = "APPROVED",
            Components = [new EmployeeSalaryComponentValue { ComponentCode = "BASIC", ComponentName = "Basic", Value = 50000m }]
        };
        var legacySalaries = new Mock<IMongoDbRepository<SalaryModel>>();
        legacySalaries.Setup(x => x.Get(It.IsAny<Expression<Func<SalaryModel, bool>>>(), It.IsAny<Func<IQueryable<SalaryModel>, IOrderedQueryable<SalaryModel>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<SalaryModel, bool>> _, Func<IQueryable<SalaryModel>, IOrderedQueryable<SalaryModel>>? _, bool _) => new[]
            {
                new SalaryModel { CompanyId = "company-1", EmployeeId = "employee-1", BasicPay = 90000m, EffectiveFrom = new DateTime(2025, 1, 1), PaymentFrequency = "MONTHLY", Currency = "INR" }
            }.AsQueryable());
        var payrollRows = Repository<EmpPayRoll>();
        payrollRows.Setup(x => x.Exist(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);
        var divisor = new Mock<IPayrollDivisorPolicyService>();
        divisor.Setup(x => x.GetEffective("company-1", new DateTime(2026, 10, 1))).ReturnsAsync(new PayrollDivisorPolicyDto { RequireAttendanceLock = false });
        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        workingCalendar.Setup(x => x.CountWorkingDaysAsync("company-1", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), It.IsAny<CancellationToken>())).ReturnsAsync(22);

        var service = new AutoPayrollServices(
            Mock.Of<IPayRollServices>(), Repository(employee).Object, payrollRows.Object,
            Repository<AttendanceModel>().Object, Repository<AttendanceDaySegment>().Object, legacySalaries.Object,
            Repository<AttendanceStatusSetting>().Object, Repository<MonthlyAttendanceSummary>().Object,
            Repository<AttendancePayrollException>().Object, divisor.Object, workingCalendar.Object,
            Repository<EmployeePayrollAssignment>().Object, Repository<PayrollPolicyProfile>().Object,
            Repository<PayrollPayGroup>().Object, Repository<SalaryTemplate>().Object,
            Repository(expiredCanonical).Object);

        var result = await service.GeneratePayrollForMonthAsync("company-1", new DateTime(2026, 10, 1));

        Assert.True(result.Success);
        Assert.Contains("1 employee(s) have no salary structure defined", result.Message);
        Assert.Contains("Employee IDs: employee-1.", result.Message);
        legacySalaries.Verify(x => x.Get(It.IsAny<Expression<Func<SalaryModel, bool>>>(), It.IsAny<Func<IQueryable<SalaryModel>, IOrderedQueryable<SalaryModel>>>(), It.IsAny<bool>()), Times.Never);
        payrollRows.Verify(x => x.AddOne(It.IsAny<EmpPayRoll>()), Times.Never);
    }

    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(params TEntity[] items)
    {
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<TEntity>>(items.Where(predicate.Compile()).ToArray()));
        repository.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _) => Task.FromResult(items.Any(predicate.Compile())));
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult(items.FirstOrDefault(predicate.Compile())));
        repository.Setup(x => x.Get(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy, bool _) =>
            {
                var compiled = predicate.Compile();
                var query = items.Where(compiled).AsQueryable();
                return orderBy is null ? query : orderBy(query);
            });
        return repository;
    }
}
