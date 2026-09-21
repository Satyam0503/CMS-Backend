using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentScreeningQueueServiceTests
{
    [Fact]
    public async Task Queue_marks_old_pending_review_overdue_and_stays_tenant_scoped()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> filter, bool _, bool __) => new[] { new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "1", CreatedDate = DateTime.UtcNow.AddHours(-72) }, new Applicant { CompanyId = "company-2", ApplicantId = "a-2", VacancyId = "v-1", FirstName = "Other", LastName = "Tenant", Phone = "2", CreatedDate = DateTime.UtcNow.AddHours(-72) } }.AsQueryable().Where(filter).ToArray());
        var reviews = new Mock<IMongoDbRepository<RecruitmentScreeningReview>>();
        reviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentScreeningReview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(Array.Empty<RecruitmentScreeningReview>());
        var service = new RecruitmentScreeningQueueService(applicants.Object, reviews.Object, TenantContext("company-1"));

        var result = await service.Get("v-1", 48);

        Assert.True(result.Success);
        Assert.Single(result.MethodResult!.Items);
        Assert.Equal("a-1", result.MethodResult.Items[0].ApplicantId);
        Assert.Equal(1, result.MethodResult.OverdueCount);
    }

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}

