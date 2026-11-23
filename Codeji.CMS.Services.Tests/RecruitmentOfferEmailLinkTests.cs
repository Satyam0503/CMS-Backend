using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentOfferEmailLinkTests
{
    [Fact]
    public async Task Sending_offer_issues_version_bound_candidate_link_and_queues_it_in_email()
    {
        ConfigManager.AppSettings = new AppSettings { AppUrl = "https://careers.example.com/", APIUrl = "https://api.example.com", AppVersion = "test", IsForDebug = false };
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "app-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "1", VacancyId = "job-1" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> filter, bool _, bool __) => new[] { applicant }.AsQueryable().FirstOrDefault(filter));
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentOffer?)null);
        offers.Setup(x => x.AddOne(It.IsAny<RecruitmentOffer>())).ReturnsAsync(new Result { Success = true });
        offers.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<RecruitmentOffer>())).ReturnsAsync(new Result { Success = true });
        var emailOutbox = new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>();
        emailOutbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentEmailOutbox?)null);
        var actions = new Mock<IRecruitmentExternalActionService>();
        actions.Setup(x => x.Issue(It.IsAny<RecruitmentExternalActionIssueCommand>(), "actor-1"))
            .ReturnsAsync((RecruitmentExternalActionIssueCommand command, string _) => new Result<RecruitmentExternalActionTokenDto>
            {
                Success = true,
                MethodResult = new RecruitmentExternalActionTokenDto { RawToken = "secure+token", ExpiresAtUtc = DateTime.UtcNow.AddDays(3) }
            });
        RecruitmentEmailQueueCommand? queuedEmail = null;
        actions.Setup(x => x.QueueEmail(It.IsAny<RecruitmentEmailQueueCommand>(), "actor-1"))
            .Callback<RecruitmentEmailQueueCommand, string>((email, _) => queuedEmail = email)
            .ReturnsAsync(new Result<string> { Success = true });
        var context = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "test")) } };
        var service = new RecruitmentOfferService(
            applicants.Object,
            offers.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<INotificationService>().Object,
            context,
            vacancies: new Mock<IMongoDbRepository<JobVacancy>>().Object,
            externalActions: actions.Object,
            emailOutbox: emailOutbox.Object);

        var result = await service.Upsert(new RecruitmentOfferCommand
        {
            ApplicantId = applicant.ApplicantId,
            VacancyId = applicant.VacancyId,
            Salary = 100000,
            Currency = "INR",
            Status = RecruitmentOfferStatus.Sent,
            IdempotencyKey = "offer-send-2026-001"
        }, "actor-1");

        Assert.True(result.Success);
        Assert.NotNull(queuedEmail);
        Assert.Contains("https://careers.example.com/candidate/offer?token=secure%2Btoken", queuedEmail!.HtmlBody);
        actions.Verify(x => x.Issue(It.Is<RecruitmentExternalActionIssueCommand>(command =>
            command.ResourceVersion == 1 && command.OfferId == result.MethodResult && command.ActionType == "OfferResponse"), "actor-1"), Times.Once);
    }

    [Fact]
    public async Task Idempotent_offer_retry_repairs_a_failed_email_outbox_queue()
    {
        ConfigManager.AppSettings = new AppSettings { AppUrl = "https://careers.example.com/", APIUrl = "https://api.example.com", AppVersion = "test", IsForDebug = false };
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "app-2", FirstName = "Grace", LastName = "Hopper", Email = "grace@example.com", Phone = "1", VacancyId = "job-2" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> filter, bool _, bool __) => new[] { applicant }.AsQueryable().FirstOrDefault(filter));
        RecruitmentOffer? persisted = null;
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<RecruitmentOffer, bool>> filter, bool _, bool __) => persisted is not null && filter.Compile()(persisted) ? persisted : null);
        offers.Setup(x => x.AddOne(It.IsAny<RecruitmentOffer>())).Callback<RecruitmentOffer>(offer => { offer.OfferId = "offer-retry"; persisted = offer; }).ReturnsAsync(new Result { Success = true });
        offers.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentOffer>>(), It.IsAny<RecruitmentOffer>()))
            .Callback<MongoDB.Driver.FilterDefinition<RecruitmentOffer>, RecruitmentOffer>((_, offer) => persisted = offer)
            .ReturnsAsync(new Result { Success = true });
        var emailOutbox = new Mock<IMongoDbRepository<RecruitmentEmailOutbox>>();
        emailOutbox.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEmailOutbox, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentEmailOutbox?)null);
        var actions = new Mock<IRecruitmentExternalActionService>();
        actions.Setup(x => x.Issue(It.IsAny<RecruitmentExternalActionIssueCommand>(), "actor-1"))
            .ReturnsAsync((RecruitmentExternalActionIssueCommand _, string _) => new Result<RecruitmentExternalActionTokenDto>
            {
                Success = true,
                MethodResult = new RecruitmentExternalActionTokenDto { RawToken = "retry-token", ExpiresAtUtc = DateTime.UtcNow.AddDays(1) }
            });
        var sendAttempts = 0;
        actions.Setup(x => x.QueueEmail(It.IsAny<RecruitmentEmailQueueCommand>(), "actor-1"))
            .ReturnsAsync(() => new Result<string> { Success = ++sendAttempts > 1 });
        var context = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "test")) } };
        var service = new RecruitmentOfferService(
            applicants.Object, offers.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<INotificationService>().Object, context,
            vacancies: new Mock<IMongoDbRepository<JobVacancy>>().Object,
            externalActions: actions.Object, emailOutbox: emailOutbox.Object);
        var command = new RecruitmentOfferCommand
        {
            ApplicantId = applicant.ApplicantId, VacancyId = applicant.VacancyId, Salary = 100000, Currency = "INR",
            Status = RecruitmentOfferStatus.Sent, IdempotencyKey = "offer-send-retry-001"
        };

        var firstAttempt = await service.Upsert(command, "actor-1");
        var retry = await service.Upsert(command, "actor-1");

        Assert.False(firstAttempt.Success);
        Assert.Equal(503, firstAttempt.StatusCode);
        Assert.True(retry.Success);
        Assert.Equal(2, sendAttempts);
        actions.Verify(x => x.Issue(It.IsAny<RecruitmentExternalActionIssueCommand>(), "actor-1"), Times.Exactly(2));
    }
}
