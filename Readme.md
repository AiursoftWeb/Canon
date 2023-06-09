# Aiursoft Canon

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.cn/aiursoft/canon/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.cn/aiursoft/canon/badges/master/pipeline.svg)](https://gitlab.aiursoft.cn/aiursoft/canon/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.cn/aiursoft/canon/badges/master/coverage.svg)](https://gitlab.aiursoft.cn/aiursoft/canon/-/pipelines)
[![NuGet version (Aiursoft.Canon)](https://img.shields.io/nuget/v/Aiursoft.Canon.svg)](https://www.nuget.org/packages/Aiursoft.Canon/)

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

## How to use Aiursoft.CanonQueue

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

That's it! Easy, right?

---------

## How to use Aiursoft.CanonPool

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
    _canonPool.RegisterNewTaskToPool(async (sender) =>
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

Now you can control the concurrency of your tasks. For example, you can start 6 tasks at the same time:

```csharp
await _canonQueue.RunTasksInQueue(6); // Start the engine with 16 concurrency and wait for all tasks to complete.
```

That helps you to avoid blocking your Email sender or database with too many tasks.
