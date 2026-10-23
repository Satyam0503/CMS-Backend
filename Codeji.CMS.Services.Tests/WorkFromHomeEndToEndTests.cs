using Codeji.CMS.DTO.LeaveManagement.LeavePolicy;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.LeaveManagement;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeEndToEndTests
{
    [Fact]
    public void ResolveEffectiveCutoffTime_WhenCustomCutoffNull_ReturnsNull()
    {
        var officeStart = new TimeSpan(9, 30, 0);
        var cutoff = WorkFromHomeService.ResolveEffectiveCutoffTime(officeStart, null, 45);

        Assert.Null(cutoff);
    }

    [Fact]
    public void ResolveEffectiveCutoffTime_WithCustomCutoff_OverridesOfficeStart()
    {
        var officeStart = new TimeSpan(9, 30, 0);
        var custom = new TimeSpan(11, 0, 0);
        var cutoff = WorkFromHomeService.ResolveEffectiveCutoffTime(officeStart, custom, 15);

        // 11:00 + 15 min grace = 11:15
        Assert.Equal(new TimeSpan(11, 15, 0), cutoff);
    }

    [Fact]
    public void ResolveEffectiveCutoffTime_WithNegativeGraceMinutes_TreatedAsZero()
    {
        var officeStart = new TimeSpan(9, 30, 0);
        var custom = new TimeSpan(9, 30, 0);
        var cutoff = WorkFromHomeService.ResolveEffectiveCutoffTime(officeStart, custom, -30);

        Assert.Equal(new TimeSpan(9, 30, 0), cutoff);
    }

    [Theory]
    [InlineData("FullDay", 10, 25, 9, 30, 60, false)] // 10:25 is BEFORE 10:30 (9:30 + 60 grace) => NOT blocked
    [InlineData("FullDay", 10, 31, 9, 30, 60, true)]  // 10:31 is AFTER 10:30 => blocked
    [InlineData("SecondHalf", 10, 25, 9, 30, 0, false)] // SecondHalf boundary is 13:30 => 10:25 is NOT blocked
    [InlineData("SecondHalf", 13, 29, 9, 30, 0, false)] // 13:29 is BEFORE 13:30 => NOT blocked
    [InlineData("SecondHalf", 13, 31, 9, 30, 0, true)]  // 13:31 is AFTER 13:30 => blocked
    [InlineData("FirstHalf", 10, 25, 9, 30, 0, true)]   // FirstHalf cutoff is 9:30 => 10:25 is blocked
    public void CutoffCalculation_RespectsDurationAndGrace(
        string duration, int nowHour, int nowMinute, int startHour, int startMinute, int graceMinutes, bool expectedCutoffPassed)
    {
        var today = new DateTime(2026, 10, 23);
        var companyNow = new DateTime(2026, 10, 23, nowHour, nowMinute, 0);
        var officeStart = new TimeSpan(startHour, startMinute, 0);
        var customCutoff = new TimeSpan(startHour, startMinute, 0);

        var cutoffPassed = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            today, companyNow, duration, officeStart, customCutoff, graceMinutes, 480, 8m);

        Assert.Equal(expectedCutoffPassed, cutoffPassed);
    }

    [Fact]
    public void WorkFromHomePolicySettings_DefaultsAllowLateApplication()
    {
        var settings = new WorkFromHomePolicySettings();
        Assert.True(settings.AllowLateApplicationWithApproval);
        Assert.Equal(0, settings.SameDayGraceMinutes);
        Assert.Null(settings.SameDayCutoffTime);
    }

    [Fact]
    public void WorkFromHomeRequest_DefaultNotLateSubmission()
    {
        var request = new WorkFromHomeRequest();
        Assert.False(request.IsLateSubmission);
    }
}

