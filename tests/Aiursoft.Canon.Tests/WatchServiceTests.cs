using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.Canon.Tests;

[TestClass]
public class WatchServiceTests
{
	private IServiceProvider? _serviceProvider;

	[TestInitialize]
	public void Init()
	{
		_serviceProvider = new ServiceCollection()
			.AddLogging()
            .AddTransient<DemoService>()
            .AddDbContext<SqlDbContext>()
			.AddTaskCanon()
			.BuildServiceProvider();
	}

	[TestCleanup]
	public void Clean()
	{
		DemoService.DoneTimes = 0;
		DemoService.DoneAsync = false;
	}

	[TestMethod]
	public async Task TestWatchMultipleTimes()
	{
		await TestWatch();
		await Task.Delay(100);
		Clean();
		await TestWatch();
	}

	[TestMethod]
	public async Task TestWatch()
	{
		var watch = _serviceProvider!.GetRequiredService<WatchService>();
		var demo = _serviceProvider!.GetRequiredService<DemoService>();
        await watch.RunWithWatchAsync(demo.DoSomethingSlowAsync);

		Assert.IsTrue(DemoService.DoneAsync);
	}
}
