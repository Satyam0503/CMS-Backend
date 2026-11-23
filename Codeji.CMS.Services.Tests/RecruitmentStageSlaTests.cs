using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentStageSlaTests
{
    [Fact]
    public async Task StageSla_uses_location_override_and_tenant_scope_and_skips_closed_stages()
    {
        const string company = "tenant-a";
        var entered = DateTime.UtcNow.AddHours(-72);
        var appRows = new List<RecruitmentJobApplication>
        {
            Application("a-ny", company, "job-ny", "New", entered),
            Application("a-la", company, "job-la", "New", entered),
            Application("a-selected", company, "job-ny", "Selected", entered),
            Application("a-cancelled", company, "job-ny", "Cancelled", entered),
            Application("a-other", "tenant-b", "job-ny", "New", entered),
        };
        var policyRows = new List<RecruitmentStageSlaPolicy>
        {
            new() { CompanyId = company, PolicyId = "default", Stage = "New", TargetHours = 120, Enabled = true },
            new() { CompanyId = company, PolicyId = "ny", Stage = "New", Location = "New York", TargetHours = 24, Enabled = true },
            new() { CompanyId = "tenant-b", PolicyId = "foreign", Stage = "New", TargetHours = 1, Enabled = true },
        };
        var appRepo = Repo(appRows);
        var policyRepo = Repo(policyRows);
        var vacancyRepo = Repo(new List<JobVacancy>
        {
            new() { CompanyId = company, JobId = "job-ny", Title = "Engineer", Location = "New York" },
            new() { CompanyId = company, JobId = "job-la", Title = "Designer", Location = "Los Angeles" },
            new() { CompanyId = "tenant-b", JobId = "job-foreign", Title = "Foreign role", Location = "New York" },
        });
        var configs = Repo(new List<RecruitmentEnterpriseConfig>
        {
            new() { CompanyId = company, SlaTracking = true, EnableSla = true },
        });
        var service = CreateService(company, appRepo.Object, vacancyRepo.Object, policyRepo.Object, configs.Object);

        var result = await service.StageSla(null);

        Assert.True(result.Success);
        var report = Assert.IsType<RecruitmentStageSlaReport>(result.MethodResult);
        Assert.Equal(2, report.TotalTrackedApplications);
        Assert.Equal(1, report.OverdueApplications);
        var ny = Assert.Single(report.Items, x => x.ApplicantId == "a-ny");
        Assert.True(ny.Overdue);
        Assert.Equal(24, ny.TargetHours);
        Assert.Equal("Engineer", ny.VacancyTitle);
        var la = Assert.Single(report.Items, x => x.ApplicantId == "a-la");
        Assert.False(la.Overdue);
        Assert.Equal(120, la.TargetHours);
        Assert.DoesNotContain(report.Items, x => x.ApplicantId is "a-selected" or "a-cancelled" or "a-other");
    }

    [Fact]
    public async Task StageSla_does_not_invent_entered_time_when_stage_history_is_missing()
    {
        const string company = "tenant-a";
        var app = new RecruitmentJobApplication { CompanyId = company, ApplicantId = "app-1", VacancyId = "job-1", CurrentStage = "New", StageHistory = [], CreatedDate = null };
        var apps = Repo(new List<RecruitmentJobApplication> { app });
        var policies = Repo(new List<RecruitmentStageSlaPolicy> { new() { CompanyId = company, Stage = "New", TargetHours = 1, Enabled = true } });
        var vacancies = Repo(new List<JobVacancy> { new() { CompanyId = company, JobId = "job-1", Location = "Remote", Title = "Role" } });
        var configs = Repo(new List<RecruitmentEnterpriseConfig> { new() { CompanyId = company, SlaTracking = true, EnableSla = true } });

        var result = await CreateService(company, apps.Object, vacancies.Object, policies.Object, configs.Object).StageSla(null);

        Assert.True(result.Success);
        Assert.Empty(result.MethodResult!.Items);
    }

    private static RecruitmentJobApplication Application(string applicant, string company, string vacancy, string stage, DateTime entered) => new()
    {
        CompanyId = company, ApplicantId = applicant, VacancyId = vacancy, CurrentStage = stage,
        StageHistory = [new RecruitmentApplicationStageEvent { ToStage = stage, ChangedAtUtc = entered }],
    };

    private static RecruitmentEnterpriseService CreateService(string company, IMongoDbRepository<RecruitmentJobApplication> applications, IMongoDbRepository<JobVacancy> vacancies, IMongoDbRepository<RecruitmentStageSlaPolicy> policies, IMongoDbRepository<RecruitmentEnterpriseConfig> configs) => new(
        new Mock<IMongoDbRepository<Applicant>>().Object,
        new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
        new Mock<IMongoDbRepository<RecruitmentInterview>>().Object,
        new Mock<IMongoDbRepository<RecruitmentOffer>>().Object,
        new Mock<IMongoDbRepository<RecruitmentOfferVersion>>().Object,
        new Mock<IMongoDbRepository<RecruitmentOfferCounterRequest>>().Object,
        new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object,
        new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>().Object,
        new Mock<IMongoDbRepository<RecruitmentHandoff>>().Object,
        new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object,
        policies,
        configs,
        new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>().Object,
        new Mock<IMongoDbRepository<RecruitmentRelationshipLink>>().Object,
        Tenant(company),
        applications: applications,
        vacancies: vacancies);

    private static IHttpContextAccessor Tenant(string company) => new HttpContextAccessor
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", company)], "test")),
        },
    };

    private static Mock<IMongoDbRepository<T>> Repo<T>(List<T> rows) where T : class
    {
        var repo = new Mock<IMongoDbRepository<T>>();
        repo.Setup(x => x.GetAll(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, bool _, bool _) => rows.Where(filter.Compile()).ToList());
        repo.Setup(x => x.GetAggregateDataAsync<T>(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<MongoDB.Driver.ProjectionDefinition<T, T>>(), It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, MongoDB.Driver.ProjectionDefinition<T, T>? _, bool _, bool? _, string? _, int? pageNo, int? pageSize, string? _) =>
            {
                var result = rows.Where(filter.Compile()).ToList();
                if (pageNo.HasValue && pageSize.HasValue) result = result.Skip((pageNo.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
                return result;
            });
        repo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, bool _, bool _) => rows.FirstOrDefault(filter.Compile()));
        return repo;
    }
}
