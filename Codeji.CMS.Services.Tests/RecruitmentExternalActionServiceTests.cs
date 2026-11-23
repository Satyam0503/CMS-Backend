using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentExternalActionServiceTests
{
    [Fact]
    public async Task Issue_stores_only_a_hash_and_scopes_token_to_current_tenant()
    {
        var tokens = new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>();
        RecruitmentExternalActionToken? saved = null;
        tokens.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentExternalActionToken, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentExternalActionToken?)null);
        tokens.Setup(x => x.AddOne(It.IsAny<RecruitmentExternalActionToken>())).Callback<RecruitmentExternalActionToken>(x => saved = x).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentOffer { CompanyId = "company-a", OfferId = "offer-1", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Sent, Version = 2 });
        var service = new RecruitmentExternalActionService(tokens.Object, new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>().Object, TenantContext("company-a"), offers: offers.Object);

        var result = await service.Issue(new RecruitmentExternalActionIssueCommand { ApplicantId = "app-1", OfferId = "offer-1", ResourceVersion = 2, ActionType = "AcceptOffer", RecipientEmail = "Candidate@Example.com", IssuanceKey = "issuance-key-001" }, "actor-1");

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("company-a", saved!.CompanyId);
        Assert.Equal("AcceptOffer", saved.ActionType);
        Assert.Equal(64, saved.TokenHash.Length);
        Assert.NotEqual("Candidate@Example.com", saved.TokenHash);
        Assert.NotEqual(saved.TokenHash, saved.RecipientEmailHash);
    }

    [Theory]
    [InlineData(null, 400)]
    [InlineData(2, 409)]
    public async Task Offer_action_link_requires_the_current_offer_version(int? requestedVersion, int expectedStatus)
    {
        var tokens = new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>();
        tokens.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentExternalActionToken, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentExternalActionToken?)null);
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer { CompanyId = "company-a", OfferId = "offer-1", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Sent, Version = 3 });
        var service = new RecruitmentExternalActionService(tokens.Object, new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>().Object, TenantContext("company-a"), offers: offers.Object);

        var result = await service.Issue(new RecruitmentExternalActionIssueCommand
        {
            ApplicantId = "app-1", OfferId = "offer-1", ActionType = "AcceptOffer", RecipientEmail = "candidate@example.com",
            ResourceVersion = requestedVersion, IssuanceKey = "offer-version-key-001"
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(expectedStatus, result.StatusCode);
        tokens.Verify(x => x.AddOne(It.IsAny<RecruitmentExternalActionToken>()), Times.Never);
    }

    [Fact]
    public async Task Validate_rejects_expired_action_link()
    {
        var tokens = new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>();
        tokens.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentExternalActionToken, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentExternalActionToken { CompanyId = "company-a", TokenHash = "hash", ActionType = "AcceptOffer", ApplicantId = "app-1", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1) });
        var service = new RecruitmentExternalActionService(tokens.Object, new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>().Object, TenantContext("company-a"));

        var result = await service.Validate("raw-token", false);

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
        Assert.Equal(410, result.StatusCode);
    }

    [Fact]
    public async Task ValidateForIdempotentReplay_accepts_only_a_previously_consumed_unrevoked_token()
    {
        var token = new RecruitmentExternalActionToken
        {
            CompanyId = "company-a",
            TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("raw-token"))).ToLowerInvariant(),
            ActionType = "OfferResponse",
            ApplicantId = "app-1",
            OfferId = "offer-1",
            ResourceVersion = 3,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            ConsumedAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        var tokens = new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>();
        tokens.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentExternalActionToken, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<RecruitmentExternalActionToken, bool>> filter, bool _, bool __) => filter.Compile()(token) ? token : null);
        var service = new RecruitmentExternalActionService(tokens.Object, new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>().Object, TenantContext("company-a"));

        var recovered = await service.ValidateForIdempotentReplay("raw-token");
        token.RevokedAtUtc = DateTime.UtcNow;
        var revoked = await service.ValidateForIdempotentReplay("raw-token");
        token.RevokedAtUtc = null;
        token.ConsumedAtUtc = null;
        var unused = await service.ValidateForIdempotentReplay("raw-token");

        Assert.True(recovered.Success);
        Assert.Equal(3, recovered.MethodResult!.ResourceVersion);
        Assert.False(revoked.Success);
        Assert.False(unused.Success);
    }

    [Fact]
    public async Task Queue_email_reuses_existing_dedupe_key_without_duplicate_write()
    {
        var outbox = new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>();
        outbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentEmailOutbox { CompanyId = "company-a", OutboxId = "out-1", DedupeKey = "offer-1-sent" });
        var service = new RecruitmentExternalActionService(new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object, outbox.Object, TenantContext("company-a"));

        var result = await service.QueueEmail(new RecruitmentEmailQueueCommand { DedupeKey = "offer-1-sent", EventType = "OfferReady", RecipientEmail = "candidate@example.com", Subject = "Offer", HtmlBody = "<p>Offer</p>" }, "actor-1");

        Assert.True(result.Success);
        Assert.Equal("out-1", result.MethodResult);
        outbox.Verify(x => x.AddOne(It.IsAny<RecruitmentEmailOutbox>()), Times.Never);
    }

    [Fact]
    public async Task Issue_rejects_recipient_that_does_not_match_tenant_candidate()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { CompanyId = "company-a", ApplicantId = "app-1", Email = "real@example.com", VacancyId = "vac-1", Phone = "1" });
        var service = new RecruitmentExternalActionService(new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object, new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>().Object, TenantContext("company-a"), applicants.Object);
        var result = await service.Issue(new RecruitmentExternalActionIssueCommand { ApplicantId = "app-1", OfferId = "offer-1", ActionType = "AcceptOffer", RecipientEmail = "attacker@example.com", IssuanceKey = "issuance-key-002" }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
    }

    [Fact]
    public async Task Issue_does_not_reuse_another_tenant_issuance_key()
    {
        var tokens = new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>();
        var seed = new[] { new RecruitmentExternalActionToken { CompanyId = "company-b", ApplicantId = "app-1", ActionType = "AcceptOffer", IssuanceKey = "issuance-key-003", TokenHash = "old-hash", ExpiresAtUtc = DateTime.UtcNow.AddHours(1) } };
        tokens.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentExternalActionToken, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<RecruitmentExternalActionToken, bool>> filter, bool _, bool __) => seed.AsQueryable().FirstOrDefault(filter));
        tokens.Setup(x => x.AddOne(It.IsAny<RecruitmentExternalActionToken>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentOffer { CompanyId = "company-a", OfferId = "offer-1", ApplicantId = "app-1", Status = RecruitmentOfferStatus.Sent, Version = 1 });
        var service = new RecruitmentExternalActionService(tokens.Object, new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>().Object, TenantContext("company-a"), offers: offers.Object);

        var result = await service.Issue(new RecruitmentExternalActionIssueCommand { ApplicantId = "app-1", OfferId = "offer-1", ResourceVersion = 1, ActionType = "AcceptOffer", RecipientEmail = "candidate@example.com", IssuanceKey = "issuance-key-003" }, "actor-1");

        Assert.True(result.Success);
        tokens.Verify(x => x.AddOne(It.Is<RecruitmentExternalActionToken>(item => item.CompanyId == "company-a")), Times.Once);
    }
    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth"));
        return new HttpContextAccessor { HttpContext = context };
    }
}




