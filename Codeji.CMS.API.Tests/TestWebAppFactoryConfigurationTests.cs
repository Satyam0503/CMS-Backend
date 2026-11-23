using Codeji.CMS.Utility.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Codeji.CMS.API.Tests;

public sealed class TestWebAppFactoryConfigurationTests
{
    [Fact]
    public void Factory_uses_production_safe_exception_handling_configuration()
    {
        using var factory = new TestWebAppFactory();
        using var client = factory.CreateClient();

        Assert.Equal("false", factory.Services.GetRequiredService<IConfiguration>()["AppSettings:isForDebug"]);
    }
}
