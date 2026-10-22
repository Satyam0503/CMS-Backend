using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentInterviewPlanService(
    IMongoDbRepository<RecruitmentInterviewPlan> plans,
    IMongoDbRepository<JobVacancy> vacancies,
    IHttpContextAccessor context) : IRecruitmentInterviewPlanService
{
    public async Task<Result<RecruitmentInterviewPlan>> Get(string? vacancyId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId)) return new Result<RecruitmentInterviewPlan> { Success = false, StatusCode = 400, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Company context is required." };
        RecruitmentInterviewPlan? plan = null;
        if (!string.IsNullOrWhiteSpace(vacancyId)) plan = await plans.FirstOrDefault(x => x.CompanyId == companyId && x.VacancyId == vacancyId.Trim() && !x.IsDeleted);
        plan ??= await plans.FirstOrDefault(x => x.CompanyId == companyId && x.IsDefault && string.IsNullOrEmpty(x.VacancyId) && !x.IsDeleted);
        plan ??= new RecruitmentInterviewPlan
        {
            CompanyId = companyId,
            Name = "System default",
            IsDefault = true,
            RatingScale = new RecruitmentRatingScaleConfig
            {
                ScaleType = "Numeric1To5",
                MinRating = 1m,
                MaxRating = 5m,
                Step = 1m,
                NormalizationMethod = "PercentageOfMax"
            },
            HiringTargetScore = 70m,
            MissingScorePolicy = "Exclude",
            Stages =
            [
                new RecruitmentInterviewPlanStage { Order = 1, Code = "technical", Name = "Technical interview", Type = "Technical", RequiredPanelSize = 1, ScorecardRequired = true, FeedbackDueHours = 24 },
                new RecruitmentInterviewPlanStage { Order = 2, Code = "hr", Name = "HR interview", Type = "HR", RequiredPanelSize = 1, ScorecardRequired = true, FeedbackDueHours = 24 }
            ]
        };
        return new Result<RecruitmentInterviewPlan> { Success = true, StatusCode = 200, MethodResult = plan, Data = plan, Message = "Interview plan loaded." };
    }

    public async Task<Result<string>> Upsert(RecruitmentInterviewPlanCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(command.Name) || command.Stages is null || command.Stages.Count == 0)
            return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Company, plan name, stages, and idempotency key are required.");
        if (!string.IsNullOrWhiteSpace(command.VacancyId) && await vacancies.FirstOrDefault(x => x.CompanyId == companyId && x.JobId == command.VacancyId.Trim() && !x.IsDeleted) is null)
            return Fail(404, RecruitmentErrorCodes.InvalidRequest, "Vacancy was not found in the current company.");
        
        var ordered = command.Stages.OrderBy(x => x.Order).ToList();
        if (ordered.Select(x => x.Order).Distinct().Count() != ordered.Count || ordered.Select(x => x.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ordered.Count)
            return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Stage order and stage codes must be unique.");
        if (ordered.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Name) || x.RequiredPanelSize < 1 || x.FeedbackDueHours < 1))
            return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Each stage needs a code, name, panel size, and feedback deadline.");

        // Validation for configurable rating scale
        if (command.RatingScale is not null)
        {
            if (command.RatingScale.MinRating >= command.RatingScale.MaxRating)
                return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Minimum rating must be strictly less than maximum rating.");
            if (command.RatingScale.Step <= 0)
                return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Rating increment step must be greater than zero.");
            if (command.RatingScale.Levels is { Count: > 0 })
            {
                var labelGroups = command.RatingScale.Levels.GroupBy(x => x.Label.Trim(), StringComparer.OrdinalIgnoreCase);
                if (labelGroups.Any(g => g.Count() > 1))
                    return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Qualitative rating labels must be unique.");
                if (command.RatingScale.Levels.Any(x => x.Value < command.RatingScale.MinRating || x.Value > command.RatingScale.MaxRating))
                    return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Qualitative rating level values must fall within the configured min and max range.");
            }
        }

        // Validation for configurable competencies
        if (command.Competencies is { Count: > 0 })
        {
            if (command.Competencies.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Name)))
                return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Each competency must have a code and name.");
            if (command.Competencies.Select(x => x.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != command.Competencies.Count)
                return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Competency codes must be unique.");
            if (command.Competencies.Any(x => x.Weight > 0))
            {
                var sumWeight = command.Competencies.Sum(x => x.Weight);
                if (Math.Abs(sumWeight - 100m) > 0.01m)
                    return Fail(400, RecruitmentErrorCodes.InvalidRequest, "When competency weights are configured, the sum of all weights must equal 100%.");
            }
        }

        // Validation for round weights
        if (command.Stages.Any(x => x.Weight.HasValue && x.Weight.Value > 0))
        {
            var sumStageWeight = command.Stages.Where(x => x.Weight.HasValue).Sum(x => x.Weight!.Value);
            if (Math.Abs(sumStageWeight - 100m) > 0.01m)
                return Fail(400, RecruitmentErrorCodes.InvalidRequest, "When round weights are configured, the sum of all weights must equal 100%.");
        }

        var existing = string.IsNullOrWhiteSpace(command.PlanId)
            ? await plans.FirstOrDefault(x => x.CompanyId == companyId && x.VacancyId == command.VacancyId && x.Name == command.Name.Trim() && !x.IsDeleted)
            : await plans.FirstOrDefault(x => x.CompanyId == companyId && x.PlanId == command.PlanId && !x.IsDeleted);
        if (existing is not null && string.Equals(existing.LastCommandIdempotencyKey, key, StringComparison.Ordinal))
            return Ok(existing.PlanId, "Interview plan command already applied.");

        var mapped = ordered.Select(x => new RecruitmentInterviewPlanStage
        {
            Order = x.Order,
            Code = x.Code.Trim(),
            Name = x.Name.Trim(),
            Type = x.Type?.Trim(),
            RequiredPanelSize = x.RequiredPanelSize,
            ScorecardRequired = x.ScorecardRequired,
            FeedbackDueHours = x.FeedbackDueHours,
            Weight = x.Weight,
            PassThreshold = x.PassThreshold,
            IsMandatory = x.IsMandatory
        }).ToList();

        var ratingScale = command.RatingScale ?? new RecruitmentRatingScaleConfig();
        var competencies = command.Competencies?.Select(c => new RecruitmentCompetencyConfig
        {
            Code = c.Code.Trim(),
            Name = c.Name.Trim(),
            Description = c.Description?.Trim(),
            Weight = c.Weight,
            IsMandatory = c.IsMandatory,
            MinScore = c.MinScore
        }).ToList() ?? [];
        var hiringTarget = command.HiringTargetScore <= 0 ? 70m : command.HiringTargetScore;
        var missingPolicy = string.IsNullOrWhiteSpace(command.MissingScorePolicy) ? "Exclude" : command.MissingScorePolicy.Trim();

        if (existing is null)
        {
            var entity = new RecruitmentInterviewPlan
            {
                CompanyId = companyId,
                VacancyId = command.VacancyId?.Trim(),
                Name = command.Name.Trim(),
                IsDefault = command.IsDefault,
                Stages = mapped,
                RatingScale = ratingScale,
                Competencies = competencies,
                HiringTargetScore = hiringTarget,
                MissingScorePolicy = missingPolicy,
                LastCommandIdempotencyKey = key,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId
            };
            var add = await plans.AddOne(entity);
            return add.Success ? Ok(entity.PlanId, "Interview plan created.") : Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Interview plan could not be created.");
        }

        var version = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version)
            return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Interview plan was changed by another user.");

        existing.Name = command.Name.Trim();
        existing.VacancyId = command.VacancyId?.Trim();
        existing.IsDefault = command.IsDefault;
        existing.Stages = mapped;
        existing.RatingScale = ratingScale;
        existing.Competencies = competencies;
        existing.HiringTargetScore = hiringTarget;
        existing.MissingScorePolicy = missingPolicy;
        existing.LastCommandIdempotencyKey = key;
        existing.Version = version + 1;
        existing.UpdatedBy = actorUserId;
        existing.UpdatedDate = DateTime.UtcNow;

        var update = await plans.Update(Builders<RecruitmentInterviewPlan>.Filter.Where(x => x.CompanyId == companyId && x.PlanId == existing.PlanId && (x.Version == version || x.Version == 0)), existing);
        return update.Success ? Ok(existing.PlanId, "Interview plan updated.") : Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Interview plan was changed by another user.");
    }

    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
    private static Result<string> Fail(int status, string code, string message) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
}
