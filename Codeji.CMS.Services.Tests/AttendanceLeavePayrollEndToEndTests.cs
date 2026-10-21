using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

/// <summary>
/// Comprehensive end-to-end integration tests verifying the full lifecycle across:
/// 1. Attendance timing and grace limits
/// 2. Leave management and segment reconciliation
/// 3. Monthly attendance validation and locking
/// 4. Payroll calculation, Loss of Pay (LOP), statutory deductions, and Net Pay
/// </summary>
public sealed class AttendanceLeavePayrollEndToEndTests
{
    private const string CompanyId = "comp-e2e-1";
    private const string UserId = "user-e2e-1";
    private const string EmployeeId = "EMP-E2E-001";
    private static readonly DateTime MonthStart = new(2026, 3, 1);
    private static readonly DateTime MonthEnd = new(2026, 3, 31);

    [Fact]
    public async Task EndToEnd_AttendanceGrace_LeaveReconciliation_And_AutoPayroll_CalculatesFlawlessly()
    {
        // ARRANGE:
        // 1. Employee setup
        var employee = new EmpUser
        {
            CompanyId = CompanyId,
            UserId = UserId,
            EmployeeId = EmployeeId,
            FirstName = "Vikram",
            LastName = "Malhotra",
            Email = "vikram.m@example.com",
            DateOfJoining = "2026-01-01",
            Status = true
        };

        // 2. Attendance Status Settings
        var statusSettings = new List<AttendanceStatusSetting>
        {
            new() { CompanyId = CompanyId, Code = "P", Name = "Present", PaidDayFraction = 1m, UnpaidDayFraction = 0m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "A", Name = "Absent", PaidDayFraction = 0m, UnpaidDayFraction = 1m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "HD", Name = "Half Day", PaidDayFraction = 0.5m, UnpaidDayFraction = 0.5m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "LHD", Name = "Late Half Day", PaidDayFraction = 0.5m, UnpaidDayFraction = 0.5m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "ED", Name = "Early Departure", PaidDayFraction = 1m, UnpaidDayFraction = 0m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "LHD+ED", Name = "Late + Early", PaidDayFraction = 0.5m, UnpaidDayFraction = 0.5m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "CL", Name = "Casual Leave", PaidDayFraction = 1m, UnpaidDayFraction = 0m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "LOP", Name = "Loss of Pay Leave", PaidDayFraction = 0m, UnpaidDayFraction = 1m, IsActive = true }
        };

        // 3. Employee Attendance & Penalty Policy: 2 monthly grace occurrences allowed
        var penaltyPolicy = new AttendancePenaltyPolicy
        {
            Id = Guid.NewGuid().ToString(),
            CompanyId = CompanyId,
            UserId = UserId,
            EmployeeId = EmployeeId,
            CombinedLhdEdMonthlyLimit = 2,
            IsEnabled = true,
            EffectiveFrom = new DateTime(2026, 1, 1)
        };

        // 4. Attendance Records for March 2026 (31 days)
        // - Day 9: LHD (1st occurrence -> within grace, 0 unpaid)
        // - Day 11: ED (2nd occurrence -> within grace, 0 unpaid)
        // - Day 15: LHD (3rd occurrence -> EXCEEDS grace -> 0.5 unpaid)
        // - Day 20: Absent (A -> 1.0 unpaid)
        // - Day 25: Half Day (HD -> 0.5 unpaid)
        var attendanceRecords = new List<AttendanceModel>
        {
            new() { CompanyId = CompanyId, UserId = UserId, EmployeeId = EmployeeId, Date = new DateTime(2026, 3, 9), Status = "LHD" },
            new() { CompanyId = CompanyId, UserId = UserId, EmployeeId = EmployeeId, Date = new DateTime(2026, 3, 11), Status = "ED" },
            new() { CompanyId = CompanyId, UserId = UserId, EmployeeId = EmployeeId, Date = new DateTime(2026, 3, 15), Status = "LHD" },
            new() { CompanyId = CompanyId, UserId = UserId, EmployeeId = EmployeeId, Date = new DateTime(2026, 3, 20), Status = "A" },
            new() { CompanyId = CompanyId, UserId = UserId, EmployeeId = EmployeeId, Date = new DateTime(2026, 3, 25), Status = "HD" }
        };

        // 5. Leave Segments reconciled into Attendance:
        // - Day 2: 1 Approved Casual Leave (CL, Paid)
        // - Day 3: 1 Approved Unpaid Leave (LOP, Unpaid -> 1.0 unpaid)
        var attendanceSegments = new List<AttendanceDaySegment>
        {
            new() { CompanyId = CompanyId, UserId = UserId, EmployeeId = EmployeeId, Date = new DateTime(2026, 3, 2), Status = "CL", SourceType = "LEAVE" },
            new() { CompanyId = CompanyId, UserId = UserId, EmployeeId = EmployeeId, Date = new DateTime(2026, 3, 3), Status = "LOP", SourceType = "LEAVE" }
        };

        // 6. Attendance Summary: Locked
        var attendanceSummary = new MonthlyAttendanceSummary
        {
            Id = Guid.NewGuid().ToString(),
            CompanyId = CompanyId,
            UserId = UserId,
            EmployeeId = EmployeeId,
            PayrollMonth = MonthStart,
            IsApproved = true,
            IsLocked = true
        };

        // 7. HR-Reviewed Exception:
        // The 3rd punctuality infraction on Day 15 was reviewed by HR and approved with a warning / 0 additional penalty
        var attendanceException = new AttendancePayrollException
        {
            Id = Guid.NewGuid().ToString(),
            CompanyId = CompanyId,
            UserId = UserId,
            EmployeeId = EmployeeId,
            PayrollMonth = MonthStart,
            ExceptionType = "LHD_ED_LIMIT_EXCEEDED",
            Status = "WAIVED", // HR waived additional penalty
            DeductionDayFraction = 0m
        };

        // 8. Contractual Salary Structure:
        // Monthly Gross = ₹62,000 (Basic ₹31,000, HRA ₹15,500, Special Allowance ₹15,500)
        var salaryStructure = new EmployeeSalaryStructure
        {
            Id = Guid.NewGuid().ToString("D"),
            CompanyId = CompanyId,
            EmployeeId = EmployeeId,
            EffectiveFrom = new DateTime(2026, 1, 1),
            Status = "ACTIVE",
            ApprovalStatus = "APPROVED",
            Components =
            [
                new() { ComponentCode = "BASIC", ComponentName = "Basic", Value = 31000m, Kind = "EARNING", IsTaxable = true, IsRecurring = true },
                new() { ComponentCode = "HRA", ComponentName = "House Rent Allowance", Value = 15500m, Kind = "EARNING", IsTaxable = true, IsRecurring = true },
                new() { ComponentCode = "SPECIAL", ComponentName = "Special Allowance", Value = 15500m, Kind = "EARNING", IsTaxable = true, IsRecurring = true }
            ]
        };

        // Repositories
        var empRepo = MockRepo(employee);
        var payrollRepo = MockRepo<EmpPayRoll>();
        var attRepo = MockRepo(attendanceRecords.ToArray());
        var segRepo = MockRepo(attendanceSegments.ToArray());
        var salaryRepo = MockRepo<SalaryModel>();
        var statusRepo = MockRepo(statusSettings.ToArray());
        var summaryRepo = MockRepo(attendanceSummary);
        var excRepo = MockRepo(attendanceException);
        var policyRepo = MockRepo(penaltyPolicy);
        var salaryStructRepo = MockRepo(salaryStructure);

        var divisorMock = new Mock<IPayrollDivisorPolicyService>();
        divisorMock.Setup(d => d.GetEffective(CompanyId, MonthStart))
            .ReturnsAsync(new PayrollDivisorPolicyDto
            {
                DivisorPolicy = "CALENDAR_DAYS",
                RequireAttendanceLock = true,
                TdsMode = "AUTO_NEW_REGIME",
                EpfMode = "ENABLED",
                EpfWageCeiling = 15000m
            });

        var workingCalendarMock = new Mock<ICompanyWorkingCalendarService>();
        var payrollServiceMock = new Mock<IPayRollServices>();
        AddUpdatePayRollRequestDto? capturedPayroll = null;
        payrollServiceMock.Setup(p => p.AddUpdatePayRoll(It.IsAny<AddUpdatePayRollRequestDto>(), It.IsAny<string>()))
            .Callback<AddUpdatePayRollRequestDto, string>((dto, _) => capturedPayroll = dto)
            .ReturnsAsync(new Result { Success = true });

        var autoPayroll = new AutoPayrollServices(
            payrollServiceMock.Object,
            empRepo.Object,
            payrollRepo.Object,
            attRepo.Object,
            segRepo.Object,
            salaryRepo.Object,
            statusRepo.Object,
            summaryRepo.Object,
            excRepo.Object,
            divisorMock.Object,
            workingCalendarMock.Object,
            MockRepo<EmployeePayrollAssignment>().Object,
            MockRepo<PayrollPolicyProfile>().Object,
            payGroups: null,
            salaryTemplates: null,
            employeeSalaryStructures: salaryStructRepo.Object,
            penaltyPolicies: policyRepo.Object
        );

        // ACT:
        var result = await autoPayroll.GeneratePayrollForMonthAsync(CompanyId, MonthStart);

        // ASSERT:
        Assert.True(result.Success, result.Message);
        Assert.NotNull(capturedPayroll);
        Assert.Equal(EmployeeId, capturedPayroll.EmployeeId);

        // EXPECTED LOSS OF PAY DAYS BREAKDOWN:
        // - Day 2: Approved Casual Leave (CL) = 0.0 unpaid
        // - Day 3: Approved Unpaid Leave (LOP) = 1.0 unpaid
        // - Day 9: LHD (1st punctuality occurrence -> within grace) = 0.0 unpaid
        // - Day 11: ED (2nd punctuality occurrence -> within grace) = 0.0 unpaid
        // - Day 15: LHD (3rd punctuality occurrence -> exceeds grace limit of 2) = 0.5 unpaid
        // - Day 20: Absent (A) = 1.0 unpaid
        // - Day 25: Half Day (HD) = 0.5 unpaid
        // Total Unpaid Days = 1.0 + 0.0 + 0.0 + 0.5 + 1.0 + 0.5 = 3.0 days!
        // Total Paid Days in March (31 days) = 31 - 3.0 = 28.0 days!
        Assert.Equal(28f, capturedPayroll.PaidDays);
        Assert.Equal(3.0m, capturedPayroll.LossOfPayDays);

        // SALARY MATH VERIFICATION:
        // Contractual Gross = ₹62,000
        // Divisor = 31 days
        // Per day rate = ₹62,000 / 31 = ₹2,000.00
        // Total LOP = 3.0 days * ₹2,000.00 = ₹6,000.00
        // Earned Gross = ₹62,000 - ₹6,000 = ₹56,000.00
        Assert.Equal(6000m, capturedPayroll.LossOfPay);

        // Basic pay: contractual ₹31,000
        Assert.Equal(31000m, capturedPayroll.BasicPay);

        // EPF Calculation: 12% on statutory ceiling (₹15,000)
        // With ceiling of 15,000: 12% of ₹15,000 = ₹1,800.00
        Assert.Equal(1800m, capturedPayroll.EPF);

        var totalDeductions = (capturedPayroll.LossOfPay ?? 0m) + (capturedPayroll.EPF ?? 0m);
        Assert.Equal(7800m, totalDeductions);
    }

