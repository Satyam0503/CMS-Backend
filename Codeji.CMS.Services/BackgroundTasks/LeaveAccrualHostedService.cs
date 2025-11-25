using Codeji.CMS.Services.LeaveManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            var now = DateTime.Now;
            DateTime nextRun;
            // If today is the 1st of the current month, run immediately
            if (now.Day == 1)
            {
                nextRun = now;
                _logger.LogInformation("Running leave accrual service on the 1st of the month: {NextRun}", nextRun);
            }
            else
            {
                // Otherwise, schedule for the 1st of the next month
                nextRun = new DateTime(now.Year, now.Month, 1).AddMonths(1).Date;
                _logger.LogInformation("Next run scheduled for the 1st of next month: {NextRun}", nextRun);
            }

            var delay = nextRun - now; // Calculate delay until next run

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
                var leaveServices = scope.ServiceProvider.GetRequiredService<ILeaveManagementService>();
                await leaveServices.EmployeeLeaveBalanceAccrual();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while accrual leave balance");
            }
        }
    }
}