using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon.TaskQueue;

public class TaskQueueWorkerService(
    ServiceTaskQueue taskQueue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TaskQueueWorkerService> logger) : IHostedService, IDisposable
{
    private Timer? _timer;
    private Timer? _cleanupTimer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Task Queue Worker is starting");
        _timer = new Timer(ProcessTasks, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        _cleanupTimer = new Timer(CleanupOldTasks, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    private void ProcessTasks(object? state)
    {
        if (!_semaphore.Wait(0))
        {
            return;
        }

        try
        {
            var queues = taskQueue.GetQueuesWithPendingTasks().ToList();
            foreach (var queueName in queues)
            {
                var task = taskQueue.TryDequeueNextTask(queueName);
                if (task != null)
                {
                    _ = Task.Run(async () => await ProcessTaskAsync(task));
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessTaskAsync(TaskExecutionInfo task)
    {
        try
        {
            logger.LogInformation("Processing task {TaskId} ({TaskName}) from queue {QueueName}",
                task.TaskId, task.TaskName, task.QueueName);

            using var scope = serviceScopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService(task.ServiceType);
            await task.TaskAction(service);

            taskQueue.CompleteTask(task.TaskId, true);
            logger.LogInformation("Task {TaskId} ({TaskName}) completed successfully", task.TaskId, task.TaskName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Task {TaskId} ({TaskName}) failed with error: {Error}",
                task.TaskId, task.TaskName, ex.Message);
            taskQueue.CompleteTask(task.TaskId, false, ex.ToString());
        }
    }

    private void CleanupOldTasks(object? state)
    {
        try
        {
            logger.LogInformation("Task Queue Worker: cleaning up old task records");
            taskQueue.CleanupOldCompletedTasks(TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Task Queue Worker: failed to clean up old task records");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Task Queue Worker is stopping");
        _timer?.Change(Timeout.Infinite, 0);
        _cleanupTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _cleanupTimer?.Dispose();
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
