using Aiursoft.Scanner.Abstract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon;

/// <summary>
/// Provides a service for caching data in memory.
/// </summary>
public class CacheService : ITransientDependency
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    /// <summary>
    /// Initializes a new instance of the CacheService class.
    /// </summary>
    /// <param name="cache">An instance of IMemoryCache used to store cached data.</param>
    /// <param name="logger">An instance of ILogger used to log cache-related events.</param>
    public CacheService(
        IMemoryCache cache,
        ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves data from the cache if available; otherwise, retrieves data using a fallback function and caches the result.
    /// </summary>
    /// <typeparam name="T">The type of the cached data.</typeparam>
    /// <param name="cacheKey">The key used to identify the cached data.</param>
    /// <param name="fallback">A function used to retrieve the data if it is not available in the cache.</param>
    /// <param name="cacheCondition">An optional predicate used to determine if the cached data is still valid.</param>
    /// <param name="cachedMinutes">The number of minutes to cache the data for.</param>
    /// <returns>The cached data, or the result of the fallback function if the data is not available in the cache.</returns>
    public async Task<T?> RunWithCache<T>(
        string cacheKey,
        Func<Task<T>> fallback,
        Predicate<T>? cacheCondition = null,
        int cachedMinutes = 20)
    {
        cacheCondition ??= (_) => true;

        if (!_cache.TryGetValue(cacheKey, out T resultValue) || resultValue == null || cachedMinutes <= 0 ||
            cacheCondition(resultValue) == false)
        {
            resultValue = await fallback();
            if (resultValue == null)
            {
                return default;
            }
            else if (cachedMinutes > 0 && cacheCondition(resultValue))
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(cachedMinutes));

                _cache.Set(cacheKey, resultValue, cacheEntryOptions);
                _logger.LogInformation("Cache set for {CachedMinutes} minutes with cached key: {CacheKey}",
                    cachedMinutes, cacheKey);
            }
        }
        else
        {
            _logger.LogInformation("Cache was hit with cached key: {CacheKey}", cacheKey);
        }

        return resultValue;
    }

    /// <summary>
    /// Retrieves data from the cache if available; otherwise, retrieves data using a fallback function, applies a selector function to the result, and caches the selected result.
    /// </summary>
    /// <typeparam name="T1">The type of the data retrieved using the fallback function.</typeparam>
    /// <typeparam name="T2">The type of the cached data.</typeparam>
    /// <param name="cacheKey">The key used to identify the cached data.</param>
    /// <param name="fallback">A function used to retrieve the data if it is not available in the cache.</param>
    /// <param name="selector">A function used to select the data to cache from the result of the fallback function.</param>
    /// <param name="cacheCondition">An optional predicate used to determine if the cached data is still valid.</param>
    /// <param name="cachedMinutes">The number of minutes to cache the data for.</param>
    /// <returns>The selected cached data, or the result of the fallback function if the data is not available in the cache.</returns>
    public async Task<T2?> QueryCacheWithSelector<T1, T2>(
        string cacheKey,
        Func<Task<T1>> fallback,
        Func<T1, T2> selector,
        Predicate<T1>? cacheCondition = null,
        int cachedMinutes = 20)
    {
        cacheCondition ??= (_) => true;

        if (!_cache.TryGetValue(cacheKey, out T1 resultValue) || resultValue == null || cachedMinutes <= 0 ||
            cacheCondition(resultValue) == false)
        {
            resultValue = await fallback();
            if (resultValue == null)
            {
                return default;
            }

            if (cachedMinutes > 0 && cacheCondition(resultValue))
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(cachedMinutes));

                _cache.Set(cacheKey, resultValue, cacheEntryOptions);
                _logger.LogInformation("Cache set for {CachedMinutes} minutes with cached key: {CacheKey}",
                    cachedMinutes, cacheKey);
            }
        }
        else
        {
            _logger.LogInformation("Cache was hit with cached key: {CacheKey}", cacheKey);
        }

        return selector(resultValue);
    }

    /// <summary>
    /// Removes the cached data associated with the specified key.
    /// </summary>
    /// <param name="key">The key used to identify the cached data to remove.</param>
    public void Clear(string key)
    {
        _cache.Remove(key);
    }
}