using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Aiursoft.Canon.Tests;

[TestClass]
public class CanonQueueTests
{
    private IServiceProvider? _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        var dbContext = new SqlDbContext();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddTaskCanon()
            .AddScoped<DemoController>()
            .AddTransient<DemoService>()
            .AddDbContext<SqlDbContext>()
            .BuildServiceProvider();
    }

    [TestCleanup]
    public void Clean()
    {
        DemoService.DoneTimes = 0;
        DemoService.Done = false;
        DemoService.DoneAsync = false;
    }

    [TestMethod]
    public async Task TestCanonQueueMultipleTimes()
    {
        await TestCanonQueue();
        await Task.Delay(100);
        Clean();
        await TestCanonQueue();
    }

    [TestMethod]
    public async Task TestCanonQueue()
    {
        var controller = _serviceProvider?.GetRequiredService<DemoController>();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        controller?.QueueActionAsync();
        stopwatch.Stop();
        Assert.IsLessThan(1000, stopwatch.ElapsedMilliseconds, "Demo action should finish very fast.");
        Assert.IsFalse(DemoService.DoneAsync, "When demo action finished, work is not over yet.");

        stopwatch = new Stopwatch();
        stopwatch.Start();
        while (DemoService.DoneTimes != 32)
        {
            await Task.Delay(20);
            Console.WriteLine(
                $"Waited for {stopwatch.Elapsed}. And {DemoService.DoneTimes} tasks are finished.");
        }
        stopwatch.Stop();
        Assert.IsLessThan(5000, stopwatch.ElapsedMilliseconds, "All actions should finish in 5 seconds.");
    }
}
