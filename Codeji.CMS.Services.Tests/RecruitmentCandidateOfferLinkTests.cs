using Codeji.CMS.Services.Recruitments;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentCandidateOfferLinkTests
{
    [Fact]
    public void Builds_encoded_candidate_offer_url_for_https_portal()
    {
        var url = RecruitmentCandidateOfferLink.Build("https://careers.example.com/", "token+with/slash");

        Assert.Equal("https://careers.example.com/candidate/offer?token=token%2Bwith%2Fslash", url);
    }

    [Fact]
    public void Allows_http_only_for_loopback_development_urls()
    {
        Assert.Equal("http://localhost:5173/candidate/offer?token=abc", RecruitmentCandidateOfferLink.Build("http://localhost:5173/", "abc"));
        Assert.Null(RecruitmentCandidateOfferLink.Build("http://careers.example.com", "abc"));
    }

    [Theory]
    [InlineData(null, "abc")]
    [InlineData("javascript:alert(1)", "abc")]
    [InlineData("https://careers.example.com", "")]
    [InlineData("https://careers.example.com/?next=https://evil.example", "abc")]
    [InlineData("https://user:pass@careers.example.com", "abc")]
    public void Rejects_invalid_roots_or_missing_token(string? root, string? token)
    {
        Assert.Null(RecruitmentCandidateOfferLink.Build(root, token));
    }
}
