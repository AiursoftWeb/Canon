using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon.TaskQueue;

public static class TaskQueueServiceCollectionExtensions
{
    public static IServiceCollection AddTaskQueueEngine(this IServiceCollection services)
    {
        services.AddSingleton<ServiceTaskQueue>();
        services.AddHostedService<TaskQueueWorkerService>();
        return services;
    }
}
