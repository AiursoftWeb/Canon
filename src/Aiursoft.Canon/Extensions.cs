using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon;

public static class Extensions
{
    public static IServiceCollection AddTaskCanon(this IServiceCollection services)
    {
        services.AddTransient<RetryEngine>();
        services.AddTransient<CacheService>();
        services.AddSingleton<CanonService>();
        services.AddSingleton<CanonQueue>();
        return services;
    }
}
