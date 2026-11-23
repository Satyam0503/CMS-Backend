using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Services.Tests;
public sealed class RecruitmentHiringDecisionServiceTests
{
    [Fact]
    public async Task Approval_requires_all_interview_rounds_to_be_debriefed()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", ActivityType = ActivityType.Shortlisted });
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync([new RecruitmentInterview { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Status = RecruitmentInterviewStatus.Scheduled, DebriefStatus = RecruitmentDebriefStatus.Pending }]);
        var service = new RecruitmentHiringDecisionService(applicants.Object, interviews.Object, new Mock<IMongoDbRepository<RecruitmentHiringDecision>>().Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Upsert(new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand { ApplicantId = "a-1", VacancyId = "v-1", Decision = RecruitmentHiringDecisionStatus.Approve, IdempotencyKey = "approve-1" }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
    }

    [Fact]
    public async Task Approval_succeeds_without_interviews_when_vacancy_workflow_is_direct()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", ActivityType = ActivityType.Shortlisted });
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([]);

        var decisions = new Mock<IMongoDbRepository<RecruitmentHiringDecision>>();
        decisions.Setup(x => x.AddOne(It.IsAny<RecruitmentHiringDecision>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        decisions.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentHiringDecision>>(), It.IsAny<RecruitmentHiringDecision>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", RecruitmentMode = "Enterprise", RequireInterviewBeforeDecision = true, AllowJobWorkflowCustomization = true });

        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new JobVacancy { CompanyId = "company-1", JobId = "v-1", WorkflowMode = "Direct", RequireInterview = false });

        var service = new RecruitmentHiringDecisionService(applicants.Object, interviews.Object, decisions.Object, logs.Object, TenantContext("company-1"), null, configs.Object, vacancies.Object);

        var result = await service.Upsert(new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand
        {
            ApplicantId = "a-1",
            VacancyId = "v-1",
            Decision = RecruitmentHiringDecisionStatus.Approve,
            IdempotencyKey = "approve-fasttrack-1",
            Notes = "Direct hire approval"
        }, "actor-1");

        Assert.True(result.Success, result.Message);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task Approval_enforces_interviews_when_vacancy_workflow_is_enterprise_even_if_company_is_simple()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", ActivityType = ActivityType.Shortlisted });

        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([]);

        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", RecruitmentMode = "Simple", RequireInterviewBeforeDecision = false, AllowJobWorkflowCustomization = true });

        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new JobVacancy { CompanyId = "company-1", JobId = "v-1", WorkflowMode = "Enterprise", RequireInterview = true });

        var service = new RecruitmentHiringDecisionService(applicants.Object, interviews.Object, new Mock<IMongoDbRepository<RecruitmentHiringDecision>>().Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"), null, configs.Object, vacancies.Object);

        var result = await service.Upsert(new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand
        {
            ApplicantId = "a-1",
            VacancyId = "v-1",
            Decision = RecruitmentHiringDecisionStatus.Approve,
            IdempotencyKey = "approve-enterprise-override-1",
            Notes = "Enterprise approval attempt"
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
        Assert.Contains("interview round must be completed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Approval_enforces_company_policy_when_company_disallows_custom_workflows_even_if_vacancy_is_direct()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", ActivityType = ActivityType.Shortlisted });

        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([]);

        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        // Company has Enterprise governance requiring interviews AND has disabled custom job workflows:
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", RecruitmentMode = "Enterprise", RequireInterviewBeforeDecision = true, AllowJobWorkflowCustomization = false });

        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        // Vacancy attempted to fast-track:
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new JobVacancy { CompanyId = "company-1", JobId = "v-1", WorkflowMode = "Direct", RequireInterview = false });

        var service = new RecruitmentHiringDecisionService(applicants.Object, interviews.Object, new Mock<IMongoDbRepository<RecruitmentHiringDecision>>().Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"), null, configs.Object, vacancies.Object);

        var result = await service.Upsert(new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand
        {
            ApplicantId = "a-1",
            VacancyId = "v-1",
            Decision = RecruitmentHiringDecisionStatus.Approve,
            IdempotencyKey = "approve-disallowed-fasttrack-1",
            Notes = "Attempted direct approval when company disabled custom workflows"
        }, "actor-1");

        // Must reject approval because company disabled per-job custom workflows and company policy requires interviews:
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
        Assert.Contains("interview round must be completed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Manager_review_policy_pauses_approval_and_does_not_advance_candidate()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-manager", VacancyId = "v-manager", ActivityType = ActivityType.Shortlisted, FirstName = "Sam", LastName = "Candidate", Phone = "555" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync([]);
        RecruitmentHiringDecision? savedDecision = null;
        var decisions = new Mock<IMongoDbRepository<RecruitmentHiringDecision>>();
        decisions.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentHiringDecision, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentHiringDecision?)null);
        decisions.Setup(x => x.AddOne(It.IsAny<RecruitmentHiringDecision>())).Callback<RecruitmentHiringDecision>(x => savedDecision = x).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        ApplicantLogs? savedLog = null;
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).Callback<ApplicantLogs>(x => savedLog = x).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", RecruitmentMode = "Simple", HiringManagerReview = true, AllowJobWorkflowCustomization = false });
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new JobVacancy { CompanyId = "company-1", JobId = "v-manager", HiringManagerUserId = "manager-1" });
        var employees = new Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.Employees.EmpUser>>();
        employees.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Codeji.CMS.Repository.Entities.Employees.EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Codeji.CMS.Repository.Entities.Employees.EmpUser { CompanyId = "company-1", UserId = "manager-1", Status = true, FirstName = "Hiring", LastName = "Manager", Email = "manager@example.test" });

        var service = new RecruitmentHiringDecisionService(applicants.Object, interviews.Object, decisions.Object, logs.Object, TenantContext("company-1"), null, configs.Object, vacancies.Object, employees.Object);
        var result = await service.Upsert(new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand { ApplicantId = "a-manager", VacancyId = "v-manager", Decision = RecruitmentHiringDecisionStatus.Approve, IdempotencyKey = "manager-approval-request-01", Notes = "Recommend hire" }, "recruiter-1");

        Assert.True(result.Success, result.Message);
        Assert.Contains("will not advance", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(savedDecision);
        Assert.Equal(RecruitmentHiringDecisionStatus.PendingManagerReview, savedDecision!.Decision);
        Assert.Equal("manager-1", savedDecision.HiringManagerUserId);
        Assert.Equal(RecruitmentManagerReviewStatus.Pending, savedDecision.ManagerReviewStatus);
        Assert.Equal(ActivityType.Shortlisted, applicant.ActivityType);
        Assert.Equal("HiringManagerReviewRequested", savedLog?.EventType);
    }

    [Fact]
    public async Task Shortlisted_candidate_can_be_manually_rejected_for_interview_no_show_with_notes()
    {
        var applicant = new Applicant
        {
            CompanyId = "company-1",
            ApplicantId = "applicant-noshow-1",
            VacancyId = "job-qa-1",
            FirstName = "Aman",
            LastName = "Singh",
            Phone = "9876543210",
            ActivityType = ActivityType.Shortlisted,
            Status = ActivityStatus.Active
        };

        var canonicalApplication = new RecruitmentJobApplication
        {
            CompanyId = "company-1",
            ApplicantId = "applicant-noshow-1",
            VacancyId = "job-qa-1",
            ApplicationId = "app-1",
            CurrentStage = "Shortlisted"
        };

        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var applications = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        applications.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(canonicalApplication);
        applications.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentJobApplication>>(), It.IsAny<RecruitmentJobApplication>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var decisions = new Mock<IMongoDbRepository<RecruitmentHiringDecision>>();
        decisions.Setup(x => x.AddOne(It.IsAny<RecruitmentHiringDecision>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        decisions.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentHiringDecision>>(), It.IsAny<RecruitmentHiringDecision>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        ApplicantLogs? capturedLog = null;
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>()))
            .Callback<ApplicantLogs>(log => capturedLog = log)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var service = new RecruitmentHiringDecisionService(
            applicants.Object,
            new Mock<IMongoDbRepository<RecruitmentInterview>>().Object,
            decisions.Object,
            logs.Object,
            TenantContext("company-1"),
            applications.Object);

        var result = await service.Upsert(new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand
        {
            ApplicantId = "applicant-noshow-1",
            VacancyId = "job-qa-1",
            Decision = RecruitmentHiringDecisionStatus.Reject,
            IdempotencyKey = "reject-noshow-1",
            Notes = "Candidate was a no-show for the scheduled technical interview and did not respond to follow-up emails."
        }, "hr-manager-1");

        Assert.True(result.Success, result.Message);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(ActivityType.Rejected, applicant.ActivityType);
        Assert.Equal("Rejected", canonicalApplication.CurrentStage);
        Assert.Contains(canonicalApplication.StageHistory, h => h.FromStage == "Shortlisted" && h.ToStage == "Rejected");

        Assert.NotNull(capturedLog);
        Assert.Equal("HiringDecisionRecorded", capturedLog!.EventType);
        Assert.Equal("Shortlisted", capturedLog.PreviousStage);
        Assert.Equal("Rejected", capturedLog.CurrentStage);
        Assert.Contains("no-show", capturedLog.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Decision_stage_projection_retries_after_partial_write_and_replays_idempotently()
    {
        var storedApplicant = new Applicant { CompanyId = "company-1", ApplicantId = "recover-decision-1", VacancyId = "job-1", FirstName = "Riya", LastName = "Candidate", Phone = "555", ActivityType = ActivityType.Shortlisted, Version = 1 };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => new Applicant { CompanyId = storedApplicant.CompanyId, ApplicantId = storedApplicant.ApplicantId, VacancyId = storedApplicant.VacancyId, FirstName = storedApplicant.FirstName, LastName = storedApplicant.LastName, Phone = storedApplicant.Phone, ActivityType = storedApplicant.ActivityType, Version = storedApplicant.Version });
        var applicantWrites = 0;
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
            .ReturnsAsync((FilterDefinition<Applicant> _, Applicant updated) =>
            {
                applicantWrites++;
                if (applicantWrites == 1) return new Codeji.CMS.Domain.Models.Result { Success = false };
                storedApplicant.ActivityType = updated.ActivityType;
                storedApplicant.Version = updated.Version;
                return new Codeji.CMS.Domain.Models.Result { Success = true };
            });

        var storedApplication = new RecruitmentJobApplication { CompanyId = "company-1", ApplicantId = storedApplicant.ApplicantId, VacancyId = storedApplicant.VacancyId, ApplicationId = "app-1", CurrentStage = "Shortlisted", Version = 1 };
        var applications = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        applications.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => new RecruitmentJobApplication { CompanyId = storedApplication.CompanyId, ApplicantId = storedApplication.ApplicantId, VacancyId = storedApplication.VacancyId, ApplicationId = storedApplication.ApplicationId, CurrentStage = storedApplication.CurrentStage, Version = storedApplication.Version, StageHistory = [.. storedApplication.StageHistory] });
        applications.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentJobApplication>>(), It.IsAny<RecruitmentJobApplication>()))
            .Callback<FilterDefinition<RecruitmentJobApplication>, RecruitmentJobApplication>((_, updated) =>
            {
                storedApplication.CurrentStage = updated.CurrentStage;
                storedApplication.Version = updated.Version;
                storedApplication.StageHistory = [.. updated.StageHistory];
            })
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        RecruitmentHiringDecision? storedDecision = null;
        var decisions = new Mock<IMongoDbRepository<RecruitmentHiringDecision>>();
        decisions.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentHiringDecision, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => storedDecision);
        decisions.Setup(x => x.AddOne(It.IsAny<RecruitmentHiringDecision>()))
            .Callback<RecruitmentHiringDecision>(decision => storedDecision = decision)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        decisions.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentHiringDecision>>(), It.IsAny<RecruitmentHiringDecision>()))
            .Callback<FilterDefinition<RecruitmentHiringDecision>, RecruitmentHiringDecision>((_, updated) => storedDecision = updated)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        ApplicantLogs? storedLog = null;
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<ApplicantLogs, bool>> filter, bool _, bool _) => storedLog is not null && filter.Compile()(storedLog) ? storedLog : null);
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>()))
            .Callback<ApplicantLogs>(log => storedLog = log)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var service = new RecruitmentHiringDecisionService(applicants.Object, new Mock<IMongoDbRepository<RecruitmentInterview>>().Object, decisions.Object, logs.Object, TenantContext("company-1"), applications.Object);
        var command = new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand { ApplicantId = storedApplicant.ApplicantId, VacancyId = storedApplicant.VacancyId, Decision = RecruitmentHiringDecisionStatus.Reject, IdempotencyKey = "reject-recovery-command-01", Notes = "Documented hiring disposition" };

        var firstAttempt = await service.Upsert(command, "recruiter-1");
        Assert.False(firstAttempt.Success);
        Assert.Equal(503, firstAttempt.StatusCode);
        Assert.Equal(RecruitmentErrorCodes.StageSyncPending, firstAttempt.ErrorCode);
        Assert.Equal("Rejected", storedApplication.CurrentStage);
        Assert.Equal(ActivityType.Shortlisted, storedApplicant.ActivityType);

        var replay = await service.Upsert(command, "recruiter-1");

        Assert.True(replay.Success, replay.Message);
        Assert.Equal(ActivityType.Rejected, storedApplicant.ActivityType);
        Assert.Equal("Rejected", storedApplication.CurrentStage);
        Assert.Single(storedApplication.StageHistory);
        Assert.Equal("Resolved", storedDecision?.StageSyncStatus);
        Assert.Equal("hiring-decision-stage:" + storedDecision?.DecisionId, storedLog?.IdempotencyKey);
        Assert.Equal(1, logs.Invocations.Count(x => x.Method.Name == nameof(IMongoDbRepository<ApplicantLogs>.AddOne)));
    }

    [Fact]
    public async Task Explicit_hold_from_shortlisted_stage_is_not_suppressed_by_recovery_ordering()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "hold-candidate-1", VacancyId = "job-1", Phone = "555", ActivityType = ActivityType.Shortlisted, Version = 1 };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var decisions = new Mock<IMongoDbRepository<RecruitmentHiringDecision>>();
        decisions.Setup(x => x.AddOne(It.IsAny<RecruitmentHiringDecision>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        decisions.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentHiringDecision>>(), It.IsAny<RecruitmentHiringDecision>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentHiringDecisionService(applicants.Object, new Mock<IMongoDbRepository<RecruitmentInterview>>().Object, decisions.Object, logs.Object, TenantContext("company-1"));

        var result = await service.Upsert(new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand
        {
            ApplicantId = applicant.ApplicantId,
            VacancyId = applicant.VacancyId,
            Decision = RecruitmentHiringDecisionStatus.Hold,
            IdempotencyKey = "hold-shortlisted-001",
            Notes = "Pause the process for a hiring team review"
        }, "recruiter-1");

        Assert.True(result.Success, result.Message);
        Assert.Equal(ActivityType.OnHold, applicant.ActivityType);
    }

    [Fact]
    public async Task Hiring_manager_decision_replays_after_stage_write_failure()
    {
        var storedApplicant = new Applicant { CompanyId = "company-1", ApplicantId = "manager-recovery-1", VacancyId = "job-1", FirstName = "Maya", LastName = "Candidate", Phone = "555", ActivityType = ActivityType.Shortlisted, Version = 1 };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => new Applicant { CompanyId = storedApplicant.CompanyId, ApplicantId = storedApplicant.ApplicantId, VacancyId = storedApplicant.VacancyId, FirstName = storedApplicant.FirstName, LastName = storedApplicant.LastName, Phone = storedApplicant.Phone, ActivityType = storedApplicant.ActivityType, Version = storedApplicant.Version });
        var applicantWrites = 0;
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
            .ReturnsAsync((FilterDefinition<Applicant> _, Applicant updated) =>
            {
                applicantWrites++;
                if (applicantWrites == 1) return new Codeji.CMS.Domain.Models.Result { Success = false };
                storedApplicant.ActivityType = updated.ActivityType;
                return new Codeji.CMS.Domain.Models.Result { Success = true };
            });

        var storedDecision = new RecruitmentHiringDecision { CompanyId = "company-1", DecisionId = "decision-1", ApplicantId = storedApplicant.ApplicantId, VacancyId = storedApplicant.VacancyId, Decision = RecruitmentHiringDecisionStatus.PendingManagerReview, HiringManagerUserId = "manager-1", ManagerReviewStatus = RecruitmentManagerReviewStatus.Pending, Version = 1 };
        var decisions = new Mock<IMongoDbRepository<RecruitmentHiringDecision>>();
        decisions.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentHiringDecision, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(() => storedDecision);
        decisions.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentHiringDecision>>(), It.IsAny<RecruitmentHiringDecision>()))
            .Callback<FilterDefinition<RecruitmentHiringDecision>, RecruitmentHiringDecision>((_, updated) => storedDecision = updated)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var employees = new Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.Employees.EmpUser>>();
        employees.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Codeji.CMS.Repository.Entities.Employees.EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Codeji.CMS.Repository.Entities.Employees.EmpUser { CompanyId = "company-1", UserId = "manager-1", FirstName = "Hiring", LastName = "Manager", Email = "manager@example.test", Status = true });
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((ApplicantLogs?)null);
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentHiringDecisionService(applicants.Object, new Mock<IMongoDbRepository<RecruitmentInterview>>().Object, decisions.Object, logs.Object, TenantContext("company-1"), employees: employees.Object);
        var command = new Codeji.CMS.DTO.Recruitments.RecruitmentManagerReviewCommand { DecisionId = storedDecision.DecisionId, Decision = RecruitmentHiringDecisionStatus.Approve, IdempotencyKey = "manager-final-review-0001", ExpectedVersion = 1, Notes = "Approved" };

        var firstAttempt = await service.ReviewAsManager(command, "manager-1");
        Assert.False(firstAttempt.Success);
        Assert.Equal(503, firstAttempt.StatusCode);
        Assert.Equal(RecruitmentErrorCodes.StageSyncPending, firstAttempt.ErrorCode);
        Assert.Equal(RecruitmentManagerReviewStatus.Approved, storedDecision.ManagerReviewStatus);
        Assert.Equal("Pending", storedDecision.StageSyncStatus);

        var replay = await service.ReviewAsManager(command, "manager-1");

        Assert.True(replay.Success, replay.Message);
        Assert.Equal(ActivityType.Selected, storedApplicant.ActivityType);
        Assert.Equal("Resolved", storedDecision.StageSyncStatus);
    }

    [Fact]
    public async Task Rejection_fails_if_notes_are_missing()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var service = new RecruitmentHiringDecisionService(
            applicants.Object,
            new Mock<IMongoDbRepository<RecruitmentInterview>>().Object,
            new Mock<IMongoDbRepository<RecruitmentHiringDecision>>().Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            TenantContext("company-1"));

        var result = await service.Upsert(new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand
        {
            ApplicantId = "applicant-1",
            VacancyId = "job-1",
            Decision = RecruitmentHiringDecisionStatus.Reject,
            IdempotencyKey = "reject-empty-notes-1",
            Notes = "" // Empty notes
        }, "hr-1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(RecruitmentErrorCodes.InvalidRequest, result.ErrorCode);
        Assert.Contains("notes are required", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}
