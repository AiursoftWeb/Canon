# Aiursoft Canon

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.com/aiursoft/canon/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.com/aiursoft/canon/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/canon/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.com/aiursoft/canon/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/canon/-/pipelines)
[![NuGet version (Aiursoft.Canon)](https://img.shields.io/nuget/v/Aiursoft.Canon.svg)](https://www.nuget.org/packages/Aiursoft.Canon/)
[![ManHours](https://manhours.aiursoft.cn/r/gitlab.aiursoft.com/aiursoft/canon.svg)](https://gitlab.aiursoft.com/aiursoft/canon/-/commits/master?ref_type=heads)

Aiursoft Canon is used to implement dependency-based Fire and Forget for .NET projects, which means starting a heavy task without waiting for it to complete or caring about its success, and continuing with subsequent logic.

This is very useful in many scenarios to avoid blocking, such as when sending emails.

## Why this project

The traditional way to fire and forget in C# is:

```csharp
_ = Task.Run(() =>
{
    // Do something heavy
});
```

However, if your task depends on something like Entity Framework, it's hard to control it's life cycle.

## How to install

First, install `Aiursoft.Canon` to your ASP.NET Core project from [nuget.org](https://www.nuget.org/packages/Aiursoft.Canon/):

```bash
dotnet add package Aiursoft.Canon
```

Add the service to your [`IServiceCollection`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection) in `StartUp.cs`:

```csharp
using Aiursoft.Canon;

services.AddTaskCanon();
```

Your project will get:

```csharp
// A retry engine.
services.AddTransient<RetryEngine>();

// An easier to use Cache service.
services.AddTransient<CacheService>();

// A transient service to throw an exception if the task takes too long.
services.AddTransient<TimeoutStopper>();

// A transient service to replace 'Task.WhenAll()'.
services.AddTransient<CanonPool>();

// Simple Fire and forget service that runs immediately.
services.AddSingleton<CanonService>();

// Application singleton background job queue.
services.AddSingleton<CanonQueue>();

// A watch service to measure how much time a task used.
services.AddTransient<WatchService>();
```

---

### How to use Aiursoft.CanonQueue

Then, you can inject `CanonQueue` to your controller. And now, you can fire and forget your task like this:

```csharp
public class YourController : Controller
{
    private readonly CanonQueue _canonQueue;

    public YourController(CanonQueue canonQueue)
    {
        _canonQueue = canonQueue;
    }

    public IActionResult Send()
    {
        // Send an confirmation email here:
        _canonQueue.QueueWithDependency<EmailSender>(async (sender) =>
        {
            await sender.SendAsync(); // Which may be slow. The service 'EmailSender' will be kept alive!
        });
        
        return Ok();
    }
}
```

That's it.

-----

### How to use Aiursoft.CanonPool

You can also put all your tasks to a task queue, and run those tasks with a limit of concurrency:

Inject `CanonPool` first:

```csharp
private readonly EmailSender _sender;
private readonly CanonPool _canonPool;

public DemoController(
    EmailSender sender,
    CanonPool canonPool)
{
    _sender = sender;
    _canonPool = canonPool;
}
```

Now you can register tasks to the pool and run them concurrently with a controlled limit.

```csharp
foreach (var user in users)
{
    _canonPool.RegisterNewTaskToPool(async () =>
    {
        await _sender.SendAsync(user); // Which may be slow.
    });
}

// Execute tasks in pool, with a maximum of 8 running at the same time.
await _canonPool.RunAllTasksInPoolAsync(); 
```

This is far better than this:

```csharp
var tasks = new List<Task>();
foreach (var user in users)
{
    tasks.Add(Task.Run(() => sender.SendAsync(user)));
}
// This may start too many tasks at once and overload your remote service.
await Task.WhenAll(tasks); 
```

You can control the concurrency of your tasks. For example, you can start 16 tasks at the same time:

```csharp
await _canonPool.RunAllTasksInPoolAsync(16); // Start the engine with 16 concurrency.
```

That helps you to avoid blocking your Email sender or database with too many tasks.

-----

### How to use Aiursoft.RetryEngine

The `RetryEngine` is useful for automatically retrying an operation that might fail, such as a network request. It uses an exponential backoff strategy to wait between retries.

First, inject `RetryEngine` into your service or controller:

```csharp
private readonly RetryEngine _retry;

public MyService(RetryEngine retry)
{
    _retry = retry;
}
```

Now, you can wrap a potentially failing task with `RunWithRetry`.

```csharp
public async Task<string> CallUnreliableApiService()
{
    var result = await _retry.RunWithRetry(async (attempt) => 
    {
        // This code will be executed up to 5 times.
        Console.WriteLine($"Trying to call the API, attempt {attempt}...");
        var client = new HttpClient();
        var response = await client.GetStringAsync("https://example.com/api/data");
        return response;
    }, 
    attempts: 5, 
    when: e => e is HttpRequestException); // Only retry on HttpRequestException.

    return result;
}
```

If the API call fails with an `HttpRequestException`, the `RetryEngine` will wait for a short, random duration (which increases after each failure) and then try again. If it fails 5 times, the exception will be re-thrown.

-----

### How to use Aiursoft.CacheService

`CacheService` provides a simple way to cache results from expensive operations in memory, reducing redundant calls.

Inject `CacheService` where you need it:

```csharp
private readonly CacheService _cache;
private readonly MyDbContext _dbContext;

public MyController(
    CacheService cache,
    MyDbContext dbContext)
{
    _cache = cache;
    _dbContext = dbContext;
}
```

Use `RunWithCache` to get data. It will first try to find the data in the cache. If it's not there, it will execute your fallback function, cache the result, and then return it.

```csharp
public async Task<IActionResult> GetDashboard()
{
    // Define a unique key for this cache entry.
    var cacheKey = "dashboard-stats";

    // This data will be cached for 10 minutes.
    var stats = await _cache.RunWithCache(cacheKey, async () => 
    {
        // This logic only runs if the cache is empty or expired.
        // It's an expensive database query.
        return await _dbContext.Statistics.SumAsync(t => t.Value);
    },
    cachedMinutes: _ => TimeSpan.FromMinutes(10));

    return View(stats);
}
```

The next time `GetDashboard` is called within 10 minutes, the expensive database query will be skipped, and the result will be served directly from the in-memory cache.

-----

### How to use Aiursoft.TimeoutStopper

`TimeoutStopper` allows you to run a task with a specified time limit. If the task doesn't complete within the timeout, a `TimeoutException` is thrown. This is useful for preventing long-running operations from blocking your application indefinitely.

Inject `TimeoutStopper` into your class:

```csharp
private readonly TimeoutStopper _timeoutStopper;

public MyProcessor(TimeoutStopper timeoutStopper)
{
    _timeoutStopper = timeoutStopper;
}
```

Wrap your long-running task with `RunWithTimeout`.

```csharp
public async Task ProcessDataWithDeadline()
{
    try
    {
        // We give this operation a 5-second deadline.
        await _timeoutStopper.RunWithTimeout(async (cancellationToken) =>
        {
            // Simulate a very long-running process.
            Console.WriteLine("Starting a heavy computation...");
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            Console.WriteLine("Computation finished."); // This line will not be reached.

        }, timeoutInSeconds: 5);
    }
    catch (TimeoutException ex)
    {
        Console.WriteLine(ex.Message); // "The operation timed out after 5 seconds."
        // Handle the timeout case, e.g., log an error or notify the user.
    }
}
```

In this example, `Task.Delay` simulates a 10-second task. Because the timeout is set to 5 seconds, the `TimeoutStopper` will throw a `TimeoutException` before the task can complete.

