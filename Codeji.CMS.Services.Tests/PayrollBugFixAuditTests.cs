using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class PayrollBugFixAuditTests
{
    [Theory]
    [InlineData("2026-10-01", "2026-10-16", false, true)] // Active month: not future, can process draft
    [InlineData("2026-09-01", "2026-10-16", false, true)] // Completed past month: not future, can process
    [InlineData("2026-11-01", "2026-10-16", true, false)] // Future month: is future, blocked
    public void Bug1_PayrollPeriodRules_AllowsActiveMonthDraft_BlocksFutureMonth(
        string periodStartStr, string nowStr, bool expectedFuture, bool expectedCanProcess)
    {
        var periodStart = DateTime.Parse(periodStartStr);
        var now = DateTime.Parse(nowStr);

        Assert.Equal(expectedFuture, PayrollPeriodRules.IsFuturePeriod(periodStart, now));
        Assert.Equal(expectedCanProcess, PayrollPeriodRules.CanProcessPayroll(periodStart, now));
    }

    [Fact]
    public async Task Bug2_DivisorPolicy_RequireAttendanceLock_ControlsLockRequirement()
    {
        var policyRepo = InMemoryRepository.Create(
            new PayrollDivisorPolicy
            {
                Id = "pol-1",
                CompanyId = "comp-1",
                EffectiveFrom = new DateTime(2026, 1, 1),
                IsActive = true,
                RequireAttendanceLock = false,
                DivisorPolicy = "CALENDAR_DAYS",
                LopCalculationBase = "FIXED_GROSS"
            }
        );
        var service = new PayrollDivisorPolicyService(policyRepo.Object);
        var effective = await service.GetEffective("comp-1", new DateTime(2026, 10, 1));

        Assert.False(effective.RequireAttendanceLock);

        var updateResult = await service.Save("comp-1", "admin-1", new PayrollDivisorPolicyDto
        {
            EffectiveFrom = new DateTime(2026, 10, 1),
            RequireAttendanceLock = true,
            DivisorPolicy = "CALENDAR_DAYS",
            LopCalculationBase = "FIXED_GROSS",
            EpfMode = "CAPPED_CEILING",
            EsicMode = "STATUTORY",
            TdsMode = "AUTO_NEW_REGIME"
        });

        Assert.True(updateResult.Success);
        var updatedEffective = await service.GetEffective("comp-1", new DateTime(2026, 10, 1));
        Assert.True(updatedEffective.RequireAttendanceLock);
    }

    [Fact]
    public async Task Bug3_And_Bug11_ValidateAndLock_IsolatesLocking_LocksCleanEmployeesAndReportsPartialWhenIssuesExist()
    {
        var companyId = "comp-1";
        var month = new DateTime(2026, 8, 1);
        var actorUserId = "admin-user";

        var empClean = new EmpUser
        {
            UserId = "user-1",
            EmployeeId = "EMP001",
            CompanyId = companyId,
            FirstName = "Clean",
            LastName = "Employee",
            Email = "clean@example.com",
            DateOfJoining = "2026-08-31",
            Status = true
        };

        var empBlocked = new EmpUser
        {
            UserId = "user-2",
            EmployeeId = "EMP002",
            CompanyId = companyId,
            FirstName = "Blocked",
            LastName = "Employee",
            Email = "blocked@example.com",
            DateOfJoining = "2026-08-31",
            Status = true
        };

        var employeesRepo = InMemoryRepository.Create(empClean, empBlocked);

        var summaryClean = new MonthlyAttendanceSummary
        {
            Id = "sum-1",
            CompanyId = companyId,
            UserId = "user-1",
            EmployeeId = "EMP001",
            PayrollMonth = month,
            IsLocked = false
        };

        var summaryBlocked = new MonthlyAttendanceSummary
        {
            Id = "sum-2",
            CompanyId = companyId,
            UserId = "user-2",
            EmployeeId = "EMP002",
            PayrollMonth = month,
            IsLocked = false
        };

        var summariesRepo = InMemoryRepository.Create(summaryClean, summaryBlocked);

        var blockingException = new AttendancePayrollException
        {
            Id = "exc-1",
            CompanyId = companyId,
            UserId = "user-2",
            EmployeeId = "EMP002",
            PayrollMonth = month,
            AttendanceDate = new DateTime(2026, 8, 31),
            Status = "PENDING_REVIEW",
            Severity = "BLOCKING",
            ExceptionType = "MISSING_PUNCH"
        };
        var exceptionsRepo = InMemoryRepository.Create(blockingException);

        var statusSettingsRepo = InMemoryRepository.Create(
            new AttendanceStatusSetting { CompanyId = companyId, Code = "P", Name = "Present", RequiresTime = false, PaidDayFraction = 1m }
        );

        var weeklyOffsRepo = InMemoryRepository.Create(
            new WeeklyOffSetting { CompanyId = companyId, OffDays = new List<int> { (int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday } }
        );

        var calendarRepo = InMemoryRepository.Create<CalendarEntity>();
        var policiesRepo = InMemoryRepository.Create<AttendancePenaltyPolicy>();
        
        var recordClean = new AttendanceModel
        {
            AttendanceId = "att-clean-1",
            CompanyId = companyId,
            UserId = "user-1",
            EmployeeId = "EMP001",
            Date = new DateTime(2026, 8, 31),
            Status = "P"
        };
        var attendanceRepo = InMemoryRepository.Create(recordClean);
        var segmentsRepo = InMemoryRepository.Create<AttendanceDaySegment>();
        var outboxRepo = InMemoryRepository.Create<AttendanceNotificationOutbox>();

        var service = new AttendancePenaltyService(
            policiesRepo.Object,
            exceptionsRepo.Object,
            summariesRepo.Object,
            attendanceRepo.Object,
            segmentsRepo.Object,
            statusSettingsRepo.Object,
            employeesRepo.Object,
            weeklyOffsRepo.Object,
            calendarRepo.Object,
            outboxRepo.Object,
            new Mock<IAttendanceRepository>().Object,
            new Mock<IAttendanceMutationValidator>().Object,
            new Mock<IAttendanceAuditWriter>().Object
        );

        var lockResult = await service.ValidateAndLock(companyId, actorUserId, month);

        // Assert: Partial lock succeeds! Clean employee is locked, blocked employee remains unlocked
        Assert.True(lockResult.Success);
        Assert.Contains("Locked attendance for 1 employee(s)", lockResult.Message);
        Assert.Contains("1 employee(s) have", lockResult.Message);
        Assert.True(summaryClean.IsLocked);
        Assert.True(summaryClean.IsApproved);
        Assert.False(summaryBlocked.IsLocked);
    }

    [Fact]
    public void Bug4_MidYearJoinerTdsMath_CalculatesZeroTaxWhenAnnualIncomeBelowRebateThreshold()
    {
        decimal monthlyTaxableIncome = 100000m;
        int remainingMonthsInFy = 5;
        decimal projectedAnnualIncome = monthlyTaxableIncome * remainingMonthsInFy;

        decimal monthlyTds = PayrollCalculationRules.CalculateMonthlyTds(
            projectedAnnualIncome,
            "AUTO_NEW_REGIME",
            ytdTaxAlreadyDeducted: 0m,
            remainingMonthsInFy: remainingMonthsInFy);

        Assert.Equal(0m, monthlyTds);
    }

    [Fact]
    public void Bug4_MidYearJoinerTdsMath_CalculatesProportionalTaxWhenAboveThreshold()
    {
        int remainingMonthsInFy = 6;
        decimal monthlyTaxableIncome = 250000m;
        decimal projectedAnnualIncome = monthlyTaxableIncome * remainingMonthsInFy;

        decimal monthlyTds = PayrollCalculationRules.CalculateMonthlyTds(
            projectedAnnualIncome,
            "AUTO_NEW_REGIME",
            ytdTaxAlreadyDeducted: 0m,
            remainingMonthsInFy: remainingMonthsInFy);

        decimal expectedAnnualTax = 97500m;
        decimal expectedMonthlyTds = 16250.00m;

        Assert.Equal(expectedMonthlyTds, monthlyTds);
    }

    [Fact]
    public void Bug5_WorkingDaysDivisor_ProrationRateEqualsDailyLopRate()
    {
        int totalWorkingDays = 20;
        int eligibleWorkingDays = 10;
        decimal monthlyBasic = 60000m;

        decimal resolvedDivisor = PayrollCalculationRules.ResolveDivisor("WORKING_DAYS", 30, totalWorkingDays);
        decimal perDayLopRate = PayrollCalculationRules.ResolvePerDayLopRate("BASIC_ONLY", monthlyBasic, monthlyBasic, resolvedDivisor);

        decimal prorationFactor = (decimal)eligibleWorkingDays / totalWorkingDays;
        decimal earnedBasic = PayrollCalculationRules.RoundMoney(monthlyBasic * prorationFactor);

        decimal dailyEarnedRate = earnedBasic / eligibleWorkingDays;
        Assert.Equal(perDayLopRate, dailyEarnedRate);
        Assert.Equal(3000m, perDayLopRate);
        Assert.Equal(30000m, earnedBasic);
    }

    [Fact]
    public async Task Bug6_AttendancePayrollReadiness_MatchesByEmployeeIdOrUserId()
    {
        var correctionsRepo = new Mock<IMongoDbRepository<AttendanceCorrectionRequest>>();
        var employeesRepo = new Mock<IMongoDbRepository<EmpUser>>();

        var pendingCorrection = new AttendanceCorrectionRequest
        {
            Id = "corr-1",
            CompanyId = "comp-1",
            UserId = "user-guid-123",
            AttendanceDate = new DateTime(2026, 10, 5),
            Status = "Pending"
        };

        correctionsRepo.Setup(x => x.GetAll(
            It.IsAny<Expression<Func<AttendanceCorrectionRequest, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()
        )).ReturnsAsync(new List<AttendanceCorrectionRequest> { pendingCorrection });

        var emp = new EmpUser
        {
            CompanyId = "comp-1",
            UserId = "user-guid-123",
            EmployeeId = "EMP101",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        employeesRepo.Setup(x => x.GetAll(
            It.IsAny<Expression<Func<EmpUser, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()
        )).ReturnsAsync(new List<EmpUser> { emp });

        var readinessService = new AttendancePayrollReadinessService(correctionsRepo.Object, employeesRepo.Object);

        var result = await readinessService.EvaluateAsync("comp-1", new DateTime(2026, 10, 1), new[] { "EMP101" });

        Assert.False(result.IsReady);
        Assert.Single(result.BlockingReasons);
        Assert.Contains("EMP101: unresolved attendance correction request", result.BlockingReasons[0]);
    }

    [Fact]
    public async Task Bug7_AttendancePenaltyService_Recalculate_IncludesDaySegmentsInLhdEdPenalties()
    {
        var companyId = "comp-1";
        var month = new DateTime(2026, 8, 1);

        var emp = new EmpUser
        {
            UserId = "user-1",
            EmployeeId = "EMP001",
            CompanyId = companyId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            DateOfJoining = "2025-01-01",
            Status = true
        };
        var employeesRepo = InMemoryRepository.Create(emp);

        var policy = new AttendancePenaltyPolicy
        {
            Id = "pol-1",
            CompanyId = companyId,
            UserId = "user-1",
            EmployeeId = "EMP001",
            CombinedLhdEdMonthlyLimit = 2,
            IsEnabled = true,
            EffectiveFrom = new DateTime(2026, 1, 1),
            Version = 1
        };
        var policiesRepo = InMemoryRepository.Create(policy);

        var record = new AttendanceModel
        {
            AttendanceId = "att-1",
            CompanyId = companyId,
            UserId = "user-1",
            EmployeeId = "EMP001",
            Date = new DateTime(2026, 8, 3),
            Status = "LHD"
        };
        var attendanceRepo = InMemoryRepository.Create(record);

        var segment1 = new AttendanceDaySegment
        {
            AttendanceDaySegmentId = "seg-1",
            CompanyId = companyId,
            UserId = "user-1",
            EmployeeId = "EMP001",
            Date = new DateTime(2026, 8, 4),
            Status = "ED"
        };
        var segment2 = new AttendanceDaySegment
        {
            AttendanceDaySegmentId = "seg-2",
            CompanyId = companyId,
            UserId = "user-1",
            EmployeeId = "EMP001",
            Date = new DateTime(2026, 8, 5),
            Status = "LHD"
        };
        var segmentsRepo = InMemoryRepository.Create(segment1, segment2);

        var exceptionsRepo = InMemoryRepository.Create<AttendancePayrollException>();
        var summariesRepo = InMemoryRepository.Create<MonthlyAttendanceSummary>();
        var statusSettingsRepo = InMemoryRepository.Create(
            new AttendanceStatusSetting { CompanyId = companyId, Code = "LHD", Name = "Late Half Day", RequiresTime = false },
            new AttendanceStatusSetting { CompanyId = companyId, Code = "ED", Name = "Early Departure", RequiresTime = false }
        );
        var weeklyOffsRepo = InMemoryRepository.Create(new WeeklyOffSetting { CompanyId = companyId });
        var calendarRepo = InMemoryRepository.Create<CalendarEntity>();
        var outboxRepo = InMemoryRepository.Create<AttendanceNotificationOutbox>();

        var service = new AttendancePenaltyService(
            policiesRepo.Object,
            exceptionsRepo.Object,
            summariesRepo.Object,
            attendanceRepo.Object,
            segmentsRepo.Object,
            statusSettingsRepo.Object,
            employeesRepo.Object,
            weeklyOffsRepo.Object,
            calendarRepo.Object,
            outboxRepo.Object,
            new Mock<IAttendanceRepository>().Object,
            new Mock<IAttendanceMutationValidator>().Object,
            new Mock<IAttendanceAuditWriter>().Object
        );

        var recalculateResult = await service.Recalculate(companyId, month);

        var allExceptions = await exceptionsRepo.Object.GetAll(x => x.CompanyId == companyId);
        var createdException = Assert.Single(allExceptions.Where(x => x.ExceptionType == "LHD_ED_LIMIT_EXCEEDED"));

        Assert.Equal(3, createdException.CombinedOccurrenceCount);
        Assert.Equal(1, createdException.ExceededOccurrenceCount);
        Assert.Equal(2, createdException.LhdCount);
        Assert.Equal(1, createdException.EdCount);
        Assert.Contains("att-1", createdException.AffectedAttendanceRecordIds);
        Assert.Contains("seg-1", createdException.AffectedAttendanceRecordIds);
        Assert.Contains("seg-2", createdException.AffectedAttendanceRecordIds);
    }

    [Fact]
    public async Task Bug9_LeaveReconciliation_WhenMonthLocked_CreatesBlockingExceptionWithoutMutatingAttendance()
    {
        var companyId = "comp-1";
        var leaveRequestId = "leave-100";
        var lockedMonth = new DateTime(2026, 8, 1);
        var leaveDate = new DateTime(2026, 8, 10);

        var leave = new LeaveRequest
        {
            LeaveRequestId = leaveRequestId,
            CompanyId = companyId,
            EmployeeId = "user-1",
            LeavePolicyId = "pol-leave",
            Reason = "Personal work",
            Status = EnumsHelper.LeaveRequestStatus.Accepted,
            StartDate = leaveDate,
            EndDate = leaveDate,
            IsHalfDay = false,
            Version = 1
        };
        var leavesRepo = InMemoryRepository.Create(leave);
        var leavesCollection = new Mock<IMongoCollection<LeaveRequest>>();
        leavesRepo.Setup(x => x.GetCollection()).Returns(leavesCollection.Object);
        leavesCollection.Setup(x => x.UpdateOneAsync(
            It.IsAny<FilterDefinition<LeaveRequest>>(),
            It.IsAny<UpdateDefinition<LeaveRequest>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var policy = new LeavePolicy
        {
            Id = "pol-leave",
            CompanyId = companyId,
            Name = "Casual Leave",
            Code = "CL",
            Status = true,
            PolicyType = EnumsHelper.LeavePolicyType.Leave,
            Paid = true,
            FullDayAttendanceStatusCode = "CL",
            AttendanceStatusCode = "CL"
        };
        var policiesRepo = InMemoryRepository.Create(policy);

        var employee = new EmpUser
        {
            CompanyId = companyId,
            UserId = "user-1",
            EmployeeId = "EMP001",
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            DateOfJoining = "2025-01-01",
            Status = true
        };
        var employeesRepo = InMemoryRepository.Create(employee);

        var statusSetting = new AttendanceStatusSetting
        {
            CompanyId = companyId,
            Code = "CL",
            Name = "Casual Leave",
            IsActive = true,
            RequiresTime = false
        };
        var statusesRepo = InMemoryRepository.Create(statusSetting);

        var lockedSummary = new MonthlyAttendanceSummary
        {
            Id = "sum-locked-1",
            CompanyId = companyId,
            UserId = "user-1",
            EmployeeId = "EMP001",
            PayrollMonth = lockedMonth,
            IsLocked = true
        };
        var summariesRepo = InMemoryRepository.Create(lockedSummary);

        var attendanceRepo = new Mock<IMongoDbRepository<AttendanceModel>>();
        attendanceRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceModel>());

        var segmentsRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        segmentsRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());

        var exceptionsRepo = InMemoryRepository.Create<AttendancePayrollException>();

        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        workingCalendar.Setup(x => x.GetLeaveDatesAsync(
            companyId,
            DateOnly.FromDateTime(leaveDate),
            DateOnly.FromDateTime(leaveDate),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new[] { DateOnly.FromDateTime(leaveDate) });

        var service = new LeaveAttendanceReconciliationService(
            leavesRepo.Object,
            policiesRepo.Object,
            employeesRepo.Object,
            statusesRepo.Object,
            attendanceRepo.Object,
            segmentsRepo.Object,
            exceptionsRepo.Object,
            summariesRepo.Object,
            workingCalendar.Object,
            new Mock<IAttendanceInitializationService>().Object
        );

        var result = await service.ReconcileAcceptedLeaveAsync(companyId, leaveRequestId, "actor-1");

        // The month is locked, so reconciliation creates an auditable blocking exception and avoids mutating attendance!
        var allExceptions = await exceptionsRepo.Object.GetAll(x => x.CompanyId == companyId);
        var createdException = Assert.Single(allExceptions);

        Assert.Equal("LEAVE_APPLIED_FOR_LOCKED_MONTH", createdException.ExceptionType);
        Assert.Equal("BLOCKING", createdException.Severity);
        Assert.Equal(lockedMonth, createdException.PayrollMonth);
        Assert.Equal(leaveDate.Date, createdException.AttendanceDate);

        // Crucial safety guarantee: Attendance table was NEVER written or mutated for the locked month
        attendanceRepo.Verify(x => x.AddOne(It.IsAny<AttendanceModel>()), Times.Never);
        attendanceRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()), Times.Never);
    }

    [Fact]
    public async Task Bug10_PayrollRunWorkflow_Process_ScopesUpdateToRunEmployeesOnly()
    {
        var runsRepo = new Mock<IMongoDbRepository<PayrollRun>>();
        var runEmployeesRepo = new Mock<IMongoDbRepository<PayrollRunEmployee>>();
        var payrollsRepo = new Mock<IMongoDbRepository<EmpPayRoll>>();

        var run = new PayrollRun
        {
            Id = "run-123",
            CompanyId = "comp-1",
            PeriodStart = new DateTime(2026, 10, 1),
            PeriodEnd = new DateTime(2026, 10, 31),
            PayDate = new DateTime(2026, 10, 31),
            Status = "APPROVED",
            Version = 1
        };

        runsRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(run);
        runsRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<PayrollRun>>(), It.IsAny<PayrollRun>()))
            .ReturnsAsync(new Result { Success = true });

        var runEmployee = new PayrollRunEmployee
        {
            Id = "re-1",
            PayrollRunId = "run-123",
            CompanyId = "comp-1",
            EmployeeId = "EMP001",
            Status = "CALCULATED"
        };

        runEmployeesRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<PayrollRunEmployee, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<PayrollRunEmployee> { runEmployee });

        FilterDefinition<EmpPayRoll>? capturedFilter = null;
        payrollsRepo.Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()))
            .Callback<FilterDefinition<EmpPayRoll>, UpdateDefinition<EmpPayRoll>, bool>((f, u, d) => capturedFilter = f)
            .ReturnsAsync(new Result { Success = true });

        var workflowService = new PayrollRunWorkflowService(runsRepo.Object, runEmployeesRepo.Object, payrollsRepo.Object);
        var transitionResult = await workflowService.Process("comp-1", "run-123", "actor-1");

        Assert.True(transitionResult.Success);
        payrollsRepo.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpPayRoll>>(), It.IsAny<UpdateDefinition<EmpPayRoll>>(), It.IsAny<bool>()), Times.Once);
        Assert.NotNull(capturedFilter);
    }

    [Fact]
    public async Task Bug12_ValidatePayrollData_ToleratesSubFiftyCentsRoundingDifference()
    {
        var payrollRepo = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeesRepo = new Mock<IMongoDbRepository<EmpUser>>();
        employeesRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { new EmpUser { EmployeeId = "EMP001", CompanyId = "comp-1", FirstName = "Test", LastName = "User", Email = "test@example.com" } });
        payrollRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);

        var service = new PayRollServices(
            payrollRepo.Object,
            employeesRepo.Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(),
            new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        // Itemized lines sum to 100.02, while OtherAllowance = 100.00 (difference = 0.02 <= 0.50 tolerance)
        var result = await service.ValidatePayrollData(new PayrollValidationRequestDto
        {
            PayMonth = new DateTime(2026, 8, 1),
            PayData = new List<EmployeePayRollModel>
            {
                new EmployeePayRollModel
                {
                    EmployeeId = "EMP001",
                    BasicPay = 10000m,
                    PaidDays = 31,
                    PaidDate = new DateTime(2026, 8, 31),
                    HRA = 2000m,
                    LossOfPayDays = 0,
                    LossOfPay = 0,
                    IncomeTax = 0,
                    OtherAllowance = 100.00m,
                    EarningLines = new List<PayrollEarningLineDto>
                    {
                        new() { Code = "BONUS1", Description = "Bonus 1", Amount = 33.34m },
                        new() { Code = "BONUS2", Description = "Bonus 2", Amount = 33.34m },
                        new() { Code = "BONUS3", Description = "Bonus 3", Amount = 33.34m }
                    }
                }
            }
        }, "comp-1");

        Assert.True(result.Success);
        Assert.Empty(result.MethodResults);
    }

    [Fact]
    public async Task Bug12_ValidatePayrollData_RejectsDiscrepancyExceedingFiftyCents()
    {
        var payrollRepo = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var employeesRepo = new Mock<IMongoDbRepository<EmpUser>>();
        employeesRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { new EmpUser { EmployeeId = "EMP001", CompanyId = "comp-1", FirstName = "Test", LastName = "User", Email = "test@example.com" } });
        payrollRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<EmpPayRoll, bool>>>(), It.IsAny<bool>())).ReturnsAsync(false);

        var service = new PayRollServices(
            payrollRepo.Object,
            employeesRepo.Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new PdfService(),
            new Mock<IMiddlewareService>().Object,
            new Mock<IAttendancePayrollReadinessService>().Object);

        // Itemized lines sum to 105.00, while OtherAllowance = 100.00 (difference = 5.00 > 0.50 tolerance)
        var result = await service.ValidatePayrollData(new PayrollValidationRequestDto
        {
            PayMonth = new DateTime(2026, 8, 1),
            PayData = new List<EmployeePayRollModel>
            {
                new EmployeePayRollModel
                {
                    EmployeeId = "EMP001",
                    BasicPay = 10000m,
                    PaidDays = 31,
                    PaidDate = new DateTime(2026, 8, 31),
                    HRA = 2000m,
                    LossOfPayDays = 0,
                    LossOfPay = 0,
                    IncomeTax = 0,
                    OtherAllowance = 100.00m,
                    EarningLines = new List<PayrollEarningLineDto>
                    {
                        new() { Code = "BONUS1", Description = "Bonus 1", Amount = 50.00m },
                        new() { Code = "BONUS2", Description = "Bonus 2", Amount = 55.00m }
                    }
                }
            }
        }, "comp-1");

        Assert.False(result.Success);
        Assert.Contains(result.MethodResults, error => error.Contains("itemized other earnings must equal the Other Allowance total"));
    }
}
