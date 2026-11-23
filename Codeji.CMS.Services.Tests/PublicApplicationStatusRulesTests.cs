using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public sealed class PublicApplicationStatusRulesTests
{
    [Fact]
    public void ApplicantLookupFilter_RequiresPublicActiveNonAnonymizedRecordAndExactCaseInsensitiveEmail()
    {
        var filter = PublicApplicationStatusRules.BuildApplicantMatchFilter("candidate-id", "person+ats@example.com").Render(
            new RenderArgs<Applicant>(
                BsonSerializer.LookupSerializer<Applicant>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: true,
                renderForElemMatch: false,
                translationOptions: null));
        var filterText = filter.ToString();

        Assert.Contains("candidate-id", filterText);
        Assert.Contains("ApplicationSubmissionIdempotencyKey", filterText);
        Assert.Contains("IsDeleted", filterText);
        Assert.Contains("Anonymized", filterText);
        var emailPattern = filter["Email"].AsBsonRegularExpression;
        Assert.Equal(@"^person\+ats@example\.com$", emailPattern.Pattern);
        Assert.Equal("i", emailPattern.Options);
    }

    [Theory]
    [InlineData("New", "In review")]
    [InlineData("Screening", "In review")]
    [InlineData("Technical Interview", "Interview process")]
    [InlineData("OfferPending", "Offer stage")]
    [InlineData("Offer approval", "Offer stage")]
    [InlineData("OfferDeclined", "Offer declined")]
    [InlineData("ApplicationWithdrawn", "Application withdrawn")]
    [InlineData("Selected", "Offer stage")]
    [InlineData("Preboarding", "Hired")]
    [InlineData("Hired", "Hired")]
    [InlineData("Rejected", "Not selected")]
    [InlineData("NotSelected", "Not selected")]
    [InlineData("No Hire", "Not selected")]
    [InlineData("RejectionReview", "In review")]
    [InlineData("HiringManagerReview", "In review")]
    [InlineData("custom confidential internal stage", "In review")]
    [InlineData(null, "In review")]
    public void MapStage_UsesCandidateSafeBroadStatuses(string? stage, string expected)
    {
        Assert.Equal(expected, PublicApplicationStatusRules.MapStage(stage));
    }
}
