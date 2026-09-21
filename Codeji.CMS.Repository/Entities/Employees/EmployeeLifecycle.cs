using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public static class EmployeeLifecycleStatuses
{
    public const string PreJoining = "PreJoining";
    public const string Active = "Active";
    public const string NoticePeriod = "NoticePeriod";
    public const string Suspended = "Suspended";
    public const string Resigned = "Resigned";
    public const string Terminated = "Terminated";
    public const string Retired = "Retired";
    public const string ContractEnded = "ContractEnded";
    public const string Deceased = "Deceased";

    public const string Working = "Working";
    public const string NotApplicable = "NotApplicable";
    public const string AnnualLeave = "AnnualLeave";
    public const string SickLeave = "SickLeave";
    public const string MedicalLeave = "MedicalLeave";
    public const string SickMedicalLeave = "SickMedicalLeave";
    public const string MaternityLeave = "MaternityLeave";
    public const string PaternityLeave = "PaternityLeave";
    public const string Sabbatical = "Sabbatical";
    public const string LongTermLeave = "LongTermLeave";
    public const string AdministrativeLeave = "AdministrativeLeave";
    public const string UnauthorizedAbsence = "UnauthorizedAbsence";
    public const string AbscondingUnderReview = "AbscondingUnderReview";
    public const string SuspendedAvailability = "Suspended";

    public const string AccountActive = "Active";
    public const string AccountLocked = "Locked";
    public const string AccountTemporarilyDisabled = "TemporarilyDisabled";
    public const string AccountLimited = "Limited";
    public const string AccountSuspended = "Suspended";
    public const string AccountClosed = "Closed";

    public const string PayrollActive = "Active";
    public const string PayrollHold = "Hold";
    public const string PayrollPaused = "Paused";
    public const string PayrollFinalSettlement = "FinalSettlement";
    public const string PayrollClosed = "Closed";

    public const string OffboardingNone = "None";
    public const string OffboardingPendingManagerApproval = "PendingManagerApproval";
    public const string OffboardingPendingHrApproval = "PendingHrApproval";
    public const string OffboardingNoticePeriod = "NoticePeriod";
    public const string OffboardingDiscussion = "Discussion";
    public const string OffboardingWithdrawalPending = "WithdrawalPending";
    public const string OffboardingCompleted = "Completed";
    public const string OffboardingWithdrawn = "Withdrawn";

    public const string FormerPortalNotEligible = "NotEligible";
    public const string FormerPortalEligible = "Eligible";
    public const string FormerPortalInvited = "Invited";
    public const string FormerPortalActive = "Active";
    public const string FormerPortalRevoked = "Revoked";
    public const string FormerPortalExpired = "Expired";

    public const string AlumniNotApplicable = "NotApplicable";
    public const string AlumniEligible = "Eligible";
    public const string AlumniInvited = "Invited";
    public const string AlumniMember = "Member";
    public const string AlumniOptedOut = "OptedOut";
    public const string AlumniBlocked = "Blocked";

    public const string ExitReasonRetirement = "Retirement";
    public const string ExitReasonContractEnd = "ContractEnd";
    public const string ExitReasonAbsconding = "Absconding";
    public const string ExitReasonDeceased = "Deceased";
    public const string ExitReasonRedundancy = "Redundancy";
    public const string ExitReasonMisconduct = "Misconduct";

    /// <summary>
    /// Final employment states. Keep this list as the single source for
    /// compatibility checks; new consumers must not maintain their own
    /// resignation/termination-only string comparisons.
    /// </summary>
    public static readonly string[] CompletedEmploymentStatuses =
    [
        Resigned,
        Terminated,
        Retired,
        ContractEnded,
        Deceased
    ];
    public static readonly string[] FormerPortalEligibleStatuses =
    [Resigned, Terminated, Retired, ContractEnded];

    /// <summary>
    /// Normalizes accepted exit-request values to the persisted final
    /// employment status. The long-form values remain supported for existing
    /// clients and historical records.
    /// </summary>
    public static string? NormalizeCompletedEmploymentStatus(string? value) => value?.Trim() switch
    {
        Resigned => Resigned,
        Terminated => Terminated,
        Retired or ExitReasonRetirement => Retired,
        ContractEnded or ExitReasonContractEnd => ContractEnded,
        Deceased or ExitReasonDeceased => Deceased,
        _ => null
    };

    public static bool IsCompletedEmploymentStatus(string? value) =>
        NormalizeCompletedEmploymentStatus(value) is not null;
}

