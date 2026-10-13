using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;

public class AttendancePayrollRulesTests
{
    [Theory]
    [InlineData(0,0,2,0)] [InlineData(1,1,2,0)] [InlineData(2,1,2,1)] [InlineData(0,4,2,2)]
    public void Combined_limit_is_applied_across_both_statuses(int lhd,int ed,int limit,int expected) =>
        Assert.Equal(expected, AttendancePayrollRules.ExceededOccurrences(lhd,ed,limit));

    [Fact]
    public void Joining_and_exit_dates_clip_the_payroll_period()
    {
        var employee = new EmpUser { CompanyId="c",UserId="u",EmployeeId="e",FirstName="A",LastName="B",Email="a@b.com",DateOfJoining="2026-07-10",ExitDate="2026-07-20" };
        Assert.True(AttendancePayrollRules.TryGetEligiblePeriod(employee,new DateTime(2026,7,1),new DateTime(2026,7,31),out var from,out var to));
        Assert.Equal(new DateTime(2026,7,10),from); Assert.Equal(new DateTime(2026,7,20),to);
    }

    [Fact]
    public void Employee_outside_month_is_not_eligible()
    {
        var employee = new EmpUser { CompanyId="c",UserId="u",EmployeeId="e",FirstName="A",LastName="B",Email="a@b.com",DateOfJoining="2026-08-01" };
        Assert.False(AttendancePayrollRules.TryGetEligiblePeriod(employee,new DateTime(2026,7,1),new DateTime(2026,7,31),out _,out _));
    }

    [Fact]
    public void Completed_exit_is_eligible_for_its_final_month_but_not_future_months()
    {
        var employee = new EmpUser
        {
            CompanyId="c", UserId="u", EmployeeId="e", FirstName="A", LastName="B", Email="a@b.com",
            DateOfJoining="2026-01-01", EmploymentStatus="Resigned", Status=false,
            LastWorkingDate=new DateTime(2026,8,25)
        };

        Assert.True(EmploymentPeriod.IsEligibleForMonth(employee, new DateTime(2026,8,1)));
        Assert.False(EmploymentPeriod.IsEligibleForMonth(employee, new DateTime(2026,9,1)));
        Assert.True(AttendancePayrollRules.TryGetEligiblePeriod(employee, new DateTime(2026,8,1), new DateTime(2026,8,31), out var from, out var to));
        Assert.Equal(new DateTime(2026,8,1), from);
        Assert.Equal(new DateTime(2026,8,25), to);
    }

    [Fact]
    public void Completed_exit_allows_historical_work_only_through_last_working_date()
    {
        var employee = new EmpUser
        {
            CompanyId="c", UserId="u", EmployeeId="e", FirstName="A", LastName="B", Email="a@b.com",
            DateOfJoining="2026-01-01", EmploymentStatus="Terminated", Status=false,
            LastWorkingDate=new DateTime(2026,8,25)
        };

        Assert.True(EmploymentPeriod.WasEmployedOn(employee, new DateTime(2026,8,25)));
        Assert.False(EmploymentPeriod.WasEmployedOn(employee, new DateTime(2026,8,26)));
    }
}
