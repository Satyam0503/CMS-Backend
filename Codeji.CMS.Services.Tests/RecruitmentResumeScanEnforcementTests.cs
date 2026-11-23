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
using Microsoft.AspNetCore.Http;
using MapsterMapper;
using Moq;
using MongoDB.Driver;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentResumeScanEnforcementTests
{
    [Fact]
    public async Task Legacy_email_upload_service_is_retired_without_applicant_lookup()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        var service = CreateService(applicants.Object, logs.Object, "tenant-resume", new StubResumeScanner(ResumeScanStatus.Clean));

        var result = await service.UploadResume(null!, "candidate@example.com");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status410Gone, result.StatusCode);
        applicants.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Recruiter_cannot_create_applicant_for_a_vacancy_outside_the_current_company()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((JobVacancy?)null);
        var service = CreateService(applicants.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            "tenant-resume", new StubResumeScanner(ResumeScanStatus.Clean), vacancies.Object);
        var command = new Codeji.CMS.DTO.Recruitments.ApplicantAddEditModel
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Phone = "9876543210",
            VacancyId = "foreign-vacancy",
            Experience = 5
        };

        var result = await service.RegisterApplicants(command);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        applicants.Verify(x => x.Exist(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>()), Times.Never);
        applicants.Verify(x => x.AddOne(It.IsAny<Applicant>()), Times.Never);
    }

    [Fact]
    public async Task Applicant_edit_rejects_invalid_experience_before_repository_access()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var service = CreateService(applicants.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            "tenant-resume", new StubResumeScanner(ResumeScanStatus.Clean));

        var result = await service.UpdateApplicants(new Codeji.CMS.DTO.Recruitments.ApplicantAddEditModel
        {
            ApplicantId = "app-1",
            Email = "ada@example.com",
            LastName = "Lovelace",
            Phone = "9876543210",
            Experience = -1
        }, "recruiter-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        applicants.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData(ResumeScanStatus.Infected, 422)]
    [InlineData(ResumeScanStatus.Unavailable, 503)]
    public async Task Recruiter_upload_never_writes_when_resume_is_infected_or_scanner_is_unavailable(ResumeScanStatus status, int expectedStatus)
    {
        const string companyId = "tenant-resume";
        var applicantRepository = new Mock<IMongoDbRepository<Applicant>>();
        applicantRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = companyId, ApplicantId = "app-1", Phone = "123", FirstName = "Candidate" });
        ApplicantLogs? scanLog = null;
        var logRepository = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logRepository.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>()))
            .Callback<ApplicantLogs>(log => scanLog = log)
            .ReturnsAsync(new Result { Success = true });
        var scanner = new StubResumeScanner(status);
        var service = CreateService(applicantRepository.Object, logRepository.Object, companyId, scanner);
        using var fileStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nexample"));
        var file = new FormFile(fileStream, 0, fileStream.Length, "File", "resume.pdf");

        var result = await service.UploadResumeForApplicant("app-1", file, "recruiter-1");

        Assert.False(result.Success);
        Assert.Equal(expectedStatus, result.StatusCode);
        Assert.Equal(status, scanner.LastResult);
        Assert.NotNull(scanLog);
        Assert.Equal(status == ResumeScanStatus.Infected ? "ResumeScanRejected" : "ResumeScanUnavailable", scanLog!.EventType);
        Assert.Empty(scanLog.ApplicantName);
        Assert.DoesNotContain("@", scanLog.Description);
        applicantRepository.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<UpdateDefinition<Applicant>>()), Times.Never);
    }

    [Fact]
    public async Task Recruiter_upload_fails_closed_when_scan_audit_cannot_be_saved()
    {
        const string companyId = "tenant-resume";
        var applicantRepository = new Mock<IMongoDbRepository<Applicant>>();
        applicantRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = companyId, ApplicantId = "app-1", Phone = "123", FirstName = "Candidate" });
        var logRepository = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logRepository.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Result { Success = false });
        var service = CreateService(applicantRepository.Object, logRepository.Object, companyId, new StubResumeScanner(ResumeScanStatus.Clean));
        using var fileStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nexample"));
        var file = new FormFile(fileStream, 0, fileStream.Length, "File", "resume.pdf");

        var result = await service.UploadResumeForApplicant("app-1", file, "recruiter-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Contains("scan could not be recorded", result.Message, StringComparison.OrdinalIgnoreCase);
        applicantRepository.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<UpdateDefinition<Applicant>>()), Times.Never);
    }

    [Fact]
    public async Task Recruiter_upload_fails_closed_when_clean_scan_has_no_file_digest()
    {
        const string companyId = "tenant-resume";
        var applicantRepository = new Mock<IMongoDbRepository<Applicant>>();
        applicantRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = companyId, ApplicantId = "app-1", Phone = "123" });
        ApplicantLogs? scanLog = null;
        var logRepository = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logRepository.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).Callback<ApplicantLogs>(log => scanLog = log)
            .ReturnsAsync(new Result { Success = true });
        var service = CreateService(applicantRepository.Object, logRepository.Object, companyId,
            new StubResumeScanner(ResumeScanStatus.Clean, includeHash: false));
        using var fileStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nexample"));
        var file = new FormFile(fileStream, 0, fileStream.Length, "File", "resume.pdf");

        var result = await service.UploadResumeForApplicant("app-1", file, "recruiter-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("ResumeScanUnavailable", scanLog?.EventType);
        applicantRepository.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<UpdateDefinition<Applicant>>()), Times.Never);
    }

    private static ApplicantServices CreateService(IMongoDbRepository<Applicant> applicants, IMongoDbRepository<ApplicantLogs> logs, string companyId, IRecruitmentResumeScanner scanner, IMongoDbRepository<JobVacancy>? vacancies = null)
    {
        var context = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId), new Claim("user_id", "recruiter-1")], "test")),
            },
        };
        return new ApplicantServices(
            applicants,
            new Mock<IMapper>().Object,
            new Mock<IJobVacancy>().Object,
            new Mock<IMongoDbRepository<Company>>().Object,
            new Mock<IMongoDbRepository<MailTemplate>>().Object,
            vacancies ?? new Mock<IMongoDbRepository<JobVacancy>>().Object,
            new Mock<IMongoDbRepository<Roles>>().Object,
            logs,
            new Mock<IMongoDbRepository<EmpUser>>().Object,
            new Mock<IPriorityTaskQueue>().Object,
            new Mock<IMiddlewareService>().Object,
            context,
            resumeScanner: scanner);
    }

    private sealed class StubResumeScanner(ResumeScanStatus status, bool includeHash = true) : IRecruitmentResumeScanner
    {
        public ResumeScanStatus LastResult { get; private set; }

        public Task<ResumeScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
        {
            LastResult = status;
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray()));
            return Task.FromResult(new ResumeScanResult(status, includeHash ? hash : null));
        }
    }
}
