using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Services.Tests;
public sealed class RecruitmentHiringDecisionServiceTests
{
    [Fact]
    public async Task Approval_requires_all_interview_rounds_to_be_debriefed()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new Applicant { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Phone = "1", ActivityType = ActivityType.Shortlisted });
        var interviews = new Mock<IMongoDbRepository<RecruitmentInterview>>();
        interviews.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentInterview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync([new RecruitmentInterview { CompanyId = "company-1", ApplicantId = "a-1", VacancyId = "v-1", Status = RecruitmentInterviewStatus.Scheduled, DebriefStatus = RecruitmentDebriefStatus.Pending }]);
        var service = new RecruitmentHiringDecisionService(applicants.Object, interviews.Object, new Mock<IMongoDbRepository<RecruitmentHiringDecision>>().Object, new Mock<IMongoDbRepository<ApplicantLogs>>().Object, TenantContext("company-1"));
        var result = await service.Upsert(new Codeji.CMS.DTO.Recruitments.RecruitmentHiringDecisionCommand { ApplicantId = "a-1", VacancyId = "v-1", Decision = RecruitmentHiringDecisionStatus.Approve, IdempotencyKey = "approve-1" }, "actor-1");
        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
    }
    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}
