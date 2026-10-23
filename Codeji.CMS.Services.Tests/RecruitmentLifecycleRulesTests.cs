using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Utility.Enums;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentLifecycleRulesTests
{
    [Fact]
    public void Interview_schedule_requires_utc_end_after_start()
    {
        var start = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
        Assert.True(RecruitmentInterviewRules.IsValidSchedule(start, start.AddMinutes(30)));
        Assert.False(RecruitmentInterviewRules.IsValidSchedule(start, start.AddMinutes(-1)));
        Assert.False(RecruitmentInterviewRules.IsValidSchedule(DateTime.SpecifyKind(start, DateTimeKind.Local), start.AddMinutes(30)));
    }

    [Fact]
    public void Interview_and_offer_transitions_are_server_authoritative()
    {
        Assert.True(RecruitmentInterviewRules.IsAllowedTransition(RecruitmentInterviewStatus.Draft, RecruitmentInterviewStatus.Scheduled));
        Assert.False(RecruitmentInterviewRules.IsAllowedTransition(RecruitmentInterviewStatus.Completed, RecruitmentInterviewStatus.Scheduled));
        Assert.True(RecruitmentOfferRules.IsAllowedTransition(RecruitmentOfferStatus.Approved, RecruitmentOfferStatus.Sent));
        Assert.False(RecruitmentOfferRules.IsAllowedTransition(RecruitmentOfferStatus.Draft, RecruitmentOfferStatus.Sent));
    }

    [Fact]
    public void Offer_compensation_rejects_missing_currency_or_negative_salary()
    {
        Assert.True(RecruitmentOfferRules.IsValidCompensation(100m, "INR"));
        Assert.False(RecruitmentOfferRules.IsValidCompensation(-1m, "INR"));
        Assert.False(RecruitmentOfferRules.IsValidCompensation(100m, ""));
    }

    [Fact]
    public void Legacy_pipeline_snapshot_is_stable_and_parses_without_empty_stages()
    {
        Assert.Equal("New|InProgress|OnHold|Shortlisted|Selected|Rejected|ReApply", RecruitmentWorkflowSnapshot.LegacyV1);
        var stages = RecruitmentPipelineRules.ParseSnapshot($"  {RecruitmentWorkflowSnapshot.LegacyV1} | ");
        Assert.Equal(new[] { "New", "InProgress", "OnHold", "Shortlisted", "Selected", "Rejected", "ReApply" }, stages);
    }

    [Fact]
    public void Pipeline_rules_reject_backward_moves_and_terminal_changes()
    {
        Assert.True(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Shortlisted, ActivityType.OnHold, out var holdError));
        Assert.True(string.IsNullOrWhiteSpace(holdError));
        Assert.False(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Shortlisted, ActivityType.InProgress, out var backwardError));
        Assert.Contains("back", backwardError, StringComparison.OrdinalIgnoreCase);
        Assert.False(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Selected, ActivityType.Rejected, out var terminalError));
        Assert.Contains("cannot", terminalError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Shortlisted_candidate_can_transition_to_rejected_directly()
    {
        Assert.True(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Shortlisted, ActivityType.Rejected, out var error));
        Assert.True(string.IsNullOrWhiteSpace(error));

        Assert.True(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.InProgress, ActivityType.Rejected, out var error2));
        Assert.True(string.IsNullOrWhiteSpace(error2));

        Assert.True(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.OnHold, ActivityType.Rejected, out var error3));
        Assert.True(string.IsNullOrWhiteSpace(error3));

        // Once rejected, terminal state cannot transition to any other stage
        Assert.False(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Rejected, ActivityType.Shortlisted, out var error4));
        Assert.Contains("cannot be changed after it is Rejected", error4, StringComparison.OrdinalIgnoreCase);
    }
}
