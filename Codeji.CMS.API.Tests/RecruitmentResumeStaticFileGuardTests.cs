using Codeji.CMS.API.App_Start;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Codeji.CMS.API.Tests;

public sealed class RecruitmentResumeStaticFileGuardTests
{
    [Theory]
    [InlineData("/fs/Resume", true)]
    [InlineData("/fs/resume/candidate.pdf", true)]
    [InlineData("/fs/Resume%2Fcandidate.pdf", true)]
    [InlineData("/fs/Resume%5Ccandidate.pdf", true)]
    [InlineData("/fs/Resume%252Fcandidate.pdf", true)]
    [InlineData("/fs%2FResume/candidate.pdf", true)]
    [InlineData("/fs//Resume/candidate.pdf", true)]
    [InlineData("/fs/other/../Resume/candidate.pdf", true)]
    [InlineData("/fs/other/%2e%2e/Resume/candidate.pdf", true)]
    [InlineData("/fs/other/%252e%252e%255cResume%255ccandidate.pdf", true)]
    [InlineData("/fs/ResumeBackup/../Resume/candidate.pdf", true)]
    [InlineData("/fs/ResumeBackup/candidate.pdf", false)]
    [InlineData("/fs/ResumeBackup/../CompanyLogo/logo.png", false)]
    [InlineData("/fs/CompanyLogo/logo.png", false)]
    [InlineData("/api/Applicants/Resume/candidate-1", false)]
    public void Only_public_static_resume_paths_are_blocked(string requestPath, bool expected)
    {
        Assert.Equal(expected, RecruitmentResumeStaticFileGuard.IsPrivateResumePath(new PathString(requestPath)));
    }
}
