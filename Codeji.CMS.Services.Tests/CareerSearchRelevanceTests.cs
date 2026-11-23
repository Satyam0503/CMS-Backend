using Codeji.CMS.Services.Recruitments;
using MongoDB.Bson;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class CareerSearchRelevanceTests
{
    [Fact]
    public void RelevanceScoreStage_PrioritizesTitleSkillsAndCompanyMatches()
    {
        var stage = PublicCareerService.BuildRelevanceScoreStage("React developer", ["company-1"]);
        var json = stage.ToJson();

        Assert.Contains("__careerRelevanceScore", json, StringComparison.Ordinal);
        Assert.Contains("RequiredSkills", json, StringComparison.Ordinal);
        Assert.Contains("Skills", json, StringComparison.Ordinal);
        Assert.Contains("CompanyId", json, StringComparison.Ordinal);
        Assert.Contains("100", json, StringComparison.Ordinal);
        Assert.Contains("50", json, StringComparison.Ordinal);
        Assert.Contains("14", json, StringComparison.Ordinal);
    }

    [Fact]
    public void RelevanceScoreStage_EscapesSearchTermsAndAllowsNoCompanyMatches()
    {
        var stage = PublicCareerService.BuildRelevanceScoreStage("C++", []);
        var scores = stage["$addFields"]["__careerRelevanceScore"]["$add"].AsBsonArray;
        var titleRegex = scores[0]["$cond"].AsBsonArray[0]["$regexMatch"]["regex"].AsString;
        var companyIds = scores[2]["$cond"].AsBsonArray[0]["$in"].AsBsonArray[1].AsBsonArray;

        Assert.Equal(@"C\+\+", titleRegex);
        Assert.Empty(companyIds);
    }

    [Fact]
    public void RelevanceScoreProjection_RemovesTheTemporaryFieldBeforeTypedDeserialization()
    {
        var projection = PublicCareerService.BuildRelevanceScoreProjectionStage();

        Assert.Equal(0, projection["$project"]["__careerRelevanceScore"].ToInt32());
    }
}
