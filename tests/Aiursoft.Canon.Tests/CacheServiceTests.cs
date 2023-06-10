using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.Canon.Tests;

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
    var result = await cacheService.RunWithCache<string>(cacheKey, () => Task.FromResult("FallbackValue"));

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
    var result = await cacheService.RunWithCache<string>(cacheKey, () => Task.FromResult(fallbackValue));

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
    await Task.Delay(1);
    var cacheService = new CacheService(memoryCache, _logger);

    // Act
    var result = await cacheService.RunWithCache<string>(cacheKey, () => Task.FromResult("FallbackValue"));

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
    var result = await cacheService.QueryCacheWithSelector<string, int>(cacheKey, () => Task.FromResult("FallbackValue"), value => value.Length);

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
    var result = await cacheService.QueryCacheWithSelector<string, int>(cacheKey, () => Task.FromResult(fallbackValue), value => value.Length);

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
    await Task.Delay(1);
    var cacheService = new CacheService(memoryCache, _logger);

    // Act
    var result = await cacheService.QueryCacheWithSelector<string, int>(cacheKey, () => Task.FromResult("FallbackValue"), value => value.Length);

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