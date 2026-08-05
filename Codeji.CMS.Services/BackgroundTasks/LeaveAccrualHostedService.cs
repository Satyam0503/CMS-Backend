using Codeji.CMS.Services.LeaveManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services.BackgroundTasks;

public class LeaveAccrualHostedService : BackgroundService
{
    readonly ILogger<LeaveAccrualHostedService> _logger;
    readonly IServiceProvider _serviceProvider;

    public LeaveAccrualHostedService(IServiceProvider serviceProvider, ILogger<LeaveAccrualHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var indiaNow = IndiaTime.Now;
            DateTime nextRunIndia;
            if (indiaNow.Day == 1)
            {
                _logger.LogInformation("Running leave accrual service on the 1st of the month in IST: {Now}", indiaNow);
                await RunJob(stoppingToken);
                nextRunIndia = new DateTime(indiaNow.Year, indiaNow.Month, 1).AddMonths(1);
            }
            else
            {
                nextRunIndia = new DateTime(indiaNow.Year, indiaNow.Month, 1).AddMonths(1);
            }
            var nextRun = IndiaTime.ToUtc(nextRunIndia);
            _logger.LogInformation("Leave accrual service next run: {NextRunIndia} IST ({NextRunUtc} UTC)", nextRunIndia, nextRun);
            var delay = nextRun - DateTime.UtcNow;
            if (delay <= TimeSpan.Zero)
            {
                delay = TimeSpan.FromSeconds(1);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunJob(CancellationToken token = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var leaveServices = scope.ServiceProvider.GetRequiredService<ILeaveManagementService>();
            await leaveServices.EmployeeLeaveBalanceAccrual();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while accrual leave balance");
        }
    }
}





