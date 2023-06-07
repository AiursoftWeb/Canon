using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Aiursoft.Canon.Tests;

[TestClass]
public class CanonServiceTests
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
        DemoService.Done = false;
        DemoService.DoneAsync = false;
    }

    [TestMethod]
    public async Task TestCannon()
    {
        var controller = _serviceProvider?.GetRequiredService<DemoController>();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        controller?.DemoAction();
        stopwatch.Stop();
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, "Demo action should finish very fast.");
        Assert.AreEqual(false, DemoService.Done, "When demo action finished, work is not over yet.");
        await Task.Delay(2000);
        Assert.AreEqual(true, DemoService.Done, "After a while, the async job is done.");
    }

    [TestMethod]
    public async Task TestCannonAsync()
    {
        var controller = _serviceProvider?.GetRequiredService<DemoController>();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        controller?.DemoActionAsync();
        stopwatch.Stop();
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, "Demo action should finish very fast.");
        Assert.AreEqual(false, DemoService.DoneAsync, "When demo action finished, work is not over yet.");
        await Task.Delay(600);
        Assert.AreEqual(true, DemoService.DoneAsync, "After a while, the async job is done.");
    }

}
