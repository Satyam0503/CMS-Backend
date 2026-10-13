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
        plan ??= new RecruitmentInterviewPlan { CompanyId = companyId, Name = "System default", IsDefault = true, Stages = [new RecruitmentInterviewPlanStage { Order = 1, Code = "technical", Name = "Technical interview", Type = "Technical", RequiredPanelSize = 1, ScorecardRequired = true, FeedbackDueHours = 24 }, new RecruitmentInterviewPlanStage { Order = 2, Code = "hr", Name = "HR interview", Type = "HR", RequiredPanelSize = 1, ScorecardRequired = true, FeedbackDueHours = 24 }] };
        return new Result<RecruitmentInterviewPlan> { Success = true, StatusCode = 200, MethodResult = plan, Data = plan, Message = "Interview plan loaded." };
    }

    public async Task<Result<string>> Upsert(RecruitmentInterviewPlanCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(command.Name) || command.Stages is null || command.Stages.Count == 0) return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Company, plan name, stages, and idempotency key are required.");
        if (!string.IsNullOrWhiteSpace(command.VacancyId) && await vacancies.FirstOrDefault(x => x.CompanyId == companyId && x.JobId == command.VacancyId.Trim() && !x.IsDeleted) is null) return Fail(404, RecruitmentErrorCodes.InvalidRequest, "Vacancy was not found in the current company.");
        var ordered = command.Stages.OrderBy(x => x.Order).ToList();
        if (ordered.Select(x => x.Order).Distinct().Count() != ordered.Count || ordered.Select(x => x.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ordered.Count) return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Stage order and stage codes must be unique.");
        if (ordered.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Name) || x.RequiredPanelSize < 1 || x.FeedbackDueHours < 1)) return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Each stage needs a code, name, panel size, and feedback deadline.");
        var existing = string.IsNullOrWhiteSpace(command.PlanId) ? await plans.FirstOrDefault(x => x.CompanyId == companyId && x.VacancyId == command.VacancyId && x.Name == command.Name.Trim() && !x.IsDeleted) : await plans.FirstOrDefault(x => x.CompanyId == companyId && x.PlanId == command.PlanId && !x.IsDeleted);
        if (existing is not null && string.Equals(existing.LastCommandIdempotencyKey, key, StringComparison.Ordinal)) return Ok(existing.PlanId, "Interview plan command already applied.");
        var mapped = ordered.Select(x => new RecruitmentInterviewPlanStage { Order = x.Order, Code = x.Code.Trim(), Name = x.Name.Trim(), Type = x.Type?.Trim(), RequiredPanelSize = x.RequiredPanelSize, ScorecardRequired = x.ScorecardRequired, FeedbackDueHours = x.FeedbackDueHours }).ToList();
        if (existing is null)
        {
            var entity = new RecruitmentInterviewPlan { CompanyId = companyId, VacancyId = command.VacancyId?.Trim(), Name = command.Name.Trim(), IsDefault = command.IsDefault, Stages = mapped, LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            var add = await plans.AddOne(entity); return add.Success ? Ok(entity.PlanId, "Interview plan created.") : Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Interview plan could not be created.");
        }
        var version = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Interview plan was changed by another user.");
        existing.Name = command.Name.Trim(); existing.VacancyId = command.VacancyId?.Trim(); existing.IsDefault = command.IsDefault; existing.Stages = mapped; existing.LastCommandIdempotencyKey = key; existing.Version = version + 1; existing.UpdatedBy = actorUserId; existing.UpdatedDate = DateTime.UtcNow;
        var update = await plans.Update(Builders<RecruitmentInterviewPlan>.Filter.Where(x => x.CompanyId == companyId && x.PlanId == existing.PlanId && (x.Version == version || x.Version == 0)), existing);
        return update.Success ? Ok(existing.PlanId, "Interview plan updated.") : Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Interview plan was changed by another user.");
    }
    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
    private static Result<string> Fail(int status, string code, string message) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
}


