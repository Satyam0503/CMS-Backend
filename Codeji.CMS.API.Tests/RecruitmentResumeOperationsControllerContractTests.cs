using System.Reflection;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Codeji.CMS.API.Tests;

public sealed class RecruitmentResumeOperationsControllerContractTests
{
    [Fact]
    public void Scan_health_route_requires_authentication_and_application_view_permission()
    {
        var controller = typeof(RecruitmentResumeOperationsController);
        var action = controller.GetMethod(nameof(RecruitmentResumeOperationsController.GetScanHealth));

        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("api/RecruitmentResumeOperations", controller.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.NotNull(action?.GetCustomAttribute<HttpGetAttribute>());
        var permission = action?.GetCustomAttribute<ModulePermissionAttribute>();
        Assert.NotNull(permission);
        Assert.Equal(AppModule.Applications, permission.Module);
        Assert.Contains(Permission.View, permission.Permissions);
    }
}

