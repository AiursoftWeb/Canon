using System.Collections.Concurrent;

namespace Aiursoft.Canon.TaskQueue;

/// <summary>
/// Application singleton task queue with named, per-queue serial execution and full status tracking.
/// </summary>
/// <remarks>
/// <para>
/// Tasks within the <b>same queue name</b> run one at a time (serial). Tasks in different
/// queue names run in parallel. This makes it safe to run multiple jobs of the same type
/// without overlap, while still executing unrelated jobs concurrently.
/// </para>
/// <para>
/// Register via <c>services.AddTaskQueueEngine()</c> which also starts
/// <see cref="TaskQueueWorkerService"/> as a hosted service.
/// </para>
/// </remarks>
public class ServiceTaskQueue
{
    private readonly ConcurrentDictionary<Guid, TaskExecutionInfo> _allTasks = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Guid>> _queuesByName = new();
    private readonly ConcurrentDictionary<string, bool> _queueProcessingStatus = new();

    public Guid QueueWithDependency<TService>(
        string queueName,
        string taskName,
        Func<TService, Task> task)
        where TService : notnull
    {
        return QueueWithDependency(queueName, taskName, task, TaskTriggerSource.Unknown);
    }

    public Guid QueueWithDependency<TService>(
        string queueName,
        string taskName,
        Func<TService, Task> task,
        TaskTriggerSource triggerSource)
        where TService : notnull
    {
        var taskInfo = new TaskExecutionInfo
        {
            QueueName = queueName,
            TaskName = taskName,
            ServiceType = typeof(TService),
            TriggerSource = triggerSource,
            TaskAction = async service => await task((TService)service)
        };

        _allTasks[taskInfo.TaskId] = taskInfo;

        var queue = _queuesByName.GetOrAdd(queueName, _ => new ConcurrentQueue<Guid>());
        queue.Enqueue(taskInfo.TaskId);

        return taskInfo.TaskId;
    }

    /// <summary>
    /// Enqueues a task using DI to resolve <typeparamref name="TService"/>.
    /// The queue name and task name both default to the service type name,
    /// and the trigger source is <see cref="TaskTriggerSource.Unknown"/>.
    /// </summary>
    public Guid QueueWithDependency<TService>(Func<TService, Task> task)
        where TService : notnull
    {
        return QueueWithDependency(typeof(TService).Name, typeof(TService).Name, task, TaskTriggerSource.Unknown);
    }

    public Guid QueueWithDependency<TService>(
        string queueName,
        string taskName,
        Func<TService, Task> task,
        Type serviceType,
        TaskTriggerSource triggerSource)
        where TService : notnull
    {
        var taskInfo = new TaskExecutionInfo
        {
            QueueName = queueName,
            TaskName = taskName,
            ServiceType = serviceType,
            TriggerSource = triggerSource,
            TaskAction = async service => await task((TService)service)
        };

        _allTasks[taskInfo.TaskId] = taskInfo;

        var queue = _queuesByName.GetOrAdd(queueName, _ => new ConcurrentQueue<Guid>());
        queue.Enqueue(taskInfo.TaskId);

        return taskInfo.TaskId;
    }

    /// <summary>
    /// Returns all task records ever recorded by this queue instance, ordered most-recent first.
    /// </summary>
    public IEnumerable<TaskExecutionInfo> GetAllTasks()
    {
        return _allTasks.Values.OrderByDescending(t => t.QueuedAt);
    }

    /// <summary>
    /// Returns tasks that completed within <paramref name="within"/> of now, most-recent first.
    /// Useful for displaying a rolling recent-history in management UIs.
    /// </summary>
    public IEnumerable<TaskExecutionInfo> GetRecentCompletedTasks(TimeSpan within)
    {
        var cutoff = DateTime.UtcNow - within;
        return _allTasks.Values
            .Where(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= cutoff)
            .OrderByDescending(t => t.CompletedAt);
    }

    /// <summary>Returns all tasks currently waiting to start, ordered by enqueue time.</summary>
    public IEnumerable<TaskExecutionInfo> GetPendingTasks()
    {
        return _allTasks.Values
            .Where(t => t.Status == TaskExecutionStatus.Pending)
            .OrderBy(t => t.QueuedAt);
    }

    /// <summary>Returns all tasks currently being executed, ordered by start time.</summary>
    public IEnumerable<TaskExecutionInfo> GetProcessingTasks()
    {
        return _allTasks.Values
            .Where(t => t.Status == TaskExecutionStatus.Processing)
            .OrderBy(t => t.StartedAt);
    }

    /// <summary>
    /// Attempts to cancel a pending task. Returns <see langword="true"/> if the task was
    /// still <see cref="TaskExecutionStatus.Pending"/> and was successfully cancelled;
    /// <see langword="false"/> if it was already running or completed.
    /// </summary>
    public bool CancelTask(Guid taskId)
    {
        if (_allTasks.TryGetValue(taskId, out var task) && task.Status == TaskExecutionStatus.Pending)
        {
            task.Status = TaskExecutionStatus.Cancelled;
            task.CompletedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    internal TaskExecutionInfo? TryDequeueNextTask(string queueName)
    {
        if (_queueProcessingStatus.TryGetValue(queueName, out var isProcessing) && isProcessing)
        {
            return null;
        }

        if (!_queuesByName.TryGetValue(queueName, out var queue))
        {
            return null;
        }

        while (queue.TryDequeue(out var taskId))
        {
            if (!_allTasks.TryGetValue(taskId, out var task))
            {
                continue;
            }

            if (task.Status == TaskExecutionStatus.Cancelled)
            {
                continue;
            }

            _queueProcessingStatus[queueName] = true;
            task.Status = TaskExecutionStatus.Processing;
            task.StartedAt = DateTime.UtcNow;
            return task;
        }

        return null;
    }

    internal void CompleteTask(Guid taskId, bool success, string? errorMessage = null)
    {
        if (_allTasks.TryGetValue(taskId, out var task))
        {
            task.Status = success ? TaskExecutionStatus.Success : TaskExecutionStatus.Failed;
            task.CompletedAt = DateTime.UtcNow;
            task.ErrorMessage = errorMessage;
            _queueProcessingStatus[task.QueueName] = false;
        }
    }

    internal IEnumerable<string> GetQueuesWithPendingTasks()
    {
        return _queuesByName.Keys;
    }

    /// <summary>
    /// Removes completed task records older than <paramref name="olderThan"/>.
    /// Called automatically by <see cref="TaskQueueWorkerService"/> every 5 minutes.
    /// </summary>
    public void CleanupOldCompletedTasks(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var taskIdsToRemove = _allTasks.Values
            .Where(t => t.CompletedAt.HasValue && t.CompletedAt.Value < cutoff)
            .Select(t => t.TaskId)
            .ToList();

        foreach (var taskId in taskIdsToRemove)
        {
            _allTasks.TryRemove(taskId, out _);
        }
    }
}
