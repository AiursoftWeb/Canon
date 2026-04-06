using Aiursoft.Canon.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon.ScheduledTasks;

/// <summary>Extension methods for attaching recurring schedules to registered background jobs.</summary>
public static class ScheduledTaskExtensions
{
    /// <summary>
    /// Attaches a recurring timer to an already-registered background job.
    /// The job must have been registered first via
    /// <see cref="BackgroundJobRegistryExtensions.RegisterBackgroundJob{TJob}"/>.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="registration">
    /// The descriptor returned by <c>RegisterBackgroundJob</c>.
    /// </param>
    /// <param name="period">
    /// How often the job should run. Defaults to 3 hours when <see langword="null"/>.
    /// </param>
    /// <param name="startDelay">
    /// How long to wait after application startup before the first trigger.
    /// Defaults to 3 minutes when <see langword="null"/>.
    /// </param>
    public static IServiceCollection RegisterScheduledTask(
        this IServiceCollection services,
        RegisteredJob registration,
        TimeSpan? period = null,
        TimeSpan? startDelay = null)
    {
        ArgumentNullException.ThrowIfNull(registration);

        services.AddSingleton(new ScheduledTaskRegistration
        {
            JobType = registration.JobType,
            Period = period ?? TimeSpan.FromHours(3),
            StartDelay = startDelay ?? TimeSpan.FromMinutes(3)
        });

        return services;
    }

    /// <summary>
    /// Registers <see cref="JobSchedulerService"/> as a hosted service that fires
    /// a <see cref="System.Threading.Timer"/> for every <see cref="ScheduledTaskRegistration"/>
    /// in the DI container.
    /// </summary>
    public static IServiceCollection AddScheduledTaskEngine(this IServiceCollection services)
    {
        services.AddHostedService<JobSchedulerService>();
        return services;
    }
}
