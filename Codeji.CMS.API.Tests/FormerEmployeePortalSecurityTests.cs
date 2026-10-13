using System.Reflection;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Repository.Entities.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public sealed class FormerEmployeePortalSecurityTests
{
    [Fact]
    public void PublicPortal_IsExplicitlyAnonymous_AndScopedToPublicRoute()
    {
        var attributes = typeof(PublicFormerEmployeePortalController).GetCustomAttributes(true);

        Assert.Contains(attributes, attribute => attribute is AllowAnonymousAttribute);
        var route = Assert.IsType<RouteAttribute>(attributes.Single(attribute => attribute is RouteAttribute));
        Assert.Equal("api/public/former-employee", route.Template);
    }

    [Theory]
    [InlineData("Redeem")]
    [InlineData("CreateRequest")]
    public void SensitivePublicPortalActions_AreRateLimited(string methodName)
    {
        var method = typeof(PublicFormerEmployeePortalController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(true), attribute => attribute is EnableRateLimitingAttribute);
    }

    [Fact]
    public void InvitationPersistence_StoresOnlyHashes_NotRawTokens()
    {
        var propertyNames = typeof(FormerEmployeeAccessToken).GetProperties().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("TokenHash", propertyNames);
        Assert.DoesNotContain("Token", propertyNames);
        Assert.Contains("SessionTokenHash", propertyNames);
    }
}
