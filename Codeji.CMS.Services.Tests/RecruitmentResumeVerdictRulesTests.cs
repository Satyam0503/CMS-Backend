using Codeji.CMS.Services.Recruitments;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentResumeVerdictRulesTests
{
    [Fact]
    public void Clean_verdict_is_reused_only_for_same_file_and_recent_scan()
    {
        var now = new DateTime(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);
        var hash = new string('A', 64);

        Assert.True(RecruitmentResumeVerdictRules.IsCachedClean("Clean", hash, hash.ToLowerInvariant(), now.AddHours(-23), now));
        Assert.False(RecruitmentResumeVerdictRules.IsCachedClean("Clean", hash, new string('B', 64), now, now));
        Assert.False(RecruitmentResumeVerdictRules.IsCachedClean("Clean", hash, hash, now.AddHours(-25), now));
        Assert.False(RecruitmentResumeVerdictRules.IsCachedClean("Infected", hash, hash, now, now));
    }

    [Fact]
    public void Infected_verdict_only_blocks_the_exact_scanned_file()
    {
        var hash = new string('A', 64);

        Assert.True(RecruitmentResumeVerdictRules.IsCachedInfected("Infected", hash, hash.ToLowerInvariant()));
        Assert.False(RecruitmentResumeVerdictRules.IsCachedInfected("Infected", hash, new string('B', 64)));
        Assert.False(RecruitmentResumeVerdictRules.IsCachedInfected("Clean", hash, hash));
    }
}
