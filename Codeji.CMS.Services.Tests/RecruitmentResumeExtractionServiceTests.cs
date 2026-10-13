using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;
namespace Codeji.CMS.Services.Tests;
public sealed class RecruitmentResumeExtractionServiceTests
{
    [Fact]
    public async Task Extraction_review_stays_pending_until_explicit_human_decision()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", FirstName = "Ada", LastName = "Lovelace" });
        var reviews = new Mock<IMongoDbRepository<RecruitmentResumeExtractionReview>>();
        RecruitmentResumeExtractionReview? saved = null;
        reviews.Setup(x => x.AddOne(It.IsAny<RecruitmentResumeExtractionReview>())).Callback<RecruitmentResumeExtractionReview>(item => saved = item).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var service = new RecruitmentResumeExtractionService(applicants.Object, reviews.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Upsert(new RecruitmentResumeExtractionReviewCommand { ApplicantId = "a-1", VacancyId = "v-1", Decision = RecruitmentResumeExtractionDecision.Pending, ExtractedFields = new Dictionary<string, string> { ["skills"] = "C#" }, IdempotencyKey = "extract-review-1" }, "actor-1");
        Assert.True(result.Success);
        Assert.Equal(RecruitmentResumeExtractionDecision.Pending, saved!.Decision);
    }
    [Fact]
    public async Task Approved_extraction_rejects_empty_values()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1" });
        var service = new RecruitmentResumeExtractionService(applicants.Object, new Mock<IMongoDbRepository<RecruitmentResumeExtractionReview>>().Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Upsert(new RecruitmentResumeExtractionReviewCommand { ApplicantId = "a-1", VacancyId = "v-1", Decision = RecruitmentResumeExtractionDecision.Approved, ExtractedFields = new Dictionary<string, string> { ["skills"] = " " }, IdempotencyKey = "extract-review-2" }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal("Approved resume fields must contain values; keep the review pending when extraction is incomplete.", result.Message);
    }    private static IHttpContextAccessor TenantContext(string companyId) => new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) } };
}
