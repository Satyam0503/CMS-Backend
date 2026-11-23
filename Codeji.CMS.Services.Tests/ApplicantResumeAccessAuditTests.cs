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
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class ApplicantResumeAccessAuditTests
{
    [Fact]
    public async Task Resume_access_is_tenant_scoped_and_audited_before_it_is_allowed()
    {
        Applicant applicant = new()
        {
            CompanyId = "company-1",
            ApplicantId = "applicant-1",
            Phone = "5550101",
            VacancyId = "vacancy-1",
            ResumeUrl = "private:resume.pdf"
        };
        ApplicantLogs? capturedAudit = null;
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => applicant);
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>()))
            .Callback<ApplicantLogs>(entry => capturedAudit = entry)
            .ReturnsAsync(new Result { Success = true });

        var service = CreateService(applicants.Object, logs.Object, TenantContext("company-1"));
        var result = await service.RecordResumeAccess("applicant-1", "recruiter-7");

        Assert.True(result.Success);
        Assert.NotNull(capturedAudit);
        Assert.Equal("company-1", capturedAudit.CompanyId);
        Assert.Equal("applicant-1", capturedAudit.ApplicantId);
        Assert.Equal("recruiter-7", capturedAudit.UserId);
        Assert.Equal("ResumeViewed", capturedAudit.EventType);

        var filter = (Expression<Func<Applicant, bool>>)applicants.Invocations.Single(x => x.Method.Name == nameof(IMongoDbRepository<Applicant>.FirstOrDefault)).Arguments[0];
        Assert.True(filter.Compile()(applicant));
        Assert.False(filter.Compile()(new Applicant { CompanyId = "company-2", ApplicantId = "applicant-1", Phone = "5550102", ResumeUrl = "private:resume.pdf" }));
        Assert.False(filter.Compile()(new Applicant { CompanyId = "company-1", ApplicantId = "applicant-1", Phone = "5550103", ResumeUrl = "private:resume.pdf", IsDeleted = true }));
    }

    [Fact]
    public async Task Resume_access_fails_closed_if_audit_record_cannot_be_saved()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "applicant-1", Phone = "5550101", ResumeUrl = "private:resume.pdf" });
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Result { Success = false });

        var service = CreateService(applicants.Object, logs.Object, TenantContext("company-1"));
        var result = await service.RecordResumeAccess("applicant-1", "recruiter-7");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
    }

    [Fact]
    public async Task Resume_access_does_not_write_an_audit_for_a_missing_candidate()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Applicant?)null);
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();

        var service = CreateService(applicants.Object, logs.Object, TenantContext("company-1"));
        var result = await service.RecordResumeAccess("applicant-1", "recruiter-7");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        logs.Verify(x => x.AddOne(It.IsAny<ApplicantLogs>()), Times.Never);
    }

    private static ApplicantServices CreateService(
        IMongoDbRepository<Applicant> applicants,
        IMongoDbRepository<ApplicantLogs> logs,
        IHttpContextAccessor context) => new(
            applicants,
            new Mock<IMapper>().Object,
            new Mock<IJobVacancy>().Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<MailTemplate>>().Object,
            new Mock<IMongoDbRepository<JobVacancy>>().Object,
            new Mock<IMongoDbRepository<Roles>>().Object,
            logs,
            new Mock<IMongoDbRepository<EmpUser>>().Object,
            new Mock<IPriorityTaskQueue>().Object,
            new Mock<IMiddlewareService>().Object,
            context);

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var claims = new[] { new Claim("company_id", companyId) };
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}
