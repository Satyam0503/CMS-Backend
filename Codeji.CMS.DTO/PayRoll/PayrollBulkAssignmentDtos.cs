using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.PayRoll;

public sealed class PayrollBulkAssignmentRequestDto
{
    public List<string> EmployeeIds { get; set; } = [];
    public string? Department { get; set; }
    public string? JobRole { get; set; }
    public int? EmploymentType { get; set; }
    [Required] public string PayGroupId { get; set; } = string.Empty;
    public string? SalaryTemplateId { get; set; }
    public string? PolicyProfileId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public sealed class PayrollBulkAssignmentPreviewRowDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? JobRole { get; set; }
    public bool HasExistingAssignment { get; set; }
    public bool HasOverlap { get; set; }
    public string? ConflictMessage { get; set; }
    public bool CanSubmit { get; set; }
}

public sealed class PayrollBulkAssignmentResultDto
{
    public int SubmittedCount { get; set; }
    public int ConflictCount { get; set; }
    public List<PayrollBulkAssignmentPreviewRowDto> Rows { get; set; } = [];
}