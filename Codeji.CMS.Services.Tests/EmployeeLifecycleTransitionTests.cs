using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;

namespace Codeji.CMS.Services.Tests;

public class EmployeeLifecycleTransitionTests
{
    [Fact]
    public void Sabbatical_requires_return_date_and_keeps_employment_active()
    {
        var state = new EmployeeLifecycleState();
        var changed = EmployeeLifecycleTransitionService.TryChangeAvailability(
            state, EmployeeLifecycleStatuses.Sabbatical, "hr", 0,
            new DateTime(2026, 9, 7), null, "PERSONAL", "study", out var error);

        Assert.False(changed);
        Assert.Equal("SABBATICAL_RETURN_DATE_REQUIRED", error);
        Assert.Equal(EmployeeLifecycleStatuses.Active, state.EmploymentStatus);
    }

    [Fact]
    public void Availability_change_is_versioned_and_audited()
    {
        var state = new EmployeeLifecycleState();
        var changed = EmployeeLifecycleTransitionService.TryChangeAvailability(
            state, EmployeeLifecycleStatuses.MaternityLeave, "hr", 0,
            DateTime.UtcNow, new DateTime(2026, 12, 1), "MATERNITY", null, out var error);

        Assert.True(changed);
        Assert.Empty(error);
        Assert.Equal(1, state.Version);
        Assert.Single(state.History);
        Assert.Equal(EmployeeLifecycleStatuses.MaternityLeave, state.AvailabilityStatus);
    }

    [Fact]
    public void Stale_version_is_rejected_without_mutation()
    {
        var state = new EmployeeLifecycleState { Version = 2 };
        var changed = EmployeeLifecycleTransitionService.TryChangeAvailability(
            state, EmployeeLifecycleStatuses.UnauthorizedAbsence, "hr", 1,
            DateTime.UtcNow, null, "ABSENCE", null, out var error);

        Assert.False(changed);
        Assert.Equal("LIFECYCLE_VERSION_CONFLICT", error);
        Assert.Equal(EmployeeLifecycleStatuses.Working, state.AvailabilityStatus);
    }

    [Fact]
    public void Sabbatical_is_current_employee_but_not_attendance_expected()
    {
        var state = new EmployeeLifecycleState
        {
            AvailabilityStatus = EmployeeLifecycleStatuses.Sabbatical,
            AvailabilityExpectedReturnDate = new DateTime(2027, 1, 1)
        };

        Assert.True(state.IsCurrentEmployee());
        Assert.False(state.IsAttendanceExpected());
        Assert.True(state.CanResign());
        Assert.Equal(EmployeeLifecycleStatuses.Sabbatical, state.EffectiveStatus());
    }

    [Theory]
    [InlineData(EmployeeLifecycleStatuses.PayrollHold)]
    [InlineData(EmployeeLifecycleStatuses.PayrollPaused)]
    [InlineData(EmployeeLifecycleStatuses.PayrollClosed)]
    public void Non_active_payroll_status_excludes_current_employee_from_payroll(string payrollStatus)
    {
        var employee = new EmpUser
        {
            Status = true,
            FirstName = "Test",
            LastName = "Employee",
            Email = "test@example.com",
            DateOfJoining = "01 September 2026",
            Lifecycle = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.Active, PayrollStatus = payrollStatus }
        };

