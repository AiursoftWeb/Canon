using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.Canon.ScheduledTasks;
using Aiursoft.Canon.TaskQueue;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Canon.Tests;

// ── Fixtures ─────────────────────────────────────────────────────────────────

internal class CounterJob : IBackgroundJob
{
    public string Name        => "Counter Job";
    public string Description => "Counts things.";
    public Task ExecuteAsync() => Task.CompletedTask;
}

internal class CleanupJob : IBackgroundJob
{
    public string Name        => "Cleanup Job";
    public string Description => "Cleans up things.";
    public Task ExecuteAsync() => Task.CompletedTask;
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[TestClass]
public class ScheduledTaskTests
{
    private static (IServiceProvider Provider, RegisteredJob Registration) BuildWithSchedule(
        TimeSpan? period = null,
        TimeSpan? startDelay = null)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<ServiceTaskQueue>();
        var reg = services.RegisterBackgroundJob<CounterJob>();
        services.RegisterScheduledTask(reg, period, startDelay);
        return (services.BuildServiceProvider(), reg);
    }

    // ── Registration storage ──────────────────────────────────────────────────

    [TestMethod]
    public void RegisterScheduledTask_WithExplicitValues_StoredCorrectly()
    {
        var (provider, _) = BuildWithSchedule(
            period:     TimeSpan.FromHours(6),
            startDelay: TimeSpan.FromMinutes(10));

        var schedules = provider.GetServices<ScheduledTaskRegistration>().ToList();

        Assert.AreEqual(1, schedules.Count);
        Assert.AreEqual(typeof(CounterJob),         schedules[0].JobType);
        Assert.AreEqual(TimeSpan.FromHours(6),      schedules[0].Period);
        Assert.AreEqual(TimeSpan.FromMinutes(10),   schedules[0].StartDelay);
    }

    [TestMethod]
    public void RegisterScheduledTask_WithNullPeriod_DefaultsToThreeHours()
    {
        var (provider, _) = BuildWithSchedule(period: null, startDelay: TimeSpan.FromMinutes(1));

        var schedule = provider.GetServices<ScheduledTaskRegistration>().Single();

        Assert.AreEqual(TimeSpan.FromHours(3), schedule.Period);
    }

    [TestMethod]
    public void RegisterScheduledTask_WithNullStartDelay_DefaultsToThreeMinutes()
    {
        var (provider, _) = BuildWithSchedule(period: TimeSpan.FromHours(1), startDelay: null);

        var schedule = provider.GetServices<ScheduledTaskRegistration>().Single();

        Assert.AreEqual(TimeSpan.FromMinutes(3), schedule.StartDelay);
    }

    [TestMethod]
    public void RegisterScheduledTask_WithBothNulls_UsesBothDefaults()
    {
        var (provider, _) = BuildWithSchedule(period: null, startDelay: null);

        var schedule = provider.GetServices<ScheduledTaskRegistration>().Single();

        Assert.AreEqual(TimeSpan.FromHours(3),   schedule.Period);
        Assert.AreEqual(TimeSpan.FromMinutes(3), schedule.StartDelay);
    }

    [TestMethod]
    public void RegisterScheduledTask_MultipleJobs_AllRegistered()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<ServiceTaskQueue>();

        var counterReg = services.RegisterBackgroundJob<CounterJob>();
        var cleanupReg = services.RegisterBackgroundJob<CleanupJob>();

        services.RegisterScheduledTask(counterReg, period: TimeSpan.FromHours(1),  startDelay: TimeSpan.FromMinutes(1));
        services.RegisterScheduledTask(cleanupReg, period: TimeSpan.FromHours(6),  startDelay: TimeSpan.FromMinutes(5));

        var provider  = services.BuildServiceProvider();
        var schedules = provider.GetServices<ScheduledTaskRegistration>().ToList();

        Assert.AreEqual(2, schedules.Count);
        Assert.IsTrue(schedules.Any(s => s.JobType == typeof(CounterJob)));
        Assert.IsTrue(schedules.Any(s => s.JobType == typeof(CleanupJob)));
    }

    [TestMethod]
    public void RegisterScheduledTask_JobTypeMatchesRegistration()
    {
        var (provider, reg) = BuildWithSchedule(TimeSpan.FromDays(1), TimeSpan.FromSeconds(30));

        var schedule = provider.GetServices<ScheduledTaskRegistration>().Single();

        Assert.AreEqual(reg.JobType, schedule.JobType);
    }

    // ── Null registration guard ───────────────────────────────────────────────

    [TestMethod]
    public void RegisterScheduledTask_NullRegistration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        try
        {
            services.RegisterScheduledTask(null!);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException) { }
    }
}
