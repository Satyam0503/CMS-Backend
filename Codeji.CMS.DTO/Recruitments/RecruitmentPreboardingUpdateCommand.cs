namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentPreboardingUpdateCommand { public string CaseId { get; set; } = string.Empty; public string Status { get; set; } = "InProgress"; public List<RecruitmentPreboardingTaskUpdate> Tasks { get; set; } = []; public int? ExpectedVersion { get; set; } public string IdempotencyKey { get; set; } = string.Empty; }
public sealed class RecruitmentPreboardingTaskUpdate { public string Code { get; set; } = string.Empty; public string Status { get; set; } = "Pending"; public string? AssigneeUserId { get; set; }
}
