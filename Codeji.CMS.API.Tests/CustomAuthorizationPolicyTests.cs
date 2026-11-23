using System.Security.Claims;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Services.Employees.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.API.Tests;

public sealed class CustomAuthorizationPolicyTests
{
    [Fact]
    public async Task AdminOnlyPolicy_RequiresAuthentication()
    {
        using var factory = new TestWebAppFactory();
        var policyProvider = factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        var adminPolicy = await policyProvider.GetPolicyAsync("AdminOnly");
        var hrExitOverridePolicy = await policyProvider.GetPolicyAsync("HrExitOverride");

        Assert.Contains(adminPolicy!.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
        Assert.Contains(hrExitOverridePolicy!.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task RoleHandlers_DoNotReadTenantRolesForAnonymousPrincipals()
    {
        var roles = new Mock<IRoleService>(MockBehavior.Strict);
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection()
                    .AddSingleton(roles.Object)
                    .BuildServiceProvider()
            }
        };
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var roleContext = new AuthorizationHandlerContext(
            [new RoleRequirement(Roles.Administrator)], anonymous, accessor.HttpContext);
        await new RoleHandler(accessor).HandleAsync(roleContext);

        var anyRoleContext = new AuthorizationHandlerContext(
            [new AnyRoleRequirement(Roles.Administrator, Roles.HR)], anonymous, accessor.HttpContext);
        await new AnyRoleHandler(accessor).HandleAsync(anyRoleContext);

        roles.VerifyNoOtherCalls();
        Assert.False(roleContext.HasSucceeded);
        Assert.False(anyRoleContext.HasSucceeded);
    }
}
