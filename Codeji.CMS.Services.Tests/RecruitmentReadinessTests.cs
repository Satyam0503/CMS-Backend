using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentReadinessTests
{
    [Fact]
    public void New_tenant_keeps_safe_utc_fallback_but_requires_explicit_timezone_review()
    {
        var readiness = RecruitmentEnterpriseService.BuildReadiness(null);

        Assert.Equal("UTC", readiness.Timezone);
        Assert.False(readiness.Required.Single(item => item.Code == "timezone").Complete);
        Assert.False(readiness.ReadyToPublishJobs);
        Assert.True(readiness.Recommended.Single(item => item.Code == "sla").Complete);
        Assert.True(readiness.Recommended.Single(item => item.Code == "automation").Complete);
    }

    [Fact]
    public void Explicit_complete_company_setup_is_ready_to_publish()
    {
        var readiness = RecruitmentEnterpriseService.BuildReadiness(new RecruitmentEnterpriseConfig
        {
            RecruitmentMode = "Standard",
            DefaultTimezone = "Asia/Kolkata",
            Locations = ["Bengaluru"],
            ScreeningCriteria = ["Required skills"],
            PreboardingChecklist = ["documents"],
            EnableSla = false,
            EnableAutomation = false,
            OfferApproval = true,
            Scorecards = true
        });

        Assert.True(readiness.ReadyToPublishJobs);
        Assert.All(readiness.Required, item => Assert.True(item.Complete));
        Assert.Equal(100, readiness.CompletionPercent);
    }
}