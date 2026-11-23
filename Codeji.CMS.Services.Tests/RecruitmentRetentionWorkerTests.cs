using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Moq;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentRetentionWorkerTests
{
    [Fact]
    public async Task ProcessOnce_UsesMaintenanceRead_AnonymizesDueCandidate_AndGuardsUpdate()
    {
        var now = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
        var applicant = DueApplicant("app-1", now, version: 4);
        var repository = new Mock<IMongoDbRepository<Applicant>>();
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false))
            .ReturnsAsync([applicant]);
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
            .ReturnsAsync(new Result { Success = true });

        var result = await RecruitmentRetentionWorker.ProcessOnce(repository.Object, now);

        Assert.Equal(1, result.Due);
        Assert.Equal(1, result.Anonymized);
        Assert.Equal(0, result.Failed);
        Assert.True(result.Duration >= TimeSpan.Zero);
        repository.Verify(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false), Times.Once);
        repository.Verify(x => x.Update(
            It.IsAny<FilterDefinition<Applicant>>(),
            It.Is<Applicant>(a => a.CompanyId == "company-1" && a.ApplicantId == "app-1" && a.ConsentStatus == "Anonymized" && a.Version == 5 && a.Email.EndsWith("@invalid.local") && a.ResumeUrl == string.Empty)), Times.Once);
    }

    [Fact]
    public async Task ProcessOnce_LimitsEachCycleToTwoHundredCandidates()
    {
        var now = DateTime.UtcNow;
        var applicants = Enumerable.Range(1, 201).Select(i => DueApplicant($"app-{i}", now)).ToList();
        var repository = new Mock<IMongoDbRepository<Applicant>>();
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false)).ReturnsAsync(applicants);
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Result { Success = true });

        var result = await RecruitmentRetentionWorker.ProcessOnce(repository.Object, now);

        Assert.Equal(200, result.Due);
        Assert.Equal(200, result.Anonymized);
        Assert.Equal(0, result.Failed);
        repository.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Exactly(200));
    }

    [Fact]
    public async Task ProcessOnce_TransfersHiredCandidateToEmployeeRetentionWithoutAnonymizing()
    {
        var now = DateTime.UtcNow;
        var applicant = DueApplicant("app-hired", now);
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false)).ReturnsAsync([applicant]);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Result { Success = true });
        var handoffs = new Mock<IMongoDbRepository<RecruitmentHandoff>>();
        handoffs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentHandoff, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentHandoff { CompanyId = "company-1", ApplicantId = "app-hired", EmployeeUserId = "employee-1" });
        var redactor = new Mock<IRecruitmentCandidateDataRedactor>();

        var result = await RecruitmentRetentionWorker.ProcessOnce(applicants.Object, now, redactor.Object, handoffs.Object);

        Assert.Equal(1, result.Due);
        Assert.Equal(0, result.Anonymized);
        Assert.Equal(1, result.TransferredToEmployee);
        Assert.Equal(0, result.Failed);
        Assert.Equal("Granted", applicant.ConsentStatus);
        Assert.Equal("app-hired@example.com", applicant.Email);
        Assert.Equal("TransferredToEmployee", applicant.RetentionDisposition);
        Assert.Equal(now, applicant.RetentionDispositionAtUtc);
        Assert.Null(applicant.RetentionUntilUtc);
        redactor.Verify(x => x.RedactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.Is<Applicant>(saved => saved.ConsentStatus == "Granted" && saved.RetentionDisposition == "TransferredToEmployee")), Times.Once);
    }

    [Fact]
    public async Task ProcessOnce_HoldsLegacyAnonymizedHireForEmployeePrivacyCleanup()
    {
        var now = DateTime.UtcNow;
        var applicant = DueApplicant("app-hired-legacy", now);
        applicant.ConsentStatus = "Anonymized";
        applicant.AnonymizedAtUtc = now.AddDays(-1);
        applicant.AnonymizationCleanupPending = true;
        applicant.AnonymizationCleanupEmail = "legacy@example.com";
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false)).ReturnsAsync([applicant]);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Result { Success = true });
        var handoffs = new Mock<IMongoDbRepository<RecruitmentHandoff>>();
        handoffs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentHandoff, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentHandoff { CompanyId = "company-1", ApplicantId = "app-hired-legacy", EmployeeUserId = "employee-1" });
        var redactor = new Mock<IRecruitmentCandidateDataRedactor>();

        var result = await RecruitmentRetentionWorker.ProcessOnce(applicants.Object, now, redactor.Object, handoffs.Object);

        Assert.Equal(1, result.Due);
        Assert.Equal(0, result.Anonymized);
        Assert.Equal(1, result.TransferredToEmployee);
        Assert.Equal(0, result.Failed);
        Assert.Equal("Anonymized", applicant.ConsentStatus);
        Assert.True(applicant.AnonymizationCleanupPending);
        Assert.Equal("TransferredToEmployee", applicant.RetentionDisposition);
        redactor.Verify(x => x.RedactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.Is<Applicant>(saved => saved.ConsentStatus == "Anonymized" && saved.AnonymizationCleanupPending && saved.RetentionDisposition == "TransferredToEmployee")), Times.Once);
    }

    [Fact]
    public async Task ProcessOnce_CountsFailedWrites_ForOperationalAlerting()
    {
        var now = DateTime.UtcNow;
        var repository = new Mock<IMongoDbRepository<Applicant>>();
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false))
            .ReturnsAsync([DueApplicant("app-failed", now)]);
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
            .ReturnsAsync(new Result { Success = false });

        var result = await RecruitmentRetentionWorker.ProcessOnce(repository.Object, now);

        Assert.Equal(1, result.Due);
        Assert.Equal(0, result.Anonymized);
        Assert.Equal(1, result.Failed);
    }

    [Fact]
    public async Task ProcessOnce_KeepsResumeFileWhenAnonymizationWriteFails()
    {
        var now = DateTime.UtcNow;
        var previousStoragePath = Environment.GetEnvironmentVariable("ATS_RESUME_STORAGE_PATH");
        var privateDirectory = Path.Combine(Path.GetTempPath(), $"ats-retention-{Guid.NewGuid():N}");
        Directory.CreateDirectory(privateDirectory);
        var resumePath = Path.Combine(privateDirectory, "resume.pdf");
        await File.WriteAllTextAsync(resumePath, "private resume");
        Environment.SetEnvironmentVariable("ATS_RESUME_STORAGE_PATH", privateDirectory);
        try
        {
            var repository = new Mock<IMongoDbRepository<Applicant>>();
            var applicant = DueApplicant("app-file-failure", now);
            applicant.ResumeUrl = "private:resume.pdf";
            repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false))
                .ReturnsAsync([applicant]);
            repository.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
                .ReturnsAsync(new Result { Success = false });

            var result = await RecruitmentRetentionWorker.ProcessOnce(repository.Object, now);

            Assert.Equal(0, result.Anonymized);
            Assert.Equal(1, result.Failed);
            Assert.True(File.Exists(resumePath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ATS_RESUME_STORAGE_PATH", previousStoragePath);
            if (Directory.Exists(privateDirectory)) Directory.Delete(privateDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ProcessOnce_RedactsRelatedRecordsAndClearsPendingFlagOnlyAfterSuccess()
    {
        var now = DateTime.UtcNow;
        var applicant = DueApplicant("app-redact", now);
        applicant.ApplicationSubmissionIdempotencyKey = "submission-key";
        applicant.ApplicationSubmissionPayloadHash = "payload-hash";
        var repository = new Mock<IMongoDbRepository<Applicant>>();
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false)).ReturnsAsync([applicant]);
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Result { Success = true });
        var redactor = new Mock<IRecruitmentCandidateDataRedactor>();
        redactor.Setup(x => x.RedactAsync("company-1", "app-redact", "app-redact@example.com", "retention-worker", now)).Returns(Task.CompletedTask);
        ApplicantLogs? audit = null;
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(() => audit);
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).Callback<ApplicantLogs>(entry => audit = entry).ReturnsAsync(new Result { Success = true });

        var result = await RecruitmentRetentionWorker.ProcessOnce(repository.Object, now, redactor.Object, logs: logs.Object);

        Assert.Equal(1, result.Anonymized);
        Assert.Equal(0, result.Failed);
        Assert.False(applicant.AnonymizationCleanupPending);
        Assert.Null(applicant.AnonymizationCleanupEmail);
        Assert.Equal(now, applicant.AnonymizationCleanupCompletedAtUtc);
        Assert.Null(applicant.ApplicationSubmissionIdempotencyKey);
        Assert.Equal("CandidateRetentionAnonymized", audit?.EventType);
        Assert.Equal("Anonymized", audit?.CurrentStage);
        Assert.DoesNotContain("app-redact@example.com", audit?.Description);
        Assert.Equal("Privacy-controlled candidate", audit?.ApplicantName);
        redactor.Verify(x => x.RedactAsync("company-1", "app-redact", "app-redact@example.com", "retention-worker", now), Times.Once);
        repository.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessOnce_keeps_cleanup_pending_when_retention_audit_cannot_be_written()
    {
        var now = DateTime.UtcNow;
        var applicant = DueApplicant("app-audit-failed", now);
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false)).ReturnsAsync([applicant]);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Result { Success = true });
        var redactor = new Mock<IRecruitmentCandidateDataRedactor>();
        redactor.Setup(x => x.RedactAsync("company-1", "app-audit-failed", "app-audit-failed@example.com", "retention-worker", now)).Returns(Task.CompletedTask);
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ApplicantLogs, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((ApplicantLogs?)null);
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Result { Success = false });

        var result = await RecruitmentRetentionWorker.ProcessOnce(applicants.Object, now, redactor.Object, logs: logs.Object);

        Assert.Equal(1, result.Anonymized);
        Assert.Equal(1, result.Failed);
        Assert.True(applicant.AnonymizationCleanupPending);
        Assert.Null(applicant.AnonymizationCleanupCompletedAtUtc);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Once);
    }

    [Fact]
    public async Task ProcessOnce_LeavesDurableCleanupPendingWhenRelatedRecordRedactionFails()
    {
        var now = DateTime.UtcNow;
        var applicant = DueApplicant("app-redact-failed", now);
        var repository = new Mock<IMongoDbRepository<Applicant>>();
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, false)).ReturnsAsync([applicant]);
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Result { Success = true });
        var redactor = new Mock<IRecruitmentCandidateDataRedactor>();
        redactor.Setup(x => x.RedactAsync("company-1", "app-redact-failed", "app-redact-failed@example.com", "retention-worker", now))
            .ThrowsAsync(new InvalidOperationException("side record is unavailable"));

        var result = await RecruitmentRetentionWorker.ProcessOnce(repository.Object, now, redactor.Object);

        Assert.Equal(1, result.Anonymized);
        Assert.Equal(1, result.Failed);
        Assert.True(applicant.AnonymizationCleanupPending);
        Assert.Equal("app-redact-failed@example.com", applicant.AnonymizationCleanupEmail);
        repository.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Once);
    }

    private static Applicant DueApplicant(string id, DateTime now, int version = 1) => new()
    {
        ApplicantId = id,
        CompanyId = "company-1",
        FirstName = "A",
        LastName = "Candidate",
        Email = $"{id}@example.com",
        Phone = "1234567890",
        VacancyId = "job-1",
        ResumeUrl = "resume.pdf",
        ConsentStatus = "Granted",
        RetentionUntilUtc = now.AddMinutes(-1),
        Version = version
    };
}
