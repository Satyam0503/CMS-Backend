using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentScorecardService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentScorecard> scorecards,
    IMongoDbRepository<ApplicantLogs> applicantLogs,
    IHttpContextAccessor context,
    IMongoDbRepository<RecruitmentInterview>? interviews = null,
    IMongoDbRepository<RecruitmentInterviewPlan>? plans = null) : IRecruitmentScorecardService
{
    public async Task<Result<string>> Upsert(RecruitmentScorecardCommand command, string actorUserId)
    {
        var result = new Result<string> { Success = false, StatusCode = StatusCodes.Status400BadRequest };
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        var reviewerUserId = string.IsNullOrWhiteSpace(command.ReviewerUserId) ? actorUserId : command.ReviewerUserId.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(reviewerUserId))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Company, reviewer, and idempotency key are required.");

        // Resolve evaluation plan using VacancyId or company default to determine effective rating scale
        var plan = await ResolvePlanAsync(companyId, command.VacancyId);
        var minRating = plan?.RatingScale?.MinRating > 0 ? plan.RatingScale.MinRating : 1m;
        var maxRating = plan?.RatingScale?.MaxRating > minRating ? plan.RatingScale.MaxRating : 5m;
        var normMethod = plan?.RatingScale?.NormalizationMethod ?? "PercentageOfMax";

        if (command.Criteria is null || command.Criteria.Count == 0 ||
            command.Criteria.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Rating < minRating || x.Rating > maxRating))
        {
            return Fail(result, RecruitmentErrorCodes.ScorecardInvalid,
                $"At least one criterion with a rating between {minRating} and {maxRating} is required.");
        }

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && !x.IsDeleted);
        if (applicant is null) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Applicant was not found in the current company.");

        var roundNumber = command.RoundNumber <= 0 ? 1 : command.RoundNumber;
        RecruitmentInterview? interview = null;
        if (!string.IsNullOrWhiteSpace(command.InterviewId) && interviews is not null)
        {
            interview = await interviews.FirstOrDefault(x => x.CompanyId == companyId && x.InterviewId == command.InterviewId && !x.IsDeleted);
            if (interview is null) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Reviewer is not assigned to this interview.");
            roundNumber = interview.RoundNumber > 0 ? interview.RoundNumber : roundNumber;
            if (interview.PanelistUserIds.Length > 0 && !interview.PanelistUserIds.Contains(reviewerUserId, StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(command.ReviewerUserId) && !string.Equals(command.ReviewerUserId.Trim(), actorUserId, StringComparison.OrdinalIgnoreCase))
                    return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Reviewer is not assigned to this interview.");
                if (string.Equals(interview.CreatedBy, actorUserId, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(CurrentContext.AdminUserId(context)))
                    reviewerUserId = interview.PanelistUserIds[0];
                else
                    return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Reviewer is not assigned to this interview.");
            }
        }

        var existing = !string.IsNullOrWhiteSpace(command.ScorecardId)
            ? await scorecards.FirstOrDefault(x => x.CompanyId == companyId && x.ScorecardId == command.ScorecardId && !x.IsDeleted)
            : await scorecards.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && x.ReviewerUserId == reviewerUserId && (x.LastCommandIdempotencyKey == key || (x.RoundNumber == roundNumber && (string.IsNullOrEmpty(command.InterviewId) || x.InterviewId == command.InterviewId))) && !x.IsDeleted);

        if (existing is not null && existing.LastCommandIdempotencyKey == key)
            return Ok(result, existing.ScorecardId, "Scorecard command already applied.");

        var isOrganizerOrAdmin = (interview is not null && string.Equals(interview.CreatedBy, actorUserId, StringComparison.OrdinalIgnoreCase)) || !string.IsNullOrWhiteSpace(CurrentContext.AdminUserId(context));
        if (existing is not null && !isOrganizerOrAdmin && !string.Equals(existing.ReviewerUserId, actorUserId, StringComparison.OrdinalIgnoreCase) && !string.Equals(existing.CreatedBy, actorUserId, StringComparison.OrdinalIgnoreCase))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Only the assigned interviewer can update this scorecard.", StatusCodes.Status403Forbidden);

        var mappedCriteria = command.Criteria.Select(c => Map(c, minRating, maxRating, normMethod)).ToList();

        if (existing is null)
        {
            if (command.Status is not (RecruitmentScorecardStatus.Draft or RecruitmentScorecardStatus.Submitted))
                return Fail(result, RecruitmentErrorCodes.InvalidTransition, "A new scorecard must start in Draft or Submitted.", StatusCodes.Status409Conflict);

            var entity = new RecruitmentScorecard
            {
                CompanyId = companyId,
                ApplicantId = applicant.ApplicantId,
                VacancyId = command.VacancyId,
                InterviewId = command.InterviewId,
                RoundNumber = roundNumber,
                FeedbackDueAtUtc = command.FeedbackDueAtUtc,
                Recommendation = command.Recommendation?.Trim(),
                ReviewerUserId = reviewerUserId,
                Criteria = mappedCriteria,
                Status = command.Status.Trim(),
                LastCommandIdempotencyKey = key,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId
            };

            var saved = await scorecards.AddOne(entity);
            if (saved.Success)
            {
                await UpdateApplicantFinalScoreAsync(companyId, applicant.ApplicantId, plan);
                return Ok(result, entity.ScorecardId, "Scorecard saved.");
            }
            return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Scorecard could not be saved.", StatusCodes.Status409Conflict);
        }

        var next = command.Status.Trim();
        if (!IsAllowed(existing.Status, next))
            return Fail(result, RecruitmentErrorCodes.InvalidTransition, "Scorecard status transition is not allowed.", StatusCodes.Status409Conflict);

        var version = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version)
            return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Scorecard was changed by another user.", StatusCodes.Status409Conflict);

        var previousStatus = existing.Status;
        existing.Criteria = mappedCriteria;
        existing.RoundNumber = roundNumber;
        existing.FeedbackDueAtUtc = command.FeedbackDueAtUtc;
        existing.Recommendation = command.Recommendation?.Trim();
        existing.Status = next;
        existing.LastCommandIdempotencyKey = key;
        existing.Version = version + 1;
        existing.UpdatedBy = actorUserId;
        existing.UpdatedDate = DateTime.UtcNow;

        var updated = await scorecards.Update(Builders<RecruitmentScorecard>.Filter.Where(x => x.CompanyId == companyId && x.ScorecardId == existing.ScorecardId && (x.Version == version || x.Version == 0)), existing);
        if (!updated.Success)
            return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Scorecard was changed by another user.", StatusCodes.Status409Conflict);

        await applicantLogs.AddOne(new ApplicantLogs
        {
            CompanyId = companyId,
            ApplicantId = applicant.ApplicantId,
            ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(),
            JobRole = applicant.VacancyId,
            UserId = actorUserId,
            ActivityCategory = 0,
            Description = $"Scorecard {existing.Status.ToLowerInvariant()} by {existing.ReviewerUserId}.",
            EventType = "ScorecardUpdated",
            PreviousStage = previousStatus,
            CurrentStage = next,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId
        });

        await UpdateApplicantFinalScoreAsync(companyId, applicant.ApplicantId, plan);
        return Ok(result, existing.ScorecardId, "Scorecard updated.");
    }

    public async Task<Result<List<RecruitmentScorecard>>> GetByApplicant(string applicantId, string vacancyId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(applicantId) || string.IsNullOrWhiteSpace(vacancyId))
            return new Result<List<RecruitmentScorecard>> { Success = false, StatusCode = 400, Message = "Applicant and vacancy are required." };

        var rows = (await scorecards.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId.Trim() && x.VacancyId == vacancyId.Trim() && !x.IsDeleted))
            .OrderByDescending(x => x.RoundNumber)
            .ThenByDescending(x => x.UpdatedDate)
            .ToList();

        return new Result<List<RecruitmentScorecard>> { Success = true, StatusCode = 200, MethodResult = rows, Data = rows, TotalRecords = rows.Count, Message = $"{rows.Count} scorecard(s) loaded." };
    }

    public async Task<Result<RecruitmentFinalScoreResult>> GetFinalInterviewScore(string applicantId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(applicantId))
            return new Result<RecruitmentFinalScoreResult> { Success = false, StatusCode = 400, Message = "Applicant ID is required." };

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicantId.Trim() && !x.IsDeleted);
        var plan = await ResolvePlanAsync(companyId, applicant?.VacancyId);

        var allScorecards = await scorecards.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId.Trim() && !x.IsDeleted);
        var allInterviews = interviews is not null
            ? await interviews.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId.Trim() && !x.IsDeleted)
            : null;

        var computed = RecruitmentInterviewScoreCalculator.Compute(allScorecards, allInterviews, plan);
        await UpdateApplicantFinalScoreAsync(companyId, applicantId.Trim(), plan);

        return new Result<RecruitmentFinalScoreResult>
        {
            Success = true,
            StatusCode = 200,
            MethodResult = computed,
            Data = computed,
            Message = "Final interview score calculated."
        };
    }

    private async Task<RecruitmentInterviewPlan?> ResolvePlanAsync(string companyId, string? vacancyId)
    {
        if (plans is null) return null;
        RecruitmentInterviewPlan? plan = null;
        if (!string.IsNullOrWhiteSpace(vacancyId))
            plan = await plans.FirstOrDefault(x => x.CompanyId == companyId && x.VacancyId == vacancyId.Trim() && !x.IsDeleted);
        plan ??= await plans.FirstOrDefault(x => x.CompanyId == companyId && x.IsDefault && string.IsNullOrEmpty(x.VacancyId) && !x.IsDeleted);
        return plan;
    }

    private async Task UpdateApplicantFinalScoreAsync(string companyId, string applicantId, RecruitmentInterviewPlan? plan = null)
    {
        try
        {
            var allScorecards = await scorecards.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted);
            var allInterviews = interviews is not null
                ? await interviews.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted)
                : null;

            plan ??= await ResolvePlanAsync(companyId, null);
            var computed = RecruitmentInterviewScoreCalculator.Compute(allScorecards, allInterviews, plan);

            var targetApplicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted);
            if (targetApplicant is not null)
            {
                targetApplicant.FinalInterviewScore = computed.FinalInterviewScore > 0 ? computed.FinalInterviewScore : null;
                targetApplicant.FinalInterviewRating = computed.FinalInterviewRating > 0 ? computed.FinalInterviewRating : null;
                targetApplicant.FinalInterviewRecommendation = computed.FinalInterviewScore > 0 ? computed.FinalInterviewRecommendation : null;
                targetApplicant.EvaluatedRoundsCount = computed.EvaluatedRoundsCount;
                targetApplicant.FinalScoreCalculatedAtUtc = DateTime.UtcNow;
                await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == targetApplicant.ApplicantId), targetApplicant);
            }
        }
        catch
        {
            // Non-blocking telemetry
        }
    }

    private static RecruitmentScorecardCriterion Map(
        RecruitmentScorecardCriterionCommand item,
        decimal minRating,
        decimal maxRating,
        string normMethod)
    {
        var norm = RecruitmentInterviewScoreCalculator.NormalizeRating(item.Rating, minRating, maxRating, normMethod);
        return new RecruitmentScorecardCriterion
        {
            Code = item.Code?.Trim(),
            Name = item.Name.Trim(),
            Rating = item.Rating,
            NormalizedScore = norm,
            QualitativeLabel = item.QualitativeLabel?.Trim(),
            Weight = item.Weight,
            IsMandatory = item.IsMandatory,
            Notes = item.Notes?.Trim()
        };
    }

    private static bool IsAllowed(string current, string next) =>
        current == next || (current, next) switch
        {
            (RecruitmentScorecardStatus.Draft, RecruitmentScorecardStatus.Submitted) => true,
            (RecruitmentScorecardStatus.Submitted, RecruitmentScorecardStatus.Locked) => true,
            _ => false
        };

    private static Result<string> Ok(Result<string> result, string id, string message)
    {
        result.Success = true;
        result.StatusCode = StatusCodes.Status200OK;
        result.MethodResult = id;
        result.Message = message;
        return result;
    }

    private static Result<string> Fail(Result<string> result, string code, string message, int status = StatusCodes.Status400BadRequest)
    {
        result.ErrorCode = code;
        result.StatusCode = status;
        result.Message = message;
        return result;
    }
}
