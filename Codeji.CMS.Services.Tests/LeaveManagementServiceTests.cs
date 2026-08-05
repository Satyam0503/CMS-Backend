using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.Services.Tests;

public class LeaveManagementServiceTests
{
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
