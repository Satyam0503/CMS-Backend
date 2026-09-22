using Codeji.CMS.Utility.Enums;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>Pure, side-effect-free rules for the compatibility recruitment pipeline.</summary>
public static class RecruitmentPipelineRules
{
    public static IReadOnlyList<string> ParseSnapshot(string? snapshot)
        => (snapshot ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Generic applicant profile edits may not advance the recruitment lifecycle.</summary>
    public static bool RequiresCanonicalStageCommand(ActivityType current, ActivityType requested) => current != requested;

    public static bool IsAllowedTransition(ActivityType current, ActivityType target, out string error)
    {
        error = string.Empty;
        if (current == target) return true;
        if (current is ActivityType.Selected or ActivityType.Rejected or ActivityType.ReApply)
        {
            error = $"Applicant status cannot be changed after it is {current}.";
            return false;
        }
        if (target == ActivityType.Selected)
        {
            error = "Applicants must be selected through the hiring decision workflow.";
            return false;
        }
        if (target == ActivityType.Rejected) return true;
        if (target is ActivityType.New or ActivityType.ReApply)
        {
            error = $"Applicant status cannot be changed from {current} to {target}.";
            return false;
        }
        if ((int)target > (int)current) return true;
        error = $"Applicant status cannot be changed from {current} back to {target}.";
        return false;
    }
}
