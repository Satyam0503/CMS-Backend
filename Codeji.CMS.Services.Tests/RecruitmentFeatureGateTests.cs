using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentFeatureGateTests
{
    [Fact]
    public async Task Legacy_sla_alias_is_normalized_before_service_capability_gate()
    {
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", EnableSla = true, SlaTracking = false });

        var gate = await RecruitmentFeatureGate.Require<string>(configs.Object, "company-1", x => x.EnableSla && x.SlaTracking, "SLA");

        Assert.Null(gate);
    }

    [Fact]
    public async Task Legacy_automation_alias_is_normalized_before_service_capability_gate()
    {
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", EnableAutomation = true, Automation = false });

        var gate = await RecruitmentFeatureGate.Require<string>(configs.Object, "company-1", x => x.EnableAutomation && x.Automation, "Automation");

        Assert.Null(gate);
    }

    [Fact]
    public async Task Disabled_company_feature_denies_service_before_sensitive_reads()
    {
        var context = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("company_id", "company-1")], "test"));
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", Requisitions = false });
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var service = new RecruitmentRequisitionService(vacancies.Object, new Mock<IMongoDbRepository<RecruitmentRequisition>>().Object, context, configs.Object);

        var result = await service.Upsert(new RecruitmentRequisitionCommand
        {
            VacancyId = "vacancy-1",
            PositionTitle = "Engineer",
            Headcount = 1,
            IdempotencyKey = "request-key-123"
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal(RecruitmentErrorCodes.FeatureDisabled, result.ErrorCode);
        vacancies.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }
}
