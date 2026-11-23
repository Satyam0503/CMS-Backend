using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentInterviewSequenceTests
{
    [Fact]
    public async Task Multiple_panelists_are_rejected_when_panel_interviews_are_disabled()
    {
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", ParallelPanels = false });
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        var service = new RecruitmentInterviewService(
            applicants.Object,
            interviews.Object,
            new Mock<ICalendarServices>().Object,
            TenantContext("company-1"),
            configs: configs.Object);

        var start = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(2), DateTimeKind.Utc);
        var result = await service.Upsert(new RecruitmentInterviewCommand
        {
            ApplicantId = "applicant-1",
            VacancyId = "vacancy-1",
            Title = "Panel interview",
            RoundNumber = 1,
            StartsAtUtc = start,
            EndsAtUtc = start.AddHours(1),
            IdempotencyKey = "panel-interviews-disabled",
            PanelistUserIds = ["interviewer-1", "interviewer-2"],
            Status = RecruitmentInterviewStatus.Scheduled
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.FeatureDisabled, result.ErrorCode);
        applicants.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        interviews.Verify(x => x.AddOne(It.IsAny<RecruitmentInterview>()), Times.Never);
    }

    [Fact]
    public async Task Disabled_multiple_rounds_are_rejected_before_applicant_read()
    {
        var context = TenantContext("company-1");
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", InterviewRounds = false });
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        var service = new RecruitmentInterviewService(
            applicants.Object,
            interviews.Object,
            new Mock<ICalendarServices>().Object,
            context,
            configs: configs.Object);

        var result = await service.Upsert(new RecruitmentInterviewCommand
        {
            ApplicantId = "applicant-1",
            VacancyId = "vacancy-1",
            Title = "Technical interview",
            RoundNumber = 2,
            StartsAtUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(2), DateTimeKind.Utc),
            EndsAtUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(2).AddHours(1), DateTimeKind.Utc),
            IdempotencyKey = "round-two-off"
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.FeatureDisabled, result.ErrorCode);
        applicants.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        interviews.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Parallel_panels_does_not_allow_skipping_incomplete_prior_rounds()
    {
        var context = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("company_id", "company-1")], "test"));
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", InterviewRounds = true, ParallelPanels = true });
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { ApplicantId = "applicant-1", VacancyId = "vacancy-1", CompanyId = "company-1", Phone = "1" });
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentInterview?)null);
        interviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<RecruitmentInterview>());
        var service = new RecruitmentInterviewService(
            applicants.Object,
            interviews.Object,
            new Mock<ICalendarServices>().Object,
            context,
            configs: configs.Object);

        var start = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(2), DateTimeKind.Utc);
        var result = await service.Upsert(new RecruitmentInterviewCommand
        {
            ApplicantId = "applicant-1",
            VacancyId = "vacancy-1",
            Title = "Technical interview",
            RoundNumber = 2,
            StartsAtUtc = start,
            EndsAtUtc = start.AddHours(1),
            IdempotencyKey = "round-two-key"
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
        interviews.Verify(x => x.AddOne(It.IsAny<RecruitmentInterview>()), Times.Never);
    }

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "test"));
        return new HttpContextAccessor { HttpContext = context };
    }
}
