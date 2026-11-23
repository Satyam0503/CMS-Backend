using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentFeedbackReminderDeliveryTests
{
    [Fact]
    public async Task QueueFeedbackReminders_sends_durable_emails_only_to_active_interviewers_without_submitted_scorecards()
    {
        var companyId = "company-1";
        var context = TenantContext(companyId);
        var interview = new RecruitmentInterview
        {
            InterviewId = "interview-1",
            CompanyId = companyId,
            ApplicantId = "candidate-1",
            VacancyId = "vacancy-1",
            Title = "Platform Engineer",
            Status = RecruitmentInterviewStatus.Scheduled,
            StartsAtUtc = DateTime.UtcNow.AddDays(-3),
            EndsAtUtc = DateTime.UtcNow.AddDays(-3).AddHours(1),
            PanelistUserIds = ["interviewer-1", "interviewer-2"],
            QuorumRequired = 2
        };
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([interview]);
        interviews.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(interview);

        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = companyId, EnableSla = true, SlaTracking = true, EnableAutomation = true, Automation = true });
        var scorecards = new Mock<IMongoDbRepository<RecruitmentScorecard>>();
        scorecards.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentScorecard, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([new RecruitmentScorecard { CompanyId = companyId, InterviewId = "interview-1", ReviewerUserId = "interviewer-1", Status = RecruitmentScorecardStatus.Submitted }]);

        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        var employeeRows = new List<EmpUser>
        {
            new() { UserId = "interviewer-1", CompanyId = companyId, Status = true, Email = "one@example.com", FirstName = "Interviewer", LastName = "One" },
            new() { UserId = "interviewer-2", CompanyId = companyId, Status = true, Email = "two@example.com", FirstName = "Interviewer", LastName = "Two" },
            new() { UserId = "disabled-interviewer", CompanyId = companyId, Status = false, Email = "disabled@example.com", FirstName = "Disabled", LastName = "Interviewer" }
        };
        employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<EmpUser, bool>> predicate, bool _, bool _) => employeeRows.Where(predicate.Compile()).ToList());

        var queued = new List<RecruitmentEmailQueueCommand>();
        var externalActions = new Mock<IRecruitmentExternalActionService>();
        externalActions.Setup(x => x.QueueEmail(It.IsAny<RecruitmentEmailQueueCommand>(), It.IsAny<string>()))
            .Callback<RecruitmentEmailQueueCommand, string>((command, _) => queued.Add(command))
            .ReturnsAsync(new Result<string> { Success = true, StatusCode = StatusCodes.Status200OK });
        var automationOutbox = new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>();

        var service = CreateService(companyId, context, interviews.Object, configs.Object, scorecards.Object,
            employees.Object, externalActions.Object, automationOutbox.Object);

        var result = await service.QueueFeedbackReminders("vacancy-1", "recruiter-1");

        Assert.True(result.Success);
        var reminder = Assert.Single(queued);
        Assert.Equal(1, result.MethodResult);
        Assert.Equal("two@example.com", reminder.RecipientEmail);
        Assert.Equal("InterviewerFeedbackReminder", reminder.EventType);
        Assert.Equal("candidate-1", reminder.RelatedApplicantId);
        Assert.Contains("Platform Engineer", reminder.Subject);
        Assert.DoesNotContain("candidate-1", reminder.HtmlBody);
        Assert.Contains("feedback-reminder:interview-1:1:", reminder.DedupeKey);
        automationOutbox.Verify(x => x.AddOne(It.IsAny<RecruitmentAutomationOutboxEvent>()), Times.Never);
    }

    [Fact]
    public async Task QueueFeedbackReminders_fails_closed_if_email_delivery_is_not_registered()
    {
        var companyId = "company-1";
        var interview = new RecruitmentInterview
        {
            InterviewId = "interview-1", CompanyId = companyId, ApplicantId = "candidate-1", VacancyId = "vacancy-1",
            Title = "Platform Engineer", Status = RecruitmentInterviewStatus.Scheduled,
            StartsAtUtc = DateTime.UtcNow.AddDays(-3), EndsAtUtc = DateTime.UtcNow.AddDays(-3).AddHours(1),
            PanelistUserIds = ["interviewer-1"], QuorumRequired = 1
        };
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([interview]);
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = companyId, EnableSla = true, SlaTracking = true, EnableAutomation = true, Automation = true });
        var scorecards = new Mock<IMongoDbRepository<RecruitmentScorecard>>();
        scorecards.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentScorecard, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<RecruitmentScorecard>());
        var automationOutbox = new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>();

        var service = CreateService(companyId, TenantContext(companyId), interviews.Object, configs.Object,
            scorecards.Object, employees: null, externalActions: null, automationOutbox: automationOutbox.Object);

        var result = await service.QueueFeedbackReminders("vacancy-1", "recruiter-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal(RecruitmentErrorCodes.WriteFailed, result.ErrorCode);
        automationOutbox.Verify(x => x.AddOne(It.IsAny<RecruitmentAutomationOutboxEvent>()), Times.Never);
    }

    [Fact]
    public async Task QueueFeedbackReminders_is_blocked_when_company_automation_is_disabled()
    {
        var companyId = "company-1";
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = companyId, EnableSla = true, SlaTracking = true, EnableAutomation = false, Automation = false });
        var service = CreateService(companyId, TenantContext(companyId), interviews.Object, configs.Object,
            new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object, employees: null, externalActions: null);

        var result = await service.QueueFeedbackReminders("vacancy-1", "recruiter-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal(RecruitmentErrorCodes.FeatureDisabled, result.ErrorCode);
        interviews.Verify(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    private static RecruitmentEnterpriseService CreateService(
        string companyId,
        IHttpContextAccessor context,
        IMongoDbRepository<RecruitmentInterview> interviews,
        IMongoDbRepository<RecruitmentEnterpriseConfig> configs,
        IMongoDbRepository<RecruitmentScorecard> scorecards,
        IMongoDbRepository<EmpUser>? employees,
        IRecruitmentExternalActionService? externalActions,
        IMongoDbRepository<RecruitmentAutomationOutboxEvent>? automationOutbox = null) => new(
            new Mock<IMongoDbRepository<Applicant>>().Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            interviews,
            new Mock<IMongoDbRepository<RecruitmentOffer>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferVersion>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferCounterRequest>>().Object,
            scorecards,
            new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>().Object,
            new Mock<IMongoDbRepository<RecruitmentHandoff>>().Object,
            new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object,
            new Mock<IMongoDbRepository<RecruitmentStageSlaPolicy>>().Object,
            configs,
            automationOutbox ?? new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>().Object,
            new Mock<IMongoDbRepository<RecruitmentRelationshipLink>>().Object,
            context,
            employees: employees,
            externalActions: externalActions);

    private static IHttpContextAccessor TenantContext(string companyId) => new HttpContextAccessor
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "test"))
        }
    };
}
