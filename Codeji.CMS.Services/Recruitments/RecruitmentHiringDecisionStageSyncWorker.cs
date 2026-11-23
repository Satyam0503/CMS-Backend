using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>Repairs application-stage projections after a hiring decision was durably recorded.</summary>
public sealed class RecruitmentHiringDecisionStageSyncWorker(
    IServiceScopeFactory scopes,
    ILogger<RecruitmentHiringDecisionStageSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var hiringDecisions = scope.ServiceProvider.GetRequiredService<IRecruitmentHiringDecisionService>();
                var recovered = await hiringDecisions.RecoverPending(batchSize: 100, cancellationToken: stoppingToken);
                if (recovered > 0)
                    logger.LogInformation("Recovered {RecoveredCount} pending recruitment hiring decision stage update(s).", recovered);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Recruitment hiring decision stage recovery failed; pending updates will be retried.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
