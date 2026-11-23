using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentTalentPoolServiceTests
{
    private static IHttpContextAccessor TenantContext(string companyId, string userId = "u-1")
    {
        var claims = new[]
        {
            new Claim("company_id", companyId),
            new Claim("UserId", userId),
            new Claim(ClaimTypes.Role, "HR")
        };
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }

    [Fact]
    public async Task GetMembers_returns_filtered_members_for_company()
    {
        var members = new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>();
        var sampleList = new List<RecruitmentTalentPoolMember>
        {
            new() { MemberId = "m-1", CompanyId = "c-101", ApplicantId = "a-1", PoolName = "Silver Medalists", Status = "Active", Tags = ["react", "frontend"] },
            new() { MemberId = "m-2", CompanyId = "c-101", ApplicantId = "a-2", PoolName = "Silver Medalists", Status = "Active", Tags = ["c#", "backend"] }
        };

        members.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentTalentPoolMember, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<RecruitmentTalentPoolMember, bool>> predicate, bool _, bool _) =>
            {
                var func = predicate.Compile();
                return sampleList.Where(func).ToList();
            });

        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        var service = new RecruitmentTalentPoolService(applicants.Object, members.Object, logs.Object, TenantContext("c-101"));

        var result = await service.GetMembers("Silver Medalists", "Active");

        Assert.True(result.Success);
        Assert.Equal(2, result.MethodResult?.Count);
        Assert.Equal("m-1", result.MethodResult![0].MemberId);
    }

    [Fact]
    public async Task GetMembers_requires_valid_company_context()
    {
        var members = new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>();
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        var emptyContext = new Mock<IHttpContextAccessor>();
        emptyContext.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

        var service = new RecruitmentTalentPoolService(applicants.Object, members.Object, logs.Object, emptyContext.Object);
        var result = await service.GetMembers("Silver Medalists");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Upsert_creates_new_talent_pool_member_and_records_audit_log()
    {
        var applicants = new Mock<IMongoDbRepository<Applicant>>();
        applicants.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Applicant { ApplicantId = "app-1", CompanyId = "c-101", FirstName = "Ada", LastName = "Lovelace", VacancyId = "vac-1", Phone = "9876543210" });

        var members = new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>();
        members.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentTalentPoolMember, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentTalentPoolMember?)null);
        members.Setup(x => x.AddOne(It.IsAny<RecruitmentTalentPoolMember>()))
            .ReturnsAsync(new Result { Success = true });

        var logs = new Mock<IMongoDbRepository<ApplicantLogs>>();
        logs.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>()))
            .ReturnsAsync(new Result { Success = true });

        var service = new RecruitmentTalentPoolService(applicants.Object, members.Object, logs.Object, TenantContext("c-101"));

        var command = new RecruitmentTalentPoolCommand
        {
            ApplicantId = "app-1",
            PoolName = "Tech Leaders",
            Tags = ["lead", "architect"],
            IdempotencyKey = "pool-key-01"
        };

        var result = await service.Upsert(command, "actor-user");

        Assert.True(result.Success);
        members.Verify(x => x.AddOne(It.Is<RecruitmentTalentPoolMember>(m => m.PoolName == "Tech Leaders" && m.Tags.Contains("lead"))), Times.Once);
        logs.Verify(x => x.AddOne(It.Is<ApplicantLogs>(l => l.EventType == "TalentPoolUpdated")), Times.Once);
    }
}
