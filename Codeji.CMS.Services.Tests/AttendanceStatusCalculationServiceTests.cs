using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.Attendance;

public class AttendanceStatusCalculationServiceTests
{
    private readonly AttendanceStatusCalculationService _service = new();
    private static readonly CompanyOfficeSchedule Schedule = new()
    {
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(18, 0, 0),
        GraceMinutes = 60,
        CheckInAllowedUntil = new TimeSpan(9, 0, 0),
        CheckOutAllowedFrom = new TimeSpan(18, 0, 0),
    };

    [Theory]
    [InlineData(9, 0, 18, 0, "P")]
    [InlineData(9, 15, 18, 0, "LHD")]
    [InlineData(9, 0, 17, 45, "ED")]
    [InlineData(9, 15, 17, 45, "LHD+ED")]
    [InlineData(13, 1, 18, 0, "HD")]
    [InlineData(9, 0, 13, 59, "HD")]
    public void Classifies_timing_using_the_effective_schedule(int inHour, int inMinute, int outHour, int outMinute, string expected)
    {
        var actual = _service.CalculateOfficeStatus(Schedule, new TimeOnly(inHour, inMinute), new TimeOnly(outHour, outMinute), 8m);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(10, 0, "P")]
    [InlineData(10, 1, "LHD")]
    [InlineData(10, 15, "LHD")]
    [InlineData(10, 16, "HD")]
    public void Allows_lhd_only_inside_the_configured_grace_window(int hour, int minute, string expected)
    {
        var schedule = new CompanyOfficeSchedule
        {
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(18, 0, 0),
            CheckInAllowedUntil = new TimeSpan(10, 0, 0),
            GraceMinutes = 15,
            CheckOutAllowedFrom = new TimeSpan(17, 30, 0),
        };

        var actual = _service.CalculateOfficeStatus(schedule, new TimeOnly(hour, minute), new TimeOnly(18, 0), 8m);
        Assert.Equal(expected, actual);
    }
}
