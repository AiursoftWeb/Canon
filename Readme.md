# Aiursoft Canon

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.cn/aiursoft/canon/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.cn/aiursoft/canon/badges/master/pipeline.svg)](https://gitlab.aiursoft.cn/aiursoft/canon/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.cn/aiursoft/canon/badges/master/coverage.svg)](https://gitlab.aiursoft.cn/aiursoft/canon/-/pipelines)
[![NuGet version (Aiursoft.Canon)](https://img.shields.io/nuget/v/Aiursoft.Canon.svg)](https://www.nuget.org/packages/Aiursoft.Canon/)
[![ManHours](https://manhours.aiursoft.cn/gitlab/gitlab.aiursoft.cn/aiursoft/canon)](https://gitlab.aiursoft.cn/aiursoft/canon/-/commits/master?ref_type=heads)

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

## Installation

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
// An easier to use Cache service. Allow you to execute some code with a key to cache it.
services.AddTransient<CacheService>();

// A transient service to retry a task with limited times.
services.AddTransient<RetryEngine>();

// A transient service to replace 'Task.WhenAll()'. Start all tasks with limited concurrency.
services.AddTransient<CanonPool>();

// Simple Fire and forget service that runs immediately. (No concurrency limitation)
services.AddSingleton<CanonService>();

// Application singleton background job queue. (Default task concurrency is 8)
services.AddSingleton<CanonQueue>();

// A watch service to measure how much time a task used.
services.AddTransient<WatchService>();
```

### How to use Aiursoft.CanonQueue

Then, you can inject `CanonService` to your controller. And now, you can fire and forget your task like this:

```csharp
public class YourController : Controller
{
    private readonly CanonQueue _canonQueue;

    public OAuthController(CanonQueue canonQueue)
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

### How to use Aiursoft.CanonPool

You can also put all your tasks to a task queue, and run those tasks with a limit of concurrency:

Inject CanonPool first:

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

```csharp
foreach (var user in users)
{
    _canonPool.RegisterNewTaskToPool(async () =>
    {
        await sender.SendAsync(user); // Which may be slow.
    });
}

await _canonPool.RunAllTasksInPoolAsync(); // Execute tasks in pool, running tasks should be max at 8.
```

That is far better than this:

```csharp
var tasks = new List<Task>();
foreach (var user in users)
{
    tasks.Add(Task.Run(() => sender.SendAsync(user)));
}
await Task.WhenAll(tasks); // It may start too many tasks and block your remote service like email sender.
```

Now you can control the concurrency of your tasks. For example, you can start 16 tasks at the same time:

```csharp
await _canonQueue.RunTasksInQueue(16); // Start the engine with 16 concurrency and wait for all tasks to complete.
```

That helps you to avoid blocking your Email sender or database with too many tasks.
