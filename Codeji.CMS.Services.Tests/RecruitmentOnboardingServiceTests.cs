using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentOnboardingServiceTests
{
    [Fact]
    public async Task Completion_requires_all_tasks_to_be_completed_or_skipped()
    {
        var repository = new Mock<IMongoDbRepository<RecruitmentOnboardingCase>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOnboardingCase, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOnboardingCase { CaseId = "case-1", CompanyId = "company-1", ApplicantId = "app-1", Version = 1, Tasks = [new RecruitmentOnboardingTask { Code = "account-activation", Title = "Activate", Status = "Completed" }, new RecruitmentOnboardingTask { Code = "access", Title = "Access", Status = "Pending" }] });
        var service = new RecruitmentOnboardingService(repository.Object, TenantContext("company-1"));

        var result = await service.Update(new RecruitmentOnboardingUpdateCommand { CaseId = "case-1", Status = RecruitmentOnboardingStatus.Completed, IdempotencyKey = "complete-1", Tasks = [new RecruitmentOnboardingTaskUpdate { Code = "account-activation", Status = "Completed" }, new RecruitmentOnboardingTaskUpdate { Code = "access", Status = "Pending" }] }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
        repository.Verify(x => x.Update(It.IsAny<FilterDefinition<RecruitmentOnboardingCase>>(), It.IsAny<RecruitmentOnboardingCase>()), Times.Never);
    }

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}
