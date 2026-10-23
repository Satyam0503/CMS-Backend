using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentScreeningServiceFlexibleThresholdTests
{
    private readonly Mock<IMongoDbRepository<Applicant>> _applicantsMock = new();
    private readonly Mock<IMongoDbRepository<RecruitmentScreeningReview>> _reviewsMock = new();
    private readonly Mock<IMongoDbRepository<ApplicantLogs>> _logsMock = new();
    private readonly Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>> _configsMock = new();
    private readonly Mock<IMongoDbRepository<JobVacancy>> _vacanciesMock = new();
    private readonly Mock<IMongoDbRepository<EmpUser>> _employeesMock = new();

    public RecruitmentScreeningServiceFlexibleThresholdTests()
    {
        _reviewsMock.Setup(x => x.AddOne(It.IsAny<RecruitmentScreeningReview>()))
            .ReturnsAsync(new Result { Success = true });
        _reviewsMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentScreeningReview, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((RecruitmentScreeningReview?)null);
        _applicantsMock.Setup(x => x.Update(It.IsAny<FilterDefinition<Applicant>>(), It.IsAny<Applicant>()))
            .ReturnsAsync(new Result { Success = true });
        _logsMock.Setup(x => x.AddOne(It.IsAny<ApplicantLogs>()))
            .ReturnsAsync(new Result { Success = true });
    }

    private static IHttpContextAccessor TenantContext(string companyId, string? userId = "hr-user-1")
    {
        var claims = new List<Claim> { new("company_id", companyId) };
        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new(ClaimTypes.NameIdentifier, userId));
        }
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        return new HttpContextAccessor { HttpContext = context };
    }

    private void SetupCommonApplicant(string companyId = "comp-1", string applicantId = "app-101", string vacancyId = "vac-201")
    {
        var applicant = new Applicant
        {
            CompanyId = companyId,
            ApplicantId = applicantId,
            VacancyId = vacancyId,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Phone = "1234567890",
            IsDeleted = false
        };
        _applicantsMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Applicant, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(applicant);
    }

    [Fact]
    public async Task SimpleMode_WithoutJobThreshold_AllowsPassingWithAnyValidScore()
    {
        SetupCommonApplicant();

        var config = new RecruitmentEnterpriseConfig
        {
            CompanyId = "comp-1",
            RecruitmentMode = "Simple",
            AutoAdvanceOnScreening = true,
            ScreeningPassScore = 60,
            AllowJobWorkflowCustomization = false,
            ScreeningCriteria = ["Relevant experience"],
        };
        _configsMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(config);

        var vacancy = new JobVacancy
        {
            CompanyId = "comp-1",
            JobId = "vac-201",
            WorkflowMode = "Inherit",
            ScreeningPassScore = null,
            ScreeningCriteria = ["Relevant experience"],
        };
        _vacanciesMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(vacancy);

        var service = new RecruitmentScreeningService(
            _applicantsMock.Object,
            _reviewsMock.Object,
            _logsMock.Object,
            TenantContext("comp-1"),
            _configsMock.Object,
            _employeesMock.Object,
            vacancies: _vacanciesMock.Object);

        var command = new RecruitmentScreeningReviewCommand
        {
            ApplicantId = "app-101",
            VacancyId = "vac-201",
            Decision = "Pass",
            Score = 35, // Score is below 60, but Simple mode has no threshold gate
            Criteria = [new() { Name = "Relevant experience", Passed = true }],
            IdempotencyKey = "test-key-simple"
        };

        var result = await service.Upsert(command, "hr-user-1");

        Assert.True(result.Success, $"Expected success in Simple mode without threshold, but got: {result.Message}");
    }

    [Fact]
    public async Task DirectHireWorkflow_BypassesScreeningScoreThreshold()
    {
        SetupCommonApplicant();

        var config = new RecruitmentEnterpriseConfig
        {
            CompanyId = "comp-1",
            RecruitmentMode = "Standard",
            AutoAdvanceOnScreening = true,
            ScreeningPassScore = 70,
            ScreeningCriteria = ["Direct Verification"],
        };
        _configsMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(config);

        var vacancy = new JobVacancy
        {
            CompanyId = "comp-1",
            JobId = "vac-201",
            WorkflowMode = "Direct", // Fast-track direct hire role
            ScreeningPassScore = 70,
            ScreeningCriteria = ["Direct Verification"],
        };
        _vacanciesMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(vacancy);

        var service = new RecruitmentScreeningService(
            _applicantsMock.Object,
            _reviewsMock.Object,
            _logsMock.Object,
            TenantContext("comp-1"),
            _configsMock.Object,
            _employeesMock.Object,
            vacancies: _vacanciesMock.Object);

        var command = new RecruitmentScreeningReviewCommand
        {
            ApplicantId = "app-101",
            VacancyId = "vac-201",
            Decision = "Pass",
            Score = 25, // Score is 25, but Direct Hire bypasses gate
            Criteria = [new() { Name = "Direct Verification", Passed = true }],
            IdempotencyKey = "test-key-direct"
        };

        var result = await service.Upsert(command, "hr-user-1");

        Assert.True(result.Success, $"Expected direct hire to bypass threshold gate, but got: {result.Message}");
    }

    [Fact]
    public async Task JobSpecificThreshold_StrictlyEnforcesMinimumScore()
    {
        SetupCommonApplicant();

        var config = new RecruitmentEnterpriseConfig
        {
            CompanyId = "comp-1",
            RecruitmentMode = "Simple",
            AllowJobWorkflowCustomization = true,
            ScreeningCriteria = ["Coding assessment"],
        };
        _configsMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(config);

        var vacancy = new JobVacancy
        {
            CompanyId = "comp-1",
            JobId = "vac-201",
            WorkflowMode = "Standard",
            ScreeningPassScore = 80, // Explicit 80% cutoff for this role
            ScreeningCriteria = ["Coding assessment"],
        };
        _vacanciesMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(vacancy);

        var service = new RecruitmentScreeningService(
            _applicantsMock.Object,
            _reviewsMock.Object,
            _logsMock.Object,
            TenantContext("comp-1"),
            _configsMock.Object,
            _employeesMock.Object,
            vacancies: _vacanciesMock.Object);

        // Submitting with 75 (< 80) must fail
        var failingCommand = new RecruitmentScreeningReviewCommand
        {
            ApplicantId = "app-101",
            VacancyId = "vac-201",
            Decision = "Pass",
            Score = 75,
            Criteria = [new() { Name = "Coding assessment", Passed = true }],
            IdempotencyKey = "test-key-fail-threshold"
        };
        var failResult = await service.Upsert(failingCommand, "hr-user-1");
        Assert.False(failResult.Success);
        Assert.Contains("at least 80", failResult.Message);

        // Submitting with 80 (>= 80) must succeed
        var passingCommand = new RecruitmentScreeningReviewCommand
        {
            ApplicantId = "app-101",
            VacancyId = "vac-201",
            Decision = "Pass",
            Score = 80,
            Criteria = [new() { Name = "Coding assessment", Passed = true }],
            IdempotencyKey = "test-key-pass-threshold"
        };
        var passResult = await service.Upsert(passingCommand, "hr-user-1");
        Assert.True(passResult.Success, $"Expected pass with score 80 to succeed, but got: {passResult.Message}");
    }

    [Fact]
    public async Task StandardMode_WithAutoAdvance_EnforcesCompanyPolicyWhenVacancyHasNoThreshold()
    {
        SetupCommonApplicant();

        var config = new RecruitmentEnterpriseConfig
        {
            CompanyId = "comp-1",
            RecruitmentMode = "Standard",
            AutoAdvanceOnScreening = true,
            ScreeningPassScore = 65, // Company wide threshold
            ScreeningCriteria = ["Standard Criteria"],
        };
        _configsMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(config);

        var vacancy = new JobVacancy
        {
            CompanyId = "comp-1",
            JobId = "vac-201",
            WorkflowMode = "Inherit",
            ScreeningPassScore = null, // Inherits company policy
            ScreeningCriteria = ["Standard Criteria"],
        };
        _vacanciesMock.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(vacancy);

        var service = new RecruitmentScreeningService(
            _applicantsMock.Object,
            _reviewsMock.Object,
            _logsMock.Object,
            TenantContext("comp-1"),
            _configsMock.Object,
            _employeesMock.Object,
            vacancies: _vacanciesMock.Object);

        // Submitting 60 (< 65) fails
        var failResult = await service.Upsert(new RecruitmentScreeningReviewCommand
        {
            ApplicantId = "app-101",
            VacancyId = "vac-201",
            Decision = "Pass",
            Score = 60,
            Criteria = [new() { Name = "Standard Criteria", Passed = true }],
            IdempotencyKey = "test-key-fail-company"
        }, "hr-user-1");
        Assert.False(failResult.Success);
        Assert.Contains("at least 65", failResult.Message);

        // Submitting 65 (>= 65) succeeds
        var passResult = await service.Upsert(new RecruitmentScreeningReviewCommand
        {
            ApplicantId = "app-101",
            VacancyId = "vac-201",
            Decision = "Pass",
            Score = 65,
            Criteria = [new() { Name = "Standard Criteria", Passed = true }],
            IdempotencyKey = "test-key-pass-company"
        }, "hr-user-1");
        Assert.True(passResult.Success, $"Expected pass with score 65 to succeed, but got: {passResult.Message}");
    }
}

