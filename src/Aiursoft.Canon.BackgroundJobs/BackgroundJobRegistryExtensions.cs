using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon.BackgroundJobs;

public static class BackgroundJobRegistryExtensions
{
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
