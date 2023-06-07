# Aiursoft Cannon

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.cn/aiursoft/canon/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.cn/aiursoft/canon/badges/master/pipeline.svg)](https://gitlab.aiursoft.cn/aiursoft/canon/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.cn/aiursoft/canon/badges/master/coverage.svg)](https://gitlab.aiursoft.cn/aiursoft/canon/-/pipelines)
[![NuGet version (Aiursoft.Scanner)](https://img.shields.io/nuget/v/Aiursoft.Canon.svg)](https://www.nuget.org/packages/Aiursoft.Canon/)

Aiursoft Cannon is used to implement dependency-based Fire and Forget for .NET projects, which means starting a heavy task without waiting for it to complete or caring about its success, and continuing with subsequent logic.

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

## How to use Aiursoft.Canon

First, install `Aiursoft.Canon` to your ASP.NET Core project from nuget.org:

```bash
dotnet add package Aiursoft.Canon
```

Add the service to your `IServiceCollection` in `StartUp.cs`:

```csharp
services.AddCanon();
```

Then, you can inject `CanonService` to your controller:

```csharp
public class YourController : Controller
{
    private readonly CanonService _cannonService;

    public OAuthController(
        CanonService canonService)
    {
        _canonService = canonService;
    }
}
```

And then, you can fire and forget your task like this:

```csharp
// Send him an confirmation email here:
_canonService.FireAsync<EmailSender>(async (sender) =>
{
    await sender.SendAsync(); // Which may be slow. The service 'EmailSender' will be available to use.
});
```

That's it! Easy, right?

---------

## Advanced usage

You can also put all your tasks to a task queue, and run those tasks with a limit of concurrency:

Inject CanonQueue first:

```csharp
private readonly CanonQueue _cannonQueue;

public DemoController(CanonQueue cannonQueue)
{
    _cannonQueue = cannonQueue;
}
```

```csharp
foreach (var user in users)
{
    _cannonQueue.QueueWithDependency<EmailSender>(async (sender) =>
    {
        await sender.SendAsync(user); // Which may be slow. The service 'EmailSender' will be available to use.
    });
}
```

That is far better than this:

```csharp
var tasks = new List<Task>();
foreach (var user in users)
{
    tasks.Add(Task.Run(() => sender.SendAsync(user)));
}
await Task.WhenAll(tasks);
```

Don't do that anymore! It may start too many tasks and block your remote service like email sender.

Now you can control the concurrency of your tasks. For example, you can start 16 tasks at the same time:

```csharp
_cannonQueue.QueueWithDependency<EmailSender>(async (sender) =>
{
    await sender.SendAsync(user); // Which may be slow. The service 'EmailSender' will be available to use.
}, startTheEngine: false);// This won't start any task. We will await it manually.

await _cannonQueue.RunTasksInQueue(16); // Start the engine with 16 concurrency and wait for all tasks to complete.
```

That helps you to avoid blocking your Email sender or database with too many tasks.
