using System.Text.RegularExpressions;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>Removes candidate-identifying content from recruitment side records after primary anonymization.</summary>
public sealed class RecruitmentCandidateDataRedactor(IMongoDatabase database) : IRecruitmentCandidateDataRedactor
{
    private const string RedactedActor = "privacy-controlled-candidate";
    private const string RedactedText = "Candidate activity details were redacted under the retention policy.";

    public async Task RedactAsync(string companyId, string applicantId, string? originalEmail, string actor, DateTime nowUtc)
    {
        var logs = database.GetCollection<ApplicantLogs>(nameof(ApplicantLogs));
        var logFilter = Builders<ApplicantLogs>.Filter.Eq(x => x.CompanyId, companyId) &
            Builders<ApplicantLogs>.Filter.Eq(x => x.ApplicantId, applicantId);
        await logs.UpdateManyAsync(logFilter, Builders<ApplicantLogs>.Update
            .Set(x => x.ApplicantName, RedactedActor)
            .Set(x => x.Description, RedactedText)
            .Set(x => x.IdempotencyKey, null)
            .Set(x => x.UpdatedBy, actor)
            .Set(x => x.UpdatedDate, nowUtc));
        await logs.UpdateManyAsync(logFilter & Builders<ApplicantLogs>.Filter.Eq(x => x.UserId, applicantId),
            Builders<ApplicantLogs>.Update.Set(x => x.UserId, RedactedActor));
        await logs.UpdateManyAsync(logFilter & Builders<ApplicantLogs>.Filter.Eq(x => x.CreatedBy, applicantId),
            Builders<ApplicantLogs>.Update.Set(x => x.CreatedBy, RedactedActor));

        var applications = database.GetCollection<RecruitmentJobApplication>(nameof(RecruitmentJobApplication));
        var relatedApplications = await applications.Find(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted).ToListAsync();
        foreach (var application in relatedApplications)
        {
            int expectedVersion = application.Version;
            foreach (var entry in application.StageHistory)
            {
                entry.Reason = RedactedText;
                if (entry.ActorUserId == applicantId) entry.ActorUserId = RedactedActor;
            }
            application.ApplicantId = string.Empty;
            application.LastCommandIdempotencyKey = null;
            application.PublicSubmissionIdempotencyKey = null;
            application.Version = Math.Max(1, expectedVersion) + 1;
            application.UpdatedBy = actor;
            application.UpdatedDate = nowUtc;
            var saved = await applications.ReplaceOneAsync(
                x => x.CompanyId == companyId && x.ApplicationId == application.ApplicationId &&
                    (x.Version == expectedVersion || (expectedVersion == 0 && x.Version == 0)) && !x.IsDeleted,
                application);
            if (saved.MatchedCount != 1)
                throw new InvalidOperationException("A recruitment application changed during candidate redaction.");
        }

        // Capture legacy email dedupe identifiers before the related records are unlinked.
        var offers = database.GetCollection<RecruitmentOffer>(nameof(RecruitmentOffer));
        var offerIds = await offers.Find(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted)
            .Project(x => x.OfferId).ToListAsync();
        var interviews = database.GetCollection<RecruitmentInterview>(nameof(RecruitmentInterview));
        var interviewIds = await interviews.Find(x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted)
            .Project(x => x.InterviewId).ToListAsync();

        // Candidate-linked evaluation records may contain free-text or resume-derived PII.
        // Preserve workflow/audit outcomes where useful, but remove candidate content and links.
        var screeningReviews = database.GetCollection<RecruitmentScreeningReview>(nameof(RecruitmentScreeningReview));
        await screeningReviews.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentScreeningReview>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.ResumeVersion, null)
                .Set(x => x.Score, 0)
                .Set(x => x.Decision, "Redacted")
                .Set(x => x.RejectionReason, RedactedText)
                .Set(x => x.Evidence, RedactedText)
                .Set(x => x.Criteria, new List<RecruitmentScreeningCriterion>())
                .Set(x => x.LastCommandIdempotencyKey, null)
                .Set(x => x.StageSyncStatus, "Cancelled")
                .Set(x => x.StageSyncTargetStage, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        var scorecards = database.GetCollection<RecruitmentScorecard>(nameof(RecruitmentScorecard));
        await scorecards.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentScorecard>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.Status, "Redacted")
                .Set(x => x.InterviewId, null)
                .Set(x => x.Recommendation, RedactedText)
                .Set(x => x.Criteria, new List<RecruitmentScorecardCriterion>())
                .Set(x => x.LastCommandIdempotencyKey, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        var extractionReviews = database.GetCollection<RecruitmentResumeExtractionReview>(nameof(RecruitmentResumeExtractionReview));
        await extractionReviews.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentResumeExtractionReview>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.ResumeVersion, null)
                .Set(x => x.ExtractedFields, new Dictionary<string, string>())
                .Set(x => x.Notes, RedactedText)
                .Set(x => x.LastCommandIdempotencyKey, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        await interviews.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentInterview>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.Status, RecruitmentInterviewStatus.Cancelled)
                .Set(x => x.Title, "Redacted interview")
                .Set(x => x.StartsAtUtc, DateTime.UnixEpoch)
                .Set(x => x.EndsAtUtc, DateTime.UnixEpoch)
                .Set(x => x.TimeZoneId, "UTC")
                .Set(x => x.Location, null)
                .Set(x => x.MeetingUrl, null)
                .Set(x => x.CalendarItemId, null)
                .Set(x => x.CalendarSyncStatus, "Redacted")
                .Set(x => x.DebriefDecision, RedactedText)
                .Set(x => x.DebriefedByUserId, null)
                .Set(x => x.LastCommandIdempotencyKey, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        var decisions = database.GetCollection<RecruitmentHiringDecision>(nameof(RecruitmentHiringDecision));
        await decisions.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentHiringDecision>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.Decision, "Redacted")
                .Set(x => x.ManagerReviewStatus, "Redacted")
                .Set(x => x.ManagerReviewNotes, RedactedText)
                .Set(x => x.Notes, RedactedText)
                .Set(x => x.StageSyncStatus, "Cancelled")
                .Set(x => x.StageSyncFromStage, null)
                .Set(x => x.StageSyncTargetStage, null)
                .Set(x => x.LastCommandIdempotencyKey, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        var talentPool = database.GetCollection<RecruitmentTalentPoolMember>(nameof(RecruitmentTalentPoolMember));
        var talentPoolMembers = await talentPool.Find(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted).ToListAsync();
        foreach (var member in talentPoolMembers)
            await talentPool.UpdateOneAsync(
                x => x.CompanyId == companyId && x.MemberId == member.MemberId && x.ApplicantId == applicantId && !x.IsDeleted,
                Builders<RecruitmentTalentPoolMember>.Update
                    // Keep the unique company/applicant/pool index valid while severing the original ID.
                    .Set(x => x.ApplicantId, $"redacted:{member.MemberId}")
                    .Set(x => x.Tags, new List<string>())
                    .Set(x => x.Notes, RedactedText)
                    .Set(x => x.Status, RecruitmentTalentPoolStatus.Archived)
                    .Set(x => x.LastCommandIdempotencyKey, null)
                    .Set(x => x.UpdatedBy, actor)
                    .Set(x => x.UpdatedDate, nowUtc));

        var preboardingCases = database.GetCollection<RecruitmentPreboardingCase>(nameof(RecruitmentPreboardingCase));
        await preboardingCases.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentPreboardingCase>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.OfferId, string.Empty)
                .Set(x => x.TargetJoiningDateUtc, null)
                .Set(x => x.Status, RecruitmentPreboardingStatus.Cancelled)
                .Set(x => x.Tasks, new List<RecruitmentPreboardingTask>())
                .Set(x => x.LastCommandIdempotencyKey, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        await offers.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentOffer>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.Salary, 0m)
                .Set(x => x.CompensationComponents, new Dictionary<string, decimal>())
                .Set(x => x.TotalCtc, 0m)
                .Set(x => x.StartDateUtc, null)
                .Set(x => x.SentAtUtc, null)
                .Set(x => x.ExpiresAtUtc, null)
                .Set(x => x.RespondedAtUtc, null)
                .Set(x => x.ViewedAtUtc, null)
                .Set(x => x.Status, RecruitmentOfferStatus.Withdrawn)
                .Set(x => x.LastCommandIdempotencyKey, null)
                .Set(x => x.LastCandidateResponseFingerprint, null)
                .Set(x => x.LastCandidateResponseMessage, null)
                .Set(x => x.LastCandidateResponsePreviousStage, null)
                .Set(x => x.SignatureDataUrl, null)
                .Set(x => x.SignerLegalName, null)
                .Set(x => x.SignedAtUtc, null)
                .Set(x => x.SignerIpAddress, null)
                .Set(x => x.SignerUserAgent, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        var offerVersions = database.GetCollection<RecruitmentOfferVersion>(nameof(RecruitmentOfferVersion));
        await offerVersions.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentOfferVersion>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.Salary, 0m)
                .Set(x => x.CompensationComponents, new Dictionary<string, decimal>())
                .Set(x => x.TotalCtc, 0m)
                .Set(x => x.StartDateUtc, null)
                .Set(x => x.SentAtUtc, null)
                .Set(x => x.AcceptedAtUtc, null)
                .Set(x => x.ExpiresAtUtc, null)
                .Set(x => x.Status, "Redacted")
                .Set(x => x.VersionReason, RedactedText)
                .Set(x => x.IdempotencyKey, null)
                .Set(x => x.ApprovalReference, null));

        var counterRequests = database.GetCollection<RecruitmentOfferCounterRequest>(nameof(RecruitmentOfferCounterRequest));
        await counterRequests.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentOfferCounterRequest>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.RequestedSalary, 0m)
                .Set(x => x.RequestedCurrency, string.Empty)
                .Set(x => x.RequestedStartDateUtc, null)
                .Set(x => x.ResolvedAtUtc, null)
                .Set(x => x.Message, RedactedText)
                .Set(x => x.Status, "Redacted")
                .Set(x => x.ResolutionNote, RedactedText)
                .Set(x => x.DecisionIdempotencyKey, null)
                .Set(x => x.IdempotencyKey, null));

        var automationEvents = database.GetCollection<RecruitmentAutomationOutboxEvent>(nameof(RecruitmentAutomationOutboxEvent));
        await automationEvents.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentAutomationOutboxEvent>.Update
                .Set(x => x.ApplicantId, null)
                .Set(x => x.Payload, new Dictionary<string, string>())
                .Set(x => x.Status, "Redacted")
                .Set(x => x.IdempotencyKey, string.Empty)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        var relationshipLinks = database.GetCollection<RecruitmentRelationshipLink>(nameof(RecruitmentRelationshipLink));
        await relationshipLinks.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentRelationshipLink>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.RelatedPersonId, string.Empty)
                .Set(x => x.Notes, RedactedText)
                .Set(x => x.LastCommandIdempotencyKey, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        // Older deployments may have stored resume metadata in a separate collection.
        var legacyResumes = database.GetCollection<Resume>(nameof(Resume));
        await legacyResumes.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId,
            Builders<Resume>.Update
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.FileName, "redacted")
                .Set(x => x.ResumeUrl, string.Empty));

        var actionTokens = database.GetCollection<RecruitmentExternalActionToken>(nameof(RecruitmentExternalActionToken));
        await actionTokens.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentExternalActionToken>.Update
                .Set(x => x.RevokedAtUtc, nowUtc)
                .Set(x => x.TokenHash, string.Empty)
                .Set(x => x.RecipientEmailHash, null)
                .Set(x => x.IssuanceKey, null)
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.VacancyId, null)
                .Set(x => x.InterviewId, null)
                .Set(x => x.OfferId, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        var shares = database.GetCollection<RecruitmentCandidateShare>(nameof(RecruitmentCandidateShare));
        await shares.UpdateManyAsync(
            x => x.CompanyId == companyId && x.ApplicantId == applicantId && !x.IsDeleted,
            Builders<RecruitmentCandidateShare>.Update
                .Set(x => x.RevokedAtUtc, nowUtc)
                .Set(x => x.ApplicantId, string.Empty)
                .Set(x => x.SharedWithUserId, string.Empty)
                .Set(x => x.LastCommandIdempotencyKey, null)
                .Set(x => x.UpdatedBy, actor)
                .Set(x => x.UpdatedDate, nowUtc));

        var outbox = database.GetCollection<RecruitmentEmailOutbox>(nameof(RecruitmentEmailOutbox));
        var legacyDedupePrefixes = new List<string> { $"public-application:{applicantId}:" };
        legacyDedupePrefixes.AddRange(offerIds.SelectMany(id => new[] { $"offer-email:{id}:", $"offer-response:{id}:" }));
        legacyDedupePrefixes.AddRange(interviewIds.SelectMany(id => new[]
        {
            $"interview-confirmed:{id}:", $"interview-panelist:{id}:", $"interview-candidate:{id}:"
        }));
        string legacyDedupePattern = $"^(?:{string.Join('|', legacyDedupePrefixes.Select(Regex.Escape))})";
        var outboxFilter = Builders<RecruitmentEmailOutbox>.Filter.Eq(x => x.CompanyId, companyId) &
            (Builders<RecruitmentEmailOutbox>.Filter.Eq(x => x.RelatedApplicantId, applicantId) |
             Builders<RecruitmentEmailOutbox>.Filter.Regex(x => x.DedupeKey, new BsonRegularExpression(legacyDedupePattern)));
        if (!string.IsNullOrWhiteSpace(originalEmail))
            outboxFilter |= Builders<RecruitmentEmailOutbox>.Filter.Eq(x => x.CompanyId, companyId) &
                Builders<RecruitmentEmailOutbox>.Filter.Eq(x => x.RecipientEmail, originalEmail.Trim()) &
                Builders<RecruitmentEmailOutbox>.Filter.In(x => x.EventType, new[]
                {
                    "ApplicationReceivedCandidate", "InterviewConfirmed", "InterviewCandidateInvited",
                    "OfferSent", "OfferResponse", "OfferChangeRequested"
                });
        await outbox.UpdateManyAsync(outboxFilter, Builders<RecruitmentEmailOutbox>.Update
            .Set(x => x.Status, "Redacted")
            .Set(x => x.RelatedApplicantId, null)
            .Set(x => x.DedupeKey, string.Empty)
            .Set(x => x.RecipientEmail, "redacted@invalid.local")
            .Set(x => x.RecipientUserId, null)
            .Set(x => x.Subject, "Redacted candidate notification")
            .Set(x => x.HtmlBody, string.Empty)
            .Set(x => x.LastError, null)
            .Set(x => x.UpdatedBy, actor)
            .Set(x => x.UpdatedDate, nowUtc));

        var emailLogs = database.GetCollection<EmpEmailLogs>(nameof(EmpEmailLogs));
        var relatedEmailLogs = Builders<EmpEmailLogs>.Filter.Eq(x => x.UserTo, applicantId);
        if (!string.IsNullOrWhiteSpace(originalEmail))
        {
            relatedEmailLogs |= Builders<EmpEmailLogs>.Filter.Eq(x => x.Email, originalEmail.Trim()) &
                Builders<EmpEmailLogs>.Filter.In(x => x.UserFrom, ["recruitment", "public-career"]);
        }
        var emailLogFilter = Builders<EmpEmailLogs>.Filter.Eq(x => x.CompanyId, companyId) & relatedEmailLogs;
        await emailLogs.UpdateManyAsync(emailLogFilter, Builders<EmpEmailLogs>.Update
            .Set(x => x.Email, "redacted@invalid.local")
            .Set(x => x.UserTo, RedactedActor)
            .Set(x => x.Subject, "Redacted candidate notification")
            .Set(x => x.Body, string.Empty)
            .Set(x => x.ErrorMessage, string.Empty)
            .Set(x => x.UpdatedBy, actor)
            .Set(x => x.UpdatedDate, nowUtc));
    }
}
