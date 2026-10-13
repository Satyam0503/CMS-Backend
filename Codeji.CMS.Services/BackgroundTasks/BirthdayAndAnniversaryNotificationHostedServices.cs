using Codeji.CMS.Services.Employees.Interface;
using DnsClient.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services.BackgroundTasks;

public class BirthDayAndAnniversaryNotificationHostedServices : BackgroundService
{
    private readonly ILogger<BirthDayAndAnniversaryNotificationHostedServices> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BirthDayAndAnniversaryNotificationHostedServices(ILogger<BirthDayAndAnniversaryNotificationHostedServices> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = IndiaTime.Now;
            var nextRun = IndiaTime.Today.AddHours(1); // 1 AM IST
            if (now > nextRun)
            {
                nextRun = nextRun.AddDays(1); // set next run to tommarrow 1 am 
            }

            var delay = nextRun - now;
            _logger.LogInformation("notification service will run at {NextRun}", nextRun);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var empServices = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
                await empServices.SendBirthDayAndAnniversaryNotificationToEmployees();
                _logger.LogInformation("Birthday/Anniversary notifications sent at {Time}", IndiaTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending birthday/anniversary notifications");
            }
        }
    }
}
