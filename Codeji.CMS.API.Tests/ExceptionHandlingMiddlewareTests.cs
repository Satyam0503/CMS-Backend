using System.Text;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Codeji.CMS.API.Tests;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task UnexpectedException_ReturnsGeneric500AndCorrelationHeaderWithoutExposingDetails()
    {
        var originalSettings = ConfigManager.AppSettings;
        ConfigManager.AppSettings = new AppSettings { IsForDebug = false };
        try
        {
            var context = new DefaultHttpContext();
            context.TraceIdentifier = "request-correlation-123";
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/api/Dashboard/GetDashboardOverview";
            context.Response.Body = new MemoryStream();
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("secret database host and credentials"),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.Invoke(context);

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal("request-correlation-123", context.Response.Headers["X-Correlation-ID"].ToString());
            Assert.Contains("An unexpected error occurred.", body);
            Assert.DoesNotContain("secret database host", body);
        }
        finally
        {
            ConfigManager.AppSettings = originalSettings;
        }
    }

    [Fact]
    public async Task TimeoutException_ReturnsGatewayTimeoutAndCorrelationHeader()
    {
        var originalSettings = ConfigManager.AppSettings;
        ConfigManager.AppSettings = new AppSettings { IsForDebug = false };
        try
        {
            var context = new DefaultHttpContext();
            context.TraceIdentifier = "request-timeout-456";
            context.Response.Body = new MemoryStream();
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new TimeoutException("source query timed out"),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.Invoke(context);

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            Assert.Equal(StatusCodes.Status504GatewayTimeout, context.Response.StatusCode);
            Assert.Equal("request-timeout-456", context.Response.Headers["X-Correlation-ID"].ToString());
            Assert.Contains("Request timed out", body);
        }
        finally
        {
            ConfigManager.AppSettings = originalSettings;
        }
    }

    [Fact]
    public async Task RequestCancellation_DoesNotBecomeAnInternalServerErrorResponse()
    {
        using var requestCancellation = new CancellationTokenSource();
        requestCancellation.Cancel();
        var context = new DefaultHttpContext { RequestAborted = requestCancellation.Token };
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(requestCancellation.Token),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
        Assert.False(context.Response.Headers.ContainsKey("X-Correlation-ID"));
    }

    [Fact]
    public async Task HttpConfigurationOverridesStaleStaticDebugSetting()
    {
        var originalSettings = ConfigManager.AppSettings;
        ConfigManager.AppSettings = new AppSettings { IsForDebug = true };
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AppSettings:isForDebug"] = "false" })
                .Build();
            var context = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider()
            };
            context.Response.Body = new MemoryStream();
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("handled by configured production mode"),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.Invoke(context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            Assert.Contains("An unexpected error occurred.", await reader.ReadToEndAsync());
        }
        finally
        {
            ConfigManager.AppSettings = originalSettings;
        }
    }

    [Fact]
    public async Task LoggingProviderFailure_DoesNotPreventSafeErrorResponse()
    {
        var previousSettings = ConfigManager.AppSettings;
        ConfigManager.AppSettings = new AppSettings { IsForDebug = false };
        try
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("private failure detail"),
                new ThrowingLogger());

            await middleware.Invoke(context);

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Contains("An unexpected error occurred.", body);
            Assert.DoesNotContain("private failure detail", body);
        }
        finally
        {
            ConfigManager.AppSettings = previousSettings;
        }
    }

    [Fact]
    public async Task HttpPipeline_ReturnsTraceableSafeResponseForDashboardSourceFailure()
    {
        var originalSettings = ConfigManager.AppSettings;
        ConfigManager.AppSettings = new AppSettings { IsForDebug = false };
        try
        {
            using var host = await new HostBuilder().ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.Configure(app =>
                {
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.Run(_ => throw new InvalidOperationException("private Mongo connection detail"));
                });
            }).StartAsync();
            using var client = host.GetTestClient();

            using var response = await client.GetAsync("/api/Dashboard/GetDashboardOverview");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(StatusCodes.Status500InternalServerError, (int)response.StatusCode);
            Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
            Assert.False(string.IsNullOrWhiteSpace(Assert.Single(values!)));
            Assert.Contains("An unexpected error occurred.", body);
            Assert.DoesNotContain("private Mongo connection detail", body);
        }
        finally
        {
            ConfigManager.AppSettings = originalSettings;
        }
    }

    private sealed class ThrowingLogger : Microsoft.Extensions.Logging.ILogger<ExceptionHandlingMiddleware>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            throw new InvalidOperationException("logging provider unavailable");
    }
}
