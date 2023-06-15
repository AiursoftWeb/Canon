using Aiursoft.Scanner;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon;

public static class Extensions
{
    public static IServiceCollection AddTaskCanon(this IServiceCollection services)
    {
        services.AddLibraryDependencies();
        return services;
    }
}
