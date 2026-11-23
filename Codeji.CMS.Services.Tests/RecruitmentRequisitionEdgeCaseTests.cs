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

public sealed class RecruitmentRequisitionEdgeCaseTests
{
    private static IHttpContextAccessor TenantContext(string companyId, string userId = "u-1")
    {
        var claims = new[]
        {
            new Claim("company_id", companyId),
            new Claim("UserId", userId),
            new Claim(ClaimTypes.Role, "Administrator")
        };
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }

    private static IMongoDbRepository<RecruitmentEnterpriseConfig> EnabledConfig(string companyId)
    {
        var repository = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentEnterpriseConfig { CompanyId = companyId, Requisitions = true, Scorecards = true });
        return repository.Object;
    }

    [Fact]
    public async Task GetByVacancy_returns_requisition_for_valid_company_and_job()
    {
        var requisitions = new Mock<IMongoDbRepository<RecruitmentRequisition>>();
        var existing = new RecruitmentRequisition
        {
            RequisitionId = "req-101",
            CompanyId = "c-101",
            VacancyId = "v-101",
            PositionTitle = "Principal Architect",
            Headcount = 2,
            Status = RecruitmentRequisitionStatus.Draft
        };

        requisitions.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentRequisition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(existing);

        var service = new RecruitmentRequisitionService(new Mock<IMongoDbRepository<JobVacancy>>().Object, requisitions.Object, TenantContext("c-101"), EnabledConfig("c-101"));

        var result = await service.GetByVacancy("v-101");

        Assert.True(result.Success);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("req-101", result.MethodResult!.RequisitionId);
        Assert.Equal("Principal Architect", result.MethodResult.PositionTitle);
    }

    [Fact]
    public async Task GetByVacancy_returns_404_when_requisition_does_not_exist()
    {
        var requisitions = new Mock<IMongoDbRepository<RecruitmentRequisition>>();
        requisitions.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentRequisition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentRequisition?)null);

        var service = new RecruitmentRequisitionService(new Mock<IMongoDbRepository<JobVacancy>>().Object, requisitions.Object, TenantContext("c-101"), EnabledConfig("c-101"));

        var result = await service.GetByVacancy("v-missing");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetByVacancy_returns_400_when_company_is_missing()
    {
        var requisitions = new Mock<IMongoDbRepository<RecruitmentRequisition>>();
        var emptyContext = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var service = new RecruitmentRequisitionService(new Mock<IMongoDbRepository<JobVacancy>>().Object, requisitions.Object, emptyContext, EnabledConfig("c-101"));

        var result = await service.GetByVacancy("v-101");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Upsert_rejects_approved_headcount_higher_than_requested()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var requisitions = new Mock<IMongoDbRepository<RecruitmentRequisition>>();
        var service = new RecruitmentRequisitionService(vacancies.Object, requisitions.Object, TenantContext("c-101"), EnabledConfig("c-101"));

        var command = new RecruitmentRequisitionCommand
        {
            VacancyId = "v-1",
            PositionTitle = "DevOps Engineer",
            Headcount = 2,
            ApprovedHeadcount = 5,
            IdempotencyKey = "key-headcount-overflow"
        };

        var result = await service.Upsert(command, "user-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.HeadcountInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task Upsert_rejects_negative_or_zero_headcount()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var requisitions = new Mock<IMongoDbRepository<RecruitmentRequisition>>();
        var service = new RecruitmentRequisitionService(vacancies.Object, requisitions.Object, TenantContext("c-101"), EnabledConfig("c-101"));

        var command = new RecruitmentRequisitionCommand
        {
            VacancyId = "v-1",
            PositionTitle = "DevOps Engineer",
            Headcount = 0,
            IdempotencyKey = "key-zero-headcount"
        };

        var result = await service.Upsert(command, "user-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.HeadcountInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task Upsert_rejects_approval_without_approval_reference()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new JobVacancy { JobId = "v-1", CompanyId = "c-101" });

        var existing = new RecruitmentRequisition
        {
            RequisitionId = "req-1",
            CompanyId = "c-101",
            VacancyId = "v-1",
            Headcount = 3,
            Status = RecruitmentRequisitionStatus.PendingApproval,
            ApprovalReference = string.Empty,
            Version = 1
        };

        var requisitions = new Mock<IMongoDbRepository<RecruitmentRequisition>>();
        requisitions.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentRequisition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(existing);

        var service = new RecruitmentRequisitionService(vacancies.Object, requisitions.Object, TenantContext("c-101"), EnabledConfig("c-101"));

        var command = new RecruitmentRequisitionCommand
        {
            RequisitionId = "req-1",
            VacancyId = "v-1",
            PositionTitle = "Lead",
            Headcount = 3,
            Status = RecruitmentRequisitionStatus.Approved,
            ApprovalReference = "",
            ExpectedVersion = 1,
            IdempotencyKey = "approve-no-ref"
        };

        var result = await service.Upsert(command, "user-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal(RecruitmentErrorCodes.ApprovalRequired, result.ErrorCode);
    }

    [Fact]
    public async Task Upsert_rejects_concurrency_conflict_when_expected_version_mismatches()
    {
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        vacancies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new JobVacancy { JobId = "v-1", CompanyId = "c-101" });

        var existing = new RecruitmentRequisition
        {
            RequisitionId = "req-1",
            CompanyId = "c-101",
            VacancyId = "v-1",
            Headcount = 2,
            Status = RecruitmentRequisitionStatus.Draft,
            Version = 5
        };

        var requisitions = new Mock<IMongoDbRepository<RecruitmentRequisition>>();
        requisitions.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentRequisition, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(existing);

        var service = new RecruitmentRequisitionService(vacancies.Object, requisitions.Object, TenantContext("c-101"), EnabledConfig("c-101"));

        var command = new RecruitmentRequisitionCommand
        {
            RequisitionId = "req-1",
            VacancyId = "v-1",
            PositionTitle = "Lead",
            Headcount = 2,
            ExpectedVersion = 4, // Mismatched (expected 4 but current is 5)
            IdempotencyKey = "concurrency-key"
        };

        var result = await service.Upsert(command, "user-1");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal(RecruitmentErrorCodes.ConcurrencyConflict, result.ErrorCode);
    }
}
