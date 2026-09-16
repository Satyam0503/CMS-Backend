using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

/// <summary>Company policy with department taking precedence over role, then company default.</summary>
public sealed class EmployeeExitPolicy : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EmployeeExitPolicyId { get; set; } = string.Empty;
    /// <summary>Persisted tenant-local scope identity: Company, Department:&lt;id&gt;, or Role:&lt;id&gt;.</summary>
    public string ScopeKey { get; set; } = "Company";
    public string? DepartmentId { get; set; }
    /// <summary>Department scope set for a shared policy. Legacy records use DepartmentId only.</summary>
    public List<string> DepartmentIds { get; set; } = [];
    public string? RoleId { get; set; }
    public int MinimumNoticeDays { get; set; } = 30;
    /// <summary>Monthly pay component used to calculate notice recovery for this policy scope.</summary>
    public string NoticePaySalaryBasis { get; set; } = "GrossMonthly"; // BasicMonthly, GrossMonthly
    /// <summary>Policy divisor for daily notice pay. This is a rule, not a fixed employee currency rate.</summary>
    public int NoticePayDailyRateDivisor { get; set; } = 30;
    /// <summary>Allows a company (or scoped policy) to opt out of the offboarding workflow.</summary>
    public bool EnableOffboarding { get; set; } = true;
    public int Version { get; set; } = 1;
    public bool RequireManagerReview { get; set; } = true;
    public bool RequireHrReview { get; set; } = true;
    public bool AllowJointDiscussion { get; set; } = true;
    public bool RequireResignationEvidence { get; set; }
    public bool RequireEmployeeAcknowledgement { get; set; }
    public bool EnableFormerEmployeePortal { get; set; }
    public bool EnableNoticePeriodReminders { get; set; } = true;
    public int NoticePeriodReminderEveryDays { get; set; } = 3;
    public List<string> NoticePeriodReminderOffsets { get; set; } = new() { "30", "15", "7", "3", "1", "0" };
    public bool NotifyNoticePeriodEmployee { get; set; } = true;
    public bool NotifyNoticePeriodReportingManager { get; set; } = true;
    public bool NotifyNoticePeriodHr { get; set; } = true;
    public bool NotifyNoticePeriodAdmins { get; set; }
    public bool NoticePeriodReminderEmail { get; set; } = true;
    public bool RequireHandover { get; set; }
    public bool RequireAssetClearance { get; set; }
    public bool RequireAccessRevocation { get; set; }
    public bool RequireFinanceClearance { get; set; }
    public bool RequireBenefitClearance { get; set; }
    public bool RequireFinalSettlement { get; set; }
    /// <summary>Preset used to seed this policy. It never restricts the independently editable features.</summary>
    public string PresetLevel { get; set; } = "Custom";
    /// <summary>Explicit modular settings. Empty means this legacy policy uses the boolean fields above.</summary>
    public List<EmployeeExitFeatureConfiguration> Features { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

/// <summary>Tenant policy building block. Enabled controls visibility/creation; BlocksCompletion is independent.</summary>
public sealed class EmployeeExitFeatureConfiguration
{
    public string Key { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool BlocksCompletion { get; set; }
    public bool Required { get; set; }
    public string? OwnerPermission { get; set; }
    public bool AllowWaiver { get; set; }
    public bool WaiverReasonRequired { get; set; }
    public int? DueDaysBeforeLastWorkingDate { get; set; }
}
