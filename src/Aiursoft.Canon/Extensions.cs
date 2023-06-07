﻿using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon;

public static class Extensions
{
    public static IServiceCollection AddTaskCanon(this IServiceCollection services)
    {
        services.AddSingleton<CanonService>();
        services.AddSingleton<CanonQueue>();
        return services;
    }
}