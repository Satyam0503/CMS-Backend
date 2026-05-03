
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codeji.CMS.Services.BackgroundTasks
{
    public class PriorityQueuedHostedService : BackgroundService
    {
        private readonly ILogger<PriorityQueuedHostedService> _logger;
        private readonly IPriorityTaskQueue _taskQueue;

        public PriorityQueuedHostedService(IPriorityTaskQueue taskQueue, ILogger<PriorityQueuedHostedService> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Priority Queued Hosted Service is running.");

            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    Func<CancellationToken, Task> workItem = await _taskQueue.DequeueAsync(stoppingToken);

                    try
                    {
                        await workItem(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred executing {WorkItem}.", nameof(workItem));
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — DequeueAsync's SemaphoreSlim was cancelled
                // by the host. Catching here marks the exception as user-handled
                // so the debugger doesn't first-chance-break on every restart.
                _logger.LogInformation("Priority Queued Hosted Service shutdown requested.");
            }
        }
    }

}
