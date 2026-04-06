using Aiursoft.Canon.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon.ScheduledTasks;

public static class ScheduledTaskExtensions
{
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

    public static IServiceCollection AddScheduledTaskEngine(this IServiceCollection services)
    {
        services.AddHostedService<JobSchedulerService>();
        return services;
    }
}