public sealed class EmployeeLifecycleStatusHistory
{
    [BsonElement("Dimension")]
    public string Dimension { get; set; } = string.Empty;
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
    public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveToUtc { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EmployeeLifecycleState
{
    public string EmploymentStatus { get; set; } = EmployeeLifecycleStatuses.Active;
    public string AvailabilityStatus { get; set; } = EmployeeLifecycleStatuses.Working;
    public string AccountStatus { get; set; } = EmployeeLifecycleStatuses.AccountActive;
    public string PayrollStatus { get; set; } = EmployeeLifecycleStatuses.PayrollActive;
    public string OffboardingStatus { get; set; } = "None";
    public string FormerPortalStatus { get; set; } = "NotEligible";
    public string AlumniStatus { get; set; } = EmployeeLifecycleStatuses.AlumniNotApplicable;
    public DateTime? AvailabilityEffectiveFrom { get; set; }
    public DateTime? AvailabilityExpectedReturnDate { get; set; }
    public string? AvailabilityReasonCode { get; set; }
    public int Version { get; set; }
    public List<EmployeeLifecycleStatusHistory> History { get; set; } = [];
}

public static class EmployeeLifecycleRules
{
    public static bool IsFormerEmployee(this EmployeeLifecycleState state) =>
        EmployeeLifecycleStatuses.IsCompletedEmploymentStatus(state.EmploymentStatus);

    public static bool IsCurrentEmployee(this EmployeeLifecycleState state) => !state.IsFormerEmployee() &&
        state.EmploymentStatus is EmployeeLifecycleStatuses.Active or EmployeeLifecycleStatuses.NoticePeriod or EmployeeLifecycleStatuses.Suspended;

    public static bool IsAttendanceExpected(this EmployeeLifecycleState state) =>
        state.IsCurrentEmployee() &&
        (state.AvailabilityStatus is EmployeeLifecycleStatuses.Working or EmployeeLifecycleStatuses.UnauthorizedAbsence);

    public static bool CanLogin(this EmployeeLifecycleState state) =>
        state.IsCurrentEmployee() && state.AccountStatus == EmployeeLifecycleStatuses.AccountActive;

    public static bool PayrollEligible(this EmployeeLifecycleState state) =>
        state.IsCurrentEmployee() && state.PayrollStatus == EmployeeLifecycleStatuses.PayrollActive;

    public static bool CanRejoin(this EmployeeLifecycleState state) => state.IsFormerEmployee();

    public static bool CanResign(this EmployeeLifecycleState state) =>
        state.IsCurrentEmployee() && state.EmploymentStatus != EmployeeLifecycleStatuses.NoticePeriod;

    public static string EffectiveStatus(this EmployeeLifecycleState state) =>
        state.IsFormerEmployee() ? state.EmploymentStatus : state.AvailabilityStatus;

    public static string? ExpectedReturn(this EmployeeLifecycleState state) =>
        state.AvailabilityExpectedReturnDate?.ToString("yyyy-MM-dd");

    public static string LifecycleReason(this EmployeeLifecycleState state) =>
        state.AvailabilityReasonCode ?? (state.IsFormerEmployee() ? "EMPLOYMENT_ENDED" : "NORMAL_WORKING_STATE");
}

/// <summary>
/// Compatibility-aware employment rules for consumers that receive an EmpUser
/// rather than a lifecycle state. Lifecycle is authoritative when present;
/// legacy fields remain a safe fallback until data backfill is complete.
/// </summary>
public static class EmploymentExitRules
{
    public static string GetEffectiveEmploymentStatus(EmpUser employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        return employee.Lifecycle?.EmploymentStatus?.Trim()
            ?? employee.EmploymentStatus?.Trim()
            ?? EmployeeLifecycleStatuses.Active;
    }

    public static bool IsFormerEmployee(EmpUser employee) =>
        EmployeeLifecycleStatuses.IsCompletedEmploymentStatus(GetEffectiveEmploymentStatus(employee));

    public static bool IsCurrentEmployee(EmpUser employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        var status = GetEffectiveEmploymentStatus(employee);
        return employee.Status && status is (EmployeeLifecycleStatuses.Active
            or EmployeeLifecycleStatuses.NoticePeriod
            or EmployeeLifecycleStatuses.Suspended);
    }

    public static bool IsEligibleForWorkActivity(EmpUser employee) => IsCurrentEmployee(employee) &&
        (employee.Lifecycle is null || employee.Lifecycle.AvailabilityStatus is
            EmployeeLifecycleStatuses.Working or EmployeeLifecycleStatuses.UnauthorizedAbsence);

    public static bool CanLogin(EmpUser employee) =>
        IsCurrentEmployee(employee) &&
        (employee.Lifecycle is null || employee.Lifecycle.CanLogin());

    public static bool CanRejoin(EmpUser employee) =>
        IsFormerEmployee(employee) &&
        !string.Equals(GetEffectiveEmploymentStatus(employee), EmployeeLifecycleStatuses.Deceased, StringComparison.OrdinalIgnoreCase);

    public static bool CanUseFormerEmployeePortal(EmployeeExit exit) =>
        exit is not null &&
        EmployeeLifecycleStatuses.FormerPortalEligibleStatuses.Contains(exit.Status) &&
        exit.PolicySnapshot?.EnableFormerEmployeePortal == true &&
        exit.PostExitAccess?.Enabled == true &&
        exit.PostExitAccess.PersonalEmailVerified == true;
}
