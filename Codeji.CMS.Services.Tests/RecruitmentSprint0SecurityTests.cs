using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
using System.Security.Claims;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentSprint0SecurityTests
{
    [Fact]
    public async Task Candidate_offer_response_rejects_oversized_message_before_candidate_lookup()
    {
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate("oversized-message-token", false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto { CompanyId = "company-a", ActionType = "OfferResponse", ApplicantId = "app-1", OfferId = "offer-1", ResourceVersion = 1 }
        });
        var applicants = Repository<Applicant>();
        var service = new RecruitmentCandidateActionService(external.Object, applicants.Object, Repository<RecruitmentOffer>().Object, Repository<RecruitmentInterview>().Object, Repository<RecruitmentEmailOutbox>().Object);

        var result = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand
        {
            Token = "oversized-message-token",
            Decision = "Declined",
            Message = new string('x', 4001),
            IdempotencyKey = "candidate-message-limit-001"
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        applicants.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Consumed_offer_link_can_only_read_back_its_committed_terminal_response()
    {
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate("consumed-read-token", false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto> { Success = false, StatusCode = 410 });
        external.Setup(x => x.ValidateForIdempotentReplay("consumed-read-token")).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto { CompanyId = "company-a", ActionType = "OfferResponse", ApplicantId = "app-1", OfferId = "offer-1", ResourceVersion = 4 }
        });
        var applicants = Repository<Applicant>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-a", ApplicantId = "app-1", FirstName = "Priya", LastName = "Sharma", Email = "candidate@example.com", Phone = "1" });
        var offers = Repository<RecruitmentOffer>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer { CompanyId = "company-a", ApplicantId = "app-1", OfferId = "offer-1", Status = RecruitmentOfferStatus.Accepted, Version = 5, RespondedAtUtc = DateTime.UtcNow, LastCommandIdempotencyKey = "candidate-key-123", LastCandidateResponseFingerprint = "fingerprint" });
        var service = new RecruitmentCandidateActionService(external.Object, applicants.Object, offers.Object, Repository<RecruitmentInterview>().Object, Repository<RecruitmentEmailOutbox>().Object);

        var result = await service.RecordOfferView(new RecruitmentCandidateOfferViewCommand { Token = "consumed-read-token" });

        Assert.True(result.Success);
        Assert.Equal(RecruitmentOfferStatus.Accepted, result.MethodResult!.Status);
        Assert.Equal(5, result.MethodResult.Version);
        offers.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<RecruitmentOffer>>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Reloading_consumed_offer_link_retries_saved_follow_up_work_with_original_payload()
    {
        const string tokenValue = "consumed-decline-recovery-token";
        const string idempotencyKey = "candidate-decline-recovery-001";
        const string message = "Thank you for the offer.";
        var fingerprintPayload = System.Text.Json.JsonSerializer.Serialize(new
        {
            Decision = "RequestOfferChange",
            Message = message,
            LegalConsent = false,
            SignerLegalName = string.Empty
        });
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fingerprintPayload))).ToLowerInvariant();
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate(tokenValue, false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto> { Success = false, StatusCode = 410 });
        external.Setup(x => x.ValidateForIdempotentReplay(tokenValue)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto { CompanyId = "company-a", ActionType = "OfferResponse", ApplicantId = "app-1", OfferId = "offer-1", ResourceVersion = 6 }
        });
        var applicants = Repository<Applicant>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-a", ApplicantId = "app-1", FirstName = "Priya", LastName = "Sharma", Email = "candidate@example.com", Phone = "1" });
        var offers = Repository<RecruitmentOffer>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer
            {
                CompanyId = "company-a", ApplicantId = "app-1", OfferId = "offer-1", Status = RecruitmentOfferStatus.ChangeRequested,
                Version = 7, RespondedAtUtc = DateTime.UtcNow, LastCommandIdempotencyKey = idempotencyKey,
                LastCandidateResponseFingerprint = fingerprint, LastCandidateResponseMessage = message
            });
        var outbox = Repository<RecruitmentEmailOutbox>();
        outbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentEmailOutbox?)null);
        RecruitmentEmailOutbox? queuedEmail = null;
        outbox.Setup(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>())).Callback<RecruitmentEmailOutbox>(email => queuedEmail = email).ReturnsAsync(new Result { Success = true });
        var service = new RecruitmentCandidateActionService(external.Object, applicants.Object, offers.Object, Repository<RecruitmentInterview>().Object, outbox.Object);

        var result = await service.RecordOfferView(new RecruitmentCandidateOfferViewCommand { Token = tokenValue });

        Assert.True(result.Success);
        Assert.Equal(RecruitmentOfferStatus.ChangeRequested, result.MethodResult!.Status);
        outbox.Verify(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>()), Times.Once);
        Assert.Equal($"offer-response:offer-1:{idempotencyKey}", queuedEmail!.DedupeKey);
        Assert.Contains(message, queuedEmail.HtmlBody);
    }

    [Fact]
    public async Task RequestOfferChange_updates_offer_then_consumes_matching_token()
    {
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate("raw-token", false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto {
                CompanyId = "company-a",
ActionType = "RequestOfferChange",
                ApplicantId = "app-1",
                OfferId = "offer-1",
                ResourceVersion = 3
            }
        });
        external.Setup(x => x.Validate("raw-token", true)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto()
        });

        var applicants = Repository<Applicant>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-a", ApplicantId = "app-1", Email = "candidate@example.com", Phone = "1" });
        var offers = Repository<RecruitmentOffer>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer { CompanyId = "company-a", OfferId = "offer-1", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Sent, Version = 3 });
        offers.Setup(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<RecruitmentOffer>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Result { Success = true });

        var outbox = Repository<RecruitmentEmailOutbox>();
        outbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentEmailOutbox?)null);
        outbox.Setup(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>())).ReturnsAsync(new Result { Success = true });

        var service = new RecruitmentCandidateActionService(external.Object, applicants.Object, offers.Object, Repository<RecruitmentInterview>().Object, outbox.Object);

        var result = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand
        {
            Token = "raw-token",
            Decision = "RequestOfferChange",
            Message = "Please review the start date.",
            IdempotencyKey = "candidate-command-001"
        });

        Assert.True(result.Success);
        Assert.Equal("offer-1", result.MethodResult);
        external.Verify(x => x.Validate("raw-token", true), Times.Once);
        offers.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<RecruitmentOffer>>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Offer_link_rejects_decision_that_does_not_match_action_type()
    {
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate("raw-token", false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto {
                CompanyId = "company-a",
ActionType = "AcceptOffer",
                ApplicantId = "app-1",
                OfferId = "offer-1"
            }
        });

        var service = new RecruitmentCandidateActionService(
            external.Object,
            Repository<Applicant>().Object,
            Repository<RecruitmentOffer>().Object,
            Repository<RecruitmentInterview>().Object,
            Repository<RecruitmentEmailOutbox>().Object);

        var result = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand
        {
            Token = "raw-token",
            Decision = "Declined",
            IdempotencyKey = "candidate-command-002"
        });

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
        external.Verify(x => x.Validate("raw-token", true), Times.Never);
    }

    [Fact]
    public async Task Offer_response_link_allows_a_candidate_decline()
    {
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate("response-token", false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto { CompanyId = "company-a", ActionType = "OfferResponse", ApplicantId = "app-1", OfferId = "offer-1", ResourceVersion = 3 }
        });
        external.Setup(x => x.Validate("response-token", true)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto> { Success = true, MethodResult = new RecruitmentExternalActionTokenDto() });
        var applicants = Repository<Applicant>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-a", ApplicantId = "app-1", Email = "candidate@example.com", Phone = "1" });
        var offers = Repository<RecruitmentOffer>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer { CompanyId = "company-a", OfferId = "offer-1", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Sent, Version = 3 });
        offers.Setup(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<RecruitmentOffer>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Result { Success = true });
        var outbox = Repository<RecruitmentEmailOutbox>();
        outbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentEmailOutbox?)null);
        outbox.Setup(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>())).ReturnsAsync(new Result { Success = true });
        var service = new RecruitmentCandidateActionService(external.Object, applicants.Object, offers.Object, Repository<RecruitmentInterview>().Object, outbox.Object);

        var result = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand { Token = "response-token", Decision = "Declined", IdempotencyKey = "candidate-command-003" });

        Assert.True(result.Success);
        external.Verify(x => x.Validate("response-token", true), Times.Once);
    }

    [Fact]
    public async Task Candidate_accepting_offer_advances_applicant_to_selected_and_prepares_preboarding()
    {
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate("accept-token", false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto { CompanyId = "company-a", ActionType = "OfferResponse", ApplicantId = "app-1", OfferId = "offer-1", ResourceVersion = 1 }
        });
        external.Setup(x => x.Validate("accept-token", true)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto> { Success = true, MethodResult = new RecruitmentExternalActionTokenDto() });
        var applicant = new Applicant { CompanyId = "company-a", ApplicantId = "app-1", Email = "candidate@example.com", ActivityType = ActivityType.Shortlisted, Phone = "1" };
        var applicants = Repository<Applicant>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Result { Success = true });
        var offers = Repository<RecruitmentOffer>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer { CompanyId = "company-a", OfferId = "offer-1", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Sent, Version = 1, Salary = 95000, Currency = "INR" });
        offers.Setup(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<RecruitmentOffer>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Result { Success = true });
        var outbox = Repository<RecruitmentEmailOutbox>();
        outbox.Setup(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>())).ReturnsAsync(new Result { Success = true });
        var preboarding = Repository<RecruitmentPreboardingCase>();
        preboarding.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentPreboardingCase, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentPreboardingCase?)null);
        preboarding.Setup(x => x.AddOne(It.IsAny<RecruitmentPreboardingCase>())).ReturnsAsync(new Result { Success = true });
        var applications = Repository<RecruitmentJobApplication>();
        applications.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentJobApplication?)null);
        applications.Setup(x => x.AddOne(It.IsAny<RecruitmentJobApplication>())).ReturnsAsync(new Result { Success = true });
        var applicantLogs = Repository<ApplicantLogs>();
        applicantLogs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((ApplicantLogs?)null);
        applicantLogs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Result { Success = true });
        var automation = Repository<RecruitmentAutomationOutboxEvent>();
        automation.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentAutomationOutboxEvent, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentAutomationOutboxEvent?)null);
        automation.Setup(x => x.AddOne(It.IsAny<RecruitmentAutomationOutboxEvent>())).ReturnsAsync(new Result { Success = true });
        outbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentEmailOutbox?)null);
        outbox.Setup(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>())).ReturnsAsync(new Result { Success = true });

        var service = new RecruitmentCandidateActionService(
            external.Object,
            applicants.Object,
            offers.Object,
            Repository<RecruitmentInterview>().Object,
            outbox.Object,
            automationOutbox: automation.Object,
            preboardingCases: preboarding.Object,
            applications: applications.Object,
            applicantLogs: applicantLogs.Object);

        var result = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand
        {
            Token = "accept-token",
            Decision = "Accepted",
            IdempotencyKey = "candidate-command-accept-001",
            SignatureDataUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4////fwAJ+wP9KobjigAAAABJRU5ErkJggg==",
            SignerLegalName = "Candidate Name",
            LegalConsent = true
        });

        Assert.True(result.Success);
        Assert.Equal(ActivityType.Selected, applicant.ActivityType);
        applicants.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<Applicant>>(), It.Is<Applicant>(a => a.ActivityType == ActivityType.Selected)), Times.Once);
        preboarding.Verify(x => x.AddOne(It.Is<RecruitmentPreboardingCase>(c => c.OfferId == "offer-1" && c.ApplicantId == "app-1")), Times.Once);

        Assert.True(result.Success);
        external.Verify(x => x.Validate("accept-token", true), Times.Once);
    }

    [Fact]
    public async Task Stale_offer_version_link_cannot_view_or_respond_to_changed_terms()
    {
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate("stale-token", false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto { CompanyId = "company-a", ActionType = "OfferResponse", ApplicantId = "app-1", OfferId = "offer-1", ResourceVersion = 2 }
        });
        var applicants = Repository<Applicant>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-a", ApplicantId = "app-1", Email = "candidate@example.com", Phone = "1" });
        var offers = Repository<RecruitmentOffer>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer { CompanyId = "company-a", OfferId = "offer-1", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Sent, Version = 3 });
        var service = new RecruitmentCandidateActionService(external.Object, applicants.Object, offers.Object, Repository<RecruitmentInterview>().Object, Repository<RecruitmentEmailOutbox>().Object);

        var response = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand { Token = "stale-token", Decision = "Declined", IdempotencyKey = "candidate-command-stale-001" });

        Assert.False(response.Success);
        Assert.Equal(409, response.StatusCode);
        offers.Verify(x => x.UpdateMany(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<MongoDB.Driver.UpdateDefinition<RecruitmentOffer>>(), It.IsAny<bool>()), Times.Never);
        external.Verify(x => x.Validate("stale-token", true), Times.Never);
    }

    [Fact]
    public async Task Consumed_offer_link_recovers_only_the_same_idempotent_response()
    {
        const string tokenValue = "consumed-offer-token";
        const string idempotencyKey = "candidate-response-retry-001";
        var canonical = System.Text.Json.JsonSerializer.Serialize(new
        {
            Decision = "Declined",
            Message = string.Empty,
            LegalConsent = false,
            SignerLegalName = string.Empty
        });
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate(tokenValue, false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = false,
            StatusCode = 410
        });
        external.Setup(x => x.ValidateForIdempotentReplay(tokenValue)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto
            {
                CompanyId = "company-a",
                ActionType = "OfferResponse",
                ApplicantId = "app-1",
                OfferId = "offer-1",
                ResourceVersion = 3
            }
        });
        var applicants = Repository<Applicant>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-a", ApplicantId = "app-1", Email = "candidate@example.com", Phone = "1" });
        var offers = Repository<RecruitmentOffer>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer
            {
                CompanyId = "company-a", OfferId = "offer-1", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Declined,
                Version = 4, LastCommandIdempotencyKey = idempotencyKey, LastCandidateResponseFingerprint = fingerprint,
                RespondedAtUtc = DateTime.UtcNow
            });
        var outbox = Repository<RecruitmentEmailOutbox>();
        outbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentEmailOutbox?)null);
        outbox.Setup(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>())).ReturnsAsync(new Result { Success = true });
        var service = new RecruitmentCandidateActionService(
            external.Object,
            applicants.Object,
            offers.Object,
            Repository<RecruitmentInterview>().Object,
            outbox.Object);

        var retry = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand
        {
            Token = tokenValue,
            Decision = "Declined",
            IdempotencyKey = idempotencyKey
        });
        var changedRetry = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand
        {
            Token = tokenValue,
            Decision = "Accepted",
            IdempotencyKey = idempotencyKey
        });

        Assert.True(retry.Success);
        Assert.Equal("offer-1", retry.MethodResult);
        Assert.False(changedRetry.Success);
        Assert.Equal(409, changedRetry.StatusCode);
        external.Verify(x => x.Validate(tokenValue, true), Times.Never);
    }

    [Fact]
    public async Task Consumed_offer_acceptance_retry_repairs_missing_follow_up_records()
    {
        const string tokenValue = "consumed-accept-token";
        const string idempotencyKey = "candidate-accept-retry-001";
        var signature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4////fwAJ+wP9KobjigAAAABJRU5ErkJggg==";
        var canonicalFingerprint = System.Text.Json.JsonSerializer.Serialize(new
        {
            Decision = "Accepted",
            Message = string.Empty,
            LegalConsent = true,
            SignerLegalName = "Candidate Name"
        });
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonicalFingerprint))).ToLowerInvariant();
        var external = new Mock<IRecruitmentExternalActionService>();
        external.Setup(x => x.Validate(tokenValue, false)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto> { Success = false, StatusCode = 410 });
        external.Setup(x => x.ValidateForIdempotentReplay(tokenValue)).ReturnsAsync(new Result<RecruitmentExternalActionTokenDto>
        {
            Success = true,
            MethodResult = new RecruitmentExternalActionTokenDto { CompanyId = "company-a", ActionType = "OfferResponse", ApplicantId = "app-1", OfferId = "offer-1", ResourceVersion = 1 }
        });
        var applicant = new Applicant { CompanyId = "company-a", ApplicantId = "app-1", FirstName = "Candidate", LastName = "Name", Email = "candidate@example.com", Phone = "1", VacancyId = "job-1", ActivityType = ActivityType.Rejected };
        var applicants = Repository<Applicant>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Result { Success = true });
        var offers = Repository<RecruitmentOffer>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentOffer
        {
            CompanyId = "company-a", ApplicantId = "app-1", OfferId = "offer-1", Status = RecruitmentOfferStatus.Accepted,
            Version = 2, LastCommandIdempotencyKey = idempotencyKey, LastCandidateResponseFingerprint = fingerprint,
            LastCandidateResponsePreviousStage = nameof(ActivityType.Shortlisted), RespondedAtUtc = DateTime.UtcNow
        });
        var outbox = Repository<RecruitmentEmailOutbox>();
        outbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentEmailOutbox?)null);
        outbox.Setup(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>())).ReturnsAsync(new Result { Success = true });
        var applications = Repository<RecruitmentJobApplication>();
        applications.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentJobApplication { CompanyId = "company-a", ApplicantId = "app-1", ApplicationId = "application-1", CurrentStage = nameof(ActivityType.Rejected), StageHistory = [new RecruitmentApplicationStageEvent { FromStage = nameof(ActivityType.Selected), ToStage = nameof(ActivityType.Rejected) }] });
        var logs = Repository<ApplicantLogs>();
        logs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((ApplicantLogs?)null);
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Result { Success = true });
        var configs = Repository<RecruitmentEnterpriseConfig>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-a", RecruitmentMode = "Simple" });
        var automation = Repository<RecruitmentAutomationOutboxEvent>();
        automation.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentAutomationOutboxEvent, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentAutomationOutboxEvent?)null);
        automation.Setup(x => x.AddOne(It.IsAny<RecruitmentAutomationOutboxEvent>())).ReturnsAsync(new Result { Success = true });
        var service = new RecruitmentCandidateActionService(external.Object, applicants.Object, offers.Object, Repository<RecruitmentInterview>().Object, outbox.Object,
            automationOutbox: automation.Object, configs: configs.Object, applications: applications.Object, applicantLogs: logs.Object);

        var result = await service.RespondToOffer(new RecruitmentCandidateOfferActionCommand
        {
            Token = tokenValue, Decision = "Accepted", IdempotencyKey = idempotencyKey,
            LegalConsent = true, SignerLegalName = "Candidate Name", SignatureDataUrl = signature
        });

        Assert.True(result.Success);
        Assert.Equal(ActivityType.Rejected, applicant.ActivityType);
        applications.Verify(x => x.AddOne(It.IsAny<RecruitmentJobApplication>()), Times.Never);
        applications.Verify(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentJobApplication>>(), It.IsAny<RecruitmentJobApplication>()), Times.Never);
        logs.Verify(x => x.AddOne(It.Is<ApplicantLogs>(log => log.EventType == "OfferAcceptedByCandidate")), Times.Once);
        automation.Verify(x => x.AddOne(It.Is<RecruitmentAutomationOutboxEvent>(evt => evt.IdempotencyKey == $"offer-accepted:offer-1:{idempotencyKey}")), Times.Once);
    }
    private static Mock<IMongoDbRepository<T>> Repository<T>() where T : class
        => new();
}






