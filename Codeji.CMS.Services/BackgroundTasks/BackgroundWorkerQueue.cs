using System.Collections.Concurrent;

namespace Codeji.CMS.Services.BackgroundTasks
{
    public class BackgroundWorkerQueue
    {
        private ConcurrentQueue<Action<CancellationToken>> _workItems = new ConcurrentQueue<Action<CancellationToken>>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);
        public async Task<Action<CancellationToken>> DequeueAsync(
           CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out Action<CancellationToken> workItem);

            return workItem;
        }
        public void QueueBackgroundWorkItem(Action<CancellationToken> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }
    }
}
