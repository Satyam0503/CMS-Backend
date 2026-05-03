using Codeji.CMS.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Codeji.CMS.Services.PayRoll
{
    public class PayrollHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        
        private readonly ILogger<PayrollHostedService> _logger;

        public PayrollHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<PayrollHostedService> logger)
{
    _scopeFactory = scopeFactory;
    _logger = logger;
}

 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Payroll Hosted Service started at {time}", DateTime.UtcNow);

    TimeZoneInfo indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTime.UtcNow;
            var nowIndia = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, indiaZone);

            // Schedule for 11:11 IST
            var scheduledTimeIndia = new DateTime(
                nowIndia.Year,
                nowIndia.Month,
                nowIndia.Day,
                13, 30, 0);

            if (nowIndia >= scheduledTimeIndia)
                scheduledTimeIndia = scheduledTimeIndia.AddDays(1);

            var scheduledUtc = TimeZoneInfo.ConvertTimeToUtc(scheduledTimeIndia, indiaZone);
            var delay = scheduledUtc - nowUtc;

            _logger.LogInformation("Next payroll run at {time}", scheduledTimeIndia);

            if (delay.TotalMilliseconds > 0)
                await Task.Delay(delay, stoppingToken);

            await RunPayroll(stoppingToken);
        }
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
        // Graceful shutdown — Task.Delay / RunPayroll cancelled by host.
        // Catching here marks the exception as user-handled so the debugger
        // doesn't first-chance-break on every restart during dev.
        _logger.LogInformation("Payroll Hosted Service shutdown requested.");
    }
}

private async Task RunPayroll(CancellationToken stoppingToken)
{
    try
    {
        _logger.LogInformation("Payroll execution started at {time}", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();

        var autoPayroll = scope.ServiceProvider.GetRequiredService<AutoPayrollServices>();
        var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();

        var companies = await companyService.GetAllCompanyList();

        DateTime currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        foreach (var company in companies.Where(c => c.Status && !c.IsDeleted))
        {
            await autoPayroll.GenerateOrUpdatePayrollForMonthAsync(
                company.CompanyId,
                currentMonth
            );
        }

        _logger.LogInformation("Payroll execution completed successfully.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Payroll execution failed.");
    }
}


    }
}

