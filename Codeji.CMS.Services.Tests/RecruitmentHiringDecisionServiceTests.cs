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

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}
