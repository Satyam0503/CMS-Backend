using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.Enums;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentResumeAuthorizationTests
{
    [Theory]
    [InlineData((int)EnumsHelper.Roles.HR)]
    [InlineData((int)EnumsHelper.Roles.HRExecutive)]
    [InlineData((int)EnumsHelper.Roles.Administrator)]
    public async Task Active_tenant_hr_roles_can_access_resume(int roleType)
    {
        var service = Build(roleType: roleType);
        Assert.True(await service.CanAccessResume("app-1", "user-1"));
    }

    [Fact]
    public async Task Assigned_hiring_manager_can_access_resume_but_unrelated_employee_cannot()
    {
        var assigned = Build(vacancy: new JobVacancy { CompanyId = "tenant-1", JobId = "job-1", HiringManagerUserId = "user-1" });
        var unrelated = Build(vacancy: new JobVacancy { CompanyId = "tenant-1", JobId = "job-1", HiringManagerUserId = "someone-else", CreatedBy = "recruiter-2" });

        Assert.True(await assigned.CanAccessResume("app-1", "user-1"));
        Assert.False(await unrelated.CanAccessResume("app-1", "user-1"));
    }

    [Fact]
    public async Task Assigned_interviewer_and_active_explicit_share_can_access_resume()
    {
        var interview = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interview.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentInterview { CompanyId = "tenant-1", ApplicantId = "app-1", PanelistUserIds = ["user-1"], Status = RecruitmentInterviewStatus.Scheduled });
        Assert.True(await Build(interview: interview.Object).CanAccessResume("app-1", "user-1"));

        var share = new Mock<IMongoDbRepository<RecruitmentCandidateShare>>();
        share.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentCandidateShare, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentCandidateShare { CompanyId = "tenant-1", ApplicantId = "app-1", SharedWithUserId = "user-1", Scope = "View" });
        Assert.True(await Build(shares: share.Object).CanAccessResume("app-1", "user-1"));
    }

    [Fact]
    public async Task Explicit_share_filter_rejects_revoked_expired_wrong_scope_and_cross_tenant_shares()
    {
        var shares = new Mock<IMongoDbRepository<RecruitmentCandidateShare>>();
        shares.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentCandidateShare, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentCandidateShare { CompanyId = "tenant-1", ApplicantId = "app-1", SharedWithUserId = "user-1", Scope = "View" });
        var service = Build(shares: shares.Object);

        Assert.True(await service.CanAccessResume("app-1", "user-1"));
        var filter = (Expression<Func<RecruitmentCandidateShare, bool>>)shares.Invocations.Single(x => x.Method.Name == nameof(IMongoDbRepository<RecruitmentCandidateShare>.FirstOrDefault)).Arguments[0];
        var allows = filter.Compile();
        Assert.True(allows(new RecruitmentCandidateShare { CompanyId = "tenant-1", ApplicantId = "app-1", SharedWithUserId = "user-1", Scope = "View" }));
        Assert.False(allows(new RecruitmentCandidateShare { CompanyId = "tenant-2", ApplicantId = "app-1", SharedWithUserId = "user-1", Scope = "View" }));
        Assert.False(allows(new RecruitmentCandidateShare { CompanyId = "tenant-1", ApplicantId = "app-1", SharedWithUserId = "user-1", Scope = "Edit" }));
        Assert.False(allows(new RecruitmentCandidateShare { CompanyId = "tenant-1", ApplicantId = "app-1", SharedWithUserId = "user-1", Scope = "View", RevokedAtUtc = DateTime.UtcNow }));
        Assert.False(allows(new RecruitmentCandidateShare { CompanyId = "tenant-1", ApplicantId = "app-1", SharedWithUserId = "user-1", Scope = "View", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1) }));
    }

    [Fact]
    public async Task Disabled_user_or_missing_tenant_candidate_is_denied()
    {
        Assert.False(await Build(userEnabled: false).CanAccessResume("app-1", "user-1"));
        Assert.False(await Build(applicantExists: false).CanAccessResume("app-1", "user-1"));
    }

    [Fact]
    public async Task Direct_resume_download_service_call_enforces_record_access()
    {
        var service = Build();

        var result = await service.GetScannedResumeForDownload("app-1", "user-1");
        var authorizedResult = await Build(roleType: (int)EnumsHelper.Roles.HR)
            .GetScannedResumeForDownload("app-1", "user-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, authorizedResult.StatusCode);
    }

    private static ApplicantServices Build(
        int roleType = (int)EnumsHelper.Roles.Employee,
        JobVacancy? vacancy = null,
        IMongoDbRepository<RecruitmentInterview>? interview = null,
        IMongoDbRepository<RecruitmentCandidateShare>? shares = null,
        bool userEnabled = true,
        bool applicantExists = true)
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => applicantExists ? new Applicant { CompanyId = "tenant-1", ApplicantId = "app-1", VacancyId = "job-1", Phone = "555" } : null);
        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        employees.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => userEnabled ? new EmpUser { CompanyId = "tenant-1", UserId = "user-1", RoleId = "role-1", Status = true, FirstName = "Test", LastName = "User", Email = "test@example.com" } : null);
        var roles = new Mock<IMongoDbRepository<Roles>>();
        roles.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Roles { CompanyId = "tenant-1", RolesId = "role-1", RoleType = roleType });
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(vacancy);
        var context = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "tenant-1")], "test")) } };
        return new ApplicantServices(
            applicants.Object, new Mock<IMapper>().Object, new Mock<IJobVacancy>().Object,
            new Mock<IMongoDbRepository<Company>>().Object, new Mock<IMongoDbRepository<MailTemplate>>().Object,
            vacancies.Object, roles.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            employees.Object, new Mock<IPriorityTaskQueue>().Object, new Mock<IMiddlewareService>().Object,
            context, interviewRepository: interview, candidateShareRepository: shares);
    }
}
