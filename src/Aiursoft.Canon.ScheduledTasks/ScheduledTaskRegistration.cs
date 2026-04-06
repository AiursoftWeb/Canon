namespace Aiursoft.Canon.ScheduledTasks;

/// <summary>
/// Configuration record that binds an <see cref="Aiursoft.Canon.BackgroundJobs.IBackgroundJob"/>
/// type to a recurring timer managed by <see cref="JobSchedulerService"/>.
/// Created by <see cref="ScheduledTaskExtensions.RegisterScheduledTask"/>.
/// </summary>
public class ScheduledTaskRegistration
{
    /// <summary>The concrete <see cref="Aiursoft.Canon.BackgroundJobs.IBackgroundJob"/> type to trigger.</summary>
    public required Type JobType { get; init; }

    /// <summary>How long to wait after application startup before the first trigger fires.</summary>
    public required TimeSpan StartDelay { get; init; }

    /// <summary>How often the job is triggered after the initial <see cref="StartDelay"/>.</summary>
    public required TimeSpan Period { get; init; }
}
