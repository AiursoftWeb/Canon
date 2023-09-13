using Aiursoft.Canon.Models;
using Aiursoft.Scanner.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon;

/// <summary>
/// Application singleton background job queue.
/// 
/// Implements a task queue that can be used to add tasks to a queue and execute them with a specified degree of parallelism.
///
/// This service shall be used from dependency injection and is an application wide global queue, used for fire and forget.
/// </summary>
public class CanonQueue : ISingletonDependency
{
    private readonly ILogger<CanonQueue> _logger;
    private readonly SafeQueue<Func<Task>> _pendingTaskFactories = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _loc = new();
    private Task _engine = Task.CompletedTask;

    public CanonQueue(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CanonQueue> logger)
    {
        _scopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Adds a new task to the queue.
    /// </summary>
    /// <param name="taskFactory">A factory method that creates the task to be added to the queue.</param>
    /// <param name="startTheEngine">A boolean value indicating whether to start the engine to execute the tasks in the queue.</param>
    public void QueueNew(Func<Task> taskFactory, bool startTheEngine = true, int maxThreads = 8)
    {
        _pendingTaskFactories.Enqueue(taskFactory);
        if (!startTheEngine)
        {
            return;
        }

        lock (_loc)
        {
            if (!_engine.IsCompleted)
            {
                return;
            }

            _logger.LogDebug("Engine is sleeping. Trying to wake it up");
            _engine = RunTasksInQueue(maxThreads);
        }
    }

    /// <summary>
    /// Adds a new task to the queue with a dependency injection.
    /// </summary>
    /// <typeparam name="T">The type of the dependency to be injected.</typeparam>
    /// <param name="bullet">A factory method that creates the task to be added to the queue.</param>
    /// <param name="startTheEngine">A boolean value indicating whether to start the engine to execute the tasks in the queue.</param>
    public void QueueWithDependency<T>(Func<T, Task> bullet, bool startTheEngine = true) where T : class
    {
        QueueNew(async () =>
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dependency = scope.ServiceProvider.GetRequiredService<T>();
                try
                {
                    await bullet(dependency);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "An error occurred with a Canon task with dependency: '{Dependency}'", typeof(T).Name);
                }
                finally
                {
                    (dependency as IDisposable)?.Dispose();
                }
            }
        }, startTheEngine);
    }

    /// <summary>
    /// Executes the tasks in the queue with a specified degree of parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism to use when executing the tasks.</param>
    /// <returns>A task that represents the completion of all the tasks in the queue.</returns>
    public async Task RunTasksInQueue(int maxDegreeOfParallelism)
    {
        var tasksInFlight = new List<Task>(maxDegreeOfParallelism);
        while (_pendingTaskFactories.Any() || tasksInFlight.Any())
        {
            while (tasksInFlight.Count < maxDegreeOfParallelism && _pendingTaskFactories.Any())
            {
                var taskFactory = _pendingTaskFactories.Dequeue();
                tasksInFlight.Add(taskFactory());
                _logger.LogDebug(
                    "Engine selected one job to run. Currently there are still {Remaining} jobs remaining. {InFlight} jobs running", _pendingTaskFactories.Count(), tasksInFlight.Count);
            }

            var completedTask = await Task.WhenAny(tasksInFlight).ConfigureAwait(false);
            await completedTask.ConfigureAwait(false);
            _logger.LogTrace(
                "Engine finished one job. Currently there are still {Remaining} jobs remaining. {InFlight} jobs running", _pendingTaskFactories.Count(), tasksInFlight.Count);
            tasksInFlight.Remove(completedTask);
        }
    }
}
