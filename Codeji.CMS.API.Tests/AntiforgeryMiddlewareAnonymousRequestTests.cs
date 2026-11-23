using System.Security.Claims;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Codeji.CMS.API.Tests;

public sealed class AntiforgeryMiddlewareAnonymousRequestTests
{
    [Fact]
    public async Task AnonymousProtectedGet_ContinuesToAuthorizationWithoutTenantDatabaseValidation()
    {
        var previousSettings = ConfigManager.AppSettings;
        ConfigManager.AppSettings = new AppSettings { IsForDebug = true };
        try
        {
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()),
                RequestServices = new ServiceCollection().BuildServiceProvider()
            };
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/api/secure-resource";

            var nextCalled = false;
            var middleware = new AntiforgeryMiddleware(
                Mock.Of<IAntiforgery>(),
                new HttpContextAccessor { HttpContext = context });

            await middleware.InvokeAsync(context, _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Empty(context.Items);
        }
        finally
        {
            ConfigManager.AppSettings = previousSettings;
        }
    }
}
