using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;

namespace Codeji.CMS.Services.Tests.Recruitments;

public sealed class RecruitmentInterviewScoreCalculatorTests
{
    [Fact]
    public void Compute_WithTechnicalAndHrRounds_ExcludesHrRoundFromScore()
    {
        // Arrange
        var interviews = new List<RecruitmentInterview>
        {
            new() { InterviewId = "int-1", Title = "Round 1 - Technical System Design", RoundNumber = 1, RoundType = "Technical" },
            new() { InterviewId = "int-2", Title = "Round 2 - Coding & Algorithms", RoundNumber = 2, RoundType = "Technical" },
            new() { InterviewId = "int-3", Title = "Round 3 - HR Salary & Offer Discussion", RoundNumber = 3, RoundType = "HR" }
        };

        var scorecards = new List<RecruitmentScorecard>
        {
            // Round 1 Technical: 4/5, 4/5 -> avg 4.0 (80%)
            new()
            {
                ScorecardId = "sc-1",
                InterviewId = "int-1",
                RoundNumber = 1,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Hire",
                Criteria =
                [
                    new() { Name = "Architecture", Rating = 4 },
                    new() { Name = "Scalability", Rating = 4 }
                ]
            },
            // Round 2 Technical: 5/5, 5/5 -> avg 5.0 (100%)
            new()
            {
                ScorecardId = "sc-2",
                InterviewId = "int-2",
                RoundNumber = 2,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Strong Hire",
                Criteria =
                [
                    new() { Name = "Data Structures", Rating = 5 },
                    new() { Name = "Clean Code", Rating = 5 }
                ]
            },
            // Round 3 HR: 2/5 (e.g. low score due to strict CTC negotiation) -> should NOT skew technical score
            new()
            {
                ScorecardId = "sc-3",
                InterviewId = "int-3",
                RoundNumber = 3,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Hold",
                Criteria =
                [
                    new() { Name = "Compensation Expectations", Rating = 2 }
                ]
            }
        };

        // Act
        var result = RecruitmentInterviewScoreCalculator.Compute(scorecards, interviews);

        // Assert
        // Expected: only sc-1 (ratings 4,4) and sc-2 (ratings 5,5) are included.
        // Overall avg rating = (4 + 4 + 5 + 5) / 4 = 4.5
        // Final score = (4.5 / 5.0) * 100 = 90%
        Assert.True(result.ExcludesHrRound);
        Assert.Equal(90, result.FinalInterviewScore);
        Assert.Equal(4.5m, result.FinalInterviewRating);
        Assert.Equal(2, result.EvaluatedRoundsCount);
        Assert.Equal(2, result.TotalScorecardsCount);
        Assert.Contains(1, result.EvaluatedRoundNumbers);
        Assert.Contains(2, result.EvaluatedRoundNumbers);
        Assert.DoesNotContain(3, result.EvaluatedRoundNumbers);
        Assert.Equal("Strong Hire", result.FinalInterviewRecommendation);
    }

    [Fact]
    public void Compute_WithOnlyHrRound_FallsBackGracefully()
    {
        // Non-technical role with only HR interview
        var interviews = new List<RecruitmentInterview>
        {
            new() { InterviewId = "int-hr", Title = "HR Screening & Culture Interview", RoundNumber = 1, RoundType = "HR" }
        };

        var scorecards = new List<RecruitmentScorecard>
        {
            new()
            {
                ScorecardId = "sc-hr",
                InterviewId = "int-hr",
                RoundNumber = 1,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Hire",
                Criteria = [new() { Name = "Communication", Rating = 4 }]
            }
        };

        var result = RecruitmentInterviewScoreCalculator.Compute(scorecards, interviews);

        Assert.False(result.ExcludesHrRound);
        Assert.Equal(80, result.FinalInterviewScore);
        Assert.Equal(4.0m, result.FinalInterviewRating);
        Assert.Equal("Hire", result.FinalInterviewRecommendation);
    }

    [Fact]
    public void Compute_IgnoresDraftScorecards()
    {
        var interviews = new List<RecruitmentInterview>
        {
            new() { InterviewId = "int-1", Title = "Technical Round", RoundNumber = 1, RoundType = "Technical" }
        };

        var scorecards = new List<RecruitmentScorecard>
        {
            new()
            {
                ScorecardId = "sc-draft",
                InterviewId = "int-1",
                RoundNumber = 1,
                Status = RecruitmentScorecardStatus.Draft, // Still being filled
                Criteria = [new() { Name = "Draft Criteria", Rating = 1 }]
            }
        };

        var result = RecruitmentInterviewScoreCalculator.Compute(scorecards, interviews);

        Assert.Equal(0, result.FinalInterviewScore);
        Assert.Equal(0, result.EvaluatedRoundsCount);
        Assert.Equal("Pending", result.FinalInterviewRecommendation);
    }

    [Fact]
    public void Compute_WithStrongNoHire_RecommendsNoHire()
    {
        var interviews = new List<RecruitmentInterview>
        {
            new() { InterviewId = "int-1", Title = "Coding Round", RoundNumber = 1, RoundType = "Technical" }
        };

        var scorecards = new List<RecruitmentScorecard>
        {
            new()
            {
                ScorecardId = "sc-1",
                InterviewId = "int-1",
                RoundNumber = 1,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Strong No Hire", // Must NOT be counted as Strong Hire!
                Criteria = [new() { Name = "Coding", Rating = 1 }]
            }
        };

        var result = RecruitmentInterviewScoreCalculator.Compute(scorecards, interviews);

        Assert.Equal("No Hire", result.FinalInterviewRecommendation);
        Assert.Equal(20, result.FinalInterviewScore);
        Assert.Equal(1.0m, result.FinalInterviewRating);
    }

    [Fact]
    public void Compute_WithTechnicalTitleContainingHrLetters_DoesNotExcludeTechnicalRound()
    {
        // "Synchronous" contains "hr", but is NOT an HR round
        var interviews = new List<RecruitmentInterview>
        {
            new() { InterviewId = "int-1", Title = "Synchronous Microservices Round", RoundNumber = 1, RoundType = "Technical" }
        };

        var scorecards = new List<RecruitmentScorecard>
        {
            new()
            {
                ScorecardId = "sc-1",
                InterviewId = "int-1",
                RoundNumber = 1,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Hire",
                Criteria = [new() { Name = "System Design", Rating = 4 }]
            }
        };

        var result = RecruitmentInterviewScoreCalculator.Compute(scorecards, interviews);

        Assert.False(result.ExcludesHrRound);
        Assert.Equal(1, result.EvaluatedRoundsCount);
        Assert.Equal(80, result.FinalInterviewScore);
        Assert.Equal("Hire", result.FinalInterviewRecommendation);
    }
}

