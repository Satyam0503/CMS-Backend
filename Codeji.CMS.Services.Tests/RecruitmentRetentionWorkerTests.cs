using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
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