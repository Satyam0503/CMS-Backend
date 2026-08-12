using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Employee;

public sealed class EmployeeIdSequenceResponseDto
{
    public string CurrentNextEmployeeId { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

public sealed class SkipEmployeeIdsRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Skip count must be greater than 0.")]
    public int SkipCount { get; set; }
}

public sealed class StartEmployeeIdSequenceRequestDto
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;
}

public sealed class AssignMissingEmployeeIdRequestDto
{
    /// <summary>Allowed only when the company has disabled automatic Employee ID generation.</summary>
    public string? EmployeeId { get; set; }
}

public sealed class AssignMissingEmployeeIdResponseDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public bool ManualEntryAllowed { get; set; }
    public string Format { get; set; } = string.Empty;
}
