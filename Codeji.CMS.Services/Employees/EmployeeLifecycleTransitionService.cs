using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Employees;

/// <summary>
/// Applies additive lifecycle changes to an employee read model. Callers must
/// persist the returned state with a tenant and version filter; EmployeeExit
/// remains the authority for completed exits and rejoin decisions.
/// </summary>
public static class EmployeeLifecycleTransitionService
{
    public static bool TryChangeAccountStatus(EmployeeLifecycleState state, string value, string actorUserId, int expectedVersion, DateTime effectiveAtUtc, string? reason, out string error)
        => TryChangeDimension(state, nameof(EmployeeLifecycleState.AccountStatus), value, actorUserId, expectedVersion, effectiveAtUtc, reason, IsAllowedAccount, out error);

    public static bool TryChangePayrollStatus(EmployeeLifecycleState state, string value, string actorUserId, int expectedVersion, DateTime effectiveAtUtc, string? reason, out string error)
        => TryChangeDimension(state, nameof(EmployeeLifecycleState.PayrollStatus), value, actorUserId, expectedVersion, effectiveAtUtc, reason, IsAllowedPayroll, out error);

    public static bool TryChangeAlumniStatus(EmployeeLifecycleState state, string value, string actorUserId, int expectedVersion, DateTime effectiveAtUtc, string? reason, out string error)
        => TryChangeDimension(state, nameof(EmployeeLifecycleState.AlumniStatus), value, actorUserId, expectedVersion, effectiveAtUtc, reason, IsAllowedAlumni, out error);

    private static bool TryChangeDimension(EmployeeLifecycleState state, string dimension, string value, string actorUserId, int expectedVersion, DateTime effectiveAtUtc, string? reason, Func<string, bool> allowed, out string error)
    {
        error = string.Empty;
        if (state.Version != expectedVersion) { error = "LIFECYCLE_VERSION_CONFLICT"; return false; }
        if (!allowed(value)) { error = "LIFECYCLE_STATUS_INVALID"; return false; }
        if (dimension == nameof(EmployeeLifecycleState.AlumniStatus) && !state.IsFormerEmployee()) { error = "ALUMNI_CURRENT_EMPLOYEE_FORBIDDEN"; return false; }
        if (dimension == nameof(EmployeeLifecycleState.AlumniStatus) && state.EmploymentStatus == EmployeeLifecycleStatuses.Deceased) { error = "DECEASED_EMPLOYEE_ALUMNI_LOCKED"; return false; }
        if (state.IsFormerEmployee() && dimension is nameof(EmployeeLifecycleState.AccountStatus) && value != EmployeeLifecycleStatuses.AccountClosed)
        { error = "FORMER_EMPLOYEE_ACCOUNT_LOCKED"; return false; }
        var previous = dimension == nameof(EmployeeLifecycleState.AccountStatus) ? state.AccountStatus : dimension == nameof(EmployeeLifecycleState.PayrollStatus) ? state.PayrollStatus : state.AlumniStatus;
        if (dimension == nameof(EmployeeLifecycleState.AccountStatus)) state.AccountStatus = value;
        else if (dimension == nameof(EmployeeLifecycleState.PayrollStatus)) state.PayrollStatus = value;
        else state.AlumniStatus = value;
        state.Version++;
        state.History.Add(new EmployeeLifecycleStatusHistory { Dimension = dimension, PreviousValue = previous, NewValue = value, Reason = reason, EffectiveFromUtc = effectiveAtUtc, ChangedBy = actorUserId, ChangedAtUtc = DateTime.UtcNow });
        return true;
    }

    public static void ApplyCompletedExit(EmployeeLifecycleState state, string exitType, string? reason, string actorUserId, DateTime effectiveAtUtc)
    {
        var previousEmployment = state.EmploymentStatus;
        state.EmploymentStatus = EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(exitType)
            ?? EmployeeLifecycleStatuses.Terminated;
        state.AvailabilityStatus = EmployeeLifecycleStatuses.NotApplicable;
        state.AccountStatus = EmployeeLifecycleStatuses.AccountClosed;
        state.PayrollStatus = EmployeeLifecycleStatuses.PayrollFinalSettlement;
        state.OffboardingStatus = EmployeeLifecycleStatuses.OffboardingCompleted;
        // Former-portal access is still controlled by its existing invitation flow.
        state.FormerPortalStatus = EmployeeLifecycleStatuses.FormerPortalNotEligible;
        state.AlumniStatus = state.EmploymentStatus == EmployeeLifecycleStatuses.Deceased
            ? EmployeeLifecycleStatuses.AlumniBlocked
            : state.AlumniStatus is EmployeeLifecycleStatuses.AlumniBlocked or EmployeeLifecycleStatuses.AlumniOptedOut
                ? state.AlumniStatus
                : EmployeeLifecycleStatuses.AlumniEligible;
        state.AvailabilityExpectedReturnDate = null;
        state.AvailabilityReasonCode = exitType;
        state.Version++;
        state.History.Add(new EmployeeLifecycleStatusHistory
        {
            Dimension = nameof(EmployeeLifecycleState.EmploymentStatus),
            PreviousValue = previousEmployment,
            NewValue = state.EmploymentStatus,
            ReasonCode = exitType,
            Reason = reason,
            EffectiveFromUtc = effectiveAtUtc,
            ChangedBy = actorUserId,
            ChangedAtUtc = effectiveAtUtc
        });
    }

