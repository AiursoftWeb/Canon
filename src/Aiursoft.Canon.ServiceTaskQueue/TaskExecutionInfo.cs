namespace Aiursoft.Canon.TaskQueue;

/// <summary>
/// Snapshot of all metadata and lifecycle state for a single task managed by
/// <see cref="ServiceTaskQueue"/>.
/// </summary>
public class TaskExecutionInfo
{
    /// <summary>Unique identifier for this task instance.</summary>
    public Guid TaskId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Name of the named queue this task belongs to. Tasks in the same queue
    /// run serially; tasks in different queues run in parallel.
    /// </summary>
    public required string QueueName { get; init; }

    /// <summary>Human-readable label for display in management UIs and logs.</summary>
    public required string TaskName { get; init; }

    /// <summary>Current lifecycle state of this task.</summary>
    public TaskExecutionStatus Status { get; set; } = TaskExecutionStatus.Pending;

    /// <summary>UTC time when the task was added to the queue.</summary>
    public DateTime QueuedAt { get; init; } = DateTime.UtcNow;

    /// <summary>UTC time when the worker picked up this task; <see langword="null"/> while pending.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>UTC time when the task finished (succeeded, failed, or was cancelled).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Exception details when <see cref="Status"/> is <see cref="TaskExecutionStatus.Failed"/>.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>What caused this task to be enqueued.</summary>
    public TaskTriggerSource TriggerSource { get; init; } = TaskTriggerSource.Unknown;

    /// <summary>
    /// The DI service type resolved to execute this task.
    /// For <c>IBackgroundJob</c>-based tasks this is the concrete job type.
    /// </summary>
    public required Type ServiceType { get; init; }

    /// <summary>The actual work to perform, invoked by the worker with a resolved service instance.</summary>
    public required Func<object, Task> TaskAction { get; init; }
}
