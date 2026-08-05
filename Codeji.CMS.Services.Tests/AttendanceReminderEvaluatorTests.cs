using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;

namespace Codeji.CMS.Services.Tests;

public class AttendanceReminderEvaluatorTests
{
    [Theory]
    [InlineData(8, 59, false)]
    [InlineData(9, 0, true)]
    [InlineData(10, 15, true)]
    public void HasOfficeStarted_UsesTheEmployeeScheduleStartTime(int hour, int minute, bool expected)
    {
        var reference = new DateTime(2026, 8, 4, hour, minute, 0);

        Assert.Equal(expected, AttendanceReminderEvaluator.HasOfficeStarted(reference, new TimeSpan(9, 0, 0)));
    }

    [Theory]
    [InlineData(15, 59, false)]
    [InlineData(16, 0, true)]
    [InlineData(16, 1, true)]
    public void IsReviewReminderDue_TriggersAtFourPmIndiaTime(int hour, int minute, bool expected)
    {
        var reference = new DateTime(2026, 8, 5, hour, minute, 0);

        Assert.Equal(expected, AttendanceReminderEvaluator.IsReviewReminderDue(reference));
    }

    [Fact]
    public void FindMissingAttendance_ReturnsEmployeesWithoutAttendanceForDate()
    {
        var today = new DateTime(2026, 7, 30);
        var employees = new List<EmpUser>
        {
            new() { UserId = "user-1", EmployeeId = "EMP-001", FirstName = "Alice", LastName = "Smith", CompanyId = "company-1", Status = true, Email = "alice@example.com" },
            new() { UserId = "user-2", EmployeeId = "EMP-002", FirstName = "Bob", LastName = "Jones", CompanyId = "company-1", Status = true, Email = "bob@example.com" },
            new() { UserId = "user-3", EmployeeId = "EMP-003", FirstName = "Carol", LastName = "Taylor", CompanyId = "company-1", Status = true, Email = "carol@example.com" }
        };

        var attendance = new List<AttendanceModel>
        {
            new() { UserId = "user-1", CompanyId = "company-1", Date = today }
        };

        var missing = AttendanceReminderEvaluator.FindMissingAttendance(employees, attendance, today);

        Assert.Collection(missing,
            item =>
            {
                Assert.Equal("user-2", item.UserId);
                Assert.Equal("EMP-002", item.EmployeeId);
            },
            item =>
            {
                Assert.Equal("user-3", item.UserId);
                Assert.Equal("EMP-003", item.EmployeeId);
            });
    }
}
