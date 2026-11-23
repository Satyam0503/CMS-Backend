using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codeji.CMS.API.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Exercise the same safe error pipeline used in production. Development enables
        // DeveloperExceptionPage, which rethrows escaped failures into a host-only page.
        builder.UseEnvironment("Production");
        // TestServer has no HTTPS endpoint and the Windows Event Log provider is not
        // available to restricted test processes. Keep framework diagnostics from
        // turning the expected HTTP warning into a failed request.
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReverseProxy:ForwardLimit"] = "1",
                ["ReverseProxy:TrustedProxies:0"] = "127.0.0.1",
                // Keep the exception behavior safe even if another setting enables debug output.
                ["AppSettings:isForDebug"] = "false"
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
