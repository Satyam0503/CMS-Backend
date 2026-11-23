using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
namespace Codeji.CMS.Services.Tests;
public sealed class RecruitmentCandidatePrivacyServiceTests
{
    [Fact]
    public async Task Anonymize_requires_reason_and_scrubs_personal_fields()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "123", State = "Karnataka", Experience = 12, CurrentCompany = "Analytical Engines", CurrentJobTitle = "Engineer", CoverLetter = "Private note", PrivacyNoticeText = "Policy snapshot", ApplicationSubmissionIdempotencyKey = "submission-key", ApplicationSubmissionPayloadHash = "payload-hash", ResumeUrl = "resume.pdf" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, Logs().Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"), NoopRedactor());
        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-1", Action = "Anonymize", IdempotencyKey = "privacy-1", Reason = "Retention request" }, "actor-1");
        Assert.True(result.Success);
        Assert.Equal("Anonymized", applicant.ConsentStatus);
        Assert.Equal(string.Empty, applicant.ResumeUrl);
        Assert.DoesNotContain("@example.com", applicant.Email);
        Assert.Empty(applicant.State);
        Assert.Equal(0, applicant.Experience);
        Assert.Null(applicant.CurrentCompany);
        Assert.Null(applicant.CurrentJobTitle);
        Assert.Null(applicant.CoverLetter);
        Assert.Null(applicant.PrivacyNoticeText);
        Assert.Null(applicant.ApplicationSubmissionIdempotencyKey);
        Assert.Null(applicant.ApplicationSubmissionPayloadHash);
    }

    [Fact]
    public async Task Anonymize_rejects_unbounded_reason_before_writing()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-reason", VacancyId = "v-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "123" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, Logs().Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"), NoopRedactor());

        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-reason", Action = "Anonymize", IdempotencyKey = "privacy-long-reason", Reason = new string('x', 501) }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Never);
    }

    [Fact]
    public async Task Privacy_audit_records_the_reason_and_idempotency_key_is_bound_to_it()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-reason-audit", VacancyId = "v-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "123" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        ApplicantLogs? audit = null;
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(() => audit);
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).Callback<ApplicantLogs>(entry => audit = entry).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, logs.Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"), NoopRedactor());

        var original = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand
        {
            ApplicantId = "a-reason-audit",
            Action = "Anonymize",
            IdempotencyKey = "privacy-reason-audit-01",
            Reason = "Candidate request\r\ncase 123"
        }, "actor-1");
        var changedReasonReplay = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand
        {
            ApplicantId = "a-reason-audit",
            Action = "Anonymize",
            IdempotencyKey = "privacy-reason-audit-01",
            Reason = "Manager request"
        }, "actor-1");

        Assert.True(original.Success, original.Message);
        Assert.Equal("Candidate privacy action: Anonymize. Reason: Candidate request case 123", audit?.Description);
        Assert.DoesNotContain('\r', audit!.Description);
        Assert.DoesNotContain('\n', audit.Description);
        Assert.False(changedReasonReplay.Success);
        Assert.Equal(409, changedReasonReplay.StatusCode);
        logs.Verify(x => x.AddOne(It.IsAny<ApplicantLogs>()), Times.Once);
    }

    [Fact]
    public async Task Anonymize_persists_the_scrubbed_record_before_deleting_its_resume()
    {
        var previousStoragePath = Environment.GetEnvironmentVariable("ATS_RESUME_STORAGE_PATH");
        var privateDirectory = Path.Combine(Path.GetTempPath(), $"ats-privacy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(privateDirectory);
        var resumePath = Path.Combine(privateDirectory, "resume.pdf");
        await File.WriteAllTextAsync(resumePath, "private resume");
        Environment.SetEnvironmentVariable("ATS_RESUME_STORAGE_PATH", privateDirectory);
        try
        {
            var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-file", VacancyId = "v-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "123", ResumeUrl = "private:resume.pdf" };
            bool resumeExistedDuringDatabaseWrite = false;
            var applicants = new Mock<IMongoDbRepository<Applicant>>();
            applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
            applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
                .Callback<FilterDefinition<Applicant>, Applicant>((_, saved) => { if (saved.AnonymizationCleanupPending) resumeExistedDuringDatabaseWrite = File.Exists(resumePath) && saved.ConsentStatus == "Anonymized" && saved.ResumeUrl == string.Empty; })
                .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
            var service = new RecruitmentCandidatePrivacyService(applicants.Object, Logs().Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"), NoopRedactor());

            var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-file", Action = "Anonymize", IdempotencyKey = "privacy-file", Reason = "Retention request" }, "actor-1");

            Assert.True(result.Success);
            Assert.True(resumeExistedDuringDatabaseWrite);
            Assert.False(File.Exists(resumePath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ATS_RESUME_STORAGE_PATH", previousStoragePath);
            if (Directory.Exists(privateDirectory)) Directory.Delete(privateDirectory, recursive: true);
        }
    }
    [Fact]
    public async Task Anonymized_candidate_cannot_be_changed_again()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", ConsentStatus = "Anonymized", FirstName = "Anonymized", LastName = "Candidate", Email = "anonymized@invalid.local", Phone = "000" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, Logs().Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"), NoopRedactor());
        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-1", Action = "Grant", IdempotencyKey = "privacy-terminal" }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Never);
    }

    [Fact]
    public async Task Anonymize_rejects_candidate_with_completed_employee_handoff()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-hired", VacancyId = "v-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "123", ConsentStatus = "Granted" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        var handoffs = new Mock<IMongoDbRepository<RecruitmentHandoff>>();
        handoffs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentHandoff, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentHandoff { CompanyId = "company-1", ApplicantId = "a-hired", EmployeeUserId = "employee-1" });
        var redactor = new Mock<IRecruitmentCandidateDataRedactor>();
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, Logs().Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"), redactor.Object, handoffs.Object);

        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-hired", Action = "Anonymize", IdempotencyKey = "privacy-hired", Reason = "Retention request" }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("Granted", applicant.ConsentStatus);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Never);
        redactor.Verify(x => x.RedactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Anonymize_retry_does_not_redact_handed_off_candidate()
    {
        var applicant = new Applicant
        {
            CompanyId = "company-1", ApplicantId = "a-hired-retry", VacancyId = "v-1",
            FirstName = "Anonymized", LastName = "Candidate", Email = "anonymized-a-hired-retry@invalid.local",
            Phone = "0000000000", ConsentStatus = "Anonymized", LastCommandIdempotencyKey = "privacy-retry",
            AnonymizationCleanupPending = true, AnonymizationCleanupEmail = "ada@example.com"
        };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        var handoffs = new Mock<IMongoDbRepository<RecruitmentHandoff>>();
        handoffs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentHandoff, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentHandoff { CompanyId = "company-1", ApplicantId = "a-hired-retry", EmployeeUserId = "employee-1" });
        var redactor = new Mock<IRecruitmentCandidateDataRedactor>();
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, Logs().Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"), redactor.Object, handoffs.Object);

        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-hired-retry", Action = "Anonymize", IdempotencyKey = "privacy-retry", Reason = "Retention request" }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.True(applicant.AnonymizationCleanupPending);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Never);
        redactor.Verify(x => x.RedactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Grant_uses_company_default_retention_policy()
    {
        var before = DateTime.UtcNow;
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "123", Version = 2 };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", DefaultCandidateRetentionDays = 90 });

        var service = new RecruitmentCandidatePrivacyService(applicants.Object, Logs().Object, configs.Object, TenantContext("company-1"), NoopRedactor());
        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-1", Action = "Grant", IdempotencyKey = "privacy-grant" }, "actor-1");

        Assert.True(result.Success);
        Assert.Equal("Granted", applicant.ConsentStatus);
        Assert.InRange(applicant.RetentionUntilUtc!.Value, before.AddDays(90), DateTime.UtcNow.AddDays(90));
        Assert.Equal("privacy-grant", applicant.LastPrivacyCommandIdempotencyKey);
        Assert.False(string.IsNullOrWhiteSpace(applicant.LastPrivacyCommandFingerprint));
    }

    [Fact]
    public async Task Reusing_privacy_idempotency_key_with_changed_command_returns_conflict()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-key", VacancyId = "v-1", Phone = "123", Version = 1 };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, Logs().Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"), NoopRedactor());

        var original = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-key", Action = "Grant", RetentionDays = 90, IdempotencyKey = "privacy-key" }, "actor-1");
        var originalRetentionUntil = applicant.RetentionUntilUtc;
        var changedReplay = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-key", Action = "Grant", RetentionDays = 365, IdempotencyKey = "privacy-key" }, "actor-1");

        Assert.True(original.Success);
        Assert.False(changedReplay.Success);
        Assert.Equal(409, changedReplay.StatusCode);
        Assert.Equal(originalRetentionUntil, applicant.RetentionUntilUtc);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Once);
    }

    [Fact]
    public async Task Anonymize_retry_records_audit_after_related_cleanup_recovers()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-audit-retry", VacancyId = "v-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "123", Version = 1 };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        ApplicantLogs? audit = null;
        logs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(() => audit);
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).Callback<ApplicantLogs>(entry => audit = entry).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var redactor = new Mock<IRecruitmentCandidateDataRedactor>();
        redactor.SetupSequence(x => x.RedactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new InvalidOperationException("temporary cleanup failure"))
            .Returns(Task.CompletedTask);
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, logs.Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"), redactor.Object);
        var command = new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-audit-retry", Action = "Anonymize", IdempotencyKey = "privacy-audit-retry", Reason = "Retention request" };

        var firstAttempt = await service.Apply(command, "actor-1");
        var retry = await service.Apply(command, "actor-1");

        Assert.False(firstAttempt.Success);
        Assert.Equal(503, firstAttempt.StatusCode);
        Assert.True(retry.Success);
        Assert.NotNull(audit);
        Assert.Equal("CandidatePrivacyChanged", audit!.EventType);
        Assert.Equal("Anonymized", audit.CurrentStage);
        Assert.Equal("privacy-audit-retry", audit.IdempotencyKey);
        logs.Verify(x => x.AddOne(It.IsAny<ApplicantLogs>()), Times.Once);
    }

    [Fact]
    public async Task Grant_rejects_retention_outside_service_bounds()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "123", Version = 1 };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();

        var service = new RecruitmentCandidatePrivacyService(applicants.Object, Logs().Object, configs.Object, TenantContext("company-1"), NoopRedactor());
        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-1", Action = "Grant", RetentionDays = 29, IdempotencyKey = "privacy-invalid" }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Never);
    }

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }

    private static Mock<IMongoDbRepository<ApplicantLogs>> Logs()
    {
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((ApplicantLogs?)null);
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        return logs;
    }

    private static IRecruitmentCandidateDataRedactor NoopRedactor()
    {
        var redactor = new Mock<IRecruitmentCandidateDataRedactor>();
        redactor.Setup(x => x.RedactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        return redactor.Object;
    }
}

