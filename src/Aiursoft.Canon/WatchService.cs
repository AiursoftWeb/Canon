using Aiursoft.Scanner.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Aiursoft.Canon;

public class WatchService : ITransientDependency
{
    private readonly ILogger<WatchService> _logger;

    public WatchService(ILogger<WatchService> logger)
    {
        _logger = logger;
    }

    public async Task<TimeSpan> RunWithWatchAsync(Func<Task> taskFactory, [CallerMemberName]string? caller = null)
    {
        _logger.LogTrace("Started a task '{TaskName}' with watch...", caller);
        var watch = new Stopwatch();
        watch.Start();
        await taskFactory();
        watch.Stop();
        _logger.LogTrace("Finished a task '{TaskName}' with watch. Used {Used} seconds", caller, watch.Elapsed.TotalSeconds);
        return watch.Elapsed;
    }
}