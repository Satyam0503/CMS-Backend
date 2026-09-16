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
        state.EmploymentStatus is EmployeeLifecycleStatuses.Resigned or EmployeeLifecycleStatuses.Terminated
            or EmployeeLifecycleStatuses.Retired or EmployeeLifecycleStatuses.ContractEnded or EmployeeLifecycleStatuses.Deceased;

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
