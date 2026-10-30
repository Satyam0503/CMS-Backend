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

public sealed class RecruitmentInterviewDebriefService(
    IMongoDbRepository<RecruitmentInterview> interviews,
    IMongoDbRepository<RecruitmentScorecard> scorecards,
    IHttpContextAccessor context,
    IMongoDbRepository<RecruitmentJobApplication>? applications = null,
    IMongoDbRepository<Applicant>? applicants = null) : IRecruitmentInterviewDebriefService
{
    private static readonly HashSet<string> Decisions = new(StringComparer.OrdinalIgnoreCase) { "Advance", "Hold", "Reject" };

    public async Task<Result<string>> Complete(RecruitmentInterviewDebriefCommand command, string actorUserId)
    {
        var companyId = CurrentContext.CompanyId(context);
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(command.IdempotencyKey) || !Decisions.Contains(command.Decision?.Trim() ?? string.Empty)) return Fail(400, RecruitmentErrorCodes.InvalidRequest, "Company, idempotency key, and a valid debrief decision are required.");
        var interview = await interviews.FirstOrDefault(x => x.CompanyId == companyId && x.InterviewId == command.InterviewId.Trim() && !x.IsDeleted);
        if (interview is null) return Fail(404, RecruitmentErrorCodes.InvalidRequest, "Interview was not found in the current company.");
        if (string.Equals(interview.LastCommandIdempotencyKey, command.IdempotencyKey.Trim(), StringComparison.Ordinal)) return Ok(interview.InterviewId, "Interview debrief already applied.");
        var feedback = (await scorecards.GetAll(x => x.CompanyId == companyId && x.InterviewId == interview.InterviewId && !x.IsDeleted && (x.Status == RecruitmentScorecardStatus.Submitted || x.Status == RecruitmentScorecardStatus.Locked))).ToList();
        var assignedReviewers = interview.PanelistUserIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var eligibleFeedback = feedback
            .Where(x => assignedReviewers.Count == 0 || assignedReviewers.Contains(x.ReviewerUserId))
            .GroupBy(x => x.ReviewerUserId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var assigned = assignedReviewers.Count;
        var quorum = interview.QuorumRequired > 0 ? interview.QuorumRequired : Math.Max(1, (assigned + 1) / 2);
        if (eligibleFeedback.Count < quorum) return Fail(409, RecruitmentErrorCodes.InvalidTransition, $"Debrief requires {quorum} submitted interviewer scorecard(s); {eligibleFeedback.Count} available.");
        var version = interview.Version <= 0 ? 1 : interview.Version;
        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != version) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Interview was changed by another user.");
        interview.DebriefStatus = RecruitmentDebriefStatus.Complete; interview.DebriefDecision = command.Decision.Trim(); interview.DebriefedByUserId = actorUserId; interview.DebriefedAtUtc = DateTime.UtcNow; interview.LastCommandIdempotencyKey = command.IdempotencyKey.Trim(); interview.Status = RecruitmentInterviewStatus.Completed; interview.Version = version + 1; interview.UpdatedBy = actorUserId; interview.UpdatedDate = DateTime.UtcNow;
        if (applications is not null)
        {
            var application = await applications.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == interview.ApplicantId && x.VacancyId == interview.VacancyId && !x.IsDeleted);
            if (application is not null)
            {
                var nextStage = interview.DebriefDecision.Equals("Reject", StringComparison.OrdinalIgnoreCase) ? "Rejected" : interview.DebriefDecision.Equals("Hold", StringComparison.OrdinalIgnoreCase) ? "OnHold" : "Shortlisted";
                var current = application.CurrentStage;
                if (!string.Equals(current, "Rejected", StringComparison.OrdinalIgnoreCase) && !string.Equals(current, "Selected", StringComparison.OrdinalIgnoreCase) && !string.Equals(current, nextStage, StringComparison.OrdinalIgnoreCase))
                {
                    application.StageHistory.Add(new RecruitmentApplicationStageEvent { FromStage = current, ToStage = nextStage, ActorUserId = actorUserId, ChangedAtUtc = DateTime.UtcNow, Reason = $"Interview round {interview.RoundNumber} debrief: {interview.DebriefDecision}" });
                    application.CurrentStage = nextStage; application.Version = Math.Max(1, application.Version) + 1; application.UpdatedBy = actorUserId; application.UpdatedDate = DateTime.UtcNow;
                    await applications.Update(Builders<RecruitmentJobApplication>.Filter.Where(x => x.CompanyId == companyId && x.ApplicationId == application.ApplicationId), application);
                }
            }
        }
        var updated = await interviews.Update(Builders<RecruitmentInterview>.Filter.Where(x => x.CompanyId == companyId && x.InterviewId == interview.InterviewId && (x.Version == version || x.Version == 0)), interview);
        if (!updated.Success) return Fail(409, RecruitmentErrorCodes.ConcurrencyConflict, "Interview was changed by another user.");

        // Automatically update the candidate's authoritative benchmark upon debrief completion
        if (applicants is not null && !string.IsNullOrWhiteSpace(interview.ApplicantId))
        {
            try
            {
                var allScorecards = await scorecards.GetAll(x => x.CompanyId == companyId && x.ApplicantId == interview.ApplicantId && !x.IsDeleted);
                var allInterviews = await interviews.GetAll(x => x.CompanyId == companyId && x.ApplicantId == interview.ApplicantId && !x.IsDeleted);
                var computed = RecruitmentInterviewScoreCalculator.Compute(allScorecards, allInterviews);
                var targetApplicant = await applicants.FirstOrDefault(x => x.CompanyId == companyId && x.ApplicantId == interview.ApplicantId && !x.IsDeleted);
                if (targetApplicant is not null)
                {
                    targetApplicant.FinalInterviewScore = computed.FinalInterviewScore > 0 ? computed.FinalInterviewScore : null;
                    targetApplicant.FinalInterviewRating = computed.FinalInterviewRating > 0 ? computed.FinalInterviewRating : null;
                    targetApplicant.FinalInterviewRecommendation = computed.FinalInterviewScore > 0 ? computed.FinalInterviewRecommendation : null;
                    targetApplicant.EvaluatedRoundsCount = computed.EvaluatedRoundsCount;
                    targetApplicant.FinalScoreCalculatedAtUtc = DateTime.UtcNow;
                    var debriefStage = interview.DebriefDecision.Equals("Reject", StringComparison.OrdinalIgnoreCase) ? "Rejected" : interview.DebriefDecision.Equals("Hold", StringComparison.OrdinalIgnoreCase) ? "OnHold" : "Shortlisted";
                    if (Enum.TryParse<ActivityType>(debriefStage, out var newActivityType))
                    {
                        if (newActivityType is ActivityType.Rejected or ActivityType.OnHold || (newActivityType == ActivityType.Shortlisted && targetApplicant.ActivityType is ActivityType.New or ActivityType.InProgress))
                        {
                            targetApplicant.ActivityType = newActivityType;
                            targetApplicant.UpdatedBy = actorUserId;
                            targetApplicant.UpdatedDate = DateTime.UtcNow;
                        }
                    }
                    await applicants.Update(Builders<Applicant>.Filter.Where(x => x.CompanyId == companyId && x.ApplicantId == targetApplicant.ApplicantId), targetApplicant);
                }
            }
            catch
            {
                // Non-blocking telemetry
            }
        }

        return Ok(interview.InterviewId, "Interview debrief completed.");
    }

    private static Result<string> Ok(string id, string message) => new() { Success = true, StatusCode = 200, MethodResult = id, Message = message };
    private static Result<string> Fail(int status, string code, string message) => new() { Success = false, StatusCode = status, ErrorCode = code, Message = message };
}
