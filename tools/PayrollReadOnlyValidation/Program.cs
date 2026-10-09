using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PayrollReadOnlyValidation;

// Separate local test harness; never changes normal application startup behavior.
await using var factory = new ReadOnlyPayrollFactory();
factory.UseKestrel(options => options.ListenLocalhost(5036));
factory.StartServer();
Console.WriteLine($"READ_ONLY_PAYROLL_VALIDATION_READY pid={Environment.ProcessId} port=5036; company jobs disabled; sign-in only write exception.");
var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Console.CancelKeyPress += (_, args) => { args.Cancel = true; completion.TrySetResult(); };
await completion.Task;

sealed class ReadOnlyPayrollFactory : WebApplicationFactory<PayrollConfigurationController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Codeji.CMS.API")));
        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                         descriptor.ImplementationType?.Name != "GenericWebHostService").ToList())
                services.Remove(descriptor);
            services.AddTransient<IStartupFilter, ReadOnlyGuard>();
        });
    }
}

sealed class ReadOnlyGuard : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, continuation) =>
        {
            // Never forward preflights into application endpoints.
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                var origin = context.Request.Headers.Origin.ToString();
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback)
                {
                    context.Response.Headers.AccessControlAllowOrigin = origin;
                    context.Response.Headers.AccessControlAllowCredentials = "true";
                    context.Response.Headers.AccessControlAllowHeaders = "authorization,content-type,appkey";
                    context.Response.Headers.AccessControlAllowMethods = "GET,POST,OPTIONS";
                }
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }
            if (!ReadOnlyRequestPolicy.IsAllowed(context.Request.Method, context.Request.Path.Value ?? ""))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { success = false, statusCode = 403,
                    errorCode = "READ_ONLY_VALIDATION_BLOCKED", message = "This local validation host blocks all unreviewed routes and payroll changes." });
                return;
            }
            await continuation();
        });
        next(app);
    };
}
