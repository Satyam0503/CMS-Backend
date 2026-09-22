using Codeji.CMS.Utility.Enums;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>Single compatibility boundary for the canonical application stage vocabulary.</summary>
public static class RecruitmentApplicationStageCatalog
{
    public static readonly IReadOnlyList<string> OrderedStages =
    [
        nameof(ActivityType.New), nameof(ActivityType.InProgress), nameof(ActivityType.OnHold),
        nameof(ActivityType.Shortlisted), nameof(ActivityType.Selected), nameof(ActivityType.Rejected),
        nameof(ActivityType.ReApply)
    ];

    public static bool IsSupported(string? stage)
        => !string.IsNullOrWhiteSpace(stage) && OrderedStages.Any(x => string.Equals(x, stage.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string stage)
    {
        var value = stage.Trim();
        return OrderedStages.FirstOrDefault(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)) ?? value;
    }

    public static bool TryParse(string? stage, out ActivityType parsed)
        => Enum.TryParse(stage?.Trim(), true, out parsed) && IsSupported(stage);
}