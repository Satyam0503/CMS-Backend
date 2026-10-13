using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Codeji.CMS.API.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReverseProxy:ForwardLimit"] = "1",
                ["ReverseProxy:TrustedProxies:0"] = "127.0.0.1"
            });
        });

        builder.ConfigureServices(services =>
        {
            // API tests must not start production schedulers/outbox workers. They can keep the
            // in-memory host alive and make test results nondeterministic.
            services.RemoveAll<IHostedService>();
            // Override services for testing if needed (e.g. swap MongoDB for in-memory)
            // services.RemoveAll<IMongoDbRepository<T>>();
            // services.AddScoped<IMongoDbRepository<T>, FakeMongoRepository<T>>();
        });
    }
}
