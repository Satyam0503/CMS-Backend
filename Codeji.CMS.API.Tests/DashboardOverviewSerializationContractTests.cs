using System.Text.Json;
using Codeji.CMS.DTO.Dashboard;

namespace Codeji.CMS.API.Tests;

public sealed class DashboardOverviewSerializationContractTests
{
    [Fact]
    public void ResultEnvelope_UsesCamelCaseAndSerializesV4OverviewAndV1AttendanceBreakdown()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "dashboard-overview.v4.synthetic.json");
        using var json = JsonDocument.Parse(File.ReadAllText(fixturePath));
        var root = json.RootElement;
        var methodResult = root.GetProperty("methodResult");
        var attendance = methodResult.GetProperty("attendanceBreakdown");

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("dashboard-overview.v4", methodResult.GetProperty("definitionVersion").GetString());
        Assert.Equal("2026-11-17", methodResult.GetProperty("asOfDate").GetString());
        Assert.Equal("Asia/Kolkata", methodResult.GetProperty("timeZone").GetString());
        var availability = methodResult.GetProperty("sourceAvailability");
        Assert.True(availability.GetProperty("employees").GetBoolean());
        Assert.True(availability.GetProperty("attendance").GetBoolean());
        Assert.True(availability.GetProperty("leaveManagement").GetBoolean());
        Assert.False(availability.GetProperty("jobs").GetBoolean());
        Assert.True(availability.GetProperty("offboarding").GetBoolean());
        Assert.Equal("attendance-breakdown.v1", attendance.GetProperty("definitionVersion").GetString());

        var buckets = new[] { "presentOnly", "leaveOnly", "presentAndOnLeave", "explicitlyAbsent", "otherStatus", "unreported" }
            .Sum(name => attendance.GetProperty(name).GetInt32());
        Assert.Equal(attendance.GetProperty("expectedEmployees").GetInt32(), buckets);
        Assert.Equal("2026-11", methodResult.GetProperty("headcountTrend")[0].GetProperty("month").GetString());
        var movement = methodResult.GetProperty("workforceMovementTrend")[0];
        Assert.Equal(2, movement.GetProperty("hires").GetInt32());
        Assert.Equal(1, movement.GetProperty("exits").GetInt32());
        Assert.Equal(1, movement.GetProperty("netChange").GetInt32());
        var leaveCohort = methodResult.GetProperty("leaveRequestCohortTrend")[0];
        Assert.Equal(4, leaveCohort.GetProperty("totalRequests").GetInt32());
        Assert.Equal(1, leaveCohort.GetProperty("approved").GetInt32());
        Assert.Equal(4, leaveCohort.GetProperty("pending").GetInt32()
            + leaveCohort.GetProperty("approved").GetInt32()
            + leaveCohort.GetProperty("rejected").GetInt32()
            + leaveCohort.GetProperty("withdrawn").GetInt32()
            + leaveCohort.GetProperty("otherStatus").GetInt32());
        Assert.Equal("joined", methodResult.GetProperty("recentActivities")[0].GetProperty("type").GetString());
    }
}
