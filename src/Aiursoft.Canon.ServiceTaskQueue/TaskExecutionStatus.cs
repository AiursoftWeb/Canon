namespace Aiursoft.Canon.TaskQueue;

/// <summary>Lifecycle state of a task managed by <see cref="ServiceTaskQueue"/>.</summary>
public enum TaskExecutionStatus
{
    /// <summary>Queued and waiting for its named queue to become free.</summary>
    Pending,

    /// <summary>Currently being executed.</summary>
    Processing,

    /// <summary>Completed without throwing an exception.</summary>
    Success,

    /// <summary>Threw an unhandled exception; see <see cref="TaskExecutionInfo.ErrorMessage"/>.</summary>
    Failed,

    /// <summary>Cancelled via <see cref="ServiceTaskQueue.CancelTask"/> before execution started.</summary>
    Cancelled
}
