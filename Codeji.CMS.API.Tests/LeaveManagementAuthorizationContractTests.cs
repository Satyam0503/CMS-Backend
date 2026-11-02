using System.Reflection;
using System.Security.Claims;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Codeji.CMS.API.Tests;

public sealed class LeaveManagementAuthorizationContractTests
{
    [Fact]
    public async Task SelfServiceCreate_UsesAuthenticatedUserInsteadOfBodyUserId()
    {
        var service = new Mock<ILeaveManagementService>();
        service.Setup(x => x.CreateLeaveRequest(It.IsAny<LeaveRequestDto>()))
            .ReturnsAsync(new Result { Success = true });
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("user_id", "employee-1"),
            new Claim("company_id", "company-1"),
        ], "test"));
        var controller = new LeaveManagementController(service.Object, new HttpContextAccessor { HttpContext = context });
        var payload = new LeaveRequestDto
        {
            UserId = "another-employee",
            LeavePolicyId = "policy-1",
            StartDate = DateTime.UtcNow.Date.AddDays(5),
            EndDate = DateTime.UtcNow.Date.AddDays(5),
            Reason = "Personal leave",
        };

        var result = await controller.CreateMyLeaveRequest(payload);

        Assert.True(result.Success);
        service.Verify(x => x.CreateLeaveRequest(It.Is<LeaveRequestDto>(request => request.UserId == "employee-1")), Times.Once);
    }

    [Fact]
    public void EmployeeSelfServiceAndReviewerEndpoints_DeclareExpectedPermissions()
    {
        var controller = typeof(LeaveManagementController);
        Assert.NotNull(controller.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>());

        var selfCreate = GetPermission(nameof(LeaveManagementController.CreateMyLeaveRequest));
        Assert.Null(selfCreate);

        var employeeTargetCreate = GetPermission(nameof(LeaveManagementController.CreateEmployeeLeaveRequest));
        Assert.Equal(AppModule.LeaveManagement, employeeTargetCreate?.Module);
        Assert.Equal(new[] { Permission.Edit }, employeeTargetCreate?.Permissions);

        var approval = GetPermission(nameof(LeaveManagementController.UpdateLeaveRequestStatus));
        Assert.Equal(AppModule.LeaveManagement, approval?.Module);
        Assert.Equal(new[] { Permission.Edit }, approval?.Permissions);
    }

    [Fact]
    public async Task Approval_ReturnsServiceUnavailableWhenTransactionsAreUnsupported()
    {
        var service = new Mock<ILeaveManagementService>();
        service.Setup(x => x.UpdateLeaveRequestStatus("leave-1", It.IsAny<LeaveRequestUpdateDto>()))
            .ReturnsAsync(new Result
            {
                Success = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorCode = "LEAVE_WFH_TRANSACTION_REQUIRED",
                Message = "MongoDB transaction support is not configured."
            });
        var controller = new LeaveManagementController(service.Object, new HttpContextAccessor());

        var result = await controller.UpdateLeaveRequestStatus("leave-1", new LeaveRequestUpdateDto());

        var unavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        Assert.Equal("LEAVE_WFH_TRANSACTION_REQUIRED", Assert.IsType<Result>(unavailable.Value).ErrorCode);
    }

    private static ModulePermissionAttribute? GetPermission(string actionName) =>
        typeof(LeaveManagementController)
            .GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public)
            ?.GetCustomAttribute<ModulePermissionAttribute>();
}
