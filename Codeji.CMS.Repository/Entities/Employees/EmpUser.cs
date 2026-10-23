using Codeji.CMS.DTO.Employee;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public class EmpUser : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string UserId { get; set; }
    public string RoleId { get; set; }
    public required string FirstName { get; set; }
    public required string? LastName { get; set; }
    public required string Email { get; set; }
    public string EmailNormalized { get; set; } = string.Empty;
    public string Password { get; set; }
    public string Gender { get; set; }
    public string EmployeeId { get; set; }
    public string JobRole { get; set; }
    public string? DateOfBirth { get; set; }
    public string Department { get; set; }
    public string ReportingManager { get; set; }
    public string PhoneNumber { get; set; }
    public string BloodGroup { get; set; }
    public string PersonalEmail { get; set; }
    public string EmergencyContact { get; set; }
    public string DateOfJoining { get; set; }
    public string? ExitDate { get; set; }
    // The legacy ExitDate is retained for compatibility. The exit aggregate is the
    // authoritative workflow record; these fields are the read-model used by login
    // and directory queries.
    public string? EmploymentStatus { get; set; }
    /// <summary>Canonical date-only employment end date. ExitDate is retained for legacy consumers.</summary>
    public DateTime? LastWorkingDate { get; set; }
    /// <summary>Tenant-configurable contract lifecycle read model. ContractEnd exits remain authoritative in EmployeeExit.</summary>
    public EmployeeContractLifecycle Contract { get; set; } = new();
    /// <summary>Additive lifecycle read model. EmployeeExit remains authoritative for exit workflow.</summary>
    public EmployeeLifecycleState Lifecycle { get; set; } = new();
    public int? EmploymentType { get; set; } = null;
    public bool Status { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Address { get; set; }
    public string ProfileUrl { get; set; }
    public string? PanNumber { get; set; }
    public ulong? BankAccountNumber { get; set; } = null;
    public List<EmpUserCustomAttribute> CustomAttributeList { get; set; } = [];

    /// <summary>Recruitment final interview benchmark score (0–100%) stored at hiring to measure ongoing performance.</summary>
    public int? HiringBenchmarkScore { get; set; }
    /// <summary>Recruitment final interview rating (1.0–5.0 scale) aggregated across non-HR evaluation rounds.</summary>
    public decimal? HiringBenchmarkRating { get; set; }
    /// <summary>Consensus recommendation from evaluation rounds (Strong Hire, Hire, Hold, No Hire).</summary>
    public string? HiringRecommendation { get; set; }
    /// <summary>Number of substantive evaluation rounds rated.</summary>
    public int? EvaluatedRoundsCount { get; set; }
}

public sealed class EmployeeContractLifecycle
{
    public string Status { get; set; } = "Indefinite"; // Indefinite, FixedTerm, Extended, Ended
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Version { get; set; }
    public List<EmployeeContractChange> History { get; set; } = [];
}

public sealed class EmployeeContractChange
{
    public string Action { get; set; } = string.Empty; // Created, Extended, Ended
    public DateTime? PreviousEndDate { get; set; }
    public DateTime? NewEndDate { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
