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
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"));
        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-1", Action = "Anonymize", IdempotencyKey = "privacy-1", Reason = "Retention request" }, "actor-1");
        Assert.True(result.Success);
        Assert.Equal("Anonymized", applicant.ConsentStatus);
        Assert.Equal(string.Empty, applicant.ResumeUrl);
        Assert.DoesNotContain("@example.com", applicant.Email);
    }
    [Fact]
    public async Task Anonymized_candidate_cannot_be_changed_again()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", ConsentStatus = "Anonymized", FirstName = "Anonymized", LastName = "Candidate", Email = "anonymized@invalid.local", Phone = "000" };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        var service = new RecruitmentCandidatePrivacyService(applicants.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object, TenantContext("company-1"));
        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-1", Action = "Grant", IdempotencyKey = "privacy-terminal" }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Never);
    }

    [Fact]
    public async Task Grant_uses_company_default_retention_policy()
    {
        var before = DateTime.UtcNow;
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "123", Version = 2 };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        applicants.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", DefaultCandidateRetentionDays = 90 });

        var service = new RecruitmentCandidatePrivacyService(applicants.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, configs.Object, TenantContext("company-1"));
        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-1", Action = "Grant", IdempotencyKey = "privacy-grant" }, "actor-1");

        Assert.True(result.Success);
        Assert.Equal("Granted", applicant.ConsentStatus);
        Assert.InRange(applicant.RetentionUntilUtc!.Value, before.AddDays(90), DateTime.UtcNow.AddDays(90));
    }

    [Fact]
    public async Task Grant_rejects_retention_outside_service_bounds()
    {
        var applicant = new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "123", Version = 1 };
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(applicant);
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();

        var service = new RecruitmentCandidatePrivacyService(applicants.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, configs.Object, TenantContext("company-1"));
        var result = await service.Apply(new Codeji.CMS.DTO.Recruitments.RecruitmentCandidatePrivacyCommand { ApplicantId = "a-1", Action = "Grant", RetentionDays = 29, IdempotencyKey = "privacy-invalid" }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Never);
    }

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}

