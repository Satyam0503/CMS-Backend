using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeLifecycleEligibilityTests
{
    [Fact]
    public void Unauthorized_absence_remains_current_for_wfh_workflow()
    {
        var employee = new EmpUser
        {
            UserId = "u1", CompanyId = "c1", RoleId = "r1", FirstName = "A", LastName = "B",
            Email = "a@example.com", Password = "x", Gender = "", EmployeeId = "E1", JobRole = "",
            Department = "d", ReportingManager = "m", PhoneNumber = "", BloodGroup = "", PersonalEmail = "",
            EmergencyContact = "", DateOfJoining = "2026-01-01", Address = "", ProfileUrl = "", Status = false,
            Lifecycle = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.Active, AvailabilityStatus = EmployeeLifecycleStatuses.UnauthorizedAbsence }
        };

        Assert.True(WorkFromHomeService.IsCurrentEmployeeForWorkflow(employee));
    }

    [Fact]
    public void Completed_exit_is_not_current_for_wfh_workflow()
    {
        var employee = new EmpUser
        {
            UserId = "u1", CompanyId = "c1", RoleId = "r1", FirstName = "A", LastName = "B",
            Email = "a@example.com", Password = "x", Gender = "", EmployeeId = "E1", JobRole = "",
            Department = "d", ReportingManager = "m", PhoneNumber = "", BloodGroup = "", PersonalEmail = "",
            EmergencyContact = "", DateOfJoining = "2026-01-01", Address = "", ProfileUrl = "", Status = true,
            Lifecycle = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.Terminated }
        };

        Assert.False(WorkFromHomeService.IsCurrentEmployeeForWorkflow(employee));
    }

    [Theory]
    [InlineData(EmployeeLifecycleStatuses.AnnualLeave)]
    [InlineData(EmployeeLifecycleStatuses.Sabbatical)]
    [InlineData(EmployeeLifecycleStatuses.UnauthorizedAbsence)]
    [InlineData(EmployeeLifecycleStatuses.Suspended)]
    public void Non_working_availability_cannot_create_wfh(string availability)
    {
        var employee = new EmpUser
        {
            UserId = "u1", CompanyId = "c1", FirstName = "A", LastName = "B", Email = "a@example.com",
            DateOfJoining = "2026-01-01", Status = true,
            Lifecycle = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.Active, AvailabilityStatus = availability }
        };

        Assert.False(WorkFromHomeService.IsLifecycleAvailableForWfh(employee, new DateTime(2026, 9, 8)));
    }

    [Fact]
    public void Return_date_makes_wfh_available_on_return_day()
    {
        var employee = new EmpUser
        {
            UserId = "u1", CompanyId = "c1", FirstName = "A", LastName = "B", Email = "a@example.com",
            DateOfJoining = "2026-01-01", Status = true,
            Lifecycle = new EmployeeLifecycleState
            {
                EmploymentStatus = EmployeeLifecycleStatuses.Active,
                AvailabilityStatus = EmployeeLifecycleStatuses.Sabbatical,
                AvailabilityExpectedReturnDate = new DateTime(2026, 9, 10)
            }
        };

        Assert.False(WorkFromHomeService.IsLifecycleAvailableForWfh(employee, new DateTime(2026, 9, 9)));
        Assert.True(WorkFromHomeService.IsLifecycleAvailableForWfh(employee, new DateTime(2026, 9, 10)));
    }
}
