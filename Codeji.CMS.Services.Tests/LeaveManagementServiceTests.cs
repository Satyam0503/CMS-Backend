using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.middlewares;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Security.Claims;
using Moq;

namespace Codeji.CMS.Services.Tests;

public class LeaveManagementServiceTests
{
    [Fact]
    public async Task CreateLeaveRequest_RejectsIdentityWithoutActiveEmployeeRecordAndDoesNotPersist()
    {
        var employeeRepository = new Mock<IMongoDbRepository<EmpUser>>();
        employeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync((EmpUser?)null);
        var policyRepository = new Mock<IMongoDbRepository<LeavePolicy>>();
        policyRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), false, true))
            .ReturnsAsync(new LeavePolicy { Id = "policy-1", Name = "Annual Leave", Code = "AL", Status = true });
        var leaveRepository = new Mock<IMongoDbRepository<LeaveRequest>>();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("user_id", "non-employee-user"),
            new Claim("company_id", "company-1"),
        ], "test"));

        var service = new LeaveManagementService(
            employeeRepository.Object,
            leaveRepository.Object,
            new HttpContextAccessor { HttpContext = context },
            Mock.Of<IMapper>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IMongoDbRepository<Notifications>>(),
            Mock.Of<IMongoDbRepository<UserNotifications>>(),
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            Mock.Of<IEmployeeService>(),
            Mock.Of<IMiddlewareService>(),
            policyRepository.Object,
            Mock.Of<IMongoDbRepository<EmployeeLeaveBalance>>(),
            Mock.Of<ICompanyWorkingCalendarService>(),
            Mock.Of<IMongoDbRepository<AttendanceStatusSetting>>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            Mock.Of<IMongoDbRepository<WorkFromHomePolicy>>(),
            Mock.Of<IMongoDbRepository<WorkFromHomeRequest>>(),
            Mock.Of<IMongoClient>());

        var result = await service.CreateLeaveRequest(new LeaveRequestDto
        {
            UserId = "non-employee-user",
            LeavePolicyId = "policy-1",
            StartDate = DateTime.UtcNow.Date.AddDays(5),
            EndDate = DateTime.UtcNow.Date.AddDays(5),
            Reason = "Personal leave",
        });

        Assert.False(result.Success);
        Assert.Equal("Employee does not belong to the authenticated company.", result.Message);
        leaveRepository.Verify(x => x.AddOne(It.IsAny<LeaveRequest>()), Times.Never);
        leaveRepository.Verify(x => x.GetCollection(), Times.Never);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task PaidLeaveApproval_UsesStandaloneFallbackAndRechecksWfhOverlap(
        bool overlapAppearsBeforeFallbackRecheck, bool expectedSuccess)
    {
        var now = DateTime.UtcNow.Date;
        var request = new LeaveRequest
        {
            CompanyId = "company-1",
            LeaveRequestId = "request-1",
            EmployeeId = "employee-1",
            LeavePolicyId = "policy-1",
            StartDate = now.AddDays(5),
            EndDate = now.AddDays(5),
            TotalDays = 1,
            Reason = "Personal leave",
            Status = EnumsHelper.LeaveRequestStatus.Pending,
            Version = 1,
        };
        var policy = new LeavePolicy
        {
            Id = "policy-1",
            Name = "Annual Leave",
            Code = "AL",
            PolicyType = EnumsHelper.LeavePolicyType.Leave,
            FullDayAttendanceStatusCode = "PL",
            Status = true,
            Paid = true,
        };

        var leaveCollection = new Mock<IMongoCollection<LeaveRequest>>();
        var updateResult = new Mock<UpdateResult>();
        updateResult.SetupGet(x => x.IsAcknowledged).Returns(true);
        updateResult.SetupGet(x => x.ModifiedCount).Returns(1);
        leaveCollection
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<LeaveRequest>>(),
                It.IsAny<UpdateDefinition<LeaveRequest>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult.Object);
        var leaveRepository = new Mock<IMongoDbRepository<LeaveRequest>>();
        leaveRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(request);
        leaveRepository.Setup(x => x.GetCollection()).Returns(leaveCollection.Object);
        var balanceCollection = new Mock<IMongoCollection<EmployeeLeaveBalance>>();
        balanceCollection
            .Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<EmployeeLeaveBalance>>(),
                It.IsAny<UpdateDefinition<EmployeeLeaveBalance>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult.Object);
        var balanceRepository = new Mock<IMongoDbRepository<EmployeeLeaveBalance>>();
        balanceRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLeaveBalance, bool>>>(), false, true))
            .ReturnsAsync(new EmployeeLeaveBalance
            {
                Id = "balance-1", CompanyId = "company-1", UserId = "employee-1",
                LeavePolicyId = "policy-1", TotalAllocated = 10, Taken = 2, Remaining = 8,
            });
        balanceRepository.Setup(x => x.GetCollection()).Returns(balanceCollection.Object);
        var policyRepository = new Mock<IMongoDbRepository<LeavePolicy>>();
        policyRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), false, true))
            .ReturnsAsync(policy);
        var wfhRepository = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        var wfhChecks = 0;
        var conflictingWfhRequest = new WorkFromHomeRequest
        {
            CompanyId = "company-1",
            UserId = "employee-1",
            FromDate = request.StartDate,
            ToDate = request.EndDate,
            Status = "Approved",
            DurationType = "FullDay",
        };
        wfhRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), false, true))
            .ReturnsAsync(() =>
            {
                wfhChecks++;
                return overlapAppearsBeforeFallbackRecheck && wfhChecks > 1
                    ? [conflictingWfhRequest]
                    : Array.Empty<WorkFromHomeRequest>();
            });
        var session = new Mock<IClientSessionHandle>();
        session.Setup(x => x.StartTransaction(It.IsAny<TransactionOptions>()))
            .Throws(new NotSupportedException("Transactions are unavailable on standalone MongoDB."));
        var mongoClient = new Mock<IMongoClient>();
        mongoClient
            .Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);
        var reconciliation = new Mock<ILeaveAttendanceReconciliationService>();
        reconciliation
            .Setup(x => x.ReconcileAcceptedLeaveAsync("company-1", "request-1", "reviewer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result { Success = true });
        var attendanceStatuses = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        attendanceStatuses
            .Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), false))
            .ReturnsAsync(true);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("user_id", "reviewer-1"),
            new Claim("company_id", "company-1"),
        ], "test"));

        var service = new LeaveManagementService(
            Mock.Of<IMongoDbRepository<EmpUser>>(), leaveRepository.Object,
            new HttpContextAccessor { HttpContext = context }, Mock.Of<IMapper>(),
            Mock.Of<INotificationService>(), Mock.Of<IMongoDbRepository<Notifications>>(),
            Mock.Of<IMongoDbRepository<UserNotifications>>(), Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(), Mock.Of<IEmployeeService>(),
            Mock.Of<IMiddlewareService>(), policyRepository.Object,
            balanceRepository.Object, Mock.Of<ICompanyWorkingCalendarService>(),
            attendanceStatuses.Object, reconciliation.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomePolicy>>(), wfhRepository.Object, mongoClient.Object);

        var result = await service.UpdateLeaveRequestStatus("request-1", new LeaveRequestUpdateDto
        {
            Status = EnumsHelper.LeaveRequestStatus.Accepted,
            Comment = "Approved",
            ExpectedVersion = 1,
        });

        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            Assert.Equal(EnumsHelper.LeaveRequestStatus.Accepted, request.Status);
            balanceCollection.Verify(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<EmployeeLeaveBalance>>(),
                It.IsAny<UpdateDefinition<EmployeeLeaveBalance>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            leaveCollection.Verify(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<LeaveRequest>>(),
                It.IsAny<UpdateDefinition<LeaveRequest>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            reconciliation.Verify(x => x.ReconcileAcceptedLeaveAsync("company-1", "request-1", "reviewer-1", It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
            Assert.Equal("LEAVE_WFH_OVERLAP", result.ErrorCode);
            Assert.Equal(EnumsHelper.LeaveRequestStatus.Pending, request.Status);
            balanceCollection.Verify(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<EmployeeLeaveBalance>>(),
                It.IsAny<UpdateDefinition<EmployeeLeaveBalance>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()), Times.Never);
            leaveCollection.Verify(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<LeaveRequest>>(),
                It.IsAny<UpdateDefinition<LeaveRequest>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()), Times.Never);
            reconciliation.Verify(x => x.ReconcileAcceptedLeaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Theory]
    [InlineData("2026-08-11T08:59:59", false)]
    [InlineData("2026-08-11T09:00:00", true)]
    [InlineData("2026-08-11T09:01:00", true)]
    public void SameDayWfhStartCutoff_UsesTheEffectiveOfficeStartBoundary(string currentTime, bool expected)
    {
        var now = DateTime.Parse(currentTime);

        var blocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(now.Date, now, new TimeSpan(9, 0, 0));

        Assert.Equal(expected, blocked);
    }

    [Fact]
    public void SameDayWfhStartCutoff_DoesNotBlockAFutureRequest()
    {
        var now = new DateTime(2026, 8, 11, 9, 1, 0);

        var blocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(now.AddDays(1), now, new TimeSpan(9, 0, 0));

        Assert.False(blocked);
    }

    [Fact]
    public void IsValidBalance_AcceptsEmployeeSpecificAllocationInvariant()
    {
        var balance = new EmployeeLeaveBalance { UserId = "employee-a", LeavePolicyId = "casual", TotalAllocated = 12, Taken = 4, Remaining = 8 };
        Assert.True(LeaveManagementService.IsValidBalance(balance));
    }

    [Theory]
    [InlineData(12, 4, 7)]
    [InlineData(12, -1, 13)]
    [InlineData(12, 13, -1)]
    public void IsValidBalance_RejectsBrokenOrNegativeInvariant(decimal total, decimal taken, decimal remaining)
    {
        var balance = new EmployeeLeaveBalance { UserId = "employee-a", LeavePolicyId = "casual", TotalAllocated = total, Taken = taken, Remaining = remaining };
        Assert.False(LeaveManagementService.IsValidBalance(balance));
    }

    [Theory]
    [InlineData(8, 2, true, 6)]
    [InlineData(4, 2, true, 2)]
    [InlineData(1, 2, false, -1)]
    public void TryCalculateAllocation_PreservesTakenAndRejectsNegativeRemaining(
        decimal totalAllocated, decimal taken, bool expectedSuccess, decimal expectedRemaining)
    {
        var success = LeaveManagementService.TryCalculateAllocation(totalAllocated, taken, out var remaining);

        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expectedRemaining, remaining);
    }

    [Fact]
    public void TryNormalizeLeaveRequestDates_UsesTheCalendarDateFromDateOnlyPayload()
    {
        var valid = LeaveManagementService.TryNormalizeLeaveRequestDates(
            DateTime.Parse("2026-08-05"), DateTime.Parse("2026-08-05"), false, true,
            out var start, out var end, out var error);

        Assert.True(valid);
        Assert.Null(error);
        Assert.Equal(new DateOnly(2026, 8, 5), start);
        Assert.Equal(new DateOnly(2026, 8, 5), end);
    }

    [Theory]
    [InlineData("2026-07-30", "2026-07-30", "2026-07-31", true)]
    [InlineData("2026-07-31", "2026-08-02", "2026-07-31", false)]
    [InlineData("2026-08-01", "2026-08-02", "2026-07-31", false)]
    public void IsPastDatedLeaveRequest_ReturnsExpectedResult(string startDate, string endDate, string currentDate, bool expected)
    {
        var request = new LeaveRequest
        {
            LeavePolicyId = "policy-1",
            Reason = "Test",
            StartDate = DateTime.Parse(startDate),
            EndDate = DateTime.Parse(endDate)
        };

        var result = LeaveManagementService.IsPastDatedLeaveRequest(request, DateTime.Parse(currentDate));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPastDatedLeaveRequest_UsesEndDateForAnInProgressLeave()
    {
        var request = new LeaveRequest
        {
            LeavePolicyId = "policy-1",
            Reason = "Test",
            StartDate = DateTime.Parse("2026-07-30"),
            EndDate = DateTime.Parse("2026-08-02")
        };

        Assert.False(LeaveManagementService.IsPastDatedLeaveRequest(request, DateTime.Parse("2026-07-31")));
    }

    [Theory]
    [InlineData("2026-07-30", "2026-07-31", "2026-07-30", true)]
    [InlineData("2026-07-30", "2026-07-31", "2026-07-31", true)]
    [InlineData("2026-07-30", "2026-07-31", "2026-07-29", false)]
    public void IsLeaveRequestStarted_BlocksUpdatesFromTheStartDate(
        string startDate, string endDate, string currentDate, bool expected)
    {
        var request = new LeaveRequest
        {
            LeavePolicyId = "policy-1",
            Reason = "Test",
            StartDate = DateTime.Parse(startDate),
            EndDate = DateTime.Parse(endDate)
        };

        Assert.Equal(expected, LeaveManagementService.IsLeaveRequestStarted(request, DateTime.Parse(currentDate)));
    }

    [Theory]
    [InlineData(EnumsHelper.LeaveRequestStatus.Accepted, "", null)]
    [InlineData(EnumsHelper.LeaveRequestStatus.Rejected, "", "Rejection requires a reason.")]
    [InlineData(EnumsHelper.LeaveRequestStatus.Rejected, "  ", "Rejection requires a reason.")]
    [InlineData(EnumsHelper.LeaveRequestStatus.Rejected, "Late submission", null)]
    public void ValidateLeaveDecisionComment_ReturnsExpectedResult(EnumsHelper.LeaveRequestStatus status, string comment, string? expected)
    {
        var model = new LeaveRequestUpdateDto { Status = status, Comment = comment };

        var result = LeaveManagementService.ValidateLeaveDecisionComment(model);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task CreateLeaveRequest_WhenPendingRequestsExceedRemainingBalance_ReturnsInsufficientLeaveBalance()
    {
        var companyId = "company-1";
        var userId = "user-100";
        var policyId = "policy-annual";

        var employeeRepo = new Mock<IMongoDbRepository<EmpUser>>();
        employeeRepo
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new EmpUser { CompanyId = companyId, UserId = userId, EmployeeId = "EMP-100", FirstName = "Test", LastName = "User", Email = "test@company.com", Status = true });

        var policyRepo = new Mock<IMongoDbRepository<LeavePolicy>>();
        policyRepo
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), false, true))
            .ReturnsAsync(new LeavePolicy { Id = policyId, CompanyId = companyId, Name = "Annual Leave", Code = "AL", Status = true, Paid = true, MinNoticeDays = 0 });

        var balanceRepo = new Mock<IMongoDbRepository<EmployeeLeaveBalance>>();
        balanceRepo
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLeaveBalance, bool>>>(), false, true))
            .ReturnsAsync(new EmployeeLeaveBalance
            {
                CompanyId = companyId,
                UserId = userId,
                LeavePolicyId = policyId,
                TotalAllocated = 5m,
                Taken = 3m,
                Remaining = 2m
            });

        // 2 days already held in pending requests
        var existingPendingLeaves = new List<LeaveRequest>
        {
            new() {
                CompanyId = companyId,
                EmployeeId = userId,
                LeavePolicyId = policyId,
                TotalDays = 2m,
                Status = EnumsHelper.LeaveRequestStatus.Pending,
                StartDate = DateTime.UtcNow.Date.AddDays(5),
                EndDate = DateTime.UtcNow.Date.AddDays(6),
                Reason = "Pending request"
            }
        };

        var leaveRepo = new Mock<IMongoDbRepository<LeaveRequest>>();
        leaveRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(existingPendingLeaves);

        var calendar = new Mock<ICompanyWorkingCalendarService>();
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(10));
        calendar
            .Setup(x => x.GetLeaveDatesAsync(companyId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<DateOnly> { targetDate });

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("user_id", userId),
            new Claim("company_id", companyId),
        ], "test"));

        var service = new LeaveManagementService(
            employeeRepo.Object,
            leaveRepo.Object,
            new HttpContextAccessor { HttpContext = context },
            Mock.Of<IMapper>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IMongoDbRepository<Notifications>>(),
            Mock.Of<IMongoDbRepository<UserNotifications>>(),
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            Mock.Of<IEmployeeService>(),
            Mock.Of<IMiddlewareService>(),
            policyRepo.Object,
            balanceRepo.Object,
            calendar.Object,
            Mock.Of<IMongoDbRepository<AttendanceStatusSetting>>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            Mock.Of<IMongoDbRepository<WorkFromHomePolicy>>(),
            Mock.Of<IMongoDbRepository<WorkFromHomeRequest>>(),
            Mock.Of<IMongoClient>());

        // Act: Request 1 day when 2 remaining - 2 pending = 0 available
        var result = await service.CreateLeaveRequest(new LeaveRequestDto
        {
            UserId = userId,
            LeavePolicyId = policyId,
            StartDate = DateTime.UtcNow.Date.AddDays(10),
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            Reason = "Vacation"
        });

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CustomStatusCode.InsufficientLeaveBalance, result.StatusCode);
        Assert.Contains("pending approval", result.Message);
    }

    [Fact]
    public async Task CreateLeaveRequest_WhenAttendanceIsWfhWfoHybrid_RejectsWithHybridConflict()
    {
        var companyId = "company-hybrid-test";
        var userId = "user-hybrid-test";
        var policyId = "policy-sick-leave";
        var targetDate = DateTime.UtcNow.Date.AddDays(5);

        var employeeRepo = new Mock<IMongoDbRepository<EmpUser>>();
        employeeRepo
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new EmpUser
            {
                UserId = userId,
                CompanyId = companyId,
                FirstName = "Aarav",
                LastName = "Sharma",
                Email = "aarav@test.invalid",
                Status = true,
                DateOfJoining = "2025-01-01"
            });

        var policyRepo = new Mock<IMongoDbRepository<LeavePolicy>>();
        policyRepo
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), false, true))
            .ReturnsAsync(new LeavePolicy
            {
                Id = policyId,
                CompanyId = companyId,
                Name = "Sick Leave",
                Code = "SL",
                Status = true,
                PolicyType = EnumsHelper.LeavePolicyType.Leave,
                HalfDayAllowed = true
            });

        var balanceRepo = new Mock<IMongoDbRepository<EmployeeLeaveBalance>>();
        balanceRepo
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLeaveBalance, bool>>>(), false, true))
            .ReturnsAsync(new EmployeeLeaveBalance
            {
                CompanyId = companyId,
                UserId = userId,
                LeavePolicyId = policyId,
                TotalAllocated = 10m,
                Remaining = 10m
            });

        var leaveRepo = new Mock<IMongoDbRepository<LeaveRequest>>();
        leaveRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        var calendar = new Mock<ICompanyWorkingCalendarService>();
        calendar
            .Setup(x => x.GetLeaveDatesAsync(companyId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<DateOnly> { DateOnly.FromDateTime(targetDate) });

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("user_id", userId),
            new Claim("company_id", companyId),
        ], "test"));

        var mockAttendanceCol = new Mock<IMongoCollection<AttendanceModel>>();
        var mockCursor = new Mock<IAsyncCursor<AttendanceModel>>();
        mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(x => x.Current).Returns(new List<AttendanceModel>
        {
            new AttendanceModel
            {
                CompanyId = companyId,
                UserId = userId,
                Date = targetDate,
                Status = "WFH+WFO",
                RemarkCode = "WFH_WFO_COMBINATION"
            }
        });
        mockAttendanceCol.SetReturnsDefault<IAsyncCursor<AttendanceModel>>(mockCursor.Object);
        mockAttendanceCol.SetReturnsDefault<Task<IAsyncCursor<AttendanceModel>>>(Task.FromResult(mockCursor.Object));
        mockAttendanceCol.Setup(c => c.FindSync(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<FindOptions<AttendanceModel, AttendanceModel>>(), It.IsAny<CancellationToken>()))
            .Returns(mockCursor.Object);
        mockAttendanceCol.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<FindOptions<AttendanceModel, AttendanceModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);
        mockAttendanceCol.Setup(c => c.FindAsync(It.IsAny<IClientSessionHandle>(), It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<FindOptions<AttendanceModel, AttendanceModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        var mockDb = new Mock<IMongoDatabase>();
        mockDb.Setup(d => d.GetCollection<AttendanceModel>("Attendance", It.IsAny<MongoCollectionSettings>()))
            .Returns(mockAttendanceCol.Object);

        var mockWfhRequestCol = new Mock<IMongoCollection<WorkFromHomeRequest>>();
        mockWfhRequestCol.Setup(c => c.Database).Returns(mockDb.Object);

        var mockWfhRequestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        mockWfhRequestRepo.Setup(r => r.GetCollection()).Returns(mockWfhRequestCol.Object);
        mockWfhRequestRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<WorkFromHomeRequest>());

        var service = new LeaveManagementService(
            employeeRepo.Object,
            leaveRepo.Object,
            new HttpContextAccessor { HttpContext = context },
            Mock.Of<IMapper>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IMongoDbRepository<Notifications>>(),
            Mock.Of<IMongoDbRepository<UserNotifications>>(),
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            Mock.Of<IEmployeeService>(),
            Mock.Of<IMiddlewareService>(),
            policyRepo.Object,
            balanceRepo.Object,
            calendar.Object,
            Mock.Of<IMongoDbRepository<AttendanceStatusSetting>>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            Mock.Of<IMongoDbRepository<WorkFromHomePolicy>>(),
            mockWfhRequestRepo.Object,
            Mock.Of<IMongoClient>());

        var result = await service.CreateLeaveRequest(new LeaveRequestDto
        {
            UserId = userId,
            LeavePolicyId = policyId,
            StartDate = targetDate,
            EndDate = targetDate,
            IsHalfDay = true,
            HalfDayPeriod = "FIRST_HALF",
            Reason = "Sick"
        });

        Assert.False(result.Success);
        Assert.Contains("LEAVE_HYBRID_CONFLICT", result.Message);
    }
}
