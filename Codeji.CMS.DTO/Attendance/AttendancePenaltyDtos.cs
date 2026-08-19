using System.ComponentModel.DataAnnotations;

public class AttendancePenaltyPolicyDto
{
    public string? Id { get; set; }
    /// <summary>Returned employee identity. The API route, not this field, selects the employee.</summary>
    public string? UserId { get; set; }
    public string? EmployeeId { get; set; }
    public string Name { get; set; } = "Combined LHD + ED monthly allowance";
    [Range(0, 31)] public int CombinedLhdEdMonthlyLimit { get; set; } = 2;
    public bool IsEnabled { get; set; } = true;
    public bool RequiresHrApproval { get; set; } = true;
    public string DefaultDecision { get; set; } = "REVIEW_REQUIRED";
    [Required] public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Version { get; set; }
}

public class AttendanceExceptionFilterDto
{
    [Required] public DateTime PayrollMonth { get; set; }
    public string? EmployeeId { get; set; }
    public string? Status { get; set; }
    public string? Decision { get; set; }
    [Range(1, 1000)] public int PageSize { get; set; } = 25;
    [Range(1, int.MaxValue)] public int PageNo { get; set; } = 1;
}

public class AttendanceExceptionReviewDto
{
    [Required] public required string Decision { get; set; }
    [Required, MinLength(3)] public required string Reason { get; set; }
    [Range(0, 31)] public decimal? DeductionDayFraction { get; set; }
    public int Version { get; set; }
}

/// <summary>Controlled correction payload for a stored monthly attendance validation exception.</summary>
public class AttendanceExceptionResolutionDto
{
    [Required] public DateTime AttendanceDate { get; set; }
    [Required, MinLength(1)] public string Status { get; set; } = string.Empty;
    public string? CheckInTime { get; set; }
    public string? CheckOutTime { get; set; }
    [Required, MinLength(3)] public string Reason { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int ExceptionVersion { get; set; }
    public long? AttendanceVersion { get; set; }
}

public class AttendanceExceptionResolutionResultDto
{
    public required string ExceptionId { get; set; }
    public required string AttendanceAction { get; set; }
    public bool IsResolved { get; set; }
    public string? RemainingIssue { get; set; }
}

public class AttendanceMonthRequestDto
{
    [Required] public DateTime PayrollMonth { get; set; }
}

public class AttendanceMonthLockStatusDto
{
    public DateTime PayrollMonth { get; set; }
    public bool IsLocked { get; set; }
    public int EligibleEmployeeCount { get; set; }
    public int LockedEmployeeCount { get; set; }
}

/// <summary>One actionable attendance cell that prevents an attendance month from being locked.</summary>
public class AttendanceLockBlockingIssueDto
{
    public required string EmployeeId { get; set; }
    public required string EmployeeCode { get; set; }
    public required string EmployeeName { get; set; }
    public required DateTime AttendanceDate { get; set; }
    public required string ExceptionType { get; set; }
    public string? CurrentAttendanceStatus { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public required string Message { get; set; }
}

/// <summary>Lock outcome with the same blocking cells shown in Monthly Exceptions.</summary>
public class AttendanceMonthLockValidationResultDto
{
    public List<AttendanceLockBlockingIssueDto> BlockingIssues { get; set; } = [];
}
