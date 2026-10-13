using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentTenantAuthorizationMatrixTests
{
    [Fact]
    public async Task Privacy_rejects_applicant_from_another_company()
    {
        var applicants = ApplicantRepositoryWithOnlyOtherTenant();
        var service = new RecruitmentCandidatePrivacyService(
            applicants.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>().Object,
            TenantContext());

        var result = await service.Apply(new RecruitmentCandidatePrivacyCommand
        {
            ApplicantId = "app-1",
            Action = "Grant",
            IdempotencyKey = "privacy-matrix"
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        applicants.Verify(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()), Times.Never);
    }

    [Fact]
    public async Task Candidate_share_rejects_applicant_from_another_company()
    {
        var applicants = ApplicantRepositoryWithOnlyOtherTenant();
        var shares = new Mock<IMongoDbRepository<RecruitmentCandidateShare>>();
        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        var service = new RecruitmentCandidateShareService(shares.Object, applicants.Object, employees.Object, TenantContext());

        var result = await service.Grant(new RecruitmentCandidateShareCommand
        {
            ApplicantId = "app-1",
            SharedWithUserId = "user-2",
            Scope = "View",
            IdempotencyKey = "share-matrix"
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        employees.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        shares.Verify(x => x.AddOne(It.IsAny<RecruitmentCandidateShare>()), Times.Never);
    }

    [Fact]
    public async Task Resume_review_rejects_applicant_from_another_company()
    {
        var applicants = ApplicantRepositoryWithOnlyOtherTenant();
        var reviews = new Mock<IMongoDbRepository<RecruitmentResumeExtractionReview>>();
        var service = new RecruitmentResumeExtractionService(
            applicants.Object,
            reviews.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            TenantContext());

        var result = await service.Upsert(new RecruitmentResumeExtractionReviewCommand
        {
            ApplicantId = "app-1",
            VacancyId = "job-1",
            ExtractedFields = new Dictionary<string, string> { ["skills"] = "C#" },
            Decision = RecruitmentResumeExtractionDecision.Pending,
            IdempotencyKey = "resume-matrix"
        }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        reviews.Verify(x => x.AddOne(It.IsAny<RecruitmentResumeExtractionReview>()), Times.Never);
    }

    [Fact]
    public async Task Screening_rejects_applicant_from_another_company()
    {
        var applicants = ApplicantRepositoryWithOnlyOtherTenant();
        var reviews = new Mock<IMongoDbRepository<RecruitmentScreeningReview>>();
        var service = new RecruitmentScreeningService(
            applicants.Object,
            reviews.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            TenantContext());

        var result = await service.Upsert(new RecruitmentScreeningReviewCommand
        {
            ApplicantId = "app-1",
            VacancyId = "job-1",
            Decision = RecruitmentScreeningDecision.Pending,
            Score = 50,
            Criteria = [new RecruitmentScreeningCriterionCommand { Name = "Skills", Passed = true }],
            IdempotencyKey = "screening-matrix"
        }, "actor-1");

        Assert.False(result.Success);
        reviews.Verify(x => x.AddOne(It.IsAny<RecruitmentScreeningReview>()), Times.Never);
    }

    [Fact]
    public async Task Scorecard_rejects_applicant_from_another_company()
    {
        var applicants = ApplicantRepositoryWithOnlyOtherTenant();
        var scorecards = new Mock<IMongoDbRepository<RecruitmentScorecard>>();
        var service = new RecruitmentScorecardService(
            applicants.Object,
            scorecards.Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            TenantContext());

        var result = await service.Upsert(new RecruitmentScorecardCommand
        {
            ApplicantId = "app-1",
            VacancyId = "job-1",
            ReviewerUserId = "actor-1",
            Criteria = [new RecruitmentScorecardCriterionCommand { Name = "Skills", Rating = 4 }],
            Status = RecruitmentScorecardStatus.Draft,
            IdempotencyKey = "scorecard-matrix"
        }, "actor-1");

        Assert.False(result.Success);
        scorecards.Verify(x => x.AddOne(It.IsAny<RecruitmentScorecard>()), Times.Never);
    }

    private static Mock<IMongoDbRepository<Applicant>> ApplicantRepositoryWithOnlyOtherTenant()
    {
        var otherTenant = new Applicant
        {
            ApplicantId = "app-1",
            CompanyId = "company-2",
            VacancyId = "job-1",
            FirstName = "Other",
            LastName = "Candidate",
            Email = "candidate@example.com",
            Phone = "123",
            ResumeUrl = string.Empty
        };
        var repository = new Mock<IMongoDbRepository<Applicant>>();
        repository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<Applicant, bool>> predicate, bool _, bool _) =>
                predicate.Compile()(otherTenant) ? otherTenant : null);
        return repository;
    }

    private static IHttpContextAccessor TenantContext()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "TestAuth"))
        };
        return new HttpContextAccessor { HttpContext = http };
    }
}