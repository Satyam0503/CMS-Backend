using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Calendar;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentInterviewService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentInterview> interviews,
    ICalendarServices calendarServices,
    IHttpContextAccessor context,
    IMongoDbRepository<EmpUser>? employees = null) : IRecruitmentInterviewService
{
    public async Task<Result<string>> Upsert(RecruitmentInterviewCommand command, string actorUserId)
    {
        var result = new Result<string> { Success = false, StatusCode = StatusCodes.Status400BadRequest };
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Company and idempotency key are required.");

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && !x.IsDeleted);
        if (applicant is null)
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Applicant was not found in the current company.");

        var panelists = (command.PanelistUserIds ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (panelists.Length > 12) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "An interview panel cannot contain more than 12 interviewers.");
        if (panelists.Length > 0 && employees is not null)
        {
            var eligible = await employees.GetAll(x => x.CompanyId == companyId && x.Status && panelists.Contains(x.UserId));
            if (eligible.Count() != panelists.Length) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "One or more interviewers are not active in the current company.");
        }

        var startsAtUtc = DateTime.SpecifyKind(command.StartsAtUtc, DateTimeKind.Utc);
        var endsAtUtc = DateTime.SpecifyKind(command.EndsAtUtc, DateTimeKind.Utc);
        var requestedStatus = string.IsNullOrWhiteSpace(command.Status) ? RecruitmentInterviewStatus.Draft : command.Status.Trim();
        if (requestedStatus is RecruitmentInterviewStatus.Scheduled or RecruitmentInterviewStatus.Confirmed && panelists.Length == 0)
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Select at least one active interviewer before scheduling.");
        if (requestedStatus is RecruitmentInterviewStatus.Scheduled or RecruitmentInterviewStatus.Confirmed && startsAtUtc <= DateTime.UtcNow)
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Scheduled interviews must start in the future.");
        if (!RecruitmentInterviewRules.IsValidSchedule(startsAtUtc, endsAtUtc))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Interview end must be after start and both timestamps must be UTC.");

        var idempotencyKey = command.IdempotencyKey.Trim();
        var existing = string.IsNullOrWhiteSpace(command.InterviewId)
            ? await interviews.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && x.LastCommandIdempotencyKey == idempotencyKey && !x.IsDeleted)
            : await interviews.FirstOrDefault(x => x.CompanyId == companyId && x.InterviewId == command.InterviewId && !x.IsDeleted);

        if (existing is not null && string.Equals(existing.LastCommandIdempotencyKey, idempotencyKey, StringComparison.Ordinal))
        {
            if (existing.Status is RecruitmentInterviewStatus.Scheduled or RecruitmentInterviewStatus.Confirmed && existing.CalendarSyncStatus != "Synced")
                await SyncCalendar(existing, actorUserId);
            return Ok(result, existing.InterviewId, "Interview command already applied.");
        }

        if (existing is null)
        {
            var status = string.IsNullOrWhiteSpace(command.Status) ? RecruitmentInterviewStatus.Draft : command.Status.Trim();
            if (!RecruitmentInterviewRules.IsAllowedTransition(RecruitmentInterviewStatus.Draft, status))
                return Fail(result, RecruitmentErrorCodes.InvalidTransition, "Interview status transition is not allowed.", StatusCodes.Status409Conflict);

            var interview = new RecruitmentInterview
            {
                CompanyId = companyId,
                ApplicantId = command.ApplicantId,
                VacancyId = command.VacancyId,
                Title = command.Title.Trim(),
                RoundNumber = command.RoundNumber <= 0 ? 1 : command.RoundNumber,
                RoundType = command.RoundType?.Trim(),
                StartsAtUtc = startsAtUtc,
                EndsAtUtc = endsAtUtc,
                TimeZoneId = command.TimeZoneId.Trim(),
                Location = command.Location?.Trim(),
                MeetingUrl = command.MeetingUrl?.Trim(),
                PanelistUserIds = panelists,
                QuorumRequired = Math.Max(1, (panelists.Length + 1) / 2),
                Status = status,
                LastCommandIdempotencyKey = idempotencyKey,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId
            };
            var saved = await interviews.AddOne(interview);
            if (!saved.Success)
                return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Interview could not be created.", StatusCodes.Status409Conflict);
            if (interview.Status is RecruitmentInterviewStatus.Scheduled or RecruitmentInterviewStatus.Confirmed)
                await SyncCalendar(interview, actorUserId);
            return Ok(result, interview.InterviewId, interview.CalendarSyncStatus == "Synced" ? "Interview created and added to calendar." : "Interview created; calendar sync is pending.");
        }

        var nextStatus = string.IsNullOrWhiteSpace(command.Status) ? existing.Status : command.Status.Trim();
        if (!RecruitmentInterviewRules.IsAllowedTransition(existing.Status, nextStatus))
            return Fail(result, RecruitmentErrorCodes.InvalidTransition, "Interview status transition is not allowed.", StatusCodes.Status409Conflict);
        var currentVersion = existing.Version <= 0 ? 1 : existing.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != currentVersion)
            return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Interview was changed by another user.", StatusCodes.Status409Conflict);

        existing.Title = command.Title.Trim();
        existing.RoundNumber = command.RoundNumber <= 0 ? existing.RoundNumber : command.RoundNumber;
        existing.RoundType = command.RoundType?.Trim();
        existing.StartsAtUtc = startsAtUtc;
        existing.EndsAtUtc = endsAtUtc;
        existing.TimeZoneId = command.TimeZoneId.Trim();
        existing.Location = command.Location?.Trim();
        existing.MeetingUrl = command.MeetingUrl?.Trim();
        existing.PanelistUserIds = panelists;
        existing.QuorumRequired = Math.Max(1, (panelists.Length + 1) / 2);
        existing.Status = nextStatus;
        existing.LastCommandIdempotencyKey = idempotencyKey;
        existing.Version = currentVersion + 1;
        existing.UpdatedBy = actorUserId;
        existing.UpdatedDate = DateTime.UtcNow;

        var updated = await interviews.Update(Builders<RecruitmentInterview>.Filter.Where(x => x.CompanyId == companyId && x.InterviewId == existing.InterviewId && (x.Version == currentVersion || x.Version == 0)), existing);
        if (!updated.Success)
            return Fail(result, RecruitmentErrorCodes.ConcurrencyConflict, "Interview was changed by another user.", StatusCodes.Status409Conflict);
        if (existing.Status is RecruitmentInterviewStatus.Scheduled or RecruitmentInterviewStatus.Confirmed)
            await SyncCalendar(existing, actorUserId);
        return Ok(result, existing.InterviewId, existing.CalendarSyncStatus == "Synced" ? "Interview updated and calendar synchronized." : "Interview updated; calendar sync is pending.");
    }

    public async Task<Result<List<RecruitmentInterview>>> GetRounds(string applicantId, string vacancyId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(applicantId) || string.IsNullOrWhiteSpace(vacancyId)) return new Result<List<RecruitmentInterview>> { Success = false, StatusCode = 400, Message = "Applicant and vacancy are required." };
        var rows = (await interviews.GetAll(x => x.CompanyId == companyId && x.ApplicantId == applicantId.Trim() && x.VacancyId == vacancyId.Trim() && !x.IsDeleted)).OrderByDescending(x => x.RoundNumber).ThenByDescending(x => x.StartsAtUtc).ToList();
        return new Result<List<RecruitmentInterview>> { Success = true, StatusCode = 200, MethodResult = rows, Data = rows, TotalRecords = rows.Count, Message = rows.Count == 0 ? "No interview rounds found." : $"{rows.Count} interview round(s) loaded." };
    }

    public async Task<Result<string>> RetryCalendar(string interviewId, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(interviewId)) return Fail(new Result<string>(), RecruitmentErrorCodes.InvalidRequest, "Company and interview are required.");
        var interview = await interviews.FirstOrDefault(x => x.CompanyId == companyId && x.InterviewId == interviewId.Trim() && !x.IsDeleted);
        if (interview is null) return Fail(new Result<string>(), RecruitmentErrorCodes.InvalidRequest, "Interview was not found in the current company.");
        await SyncCalendar(interview, actorUserId);
        return Ok(new Result<string>(), interview.InterviewId, interview.CalendarSyncStatus == "Synced" ? "Interview calendar synchronized." : "Interview calendar sync is still pending.");
    }

    private async Task SyncCalendar(RecruitmentInterview interview, string actorUserId)
    {
        var sync = await calendarServices.AddUpdateCalendarItem(new CalendarRequestDto
        {
            Id = interview.CalendarItemId,
            Name = $"Interview: {interview.Title}",
            Date = interview.StartsAtUtc.Date,
            StartTimeUtc = interview.StartsAtUtc,
            EndTimeUtc = interview.EndsAtUtc,
            Type = CalendarItem.Event,
            Recurring = false,
            Description = $"Applicant {interview.ApplicantId}; {interview.StartsAtUtc:u} - {interview.EndsAtUtc:u}; panel: {string.Join(", ", interview.PanelistUserIds)}"
        });
        interview.UpdatedBy = actorUserId;
        interview.CalendarSyncStatus = sync.Success && !string.IsNullOrWhiteSpace(sync.ResourceId) ? "Synced" : "Pending";
        if (sync.Success && !string.IsNullOrWhiteSpace(sync.ResourceId)) interview.CalendarItemId = sync.ResourceId;
        await interviews.Update(Builders<RecruitmentInterview>.Filter.Where(x => x.CompanyId == interview.CompanyId && x.InterviewId == interview.InterviewId), interview);
    }

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






