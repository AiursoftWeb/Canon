namespace Aiursoft.Canon.ScheduledTasks;

public class ScheduledTaskRegistration
{
    public required Type JobType { get; init; }

    public required TimeSpan StartDelay { get; init; }

    public required TimeSpan Period { get; init; }
}
