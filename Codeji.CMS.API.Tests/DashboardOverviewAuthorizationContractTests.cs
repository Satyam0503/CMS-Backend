using System.Reflection;
using System.Security.Claims;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Codeji.CMS.API.Tests;

public sealed class DashboardOverviewAuthorizationContractTests
{
    [Fact]
    public void Overview_endpoint_requires_authenticated_dashboard_view_all_permission()
    {
        var controller = typeof(DashboardController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());

        var route = controller.GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/[controller]", route?.Template);

        var action = controller.GetMethod(nameof(DashboardController.GetDashboardOverview), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        Assert.Single(action!.GetCustomAttributes<HttpGetAttribute>());
        Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());

        var permission = Assert.Single(action.GetCustomAttributes<ModulePermissionAttribute>());
        Assert.Equal(AppModule.Dashboard, permission.Module);
        Assert.Contains(Permission.ViewAll, permission.Permissions);
    }

    [Fact]
    public async Task Tenant_middleware_rejects_duplicate_authenticated_company_claims()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/Dashboard/GetDashboardOverview";
        context.Request.Headers["cId"] = "company-gamma";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("user_id", "user-alpha"),
            new Claim("company_id", "company-alpha"),
            new Claim("company_id", "company-beta"),
            new Claim("role_id", "role-alpha"),
        ], "AnalyticsSecurityRegression"));
        context.RequestServices = new ServiceCollection().BuildServiceProvider();

        var result = await CompanyIdMiddleware.AuthenticateUserRequest(new HttpContextAccessor { HttpContext = context });

        Assert.Null(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }
}
