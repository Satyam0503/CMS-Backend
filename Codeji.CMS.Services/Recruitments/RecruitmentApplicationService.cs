using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Services.Recruitments;
public sealed class RecruitmentApplicationService(
    IMongoDbRepository<RecruitmentJobApplication> applications,
    IMongoDbRepository<Applicant> applicants,
    IHttpContextAccessor context,
    IMongoDbRepository<RecruitmentEnterpriseConfig>? configs = null,
    IMongoDbRepository<ApplicantLogs>? logs = null) : IRecruitmentApplicationService
{
    public async Task<Result<string>> UpsertStage(RecruitmentApplicationStageCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(command.IdempotencyKey) || !RecruitmentApplicationStageCatalog.IsSupported(command.Stage))
            return Fail(400, "REC_INVALID_REQUEST", "Company, idempotency key, and a valid application stage are required.");
        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId.Trim() && !x.IsDeleted);
        if (applicant is null || !string.Equals(applicant.VacancyId, command.VacancyId.Trim(), StringComparison.Ordinal))
            return Fail(404, "REC_INVALID_REQUEST", "Applicant and vacancy must belong to the current company.");
        var key = command.IdempotencyKey.Trim();
        var existing = string.IsNullOrWhiteSpace(command.ApplicationId)
            ? await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted)
            : await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicationId == command.ApplicationId && !x.IsDeleted);
        if (existing is not null && string.Equals(existing.LastCommandIdempotencyKey, key, StringComparison.Ordinal)) return Ok(existing.ApplicationId, "Application stage command already applied.");
        var next = RecruitmentApplicationStageCatalog.Normalize(command.Stage);
        if (string.Equals(next, "Selected", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "REC_INVALID_TRANSITION", "Applicants must be selected through the hiring decision workflow.");
        if (existing is null)
        {
            var config = configs is null ? null : await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);
            var pipelineVersion = config?.DefaultVersion > 0 ? config.DefaultVersion : applicant.PipelineVersion <= 0 ? 1 : applicant.PipelineVersion;
            var snapshot = string.IsNullOrWhiteSpace(command.PipelineSnapshot) ? (string.IsNullOrWhiteSpace(applicant.WorkflowSnapshot) ? RecruitmentWorkflowSnapshot.LegacyV1 : applicant.WorkflowSnapshot) : command.PipelineSnapshot.Trim();
            var application = new RecruitmentJobApplication { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, CurrentStage = next, PipelineVersion = pipelineVersion, PipelineSnapshot = snapshot, LastCommandIdempotencyKey = key, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            application.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = string.Empty, ToStage = next, ActorUserId = actorUserId, Reason = command.Reason?.Trim() });
            var added = await applications.AddOne(application);
            if (added.Success) { await SyncApplicantStage(applicant, next, actorUserId); await WriteStageLog(applicant, string.Empty, next, actorUserId, command.Reason); }
            return added.Success ? Ok(application.ApplicationId, "Job application created.") : Fail(409, "REC_CONCURRENCY_CONFLICT", "Job application could not be created.");
        }
        var version = existing.Version <= 0 ? 1 : existing.Version;
        var previousStage = existing.CurrentStage;
        var transitionError = string.Empty;
        if (!Enum.TryParse<ActivityType>(existing.CurrentStage, true, out var currentStage) || !Enum.TryParse<ActivityType>(next, true, out var targetStage) || !RecruitmentPipelineRules.IsAllowedTransition(currentStage, targetStage, out transitionError))
            return Fail(409, "REC_INVALID_TRANSITION", string.IsNullOrWhiteSpace(transitionError) ? "Application stage transition is not allowed." : transitionError);
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(409, "REC_CONCURRENCY_CONFLICT", "Application was changed by another user.");
        if (string.Equals(existing.CurrentStage, next, StringComparison.OrdinalIgnoreCase)) { existing.LastCommandIdempotencyKey = key; }
        else { existing.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = existing.CurrentStage, ToStage = next, ActorUserId = actorUserId, Reason = command.Reason?.Trim() }); existing.CurrentStage = next; existing.Version = version + 1; existing.UpdatedDate = DateTime.UtcNow; }
        existing.LastCommandIdempotencyKey = key; existing.UpdatedBy = actorUserId;
        var updated = await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == existing.ApplicationId && (x.Version == version || x.Version == 0)), existing);
        if (updated.Success) { await SyncApplicantStage(applicant, next, actorUserId); await WriteStageLog(applicant, previousStage, next, actorUserId, command.Reason); }
        return updated.Success ? Ok(existing.ApplicationId, "Job application stage updated.") : Fail(409, "REC_CONCURRENCY_CONFLICT", "Application was changed by another user.");
    }
    public async Task<Result<int>> BackfillLegacy(string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(actorUserId)) return new Result<int> { Success = false, StatusCode = 400, ErrorCode = "REC_INVALID_REQUEST", Message = "Company and actor are required." };
        var tenantApplicants = (await applicants.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)).ToList();
        var created = 0;
        foreach (var applicant in tenantApplicants)
        {
            var existing = await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted);
            if (existing is not null) continue;
            var stage = applicant.ActivityType.ToString();
            if (!RecruitmentApplicationStageCatalog.IsSupported(stage)) stage = nameof(ActivityType.New);
            var snapshot = string.IsNullOrWhiteSpace(applicant.WorkflowSnapshot) ? RecruitmentWorkflowSnapshot.LegacyV1 : applicant.WorkflowSnapshot;
            var entity = new RecruitmentJobApplication { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, CurrentStage = stage, PipelineVersion = applicant.PipelineVersion > 0 ? applicant.PipelineVersion : 1, PipelineSnapshot = snapshot, CreatedBy = actorUserId, UpdatedBy = actorUserId };
            entity.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = string.Empty, ToStage = stage, ActorUserId = actorUserId, Reason = "Legacy applicant backfill" });
            var added = await applications.AddOne(entity);
            if (added.Success) created++;
        }
        return new Result<int> { Success = true, StatusCode = 200, MethodResult = created, Data = created, TotalRecords = created, Message = $"Created {created} canonical application record(s)." };
    }
    public async Task<Result<List<RecruitmentApplicationStageEvent>>> GetHistory(string applicationId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(applicationId)) return new Result<List<RecruitmentApplicationStageEvent>> { Success = false, StatusCode = 400, ErrorCode = "REC_INVALID_REQUEST", Message = "Company and application are required." };
        var application = await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicationId == applicationId.Trim() && !x.IsDeleted);
        if (application is null) return new Result<List<RecruitmentApplicationStageEvent>> { Success = false, StatusCode = 404, ErrorCode = "REC_NOT_FOUND", Message = "Application was not found." };
        var history = application.StageHistory.OrderBy(x => x.ChangedAtUtc).ToList();
        return new Result<List<RecruitmentApplicationStageEvent>> { Success = true, StatusCode = 200, MethodResult = history, Data = history, TotalRecords = history.Count, Message = "Application stage history loaded." };
    }
    private async Task WriteStageLog(Applicant applicant, string previous, string current, string actorUserId, string? reason)
    {
        if (logs is null) return;
        await logs.AddOne(new ApplicantLogs { CompanyId = applicant.CompanyId, ApplicantId = applicant.ApplicantId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobRole = applicant.VacancyId, UserId = actorUserId, ActivityCategory = 0, Description = $"Application stage changed from {previous} to {current}." + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Reason: {reason.Trim()}"), EventType = "ApplicationStageChanged", PreviousStage = previous, CurrentStage = current, CreatedBy = actorUserId, UpdatedBy = actorUserId });
    }
    private async Task SyncApplicantStage(Applicant applicant, string stage, string actorUserId)
    {
        if (!Enum.TryParse<ActivityType>(stage, true, out var next) || applicant.ActivityType == next)
            return;
        var previous = applicant.ActivityType;
        applicant.ActivityType = next;
        applicant.UpdatedBy = actorUserId;
        applicant.UpdatedDate = DateTime.UtcNow;
        await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == applicant.CompanyId && x.ApplicantId == applicant.ApplicantId && x.ActivityType == previous), applicant);
    }
    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
    private static Result<string> Fail(int status, string code, string message) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
}



