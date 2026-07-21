using System.ComponentModel.DataAnnotations;

public class AttendancePenaltyPolicyDto
{
    public string? Id { get; set; }
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
