using Aiursoft.Canon.Models;
using Aiursoft.Scanner.Abstract;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon;

/// <summary>
/// A transient service to replace 'Task.WhenAll()'.
/// 
/// Implements a task queue that can be used to add tasks to a queue and execute them with a specified degree of parallelism.
///
/// This service shall be used from dependency injection and is a transient pool, used for replacement for 'Task.WhenAll()'.
/// </summary>
public class CanonPool : ITransientDependency
{
    private readonly ILogger<CanonPool> _logger;
    private readonly SafeQueue<Func<Task>> _pendingTaskFactories = new();

    public CanonPool(ILogger<CanonPool> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a new task to the queue. (This will NOT run the task!) Call 'await RunAllInPoolAsync()' to run.
    /// </summary>
    /// <param name="taskFactory">A factory method that creates the task to be added to the queue.</param>
    public void RegisterNewTaskToPool(Func<Task> taskFactory)
    {
        _pendingTaskFactories.Enqueue(taskFactory);
    }

    /// <summary>
    /// Executes the tasks in the queue with a specified degree of parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism to use when executing the tasks.</param>
    /// <returns>A task that represents the completion of all the tasks in the queue.</returns>
    public async Task RunAllTasksInPoolAsync(int maxDegreeOfParallelism = 8)
    {
        var tasksInFlight = new List<Task>(maxDegreeOfParallelism);
        while (_pendingTaskFactories.Any() || tasksInFlight.Any())
        {
            while (tasksInFlight.Count < maxDegreeOfParallelism && _pendingTaskFactories.Any())
            {
                var taskFactory = _pendingTaskFactories.Dequeue();
                tasksInFlight.Add(taskFactory());
                _logger?.LogDebug(
                    "Engine selected one job to run. Currently there are still {Remaining} jobs remaining. {InFlight} jobs running", _pendingTaskFactories.Count(), tasksInFlight.Count);
            }

            var completedTask = await Task.WhenAny(tasksInFlight).ConfigureAwait(false);
            await completedTask.ConfigureAwait(false);
            _logger?.LogInformation(
                "Engine finished one job. Currently there are still {Remaining} jobs remaining. {InFlight} jobs running", _pendingTaskFactories.Count(), tasksInFlight.Count);
            tasksInFlight.Remove(completedTask);
        }
    }
}
