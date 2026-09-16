using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeLifecyclePolicyServiceTests
{
    [Fact]
    public async Task Validation_uses_tenant_policy_for_required_fields()
    {
        var repository = new Mock<IMongoDbRepository<EmployeeLifecyclePolicy>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLifecyclePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new EmployeeLifecyclePolicy { CompanyId = "company-1", EventType = "Sabbatical", RequiresExpectedReturnDate = true, RequiresReason = true });
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "TestAuth"));
        var service = new EmployeeLifecyclePolicyService(repository.Object, accessor);

        Assert.Equal("LIFECYCLE_RETURN_DATE_REQUIRED", await service.ValidateAvailabilityAsync("Sabbatical", null, "study"));
        Assert.Equal("LIFECYCLE_REASON_REQUIRED", await service.ValidateAvailabilityAsync("Sabbatical", DateTime.UtcNow.Date.AddDays(1), null));
    }

    [Fact]
    public async Task Validation_is_optional_when_no_active_tenant_policy_exists()
    {
        var repository = new Mock<IMongoDbRepository<EmployeeLifecyclePolicy>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLifecyclePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((EmployeeLifecyclePolicy?)null);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "TestAuth"));

        var service = new EmployeeLifecyclePolicyService(repository.Object, accessor);

        Assert.Null(await service.ValidateAvailabilityAsync("Working", null, null));
    }

    [Fact]
    public async Task Save_rejects_unsupported_type_or_missing_leave_policy_link()
    {
        var repository = new Mock<IMongoDbRepository<EmployeeLifecyclePolicy>>();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "TestAuth"));
        var service = new EmployeeLifecyclePolicyService(repository.Object, accessor);

        var unsupported = await service.SavePolicyAsync(new EmployeeLifecyclePolicy { EventType = "AnnualLeave", LeavePolicyId = "annual" });
        var unlinked = await service.SavePolicyAsync(new EmployeeLifecyclePolicy { EventType = "MedicalLeave" });

        Assert.False(unsupported.Success);
        Assert.Equal("LIFECYCLE_POLICY_INVALID", unsupported.Error);
        Assert.False(unlinked.Success);
        Assert.Equal("LIFECYCLE_POLICY_INVALID", unlinked.Error);
        repository.Verify(x => x.AddOne(It.IsAny<EmployeeLifecyclePolicy>()), Times.Never);
    }

    [Fact]
    public async Task Validation_rejects_past_expected_return_date()
    {
        var repository = new Mock<IMongoDbRepository<EmployeeLifecyclePolicy>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLifecyclePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new EmployeeLifecyclePolicy { CompanyId = "company-1", EventType = "LongTermLeave", RequiresExpectedReturnDate = false });
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "TestAuth"));
        var service = new EmployeeLifecyclePolicyService(repository.Object, accessor);

        Assert.Equal("LIFECYCLE_RETURN_DATE_INVALID", await service.ValidateAvailabilityAsync("LongTermLeave", DateTime.UtcNow.Date.AddDays(-1), "treatment"));
    }

    [Fact]
    public async Task Save_preserves_existing_effective_date_when_legacy_payload_omits_it()
    {
        var existingDate = DateTime.UtcNow.Date.AddDays(10);
        var existing = new EmployeeLifecyclePolicy { Id = "policy-1", CompanyId = "company-1", EventType = "MedicalLeave", LeavePolicyId = "medical", EffectiveFromUtc = existingDate };
        var repository = new Mock<IMongoDbRepository<EmployeeLifecyclePolicy>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLifecyclePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(existing);
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeeLifecyclePolicy>>(), It.IsAny<EmployeeLifecyclePolicy>())).ReturnsAsync(new Result { Success = true });
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1"), new Claim("sub", "hr-1")], "TestAuth"));
        var service = new EmployeeLifecyclePolicyService(repository.Object, accessor);

        var result = await service.SavePolicyAsync(new EmployeeLifecyclePolicy { EventType = "MedicalLeave", LeavePolicyId = "medical" });

        Assert.True(result.Success);
        Assert.Equal(existingDate, result.Policy?.EffectiveFromUtc);
    }

    [Fact]
    public async Task Save_accepts_today_and_uses_the_previous_version_for_concurrency()
    {
        var existing = new EmployeeLifecyclePolicy { Id = "policy-1", CompanyId = "company-1", EventType = "MedicalLeave", LeavePolicyId = "medical", EffectiveFromUtc = DateTime.UtcNow.Date, Version = 3 };
        var repository = new Mock<IMongoDbRepository<EmployeeLifecyclePolicy>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLifecyclePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(existing);
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeeLifecyclePolicy>>(), It.Is<EmployeeLifecyclePolicy>(p => p.Version == 4))).ReturnsAsync(new Result { Success = true });
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1"), new Claim("sub", "hr-1")], "TestAuth"));
        var service = new EmployeeLifecyclePolicyService(repository.Object, accessor);

        var result = await service.SavePolicyAsync(new EmployeeLifecyclePolicy { EventType = "MedicalLeave", LeavePolicyId = "medical", EffectiveFromUtc = DateTime.UtcNow.Date });

        Assert.True(result.Success);
        Assert.Equal(4, result.Policy?.Version);
        repository.Verify(x => x.Update(It.IsAny<FilterDefinition<EmployeeLifecyclePolicy>>(), It.IsAny<EmployeeLifecyclePolicy>()), Times.Once);
    }

    [Fact]
    public async Task Save_does_not_remove_entitlements_when_version_update_fails()
    {
        var existing = new EmployeeLifecyclePolicy
        {
            Id = "policy-1", CompanyId = "company-1", EventType = "MedicalLeave", LeavePolicyId = "medical",
            EffectiveFromUtc = DateTime.UtcNow.Date, Version = 4
        };
        var repository = new Mock<IMongoDbRepository<EmployeeLifecyclePolicy>>();
        var leavePolicies = new Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.Leave.LeavePolicy>>();
        var leaveBalances = new Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.Leave.EmployeeLeaveBalance>>();
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLifecyclePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(existing);
        leavePolicies.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Codeji.CMS.Repository.Entities.Leave.LeavePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new Codeji.CMS.Repository.Entities.Leave.LeavePolicy { CompanyId = "company-1", Id = "medical", Name = "Medical Leave", Code = "MED", Status = true });
        repository.Setup(x => x.Update(It.IsAny<FilterDefinition<EmployeeLifecyclePolicy>>(), It.IsAny<EmployeeLifecyclePolicy>()))
            .ReturnsAsync(new Result { Success = false });
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("company_id", "company-1")], "TestAuth"));
        var service = new EmployeeLifecyclePolicyService(repository.Object, accessor, leavePolicies.Object, leaveBalances.Object);

        var result = await service.SavePolicyAsync(new EmployeeLifecyclePolicy
        {
            Version = 4, EventType = "MedicalLeave", LeavePolicyId = "medical", EffectiveFromUtc = DateTime.UtcNow.Date
        });

        Assert.False(result.Success);
        Assert.Equal("LIFECYCLE_POLICY_VERSION_CONFLICT", result.Error);
        leaveBalances.Verify(x => x.Delete(It.IsAny<FilterDefinition<Codeji.CMS.Repository.Entities.Leave.EmployeeLeaveBalance>>()), Times.Never);
        leaveBalances.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<Codeji.CMS.Repository.Entities.Leave.EmployeeLeaveBalance>>(), It.IsAny<UpdateDefinition<Codeji.CMS.Repository.Entities.Leave.EmployeeLeaveBalance>>()), Times.Never);
    }
}
