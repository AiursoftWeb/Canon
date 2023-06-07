using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon;

/// <summary>
/// A service that provides a way to execute actions and tasks with dependency injection and logging.
/// </summary>
public class CanonService
{
    private readonly ILogger<CanonService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CanonService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scopeFactory">The service scope factory.</param>
    public CanonService(
        ILogger<CanonService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Executes an action in a new task with the specified dependency.
    /// </summary>
    /// <typeparam name="T">The type of the dependency.</typeparam>
    /// <param name="bullet">The action to execute.</param>
    public void Fire<T>(Action<T> bullet) where T : class
    {
        _logger.LogInformation("Canon Service started a new action.");
        Task.Run(() =>
        {
            using var scope = _scopeFactory.CreateScope();
            var dependency = scope.ServiceProvider.GetRequiredService<T>();
            try
            {
                bullet(dependency);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Cannon crashed inside a task!");
            }
            finally
            {
                (dependency as IDisposable)?.Dispose();
            }
        });
    }

    /// <summary>
    /// Executes an asynchronous function in a new task with the specified dependency.
    /// </summary>
    /// <typeparam name="T">The type of the dependency.</typeparam>
    /// <param name="bullet">The asynchronous function to execute.</param>
    public void FireAsync<T>(Func<T, Task> bullet) where T : class
    {
        _logger.LogInformation("Fired a new async action.");
        Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var dependency = scope.ServiceProvider.GetRequiredService<T>();
            try
            {
                await bullet(dependency);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cannon crashed inside a task!");
            }
            finally
            {
                (dependency as IDisposable)?.Dispose();
            }
        });
    }
}
