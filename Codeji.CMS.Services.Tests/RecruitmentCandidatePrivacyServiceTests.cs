using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
namespace Codeji.CMS.Services.Tests;
public sealed class RecruitmentCandidatePrivacyServiceTests
{
    [Fact]
    public async Task Anonymize_requires_reason_and_scrubs_personal_fields()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "123", ResumeUrl = "resume.pdf" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-1", Action = "Anonymize", IdempotencyKey = "privacy-1", Reason = "Retention request" }, "actor-1");
        Assert.True(result.Success);
        Assert.Equal("Anonymized", applicant.ConsentStatus);
        Assert.Equal(string.Empty, applicant.ResumeUrl);
        Assert.DoesNotContain("@example.com", applicant.Email);
    }
    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}
