using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.DTO.PublicCareers;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentPublicApplicationReadinessTests
{
    [Theory]
    [InlineData("9876543210", true)]
    [InlineData("+1 (415) 555-0132", true)]
    [InlineData("0044 20 7946 0018", true)]
    [InlineData("1234", false)]
    [InlineData("not a phone number", false)]
    [InlineData("+1234567890123456", false)]
    [InlineData("++14155550132", false)]
    public void Candidate_phone_validation_accepts_international_format_but_limits_digit_count(string phone, bool expected)
    {
        Assert.Equal(expected, PublicCareerApplicationRules.IsValidCandidatePhone(phone));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(80, true)]
    [InlineData(-0.1, false)]
    [InlineData(80.1, false)]
    public void Candidate_experience_is_bounded_to_plausible_year_values(decimal years, bool expected) =>
        Assert.Equal(expected, PublicCareerApplicationRules.IsValidCandidateExperience(years));

    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(3650, true)]
    [InlineData(-1, false)]
    [InlineData(3651, false)]
    public void Candidate_notice_period_is_bounded_when_provided(int? days, bool expected) =>
        Assert.Equal(expected, PublicCareerApplicationRules.IsValidCandidateNoticePeriod(days));

    [Theory]
    [InlineData(true, "Internal", "Privacy notice", "2026-01", true)]
    [InlineData(true, "Internal", null, "2026-01", false)]
    [InlineData(true, "Internal", "Privacy notice", null, false)]
    [InlineData(true, "External", null, null, true)]
    [InlineData(false, "External", null, null, false)]
    public void Candidate_application_readiness_matches_listing_and_detail_gates(
        bool postingIsOpen,
        string applicationMode,
        string? notice,
        string? noticeVersion,
        bool expected)
    {
        Assert.Equal(expected, PublicCareerApplicationRules.CanAcceptApplications(
            postingIsOpen,
            applicationMode,
            notice,
            noticeVersion));
    }

    [Fact]
    public void Public_application_rejects_oversized_email_before_persistence()
    {
        var request = ValidApplication(email: $"{new string('a', 315)}@x.co");
        Assert.Null(PublicCareerApplicationRules.ValidateApplicationRequest(request));

        var oversized = ValidApplication(email: $"{new string('a', 316)}@x.co");
        Assert.Equal("A valid email address is required.", PublicCareerApplicationRules.ValidateApplicationRequest(oversized));
    }

    [Fact]
    public void Public_application_rejects_oversized_profile_urls_before_uri_parsing()
    {
        var atLimit = ValidApplication(linkedInUrl: $"https://example.com/{new string('a', 2028)}");
        Assert.Null(PublicCareerApplicationRules.ValidateApplicationRequest(atLimit));

        var tooLong = ValidApplication(linkedInUrl: $"https://example.com/{new string('a', 2029)}");
        Assert.Contains("2048", PublicCareerApplicationRules.ValidateApplicationRequest(tooLong));
    }

    [Fact]
    public void Public_application_rejects_region_values_over_configured_limit()
    {
        Assert.Null(PublicCareerApplicationRules.ValidateApplicationRequest(ValidApplication(state: new string('x', 120))));
        var request = ValidApplication(state: new string('x', 121));
        Assert.Contains("allowed length", PublicCareerApplicationRules.ValidateApplicationRequest(request));
    }

    [Fact]
    public void Public_application_rejects_oversized_attribution_before_database_access()
    {
        var request = ValidApplication(attributionSource: new string('x', 201));
        Assert.Contains("allowed length", PublicCareerApplicationRules.ValidateApplicationRequest(request));
    }

    private static PublicJobApplicationRequest ValidApplication(
        string email = "candidate@example.com",
        string? linkedInUrl = null,
        string? state = "Uttarakhand",
        string? attributionSource = null) => new()
    {
        IdempotencyKey = "candidate-application-123",
        FirstName = "Test",
        LastName = "Candidate",
        Email = email,
        Phone = "+1 (415) 555-0132",
        State = state,
        Experience = 4,
        LinkedInUrl = linkedInUrl,
        AttributionSource = attributionSource
    };
}
