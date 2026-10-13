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
        var service = new RecruitmentExternalActionService(tokens.Object, new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>().Object, TenantContext("company-a"));

        var result = await service.Issue(new RecruitmentExternalActionIssueCommand { ApplicantId = "app-1", OfferId = "offer-1", ActionType = "AcceptOffer", RecipientEmail = "Candidate@Example.com", IssuanceKey = "issuance-key-001" }, "actor-1");

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("company-a", saved!.CompanyId);
        Assert.Equal("AcceptOffer", saved.ActionType);
        Assert.Equal(64, saved.TokenHash.Length);
        Assert.NotEqual("Candidate@Example.com", saved.TokenHash);
        Assert.NotEqual(saved.TokenHash, saved.RecipientEmailHash);
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
        var service = new RecruitmentExternalActionService(tokens.Object, new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>().Object, TenantContext("company-a"));

        var result = await service.Issue(new RecruitmentExternalActionIssueCommand { ApplicantId = "app-1", OfferId = "offer-1", ActionType = "AcceptOffer", RecipientEmail = "candidate@example.com", IssuanceKey = "issuance-key-003" }, "actor-1");

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




