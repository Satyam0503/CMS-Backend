using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.CareerPortal.Interface;
using Codeji.CMS.Services.Recruitments;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentVacancyTenantTests
{
    [Fact]
    public async Task Vacancy_detail_fails_closed_without_authenticated_or_explicit_company()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var service = CreateService(vacancies, new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        var result = await service.GetVacancyById("job-1");

        Assert.Null(result);
        vacancies.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Vacancy_detail_predicate_rejects_same_id_from_another_company()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var otherTenant = new JobVacancy { JobId = "job-1", CompanyId = "company-2", Title = "Private role" };
        vacancies
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<JobVacancy, bool>> predicate, bool _, bool _) =>
                predicate.Compile()(otherTenant) ? otherTenant : null);
        var service = CreateService(vacancies, new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        var result = await service.GetVacancyById("job-1", "company-1");

        Assert.Null(result);
        vacancies.Verify(x => x.FirstOrDefault(
            It.Is<Expression<Func<JobVacancy, bool>>>(predicate =>
                predicate.Compile()(new JobVacancy { JobId = "job-1", CompanyId = "company-1", Title = "Owned" }) &&
                !predicate.Compile()(otherTenant)),
            It.IsAny<bool>(),
            It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Vacancy_creation_requires_an_assigned_manager_when_company_review_is_enabled()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", HiringManagerReview = true });
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "test"));
        var service = new JobVacancyService(
            vacancies.Object,
            new Mock<IMongoDbRepository<Applicant>>().Object,
            new Mock<IMapper>().Object,
            new Mock<ICareerNotificationService>().Object,
            new HttpContextAccessor { HttpContext = httpContext },
            new Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.Employees.EmpUser>>().Object,
            configs.Object);

        var result = await service.AddJobVacancy(new JobVacancyModel { Title = "QA Engineer", Description = "Test role", Vacancies = 1 });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Assign an active hiring manager", result.Message, StringComparison.OrdinalIgnoreCase);
        vacancies.Verify(x => x.AddOne(It.IsAny<JobVacancy>()), Times.Never);
    }

    private static JobVacancyService CreateService(
        Mock<IMongoDbRepository<JobVacancy>> vacancies,
        IHttpContextAccessor context) =>
        new(
            vacancies.Object,
            new Mock<IMongoDbRepository<Applicant>>().Object,
            new Mock<IMapper>().Object,
            new Mock<ICareerNotificationService>().Object,
            context);
}
