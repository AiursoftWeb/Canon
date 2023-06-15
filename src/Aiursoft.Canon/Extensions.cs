using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon;

public static class Extensions
{
    /// <summary>
    /// Register task canon tasks.
    /// 
    /// (If your project is using Aiursoft.Scanner, you do NOT have to call this!)
    /// </summary>
    /// <param name="services">Services to be injected.</param>
    /// <returns>The original services.</returns>
    public static IServiceCollection AddTaskCanon(this IServiceCollection services)
    {
        // A retry engine.
        services.AddTransient<RetryEngine>();

        // An easier to use Cache service.
        // Requires IMemoryCache!
        services.AddTransient<CacheService>();

        // A transient service to replace 'Task.WhenAll()'.
        services.AddTransient<CanonPool>();

        // Simple Fire and forget service that runs immediately.
        services.AddSingleton<CanonService>();

        // Application singleton background job queue.
        services.AddSingleton<CanonQueue>();

        return services;
    }
}
