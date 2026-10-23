using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests.Recruitments;

public sealed class RecruitmentConfigurableScoringEngineTests
{
    private static IHttpContextAccessor MockTenantContext(string companyId, string userId = "test-user")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim("company_id", companyId),
            new System.Security.Claims.Claim("user_id", userId),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
        ], "TestAuth"));
        return new HttpContextAccessor { HttpContext = httpContext };
    }

    [Fact]
    public async Task PlanService_Rejects_Invalid_Scale_Where_Min_Is_Not_Less_Than_Max()
    {
        var plans = new Mock<IMongoDbRepository<RecruitmentInterviewPlan>>();
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var service = new RecruitmentInterviewPlanService(plans.Object, vacancies.Object, MockTenantContext("c-1"));

        var command = new RecruitmentInterviewPlanCommand
        {
            Name = "Bad Scale Plan",
            IdempotencyKey = "key-1",
            RatingScale = new RecruitmentRatingScaleConfig
            {
                MinRating = 10,
                MaxRating = 5, // Invalid: min > max
                Step = 1
            },
            Stages = [new RecruitmentInterviewPlanStageCommand { Order = 1, Code = "r1", Name = "Round 1" }]
        };

        var result = await service.Upsert(command, "user-1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Minimum rating must be strictly less than maximum rating", result.Message);
    }

    [Fact]
    public async Task PlanService_Rejects_Duplicate_Qualitative_Labels()
    {
        var plans = new Mock<IMongoDbRepository<RecruitmentInterviewPlan>>();
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var service = new RecruitmentInterviewPlanService(plans.Object, vacancies.Object, MockTenantContext("c-1"));

        var command = new RecruitmentInterviewPlanCommand
        {
            Name = "Duplicate Label Plan",
            IdempotencyKey = "key-2",
            RatingScale = new RecruitmentRatingScaleConfig
            {
                MinRating = 1,
                MaxRating = 5,
                Step = 1,
                Levels =
                [
                    new() { Label = "Good", Value = 3 },
                    new() { Label = "good", Value = 4 } // Duplicate case-insensitive
                ]
            },
            Stages = [new RecruitmentInterviewPlanStageCommand { Order = 1, Code = "r1", Name = "Round 1" }]
        };

        var result = await service.Upsert(command, "user-1");

        Assert.False(result.Success);
        Assert.Contains("Qualitative rating labels must be unique", result.Message);
    }

    [Fact]
    public async Task PlanService_Rejects_Competency_Weights_That_Do_Not_Total_100()
    {
        var plans = new Mock<IMongoDbRepository<RecruitmentInterviewPlan>>();
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var service = new RecruitmentInterviewPlanService(plans.Object, vacancies.Object, MockTenantContext("c-1"));

        var command = new RecruitmentInterviewPlanCommand
        {
            Name = "Weighted Plan",
            IdempotencyKey = "key-3",
            Stages = [new RecruitmentInterviewPlanStageCommand { Order = 1, Code = "r1", Name = "Round 1" }],
            Competencies =
            [
                new() { Code = "c1", Name = "Comp 1", Weight = 40m },
                new() { Code = "c2", Name = "Comp 2", Weight = 30m } // Sum = 70%, invalid!
            ]
        };

        var result = await service.Upsert(command, "user-1");

        Assert.False(result.Success);
        Assert.Contains("sum of all weights must equal 100%", result.Message);
    }

    [Fact]
    public void Compute_With1To10Scale_NormalizesCorrectly()
    {
        var plan = new RecruitmentInterviewPlan
        {
            RatingScale = new RecruitmentRatingScaleConfig
            {
                ScaleType = "Numeric1To10",
                MinRating = 1m,
                MaxRating = 10m,
                Step = 0.5m,
                NormalizationMethod = "PercentageOfMax"
            },
            HiringTargetScore = 75m,
            Stages = [new RecruitmentInterviewPlanStage { Order = 1, Code = "tech", Name = "Technical Round" }]
        };

        var scorecards = new List<RecruitmentScorecard>
        {
            new()
            {
                ScorecardId = "sc-10",
                RoundNumber = 1,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Hire",
                Criteria =
                [
                    new() { Name = "Coding", Rating = 8m }, // 8 / 10 = 80%
                    new() { Name = "System Design", Rating = 9m } // 9 / 10 = 90%
                ]
            }
        };

        var result = RecruitmentInterviewScoreCalculator.Compute(scorecards, null, plan);

        Assert.Equal(85, result.FinalInterviewScore);
        Assert.Equal(8.5m, result.FinalInterviewRating);
        Assert.True(result.MeetsHiringTarget);
        Assert.True(result.PassedMandatoryGates);
        Assert.Equal("Hire", result.FinalInterviewRecommendation);
    }

    [Fact]
    public void Compute_MandatoryCompetencyFailure_ForcesNoHire_EvenWithHighScore()
    {
        var plan = new RecruitmentInterviewPlan
        {
            RatingScale = new RecruitmentRatingScaleConfig { MinRating = 1, MaxRating = 5 },
            HiringTargetScore = 60m,
            Competencies =
            [
                new() { Code = "ARCH", Name = "Architecture", Weight = 50m, IsMandatory = true, MinScore = 70m },
                new() { Code = "TEAM", Name = "Teamwork", Weight = 50m, IsMandatory = false }
            ],
            Stages = [new RecruitmentInterviewPlanStage { Order = 1, Code = "round-1", Name = "Panel" }]
        };

        var scorecards = new List<RecruitmentScorecard>
        {
            new()
            {
                ScorecardId = "sc-gate-1",
                RoundNumber = 1,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Strong Hire", // Reviewer gave Strong Hire
                Criteria =
                [
                    // Architecture scored 2/5 = 40% (fails mandatory min 70%)
                    new() { Code = "ARCH", Name = "Architecture", Rating = 2m },
                    // Teamwork scored 5/5 = 100%
                    new() { Code = "TEAM", Name = "Teamwork", Rating = 5m }
                ]
            }
        };

        var result = RecruitmentInterviewScoreCalculator.Compute(scorecards, null, plan);

        // Overall score is (40 + 100) / 2 = 70%, which is >= target 60%
        Assert.Equal(70, result.FinalInterviewScore);
        Assert.False(result.PassedMandatoryGates);
        Assert.NotEmpty(result.MandatoryGateFailures);
        Assert.Contains("Architecture", result.MandatoryGateFailures[0]);
        // Critical requirement: Mandatory gate failure MUST block Hire / Strong Hire
        Assert.Equal("No Hire", result.FinalInterviewRecommendation);
    }

    [Fact]
    public void Compute_MandatoryRoundThresholdFailure_ForcesNoHire()
    {
        var plan = new RecruitmentInterviewPlan
        {
            RatingScale = new RecruitmentRatingScaleConfig { MinRating = 1, MaxRating = 5 },
            HiringTargetScore = 60m,
            Stages =
            [
                new() { Order = 1, Code = "tech-1", Name = "Technical 1", IsMandatory = true, PassThreshold = 75m },
                new() { Order = 2, Code = "tech-2", Name = "Technical 2", IsMandatory = false, PassThreshold = 50m }
            ]
        };

        var scorecards = new List<RecruitmentScorecard>
        {
            // Round 1 Technical 1: scored 3/5 = 60%, fails threshold 75%
            new()
            {
                ScorecardId = "sc-r1",
                RoundNumber = 1,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Hire",
                Criteria = [new() { Name = "Algorithms", Rating = 3m }]
            },
            // Round 2 Technical 2: scored 5/5 = 100%, passes threshold 50%
            new()
            {
                ScorecardId = "sc-r2",
                RoundNumber = 2,
                Status = RecruitmentScorecardStatus.Submitted,
                Recommendation = "Strong Hire",
                Criteria = [new() { Name = "Domain", Rating = 5m }]
            }
        };

        var result = RecruitmentInterviewScoreCalculator.Compute(scorecards, null, plan);

        Assert.False(result.PassedMandatoryGates);
        Assert.Single(result.MandatoryGateFailures);
        Assert.Contains("Technical 1", result.MandatoryGateFailures[0]);
        Assert.Equal("No Hire", result.FinalInterviewRecommendation);
    }

    [Fact]
    public void Compute_QuorumMet_IdentifiesInsufficientPanelSubmissions()
    {
        var plan = new RecruitmentInterviewPlan
        {
            Stages =
            [
                new() { Order = 1, Code = "panel", Name = "Executive Panel", RequiredPanelSize = 3 }
            ]
        };

        var scorecards = new List<RecruitmentScorecard>
        {
            // Only 1 scorecard submitted so far, quorum requires 3
            new()
            {
                ScorecardId = "sc-panel-1",
                RoundNumber = 1,
                Status = RecruitmentScorecardStatus.Submitted,
                Criteria = [new() { Name = "Leadership", Rating = 4m }]
            }
        };

        var result = RecruitmentInterviewScoreCalculator.Compute(scorecards, null, plan);

        Assert.Single(result.RoundBreakdowns);
        var round = result.RoundBreakdowns[0];
        Assert.Equal(3, round.RequiredPanelSize);
        Assert.Equal(1, round.ScorecardsCount);
        Assert.False(round.QuorumMet);
    }

    [Fact]
    public async Task ScorecardService_EnforcesCustomRatingScaleRange()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { ApplicantId = "a-custom", CompanyId = "c-1", VacancyId = "v-custom", Phone = "1234567890" });

        var plans = new Mock<IMongoDbRepository<RecruitmentInterviewPlan>>();
        // Plan has 1-10 scale
        plans.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterviewPlan, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentInterviewPlan
            {
                CompanyId = "c-1",
                VacancyId = "v-custom",
                RatingScale = new RecruitmentRatingScaleConfig
                {
                    ScaleType = "Numeric1To10",
                    MinRating = 1m,
                    MaxRating = 10m
                }
            });

        var scorecards = new Mock<IMongoDbRepository<RecruitmentScorecard>>();
        scorecards.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentScorecard, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentScorecard?)null);
        scorecards.Setup(x => x.AddOne(It.IsAny<RecruitmentScorecard>())).ReturnsAsync(new Result { Success = true });

        var service = new RecruitmentScorecardService(
            applicants.Object,
            scorecards.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            MockTenantContext("c-1"),
            null,
            plans.Object);

        // Rating 8.5 is valid on 1-10 scale (would have failed on legacy 1-5 scale)
        var validCmd = new RecruitmentScorecardCommand
        {
            ApplicantId = "a-custom",
            VacancyId = "v-custom",
            ReviewerUserId = "rev-1",
            IdempotencyKey = "cmd-1",
            Status = RecruitmentScorecardStatus.Submitted,
            Criteria = [new RecruitmentScorecardCriterionCommand { Name = "Advanced Coding", Rating = 8.5m }]
        };

        var validResult = await service.Upsert(validCmd, "rev-1");
        Assert.True(validResult.Success);

        // Rating 12 exceeds max of 10 -> must be rejected
        var invalidCmd = new RecruitmentScorecardCommand
        {
            ApplicantId = "a-custom",
            VacancyId = "v-custom",
            ReviewerUserId = "rev-1",
            IdempotencyKey = "cmd-2",
            Status = RecruitmentScorecardStatus.Submitted,
            Criteria = [new RecruitmentScorecardCriterionCommand { Name = "Advanced Coding", Rating = 12m }]
        };

        var invalidResult = await service.Upsert(invalidCmd, "rev-1");
        Assert.False(invalidResult.Success);
        Assert.Equal(RecruitmentErrorCodes.ScorecardInvalid, invalidResult.ErrorCode);
    }
}
