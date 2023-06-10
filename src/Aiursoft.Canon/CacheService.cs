using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon;

public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(
        IMemoryCache cache,
        ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> RunWithCache<T>(
        string cacheKey,
        Func<Task<T>> fallback,
        Predicate<T>? cacheCondition = null,
        int cachedMinutes = 20)
    {
        cacheCondition ??= (_) => true;

        if (!this._cache.TryGetValue(cacheKey, out T resultValue) || resultValue == null || cachedMinutes <= 0 ||
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

                this._cache.Set(cacheKey, resultValue, cacheEntryOptions);
                this._logger.LogInformation("Cache set for {CachedMinutes} minutes with cached key: {CacheKey}", cachedMinutes, cacheKey);
            }
        }
        else
        {
            this._logger.LogInformation("Cache was hit with cached key: {CacheKey}", cacheKey);
        }

        return resultValue;
    }

    public async Task<T2?> QueryCacheWithSelector<T1, T2>(
        string cacheKey,
        Func<Task<T1>> fallback,
        Func<T1, T2> selector,
        Predicate<T1>? cacheCondition = null,
        int cachedMinutes = 20)
    {
        cacheCondition ??= (_) => true;

        if (!this._cache.TryGetValue(cacheKey, out T1 resultValue) || resultValue == null || cachedMinutes <= 0 ||
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

                this._cache.Set(cacheKey, resultValue, cacheEntryOptions);
                this._logger.LogInformation("Cache set for {CachedMinutes} minutes with cached key: {CacheKey}", cachedMinutes, cacheKey);
            }               

        }
        else
        {
            this._logger.LogInformation("Cache was hit with cached key: {CacheKey}", cacheKey);
        }

        return selector(resultValue);
    }

    public void Clear(string key)
    {
        this._cache.Remove(key);
    }
}