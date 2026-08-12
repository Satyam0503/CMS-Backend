using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.Attendance;

public class AttendanceStatusCalculationServiceTests
{
    private readonly AttendanceStatusCalculationService _service = new();
    private static readonly CompanyOfficeSchedule Schedule = new()
    {
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(18, 0, 0),
        GraceMinutes = 10,
        CheckInAllowedUntil = new TimeSpan(13, 0, 0),
        CheckOutAllowedFrom = new TimeSpan(14, 0, 0),
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
}
