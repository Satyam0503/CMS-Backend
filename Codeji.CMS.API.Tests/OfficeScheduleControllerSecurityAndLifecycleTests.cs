using System.Reflection;
using System.Security.Claims;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public sealed class OfficeScheduleControllerSecurityAndLifecycleTests
{
    private const string CurrentCompanyId = "company-tenant-456";
    private const string CurrentUserId = "admin-user-1";

    private static (CompanyMasterController Controller,
        Mock<ICompanyMasterService> CompanyService,
        Mock<IOfficeScheduleSettingsService> ScheduleSettings,
        Mock<IOfficeScheduleAssignmentService> ScheduleAssignments) CreateFixture()
    {
        var companyService = new Mock<ICompanyMasterService>();
        var scheduleSettings = new Mock<IOfficeScheduleSettingsService>();
        var scheduleAssignments = new Mock<IOfficeScheduleAssignmentService>();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("company_id", CurrentCompanyId),
            new Claim("user_id", CurrentUserId),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "TestAuth"));

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var controller = new CompanyMasterController(
            companyService.Object,
            httpContextAccessor.Object,
            scheduleSettings.Object,
            scheduleAssignments.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return (controller, companyService, scheduleSettings, scheduleAssignments);
    }

    [Fact]
    public void CompanyMasterController_RequiresAuthentication()
    {
        var authorizeAttribute = typeof(CompanyMasterController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttribute);
    }

    [Theory]
    [InlineData("GetOfficeSchedule")]
    [InlineData("GetOfficeSchedules")]
    [InlineData("SaveOfficeSchedule")]
    [InlineData("DeleteOfficeSchedule")]
    [InlineData("GetDepartmentOfficeScheduleAssignments")]
    [InlineData("SetDepartmentOfficeScheduleAssignment")]
    public void OfficeScheduleEndpoints_RequireAdminOnlyPolicy(string methodName)
    {
        var method = typeof(CompanyMasterController).GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttribute = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("AdminOnly", authorizeAttribute.Policy);
    }

    [Fact]
    public async Task GetOfficeSchedule_RoutesToTenantCompanyId()
    {
        var (controller, _, scheduleSettings, _) = CreateFixture();
        scheduleSettings
            .Setup(s => s.GetAsync(CurrentCompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<OfficeScheduleSettingsDto>
            {
                Success = true,
                MethodResult = new OfficeScheduleSettingsDto { Name = "Default shift", IsDefault = true }
            });

        var result = await controller.GetOfficeSchedule(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Default shift", result.MethodResult?.Name);
        scheduleSettings.Verify(s => s.GetAsync(CurrentCompanyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllOfficeSchedules_ReturnsAllActiveSchedulesForTenant()
    {
        var (controller, _, scheduleSettings, _) = CreateFixture();
        scheduleSettings
            .Setup(s => s.GetAllAsync(CurrentCompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<OfficeScheduleSettingsDto>
            {
                Success = true,
                MethodResults = new List<OfficeScheduleSettingsDto>
                {
                    new() { ScheduleId = "shift-def", Name = "Default shift", IsDefault = true },
                    new() { ScheduleId = "shift-cust", Name = "Evening Shift", IsDefault = false }
                }
            });

        var result = await controller.GetOfficeSchedules(CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResults);
        Assert.Equal(2, result.MethodResults.Count);
        scheduleSettings.Verify(s => s.GetAllAsync(CurrentCompanyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveOfficeSchedule_PassesTenantAndActorContext()
    {
        var (controller, _, scheduleSettings, _) = CreateFixture();
        var request = new OfficeScheduleSettingsDto
        {
            Name = "Shift 1",
            StandardCheckIn = "10:00",
            StandardCheckOut = "19:00"
        };

        scheduleSettings
            .Setup(s => s.SaveAsync(CurrentCompanyId, CurrentUserId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<OfficeScheduleSettingsDto>
            {
                Success = true,
                MethodResult = request
            });

        var result = await controller.SaveOfficeSchedule(request, CancellationToken.None);

        Assert.True(result.Success);
        scheduleSettings.Verify(s => s.SaveAsync(CurrentCompanyId, CurrentUserId, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteOfficeSchedule_PassesTenantAndActorContext()
    {
        var (controller, _, scheduleSettings, _) = CreateFixture();

        scheduleSettings
            .Setup(s => s.DeleteAsync(CurrentCompanyId, CurrentUserId, "custom-shift-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result { Success = true, Message = "Office schedule deleted." });

        var result = await controller.DeleteOfficeSchedule("custom-shift-id", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Office schedule deleted.", result.Message);
        scheduleSettings.Verify(s => s.DeleteAsync(CurrentCompanyId, CurrentUserId, "custom-shift-id", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteOfficeSchedule_WhenShiftIsDefault_ReturnsFailurePreventingDeletion()
    {
        var (controller, _, scheduleSettings, _) = CreateFixture();

        scheduleSettings
            .Setup(s => s.DeleteAsync(CurrentCompanyId, CurrentUserId, "default-shift-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result { Success = false, Message = "OFFICE_SCHEDULE_DEFAULT_CANNOT_BE_DELETED" });

        var result = await controller.DeleteOfficeSchedule("default-shift-id", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("OFFICE_SCHEDULE_DEFAULT_CANNOT_BE_DELETED", result.Message);
    }
}
