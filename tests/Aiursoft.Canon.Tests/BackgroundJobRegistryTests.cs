using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.Canon.TaskQueue;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon.Tests;

// ── Fixtures ─────────────────────────────────────────────────────────────────

internal class HelloJob : IBackgroundJob
{
    public string Name        => "Hello Job";
    public string Description => "Says hello.";
    public Task ExecuteAsync() => Task.CompletedTask;
}

internal class GoodbyeJob : IBackgroundJob
{
    public string Name        => "Goodbye Job";
    public string Description => "Says goodbye.";
    public Task ExecuteAsync() => Task.CompletedTask;
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[TestClass]
public class BackgroundJobRegistryTests
{
    private IServiceProvider BuildProvider(Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection().AddLogging();

        // Register ServiceTaskQueue WITHOUT the worker so enqueued tasks stay Pending.
        services.AddSingleton<ServiceTaskQueue>();

        services.RegisterBackgroundJob<HelloJob>();
        services.RegisterBackgroundJob<GoodbyeJob>();

        extra?.Invoke(services);
        return services.BuildServiceProvider();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAll_WithTwoRegisteredJobs_ReturnsBothDescriptors()
    {
        var registry = BuildProvider().GetRequiredService<BackgroundJobRegistry>();

        var all = registry.GetAll();

        Assert.AreEqual(2, all.Count);
    }

    [TestMethod]
    public void GetAll_JobDescriptors_HaveCorrectNameAndDescription()
    {
        var registry = BuildProvider().GetRequiredService<BackgroundJobRegistry>();

        var all = registry.GetAll();
        var hello = all.First(j => j.JobType == typeof(HelloJob));

        Assert.AreEqual("Hello Job",  hello.Name);
        Assert.AreEqual("Says hello.", hello.Description);
    }

    // ── FindByType ────────────────────────────────────────────────────────────

    [TestMethod]
    public void FindByType_WithRegisteredType_ReturnsDescriptor()
    {
        var registry = BuildProvider().GetRequiredService<BackgroundJobRegistry>();

        var result = registry.FindByType(typeof(HelloJob));

        Assert.IsNotNull(result);
        Assert.AreEqual(typeof(HelloJob), result.JobType);
    }

    [TestMethod]
    public void FindByType_WithUnregisteredType_ReturnsNull()
    {
        var registry = BuildProvider().GetRequiredService<BackgroundJobRegistry>();

        var result = registry.FindByType(typeof(BackgroundJobRegistryTests)); // not a job

        Assert.IsNull(result);
    }

    // ── FindByTypeName ────────────────────────────────────────────────────────

    [TestMethod]
    public void FindByTypeName_WithRegisteredName_ReturnsDescriptor()
    {
        var registry = BuildProvider().GetRequiredService<BackgroundJobRegistry>();

        var result = registry.FindByTypeName(nameof(GoodbyeJob));

        Assert.IsNotNull(result);
        Assert.AreEqual(typeof(GoodbyeJob), result.JobType);
    }

    [TestMethod]
    public void FindByTypeName_WithUnknownName_ReturnsNull()
    {
        var registry = BuildProvider().GetRequiredService<BackgroundJobRegistry>();

        var result = registry.FindByTypeName("NonExistentJob");

        Assert.IsNull(result);
    }

    // ── TriggerNow ────────────────────────────────────────────────────────────

    [TestMethod]
    public void TriggerNow_Generic_ReturnsPendingTaskId()
    {
        var provider = BuildProvider();
        var registry  = provider.GetRequiredService<BackgroundJobRegistry>();
        var queue     = provider.GetRequiredService<ServiceTaskQueue>();

        var taskId = registry.TriggerNow<HelloJob>();

        var task = queue.GetAllTasks().FirstOrDefault(t => t.TaskId == taskId);
        Assert.IsNotNull(task, "Triggered task should appear in the queue.");
    }

    [TestMethod]
    public void TriggerNow_Generic_TaskHasManualTriggerSource()
    {
        var provider = BuildProvider();
        var registry  = provider.GetRequiredService<BackgroundJobRegistry>();
        var queue     = provider.GetRequiredService<ServiceTaskQueue>();

        var taskId = registry.TriggerNow<HelloJob>();

        var task = queue.GetAllTasks().First(t => t.TaskId == taskId);
        Assert.AreEqual(TaskTriggerSource.Manual, task.TriggerSource);
    }

    [TestMethod]
    public void TriggerNow_Generic_TaskQueueNameEqualsJobTypeName()
    {
        var provider = BuildProvider();
        var registry  = provider.GetRequiredService<BackgroundJobRegistry>();
        var queue     = provider.GetRequiredService<ServiceTaskQueue>();

        var taskId = registry.TriggerNow<HelloJob>();

        var task = queue.GetAllTasks().First(t => t.TaskId == taskId);
        Assert.AreEqual(nameof(HelloJob), task.QueueName);
    }

    [TestMethod]
    public void TriggerNow_ByTypeName_EnqueuesTask()
    {
        var provider = BuildProvider();
        var registry  = provider.GetRequiredService<BackgroundJobRegistry>();
        var queue     = provider.GetRequiredService<ServiceTaskQueue>();

        var taskId = registry.TriggerNow(nameof(GoodbyeJob));

        var task = queue.GetAllTasks().FirstOrDefault(t => t.TaskId == taskId);
        Assert.IsNotNull(task);
        Assert.AreEqual(TaskTriggerSource.Manual, task.TriggerSource);
    }

    [TestMethod]
    public void TriggerNow_WithScheduledSource_TaskHasScheduledTriggerSource()
    {
        var provider = BuildProvider();
        var registry  = provider.GetRequiredService<BackgroundJobRegistry>();
        var queue     = provider.GetRequiredService<ServiceTaskQueue>();

        var taskId = registry.TriggerNow(typeof(HelloJob), TaskTriggerSource.Scheduled);

        var task = queue.GetAllTasks().First(t => t.TaskId == taskId);
        Assert.AreEqual(TaskTriggerSource.Scheduled, task.TriggerSource);
    }

    [TestMethod]
    public void TriggerNow_ByType_UnregisteredType_Throws()
    {
        var registry = BuildProvider().GetRequiredService<BackgroundJobRegistry>();

        try
        {
            registry.TriggerNow(typeof(BackgroundJobRegistryTests));
            Assert.Fail("Expected InvalidOperationException was not thrown.");
        }
        catch (InvalidOperationException) { }
    }

    [TestMethod]
    public void TriggerNow_ByTypeName_UnregisteredName_Throws()
    {
        var registry = BuildProvider().GetRequiredService<BackgroundJobRegistry>();

        try
        {
            registry.TriggerNow("NonExistentJob");
            Assert.Fail("Expected InvalidOperationException was not thrown.");
        }
        catch (InvalidOperationException) { }
    }

    // ── ServiceTaskQueue cancel integration ───────────────────────────────────

    [TestMethod]
    public void CancelTask_PendingTriggeredJob_ReturnsTrueAndStatusIsCancelled()
    {
        var provider = BuildProvider();
        var registry  = provider.GetRequiredService<BackgroundJobRegistry>();
        var queue     = provider.GetRequiredService<ServiceTaskQueue>();

        var taskId = registry.TriggerNow<HelloJob>();
        var cancelled = queue.CancelTask(taskId);

        Assert.IsTrue(cancelled);
        var task = queue.GetAllTasks().First(t => t.TaskId == taskId);
        Assert.AreEqual(TaskExecutionStatus.Cancelled, task.Status);
    }
}
