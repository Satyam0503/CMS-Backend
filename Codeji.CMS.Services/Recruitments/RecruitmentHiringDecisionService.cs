using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentHiringDecisionService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentInterview> interviews,
    IMongoDbRepository<RecruitmentHiringDecision> decisions,
    IMongoDbRepository<ApplicantLogs> logs,
    IHttpContextAccessor context,
    IMongoDbRepository<RecruitmentJobApplication>? applications = null,
    IMongoDbRepository<RecruitmentEnterpriseConfig>? configs = null,
    IMongoDbRepository<JobVacancy>? vacancies = null,
    IMongoDbRepository<EmpUser>? employees = null) : IRecruitmentHiringDecisionService
{
    private static readonly HashSet<string> Valid = [RecruitmentHiringDecisionStatus.Approve, RecruitmentHiringDecisionStatus.Hold, RecruitmentHiringDecisionStatus.Reject];

    public async Task<Result<string>> Upsert(RecruitmentHiringDecisionCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        var decisionValue = command.Decision?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(key) || !Valid.Contains(decisionValue ?? string.Empty))
            return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Company, idempotency key, and a valid hiring decision are required.");
        if (decisionValue is RecruitmentHiringDecisionStatus.Hold or RecruitmentHiringDecisionStatus.Reject && string.IsNullOrWhiteSpace(command.Notes))
            return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Decision notes are required for Hold or Reject outcomes.");

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId.Trim() && x.VacancyId == command.VacancyId.Trim() && !x.IsDeleted);
        if (applicant is null)
            return Fail(404, RecruitmentErrorCodes.InvalidRequest, "Applicant was not found in the current company.");

        var existing = string.IsNullOrWhiteSpace(command.DecisionId)
            ? await decisions.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted)
            : await decisions.FirstOrDefault(x => x.CompanyId == companyId && x.DecisionId == command.DecisionId && !x.IsDeleted);
        if (existing is not null && existing.LastCommandIdempotencyKey == key)
        {
            if (existing.ManagerReviewStatus == RecruitmentManagerReviewStatus.Pending)
                return Ok(existing.DecisionId, "Approval request is still pending with the assigned hiring manager. The candidate has not advanced to offer.");
            return await ProcessPendingStageSync(existing, existing.UpdatedBy ?? actorUserId)
                ? Ok(existing.DecisionId, "Hiring decision already applied.")
                : Fail(StatusCodes.Status503ServiceUnavailable, RecruitmentErrorCodes.StageSyncPending, "The hiring decision was saved, and its application stage is queued for recovery. Retry this same request.");
        }

        var config = configs is null ? null : await configs.FirstOrDefault(x => x.CompanyId == companyId && !x.IsDeleted);
        var vacancy = vacancies is null ? null : await vacancies.FirstOrDefault(x => x.CompanyId == companyId && x.JobId == applicant.VacancyId && !x.IsDeleted);
        var requiresManagerReview = config?.HiringManagerReview == true;
        var allowCustomWorkflows = config is null || config.AllowJobWorkflowCustomization;

        if (existing?.ManagerReviewStatus == RecruitmentManagerReviewStatus.Pending)
            return Fail(409, RecruitmentErrorCodes.InvalidTransition, "This approval is waiting for the assigned hiring manager.");
        if (existing?.ManagerReviewStatus is RecruitmentManagerReviewStatus.Approved or RecruitmentManagerReviewStatus.Rejected)
            return Fail(409, RecruitmentErrorCodes.InvalidTransition, "The hiring manager has already completed this review.");

        var allowDirectFromEarlyStage = (allowCustomWorkflows ? vacancy?.WorkflowMode switch
        {
            "Direct" => true,
            "Enterprise" => false,
            _ => (bool?)null
        } : null) ?? (config is not null && (!config.ApplicationReview || string.Equals(config.RecruitmentMode, "Simple", StringComparison.OrdinalIgnoreCase)));

        if (string.Equals(decisionValue, RecruitmentHiringDecisionStatus.Approve, StringComparison.OrdinalIgnoreCase)
            && applicant.ActivityType is not (ActivityType.Shortlisted or ActivityType.OnHold or ActivityType.Selected)
            && !(allowDirectFromEarlyStage && applicant.ActivityType is ActivityType.New or ActivityType.InProgress))
        {
            return Fail(409, RecruitmentErrorCodes.InvalidTransition, "The application must be shortlisted through screening before approval.");
        }

        if (string.Equals(decisionValue, RecruitmentHiringDecisionStatus.Approve, StringComparison.OrdinalIgnoreCase))
        {
            var openInterviews = (await interviews.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted && x.Status != RecruitmentInterviewStatus.Cancelled)).ToList();
            var requireInterviewGate = (allowCustomWorkflows ? vacancy?.RequireInterview : null) ?? (allowCustomWorkflows ? vacancy?.WorkflowMode switch
            {
                "Direct" => false,
                "Standard" => true,
                "Enterprise" => true,
                _ => (bool?)null
            } : null) ?? (config is null || (config.RequireInterviewBeforeDecision && (config.InterviewRounds || config.Scorecards || !string.Equals(config.RecruitmentMode, "Simple", StringComparison.OrdinalIgnoreCase))));
            if (openInterviews.Count == 0 && requireInterviewGate)
                return Fail(409, RecruitmentErrorCodes.InvalidTransition, "At least one interview round must be completed and debriefed before approval.");
            if (requireInterviewGate && openInterviews.Any(x => x.Status != RecruitmentInterviewStatus.Completed || x.DebriefStatus != RecruitmentDebriefStatus.Complete))
                return Fail(409, RecruitmentErrorCodes.InvalidTransition, "All scheduled interview rounds must be completed and debriefed before approval.");
        }
        if (requiresManagerReview && string.Equals(decisionValue, RecruitmentHiringDecisionStatus.Approve, StringComparison.OrdinalIgnoreCase))
        {
            var managerId = vacancy?.HiringManagerUserId?.Trim();
            if (string.IsNullOrWhiteSpace(managerId))
                return Fail(409, RecruitmentErrorCodes.InvalidTransition, "Assign an active hiring manager to this vacancy before requesting approval.");
            if (employees is null || await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == managerId && x.Status && !x.IsDeleted) is null)
                return Fail(409, RecruitmentErrorCodes.InvalidTransition, "The vacancy hiring manager is not an active employee in this company.");
            if (string.Equals(managerId, actorUserId, StringComparison.OrdinalIgnoreCase))
                return Fail(403, RecruitmentErrorCodes.PermissionDenied, "The requester cannot approve their own hiring decision.");

            var wasNew = existing is null;
            existing ??= new RecruitmentHiringDecision { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, CreatedBy = actorUserId };
            var managerVersion = existing.Version <= 0 ? 1 : existing.Version;
            if (!wasNew && command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != managerVersion)
                return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision was changed by another user.");
            existing.Decision = RecruitmentHiringDecisionStatus.PendingManagerReview;
            existing.Notes = command.Notes?.Trim();
            existing.HiringManagerUserId = managerId;
            existing.ManagerReviewStatus = RecruitmentManagerReviewStatus.Pending;
            existing.ManagerReviewRequestedByUserId = actorUserId;
            existing.ManagerReviewRequestedAtUtc = DateTime.UtcNow;
            existing.ManagerReviewNotes = null;
            existing.ManagerReviewedByUserId = null;
            existing.ManagerReviewedAtUtc = null;
            existing.StageSyncFromStage = applicant.ActivityType.ToString();
            existing.StageSyncTargetStage = null;
            existing.StageSyncStatus = "NotRequired";
            existing.LastCommandIdempotencyKey = key;
            existing.Version = wasNew ? 1 : managerVersion + 1;
            existing.UpdatedBy = actorUserId;
            existing.UpdatedDate = DateTime.UtcNow;
            var savedForReview = wasNew
                ? await decisions.AddOne(existing)
                : await decisions.Update(Builders<RecruitmentHiringDecision>.Filter.Where(x => x.CompanyId == companyId && x.DecisionId == existing.DecisionId && (x.Version == managerVersion || x.Version == 0)), existing);
            if (!savedForReview.Success) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Manager approval request could not be saved.");

            await logs.AddOne(new ApplicantLogs { CompanyId = companyId, ApplicantId = applicant.ApplicantId, ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(), JobRole = applicant.VacancyId, UserId = actorUserId, ActivityCategory = 0, Description = $"Hiring decision approval requested from manager {managerId}. {command.Notes?.Trim()}", EventType = "HiringManagerReviewRequested", PreviousStage = applicant.ActivityType.ToString(), CurrentStage = applicant.ActivityType.ToString(), CreatedBy = actorUserId, UpdatedBy = actorUserId });
            return Ok(existing.DecisionId, "Approval request sent to the assigned hiring manager. The candidate will not advance until the manager records a decision.");
        }

        var target = decisionValue.Equals(RecruitmentHiringDecisionStatus.Approve, StringComparison.OrdinalIgnoreCase)
            ? ActivityType.Selected
            : decisionValue.Equals(RecruitmentHiringDecisionStatus.Reject, StringComparison.OrdinalIgnoreCase)
                ? ActivityType.Rejected
                : ActivityType.OnHold;

        if (existing is null)
        {
            existing = new RecruitmentHiringDecision { CompanyId = companyId, ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, Decision = decisionValue!, Notes = command.Notes?.Trim(), LastCommandIdempotencyKey = key, StageSyncFromStage = applicant.ActivityType.ToString(), StageSyncTargetStage = target.ToString(), StageSyncStatus = "Pending", CreatedBy = actorUserId, UpdatedBy = actorUserId };
            var added = await decisions.AddOne(existing);
            if (!added.Success) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision could not be saved.");
        }
        else
        {
            var version = existing.Version <= 0 ? 1 : existing.Version;
            if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version)
                return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision was changed by another user.");
            existing.Decision = decisionValue!;
            existing.Notes = command.Notes?.Trim();
            existing.LastCommandIdempotencyKey = key;
            existing.StageSyncFromStage = applicant.ActivityType.ToString();
            existing.StageSyncTargetStage = target.ToString();
            existing.StageSyncStatus = "Pending";
            existing.Version = version + 1;
            existing.UpdatedBy = actorUserId;
            existing.UpdatedDate = DateTime.UtcNow;
            var updated = await decisions.Update(Builders<RecruitmentHiringDecision>.Filter.Where(x => x.CompanyId == companyId && x.DecisionId == existing.DecisionId && (x.Version == version || x.Version == 0)), existing);
            if (!updated.Success) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring decision was changed by another user.");
        }

        if (!await ProcessPendingStageSync(existing, actorUserId))
            return Fail(StatusCodes.Status503ServiceUnavailable, RecruitmentErrorCodes.StageSyncPending, "The hiring decision was saved, and its application stage is queued for recovery. Retry this same request.");
        return Ok(existing.DecisionId, "Hiring decision saved.");
    }

    public async Task<Result<List<RecruitmentManagerReviewQueueItem>>> PendingManagerReviews()
    {
        var companyId = CurrentContext.CompanyId(context);
        var managerId = CurrentContext.UserId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(managerId))
            return new Result<List<RecruitmentManagerReviewQueueItem>> { Success = false, StatusCode = 400, ErrorCode = RecruitmentErrorCodes.InvalidRequest, Message = "Company and user context are required." };
        if (employees is null || await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == managerId && x.Status && !x.IsDeleted) is null)
            return new Result<List<RecruitmentManagerReviewQueueItem>> { Success = false, StatusCode = 403, ErrorCode = RecruitmentErrorCodes.PermissionDenied, Message = "Only an active company employee can access hiring manager reviews." };

        var pending = (await decisions.GetAll(x => x.CompanyId == companyId && x.HiringManagerUserId == managerId && x.ManagerReviewStatus == RecruitmentManagerReviewStatus.Pending && !x.IsDeleted))
            .OrderBy(x => x.ManagerReviewRequestedAtUtc).ToList();
        var applicantIds = pending.Select(x => x.ApplicantId).Distinct().ToArray();
        var vacancyIds = pending.Select(x => x.VacancyId).Distinct().ToArray();
        var applicantRows = applicantIds.Length == 0 ? [] : (await applicants.GetAll(x => x.CompanyId == companyId && applicantIds.Contains(x.ApplicantId) && !x.IsDeleted)).ToList();
        var vacancyRows = vacancies is null || vacancyIds.Length == 0 ? [] : (await vacancies.GetAll(x => x.CompanyId == companyId && vacancyIds.Contains(x.JobId) && !x.IsDeleted)).ToList();
        var applicantMap = applicantRows.GroupBy(x => x.ApplicantId).ToDictionary(x => x.Key, x => x.First());
        var vacancyMap = vacancyRows.GroupBy(x => x.JobId).ToDictionary(x => x.Key, x => x.First());
        var items = pending.Where(x => applicantMap.ContainsKey(x.ApplicantId) && vacancyMap.ContainsKey(x.VacancyId)).Select(x =>
        {
            var candidate = applicantMap[x.ApplicantId];
            var vacancy = vacancyMap[x.VacancyId];
            return new RecruitmentManagerReviewQueueItem
            {
                DecisionId = x.DecisionId,
                ApplicantId = x.ApplicantId,
                ApplicantName = $"{candidate.FirstName} {candidate.LastName}".Trim(),
                Email = candidate.Email,
                VacancyId = x.VacancyId,
                VacancyTitle = vacancy.Title,
                RecruiterNotes = x.Notes,
                HiringManagerUserId = managerId,
                RequestedAtUtc = x.ManagerReviewRequestedAtUtc ?? x.CreatedDate ?? DateTime.UtcNow,
                Version = x.Version
            };
        }).ToList();
        return new Result<List<RecruitmentManagerReviewQueueItem>> { Success = true, StatusCode = 200, MethodResult = items, Data = items, TotalRecords = items.Count, Message = $"{items.Count} hiring manager review(s) are pending." };
    }

    public async Task<Result<string>> ReviewAsManager(RecruitmentManagerReviewCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        var key = command.IdempotencyKey?.Trim();
        var outcome = command.Decision?.Trim();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(actorUserId) || string.IsNullOrWhiteSpace(command.DecisionId) || string.IsNullOrWhiteSpace(key) || key.Length < 12 || outcome is not (RecruitmentHiringDecisionStatus.Approve or RecruitmentHiringDecisionStatus.Reject))
            return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Company, decision, manager, and a valid idempotency key are required.");
        if (outcome == RecruitmentHiringDecisionStatus.Reject && string.IsNullOrWhiteSpace(command.Notes))
            return Fail(400, RecruitmentErrorCodes.InvalidRequest, "A reason is required when the hiring manager rejects the recommendation.");
        if (employees is null || await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == actorUserId && x.Status && !x.IsDeleted) is null)
            return Fail(403, RecruitmentErrorCodes.PermissionDenied, "Only an active company employee can review a hiring decision.");

        var item = await decisions.FirstOrDefault(x => x.CompanyId == companyId && x.DecisionId == command.DecisionId.Trim() && x.HiringManagerUserId == actorUserId && !x.IsDeleted);
        if (item is null) return Fail(404, RecruitmentErrorCodes.PermissionDenied, "Review was not found or is not assigned to you.");
        if (item.LastCommandIdempotencyKey == key && item.ManagerReviewStatus is RecruitmentManagerReviewStatus.Approved or RecruitmentManagerReviewStatus.Rejected)
        {
            if (!await ApplyFinalDecisionStage(companyId, actorUserId, item)) return Fail(StatusCodes.Status503ServiceUnavailable, RecruitmentErrorCodes.StageSyncPending, "Review was recorded, and its application stage is queued for recovery. Retry this same request.");
            return Ok(item.DecisionId, "Hiring manager review already applied.");
        }
        if (item.ManagerReviewStatus != RecruitmentManagerReviewStatus.Pending)
            return Fail(409, RecruitmentErrorCodes.InvalidTransition, "This hiring manager review is no longer pending.");
        var version = item.Version <= 0 ? 1 : item.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version)
            return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring manager review changed; refresh the queue and try again.");

        var applicantForSync = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == item.ApplicantId && x.VacancyId == item.VacancyId && !x.IsDeleted);
        if (applicantForSync is null) return Fail(404, RecruitmentErrorCodes.InvalidRequest, "The candidate is no longer available for this hiring decision.");

        item.Decision = outcome!;
        item.ManagerReviewStatus = outcome == RecruitmentHiringDecisionStatus.Approve ? RecruitmentManagerReviewStatus.Approved : RecruitmentManagerReviewStatus.Rejected;
        item.ManagerReviewNotes = command.Notes?.Trim();
        item.ManagerReviewedByUserId = actorUserId;
        item.ManagerReviewedAtUtc = DateTime.UtcNow;
        item.LastCommandIdempotencyKey = key;
        item.StageSyncFromStage = applicantForSync.ActivityType.ToString();
        item.StageSyncTargetStage = (outcome == RecruitmentHiringDecisionStatus.Approve ? ActivityType.Selected : ActivityType.Rejected).ToString();
        item.StageSyncStatus = "Pending";
        item.Version = version + 1;
        item.UpdatedBy = actorUserId;
        item.UpdatedDate = DateTime.UtcNow;
        var saved = await decisions.Update(Builders<RecruitmentHiringDecision>.Filter.Where(x => x.CompanyId == companyId && x.DecisionId == item.DecisionId && x.HiringManagerUserId == actorUserId && x.ManagerReviewStatus == RecruitmentManagerReviewStatus.Pending && (x.Version == version || x.Version == 0)), item);
        if (!saved.Success) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Hiring manager review was changed by another user.");

        if (!await ApplyFinalDecisionStage(companyId, actorUserId, item))
            return Fail(StatusCodes.Status503ServiceUnavailable, RecruitmentErrorCodes.StageSyncPending, "Review was recorded, and its application stage is queued for recovery. Retry this same request.");
        return Ok(item.DecisionId, outcome == RecruitmentHiringDecisionStatus.Approve ? "Hiring manager approved the candidate; the offer step is now available." : "Hiring manager rejected the candidate and the disposition was recorded.");
    }

    private async Task<bool> ApplyFinalDecisionStage(string companyId, string actorUserId, RecruitmentHiringDecision decision)
    {
        if (decision.StageSyncStatus is "Resolved" or "Cancelled") return true;
        if (decision.StageSyncStatus != "Pending")
        {
            var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == decision.ApplicantId && x.VacancyId == decision.VacancyId && !x.IsDeleted);
            if (applicant is null) return false;
            decision.StageSyncFromStage = applicant.ActivityType.ToString();
            decision.StageSyncTargetStage = (decision.Decision == RecruitmentHiringDecisionStatus.Approve ? ActivityType.Selected : ActivityType.Rejected).ToString();
            decision.StageSyncStatus = "Pending";
            var version = decision.Version <= 0 ? 1 : decision.Version;
            var queued = await decisions.Update(Builders<RecruitmentHiringDecision>.Filter.Where(x => x.CompanyId == companyId && x.DecisionId == decision.DecisionId && (x.Version == version || x.Version == 0)), decision);
            if (queued?.Success != true) return false;
        }
        return await ProcessPendingStageSync(decision, actorUserId);
    }

    public async Task<int> RecoverPending(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        var pending = await decisions.GetCollection()
            .Find(x => x.StageSyncStatus == "Pending" && !x.IsDeleted)
            .SortBy(x => x.UpdatedDate)
            .Limit(Math.Clamp(batchSize, 1, 200))
            .ToListAsync(cancellationToken);
        var recovered = 0;
        foreach (var decision in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await ProcessPendingStageSync(decision, decision.UpdatedBy ?? decision.CreatedBy ?? "hiring-decision-recovery")) recovered++;
        }
        return recovered;
    }

    private async Task<bool> ProcessPendingStageSync(RecruitmentHiringDecision decision, string actorUserId)
    {
        if (!string.Equals(decision.StageSyncStatus, "Pending", StringComparison.OrdinalIgnoreCase)) return true;
        if (!Enum.TryParse<ActivityType>(decision.StageSyncTargetStage, true, out var target) || !Enum.IsDefined(target)) return false;

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == decision.CompanyId && x.ApplicantId == decision.ApplicantId && x.VacancyId == decision.VacancyId && !x.IsDeleted);
        if (applicant is null || string.Equals(applicant.ConsentStatus, "Anonymized", StringComparison.OrdinalIgnoreCase))
            return await SetStageSyncStatus(decision, "Cancelled");

        var reason = decision.ManagerReviewStatus is RecruitmentManagerReviewStatus.Approved or RecruitmentManagerReviewStatus.Rejected
            ? $"Hiring manager decision: {decision.Decision}"
            : $"Hiring decision: {decision.Decision}";
        var fromStage = decision.StageSyncFromStage ?? applicant.ActivityType.ToString();
        var canonical = applications is null ? null : await applications.FirstOrDefault(x => x.CompanyId == decision.CompanyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted);
        var preserveAdvancedCanonicalStage = false;
        if (applications is not null && canonical is not null && !string.Equals(canonical.CurrentStage, target.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            if (IsAtOrBeforeStage(canonical.CurrentStage, fromStage))
            {
                var previousVersion = canonical.Version;
                canonical.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = canonical.CurrentStage, ToStage = target.ToString(), ActorUserId = actorUserId, Reason = reason });
                canonical.CurrentStage = target.ToString();
                canonical.Version = (previousVersion <= 0 ? 1 : previousVersion) + 1;
                canonical.UpdatedBy = actorUserId;
                canonical.UpdatedDate = DateTime.UtcNow;
                var updated = await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == decision.CompanyId && x.ApplicationId == canonical.ApplicationId && !x.IsDeleted && (x.Version == previousVersion || x.Version == 0)), canonical);
                if (updated?.Success != true) return false;
            }
            else
            {
                // A custom stage or a later recognized stage is owned by its workflow and is not regressed by recovery.
                preserveAdvancedCanonicalStage = true;
            }
        }
        else if (applications is not null && canonical is null)
        {
            var added = await applications.AddOne(new RecruitmentJobApplication
            {
                CompanyId = decision.CompanyId,
                ApplicantId = applicant.ApplicantId,
                VacancyId = applicant.VacancyId ?? string.Empty,
                CurrentStage = target.ToString(),
                CreatedBy = actorUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedBy = actorUserId,
                UpdatedDate = DateTime.UtcNow,
                StageHistory = fromStage == target.ToString() ? [] : [new RecruitmentApplicationStageEvent { FromStage = fromStage, ToStage = target.ToString(), ActorUserId = actorUserId, Reason = reason }]
            });
            if (added?.Success != true)
            {
                var afterConflict = await applications.FirstOrDefault(x => x.CompanyId == decision.CompanyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted);
                if (afterConflict is null) return false;
                if (!string.Equals(afterConflict.CurrentStage, target.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    if (IsAtOrBeforeStage(afterConflict.CurrentStage, fromStage))
                    {
                        var previousVersion = afterConflict.Version;
                        afterConflict.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = afterConflict.CurrentStage, ToStage = target.ToString(), ActorUserId = actorUserId, Reason = reason });
                        afterConflict.CurrentStage = target.ToString();
                        afterConflict.Version = (previousVersion <= 0 ? 1 : previousVersion) + 1;
                        afterConflict.UpdatedBy = actorUserId;
                        afterConflict.UpdatedDate = DateTime.UtcNow;
                        var updated = await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == decision.CompanyId && x.ApplicationId == afterConflict.ApplicationId && !x.IsDeleted && (x.Version == previousVersion || x.Version == 0)), afterConflict);
                        if (updated?.Success != true) return false;
                    }
                    else
                    {
                        preserveAdvancedCanonicalStage = true;
                    }
                }
            }
        }

        if (!preserveAdvancedCanonicalStage && applicant.ActivityType != target && IsAtOrBeforeStage(applicant.ActivityType.ToString(), fromStage))
        {
            var previous = applicant.ActivityType;
            var applicantVersion = applicant.Version;
            applicant.ActivityType = target;
            applicant.UpdatedBy = actorUserId;
            applicant.UpdatedDate = DateTime.UtcNow;
            var updated = await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == decision.CompanyId && x.ApplicantId == applicant.ApplicantId && x.VacancyId == applicant.VacancyId && !x.IsDeleted && x.ActivityType == previous && (x.Version == applicantVersion || x.Version == 0)), applicant);
            if (updated?.Success != true) return false;
        }

        var logKey = $"hiring-decision-stage:{decision.DecisionId}";
        var existingLog = await logs.FirstOrDefault(x => x.CompanyId == decision.CompanyId && x.IdempotencyKey == logKey && !x.IsDeleted);
        if (existingLog is null)
        {
            var managerDecision = decision.ManagerReviewStatus is RecruitmentManagerReviewStatus.Approved or RecruitmentManagerReviewStatus.Rejected;
            var log = new ApplicantLogs
            {
                CompanyId = decision.CompanyId,
                ApplicantId = applicant.ApplicantId,
                ApplicantName = $"{applicant.FirstName} {applicant.LastName}".Trim(),
                JobRole = applicant.VacancyId,
                UserId = actorUserId,
                ActivityCategory = 0,
                Description = $"{reason}. {(managerDecision ? decision.ManagerReviewNotes : decision.Notes)?.Trim()}",
                EventType = managerDecision ? "HiringManagerDecisionRecorded" : "HiringDecisionRecorded",
                PreviousStage = fromStage,
                CurrentStage = preserveAdvancedCanonicalStage ? canonical!.CurrentStage : target.ToString(),
                IdempotencyKey = logKey,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId
            };
            var addedLog = await logs.AddOne(log);
            if (addedLog?.Success != true && await logs.FirstOrDefault(x => x.CompanyId == decision.CompanyId && x.IdempotencyKey == logKey && !x.IsDeleted) is null) return false;
        }

        return await SetStageSyncStatus(decision, "Resolved");
    }

    private async Task<bool> SetStageSyncStatus(RecruitmentHiringDecision decision, string status)
    {
        var version = decision.Version <= 0 ? 1 : decision.Version;
        decision.StageSyncStatus = status;
        var updated = await decisions.Update(
            Builders<RecruitmentHiringDecision>.Filter.Where(x => x.CompanyId == decision.CompanyId && x.DecisionId == decision.DecisionId && x.StageSyncStatus == "Pending" && (x.Version == version || x.Version == 0)),
            decision);
        return updated?.Success == true;
    }

    private static int StageRank(ActivityType stage) => stage switch
    {
        ActivityType.New => 0,
        ActivityType.InProgress => 1,
        ActivityType.OnHold => 2,
        ActivityType.Shortlisted => 3,
        ActivityType.Selected => 4,
        ActivityType.Rejected => 5,
        _ => 1
    };

    private static bool TryGetStageRank(string? stage, out int rank)
    {
        if (Enum.TryParse<ActivityType>(stage, true, out var parsed) && Enum.IsDefined(parsed))
        {
            rank = StageRank(parsed);
            return true;
        }
        rank = -1;
        return false;
    }

    private static bool IsAtOrBeforeStage(string? currentStage, string? expectedStage)
    {
        if (string.Equals(currentStage, expectedStage, StringComparison.OrdinalIgnoreCase)) return true;
        return TryGetStageRank(currentStage, out var currentRank) &&
               TryGetStageRank(expectedStage, out var expectedRank) &&
               currentRank <= expectedRank;
    }

    private static Result<string> Ok(string id, string msg) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = msg };
    private static Result<string> Fail(int status, string code, string msg) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = msg };
}
