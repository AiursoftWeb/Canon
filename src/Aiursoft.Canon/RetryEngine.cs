using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon;

/// <summary>
/// Provides a service for retrying tasks that may fail.
/// </summary>
public class RetryEngine
{
    private static readonly Random Rnd = new();
    private readonly ILogger<RetryEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the RetryEngine class.
    /// </summary>
    /// <param name="logger">An instance of ILogger used to log retry-related events.</param>
    public RetryEngine(ILogger<RetryEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Runs a task with a specified number of attempts and exponential backoff.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the task.</typeparam>
    /// <param name="taskFactory">A function that returns the task to be executed.</param>
    /// <param name="attempts">The maximum number of attempts to execute the task. Default is 3.</param>
    /// <param name="when">An optional predicate that determines if the task should be retried based on the exception that occurred.</param>
    /// <returns>The result of the task if it succeeds within the specified number of attempts, otherwise an exception is thrown.</returns>
    public async Task<T> RunWithRetry<T>(
        Func<int, Task<T>> taskFactory,
        int attempts = 3,
        Predicate<Exception>? when = null,
        Action<Exception>? onError = null)
    {
        for (var i = 1; i <= attempts; i++)
        {
            try
            {
                _logger.LogDebug("Starting a job with retry... Attempt: {Attempt} (Starts from 1)", i);
                var response = await taskFactory(i);
                return response;
            }
            catch (Exception e)
            {
                onError?.Invoke(e);
                if (when != null)
                {
                    var shouldRetry = when.Invoke(e);
                    if (!shouldRetry)
                    {
                        _logger.LogWarning(e, $"A task that was asked to retry failed. But from the given condition is false, we gave up retry.");
                        throw;
                    }
                }

                if (i >= attempts)
                {
                    _logger.LogWarning(e,
                        "A task that was asked to retry failed. Maximum attempts {Max} already reached. We have to crash it", attempts);
                    throw;
                }

                _logger.LogCritical(e,
                    "A task that was asked to retry failed. Will retry soon. Current attempt is {Current}. maximum attempts is {Max}", i, attempts);

                await Task.Delay(ExponentialBackoffTimeSlot(i) * 1000);
            }
        }

        throw new InvalidOperationException("Code shall not reach here.");
    }

    /// <summary>
    /// Calculates the time to wait for the next retry attempt using an exponential backoff algorithm.
    /// </summary>
    /// <param name="time">The current attempt number.</param>
    /// <returns>The time to wait in seconds before the next retry attempt.</returns>
    private static int ExponentialBackoffTimeSlot(int time)
    {
        var max = (int)Math.Pow(2, time);
        return Rnd.Next(0, max);
    }
}
