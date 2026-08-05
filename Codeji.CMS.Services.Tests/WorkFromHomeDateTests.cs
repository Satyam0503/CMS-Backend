using Codeji.CMS.Services.Attendance;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeDateTests
{
    [Fact]
    public void BusinessDateUtc_PreservesTheSelectedCalendarDay()
    {
        var date = WorkFromHomeService.BusinessDateUtc(new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Utc), date);
    }
}
