using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Calendar;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentInterviewNotificationTests
{
    private static IHttpContextAccessor TenantContext(string companyId, string userId = "actor-1")
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("company_id", companyId),
            new Claim("user_id", userId),
            new Claim(ClaimTypes.NameIdentifier, userId)
        ], "TestAuth"));
        return new HttpContextAccessor { HttpContext = context };
    }

    [Fact]
    public async Task Interview_scheduling_queues_briefing_emails_to_panelists_and_candidate()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant
            {
                ApplicantId = "app-100",
                CompanyId = "company-1",
                FirstName = "Priya",
                LastName = "Sharma",
                Email = "priya@example.com",
                Phone = "9876543210",
                Experience = 4.5m,
                ResumeUrl = "resumes/priya_cv.pdf",
                VacancyId = "vac-100"
            });

        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentInterview?)null);
        interviews.Setup(x => x.AddOne(It.IsAny<RecruitmentInterview>()))
            .ReturnsAsync(new Result { Success = true });
        interviews.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentInterview>>(), It.IsAny<RecruitmentInterview>()))
            .ReturnsAsync(new Result { Success = true });

        var calendar = new Mock<ICalendarServices>();
        calendar.Setup(x => x.AddUpdateCalendarItem(It.IsAny<CalendarRequestDto>()))
            .ReturnsAsync(new Result { Success = true, ResourceId = "cal-item-1" });

        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        employees.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new[]
            {
                new EmpUser
                {
                    UserId = "emp-tech-lead",
                    CompanyId = "company-1",
                    FirstName = "Vikram",
                    LastName = "Malhotra",
                    Email = "vikram.lead@company.com",
                    Status = true
                }
            });

        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new JobVacancy
            {
                JobId = "vac-100",
                CompanyId = "company-1",
                Title = "Senior Full Stack Engineer"
            });

        var externalActions = new Mock<IRecruitmentExternalActionService>();
        var queuedCommands = new List<RecruitmentEmailQueueCommand>();
        externalActions.Setup(x => x.QueueEmail(It.IsAny<RecruitmentEmailQueueCommand>(), It.IsAny<string>()))
            .Callback<RecruitmentEmailQueueCommand, string>((cmd, _) => queuedCommands.Add(cmd))
            .ReturnsAsync(new Result<string> { Success = true, MethodResult = "outbox-id-1" });

        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>()))
            .ReturnsAsync(new Result { Success = true });

        var service = new RecruitmentInterviewService(
            applicants.Object,
            interviews.Object,
            calendar.Object,
            TenantContext("company-1"),
            employees.Object,
            vacancies.Object,
            externalActions.Object,
            logs.Object);

        var start = DateTime.UtcNow.AddDays(2);
        var result = await service.Upsert(new RecruitmentInterviewCommand
        {
            ApplicantId = "app-100",
            VacancyId = "vac-100",
            Title = "System Architecture & Coding",
            RoundNumber = 1,
            RoundType = "Technical",
            StartsAtUtc = start,
            EndsAtUtc = start.AddHours(1),
            Location = "https://meet.google.com/abc-tech-round",
            PanelistUserIds = ["emp-tech-lead"],
            IdempotencyKey = "sched-panelist-notify-001",
            Status = RecruitmentInterviewStatus.Scheduled
        }, "hr-recruiter-1");

        Assert.True(result.Success, $"Upsert failed: {result.Message}");

        // 1. Verify panelist notification email was queued with meeting URL, resume link and candidate details
        var panelistMail = Assert.Single(queuedCommands, c => c.EventType == "InterviewPanelistInvited");
        Assert.Equal("vikram.lead@company.com", panelistMail.RecipientEmail);
        Assert.Contains("Priya Sharma", panelistMail.Subject);
        Assert.Contains("Senior Full Stack Engineer", panelistMail.Subject);
        Assert.Contains("https://meet.google.com/abc-tech-round", panelistMail.HtmlBody);
        Assert.Contains("4.5 years", panelistMail.HtmlBody);

        // 2. Verify candidate confirmation email was queued
        var candidateMail = Assert.Single(queuedCommands, c => c.EventType == "InterviewCandidateInvited");
        Assert.Equal("priya@example.com", candidateMail.RecipientEmail);
        Assert.Contains("Senior Full Stack Engineer", candidateMail.Subject);
        Assert.Contains("https://meet.google.com/abc-tech-round", candidateMail.HtmlBody);

        // 3. Verify audit log was recorded in ApplicantLogs
        logs.Verify(x => x.AddOne(It.Is<ApplicantLogs>(l =>
            l.ApplicantId == "app-100" &&
            l.EventType == "InterviewScheduled" &&
            l.Description.Contains("Interview Round 1"))), Times.Once);
    }

    [Fact]
    public async Task Interview_auto_detects_meeting_url_when_location_contains_http_link()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant
            {
                ApplicantId = "app-200",
                CompanyId = "company-1",
                FirstName = "Aarav",
                LastName = "Patel",
                Email = "aarav@example.com",
                Phone = "9876543211",
                VacancyId = "vac-200"
            });

        RecruitmentInterview? captured = null;
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentInterview?)null);
        interviews.Setup(x => x.AddOne(It.IsAny<RecruitmentInterview>()))
            .Callback<RecruitmentInterview>(i => captured = i)
            .ReturnsAsync(new Result { Success = true });
        interviews.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentInterview>>(), It.IsAny<RecruitmentInterview>()))
            .ReturnsAsync(new Result { Success = true });

        var calendar = new Mock<ICalendarServices>();
        calendar.Setup(x => x.AddUpdateCalendarItem(It.IsAny<CalendarRequestDto>()))
            .ReturnsAsync(new Result { Success = true, ResourceId = "cal-item-2" });

        var service = new RecruitmentInterviewService(
            applicants.Object,
            interviews.Object,
            calendar.Object,
            TenantContext("company-1"));

        var start = DateTime.UtcNow.AddDays(1);
        var result = await service.Upsert(new RecruitmentInterviewCommand
        {
            ApplicantId = "app-200",
            VacancyId = "vac-200",
            Title = "Leadership Round",
            RoundNumber = 1,
            RoundType = "Management",
            StartsAtUtc = start,
            EndsAtUtc = start.AddHours(1),
            Location = "https://teams.microsoft.com/l/meetup-join/12345",
            PanelistUserIds = ["emp-manager"],
            IdempotencyKey = "sched-meeting-url-detect-001",
            Status = RecruitmentInterviewStatus.Scheduled
        }, "hr-recruiter-1");

        Assert.True(result.Success, $"Upsert failed: {result.Message}");
        Assert.NotNull(captured);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/12345", captured.MeetingUrl);
    }
}
