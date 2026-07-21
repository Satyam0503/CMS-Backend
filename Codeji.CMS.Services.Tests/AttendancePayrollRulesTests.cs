using Codeji.CMS.Repository.Entities.Employees;

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
}
