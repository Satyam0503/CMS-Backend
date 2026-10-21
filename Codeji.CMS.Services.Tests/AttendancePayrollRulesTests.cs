using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;

public class AttendancePayrollRulesTests
{
    [Theory]
    [InlineData(0,0,2,0)] [InlineData(1,1,2,0)] [InlineData(2,1,2,1)] [InlineData(0,4,2,2)]
    public void Combined_limit_is_applied_across_both_statuses(int lhd,int ed,int limit,int expected) =>
        Assert.Equal(expected, AttendancePayrollRules.ExceededOccurrences(lhd,ed,limit));

    [Fact]
    public void GraceExemption_ResolveGraceExemptDates_PicksFirstNChronologicalDates()
    {
        var dates = new List<DateTime>
        {
            new(2026, 3, 15),
            new(2026, 3, 9),
            new(2026, 3, 11),
            new(2026, 3, 20)
        };

        var graceDates = AttendancePayrollRules.ResolveGraceExemptDates(dates, allowedGraceLimit: 2);

        Assert.Equal(2, graceDates.Count);
        Assert.Contains(new DateTime(2026, 3, 9), graceDates);
        Assert.Contains(new DateTime(2026, 3, 11), graceDates);
        Assert.DoesNotContain(new DateTime(2026, 3, 15), graceDates);
        Assert.DoesNotContain(new DateTime(2026, 3, 20), graceDates);
    }

    [Fact]
    public void GraceExemption_ResolveEffectiveUnpaidDayFraction_ExcusesLhdWithinGrace()
    {
        // Within grace: LHD and LHD+ED should have 0 unpaid fraction (100% paid)
        Assert.Equal(0m, AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction("LHD", isGraceExempt: true, configuredUnpaidFraction: 0.5m));
        Assert.Equal(0m, AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction("LHD+ED", isGraceExempt: true, configuredUnpaidFraction: 0.5m));

        // Beyond grace (or no grace): normal configured unpaid fraction applies
        Assert.Equal(0.5m, AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction("LHD", isGraceExempt: false, configuredUnpaidFraction: 0.5m));
        Assert.Equal(0.5m, AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction("LHD+ED", isGraceExempt: false, configuredUnpaidFraction: 0.5m));

        // Other statuses like HD or Absent are not excused by punctuality grace
        Assert.Equal(0.5m, AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction("HD", isGraceExempt: true, configuredUnpaidFraction: 0.5m));
        Assert.Equal(1.0m, AttendancePayrollRules.ResolveEffectiveUnpaidDayFraction("A", isGraceExempt: true, configuredUnpaidFraction: 1.0m));
    }

    [Fact]
    public void GraceExemption_ResolveEffectivePaidDayFraction_TreatsLhdWithinGraceAsFullyPaid()
    {
        Assert.Equal(1m, AttendancePayrollRules.ResolveEffectivePaidDayFraction("LHD", isGraceExempt: true, configuredPaidFraction: 0.5m));
        Assert.Equal(1m, AttendancePayrollRules.ResolveEffectivePaidDayFraction("LHD+ED", isGraceExempt: true, configuredPaidFraction: 0.5m));

        // When not exempt
        Assert.Equal(0.5m, AttendancePayrollRules.ResolveEffectivePaidDayFraction("LHD", isGraceExempt: false, configuredPaidFraction: 0.5m));
        Assert.Equal(0.5m, AttendancePayrollRules.ResolveEffectivePaidDayFraction("HD", isGraceExempt: true, configuredPaidFraction: 0.5m));
    }

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

    [Fact]
    public void PunctualityWeight_LhdEdIs2_LhdIs1_EdIs1()
    {
        Assert.Equal(2, AttendancePayrollRules.GetPunctualityWeight("LHD+ED"));
        Assert.Equal(2, AttendancePayrollRules.GetPunctualityWeight("lhd+ed"));
        Assert.Equal(1, AttendancePayrollRules.GetPunctualityWeight("LHD"));
        Assert.Equal(1, AttendancePayrollRules.GetPunctualityWeight("ED"));
        Assert.Equal(0, AttendancePayrollRules.GetPunctualityWeight("P"));
        Assert.Equal(0, AttendancePayrollRules.GetPunctualityWeight("HD"));
        Assert.Equal(0, AttendancePayrollRules.GetPunctualityWeight("A"));
    }

    [Fact]
    public void GraceExemption_WhenLhdEdCountsAs2_Consumes2GraceOccurrences()
    {
        // 09 Mar: LHD (weight 1)
        // 11 Mar: LHD+ED (weight 2)
        // Allowed limit = 2
        var infractions = new List<(DateTime Date, string? Status)>
        {
            (new DateTime(2026, 3, 9), "LHD"),
            (new DateTime(2026, 3, 11), "LHD+ED")
        };

        var exemptDates = AttendancePayrollRules.ResolveGraceExemptDates(infractions, allowedGraceLimit: 2);

        // 09 Mar takes 1 occurrence (1 <= 2) -> EXEMPT
        // 11 Mar takes 2 occurrences (1 + 2 = 3 > 2) -> EXCEEDED, NOT EXEMPT
        Assert.Single(exemptDates);
        Assert.Contains(new DateTime(2026, 3, 9), exemptDates);
        Assert.DoesNotContain(new DateTime(2026, 3, 11), exemptDates);
    }

    [Fact]
    public void GraceExemption_WhenLhdEdCountsAs2_BothExemptIfLimitIsAtLeast3()
    {
        var infractions = new List<(DateTime Date, string? Status)>
        {
            (new DateTime(2026, 3, 9), "LHD"),
            (new DateTime(2026, 3, 11), "LHD+ED")
        };

        var exemptDates = AttendancePayrollRules.ResolveGraceExemptDates(infractions, allowedGraceLimit: 3);

        // 09 Mar (weight 1) + 11 Mar (weight 2) = 3 <= 3 -> Both EXEMPT
        Assert.Equal(2, exemptDates.Count);
        Assert.Contains(new DateTime(2026, 3, 9), exemptDates);
        Assert.Contains(new DateTime(2026, 3, 11), exemptDates);
    }
}
