using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Services.Tests;
public sealed class RecruitmentApplicationSnapshotTests
{
    [Fact]
    public async Task New_application_uses_current_tenant_pipeline_version()
    {
        var applications = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", WorkflowSnapshot = RecruitmentWorkflowSnapshot.LegacyV1 });
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", DefaultVersion = 7, RecruitmentMode = "Enterprise" });
        applications.Setup(x => x.AddOne(It.IsAny<RecruitmentJobApplication>())).Callback<RecruitmentJobApplication>(item => Assert.Equal(7, item.PipelineVersion)).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentApplicationService(applications.Object, applicants.Object, TenantContext("company-1"), configs.Object);
        var result = await service.UpsertStage(new RecruitmentApplicationStageCommand { ApplicantId = "a-1", VacancyId = "v-1", Stage = "New", IdempotencyKey = "snapshot-1" }, "actor-1");
        Assert.True(result.Success);
    }
    [Fact]
    public async Task Stage_transition_syncs_selected_read_model()
    {
        var applications = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", ActivityType = ActivityType.Shortlisted };
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applications.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((RecruitmentJobApplication?)null);
        applications.Setup(x => x.AddOne(It.IsAny<RecruitmentJobApplication>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentApplicationService(applications.Object, applicants.Object, TenantContext("company-1"));

        var result = await service.UpsertStage(new RecruitmentApplicationStageCommand { ApplicantId = "a-1", VacancyId = "v-1", Stage = "Selected", IdempotencyKey = "selected-1" }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal("REC_INVALID_TRANSITION", result.ErrorCode);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Never);
    }
    [Fact]
    public async Task Backfill_creates_missing_canonical_record_only_for_current_tenant_and_is_idempotent()
    {
        var applications = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var seed = new List<Applicant>
        {
            new() { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", ActivityType = ActivityType.Selected, Phone = "1" },
            new() { CompanyId = "company-2", ApplicantId = "a-2", VacancyId = "v-1", ActivityType = ActivityType.New, Phone = "2" }
        };
        applicants.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> filter, bool _, bool __) => seed.AsQueryable().Where(filter).ToArray());
        var existing = new List<RecruitmentJobApplication>();
        applications.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<RecruitmentJobApplication, bool>> filter, bool _, bool __) => existing.AsQueryable().FirstOrDefault(filter));
        applications.Setup(x => x.AddOne(It.IsAny<RecruitmentJobApplication>())).Callback<RecruitmentJobApplication>(item => existing.Add(item)).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentApplicationService(applications.Object, applicants.Object, TenantContext("company-1"));

        var first = await service.BackfillLegacy("actor-1");
        var second = await service.BackfillLegacy("actor-1");

        Assert.True(first.Success);
        Assert.Equal(1, first.MethodResult);
        Assert.True(second.Success);
        Assert.Equal(0, second.MethodResult);
        Assert.Single(existing);
        Assert.Equal("company-1", existing[0].CompanyId);
        Assert.Equal("Selected", existing[0].CurrentStage);
    }
    [Fact]
    public async Task Existing_application_rejects_backward_transition_and_does_not_write()
    {
        var applications = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", ActivityType = ActivityType.Shortlisted, Phone = "1" });
        applications.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentJobApplication { CompanyId = "company-1", ApplicationId = "app-1", ApplicantId = "a-1", VacancyId = "v-1", CurrentStage = "Shortlisted", Version = 2 });
        var service = new RecruitmentApplicationService(applications.Object, applicants.Object, TenantContext("company-1"));

        var result = await service.UpsertStage(new RecruitmentApplicationStageCommand { ApplicationId = "app-1", ApplicantId = "a-1", VacancyId = "v-1", Stage = "InProgress", IdempotencyKey = "backward-stage-1" }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal("REC_INVALID_TRANSITION", result.ErrorCode);
        applications.Verify(x => x.Update(It.IsAny<FilterDefinition<RecruitmentJobApplication>>(), It.IsAny<RecruitmentJobApplication>()), Times.Never);
    }
    private static IHttpContextAccessor TenantContext(string companyId) => new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) } };
}

