using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Recruitments.Interface;
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
    [Fact]
    public async Task Offer_direct_send_from_draft_succeeds_when_no_policy_blocks()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", Email = "candidate@example.com", FirstName = "Neha", LastName = "Singh" });
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        RecruitmentOffer? updatedOffer = null;
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer { CompanyId = "company-1", OfferId = "o-1", ApplicantId = "a-1", VacancyId = "v-1", Status = RecruitmentOfferStatus.Draft, Version = 1, Salary = 30000, TotalCtc = 57000, Currency = "INR" });
        offers.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentOffer>>(), It.IsAny<RecruitmentOffer>()))
            .Callback<FilterDefinition<RecruitmentOffer>, RecruitmentOffer>((_, entity) => updatedOffer = entity)
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", OfferApproval = true, RecruitmentMode = "Standard" });
        var policies = new Mock<IMongoDbRepository<RecruitmentOfferApprovalPolicy>>();
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOfferApprovalPolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentOfferApprovalPolicy?)null);

        var service = new RecruitmentOfferService(
            applicants.Object,
            offers.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<INotificationService>().Object,
            TenantContext("company-1", "role-admin"),
            policies.Object,
            new Mock<IMongoDbRepository<JobVacancy>>().Object,
            new Mock<IRecruitmentExternalActionService>().Object,
            null,
            configs.Object);

        var result = await service.Upsert(new RecruitmentOfferCommand
        {
            OfferId = "o-1",
            ApplicantId = "a-1",
            VacancyId = "v-1",
            Salary = 30000,
            Currency = "INR",
            Status = RecruitmentOfferStatus.Sent,
            IdempotencyKey = "send-offer-draft-1"
        }, "actor-1");

        Assert.True(result.Success);
        Assert.Equal("Offer updated.", result.Message);
        Assert.NotNull(updatedOffer);
        Assert.Equal(RecruitmentOfferStatus.Sent, updatedOffer!.Status);
        Assert.NotNull(updatedOffer.SentAtUtc);
    }

    [Fact]
    public async Task Offer_direct_send_from_draft_blocked_when_approver_role_required()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", Email = "candidate@example.com" });
        var offers = new Mock<IMongoDbRepository<RecruitmentOffer>>();
        offers.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOffer, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOffer { CompanyId = "company-1", OfferId = "o-1", ApplicantId = "a-1", VacancyId = "v-1", Status = RecruitmentOfferStatus.Draft, Version = 1, Salary = 50000, TotalCtc = 80000, Currency = "INR" });
        var policies = new Mock<IMongoDbRepository<RecruitmentOfferApprovalPolicy>>();
        policies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOfferApprovalPolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOfferApprovalPolicy { CompanyId = "company-1", VacancyId = "v-1", ApproverRoleId = "role-director", ApprovalThreshold = 10000, Currency = "INR" });
        var configs = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configs.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = "company-1", OfferApproval = true, RecruitmentMode = "Standard" });

        var service = new RecruitmentOfferService(
            applicants.Object,
            offers.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<INotificationService>().Object,
            TenantContext("company-1", "role-junior-recruiter"),
            policies.Object,
            new Mock<IMongoDbRepository<JobVacancy>>().Object,
            new Mock<IRecruitmentExternalActionService>().Object,
            null,
            configs.Object);

        var result = await service.Upsert(new RecruitmentOfferCommand
        {
            OfferId = "o-1",
            ApplicantId = "a-1",
            VacancyId = "v-1",
            Salary = 50000,
            Currency = "INR",
            Status = RecruitmentOfferStatus.Sent,
            IdempotencyKey = "send-offer-draft-blocked"
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
    }

    private static IHttpContextAccessor TenantContext(string companyId, string roleId) => new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId), new Claim("role_id", roleId)], "TestAuth")) } };
}




