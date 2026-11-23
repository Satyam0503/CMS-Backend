using Codeji.CMS.Services.Recruitments;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentResumeOrphanCleanupTests
{
    [Fact]
    public void Cleanup_only_selects_pdf_files_older_than_the_safety_window()
    {
        var nowUtc = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
        var minimumAge = TimeSpan.FromHours(24);

        Assert.True(RecruitmentResumeOrphanCleanupRules.ShouldClean("orphan.pdf", nowUtc.AddDays(-2), nowUtc, minimumAge));
        Assert.False(RecruitmentResumeOrphanCleanupRules.ShouldClean("recent.pdf", nowUtc.AddHours(-23), nowUtc, minimumAge));
        Assert.False(RecruitmentResumeOrphanCleanupRules.ShouldClean("document.docx", nowUtc.AddDays(-2), nowUtc, minimumAge));
        Assert.False(RecruitmentResumeOrphanCleanupRules.ShouldClean("future.pdf", nowUtc.AddMinutes(1), nowUtc, minimumAge));
        Assert.False(RecruitmentResumeOrphanCleanupRules.ShouldClean("orphan.pdf", nowUtc.AddDays(-2), nowUtc, TimeSpan.Zero));
    }
}