    [Fact]
    public async Task AutoPayroll_StrictlyBlocksPayroll_WhenAttendanceLockIsMissing()
    {
        var employee = new EmpUser
        {
            CompanyId = CompanyId,
            UserId = UserId,
            EmployeeId = EmployeeId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            DateOfJoining = "2026-01-01",
            Status = true
        };

        var divisorMock = new Mock<IPayrollDivisorPolicyService>();
        divisorMock.Setup(d => d.GetEffective(CompanyId, MonthStart))
            .ReturnsAsync(new PayrollDivisorPolicyDto { RequireAttendanceLock = true, TdsMode = "AUTO_NEW_REGIME" });

        // Missing attendance lock: summary repository returns null
        var summaryRepo = MockRepo<MonthlyAttendanceSummary>();

        var autoPayroll = new AutoPayrollServices(
            new Mock<IPayRollServices>().Object,
            MockRepo(employee).Object,
            MockRepo<EmpPayRoll>().Object,
            MockRepo<AttendanceModel>().Object,
            MockRepo<AttendanceDaySegment>().Object,
            MockRepo<SalaryModel>().Object,
            MockRepo<AttendanceStatusSetting>().Object,
            summaryRepo.Object,
            MockRepo<AttendancePayrollException>().Object,
            divisorMock.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            MockRepo<EmployeePayrollAssignment>().Object,
            MockRepo<PayrollPolicyProfile>().Object
        );

        var result = await autoPayroll.GeneratePayrollForMonthAsync(CompanyId, MonthStart);

        Assert.False(result.Success);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Message.Contains("must be approved and locked", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AutoPayroll_StrictlyBlocksPayroll_WhenBlockingExceptionRequiresReview()
    {
        var employee = new EmpUser
        {
            CompanyId = CompanyId,
            UserId = UserId,
            EmployeeId = EmployeeId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            DateOfJoining = "2026-01-01",
            Status = true
        };

        var divisorMock = new Mock<IPayrollDivisorPolicyService>();
        divisorMock.Setup(d => d.GetEffective(CompanyId, MonthStart))
            .ReturnsAsync(new PayrollDivisorPolicyDto { RequireAttendanceLock = true, TdsMode = "AUTO_NEW_REGIME" });

        var summary = new MonthlyAttendanceSummary
        {
            Id = Guid.NewGuid().ToString(),
            CompanyId = CompanyId,
            UserId = UserId,
            EmployeeId = EmployeeId,
            PayrollMonth = MonthStart,
            IsApproved = true,
            IsLocked = true
        };

        // Unresolved exception still pending review
        var pendingException = new AttendancePayrollException
        {
            Id = Guid.NewGuid().ToString(),
            CompanyId = CompanyId,
            UserId = UserId,
            EmployeeId = EmployeeId,
            PayrollMonth = MonthStart,
            ExceptionType = "LHD_ED_LIMIT_EXCEEDED",
            Status = "PENDING_REVIEW"
        };

        var autoPayroll = new AutoPayrollServices(
            new Mock<IPayRollServices>().Object,
            MockRepo(employee).Object,
            MockRepo<EmpPayRoll>().Object,
            MockRepo<AttendanceModel>().Object,
            MockRepo<AttendanceDaySegment>().Object,
            MockRepo<SalaryModel>().Object,
            MockRepo<AttendanceStatusSetting>().Object,
            MockRepo(summary).Object,
            MockRepo(pendingException).Object,
            divisorMock.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            MockRepo<EmployeePayrollAssignment>().Object,
            MockRepo<PayrollPolicyProfile>().Object
        );

        var result = await autoPayroll.GeneratePayrollForMonthAsync(CompanyId, MonthStart);

        Assert.False(result.Success);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Message.Contains("unresolved attendance exception requires review", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EndToEnd_WhenLhdEdCountsAs2_GraceLimitOf2CorrectlyExceededAndYields2LopDays()
    {
        // ARRANGE:
        // Satyam Dubey's exact production scenario
        const string satyamEmpId = "EMP-SATYAM-2026";
        const string satyamUserId = "USER-SATYAM";
        var satyam = new EmpUser
        {
            CompanyId = CompanyId,
            UserId = satyamUserId,
            EmployeeId = satyamEmpId,
            FirstName = "Satyam",
            LastName = "Dubey",
            Email = "satyam@example.com",
            DateOfJoining = "2026-01-01",
            Status = true
        };

        var statusSettings = new List<AttendanceStatusSetting>
        {
            new() { CompanyId = CompanyId, Code = "P", Name = "Present", PaidDayFraction = 1m, UnpaidDayFraction = 0m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "A", Name = "Absent", PaidDayFraction = 0m, UnpaidDayFraction = 1m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "HD", Name = "Half Day", PaidDayFraction = 0.5m, UnpaidDayFraction = 0.5m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "LHD", Name = "Late Half Day", PaidDayFraction = 0.5m, UnpaidDayFraction = 0.5m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "ED", Name = "Early Departure", PaidDayFraction = 1m, UnpaidDayFraction = 0m, IsActive = true },
            new() { CompanyId = CompanyId, Code = "LHD+ED", Name = "Late + Early", PaidDayFraction = 0.5m, UnpaidDayFraction = 0.5m, IsActive = true }
        };

        var penaltyPolicy = new AttendancePenaltyPolicy
        {
            CompanyId = CompanyId,
            UserId = satyamUserId,
            EmployeeId = satyamEmpId,
            CombinedLhdEdMonthlyLimit = 2,
            IsEnabled = true,
            EffectiveFrom = new DateTime(2026, 1, 1)
        };

        // Satyam's 4 attendance events in March 2026:
        // 1. 03 Mar: HD (check-in 11:01) -> 0.5 unpaid
        // 2. 09 Mar: LHD (weight 1 -> running count 1 <= 2 -> grace exempt, 0.0 unpaid)
        // 3. 11 Mar: LHD+ED (weight 2 -> running count 1 + 2 = 3 > 2 -> EXCEEDS grace limit -> 0.5 unpaid)
        // 4. 27 Mar: Absent (A) -> 1.0 unpaid
        var satyamRecords = new List<AttendanceModel>
        {
            new() { CompanyId = CompanyId, UserId = satyamUserId, EmployeeId = satyamEmpId, Date = new DateTime(2026, 3, 3), Status = "HD" },
            new() { CompanyId = CompanyId, UserId = satyamUserId, EmployeeId = satyamEmpId, Date = new DateTime(2026, 3, 9), Status = "LHD" },
            new() { CompanyId = CompanyId, UserId = satyamUserId, EmployeeId = satyamEmpId, Date = new DateTime(2026, 3, 11), Status = "LHD+ED" },
            new() { CompanyId = CompanyId, UserId = satyamUserId, EmployeeId = satyamEmpId, Date = new DateTime(2026, 3, 27), Status = "A" }
        };

        // 4. Punctuality math verification via AttendancePayrollRules:
        var lhdCount = satyamRecords.Count(r => r.Status is "LHD" or "LHD+ED");
        var edCount = satyamRecords.Count(r => r.Status is "ED" or "LHD+ED");
        var combinedCount = AttendancePayrollRules.CalculateCombinedPunctualityCount(satyamRecords.Select(r => r.Status));
        const int allowedLimit = 2;
        var exceededCount = Math.Max(0, combinedCount - allowedLimit);

        Assert.Equal(2, lhdCount); // 09 Mar (LHD) + 11 Mar (LHD+ED)
        Assert.Equal(1, edCount);  // 11 Mar (LHD+ED)
        Assert.Equal(3, combinedCount); // 2 + 1 = 3
        Assert.Equal(1, exceededCount); // 3 - 2 = 1

        var exemptDates = AttendancePayrollRules.ResolveGraceExemptDates(
            satyamRecords.Where(r => AttendancePayrollRules.IsPunctualityInfraction(r.Status)).Select(r => (r.Date, r.Status)),
            allowedLimit).ToList();

        // 09 Mar LHD consumes 1 grace occurrence (<= 2) -> EXEMPT
        // 11 Mar LHD+ED consumes 2 occurrences (1 + 2 = 3 > 2) -> NOT EXEMPT
        Assert.Single(exemptDates);
        Assert.Equal(new DateTime(2026, 3, 9), exemptDates[0]);

        var summary = new MonthlyAttendanceSummary
        {
            Id = Guid.NewGuid().ToString(),
            CompanyId = CompanyId,
            UserId = satyamUserId,
            EmployeeId = satyamEmpId,
            PayrollMonth = MonthStart,
            LhdCount = lhdCount,
            EdCount = edCount,
            CombinedLhdEdCount = combinedCount,
            BlockingExceptionCount = 0,
            IsApproved = true,
            IsLocked = true
        };

        // Structure: ₹33,250
        var salaryStructure = new EmployeeSalaryStructure
        {
            Id = Guid.NewGuid().ToString("D"),
            CompanyId = CompanyId,
            EmployeeId = satyamEmpId,
            EffectiveFrom = new DateTime(2026, 1, 1),
            Status = "ACTIVE",
            ApprovalStatus = "APPROVED",
            Components =
            [
                new() { ComponentCode = "BASIC", ComponentName = "Basic", Value = 16625m, Kind = "EARNING", IsTaxable = true, IsRecurring = true },
                new() { ComponentCode = "HRA", ComponentName = "House Rent Allowance", Value = 8312.50m, Kind = "EARNING", IsTaxable = true, IsRecurring = true },
                new() { ComponentCode = "SPECIAL", ComponentName = "Special Allowance", Value = 8312.50m, Kind = "EARNING", IsTaxable = true, IsRecurring = true }
            ]
        };

        var divisorMock = new Mock<IPayrollDivisorPolicyService>();
        divisorMock.Setup(d => d.GetEffective(CompanyId, MonthStart))
            .ReturnsAsync(new PayrollDivisorPolicyDto
            {
                DivisorPolicy = "CALENDAR_DAYS",
                RequireAttendanceLock = true,
                TdsMode = "AUTO_NEW_REGIME",
                EpfMode = "DISABLED"
            });

        // Locked summary & approved/waived exception
        summary.IsLocked = true;
        summary.IsApproved = true;

        var hrApprovedException = new AttendancePayrollException
        {
            CompanyId = CompanyId,
            UserId = satyamUserId,
            EmployeeId = satyamEmpId,
            PayrollMonth = MonthStart,
            ExceptionType = "LHD_ED_LIMIT_EXCEEDED",
            Status = "APPROVED",
            DeductionDayFraction = 0m
        };

        AddUpdatePayRollRequestDto? capturedPayroll = null;
        var payrollServiceMock = new Mock<IPayRollServices>();
        payrollServiceMock.Setup(p => p.AddUpdatePayRoll(It.IsAny<AddUpdatePayRollRequestDto>(), It.IsAny<string>()))
            .Callback<AddUpdatePayRollRequestDto, string>((dto, _) => capturedPayroll = dto)
            .ReturnsAsync(new Result { Success = true });

        var autoPayroll = new AutoPayrollServices(
            payrollServiceMock.Object,
            MockRepo(satyam).Object,
            MockRepo<EmpPayRoll>().Object,
            MockRepo(satyamRecords.ToArray()).Object,
            MockRepo<AttendanceDaySegment>().Object,
            MockRepo<SalaryModel>().Object,
            MockRepo(statusSettings.ToArray()).Object,
            MockRepo(summary).Object,
            MockRepo(hrApprovedException).Object,
            divisorMock.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            MockRepo<EmployeePayrollAssignment>().Object,
            MockRepo<PayrollPolicyProfile>().Object,
            payGroups: null,
            salaryTemplates: null,
            employeeSalaryStructures: MockRepo(salaryStructure).Object,
            penaltyPolicies: MockRepo(penaltyPolicy).Object
        );

        var result = await autoPayroll.GeneratePayrollForMonthAsync(CompanyId, MonthStart);
        Assert.True(result.Success, result.Message);
        Assert.NotNull(capturedPayroll);

        // Verification:
        // Total LOP days = 0.5 (03 Mar HD) + 0.5 (11 Mar LHD+ED unexcused) + 1.0 (27 Mar Absent) = 2.0 days
        Assert.Equal(2.0m, capturedPayroll.LossOfPayDays);
        // Total Paid days in 31-day March = 31 - 2.0 = 29.0 days
        Assert.Equal(29f, capturedPayroll.PaidDays);

        // LOP Amount = 2.0 * (33,250 / 31) = ₹2,145.16
        var expectedLop = Math.Round(2.0m * (33250m / 31m), 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedLop, capturedPayroll.LossOfPay);
        Assert.Equal(2145.16m, capturedPayroll.LossOfPay);
    }

    private static Mock<IMongoDbRepository<TEntity>> MockRepo<TEntity>(params TEntity[] items)
    {
        var list = items.ToList();
        var repo = new Mock<IMongoDbRepository<TEntity>>();
        repo.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<TEntity>>(list.Where(predicate.Compile()).ToArray()));
        repo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult(list.FirstOrDefault(predicate.Compile())));
        repo.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _) => Task.FromResult(list.Any(predicate.Compile())));
        repo.Setup(x => x.Get(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy, bool _) =>
            {
                var query = predicate == null ? list.AsQueryable() : list.Where(predicate.Compile()).AsQueryable();
                return orderBy != null ? orderBy(query) : query;
            });
        return repo;
    }
}

