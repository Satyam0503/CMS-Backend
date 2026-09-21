using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
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
    private static IHttpContextAccessor TenantContext(string companyId) => new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) } };
}
