using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.Services.Employees.Interface;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentLifecycleServiceTests
{
    [Fact]
    public async Task Offer_requires_authenticated_tenant_context()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        var notifications = new Mock<INotificationService>();
        var context = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var service = new RecruitmentOfferService(applicants.Object, offers.Object, logs.Object, notifications.Object, context);

        var result = await service.Upsert(new RecruitmentOfferCommand { ApplicantId = "a-1", VacancyId = "v-1", Salary = 100, Currency = "INR", Status = RecruitmentOfferStatus.Draft, IdempotencyKey = "k-1" }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
        applicants.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Offer_idempotency_is_tenant_scoped_and_replays_without_writing()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> filter, bool _, bool __) => new[] { new Applicant { ApplicantId = "a-1", CompanyId = "company-1", FirstName = "Ada", LastName = "Lovelace", VacancyId = "v-1", Phone = "1" } }.AsQueryable().FirstOrDefault(filter));
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<RecruitmentOffer, bool>> filter, bool _, bool __) => new[] { new RecruitmentOffer { OfferId = "offer-1", ApplicantId = "a-1", CompanyId = "company-1", LastCommandIdempotencyKey = "same-key", Status = RecruitmentOfferStatus.Draft } }.AsQueryable().FirstOrDefault(filter));
        var context = TenantContext("company-1");
        var service = new RecruitmentOfferService(applicants.Object, offers.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, new Mock<INotificationService>().Object, context);

        var result = await service.Upsert(new RecruitmentOfferCommand { ApplicantId = "a-1", VacancyId = "v-1", Salary = 100, Currency = "INR", Status = RecruitmentOfferStatus.Draft, IdempotencyKey = "same-key" }, "actor-1");

        Assert.True(result.Success);
        Assert.Equal("offer-1", result.MethodResult);
        offers.Verify(x => x.AddOne(It.IsAny<RecruitmentOffer>()), Times.Never);
        offers.Verify(x => x.Update(It.IsAny<FilterDefinition<RecruitmentOffer>>(), It.IsAny<RecruitmentOffer>()), Times.Never);
    }

    [Fact]
    public async Task Offer_rejects_stale_expected_version_before_update()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { ApplicantId = "a-1", CompanyId = "company-1", FirstName = "Ada", LastName = "Lovelace", VacancyId = "v-1", Phone = "1" });
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentOffer { OfferId = "offer-1", ApplicantId = "a-1", CompanyId = "company-1", LastCommandIdempotencyKey = "old", Status = RecruitmentOfferStatus.Draft, Version = 3 });
        var service = new RecruitmentOfferService(applicants.Object, offers.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, new Mock<INotificationService>().Object, TenantContext("company-1"));

        var result = await service.Upsert(new RecruitmentOfferCommand { OfferId = "offer-1", ApplicantId = "a-1", VacancyId = "v-1", Salary = 100, Currency = "INR", Status = RecruitmentOfferStatus.PendingApproval, IdempotencyKey = "new", ExpectedVersion = 2 }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.ConcurrencyConflict, result.ErrorCode);
        offers.Verify(x => x.Update(It.IsAny<FilterDefinition<RecruitmentOffer>>(), It.IsAny<RecruitmentOffer>()), Times.Never);
    }

    [Fact]
    public async Task Interview_lookup_does_not_cross_tenant_boundary()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> filter, bool _, bool __) => new[] { new Applicant { ApplicantId = "a-1", CompanyId = "company-2", Phone = "1" } }.AsQueryable().FirstOrDefault(filter));
        var service = new RecruitmentInterviewService(applicants.Object, new Mock<IMongoDbRepository<RecruitmentInterview>>().Object, new Mock<ICalendarServices>().Object, TenantContext("company-1"));

        var result = await service.Upsert(new RecruitmentInterviewCommand { ApplicantId = "a-1", VacancyId = "v-1", Title = "Screen", StartsAtUtc = DateTime.UtcNow, EndsAtUtc = DateTime.UtcNow.AddHours(1), IdempotencyKey = "k-1", Status = RecruitmentInterviewStatus.Scheduled }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
    }

    [Fact]
    public async Task Talent_pool_is_tenant_scoped_and_normalizes_tags()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { ApplicantId = "a-1", CompanyId = "company-1", FirstName = "Ada", LastName = "Lovelace", VacancyId = "v-1", Phone = "1" });
        var members = new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>();
        members.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentTalentPoolMember, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentTalentPoolMember?)null);
        members.Setup(x => x.AddOne(It.IsAny<RecruitmentTalentPoolMember>())).ReturnsAsync(new Result { Success = true });
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Result { Success = true });
        var service = new RecruitmentTalentPoolService(applicants.Object, members.Object, logs.Object, TenantContext("company-1"));

        var result = await service.Upsert(new RecruitmentTalentPoolCommand { ApplicantId = "a-1", PoolName = "Backend", Tags = [" senior ", "Senior", "dotnet"], IdempotencyKey = "pool-1", Status = RecruitmentTalentPoolStatus.Active }, "actor-1");

        Assert.True(result.Success);
        members.Verify(x => x.AddOne(It.Is<RecruitmentTalentPoolMember>(m => m.CompanyId == "company-1" && m.Tags.Count == 2 && m.Tags.Contains("senior"))), Times.Once);
    }

    [Fact]
    public async Task Talent_pool_rejects_invalid_status()
    {
        var service = new RecruitmentTalentPoolService(new Mock<IMongoDbRepository<Applicant>>().Object, new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>().Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Upsert(new RecruitmentTalentPoolCommand { ApplicantId = "a-1", PoolName = "Backend", IdempotencyKey = "pool-1", Status = "Unknown" }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.TalentPoolInvalid, result.ErrorCode);
    }
    [Fact]
    public async Task Duplicate_detection_is_tenant_scoped_and_normalizes_identifiers()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var seed = new[]
        {
            new Applicant { ApplicantId = "same", CompanyId = "company-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "+91 999-111", LinkedInUrl = "https://linkedin.com/in/ada" },
            new Applicant { ApplicantId = "other-tenant", CompanyId = "company-2", FirstName = "Ada", LastName = "Other", Email = "ada@example.com", Phone = "+91 999-111" }
        };
        applicants.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((Expression<Func<Applicant, bool>> filter, bool _, bool __) => seed.AsQueryable().Where(filter).ToArray());        var service = new RecruitmentDuplicateDetectionService(applicants.Object, TenantContext("company-1"));
        var result = await service.Find(new RecruitmentDuplicateLookupRequest { Email = " ADA@EXAMPLE.COM ", Phone = "+91 999111" });
        Assert.True(result.Success);
        Assert.Single(result.MethodResults);
        Assert.Equal("same", result.MethodResults[0].ApplicantId);
        Assert.Contains("Email", result.MethodResults[0].MatchReasons);
        Assert.Contains("Phone", result.MethodResults[0].MatchReasons);
    }

    [Fact]
    public async Task Duplicate_detection_requires_an_identifier()
    {
        var service = new RecruitmentDuplicateDetectionService(new Mock<IMongoDbRepository<Applicant>>().Object, TenantContext("company-1"));
        var result = await service.Find(new RecruitmentDuplicateLookupRequest());
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
    }
    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth"));
        return new HttpContextAccessor { HttpContext = context };
    }

    [Fact]
    public async Task Handoff_rejects_non_selected_applicant_without_inviting_employee()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { ApplicantId = "a-1", CompanyId = "company-1", ActivityType = ActivityType.Shortlisted, Phone = "1" });
        var employees = new Mock<IEmployeeService>();
        var service = new RecruitmentHandoffService(applicants.Object, new Mock<IMongoDbRepository<RecruitmentHandoff>>().Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, employees.Object, TenantContext("company-1"));

        var result = await service.HandoffToPreboarding(HandoffRequest(), "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
        employees.Verify(x => x.InviteNewEmployee(It.IsAny<InviteEmployeeDto>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handoff_invites_selected_applicant_and_records_audit_event()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { ApplicantId = "a-1", CompanyId = "company-1", ActivityType = ActivityType.Selected, FirstName = "Ada", LastName = "Lovelace", VacancyId = "v-1", Phone = "1" });
        var employees = new Mock<IEmployeeService>();
        employees.Setup(x => x.InviteNewEmployee(It.IsAny<InviteEmployeeDto>(), "actor-1")).ReturnsAsync(new Result<InviteEmployeeDto> { Success = true, MethodResult = new InviteEmployeeDto { UserId = "employee-user-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", EmployeeId = "E-1" } });
        var handoffs = new Mock<IMongoDbRepository<RecruitmentHandoff>>();
        handoffs.Setup(x => x.AddOne(It.IsAny<RecruitmentHandoff>())).ReturnsAsync(new Result { Success = true });
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Result { Success = true });
        var service = new RecruitmentHandoffService(applicants.Object, handoffs.Object, logs.Object, employees.Object, TenantContext("company-1"));

        var result = await service.HandoffToPreboarding(HandoffRequest(), "actor-1");

        Assert.True(result.Success);
        Assert.Equal("employee-user-1", result.MethodResult);
        logs.Verify(x => x.AddOne(It.Is<ApplicantLogs>(log => log.EventType == "HandoffToPreboarding" && log.CurrentStage == "Preboarding")), Times.Once);
    }

    [Fact]
    public async Task Handoff_replays_existing_idempotency_record_without_inviting_again()
    {
        var handoffs = new Mock<IMongoDbRepository<RecruitmentHandoff>>();
        handoffs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentHandoff, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentHandoff { CompanyId = "company-1", ApplicantId = "a-1", IdempotencyKey = "handoff-key", EmployeeUserId = "employee-user-1" });
        var employees = new Mock<IEmployeeService>();
        var service = new RecruitmentHandoffService(new Mock<IMongoDbRepository<Applicant>>().Object, handoffs.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, employees.Object, TenantContext("company-1"));

        var result = await service.HandoffToPreboarding(HandoffRequest(), "actor-1");

        Assert.True(result.Success);
        Assert.Equal("employee-user-1", result.MethodResult);
        employees.Verify(x => x.InviteNewEmployee(It.IsAny<InviteEmployeeDto>(), It.IsAny<string>()), Times.Never);
    }

    private static CandidateHandoffRequest HandoffRequest() => new() { ApplicantId = "a-1", IdempotencyKey = "handoff-key", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", EmployeeId = "E-1" };
    [Fact]
    public async Task Scheduled_interview_syncs_shared_calendar_and_persists_resource_id()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { ApplicantId = "a-1", CompanyId = "company-1", Phone = "1" });
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.AddOne(It.IsAny<RecruitmentInterview>())).ReturnsAsync(new Result { Success = true });
        interviews.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentInterview>>(), It.IsAny<RecruitmentInterview>())).ReturnsAsync(new Result { Success = true });
        var calendar = new Mock<ICalendarServices>();
        calendar.Setup(x => x.AddUpdateCalendarItem(It.IsAny<Codeji.CMS.DTO.Calendar.CalendarRequestDto>())).ReturnsAsync(new Result { Success = true, ResourceId = "calendar-1" });
        var service = new RecruitmentInterviewService(applicants.Object, interviews.Object, calendar.Object, TenantContext("company-1"));
        var start = DateTime.UtcNow.AddHours(2);

        var result = await service.Upsert(new RecruitmentInterviewCommand { ApplicantId = "a-1", VacancyId = "v-1", Title = "Technical screen", StartsAtUtc = start, EndsAtUtc = start.AddHours(1), IdempotencyKey = "interview-key", Status = RecruitmentInterviewStatus.Scheduled }, "actor-1");

        Assert.True(result.Success);
        Assert.Equal("Synced", (await CapturedInterview(interviews)).CalendarSyncStatus);
        calendar.Verify(x => x.AddUpdateCalendarItem(It.Is<Codeji.CMS.DTO.Calendar.CalendarRequestDto>(request => request.StartTimeUtc == start && request.EndTimeUtc == start.AddHours(1))), Times.Once);
    }

    private static async Task<RecruitmentInterview> CapturedInterview(Mock<IMongoDbRepository<RecruitmentInterview>> repository)
    {
        var capture = repository.Invocations.FirstOrDefault(invocation => invocation.Method.Name == nameof(IMongoDbRepository<RecruitmentInterview>.AddOne));
        return (RecruitmentInterview)capture!.Arguments[0];
    }
    [Fact]
    public async Task Requisition_rejects_vacancy_from_another_tenant()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((Expression<Func<JobVacancy, bool>> filter, bool _, bool __) => new[] { new JobVacancy { JobId = "v-1", CompanyId = "company-2" } }.AsQueryable().FirstOrDefault(filter));
        var service = new RecruitmentRequisitionService(vacancies.Object, new Mock<IMongoDbRepository<RecruitmentRequisition>>().Object, TenantContext("company-1"));

        var result = await service.Upsert(new RecruitmentRequisitionCommand { VacancyId = "v-1", PositionTitle = "Engineer", Headcount = 2, IdempotencyKey = "req-1" }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.JobNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Requisition_creates_pending_headcount_request_idempotently()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new JobVacancy { JobId = "v-1", CompanyId = "company-1" });
        var requisitions = new Mock<IMongoDbRepository<RecruitmentRequisition>>();
        requisitions.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentRequisition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentRequisition?)null);
        requisitions.Setup(x => x.AddOne(It.IsAny<RecruitmentRequisition>())).ReturnsAsync(new Result { Success = true });
        var service = new RecruitmentRequisitionService(vacancies.Object, requisitions.Object, TenantContext("company-1"));

        var result = await service.Upsert(new RecruitmentRequisitionCommand { VacancyId = "v-1", PositionTitle = "Engineer", Headcount = 2, IdempotencyKey = "req-1", Status = RecruitmentRequisitionStatus.PendingApproval }, "actor-1");

        Assert.True(result.Success);
        requisitions.Verify(x => x.AddOne(It.Is<RecruitmentRequisition>(request => request.Status == RecruitmentRequisitionStatus.PendingApproval && request.Headcount == 2 && request.CompanyId == "company-1")), Times.Once);
    }

    [Fact]
    public async Task Requisition_requires_approval_reference_before_headcount_approval()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new JobVacancy { JobId = "v-1", CompanyId = "company-1" });
        var requisitions = new Mock<IMongoDbRepository<RecruitmentRequisition>>();
        requisitions.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentRequisition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentRequisition { RequisitionId = "r-1", VacancyId = "v-1", CompanyId = "company-1", Status = RecruitmentRequisitionStatus.PendingApproval, Version = 1, LastCommandIdempotencyKey = "old" });
        var service = new RecruitmentRequisitionService(vacancies.Object, requisitions.Object, TenantContext("company-1"));

        var result = await service.Upsert(new RecruitmentRequisitionCommand { RequisitionId = "r-1", VacancyId = "v-1", PositionTitle = "Engineer", Headcount = 2, IdempotencyKey = "new", ExpectedVersion = 1, Status = RecruitmentRequisitionStatus.Approved }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.ApprovalRequired, result.ErrorCode);
        requisitions.Verify(x => x.Update(It.IsAny<FilterDefinition<RecruitmentRequisition>>(), It.IsAny<RecruitmentRequisition>()), Times.Never);
    }
    [Fact]
    public async Task Scorecard_rejects_invalid_rating_before_persistence()
    {
        var service = new RecruitmentScorecardService(new Mock<IMongoDbRepository<Applicant>>().Object, new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Upsert(new RecruitmentScorecardCommand { ApplicantId = "a-1", VacancyId = "v-1", ReviewerUserId = "reviewer-1", IdempotencyKey = "score-1", Criteria = [new RecruitmentScorecardCriterionCommand { Name = "Technical", Rating = 6 }] }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.ScorecardInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task Scorecard_saves_submitted_evaluation_for_current_tenant()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { ApplicantId = "a-1", CompanyId = "company-1", VacancyId = "v-1", Phone = "1" });
        var scorecards = new Mock<IMongoDbRepository<RecruitmentScorecard>>();
        scorecards.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentScorecard, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentScorecard?)null);
        scorecards.Setup(x => x.AddOne(It.IsAny<RecruitmentScorecard>())).ReturnsAsync(new Result { Success = true });
        var service = new RecruitmentScorecardService(applicants.Object, scorecards.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Upsert(new RecruitmentScorecardCommand { ApplicantId = "a-1", VacancyId = "v-1", ReviewerUserId = "reviewer-1", IdempotencyKey = "score-1", Status = RecruitmentScorecardStatus.Submitted, Criteria = [new RecruitmentScorecardCriterionCommand { Name = "Technical", Rating = 5, Notes = "Strong" }] }, "actor-1");
        Assert.True(result.Success);
        scorecards.Verify(x => x.AddOne(It.Is<RecruitmentScorecard>(card => card.CompanyId == "company-1" && card.Status == RecruitmentScorecardStatus.Submitted && card.Criteria.Single().Rating == 5)), Times.Once);
            }

    [Fact]
    public async Task Scorecard_rejects_reviewer_not_assigned_to_interview()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { ApplicantId = "a-1", CompanyId = "company-1", VacancyId = "v-1", Phone = "1" });
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentInterview { InterviewId = "i-1", CompanyId = "company-1", PanelistUserIds = ["other-reviewer"] });
        var service = new RecruitmentScorecardService(applicants.Object, new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"), interviews.Object);
        var result = await service.Upsert(new RecruitmentScorecardCommand { ApplicantId = "a-1", VacancyId = "v-1", InterviewId = "i-1", ReviewerUserId = "reviewer-1", IdempotencyKey = "score-assignment", Criteria = [new RecruitmentScorecardCriterionCommand { Name = "Technical", Rating = 4 }] }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
    }
    [Fact]
    public async Task Existing_scorecard_can_only_be_updated_by_assigned_interviewer()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { ApplicantId = "a-1", CompanyId = "company-1", VacancyId = "v-1", Phone = "1" });
        var scorecards = new Mock<IMongoDbRepository<RecruitmentScorecard>>();
        scorecards.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentScorecard, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentScorecard { ScorecardId = "s-1", ApplicantId = "a-1", CompanyId = "company-1", ReviewerUserId = "assigned-1", Status = RecruitmentScorecardStatus.Draft, Version = 1 });
        var service = new RecruitmentScorecardService(applicants.Object, scorecards.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Upsert(new RecruitmentScorecardCommand { ScorecardId = "s-1", ApplicantId = "a-1", VacancyId = "v-1", IdempotencyKey = "score-owner", Status = RecruitmentScorecardStatus.Submitted, Criteria = [new RecruitmentScorecardCriterionCommand { Name = "Technical", Rating = 4 }] }, "other-user");
        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        scorecards.Verify(x => x.Update(It.IsAny<FilterDefinition<RecruitmentScorecard>>(), It.IsAny<RecruitmentScorecard>()), Times.Never);
    }

    [Fact]
    public async Task Interview_debrief_requires_panel_quorum()
    {
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentInterview { InterviewId = "i-1", CompanyId = "company-1", PanelistUserIds = ["r-1", "r-2"], QuorumRequired = 2, Version = 1 });
        var scorecards = new Mock<IMongoDbRepository<RecruitmentScorecard>>();
        scorecards.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentScorecard, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new[] { new RecruitmentScorecard { InterviewId = "i-1", CompanyId = "company-1", Status = RecruitmentScorecardStatus.Submitted } });
        var service = new RecruitmentInterviewDebriefService(interviews.Object, scorecards.Object, TenantContext("company-1"));
        var result = await service.Complete(new RecruitmentInterviewDebriefCommand { InterviewId = "i-1", Decision = "Advance", IdempotencyKey = "debrief-quorum" }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
        interviews.Verify(x => x.Update(It.IsAny<FilterDefinition<RecruitmentInterview>>(), It.IsAny<RecruitmentInterview>()), Times.Never);
    }    [Fact]
    public async Task Interview_plan_rejects_duplicate_stage_order()
    {
        var service = new RecruitmentInterviewPlanService(new Mock<IMongoDbRepository<RecruitmentInterviewPlan>>().Object, new Mock<IMongoDbRepository<JobVacancy>>().Object, TenantContext("company-1"));
        var result = await service.Upsert(new RecruitmentInterviewPlanCommand { Name = "Default", IsDefault = true, IdempotencyKey = "plan-duplicate-order", Stages = [new RecruitmentInterviewPlanStageCommand { Order = 1, Code = "tech", Name = "Technical" }, new RecruitmentInterviewPlanStageCommand { Order = 1, Code = "hr", Name = "HR" }] }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
    }}












