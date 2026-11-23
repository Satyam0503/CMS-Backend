using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
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

        // Scheduled/outbox jobs plus the priority queue, recruitment stage recovery/indexes, and private resume cleanup workers.
        Assert.Equal(17, provider.GetServices<IHostedService>().Count());
        Assert.Contains(provider.GetServices<IHostedService>(), worker => worker is RecruitmentScreeningStageSyncWorker);
        Assert.Contains(provider.GetServices<IHostedService>(), worker => worker is RecruitmentHiringDecisionStageSyncWorker);
        Assert.Contains(provider.GetServices<IHostedService>(), worker => worker is RecruitmentResumeScanMigrationWorker);
        Assert.IsType<ClamAvRecruitmentResumeScanner>(provider.GetRequiredService<IRecruitmentResumeScanner>());
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IPublicCareerService) &&
            descriptor.ImplementationType == typeof(PublicCareerService));
    }
}
