using Aiursoft.Scanner.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Aiursoft.Canon;

public class WatchService(ILogger<WatchService> logger) : ITransientDependency
{
    public async Task<TimeSpan> RunWithWatchAsync(Func<Task> taskFactory, [CallerMemberName] string? caller = null)
    {
        logger.LogTrace("Started a task '{TaskName}' with watch...", caller);
        var watch = new Stopwatch();
        watch.Start();
        await taskFactory();
        watch.Stop();
        logger.LogTrace("Finished a task '{TaskName}' with watch. Used {Used} seconds", caller, watch.Elapsed.TotalSeconds);
        return watch.Elapsed;
    }

    public async Task<(TimeSpan time, T result)> RunWithWatchAsync<T>(Func<Task<T>> taskFactory, [CallerMemberName] string? caller = null)
    {
        logger.LogTrace("Started a task '{TaskName}' with watch...", caller);
        var watch = new Stopwatch();
        watch.Start();
        var result = await taskFactory();
        watch.Stop();
        logger.LogTrace("Finished a task '{TaskName}' with watch. Used {Used} seconds", caller, watch.Elapsed.TotalSeconds);
        return (watch.Elapsed, result);
    }
}