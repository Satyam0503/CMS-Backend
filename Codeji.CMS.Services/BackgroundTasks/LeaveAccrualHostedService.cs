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
        // Execute catch-up immediately upon application startup
        _logger.LogInformation("Leave accrual hosted service started. Executing startup check...");
        await RunJob(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var indiaNow = IndiaTime.Now;
            // Schedule next execution for tomorrow at 00:05 AM IST
            var nextRunIndia = indiaNow.Date.AddDays(1).AddMinutes(5);
            var nextRun = IndiaTime.ToUtc(nextRunIndia);
            _logger.LogInformation("Next leave accrual check scheduled at: {NextRunIndia} IST ({NextRunUtc} UTC)", nextRunIndia, nextRun);
            var delay = nextRun - DateTime.UtcNow;
            if (delay <= TimeSpan.Zero)
            {
                delay = TimeSpan.FromMinutes(1);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
                await RunJob(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in leave accrual service loop.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
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
            _logger.LogInformation("Leave accrual cycle completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while accrual leave balance");
        }
    }
}
