using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>Retries screening stage synchronization left pending by transient storage failures.</summary>
public sealed class RecruitmentScreeningStageSyncWorker(
    IServiceScopeFactory scopes,
    ILogger<RecruitmentScreeningStageSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var screening = scope.ServiceProvider.GetRequiredService<IRecruitmentScreeningService>();
                var recovered = await screening.RecoverPending(batchSize: 100, cancellationToken: stoppingToken);
                if (recovered > 0)
                    logger.LogInformation("Recovered {RecoveredCount} pending recruitment screening stage update(s).", recovered);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Recruitment screening stage recovery failed; pending updates will be retried.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
