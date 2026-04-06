namespace Aiursoft.Canon.TaskQueue;

/// <summary>Indicates what caused a task to be enqueued.</summary>
public enum TaskTriggerSource
{
    /// <summary>Source is not known (enqueued directly via <see cref="ServiceTaskQueue"/> without context).</summary>
    Unknown,

    /// <summary>Triggered explicitly by a user or administrator.</summary>
    Manual,

    /// <summary>Triggered automatically by <c>JobSchedulerService</c> on a recurring timer.</summary>
    Scheduled
}
