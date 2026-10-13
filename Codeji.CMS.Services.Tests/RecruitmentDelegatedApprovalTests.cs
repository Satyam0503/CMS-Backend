using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Employees.Interface;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Services.Tests;
public sealed class RecruitmentDelegatedApprovalTests
{
    [Fact]
    public async Task Offer_approval_requires_configured_approver_role()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>(); applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1" });
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>(); offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentOffer { CompanyId = "company-1", OfferId = "o-1", ApplicantId = "a-1", VacancyId = "v-1", Status = RecruitmentOfferStatus.PendingApproval, Version = 1 });
        var policies = new Mock<IMongoDbRepository<RecruitmentOfferApprovalPolicy>>(); policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOfferApprovalPolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new RecruitmentOfferApprovalPolicy { CompanyId = "company-1", VacancyId = "v-1", ApproverRoleId = "role-approver", RequireApprovalReference = false });
        var service = new RecruitmentOfferService(applicants.Object, offers.Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, new Mock<INotificationService>().Object, TenantContext("company-1", "role-recruiter"), policies.Object, new Mock<IMongoDbRepository<JobVacancy>>().Object);
        var result = await service.Upsert(new RecruitmentOfferCommand { OfferId = "o-1", ApplicantId = "a-1", VacancyId = "v-1", Salary = 100, Currency = "INR", Status = RecruitmentOfferStatus.Approved, ApprovalReference = "approved", IdempotencyKey = "delegate-approval-1" }, "actor-1");
        Assert.False(result.Success); Assert.Equal(403, result.StatusCode); offers.Verify(x => x.Update(It.IsAny<FilterDefinition<RecruitmentOffer>>(), It.IsAny<RecruitmentOffer>()), Times.Never);
    }
    private static IHttpContextAccessor TenantContext(string companyId, string roleId) => new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId), new Claim("role_id", roleId)], "TestAuth")) } };
}
