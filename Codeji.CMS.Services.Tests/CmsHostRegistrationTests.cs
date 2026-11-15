using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Services.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class CmsHostRegistrationTests
{
    private sealed class NoopNotifications : INotificationService
    {
        public Task SendNotificationToUser(string userId, NotificationViewModel notification) => Task.CompletedTask;
    }

    [Fact]
    public void SchedulerContainer_ResolvesEveryBackgroundJob()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mongodb"] = "mongodb://localhost:27017/codeji_di_check",
            ["AppSettings:AppUrl"] = "http://localhost/",
            ["AppSettings:APIUrl"] = "http://localhost/api",
            ["AppSettings:AppVersion"] = "test",
            ["AppSettings:IsForDebug"] = "true",
            ["FileSettings:MaxFileSize"] = "1",
            ["EmailSettings:From"] = "noreply@localhost",
            ["reCaptcha:SecretKey"] = "x",
            ["Jwt:SecretKey"] = new string('k', 64),
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<INotificationService, NoopNotifications>();
        services.AddCmsCore(configuration);
        services.AddCmsBackgroundJobs();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        // 11 scheduled/outbox jobs + the in-process priority queue worker.
        Assert.Equal(12, provider.GetServices<IHostedService>().Count());
    }
}
