using System.Text.RegularExpressions;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentFinalScoreResult
{
    public int FinalInterviewScore { get; set; } // 0 - 100%
    public decimal FinalInterviewRating { get; set; } // Scaled rating (1.00 - 5.00 or custom scale)
    public string FinalInterviewRecommendation { get; set; } = "Hire";
    public int EvaluatedRoundsCount { get; set; }
    public int TotalScorecardsCount { get; set; }
    public bool ExcludesHrRound { get; set; }
    public List<int> EvaluatedRoundNumbers { get; set; } = [];

    // Explainable calculation metadata
    public bool PassedMandatoryGates { get; set; } = true;
    public List<string> MandatoryGateFailures { get; set; } = [];
    public decimal TargetScore { get; set; } = 70m;
    public bool MeetsHiringTarget { get; set; } = true;
    public string ScaleType { get; set; } = "Numeric1To5";
    public string NormalizationFormula { get; set; } = "(Rating / Max) * 100";
    public List<RecruitmentRoundScoreBreakdown> RoundBreakdowns { get; set; } = [];
    public List<RecruitmentCompetencyScoreBreakdown> CompetencyBreakdowns { get; set; } = [];
}

public sealed class RecruitmentRoundScoreBreakdown
{
    public int RoundNumber { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string? StageType { get; set; }
    public decimal NormalizedScore { get; set; }
    public decimal RawRating { get; set; }
    public decimal? Weight { get; set; }
    public decimal? PassThreshold { get; set; }
    public bool PassedThreshold { get; set; } = true;
    public bool IsMandatory { get; set; }
    public int ScorecardsCount { get; set; }
    public int RequiredPanelSize { get; set; } = 1;
    public bool QuorumMet { get; set; } = true;
    public string ConsensusRecommendation { get; set; } = "Hire";
}

public sealed class RecruitmentCompetencyScoreBreakdown
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal NormalizedScore { get; set; }
    public decimal RawRating { get; set; }
    public decimal? Weight { get; set; }
    public decimal? MinScore { get; set; }
    public bool IsMandatory { get; set; }
    public bool Passed { get; set; } = true;
    public int RatingCount { get; set; }
}

