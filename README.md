# Aiursoft Canon

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.com/aiursoft/canon/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.com/aiursoft/canon/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/canon/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.com/aiursoft/canon/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/canon/-/pipelines)
[![NuGet version (Aiursoft.Canon)](https://img.shields.io/nuget/v/Aiursoft.Canon.svg)](https://www.nuget.org/packages/Aiursoft.Canon/)
[![Man hours](https://manhours.aiursoft.com/r/gitlab.aiursoft.com/aiursoft/canon.svg)](https://manhours.aiursoft.com/r/gitlab.aiursoft.com/aiursoft/canon.html)

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

---

## Background Job Framework

Beyond fire-and-forget, Canon ships three layered packages that give you a full observable background job system — with registry, admin-triggerable jobs, status tracking, and recurring schedules.

```
Aiursoft.Canon                     (fire-and-forget primitives)
  └─ Aiursoft.Canon.ServiceTaskQueue  (named queues + task status tracking)
       └─ Aiursoft.Canon.BackgroundJobs  (job registry + IBackgroundJob contract)
            └─ Aiursoft.Canon.ScheduledTasks  (recurring timers)
```

Install the packages you need:

```bash
dotnet add package Aiursoft.Canon.ServiceTaskQueue
dotnet add package Aiursoft.Canon.BackgroundJobs
dotnet add package Aiursoft.Canon.ScheduledTasks
```

---

### Layer 1 — `Aiursoft.Canon.ServiceTaskQueue`

`ServiceTaskQueue` is a named, per-queue-serial task engine with full status tracking. Unlike `CanonQueue` (which is parallel fire-and-forget), tasks within the **same queue name** run **one at a time**, while tasks in different queues run in parallel.

Register the engine in `Startup.cs`:

```csharp
using Aiursoft.Canon.TaskQueue;

services.AddTaskQueueEngine();   // registers ServiceTaskQueue + TaskQueueWorkerService
```

Enqueue tasks with DI:

```csharp
public class MyService(ServiceTaskQueue taskQueue)
{
    public void SendReport(int userId)
    {
        taskQueue.QueueWithDependency<ReportSender>(
            queueName: "reports",       // tasks in "reports" run serially
            taskName: "Weekly report",
            task: sender => sender.SendAsync(userId));
    }
}
```

Observe task state at any time:

```csharp
// All tasks ever recorded (most-recent first)
IEnumerable<TaskExecutionInfo> all = taskQueue.GetAllTasks();

// Tasks still waiting to start
IEnumerable<TaskExecutionInfo> pending = taskQueue.GetPendingTasks();

// Tasks currently running
IEnumerable<TaskExecutionInfo> running = taskQueue.GetProcessingTasks();

// Tasks that completed within the last hour
IEnumerable<TaskExecutionInfo> recent = taskQueue.GetRecentCompletedTasks(TimeSpan.FromHours(1));

// Cancel a task that hasn't started yet
bool wasCancelled = taskQueue.CancelTask(taskId);
```

Each `TaskExecutionInfo` carries:

| Property | Type | Meaning |
|---|---|---|
| `TaskId` | `Guid` | Unique task identifier |
| `QueueName` | `string` | Which named queue this task belongs to |
| `TaskName` | `string` | Human-readable label |
| `Status` | `TaskExecutionStatus` | `Pending / Processing / Success / Failed / Cancelled` |
| `TriggerSource` | `TaskTriggerSource` | `Unknown / Manual / Scheduled` |
| `QueuedAt` | `DateTime` | When the task was enqueued |
| `StartedAt` | `DateTime?` | When execution began |
| `CompletedAt` | `DateTime?` | When execution finished |
| `ErrorMessage` | `string?` | Exception text if the task failed |

---

### Layer 2 — `Aiursoft.Canon.BackgroundJobs`

This layer introduces the `IBackgroundJob` contract and a **global registry** so that an admin page (or any code) can discover, describe, and instantly trigger any pre-registered job by type — without knowing the implementation details.

#### Step 1 — Implement `IBackgroundJob`

```csharp
using Aiursoft.Canon.BackgroundJobs;

public class SendWeeklyDigestJob : IBackgroundJob
{
    // Human-readable metadata shown in management UIs
    public string Name        => "Weekly Digest";
    public string Description => "Sends the weekly digest email to all subscribers.";

    private readonly MailService _mail;

    public SendWeeklyDigestJob(MailService mail) => _mail = mail;

    public async Task ExecuteAsync()
    {
        await _mail.SendDigestAsync();
    }
}
```

Key design rules:

- A job receives its dependencies through **constructor injection** (normal DI — scoped services are fine).
- When triggered from the admin UI, the job runs with all its **default behaviour** (no extra parameters needed).
- `Name` and `Description` are pure metadata; they are read lazily when the registry is queried.

#### Step 2 — Register jobs

```csharp
using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.Canon.TaskQueue;

// Required: the underlying task engine
services.AddTaskQueueEngine();

// Register each job — order is preserved in the registry
services.RegisterBackgroundJob<SendWeeklyDigestJob>();
services.RegisterBackgroundJob<CleanupOrphanFilesJob>();
```

`RegisterBackgroundJob<TJob>()` registers the job as `Transient` (so DI creates a fresh instance per run) and records a `RegisteredJob` descriptor in the DI container.

#### Step 3 — Trigger jobs

Inject `BackgroundJobRegistry` wherever you need to fire a job:

```csharp
public class AdminController(BackgroundJobRegistry registry) : Controller
{
    // Trigger by generic type — compile-time safe
    [HttpPost]
    public IActionResult RunDigest()
    {
        registry.TriggerNow<SendWeeklyDigestJob>();
        return RedirectToAction("Jobs");
    }

    // Trigger by runtime type (useful in generic admin endpoints)
    [HttpPost]
    public IActionResult TriggerJob(string typeName)
    {
        registry.TriggerNow(typeName);   // throws if not registered
        return RedirectToAction("Jobs");
    }
}
```

List all registered jobs to build a management UI:

```csharp
IReadOnlyList<RegisteredJob> jobs = registry.GetAll();

foreach (var job in jobs)
{
    Console.WriteLine($"{job.Name}: {job.Description}");
}
```

---

### Layer 3 — `Aiursoft.Canon.ScheduledTasks`

Once a job is registered in the `BackgroundJobRegistry`, you can attach a **recurring schedule** to it with a single call. The `JobSchedulerService` hosted service fires each timer and enqueues the job with `TriggerSource.Scheduled`.

```csharp
using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.Canon.ScheduledTasks;
using Aiursoft.Canon.TaskQueue;

services.AddTaskQueueEngine();
services.AddScheduledTaskEngine();   // registers the timer-based hosted service

// Register the job and capture the descriptor
var digestJob = services.RegisterBackgroundJob<SendWeeklyDigestJob>();
var cleanupJob = services.RegisterBackgroundJob<CleanupOrphanFilesJob>();

// Schedule: period + optional start delay
services.RegisterScheduledTask(
    registration: digestJob,
    period:       TimeSpan.FromDays(7),
    startDelay:   TimeSpan.FromMinutes(1));   // wait 1 min after app start

services.RegisterScheduledTask(
    registration: cleanupJob,
    period:       TimeSpan.FromHours(6),
    startDelay:   TimeSpan.FromMinutes(5));
```

Default values: `period = 3 hours`, `startDelay = 3 minutes` if not specified.

At runtime the scheduler logs each enqueue:

```
Job Scheduler: scheduling SendWeeklyDigestJob every 7.00:00:00 (first run after 00:01:00)
Job Scheduler: enqueued scheduled run of SendWeeklyDigestJob (taskId=3f2a…)
```

The resulting `TaskExecutionInfo` will have `TriggerSource = Scheduled`, so your management UI can distinguish scheduled runs from manual ones.

---

### Full startup example

```csharp
using Aiursoft.Canon.TaskQueue;
using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.Canon.ScheduledTasks;

// ── Infrastructure ──────────────────────────────────────────────
services.AddTaskQueueEngine();     // ServiceTaskQueue + TaskQueueWorkerService
services.AddScheduledTaskEngine(); // JobSchedulerService

// ── Jobs ────────────────────────────────────────────────────────
var digestJob  = services.RegisterBackgroundJob<SendWeeklyDigestJob>();
var cleanupJob = services.RegisterBackgroundJob<CleanupOrphanFilesJob>();

// ── Schedules ───────────────────────────────────────────────────
services.RegisterScheduledTask(digestJob,  period: TimeSpan.FromDays(7),  startDelay: TimeSpan.FromMinutes(1));
services.RegisterScheduledTask(cleanupJob, period: TimeSpan.FromHours(6), startDelay: TimeSpan.FromMinutes(5));
```