        Assert.False(EmploymentPeriod.IsPayrollIncludedForMonth(employee, new DateTime(2026, 9, 1)));
    }

    [Fact]
    public void Former_state_can_rejoin_but_cannot_be_changed_as_temporary_absence()
    {
        var state = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.Terminated };

        Assert.True(state.IsFormerEmployee());
        Assert.True(state.CanRejoin());
        Assert.False(state.CanResign());
        Assert.False(EmployeeLifecycleTransitionService.TryChangeAvailability(
            state, EmployeeLifecycleStatuses.Sabbatical, "hr", 0, DateTime.UtcNow,
            DateTime.UtcNow.Date.AddDays(30), "SABBATICAL", null, out var error));
        Assert.Equal("FORMER_EMPLOYEE_LIFECYCLE_LOCKED", error);
    }

    [Fact]
    public void Attendance_expectation_is_separate_from_employment()
    {
        var employee = new EmpUser
        {
            UserId = "e1", CompanyId = "c1", FirstName = "A", LastName = "B", Email = "a@b.test",
            DateOfJoining = "2026-01-01", Status = true,
            Lifecycle = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.Active, AvailabilityStatus = EmployeeLifecycleStatuses.Sabbatical }
        };

        Assert.True(EmploymentPeriod.WasEmployedOn(employee, new DateTime(2026, 9, 7)));
        Assert.False(EmploymentPeriod.IsAttendanceExpectedOn(employee, new DateTime(2026, 9, 7)));
    }

    [Fact]
    public void Temporary_lifecycle_leave_returns_to_attendance_on_expected_date()
    {
        var employee = new EmpUser
        {
            UserId = "e-return", CompanyId = "c1", FirstName = "A", LastName = "B", Email = "return@b.test",
            DateOfJoining = "2026-01-01", Status = true,
            Lifecycle = new EmployeeLifecycleState
            {
                EmploymentStatus = EmployeeLifecycleStatuses.Active,
                AvailabilityStatus = EmployeeLifecycleStatuses.MedicalLeave,
                AvailabilityExpectedReturnDate = new DateTime(2026, 9, 10)
            }
        };

        Assert.False(EmploymentPeriod.IsAttendanceExpectedOn(employee, new DateTime(2026, 9, 9)));
        Assert.True(EmploymentPeriod.IsAttendanceExpectedOn(employee, new DateTime(2026, 9, 10)));
        Assert.Equal(EmployeeLifecycleStatuses.Working, EmploymentPeriod.ResolveAvailabilityOn(employee, new DateTime(2026, 9, 10)));
    }

    [Fact]
    public void Former_employee_never_receives_attendance_even_with_legacy_unauthorized_state()
    {
        var state = new EmployeeLifecycleState
        {
            EmploymentStatus = EmployeeLifecycleStatuses.Terminated,
            AvailabilityStatus = EmployeeLifecycleStatuses.UnauthorizedAbsence
        };

        Assert.False(state.IsAttendanceExpected());
    }

    [Fact]
    public void Payroll_closed_employee_is_included_only_in_exit_month()
    {
        var employee = new EmpUser
        {
            UserId = "e2", CompanyId = "c1", FirstName = "A", LastName = "B", Email = "a@b.test",
            DateOfJoining = "2026-01-01", ExitDate = "2026-09-15", LastWorkingDate = new DateTime(2026, 9, 15), Status = false,
            EmploymentStatus = EmployeeLifecycleStatuses.Terminated,
            Lifecycle = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.Terminated, PayrollStatus = EmployeeLifecycleStatuses.PayrollClosed }
        };

        Assert.True(EmploymentPeriod.IsPayrollIncludedForMonth(employee, new DateTime(2026, 9, 1)));
        Assert.False(EmploymentPeriod.IsPayrollIncludedForMonth(employee, new DateTime(2026, 10, 1)));
    }

    [Fact]
    public void Completed_exit_closes_account_and_payroll_without_granting_former_portal_access()
    {
        var state = new EmployeeLifecycleState
        {
            EmploymentStatus = EmployeeLifecycleStatuses.NoticePeriod,
            AvailabilityStatus = EmployeeLifecycleStatuses.UnauthorizedAbsence,
            Version = 3
        };

        EmployeeLifecycleTransitionService.ApplyCompletedExit(
            state, EmployeeLifecycleStatuses.Terminated, "Absconding", "hr", new DateTime(2026, 9, 7));

        Assert.Equal(EmployeeLifecycleStatuses.Terminated, state.EmploymentStatus);
        Assert.Equal(EmployeeLifecycleStatuses.AccountClosed, state.AccountStatus);
        Assert.Equal(EmployeeLifecycleStatuses.PayrollFinalSettlement, state.PayrollStatus);
        Assert.Equal(EmployeeLifecycleStatuses.NotApplicable, state.AvailabilityStatus);
        Assert.Equal("Completed", state.OffboardingStatus);
        Assert.Equal("NotEligible", state.FormerPortalStatus);
        Assert.Equal(EmployeeLifecycleStatuses.AlumniEligible, state.AlumniStatus);
        Assert.Equal(4, state.Version);
        Assert.Single(state.History);
    }

    [Theory]
    [InlineData(EmployeeLifecycleStatuses.Retired)]
    [InlineData(EmployeeLifecycleStatuses.ContractEnded)]
    public void Retirement_and_contract_end_are_rejoinable_former_employment_states(string employmentStatus)
    {
        var state = new EmployeeLifecycleState { EmploymentStatus = employmentStatus };

        Assert.True(state.IsFormerEmployee());
        Assert.False(state.IsCurrentEmployee());
        Assert.True(state.CanRejoin());
    }

    [Fact]
    public void Deceased_employee_is_former_but_cannot_rejoin()
    {
        var employee = new EmpUser
        {
            Status = false,
            FirstName = "Deceased",
            LastName = "Employee",
            Email = "deceased@example.com",
            EmploymentStatus = EmployeeLifecycleStatuses.Deceased,
            Lifecycle = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.Deceased }
        };

        Assert.True(EmploymentExitRules.IsFormerEmployee(employee));
        Assert.False(EmploymentExitRules.IsCurrentEmployee(employee));
        Assert.False(EmploymentExitRules.CanLogin(employee));
        Assert.False(EmploymentExitRules.CanRejoin(employee));
    }

    [Theory]
    [InlineData(EmployeeLifecycleStatuses.Resigned)]
    [InlineData(EmployeeLifecycleStatuses.Terminated)]
    [InlineData(EmployeeLifecycleStatuses.Retired)]
    [InlineData(EmployeeLifecycleStatuses.ContractEnded)]
    public void Rejoin_rule_allows_non_deceased_completed_exits(string exitStatus)
    {
        var employee = new EmpUser
        {
            FirstName = "Former", LastName = "Employee", Email = "former@example.com",
            Status = false,
            EmploymentStatus = exitStatus,
            Lifecycle = new EmployeeLifecycleState { EmploymentStatus = exitStatus }
        };

        Assert.True(EmploymentExitRules.CanRejoin(employee));
    }

    [Fact]
    public void Former_portal_rule_requires_completed_non_deceased_policy_enabled_verified_access()
    {
        var exit = new EmployeeExit
        {
            Status = EmployeeLifecycleStatuses.Resigned,
            PolicySnapshot = new EmployeeExitPolicySnapshot { EnableFormerEmployeePortal = true },
            PostExitAccess = new EmployeeExitPostExitAccess { Enabled = true, PersonalEmailVerified = true }
        };

        Assert.True(EmploymentExitRules.CanUseFormerEmployeePortal(exit));
        exit.Status = EmployeeLifecycleStatuses.Deceased;
        Assert.False(EmploymentExitRules.CanUseFormerEmployeePortal(exit));
    }

    [Theory]
    [InlineData(EmployeeLifecycleStatuses.Resigned)]
    [InlineData(EmployeeLifecycleStatuses.Terminated)]
    [InlineData(EmployeeLifecycleStatuses.Retired)]
    [InlineData(EmployeeLifecycleStatuses.ContractEnded)]
    [InlineData(EmployeeLifecycleStatuses.Deceased)]
    public void Every_completed_exit_status_uses_the_same_employment_period_rules(string employmentStatus)
    {
        var employee = new EmpUser
        {
            UserId = "completed-exit", CompanyId = "c1", FirstName = "A", LastName = "B", Email = "exit@example.com",
            DateOfJoining = "2026-01-01", Status = false, EmploymentStatus = employmentStatus,
            LastWorkingDate = new DateTime(2026, 9, 15),
            Lifecycle = new EmployeeLifecycleState { EmploymentStatus = employmentStatus, PayrollStatus = EmployeeLifecycleStatuses.PayrollClosed }
        };

        Assert.True(EmploymentExitRules.IsFormerEmployee(employee));
        Assert.False(EmploymentPeriod.WasEmployedOn(employee, new DateTime(2026, 9, 16)));
        Assert.True(EmploymentPeriod.IsPayrollIncludedForMonth(employee, new DateTime(2026, 9, 1)));
        Assert.False(EmploymentPeriod.IsPayrollIncludedForMonth(employee, new DateTime(2026, 10, 1)));
    }

    [Theory]
    [InlineData(EmployeeLifecycleStatuses.Retired)]
    [InlineData(EmployeeLifecycleStatuses.ContractEnded)]
    [InlineData(EmployeeLifecycleStatuses.Deceased)]
    public void Completed_exit_request_aliases_normalize_to_final_status(string input)
    {
        Assert.Equal(input, EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(input));
    }

    [Theory]
    [InlineData(EmployeeLifecycleStatuses.ExitReasonRetirement, EmployeeLifecycleStatuses.Retired)]
    [InlineData(EmployeeLifecycleStatuses.ExitReasonContractEnd, EmployeeLifecycleStatuses.ContractEnded)]
    public void Legacy_exit_request_aliases_normalize_to_final_status(string input, string expected)
    {
        Assert.Equal(expected, EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(input));
    }

    [Fact]
    public void Account_and_payroll_changes_are_versioned_and_audited()
    {
        var state = new EmployeeLifecycleState();

        Assert.True(EmployeeLifecycleTransitionService.TryChangeAccountStatus(state, EmployeeLifecycleStatuses.AccountLimited, "hr", 0, DateTime.UtcNow, "security review", out _));
        Assert.True(EmployeeLifecycleTransitionService.TryChangePayrollStatus(state, EmployeeLifecycleStatuses.PayrollHold, "hr", 1, DateTime.UtcNow, "unpaid leave review", out _));
        Assert.Equal(EmployeeLifecycleStatuses.AccountLimited, state.AccountStatus);
        Assert.Equal(EmployeeLifecycleStatuses.PayrollHold, state.PayrollStatus);
        Assert.Equal(2, state.Version);
        Assert.Equal(2, state.History.Count);
    }

    [Fact]
    public void Alumni_status_is_only_editable_for_former_employees()
    {
        var active = new EmployeeLifecycleState();
        Assert.False(EmployeeLifecycleTransitionService.TryChangeAlumniStatus(active, EmployeeLifecycleStatuses.AlumniMember, "hr", 0, DateTime.UtcNow, "membership", out var activeError));
        Assert.Equal("ALUMNI_CURRENT_EMPLOYEE_FORBIDDEN", activeError);

        var former = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.Retired };
        Assert.True(EmployeeLifecycleTransitionService.TryChangeAlumniStatus(former, EmployeeLifecycleStatuses.AlumniMember, "hr", 0, DateTime.UtcNow, "membership", out _));
        Assert.Equal(EmployeeLifecycleStatuses.AlumniMember, former.AlumniStatus);
    }

    [Fact]
    public void Absconding_review_cannot_be_resolved_without_reason()
    {
        var state = new EmployeeLifecycleState { AvailabilityStatus = EmployeeLifecycleStatuses.AbscondingUnderReview };
        Assert.False(EmployeeLifecycleTransitionService.TryChangeAvailability(state, EmployeeLifecycleStatuses.Working, "hr", 0, DateTime.UtcNow, null, "RESOLVED", null, out var error));
        Assert.Equal("ABSENCE_RESOLUTION_REASON_REQUIRED", error);
    }

    [Fact]
    public void Absconding_under_review_remains_current_until_exit_completion()
    {
        var state = new EmployeeLifecycleState
        {
            EmploymentStatus = EmployeeLifecycleStatuses.Active,
            AvailabilityStatus = EmployeeLifecycleStatuses.UnauthorizedAbsence
        };

        Assert.True(EmployeeLifecycleTransitionService.TryChangeAvailability(state, EmployeeLifecycleStatuses.AbscondingUnderReview, "hr", 0, DateTime.UtcNow, null, EmployeeLifecycleStatuses.ExitReasonAbsconding, "Investigation opened", out _));
        Assert.True(state.IsCurrentEmployee());
        Assert.False(state.IsFormerEmployee());
        Assert.True(EmployeeLifecycleTransitionService.TryChangeAvailability(state, EmployeeLifecycleStatuses.Working, "hr", 1, DateTime.UtcNow, null, "ABSENCE_RESOLVED", "Returned to work", out _));
        Assert.Equal(EmployeeLifecycleStatuses.Working, state.AvailabilityStatus);
    }
}

