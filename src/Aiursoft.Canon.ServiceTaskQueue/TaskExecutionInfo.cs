namespace Aiursoft.Canon.TaskQueue;

public class TaskExecutionInfo
{
    public Guid TaskId { get; init; } = Guid.NewGuid();

    public required string QueueName { get; init; }

    public required string TaskName { get; init; }

    public TaskExecutionStatus Status { get; set; } = TaskExecutionStatus.Pending;

    public DateTime QueuedAt { get; init; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public TaskTriggerSource TriggerSource { get; init; } = TaskTriggerSource.Unknown;

    public required Type ServiceType { get; init; }

    public required Func<object, Task> TaskAction { get; init; }
}
