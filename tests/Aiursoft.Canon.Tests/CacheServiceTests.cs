using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon.Tests;

[TestClass]
public class TimeoutStopperTests
{
    private readonly ILogger<TimeoutStopper> _logger =
        LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger<TimeoutStopper>();

    [TestMethod]
    public async Task RunWithTimeout_ReturnsResult_WhenTaskCompletesWithinTimeout()
    {
        // Arrange
        var timeoutStopper = new TimeoutStopper(_logger);
        var expectedResult = "Task completed";

        // Act
        var result = await timeoutStopper.RunWithTimeout(async token =>
        {
            await Task.Delay(100, token);
            return expectedResult;
        }, 5);

        // Assert
        Assert.AreEqual(expectedResult, result);
    }

    [TestMethod]
    public async Task RunWithTimeout_ThrowsException_WhenTaskTimesOut()
    {
        // Arrange
        var timeoutStopper = new TimeoutStopper(_logger);
        var timeoutInSeconds = 1;

        // Act & Assert
        await Assert.ThrowsExactlyAsync<TimeoutException>(() =>
        {
            return timeoutStopper.RunWithTimeout(async token =>
            {
                await Task.Delay(TimeSpan.FromSeconds(timeoutInSeconds + 1), token);
                return "This should not be returned.";
            }, timeoutInSeconds);
        });
    }

    [TestMethod]
    public async Task RunWithTimeout_Action_Returns_WhenTaskCompletesWithinTimeout()
    {
        // Arrange
        var timeoutStopper = new TimeoutStopper(_logger);
        var taskCompleted = false;

        // Act
        await timeoutStopper.RunWithTimeout(async token =>
        {
            await Task.Delay(100, token);
            taskCompleted = true;
        }, 5);

        // Assert
        Assert.IsTrue(taskCompleted);
    }

    [TestMethod]
    public async Task RunWithTimeout_Action_ThrowsException_WhenTaskTimesOut()
    {
        // Arrange
        var timeoutStopper = new TimeoutStopper(_logger);
        var timeoutInSeconds = 1;

        // Act & Assert
        await Assert.ThrowsExactlyAsync<TimeoutException>(() =>
        {
            return timeoutStopper.RunWithTimeout(async token =>
            {
                await Task.Delay(TimeSpan.FromSeconds(timeoutInSeconds + 1), token);
            }, timeoutInSeconds);
        });
    }

    [TestMethod]
    public async Task RunWithTimeout_PassesCancellationToken_ToInnerTask()
    {
        // Arrange
        var timeoutStopper = new TimeoutStopper(_logger);
        var wasCanceled = new TaskCompletionSource<bool>();

        // Act
        await Assert.ThrowsExactlyAsync<TimeoutException>(() =>
        {
            return timeoutStopper.RunWithTimeout(async token =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
                catch (TaskCanceledException)
                {
                    // This is the key part: we set the TaskCompletionSource
                    // to true when the inner task's cancellation token is triggered.
                    wasCanceled.SetResult(true);
                    throw; // Rethrow to show the inner task was canceled.
                }
                // If the task completes without cancellation, this will prevent the test from passing.
                wasCanceled.SetResult(false);
            }, 1);
        });

        // Assert
        // Now we can check the state of our TaskCompletionSource outside the try-catch.
        // We use Task.WhenAny to wait for either the cancellation flag to be set
        // or a short delay to pass, preventing an indefinite wait.
        var completedTask = await Task.WhenAny(wasCanceled.Task, Task.Delay(1000));
        Assert.AreEqual(wasCanceled.Task, completedTask);
        Assert.IsTrue(wasCanceled.Task.Result); // Now we check the bool result of the completed task
    }
}

[TestClass]
public class CacheServiceTests
{
    private readonly ILogger<CacheService> _logger =
        LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger<CacheService>();

    [TestMethod]
    public async Task RunWithCache_ReturnsCachedValue_WhenCacheIsNotEmpty()
    {
        // Arrange
        var cacheKey = "TestCacheKey1";
        var cacheValue = "TestCacheValue2";
        var memoryCacheOptions = new MemoryCacheOptions();
        var memoryCache = new MemoryCache(memoryCacheOptions);
        memoryCache.Set(cacheKey, cacheValue);
        var cacheService = new CacheService(memoryCache, _logger);

        // Act
        var result = await cacheService.RunWithCache(cacheKey, () => Task.FromResult("FallbackValue"));

        // Assert
        Assert.AreEqual(cacheValue, result);
    }

