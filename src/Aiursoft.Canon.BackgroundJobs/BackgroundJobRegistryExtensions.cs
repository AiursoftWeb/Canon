using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon.BackgroundJobs;

/// <summary>Extension methods for registering background jobs into the DI container.</summary>
public static class BackgroundJobRegistryExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TJob"/> as a transient <see cref="IBackgroundJob"/> and
    /// records it in the application-wide <see cref="BackgroundJobRegistry"/>.
    /// </summary>
    /// <typeparam name="TJob">Concrete job type that implements <see cref="IBackgroundJob"/>.</typeparam>
    /// <param name="services">The application service collection.</param>
    /// <returns>
    /// A <see cref="RegisteredJob"/> descriptor that can be passed directly to
    /// <c>ScheduledTaskExtensions.RegisterScheduledTask</c> to attach a recurring schedule.
    /// </returns>
    public static RegisteredJob RegisterBackgroundJob<TJob>(this IServiceCollection services)
        where TJob : class, IBackgroundJob
    {
        services.AddTransient<TJob>();

        var registration = new RegisteredJob
        {
            JobType = typeof(TJob)
        };

        services.AddSingleton(registration);
        services.AddSingleton<BackgroundJobRegistry>();

        return registration;
    }
}
