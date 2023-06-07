using Aiursoft.Canon.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon;

/// <summary>
/// 
/// </summary>
public class CanonQueue
{
    private readonly ILogger<CanonQueue> _logger;
    private readonly SafeQueue<Func<Task>> _pendingTaskFactories = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object loc = new();
    private Task _engine = Task.CompletedTask;

    public CanonQueue(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CanonQueue> logger)
    {
        _scopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="taskFactory"></param>
    /// <param name="startTheEngine"></param>
    public void QueueNew(Func<Task> taskFactory, bool startTheEngine = true)
    {
        _pendingTaskFactories.Enqueue(taskFactory);
        if (!startTheEngine)
        {
            return;
        }

        lock (loc)
        {
            if (!_engine.IsCompleted)
            {
                return;
            }

            _logger.LogDebug("Engine is sleeping. Trying to wake it up.");
            _engine = RunTasksInQueue();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bullet"></param>
    public void QueueWithDependency<T>(Func<T, Task> bullet) where T : class
    {
        QueueNew(async () =>
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var dependency = scope.ServiceProvider.GetRequiredService<T>();
                    await bullet(dependency);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, $"An error occurred with a Canon task with dependency: '{typeof(T).Name}'.");
                }
            }
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="maxDegreeOfParallelism"></param>
    /// <returns></returns>
    public async Task RunTasksInQueue(int maxDegreeOfParallelism = 8)
    {
        var tasksInFlight = new List<Task>(maxDegreeOfParallelism);
        while (_pendingTaskFactories.Any() || tasksInFlight.Any())
        {
            while (tasksInFlight.Count < maxDegreeOfParallelism && _pendingTaskFactories.Any())
            {
                var taskFactory = _pendingTaskFactories.Dequeue();
                tasksInFlight.Add(taskFactory());
                _logger.LogDebug(
                    $"Engine selected one job to run. Currently there are still {_pendingTaskFactories.Count()} jobs remaining. {tasksInFlight.Count} jobs running.");
            }

            var completedTask = await Task.WhenAny(tasksInFlight).ConfigureAwait(false);
            await completedTask.ConfigureAwait(false);
            _logger.LogInformation(
                $"Engine finished one job. Currently there are still {_pendingTaskFactories.Count()} jobs remaining. {tasksInFlight.Count} jobs running.");
            tasksInFlight.Remove(completedTask);
        }
    }
}