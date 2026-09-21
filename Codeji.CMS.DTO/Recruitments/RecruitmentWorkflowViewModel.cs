using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentWorkflowActionViewModel
{
    public string Code { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? DisabledReason { get; set; }
}

public sealed class RecruitmentWorkflowViewModel
{
    public string ApplicantId { get; set; } = string.Empty;
    public ActivityType CurrentStage { get; set; }
    public int PipelineVersion { get; set; }
    public List<RecruitmentWorkflowActionViewModel> Actions { get; set; } = [];
}