using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentApplicationService(
    IMongoDbRepository<RecruitmentJobApplication> applications,
    IMongoDbRepository<Applicant> applicants,
    IHttpContextAccessor context,
    IMongoDbRepository<RecruitmentEnterpriseConfig>? configs = null) : IRecruitmentApplicationService
{
    private static readonly HashSet<string> AllowedStages = new(StringComparer.OrdinalIgnoreCase) { "New", "InProgress", "OnHold", "Shortlisted", "Selected", "Rejected", "ReApply" };
    public async Task<Result<string>> UpsertStage(RecruitmentApplicationStageCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(command.IdempotencyKey) || !AllowedStages.Contains(command.Stage?.Trim() ?? string.Empty))
            return Fail(400, "REC_INVALID_REQUEST", "Company, idempotency key, and a valid application stage are required.");
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId.Trim() && !x.IsDeleted);
        if (applicant is null || !string.Equals(applicant.VacancyId, command.VacancyId.Trim(), StringComparison.Ordinal))
            return Fail(404, "REC_INVALID_REQUEST", "Applicant and vacancy must belong to the current company.");
        var key = command.IdempotencyKey.Trim();
        var existing = string.IsNullOrWhiteSpace(command.ApplicationId)
            ? await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted)
            : await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicationId == command.ApplicationId && !x.IsDeleted);
        if (existing is not null && string.Equals(existing.LastCommandIdempotencyKey, key, StringComparison.Ordinal)) return Ok(existing.ApplicationId, "Application stage command already applied.");
        var next = command.Stage.Trim();
        if (existing is null)
        {
            var config = configs is null ? null : await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);
            var pipelineVersion = config?.DefaultVersion > 0 ? config.DefaultVersion : applicant.PipelineVersion <= 0 ? 1 : applicant.PipelineVersion;
            var snapshot = string.IsNullOrWhiteSpace(command.PipelineSnapshot) ? (string.IsNullOrWhiteSpace(applicant.WorkflowSnapshot) ? RecruitmentWorkflowSnapshot.LegacyV1 : applicant.WorkflowSnapshot) : command.PipelineSnapshot.Trim();
            var application = new RecruitmentJobApplication { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, CurrentStage = next, PipelineVersion = pipelineVersion, PipelineSnapshot = snapshot, LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            application.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = string.Empty, ToStage = next, ActorUserId = actorUserId, Reason = command.Reason?.Trim() });
            var added = await applications.AddOne(application);
            return added.Success ? Ok(application.ApplicationId, "Job application created.") : Fail(409, "REC_CONCURRENCY_CONFLICT", "Job application could not be created.");
        }
        var version = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(409, "REC_CONCURRENCY_CONFLICT", "Application was changed by another user.");
        if (string.Equals(existing.CurrentStage, next, StringComparison.OrdinalIgnoreCase)) { existing.LastCommandIdempotencyKey = key; }
        else { existing.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = existing.CurrentStage, ToStage = next, ActorUserId = actorUserId, Reason = command.Reason?.Trim() }); existing.CurrentStage = next; existing.Version = version + 1; existing.UpdatedDate = DateTime.UtcNow; }
        existing.LastCommandIdempotencyKey = key; existing.UpdatedBy = actorUserId;
        var updated = await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == existing.ApplicationId && (x.Version == version || x.Version == 0)), existing);
        return updated.Success ? Ok(existing.ApplicationId, "Job application stage updated.") : Fail(409, "REC_CONCURRENCY_CONFLICT", "Application was changed by another user.");
    }
    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
    private static Result<string> Fail(int status, string code, string message) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
}




