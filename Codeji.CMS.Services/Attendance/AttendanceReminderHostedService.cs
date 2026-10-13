using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codeji.CMS.Services.Attendance;

public sealed class AttendanceReminderHostedService(IServiceProvider services, ILogger<AttendanceReminderHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan SchedulerInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeZoneInfo IndiaTimeZone = ResolveIndiaTimeZone();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) 
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var indianNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
                var processor = scope.ServiceProvider.GetRequiredService<IAttendanceReminderProcessor>();
                await processor.ProcessAutomaticPresentAsync(indianNow, stoppingToken);
                await processor.ProcessReviewReminderAsync(indianNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            // A failed host startup (for example, a duplicate HTTP port) disposes the root
            // provider while hosted services are being stopped. This is shutdown noise, not an
            // attendance-processing failure, and the worker cannot safely continue afterwards.
            catch (ObjectDisposedException ex) when (ex.ObjectName == "IServiceProvider" || stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attendance start/review scheduler run failed.");
            }

            try
            {
                await Task.Delay(SchedulerInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private static TimeZoneInfo ResolveIndiaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}
 
