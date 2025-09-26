using Codeji.CMS.Services.Employees.Interface;
using DnsClient.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codeji.CMS.Services.BackgroundTasks;

public class BirthDayNotificationHostedServices : BackgroundService
{
    private readonly ILogger<BirthDayNotificationHostedServices> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BirthDayNotificationHostedServices(ILogger<BirthDayNotificationHostedServices> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = DateTime.Today.AddHours(1); // today at 1 AM

            if (now > nextRun)
            {
                // If it's already past 1 AM today, schedule for tomorrow 1 AM
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            _logger.LogInformation("Birthday notification service will run at {NextRun}", nextRun);

            try
            {
                await Task.Delay(delay, stoppingToken); // wait until 1 AM
            }
            catch (TaskCanceledException)
            {
                // stoppingToken was triggered, exit cleanly
                break;
            }

            // Run the actual work
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var empServices = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
                await empServices.SendBirthDayNotificationToEmployees();
                _logger.LogInformation("Birthday notifications sent at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending birthday notifications");
            }
        }
    }
}