    public static bool TryChangeAvailability(
        EmployeeLifecycleState state,
        string availability,
        string actorUserId,
        int expectedVersion,
        DateTime effectiveFromUtc,
        DateTime? expectedReturnDate,
        string? reasonCode,
        string? reason,
        out string error)
    {
        error = string.Empty;
        if (state.Version != expectedVersion) { error = "LIFECYCLE_VERSION_CONFLICT"; return false; }
        if (!IsAllowedAvailability(availability)) { error = "LIFECYCLE_AVAILABILITY_INVALID"; return false; }
        if (availability == EmployeeLifecycleStatuses.Sabbatical && !expectedReturnDate.HasValue)
        { error = "SABBATICAL_RETURN_DATE_REQUIRED"; return false; }
        if (expectedReturnDate.HasValue && expectedReturnDate.Value.Date < effectiveFromUtc.Date.AddDays(-1))
        { error = "LIFECYCLE_RETURN_DATE_INVALID"; return false; }
        if (state.IsFormerEmployee()) { error = "FORMER_EMPLOYEE_LIFECYCLE_LOCKED"; return false; }
        if (state.AvailabilityStatus == EmployeeLifecycleStatuses.AbscondingUnderReview && availability == EmployeeLifecycleStatuses.Working && string.IsNullOrWhiteSpace(reason))
        { error = "ABSENCE_RESOLUTION_REASON_REQUIRED"; return false; }

        var previous = state.AvailabilityStatus;
        state.AvailabilityStatus = availability;
        state.AvailabilityEffectiveFrom = effectiveFromUtc;
        state.AvailabilityExpectedReturnDate = availability == EmployeeLifecycleStatuses.Working ? null : expectedReturnDate?.Date;
        state.AvailabilityReasonCode = reasonCode;
        state.Version++;
        state.History.Add(new EmployeeLifecycleStatusHistory
        {
            Dimension = nameof(EmployeeLifecycleState.AvailabilityStatus),
            PreviousValue = previous,
            NewValue = availability,
            ReasonCode = reasonCode,
            Reason = reason,
            EffectiveFromUtc = effectiveFromUtc,
            ExpectedReturnDate = expectedReturnDate?.Date,
            ChangedBy = actorUserId,
            ChangedAtUtc = DateTime.UtcNow
        });
        return true;
    }

    private static bool IsAllowedAvailability(string value) => value is
        EmployeeLifecycleStatuses.Working or EmployeeLifecycleStatuses.MedicalLeave or
        EmployeeLifecycleStatuses.MaternityLeave or
        EmployeeLifecycleStatuses.PaternityLeave or EmployeeLifecycleStatuses.Sabbatical or
        EmployeeLifecycleStatuses.LongTermLeave or EmployeeLifecycleStatuses.AdministrativeLeave or
        EmployeeLifecycleStatuses.UnauthorizedAbsence or EmployeeLifecycleStatuses.AbscondingUnderReview or
        EmployeeLifecycleStatuses.Suspended;

    private static bool IsAllowedAccount(string value) => value is EmployeeLifecycleStatuses.AccountActive or EmployeeLifecycleStatuses.AccountLocked or EmployeeLifecycleStatuses.AccountLimited or EmployeeLifecycleStatuses.AccountTemporarilyDisabled or EmployeeLifecycleStatuses.AccountSuspended or EmployeeLifecycleStatuses.AccountClosed;

    private static bool IsAllowedPayroll(string value) => value is EmployeeLifecycleStatuses.PayrollActive or EmployeeLifecycleStatuses.PayrollHold or EmployeeLifecycleStatuses.PayrollPaused or EmployeeLifecycleStatuses.PayrollFinalSettlement or EmployeeLifecycleStatuses.PayrollClosed;

    private static bool IsAllowedAlumni(string value) => value is EmployeeLifecycleStatuses.AlumniNotApplicable or EmployeeLifecycleStatuses.AlumniEligible or EmployeeLifecycleStatuses.AlumniInvited or EmployeeLifecycleStatuses.AlumniMember or EmployeeLifecycleStatuses.AlumniOptedOut or EmployeeLifecycleStatuses.AlumniBlocked;
}