    [TestMethod]
    public async Task RunWithCache_ReturnsFallbackValue_WhenCacheIsEmpty()
    {
        // Arrange
        var cacheKey = "TestCacheKey3";
        var fallbackValue = "FallbackValue4";
        var memoryCacheOptions = new MemoryCacheOptions();
        var memoryCache = new MemoryCache(memoryCacheOptions);
        var cacheService = new CacheService(memoryCache, _logger);

        // Act
        var result = await cacheService.RunWithCache(cacheKey, () => Task.FromResult(fallbackValue));

        // Assert
        Assert.AreEqual(fallbackValue, result);
    }

    [TestMethod]
    public async Task RunWithCache_ReturnsFallbackValue_WhenCacheIsExpired()
    {
        // Arrange
        var cacheKey = "TestCacheKey5";
        var cacheValue = "TestCacheValue6";
        var memoryCacheOptions = new MemoryCacheOptions();
        var memoryCache = new MemoryCache(memoryCacheOptions);
        memoryCache.Set(cacheKey, cacheValue, TimeSpan.FromMilliseconds(1));
        await Task.Delay(10);
        var cacheService = new CacheService(memoryCache, _logger);

        // Act
        var result = await cacheService.RunWithCache(cacheKey, () => Task.FromResult("FallbackValue"));

        // Assert
        Assert.AreEqual("FallbackValue", result);
    }

    [TestMethod]
    public async Task QueryCacheWithSelector_ReturnsCachedValue_WhenCacheIsNotEmpty()
    {
        // Arrange
        var cacheKey = "TestCacheKey7";
        var cacheValue = "TestCacheValue8";
        var memoryCacheOptions = new MemoryCacheOptions();
        var memoryCache = new MemoryCache(memoryCacheOptions);
        memoryCache.Set(cacheKey, cacheValue);
        var cacheService = new CacheService(memoryCache, _logger);

        // Act
        var result = await cacheService.QueryCacheWithSelector(cacheKey, () => Task.FromResult("FallbackValue"),
            value => value.Length);

        // Assert
        Assert.AreEqual(cacheValue.Length, result);
    }

    [TestMethod]
    public async Task QueryCacheWithSelector_ReturnsFallbackValue_WhenCacheIsEmpty()
    {
        // Arrange
        var cacheKey = "TestCacheKey9";
        var fallbackValue = "FallbackValue0";
        var memoryCacheOptions = new MemoryCacheOptions();
        var memoryCache = new MemoryCache(memoryCacheOptions);
        var cacheService = new CacheService(memoryCache, _logger);

        // Act
        var result =
            await cacheService.QueryCacheWithSelector(cacheKey, () => Task.FromResult(fallbackValue),
                value => value.Length);

        // Assert
        Assert.AreEqual(fallbackValue.Length, result);
    }

    [TestMethod]
    public async Task QueryCacheWithSelector_ReturnsFallbackValue_WhenCacheIsExpired()
    {
        // Arrange
        var cacheKey = "TestCacheKeyA";
        var cacheValue = "TestCacheValueB";
        var memoryCacheOptions = new MemoryCacheOptions();
        var memoryCache = new MemoryCache(memoryCacheOptions);
        memoryCache.Set(cacheKey, cacheValue, TimeSpan.FromMilliseconds(1));
        await Task.Delay(10);
        var cacheService = new CacheService(memoryCache, _logger);

        // Act
        var result = await cacheService.QueryCacheWithSelector(cacheKey, () => Task.FromResult("FallbackValue"),
            value => value.Length);

        // Assert
        Assert.AreEqual("FallbackValue".Length, result);
    }

    [TestMethod]
    public void Clear_RemovesCachedValue()
    {
        // Arrange
        var cacheKey = "TestCacheKeyC";
        var cacheValue = "TestCacheValueD";
        var memoryCacheOptions = new MemoryCacheOptions();
        var memoryCache = new MemoryCache(memoryCacheOptions);
        memoryCache.Set(cacheKey, cacheValue);
        var cacheService = new CacheService(memoryCache, _logger);

        // Act
        cacheService.Clear(cacheKey);

        // Assert
        Assert.IsFalse(memoryCache.TryGetValue(cacheKey, out _));
    }
}
