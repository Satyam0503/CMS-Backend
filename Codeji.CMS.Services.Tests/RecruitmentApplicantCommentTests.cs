using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Codeji.CMS.Utility.middlewares;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentApplicantCommentTests
{
    [Fact]
    public async Task Add_comment_uses_tenant_applicant_and_persists_sanitized_comment_with_authoritative_job_title()
    {
        ApplicantLogs? savedLog = null;
        var candidate = new Applicant
        {
            CompanyId = "tenant-1",
            ApplicantId = "app-1",
            VacancyId = "job-1",
            Phone = "555",
            FirstName = "Avery",
            LastName = "Taylor",
            ConsentStatus = "Granted",
        };
        var (service, applicants, logs) = CreateService(candidate, new JobVacancy
        {
            CompanyId = "tenant-1",
            JobId = "job-1",
            Title = "Senior Engineer",
        });
        logs.Setup(repository => repository.AddOne(It.IsAny<ApplicantLogs>()))
            .ReturnsAsync((ApplicantLogs log) =>
            {
                savedLog = log;
                return new Result { Success = true, StatusCode = 200 };
            });

        var result = await service.AddComment("recruiter-1", new CommentRequestModel
        {
            ApplicantId = " app-1 ",
            Description = "<p>Reviewed profile</p><script>alert(1)</script>",
            ActivityCategory = 2,
            JobTitle = "Forged title",
        });

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(savedLog);
        Assert.Equal("tenant-1", savedLog!.CompanyId);
        Assert.Equal("app-1", savedLog.ApplicantId);
        Assert.Equal("recruiter-1", savedLog.UserId);
        Assert.Equal("Senior Engineer", savedLog.JobRole);
        Assert.Contains("Reviewed profile", savedLog.Description);
        Assert.DoesNotContain("<script", savedLog.Description, StringComparison.OrdinalIgnoreCase);
        applicants.Verify(repository => repository.FirstOrDefault(
            It.Is<Expression<Func<Applicant, bool>>>(predicate =>
                predicate.Compile()(candidate) &&
                !predicate.Compile()(new Applicant { CompanyId = "tenant-2", ApplicantId = "app-1", VacancyId = "job-1", Phone = "555" }) &&
                !predicate.Compile()(new Applicant { CompanyId = "tenant-1", ApplicantId = "app-1", VacancyId = "job-1", Phone = "555", IsDeleted = true }) &&
                !predicate.Compile()(new Applicant { CompanyId = "tenant-1", ApplicantId = "app-1", VacancyId = "job-1", Phone = "555", AnonymizationCleanupPending = true })),
            It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Add_comment_returns_not_found_without_writing_when_candidate_is_missing()
    {
        var (service, _, logs) = CreateService(null, null);

        var result = await service.AddComment("recruiter-1", ValidComment());

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        logs.Verify(repository => repository.AddOne(It.IsAny<ApplicantLogs>()), Times.Never);
    }

    [Fact]
    public async Task Add_comment_reports_persistence_failure_instead_of_success()
    {
        var candidate = new Applicant { CompanyId = "tenant-1", ApplicantId = "app-1", VacancyId = "job-1", Phone = "555" };
        var (service, _, logs) = CreateService(candidate, null);
        logs.Setup(repository => repository.AddOne(It.IsAny<ApplicantLogs>()))
            .ReturnsAsync(new Result { Success = false, StatusCode = 500 });

        var result = await service.AddComment("recruiter-1", ValidComment());

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("REC_WRITE_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task Add_comment_rejects_oversized_comment_before_repository_access()
    {
        var (service, applicants, logs) = CreateService(null, null);
        var request = ValidComment();
        request.Description = new string('x', 10001);

        var result = await service.AddComment("recruiter-1", request);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        applicants.Verify(repository => repository.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        logs.Verify(repository => repository.AddOne(It.IsAny<ApplicantLogs>()), Times.Never);
    }

    private static CommentRequestModel ValidComment() => new()
    {
        ApplicantId = "app-1",
        Description = "<p>Helpful comment</p>",
        ActivityCategory = 2,
    };

    private static (ApplicantServices Service, Mock<IMongoDbRepository<Applicant>> Applicants, Mock<IMongoDbRepository<ApplicantLogs>> Logs) CreateService(
        Applicant? applicant,
        JobVacancy? vacancy)
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(repository => repository.FirstOrDefault(
                It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> predicate, bool _, bool _) =>
                applicant is not null && predicate.Compile()(applicant) ? applicant : null);

        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(repository => repository.FirstOrDefault(
                It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<JobVacancy, bool>> predicate, bool _, bool _) =>
                vacancy is not null && predicate.Compile()(vacancy) ? vacancy : null);

        var context = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "tenant-1")], "test")),
            },
        };
        var service = new ApplicantServices(
            applicants.Object,
            new Mock<IMapper>().Object,
            new Mock<IJobVacancy>().Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<MailTemplate>>().Object,
            vacancies.Object,
            new Mock<IMongoDbRepository<Roles>>().Object,
            logs.Object,
            new Mock<IMongoDbRepository<EmpUser>>().Object,
            new Mock<IPriorityTaskQueue>().Object,
            new Mock<IMiddlewareService>().Object,
            context);
        return (service, applicants, logs);
    }
}
