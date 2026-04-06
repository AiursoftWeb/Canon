using Aiursoft.Canon.TaskQueue;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon.BackgroundJobs;

/// <summary>
/// Application-wide singleton registry of all pre-registered <see cref="IBackgroundJob"/> types.
/// Allows administrators to discover, inspect, and instantly trigger any registered job.
/// </summary>
/// <remarks>
/// Register via <see cref="BackgroundJobRegistryExtensions.RegisterBackgroundJob{TJob}"/>.
/// Inject this class wherever you need to list or trigger jobs (e.g. an admin controller).
/// </remarks>
public class BackgroundJobRegistry(
    ServiceTaskQueue taskQueue,
    IServiceScopeFactory serviceScopeFactory,
    IEnumerable<RegisteredJob> registrations)
{
    private readonly IReadOnlyList<RegisteredJob> _registrations = registrations.ToList().AsReadOnly();

    /// <summary>
    /// Returns descriptors for all registered jobs, with <see cref="RegisteredJob.Name"/> and
    /// <see cref="RegisteredJob.Description"/> populated by resolving each job from DI.
    /// </summary>
    public IReadOnlyList<RegisteredJob> GetAll() =>
        _registrations.Select(r => BuildSummary(r.JobType)).ToList().AsReadOnly();

    /// <summary>Finds a registration by its exact <see cref="IBackgroundJob"/> implementation type.</summary>
    /// <returns>The matching <see cref="RegisteredJob"/>, or <see langword="null"/> if not registered.</returns>
    public RegisteredJob? FindByType(Type jobType) =>
        _registrations.FirstOrDefault(r => r.JobType == jobType);

    /// <summary>Finds a registration by the simple type name (e.g. <c>"CleanupJob"</c>).</summary>
    /// <returns>The matching <see cref="RegisteredJob"/>, or <see langword="null"/> if not registered.</returns>
    public RegisteredJob? FindByTypeName(string typeName) =>
        _registrations.FirstOrDefault(r => r.JobType.Name == typeName);

    /// <summary>Enqueues <typeparamref name="TJob"/> for immediate execution with <c>TriggerSource.Manual</c>.</summary>
    /// <returns>The <see cref="TaskExecutionInfo.TaskId"/> assigned to this run.</returns>
    public Guid TriggerNow<TJob>() where TJob : class, IBackgroundJob =>
        TriggerNow(typeof(TJob), TaskTriggerSource.Manual);

    /// <summary>
    /// Enqueues a job by runtime type with <c>TriggerSource.Manual</c>.
    /// Throws <see cref="InvalidOperationException"/> if the type is not registered.
    /// </summary>
    public Guid TriggerNow(Type jobType)
    {
        return TriggerNow(jobType, TaskTriggerSource.Manual);
    }

    /// <summary>
    /// Enqueues a job by runtime type with an explicit <paramref name="triggerSource"/>.
    /// Used internally by <c>JobSchedulerService</c> to mark scheduled runs.
    /// </summary>
    public Guid TriggerNow(Type jobType, TaskTriggerSource triggerSource)
    {
        var registration = FindByType(jobType)
            ?? throw new InvalidOperationException(
                $"Job type '{jobType.Name}' is not registered. Make sure you called services.RegisterBackgroundJob<TJob>().");

        var summary = BuildSummary(registration.JobType);

        return taskQueue.QueueWithDependency<IBackgroundJob>(
            queueName: jobType.Name,
            taskName: BuildTaskName(summary.Name ?? jobType.Name, triggerSource),
            task: job => job.ExecuteAsync(),
            serviceType: jobType,
            triggerSource: triggerSource);
    }

    /// <summary>
    /// Enqueues a job by its simple type name string with <c>TriggerSource.Manual</c>.
    /// Useful for generic admin endpoints that receive the job name from user input.
    /// </summary>
    public Guid TriggerNow(string jobTypeName)
    {
        return TriggerNow(jobTypeName, TaskTriggerSource.Manual);
    }

    /// <summary>Enqueues a job by its simple type name with an explicit <paramref name="triggerSource"/>.</summary>
    public Guid TriggerNow(string jobTypeName, TaskTriggerSource triggerSource)
    {
        var registration = FindByTypeName(jobTypeName)
            ?? throw new InvalidOperationException($"No registered job with type name '{jobTypeName}'.");
        return TriggerNow(registration.JobType, triggerSource);
    }

    private RegisteredJob BuildSummary(Type jobType)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService(jobType) as IBackgroundJob
            ?? throw new InvalidOperationException(
                $"Registered job type '{jobType.Name}' does not implement IBackgroundJob.");

        return new RegisteredJob
        {
            JobType = jobType,
            Name = job.Name,
            Description = job.Description
        };
    }

    private static string BuildTaskName(string displayName, TaskTriggerSource triggerSource)
    {
        var sourceText = triggerSource switch
        {
            TaskTriggerSource.Manual => "manual",
            TaskTriggerSource.Scheduled => "scheduled",
            _ => "unknown"
        };

        return $"{displayName} ({sourceText} trigger {DateTime.UtcNow:HH:mm:ss})";
    }
}
