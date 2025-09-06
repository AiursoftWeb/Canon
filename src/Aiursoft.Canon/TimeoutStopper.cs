using Aiursoft.Scanner.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon;

/// <summary>
/// Provides a service for running tasks that can be cancelled after a specified timeout.
/// </summary>
public class TimeoutStopper : ITransientDependency
{
    private readonly ILogger<TimeoutStopper> _logger;

    /// <summary>
    /// Initializes a new instance of the TimeoutStopper class.
    /// </summary>
    /// <param name="logger">An instance of ILogger used to log timeout-related events.</param>
    public TimeoutStopper(ILogger<TimeoutStopper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes an asynchronous function with a timeout. If the function does not complete within the timeout, a TimeoutException is thrown.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the task.</typeparam>
    /// <param name="taskFactory">A function that takes a CancellationToken and returns the task to be executed.</param>
    /// <param name="timeoutInSeconds">The timeout duration in seconds. Defaults to 30 seconds.</param>
    /// <returns>The result of the task if it completes within the specified timeout.</returns>
    /// <exception cref="TimeoutException">Thrown when the task does not complete within the specified timeout.</exception>
    public async Task<T> RunWithTimeout<T>(Func<CancellationToken, Task<T>> taskFactory, int timeoutInSeconds = 30)
    {
        _logger.LogTrace("Starting a task with a timeout of {Timeout} seconds.", timeoutInSeconds);
        using var cts = new CancellationTokenSource();
        var task = taskFactory(cts.Token);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutInSeconds), cts.Token);

        var completedTask = await Task.WhenAny(task, timeoutTask);

        await cts.CancelAsync();
        if (completedTask == timeoutTask)
        {
            // The timeout task completed first, so cancel the original task.
            _logger.LogError("Task timed out after {Timeout} seconds.", timeoutInSeconds);
            throw new TimeoutException($"The operation timed out after {timeoutInSeconds} seconds.");
        }
        else
        {
            // The original task completed first, so we can cancel the timeout token and return its result.
            // This is just good practice, although not strictly necessary if 'using' is used.
            return await task;
        }
    }

    /// <summary>
    /// Executes an asynchronous function with a timeout, without a return value. If the function does not complete within the timeout, a TimeoutException is thrown.
    /// </summary>
    /// <param name="taskFactory">A function that takes a CancellationToken and returns the task to be executed.</param>
    /// <param name="timeoutInSeconds">The timeout duration in seconds. Defaults to 30 seconds.</param>
    /// <exception cref="TimeoutException">Thrown when the task does not complete within the specified timeout.</exception>
    public async Task RunWithTimeout(Func<CancellationToken, Task> taskFactory, int timeoutInSeconds = 30)
    {
        await RunWithTimeout(async (token) =>
        {
            await taskFactory(token);
            return 0;
        }, timeoutInSeconds);
    }
}
