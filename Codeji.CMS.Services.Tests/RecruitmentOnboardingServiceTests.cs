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

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentOnboardingServiceTests
{
    [Fact]
    public async Task Completion_requires_all_tasks_to_be_completed_or_skipped()
    {
        var repository = new Mock<IMongoDbRepository<RecruitmentOnboardingCase>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOnboardingCase, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOnboardingCase { CaseId = "case-1", CompanyId = "company-1", ApplicantId = "app-1", Version = 1, Tasks = [new RecruitmentOnboardingTask { Code = "account-activation", Title = "Activate", Status = "Completed" }, new RecruitmentOnboardingTask { Code = "access", Title = "Access", Status = "Pending" }] });
        var service = new RecruitmentOnboardingService(repository.Object, TenantContext("company-1"));

        var result = await service.Update(new RecruitmentOnboardingUpdateCommand { CaseId = "case-1", Status = RecruitmentOnboardingStatus.Completed, IdempotencyKey = "complete-1", Tasks = [new RecruitmentOnboardingTaskUpdate { Code = "account-activation", Status = "Completed" }, new RecruitmentOnboardingTaskUpdate { Code = "access", Status = "Pending" }] }, "actor-1");

        Assert.False(result.Success);
        Assert.Equal(RecruitmentErrorCodes.InvalidTransition, result.ErrorCode);
        repository.Verify(x => x.Update(It.IsAny<FilterDefinition<RecruitmentOnboardingCase>>(), It.IsAny<RecruitmentOnboardingCase>()), Times.Never);
    }

    [Fact]
    public async Task Completion_activates_linked_employee_lifecycle()
    {
        var repository = new Mock<IMongoDbRepository<RecruitmentOnboardingCase>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOnboardingCase, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOnboardingCase { CaseId = "case-1", CompanyId = "company-1", ApplicantId = "app-1", EmployeeUserId = "emp-1", Version = 1, Tasks = [new RecruitmentOnboardingTask { Code = "account-activation", Title = "Activate", Status = "Pending" }, new RecruitmentOnboardingTask { Code = "access", Title = "Access", Status = "Pending" }] });
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentOnboardingCase>>(), It.IsAny<RecruitmentOnboardingCase>())).ReturnsAsync(new Result { Success = true });
        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        EmpUser? captured = null;
        employees.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new EmpUser { UserId = "emp-1", CompanyId = "company-1", FirstName = "A", LastName = "B", Email = "a@example.com", Lifecycle = new EmployeeLifecycleState { EmploymentStatus = EmployeeLifecycleStatuses.PreJoining, AvailabilityStatus = EmployeeLifecycleStatuses.NotApplicable, AccountStatus = EmployeeLifecycleStatuses.AccountLocked, PayrollStatus = EmployeeLifecycleStatuses.PayrollHold, Version = 2 } });
        employees.Setup(x => x.Update(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<EmpUser>())).Callback<FilterDefinition<EmpUser>, EmpUser>((_, item) => captured = item).ReturnsAsync(new Result { Success = true });
        var service = new RecruitmentOnboardingService(repository.Object, TenantContext("company-1"), employees.Object);

        var result = await service.Update(new RecruitmentOnboardingUpdateCommand { CaseId = "case-1", Status = RecruitmentOnboardingStatus.Completed, IdempotencyKey = "complete-1", Tasks = [new RecruitmentOnboardingTaskUpdate { Code = "account-activation", Status = "Completed" }, new RecruitmentOnboardingTaskUpdate { Code = "access", Status = "Completed" }] }, "actor-1");

        Assert.True(result.Success);
        Assert.Contains("activated", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(captured);
        Assert.Equal(EmployeeLifecycleStatuses.Active, captured!.Lifecycle.EmploymentStatus);
        Assert.Equal(EmployeeLifecycleStatuses.Working, captured.Lifecycle.AvailabilityStatus);
        Assert.Equal(EmployeeLifecycleStatuses.AccountActive, captured.Lifecycle.AccountStatus);
        Assert.Equal(EmployeeLifecycleStatuses.PayrollActive, captured.Lifecycle.PayrollStatus);
        Assert.Contains(captured.Lifecycle.History, x => x.ReasonCode == "RECRUITMENT_ONBOARDING_COMPLETED");
    }

    [Fact]
    public async Task Completion_does_not_activate_employee_from_another_tenant()
    {
        var repository = new Mock<IMongoDbRepository<RecruitmentOnboardingCase>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentOnboardingCase, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new RecruitmentOnboardingCase { CaseId = "case-1", CompanyId = "company-1", ApplicantId = "app-1", EmployeeUserId = "emp-1", Version = 1, Tasks = [new RecruitmentOnboardingTask { Code = "access", Title = "Access", Status = "Pending" }] });
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<RecruitmentOnboardingCase>>(), It.IsAny<RecruitmentOnboardingCase>())).ReturnsAsync(new Result { Success = true });
        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        employees.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((EmpUser?)null);
        var service = new RecruitmentOnboardingService(repository.Object, TenantContext("company-1"), employees.Object);

        var result = await service.Update(new RecruitmentOnboardingUpdateCommand { CaseId = "case-1", Status = RecruitmentOnboardingStatus.Completed, IdempotencyKey = "complete-2", Tasks = [new RecruitmentOnboardingTaskUpdate { Code = "access", Status = "Completed" }] }, "actor-1");

        Assert.True(result.Success);
        Assert.Contains("pending", result.Message, StringComparison.OrdinalIgnoreCase);
        employees.Verify(x => x.Update(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<EmpUser>()), Times.Never);
    }

    [Fact]
    public async Task Queue_reports_progress_and_first_blocker_for_current_tenant()
    {
        var repository = new Mock<IMongoDbRepository<RecruitmentOnboardingCase>>();
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<RecruitmentOnboardingCase, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[]
            {
                new RecruitmentOnboardingCase { CaseId = "case-1", CompanyId = "company-1", ApplicantId = "app-1", Status = RecruitmentOnboardingStatus.InProgress, Tasks = [new RecruitmentOnboardingTask { Code = "docs", Title = "Documents", Status = "Completed" }, new RecruitmentOnboardingTask { Code = "payroll", Title = "Payroll", Status = "Pending" }] }
            });
        var service = new RecruitmentOnboardingService(repository.Object, TenantContext("company-1"));

        var result = await service.GetQueue(RecruitmentOnboardingStatus.InProgress, 1, 25);

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalRecords);
        var item = Assert.Single(result.MethodResults!);
        Assert.Equal("case-1", item.CaseId);
        Assert.Equal(50, item.CompletionPercent);
        Assert.Equal("payroll", item.FirstBlockingTaskCode);
    }

    private static IHttpContextAccessor TenantContext(string companyId)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", companyId)], "TestAuth")) };
        return new HttpContextAccessor { HttpContext = context };
    }
}
