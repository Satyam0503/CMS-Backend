using System.Reflection;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Codeji.CMS.API.Tests;

public sealed class WorkFromHomeControllerSecurityTests
{
    [Fact]
    public void CreateRequest_RequiresOwnRequestPermission()
    {
        var method = typeof(WorkFromHomeController).GetMethod(nameof(WorkFromHomeController.Create));
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();

        Assert.NotNull(permission);
        Assert.Equal(AppModule.WorkFromHome, permission.Module);
        Assert.Equal(new[] { Permission.CreateOwn }, permission.Permissions);
    }

    [Fact]
    public void CreateRequest_RequiresIdempotencyKeyHeader()
    {
        var method = typeof(WorkFromHomeController).GetMethod(nameof(WorkFromHomeController.Create));
        var parameter = Assert.Single(method!.GetParameters(), x => x.Name == "idempotencyKey");
        var header = parameter.GetCustomAttribute<FromHeaderAttribute>();

        Assert.NotNull(header);
        Assert.Equal("Idempotency-Key", header.Name);
        Assert.False(parameter.IsOptional);
    }

    [Theory]
    [InlineData(nameof(WorkFromHomeController.DeadLetterOutbox), "GET")]
    [InlineData(nameof(WorkFromHomeController.ReplayDeadLetterOutbox), "POST")]
    public void DeadLetterOperations_RequireWfhPolicyEdit(string methodName, string httpMethod)
    {
        var method = typeof(WorkFromHomeController).GetMethod(methodName);
        var permission = method?.GetCustomAttribute<ModulePermissionAttribute>();

        Assert.NotNull(permission);
        Assert.Equal(AppModule.WorkFromHome, permission.Module);
        Assert.Equal(new[] { Permission.PolicyEdit }, permission.Permissions);
        var expectedRouteAttribute = httpMethod == "GET" ? nameof(HttpGetAttribute) : nameof(HttpPostAttribute);
        Assert.Contains(method!.GetCustomAttributes(inherit: true), attribute => attribute.GetType().Name == expectedRouteAttribute);
    }
}
