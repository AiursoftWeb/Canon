using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon;

public static class Extensions
{
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
