using Aiursoft.Canon.TaskQueue;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon.BackgroundJobs;

public class BackgroundJobRegistry(
    ServiceTaskQueue taskQueue,
    IServiceScopeFactory serviceScopeFactory,
    IEnumerable<RegisteredJob> registrations)
{
    private readonly IReadOnlyList<RegisteredJob> _registrations = registrations.ToList().AsReadOnly();

    public IReadOnlyList<RegisteredJob> GetAll() =>
        _registrations.Select(r => BuildSummary(r.JobType)).ToList().AsReadOnly();

    public RegisteredJob? FindByType(Type jobType) =>
        _registrations.FirstOrDefault(r => r.JobType == jobType);

    public RegisteredJob? FindByTypeName(string typeName) =>
        _registrations.FirstOrDefault(r => r.JobType.Name == typeName);

    public Guid TriggerNow<TJob>() where TJob : class, IBackgroundJob =>
        TriggerNow(typeof(TJob), TaskTriggerSource.Manual);

    public Guid TriggerNow(Type jobType)
    {
        return TriggerNow(jobType, TaskTriggerSource.Manual);
    }

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

    public Guid TriggerNow(string jobTypeName)
    {
        return TriggerNow(jobTypeName, TaskTriggerSource.Manual);
    }

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
