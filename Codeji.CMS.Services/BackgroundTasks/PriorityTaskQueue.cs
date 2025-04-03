using System.Collections.Concurrent;

namespace Codeji.CMS.Services.BackgroundTasks
{
    public interface IPriorityTaskQueue
    {
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, int priority = 0);
        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
        IEnumerable<string> GetQueuedTasks();
    }
    public class PriorityTaskQueue : IPriorityTaskQueue
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<Func<CancellationToken, Task>>> _queues;
        private readonly ConcurrentDictionary<int, ConcurrentQueue<string>> _taskDetails;
        private readonly SemaphoreSlim _signal;

        public PriorityTaskQueue()
        {
            _queues = new ConcurrentDictionary<int, ConcurrentQueue<Func<CancellationToken, Task>>>();
            _taskDetails = new ConcurrentDictionary<int, ConcurrentQueue<string>>();
            _signal = new SemaphoreSlim(0);
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, int priority = 0)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            ConcurrentQueue<Func<CancellationToken, Task>> queue = _queues.GetOrAdd(priority, _ => new ConcurrentQueue<Func<CancellationToken, Task>>());
            ConcurrentQueue<string> detailsQueue = _taskDetails.GetOrAdd(priority, _ => new ConcurrentQueue<string>());

            queue.Enqueue(workItem);
            detailsQueue.Enqueue($"Task with priority {priority} enqueued at {DateTime.Now}");

            _signal.Release();
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);

            foreach (int priority in _queues.Keys.OrderByDescending(k => k))
            {
                if (_queues.TryGetValue(priority, out ConcurrentQueue<Func<CancellationToken, Task>> queue) && queue.TryDequeue(out Func<CancellationToken, Task> workItem))
                {
                    if (_taskDetails.TryGetValue(priority, out ConcurrentQueue<string> detailsQueue))
                    {
                        detailsQueue.TryDequeue(out _);
                    }
                    return workItem;
                }
            }

            throw new InvalidOperationException("Failed to dequeue work item.");
        }

        public IEnumerable<string> GetQueuedTasks()
        {
            List<string> taskDetails = new List<string>();

            foreach (int priority in _taskDetails.Keys.OrderByDescending(k => k))
            {
                if (_taskDetails.TryGetValue(priority, out ConcurrentQueue<string> detailsQueue))
                {
                    taskDetails.AddRange(detailsQueue);
                }
            }

            return taskDetails;
        }
    }
}
