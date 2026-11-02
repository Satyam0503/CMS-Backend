using System.Reflection;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Codeji.CMS.API.Tests;

public sealed class TaxDeclarationControllerContractTests
{
    [Fact]
    public void Controller_has_api_route_and_authorize_attribute()
    {
        var type = typeof(TaxDeclarationController);
        var auth = type.GetCustomAttribute<AuthorizeAttribute>();
        var route = type.GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(auth);
        Assert.NotNull(route);
        Assert.Equal("api/payroll/tax-declarations", route!.Template);
    }

    [Theory]
    [InlineData(nameof(TaxDeclarationController.GetDeclaration), Permission.View)]
    [InlineData(nameof(TaxDeclarationController.GetCompanyDeclarations), Permission.View)]
    [InlineData(nameof(TaxDeclarationController.CalculateTaxProjection), Permission.View)]
    [InlineData(nameof(TaxDeclarationController.SubmitDeclaration), Permission.Edit)]
    [InlineData(nameof(TaxDeclarationController.ReviewDeclaration), Permission.Approve)]
    public void Endpoints_enforce_correct_payroll_permissions(string methodName, string expectedPermission)
    {
        var method = typeof(TaxDeclarationController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        var perm = method?.GetCustomAttribute<ModulePermissionAttribute>();

        Assert.NotNull(perm);
        Assert.Equal(AppModule.PayRoll, perm!.Module);
        Assert.Contains(expectedPermission, perm.Permissions);
    }
}