/// <summary>
/// Authoritative calculation engine for applicant interview benchmarks.
/// Supports configurable rating scales (1-5, 1-10, 0-100, custom decimal, qualitative),
/// weighted competencies, round-level thresholds, mandatory gates, and panel quorum validation.
/// Maintains full backward compatibility with legacy 1-5 scorecards.
/// </summary>
public static class RecruitmentInterviewScoreCalculator
{
    private static readonly Regex HrTitleRegex = new(
        @"\b(HR|Human\s+Resources?|Salary|Negotiation|Offer\s+Discussion|Compensation)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsHrRound(RecruitmentScorecard scorecard, IEnumerable<RecruitmentInterview>? interviews)
    {
        if (interviews == null) return false;

        RecruitmentInterview? interview = null;
        if (!string.IsNullOrEmpty(scorecard.InterviewId))
        {
            interview = interviews.FirstOrDefault(i => i.InterviewId == scorecard.InterviewId);
        }

        interview ??= interviews.FirstOrDefault(i => i.RoundNumber == scorecard.RoundNumber);

        return interview != null && IsHrInterview(interview);
    }

    public static bool IsHrInterview(RecruitmentInterview interview)
    {
        if (string.Equals(interview.RoundType, "HR", StringComparison.OrdinalIgnoreCase)) return true;

        var title = interview.Title ?? string.Empty;
        return HrTitleRegex.IsMatch(title);
    }

    public static decimal NormalizeRating(decimal rating, decimal min, decimal max, string normalizationMethod)
    {
        if (max <= min) return 0m;
        decimal normalized;
        if (string.Equals(normalizationMethod, "MinMaxPercentage", StringComparison.OrdinalIgnoreCase))
        {
            normalized = ((rating - min) / (max - min)) * 100m;
        }
        else // Default PercentageOfMax
        {
            normalized = max > 0m ? (rating / max) * 100m : 0m;
        }
        return Math.Clamp(Math.Round(normalized, 2), 0m, 100m);
    }

    public static RecruitmentFinalScoreResult Compute(
        IEnumerable<RecruitmentScorecard> allScorecards,
        IEnumerable<RecruitmentInterview>? interviews,
        RecruitmentInterviewPlan? plan = null)
    {
        var validScorecards = allScorecards
            .Where(s => !s.IsDeleted &&
                        (s.Status == RecruitmentScorecardStatus.Submitted || s.Status == RecruitmentScorecardStatus.Locked) &&
                        s.Criteria is { Count: > 0 })
            .ToList();

        // Effective scale configuration
        var scaleConfig = plan?.RatingScale ?? new RecruitmentRatingScaleConfig();
        decimal minRating = scaleConfig.MinRating > 0 ? scaleConfig.MinRating : 1m;
        decimal maxRating = scaleConfig.MaxRating > minRating ? scaleConfig.MaxRating : 5m;
        string normMethod = string.IsNullOrWhiteSpace(scaleConfig.NormalizationMethod) ? "PercentageOfMax" : scaleConfig.NormalizationMethod;
        string scaleType = string.IsNullOrWhiteSpace(scaleConfig.ScaleType) ? "Numeric1To5" : scaleConfig.ScaleType;
        decimal targetScore = plan?.HiringTargetScore > 0 ? plan.HiringTargetScore : 70m;
        string normFormula = string.Equals(normMethod, "MinMaxPercentage", StringComparison.OrdinalIgnoreCase)
            ? $"((Rating - {minRating}) / ({maxRating} - {minRating})) * 100"
            : $"(Rating / {maxRating}) * 100";

        if (validScorecards.Count == 0)
        {
            return new RecruitmentFinalScoreResult
            {
                FinalInterviewScore = 0,
                FinalInterviewRating = 0m,
                FinalInterviewRecommendation = "Pending",
                EvaluatedRoundsCount = 0,
                TotalScorecardsCount = 0,
                ExcludesHrRound = false,
                TargetScore = targetScore,
                ScaleType = scaleType,
                NormalizationFormula = normFormula
            };
        }

        // Substantive rounds (technical/managerial) take priority over administrative HR discussions
        var nonHrScorecards = validScorecards.Where(s => !IsHrRound(s, interviews)).ToList();
        bool excludesHr = false;
        List<RecruitmentScorecard> targetScorecards;

        if (nonHrScorecards.Count > 0)
        {
            targetScorecards = nonHrScorecards;
            excludesHr = validScorecards.Count > nonHrScorecards.Count;
        }
        else
        {
            targetScorecards = validScorecards;
            excludesHr = false;
        }

        var allCriteria = targetScorecards.SelectMany(s => s.Criteria).ToList();
        if (allCriteria.Count == 0)
        {
            return new RecruitmentFinalScoreResult
            {
                FinalInterviewScore = 0,
                FinalInterviewRating = 0m,
                FinalInterviewRecommendation = "Pending",
                EvaluatedRoundsCount = 0,
                TotalScorecardsCount = targetScorecards.Count,
                ExcludesHrRound = excludesHr,
                TargetScore = targetScore,
                ScaleType = scaleType,
                NormalizationFormula = normFormula
            };
        }

        var roundGroups = targetScorecards.GroupBy(s => s.RoundNumber).OrderBy(g => g.Key).ToList();
        var roundBreakdowns = new List<RecruitmentRoundScoreBreakdown>();
        var mandatoryFailures = new List<string>();

        foreach (var rg in roundGroups)
        {
            int roundNum = rg.Key;
            var stage = plan?.Stages.FirstOrDefault(s => s.Order == roundNum);
            var roundCriteria = rg.SelectMany(s => s.Criteria).ToList();

            decimal roundRawRating = roundCriteria.Count > 0
                ? Math.Round(roundCriteria.Average(c => c.Rating), 2)
                : 0m;

            // Compute normalized score for this round
            decimal roundNormScore;
            if (roundCriteria.Any(c => c.Weight.HasValue && c.Weight.Value > 0))
            {
                var weightedItems = roundCriteria.Where(c => c.Weight.HasValue && c.Weight.Value > 0).ToList();
                decimal totalWeight = weightedItems.Sum(c => c.Weight!.Value);
                if (totalWeight > 0)
                {
                    decimal weightedSum = weightedItems.Sum(c =>
                    {
                        var norm = c.NormalizedScore ?? NormalizeRating(c.Rating, minRating, maxRating, normMethod);
                        return norm * c.Weight!.Value;
                    });
                    roundNormScore = Math.Round(weightedSum / totalWeight, 2);
                }
                else
                {
                    roundNormScore = Math.Round(roundCriteria.Average(c => c.NormalizedScore ?? NormalizeRating(c.Rating, minRating, maxRating, normMethod)), 2);
                }
            }
            else
            {
                roundNormScore = Math.Round(roundCriteria.Average(c => c.NormalizedScore ?? NormalizeRating(c.Rating, minRating, maxRating, normMethod)), 2);
            }

            int reqPanelSize = stage?.RequiredPanelSize ?? 1;
            bool quorumMet = rg.Count() >= reqPanelSize;
            bool passedThreshold = true;
            if (stage?.PassThreshold.HasValue == true)
            {
                passedThreshold = roundNormScore >= stage.PassThreshold.Value;
                if (!passedThreshold && stage.IsMandatory)
                {
                    mandatoryFailures.Add($"Round {roundNum} ('{stage.Name}') scored {roundNormScore}%, failing mandatory pass threshold of {stage.PassThreshold.Value}%.");
                }
            }

            // Round recommendation consensus
            int rStrong = rg.Count(s => IsStrongHire(s.Recommendation));
            int rHire = rg.Count(s => IsRegularHire(s.Recommendation));
            int rNoHire = rg.Count(s => IsNoHire(s.Recommendation));
            string roundConsensus = (rNoHire > 0 && rNoHire >= rHire + rStrong) ? "No Hire"
                : (rStrong > 0 && rStrong >= rHire) ? "Strong Hire"
                : (rHire > 0) ? "Hire" : "Hold";

            roundBreakdowns.Add(new RecruitmentRoundScoreBreakdown
            {
                RoundNumber = roundNum,
                StageName = stage?.Name ?? $"Round {roundNum}",
                StageType = stage?.Type,
                NormalizedScore = roundNormScore,
                RawRating = roundRawRating,
                Weight = stage?.Weight,
                PassThreshold = stage?.PassThreshold,
                PassedThreshold = passedThreshold,
                IsMandatory = stage?.IsMandatory ?? false,
                ScorecardsCount = rg.Count(),
                RequiredPanelSize = reqPanelSize,
                QuorumMet = quorumMet,
                ConsensusRecommendation = roundConsensus
            });
        }

        // Competency breakdown
        var competencyBreakdowns = new List<RecruitmentCompetencyScoreBreakdown>();
        if (plan?.Competencies is { Count: > 0 })
        {
            foreach (var comp in plan.Competencies)
            {
                var matched = allCriteria.Where(c =>
                    (!string.IsNullOrEmpty(c.Code) && string.Equals(c.Code, comp.Code, StringComparison.OrdinalIgnoreCase)) ||
                    string.Equals(c.Name, comp.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                if (matched.Count > 0)
                {
                    decimal avgRaw = Math.Round(matched.Average(c => c.Rating), 2);
                    decimal avgNorm = Math.Round(matched.Average(c => c.NormalizedScore ?? NormalizeRating(c.Rating, minRating, maxRating, normMethod)), 2);
                    bool passed = true;
                    if (comp.MinScore.HasValue)
                    {
                        passed = avgNorm >= comp.MinScore.Value;
                        if (!passed && comp.IsMandatory)
                        {
                            mandatoryFailures.Add($"Mandatory competency '{comp.Name}' scored {avgNorm}%, failing minimum requirement of {comp.MinScore.Value}%.");
                        }
                    }

                    competencyBreakdowns.Add(new RecruitmentCompetencyScoreBreakdown
                    {
                        Code = comp.Code,
                        Name = comp.Name,
                        NormalizedScore = avgNorm,
                        RawRating = avgRaw,
                        Weight = comp.Weight,
                        MinScore = comp.MinScore,
                        IsMandatory = comp.IsMandatory,
                        Passed = passed,
                        RatingCount = matched.Count
                    });
                }
                else if (comp.IsMandatory && string.Equals(plan.MissingScorePolicy, "Zero", StringComparison.OrdinalIgnoreCase))
                {
                    mandatoryFailures.Add($"Mandatory competency '{comp.Name}' was unassessed (evaluated as zero score).");
                    competencyBreakdowns.Add(new RecruitmentCompetencyScoreBreakdown
                    {
                        Code = comp.Code,
                        Name = comp.Name,
                        NormalizedScore = 0m,
                        RawRating = 0m,
                        Weight = comp.Weight,
                        MinScore = comp.MinScore,
                        IsMandatory = comp.IsMandatory,
                        Passed = false,
                        RatingCount = 0
                    });
                }
            }
        }

        // Overall Final Score aggregation
        decimal overallNormScore;
        var weightedRounds = roundBreakdowns.Where(r => r.Weight.HasValue && r.Weight.Value > 0).ToList();
        if (weightedRounds.Count > 0 && Math.Abs(weightedRounds.Sum(r => r.Weight!.Value) - 100m) <= 0.01m)
        {
            overallNormScore = weightedRounds.Sum(r => r.NormalizedScore * (r.Weight!.Value / 100m));
        }
        else
        {
            overallNormScore = roundBreakdowns.Count > 0
                ? roundBreakdowns.Average(r => r.NormalizedScore)
                : 0m;
        }

        var avgRawRating = allCriteria.Average(c => c.Rating);
        var finalRating = Math.Round(avgRawRating, 2);
        var finalScore = (int)Math.Round(overallNormScore);

        // Precise panel consensus recommendation parsing
        int strongHire = targetScorecards.Count(s => IsStrongHire(s.Recommendation));
        int hire = targetScorecards.Count(s => IsRegularHire(s.Recommendation));
        int noHire = targetScorecards.Count(s => IsNoHire(s.Recommendation));
        int hold = targetScorecards.Count(s => IsHold(s.Recommendation));

        string recommendation = "Hire";
        if (noHire > 0 && noHire >= hire + strongHire)
        {
            recommendation = "No Hire";
        }
        else if (strongHire > 0 && strongHire >= hire)
        {
            recommendation = "Strong Hire";
        }
        else if (hire > 0)
        {
            recommendation = "Hire";
        }
        else if (hold > 0)
        {
            recommendation = "Hold";
        }

        // Mandatory Gates enforcement: Cannot recommend hire if any mandatory gate fails!
        bool passedMandatoryGates = mandatoryFailures.Count == 0;
        if (!passedMandatoryGates)
        {
            recommendation = "No Hire";
        }
        else if (finalScore < (int)targetScore && recommendation is "Strong Hire" or "Hire")
        {
            recommendation = "Hold"; // Below hiring target triggers human committee review
        }

        var evaluatedRounds = roundBreakdowns.Select(r => r.RoundNumber).ToList();

        return new RecruitmentFinalScoreResult
        {
            FinalInterviewScore = finalScore,
            FinalInterviewRating = finalRating,
            FinalInterviewRecommendation = recommendation,
            EvaluatedRoundsCount = evaluatedRounds.Count,
            TotalScorecardsCount = targetScorecards.Count,
            ExcludesHrRound = excludesHr,
            EvaluatedRoundNumbers = evaluatedRounds,
            PassedMandatoryGates = passedMandatoryGates,
            MandatoryGateFailures = mandatoryFailures,
            TargetScore = targetScore,
            MeetsHiringTarget = finalScore >= (int)targetScore,
            ScaleType = scaleType,
            NormalizationFormula = normFormula,
            RoundBreakdowns = roundBreakdowns,
            CompetencyBreakdowns = competencyBreakdowns
        };
    }

    private static bool IsNoHire(string? r) => !string.IsNullOrWhiteSpace(r) &&
        (r.IndexOf("No", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("Reject", StringComparison.OrdinalIgnoreCase) >= 0);

    private static bool IsStrongHire(string? r) => !string.IsNullOrWhiteSpace(r) &&
        r.IndexOf("Strong", StringComparison.OrdinalIgnoreCase) >= 0 && !IsNoHire(r);

    private static bool IsRegularHire(string? r) => !string.IsNullOrWhiteSpace(r) &&
        r.IndexOf("Hire", StringComparison.OrdinalIgnoreCase) >= 0 && !IsStrongHire(r) && !IsNoHire(r);

    private static bool IsHold(string? r) => !string.IsNullOrWhiteSpace(r) &&
        r.IndexOf("Hold", StringComparison.OrdinalIgnoreCase) >= 0 && !IsNoHire(r);
}
