using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.Services.Tests;

public class LeaveManagementServiceTests
{
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
}
