using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments;

public static class RecruitmentPreboardingPolicy
{
    public static bool Requires(RecruitmentEnterpriseConfig? config, JobVacancy? vacancy)
    {
        var allowCustomWorkflows = config is null || config.AllowJobWorkflowCustomization;
        var modeOverride = vacancy?.WorkflowMode switch
        {
            "Direct" => false,
            "Enterprise" => true,
            _ => (bool?)null,
        };
        var vacancyOverride = allowCustomWorkflows ? vacancy?.RequirePreboarding ?? modeOverride : null;

        return vacancyOverride
            ?? ((config is null || config.RequirePreboardingBeforeHandoff)
                && !string.Equals(config?.RecruitmentMode, "Simple", StringComparison.OrdinalIgnoreCase));
    }
}
