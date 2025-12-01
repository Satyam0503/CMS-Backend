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
            var now = DateTime.UtcNow;
            DateTime nextRun;
            if (now.Day == 1)
            {
                _logger.LogInformation("Running leave accrual service on the 1st of the month: {Now}", now);
                await RunJob(stoppingToken);
                nextRun = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
                _logger.LogInformation("Leave accrual service Next run scheduled for: {NextRun}", nextRun);
            }
            else
            {
                // Schedule for the 1st of next month if todat is not 1st day of month
                nextRun = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
                _logger.LogInformation("Leave accrual service Next run scheduled for: {NextRun}", nextRun);
            }
            var delay = nextRun - now;
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





