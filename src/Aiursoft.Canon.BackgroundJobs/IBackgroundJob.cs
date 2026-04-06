namespace Aiursoft.Canon.BackgroundJobs;

/// <summary>
/// Contract for a named, describable background job that can be registered in
/// <see cref="BackgroundJobRegistry"/> and optionally scheduled via
/// <c>Aiursoft.Canon.ScheduledTasks</c>.
/// </summary>
/// <remarks>
/// Implement this interface on a class that receives its dependencies through the
/// constructor (normal DI). When triggered from the admin UI all defaults apply;
/// there are no extra call-time parameters on this interface.
/// </remarks>
public interface IBackgroundJob
{
    /// <summary>Human-readable display name shown in management UIs.</summary>
    string Name { get; }

    /// <summary>One-line description of what this job does.</summary>
    string Description { get; }

    /// <summary>Runs the job. Called once per trigger (manual or scheduled).</summary>
    Task ExecuteAsync();
}
