using Codeji.CMS.Services.Recruitments;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentApplicationStageCatalogTests
{
    [Fact]
    public void Catalog_preserves_legacy_order_and_normalizes_case()
    {
        Assert.Equal(nameof(ActivityType.New), RecruitmentApplicationStageCatalog.OrderedStages[0]);
        Assert.Equal(nameof(ActivityType.ReApply), RecruitmentApplicationStageCatalog.OrderedStages[^1]);
        Assert.Equal(nameof(ActivityType.Shortlisted), RecruitmentApplicationStageCatalog.Normalize(" shortlisted "));
    }

    [Theory]
    [InlineData("New")]
    [InlineData("InProgress")]
    [InlineData("OnHold")]
    [InlineData("Shortlisted")]
    [InlineData("Selected")]
    [InlineData("Rejected")]
    [InlineData("ReApply")]
    public void Catalog_accepts_only_supported_stages(string stage)
    {
        Assert.True(RecruitmentApplicationStageCatalog.IsSupported(stage));
        Assert.True(RecruitmentApplicationStageCatalog.TryParse(stage, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Interview")]
    [InlineData("Offer")]
    public void Catalog_rejects_non_canonical_stages(string stage)
    {
        Assert.False(RecruitmentApplicationStageCatalog.IsSupported(stage));
        Assert.False(RecruitmentApplicationStageCatalog.TryParse(stage, out _));
    }
    [Fact]
    public void Generic_profile_edits_cannot_change_recruitment_stage()
    {
        Assert.False(RecruitmentPipelineRules.RequiresCanonicalStageCommand(ActivityType.Shortlisted, ActivityType.Shortlisted));
        Assert.True(RecruitmentPipelineRules.RequiresCanonicalStageCommand(ActivityType.Shortlisted, ActivityType.Rejected));
        Assert.True(RecruitmentPipelineRules.RequiresCanonicalStageCommand(ActivityType.Shortlisted, ActivityType.Selected));
    }
}