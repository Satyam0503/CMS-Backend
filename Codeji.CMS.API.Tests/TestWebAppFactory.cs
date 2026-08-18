using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Codeji.CMS.API.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Override services for testing if needed (e.g. swap MongoDB for in-memory)
            // services.RemoveAll<IMongoDbRepository<T>>();
            // services.AddScoped<IMongoDbRepository<T>, FakeMongoRepository<T>>();
        });
    }
}
