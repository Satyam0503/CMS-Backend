using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Calendar;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentCalendarAndMeetingIntegrationTests
{
    [Fact]
    public void GenerateGoogleMeetUrl_ReturnsValidFormat()
    {
        var url = RecruitmentInterviewService.GenerateGoogleMeetUrl();
        Assert.NotNull(url);
        Assert.Matches(@"^https:\/\/meet\.google\.com\/[a-z]{3}-[a-z]{4}-[a-z]{3}$", url);
    }

    [Fact]
    public void GenerateMSTeamsMeetingUrl_ReturnsValidFormat()
    {
        var url = RecruitmentInterviewService.GenerateMSTeamsMeetingUrl("test-session-123");
        Assert.NotNull(url);
        Assert.Contains("https://teams.microsoft.com/l/meetup-join/", url);
        Assert.Contains("test-session-123", url);
    }

    [Fact]
    public async Task Upsert_WhenInterviewerHasOverlappingInterview_Returns409Conflict()
    {
        var context = TenantContext("company-test");
        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        applicantRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant
            {
                ApplicantId = "app-101",
                VacancyId = "vac-1",
                CompanyId = "company-test",
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@example.com",
                Phone = "+1234567890"
            });

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser>
            {
                new() { UserId = "interviewer-1", CompanyId = "company-test", Status = true, Email = "lead@example.com", FirstName = "Lead", LastName = "Dev" }
            });

        var startTime = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(1).Date.AddHours(10), DateTimeKind.Utc);
        var endTime = startTime.AddHours(1);

        var existingInterviews = new List<RecruitmentInterview>
        {
            new()
            {
                CompanyId = "company-test",
                InterviewId = "int-existing-99",
                ApplicantId = "other-app",
                VacancyId = "vac-1",
                Title = "System Design Round",
                Status = RecruitmentInterviewStatus.Scheduled,
                StartsAtUtc = startTime,
                EndsAtUtc = endTime,
                PanelistUserIds = ["interviewer-1"]
            }
        };

        var interviewRepo = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviewRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(existingInterviews);
        interviewRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentInterview?)null);

        var service = new RecruitmentInterviewService(
            applicantRepo.Object,
            interviewRepo.Object,
            new Mock<ICalendarServices>().Object,
            context,
            employees: empRepo.Object);

        var command = new RecruitmentInterviewCommand
        {
            ApplicantId = "app-101",
            VacancyId = "vac-1",
            Title = "Backend Architecture Interview",
            RoundNumber = 1,
            StartsAtUtc = startTime.AddMinutes(15), // Overlaps with 10:00 - 11:00
            EndsAtUtc = startTime.AddMinutes(45),
            Status = RecruitmentInterviewStatus.Scheduled,
            PanelistUserIds = ["interviewer-1"],
            IdempotencyKey = "cmd-conflict-check-1"
        };

        var result = await service.Upsert(command, "actor-hr-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Contains("Interviewer schedule conflict", result.Message);
        Assert.Contains("interviewer-1", result.Message);
    }

    [Fact]
    public async Task Upsert_WhenAutoGenerateMeetingLinkEnabled_GeneratesGoogleMeetUrlAndSchedules()
    {
        var context = TenantContext("company-test");
        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        applicantRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant
            {
                ApplicantId = "app-202",
                VacancyId = "vac-2",
                CompanyId = "company-test",
                FirstName = "Alex",
                LastName = "Rivera",
                Email = "alex.rivera@example.com",
                Phone = "+1987654321"
            });

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser>
            {
                new() { UserId = "lead-dev", CompanyId = "company-test", Status = true, Email = "lead.dev@example.com", FirstName = "Tech", LastName = "Lead" }
            });

        RecruitmentInterview? capturedInterview = null;
        var interviewRepo = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviewRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<RecruitmentInterview>());
        interviewRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentInterview?)null);
        interviewRepo.Setup(x => x.AddOne(It.IsAny<RecruitmentInterview>()))
            .Callback<RecruitmentInterview>(interview => capturedInterview = interview)
            .ReturnsAsync(new Result { Success = true });

        interviewRepo.Setup(x => x.Update(It.IsAny<MongoDB.Driver.FilterDefinition<RecruitmentInterview>>(), It.IsAny<RecruitmentInterview>()))
            .ReturnsAsync(new Result { Success = true });

        var calendarServices = new Mock<ICalendarServices>();
        calendarServices.Setup(x => x.AddUpdateCalendarItem(It.IsAny<CalendarRequestDto>()))
            .ReturnsAsync(new Result { Success = true, ResourceId = "cal-item-12345" });

        var service = new RecruitmentInterviewService(
            applicantRepo.Object,
            interviewRepo.Object,
            calendarServices.Object,
            context,
            employees: empRepo.Object);

        var startTime = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(3).Date.AddHours(14), DateTimeKind.Utc);
        var endTime = startTime.AddHours(1);

        var command = new RecruitmentInterviewCommand
        {
            ApplicantId = "app-202",
            VacancyId = "vac-2",
            Title = "Coding Exercise Round",
            RoundNumber = 1,
            StartsAtUtc = startTime,
            EndsAtUtc = endTime,
            Status = RecruitmentInterviewStatus.Scheduled,
            MeetingProvider = "GoogleMeet",
            CalendarSyncProvider = "GoogleCalendar",
            AutoGenerateMeetingLink = true,
            PanelistUserIds = ["lead-dev"],
            IdempotencyKey = "cmd-autogen-meet-1"
        };

        var result = await service.Upsert(command, "actor-hr-1");

        Assert.True(result.Success);
        Assert.NotNull(capturedInterview);
        Assert.NotNull(capturedInterview.MeetingUrl);
        Assert.Matches(@"^https:\/\/meet\.google\.com\/[a-z]{3}-[a-z]{4}-[a-z]{3}$", capturedInterview.MeetingUrl);
        Assert.Equal("GoogleMeet", capturedInterview.MeetingProvider);
        Assert.Equal("Synced", capturedInterview.CalendarSyncStatus);
        Assert.Equal("cal-item-12345", capturedInterview.CalendarItemId);
    }

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "test"));
        return new HttpContextAccessor { HttpContext = context };
    }
}
