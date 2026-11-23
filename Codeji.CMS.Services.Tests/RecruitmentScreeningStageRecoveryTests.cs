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
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentScreeningStageRecoveryTests
{
    [Fact]
    public async Task IdempotentRetry_RepairsMissingCanonicalStageAfterInitialWriteFailure()
    {
        var applicant = new Applicant
        {
            CompanyId = "company-1", ApplicantId = "applicant-1", VacancyId = "vacancy-1",
            FirstName = "Sam", LastName = "Candidate", Email = "sam@example.com", Phone = "5551234",
            ActivityType = Codeji.CMS.Utility.Enums.EnumsHelper.ActivityType.New
        };
        RecruitmentScreeningReview? savedReview = null;
        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        applicantRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => applicant);
        applicantRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
            .ReturnsAsync(new Result { Success = true });

        var reviewsRepo = new Mock<IMongoDbRepository<RecruitmentScreeningReview>>();
        reviewsRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentScreeningReview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => savedReview);
        reviewsRepo.Setup(x => x.AddOne(It.IsAny<RecruitmentScreeningReview>()))
            .Callback<RecruitmentScreeningReview>(review => savedReview = review)
            .ReturnsAsync(new Result { Success = true });
        reviewsRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentScreeningReview>>(), It.IsAny<RecruitmentScreeningReview>()))
            .ReturnsAsync(new Result { Success = true });

        var canonicalAttempts = 0;
        var applicationsRepo = new Mock<IMongoDbRepository<RecruitmentJobApplication>>();
        applicationsRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentJobApplication, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentJobApplication?)null);
        applicationsRepo.Setup(x => x.AddOne(It.IsAny<RecruitmentJobApplication>()))
            .ReturnsAsync(() => new Result { Success = ++canonicalAttempts > 1 });

        var logsRepo = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logsRepo.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>())).ReturnsAsync(new Result { Success = true });
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "test"));
        var service = new RecruitmentScreeningService(applicantRepo.Object, reviewsRepo.Object, logsRepo.Object,
            new HttpContextAccessor { HttpContext = http }, applications: applicationsRepo.Object);
        var command = new RecruitmentScreeningReviewCommand
        {
            ApplicantId = "applicant-1", VacancyId = "vacancy-1", Decision = RecruitmentScreeningDecision.Pass,
            Score = 80, Criteria = [new() { Name = "Experience", Passed = true }], IdempotencyKey = "screening-command-1"
        };

        var first = await service.Upsert(command, "reviewer-1");
        var recovered = await service.RecoverPending(savedReview!.ReviewId);
        var retry = await service.Upsert(command, "reviewer-1");

        Assert.False(first.Success);
        Assert.Equal(503, first.StatusCode);
        Assert.Equal(1, recovered);
        Assert.True(retry.Success);
        Assert.Equal(RecruitmentScreeningDecision.Pass, savedReview?.Decision);
        Assert.Equal("Resolved", savedReview?.StageSyncStatus);
        Assert.Equal(1, savedReview?.Version);
        Assert.Equal(2, canonicalAttempts);
        Assert.Equal(Codeji.CMS.Utility.Enums.EnumsHelper.ActivityType.Shortlisted, applicant.ActivityType);
    }
}
