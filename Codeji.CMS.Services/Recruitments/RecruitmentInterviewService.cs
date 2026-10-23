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
using Codeji.CMS.Utility;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Recruitments;

public sealed class RecruitmentInterviewService(
    IMongoDbRepository<Applicant> applicants,
    IMongoDbRepository<RecruitmentInterview> interviews,
    ICalendarServices calendarServices,
    IHttpContextAccessor context,
    IMongoDbRepository<EmpUser>? employees = null,
    IMongoDbRepository<JobVacancy>? vacancies = null,
    IRecruitmentExternalActionService? externalActions = null,
    IMongoDbRepository<ApplicantLogs>? applicantLogs = null) : IRecruitmentInterviewService
{
    public async Task<Result<string>> Upsert(RecruitmentInterviewCommand command, string actorUserId)
    {
        var result = new Result<string> { Success = false, StatusCode = StatusCodes.Status400BadRequest };
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Company and idempotency key are required.");

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && x.VacancyId == command.VacancyId && !x.IsDeleted);
        if (applicant is null)
            return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Applicant was not found in the current company.");

        var panelists = (command.PanelistUserIds ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (panelists.Length > 12) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "An interview panel cannot contain more than 12 interviewers.");
        if (command.RoundNumber is < 1 or > 50) return Fail(result, RecruitmentErrorCodes.InvalidRequest, "Interview round must be between 1 and 50.");
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

        if (existing is null && command.RoundNumber > 1)
        {
            var priorRounds = (await interviews.GetAll(x => x.CompanyId == companyId && x.ApplicantId == command.ApplicantId && x.VacancyId == command.VacancyId && !x.IsDeleted && x.RoundNumber < command.RoundNumber)).ToList();
            for (var round = 1; round < command.RoundNumber; round++)
            {
                var prior = priorRounds.Where(x => x.RoundNumber == round).OrderByDescending(x => x.UpdatedDate).FirstOrDefault();
                if (prior is null || prior.Status != RecruitmentInterviewStatus.Completed || prior.DebriefStatus != RecruitmentDebriefStatus.Complete)
                    return Fail(result, RecruitmentErrorCodes.InvalidTransition, $"Complete and debrief interview round {round} before scheduling round {command.RoundNumber}.", StatusCodes.Status409Conflict);
            }
        }

        if (existing is not null && string.Equals(existing.LastCommandIdempotencyKey, idempotencyKey, StringComparison.Ordinal))
        {
            if (existing.Status is RecruitmentInterviewStatus.Scheduled or RecruitmentInterviewStatus.Confirmed && existing.CalendarSyncStatus != "Synced")
            {
            await SyncCalendar(existing, actorUserId);
            await NotifyParticipantsAsync(existing, applicant, actorUserId, companyId);
        }
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
                TimeZoneId = (command.TimeZoneId ?? "UTC").Trim(),
                Location = !string.IsNullOrWhiteSpace(command.Location) && !command.Location.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? command.Location.Trim()
                    : (!string.IsNullOrWhiteSpace(command.Location) ? command.Location.Trim() : (command.MeetingUrl != null ? "Virtual (Video Call)" : null)),
                MeetingUrl = !string.IsNullOrWhiteSpace(command.MeetingUrl)
                    ? command.MeetingUrl.Trim()
                    : (command.Location?.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase) == true ? command.Location.Trim() : null),
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
            {
                await SyncCalendar(interview, actorUserId);
                await NotifyParticipantsAsync(interview, applicant, actorUserId, companyId);
            }
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
        existing.TimeZoneId = (command.TimeZoneId ?? "UTC").Trim();
        existing.Location = !string.IsNullOrWhiteSpace(command.Location) && !command.Location.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? command.Location.Trim()
            : (!string.IsNullOrWhiteSpace(command.Location) ? command.Location.Trim() : (command.MeetingUrl != null ? "Virtual (Video Call)" : existing.Location));
        existing.MeetingUrl = !string.IsNullOrWhiteSpace(command.MeetingUrl)
            ? command.MeetingUrl.Trim()
            : (command.Location?.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase) == true ? command.Location.Trim() : existing.MeetingUrl);
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
        {
            await SyncCalendar(existing, actorUserId);
            await NotifyParticipantsAsync(existing, applicant, actorUserId, companyId);
        }
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
        var targetUsers = (interview.PanelistUserIds ?? [])
            .Concat([actorUserId, interview.CreatedBy ?? string.Empty])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var applicant = await applicants.FirstOrDefault(x => x.CompanyId == interview.CompanyId && x.ApplicantId == interview.ApplicantId && !x.IsDeleted);
        var candidateName = applicant != null ? $"{applicant.FirstName} {applicant.LastName}".Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(candidateName)) candidateName = applicant?.Email ?? "Candidate";

        var roleTitle = "Candidate";
        if (vacancies is not null && !string.IsNullOrWhiteSpace(interview.VacancyId))
        {
            var vac = await vacancies.FirstOrDefault(x => x.CompanyId == interview.CompanyId && (x.JobId == interview.VacancyId || x.PublicJobId == interview.VacancyId) && !x.IsDeleted);
            if (vac is not null && !string.IsNullOrWhiteSpace(vac.Title)) roleTitle = vac.Title;
        }

        var meetingInfo = !string.IsNullOrWhiteSpace(interview.MeetingUrl) ? interview.MeetingUrl : (!string.IsNullOrWhiteSpace(interview.Location) ? interview.Location : "Virtual");
        var panelistNames = string.Join(", ", interview.PanelistUserIds ?? []);

        var sync = await calendarServices.AddUpdateCalendarItem(new CalendarRequestDto
        {
            Id = interview.CalendarItemId,
            Name = $"Interview: {candidateName} - {interview.Title}",
            Date = interview.StartsAtUtc.Date,
            StartTimeUtc = interview.StartsAtUtc,
            EndTimeUtc = interview.EndsAtUtc,
            Type = CalendarItem.Event,
            Recurring = false,
            Description = $"Candidate: {candidateName} | Role: {roleTitle} | Round {interview.RoundNumber} ({interview.RoundType ?? "Interview"}) | Time: {interview.StartsAtUtc:yyyy-MM-dd HH:mm} UTC | Location: {meetingInfo} | Panel: {panelistNames} | ApplicantId: {interview.ApplicantId} | VacancyId: {interview.VacancyId} | InterviewId: {interview.InterviewId}",
            TargetUserIds = targetUsers,
            RestrictToTargetUsersAndHrAdmin = true
        });
        interview.UpdatedBy = actorUserId;
        interview.CalendarSyncStatus = sync.Success && !string.IsNullOrWhiteSpace(sync.ResourceId) ? "Synced" : "Pending";
        if (sync.Success && !string.IsNullOrWhiteSpace(sync.ResourceId)) interview.CalendarItemId = sync.ResourceId;
        await interviews.Update(Builders<RecruitmentInterview>.Filter.Where(x => x.CompanyId == interview.CompanyId && x.InterviewId == interview.InterviewId), interview);
    }

    private async Task NotifyParticipantsAsync(RecruitmentInterview interview, Applicant applicant, string actorUserId, string companyId)
    {
        try
        {
            var candidateName = $"{applicant.FirstName} {applicant.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(candidateName)) candidateName = applicant.Email ?? "Candidate";

            var roleTitle = "Candidate";
            if (vacancies is not null && !string.IsNullOrWhiteSpace(interview.VacancyId))
            {
                var vac = await vacancies.FirstOrDefault(x => x.CompanyId == companyId && (x.JobId == interview.VacancyId || x.PublicJobId == interview.VacancyId) && !x.IsDeleted);
                if (vac is not null && !string.IsNullOrWhiteSpace(vac.Title)) roleTitle = vac.Title;
            }

            var meetingInfo = !string.IsNullOrWhiteSpace(interview.MeetingUrl) ? interview.MeetingUrl : (!string.IsNullOrWhiteSpace(interview.Location) ? interview.Location : "Virtual / Online");
            string? resumeUrl = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(applicant.ResumeUrl))
                {
                    resumeUrl = applicant.ResumeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? applicant.ResumeUrl
                        : (Common.GetApplicantResumeFullPath(applicant.ResumeUrl) ?? applicant.ResumeUrl);
                }
            }
            catch
            {
                resumeUrl = applicant.ResumeUrl;
            }
            var scheduleFormatted = $"{interview.StartsAtUtc:dddd, MMMM dd, yyyy · hh:mm tt} UTC";

            // 1. Queue email to each panelist / interviewer
            if (externalActions is not null && interview.PanelistUserIds is { Length: > 0 } && employees is not null)
            {
                var panelistEmployees = await employees.GetAll(x => x.CompanyId == companyId && interview.PanelistUserIds.Contains(x.UserId) && !x.IsDeleted);
                foreach (var panelist in panelistEmployees)
                {
                    if (string.IsNullOrWhiteSpace(panelist.Email)) continue;

                    var panelistName = $"{panelist.FirstName} {panelist.LastName}".Trim();
                    var meetingHtml = !string.IsNullOrWhiteSpace(interview.MeetingUrl)
                        ? $"<p style=\"margin:8px 0;\"><strong>Meeting Link:</strong> <a href=\"{interview.MeetingUrl}\" target=\"_blank\" style=\"color:#2563EB;font-weight:600;\">{interview.MeetingUrl}</a></p>"
                        : $"<p style=\"margin:8px 0;\"><strong>Location:</strong> {interview.Location ?? "Virtual"}</p>";

                    var resumeHtml = !string.IsNullOrWhiteSpace(resumeUrl)
                        ? $"<p style=\"margin:8px 0;\"><strong>Candidate Resume:</strong> <a href=\"{resumeUrl}\" target=\"_blank\" style=\"color:#2563EB;font-weight:600;\">View Candidate CV / Resume</a></p>"
                        : string.Empty;

                    var panelistBody = $@"
<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #1E293B; max-width: 600px; margin: 0 auto; border: 1px solid #E2E8F0; border-radius: 8px; overflow: hidden;"">
    <div style=""background-color: #0F172A; padding: 20px 24px; color: #FFFFFF;"">
        <h2 style=""margin: 0; font-size: 1.25rem; font-weight: 700;"">Interview Kit &amp; Evaluation Brief</h2>
        <p style=""margin: 4px 0 0 0; color: #94A3B8; font-size: 0.875rem;"">Round {interview.RoundNumber}: {interview.Title}</p>
    </div>
    <div style=""padding: 24px;"">
        <p>Hello <strong>{panelistName}</strong>,</p>
        <p>You have been assigned as an interviewer for <strong>{candidateName}</strong> applying for the position of <strong>{roleTitle}</strong>.</p>
        
        <div style=""background-color: #F8FAFC; border-left: 4px solid #3B82F6; padding: 16px; border-radius: 4px; margin: 16px 0;"">
            <h3 style=""margin: 0 0 10px 0; font-size: 1rem; color: #1E293B;"">Interview Details</h3>
            <p style=""margin: 4px 0;""><strong>Candidate:</strong> {candidateName}</p>
            <p style=""margin: 4px 0;""><strong>Role:</strong> {roleTitle}</p>
            <p style=""margin: 4px 0;""><strong>Experience:</strong> {applicant.Experience} years</p>
            <p style=""margin: 4px 0;""><strong>Round:</strong> Round {interview.RoundNumber} ({interview.RoundType ?? "Technical"})</p>
            <p style=""margin: 4px 0;""><strong>Date &amp; Time:</strong> {scheduleFormatted} ({interview.TimeZoneId})</p>
            {meetingHtml}
            {resumeHtml}
        </div>

        <div style=""background-color: #FEF3C7; border: 1px solid #FDE68A; padding: 14px; border-radius: 6px; margin: 16px 0;"">
            <p style=""margin: 0; font-size: 0.875rem; color: #92400E;"">
                <strong>Scorecard Instructions:</strong> Please review the candidate's resume prior to the session. Following the interview, please log in to Codeji CMS to submit your competency ratings, feedback notes, and hiring recommendation.
            </p>
        </div>

        <p style=""font-size: 0.8125rem; color: #64748B; margin-top: 24px; border-top: 1px solid #E2E8F0; padding-top: 12px;"">
            This notification was dispatched automatically by Codeji Recruitment ATS.
        </p>
    </div>
</div>";

                    await externalActions.QueueEmail(new RecruitmentEmailQueueCommand
                    {
                        DedupeKey = $"interview-panelist:{interview.InterviewId}:{panelist.UserId}:{interview.Version}",
                        EventType = "InterviewPanelistInvited",
                        RecipientEmail = panelist.Email.Trim(),
                        RecipientUserId = panelist.UserId,
                        Subject = $"[Interview Kit] {candidateName} - {roleTitle} (Round {interview.RoundNumber})",
                        HtmlBody = panelistBody
                    }, actorUserId);
                }
            }

            // 2. Queue email to the candidate
            if (externalActions is not null && !string.IsNullOrWhiteSpace(applicant.Email))
            {
                var meetingCandidateHtml = !string.IsNullOrWhiteSpace(interview.MeetingUrl)
                    ? $"<p style=\"margin:8px 0;\"><strong>Video Meeting Link:</strong> <a href=\"{interview.MeetingUrl}\" target=\"_blank\" style=\"color:#2563EB;font-weight:600;\">{interview.MeetingUrl}</a></p>"
                    : $"<p style=\"margin:8px 0;\"><strong>Location:</strong> {interview.Location ?? "Virtual / Online"}</p>";

                var candidateBody = $@"
<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #1E293B; max-width: 600px; margin: 0 auto; border: 1px solid #E2E8F0; border-radius: 8px; overflow: hidden;"">
    <div style=""background-color: #0F172A; padding: 20px 24px; color: #FFFFFF;"">
        <h2 style=""margin: 0; font-size: 1.25rem; font-weight: 700;"">Interview Confirmation</h2>
        <p style=""margin: 4px 0 0 0; color: #94A3B8; font-size: 0.875rem;"">{roleTitle} Application</p>
    </div>
    <div style=""padding: 24px;"">
        <p>Dear <strong>{candidateName}</strong>,</p>
        <p>Thank you for your interest in joining our team. Your interview for the position of <strong>{roleTitle}</strong> has been scheduled.</p>
        
        <div style=""background-color: #F8FAFC; border-left: 4px solid #10B981; padding: 16px; border-radius: 4px; margin: 16px 0;"">
            <h3 style=""margin: 0 0 10px 0; font-size: 1rem; color: #1E293B;"">Schedule Summary</h3>
            <p style=""margin: 4px 0;""><strong>Round:</strong> Round {interview.RoundNumber} ({interview.RoundType ?? "Interview"})</p>
            <p style=""margin: 4px 0;""><strong>Date &amp; Time:</strong> {scheduleFormatted} ({interview.TimeZoneId})</p>
            {meetingCandidateHtml}
        </div>

        <p style=""font-size: 0.875rem; color: #475569;"">
            Please ensure your audio, camera, and internet connection are tested prior to joining. If you have any questions or require rescheduling, please reply directly to this email.
        </p>
        <p style=""font-size: 0.875rem; color: #475569;"">We look forward to speaking with you!</p>
        <p style=""font-size: 0.875rem; color: #64748B; margin-top: 24px; border-top: 1px solid #E2E8F0; padding-top: 12px;"">
            Kind regards,<br/>
            <strong>Talent Acquisition Team</strong>
        </p>
    </div>
</div>";

                await externalActions.QueueEmail(new RecruitmentEmailQueueCommand
                {
                    DedupeKey = $"interview-candidate:{interview.InterviewId}:{interview.Version}",
                    EventType = "InterviewCandidateInvited",
                    RecipientEmail = applicant.Email.Trim(),
                    Subject = $"Interview Scheduled: {roleTitle} (Round {interview.RoundNumber})",
                    HtmlBody = candidateBody
                }, actorUserId);
            }

            // 3. Record audit trail in ApplicantLogs
            if (applicantLogs is not null)
            {
                await applicantLogs.AddOne(new ApplicantLogs
                {
                    CompanyId = companyId,
                    ApplicantId = applicant.ApplicantId,
                    ApplicantName = candidateName,
                    JobRole = roleTitle,
                    UserId = actorUserId,
                    ActivityCategory = 0,
                    Description = $"Interview Round {interview.RoundNumber} ({interview.Title}) scheduled for {scheduleFormatted}. Panelists: {interview.PanelistUserIds?.Length ?? 0}. Meeting: {meetingInfo}.",
                    EventType = "InterviewScheduled",
                    PreviousStage = applicant.ActivityType.ToString(),
                    CurrentStage = "Interview",
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId
                });
            }
        }
        catch
        {
            // Non-blocking notification telemetry
        }
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






