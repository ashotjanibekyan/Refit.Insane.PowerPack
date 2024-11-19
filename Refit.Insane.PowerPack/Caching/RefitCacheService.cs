using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Refit.Insane.PowerPack.Caching.Internal;
using Refit.Insane.PowerPack.Data;

namespace Refit.Insane.PowerPack.Caching;

public class RefitCacheService
{
    private static readonly Lazy<RefitCacheService> LazyInstance = new(() => new RefitCacheService());
    private readonly RefitCacheController _refitCacheController;
    private IPersistedCache _persistedCache;

    private RefitCacheService()
    {
        _refitCacheController = new RefitCacheController();
        _persistedCache = new AkavachePersistedCache();
    }

    public static RefitCacheService Instance => LazyInstance.Value;

    public async Task<Response<DateTimeOffset>> GetSavedAt<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> forApiMethodCall)
    {
        if (!_refitCacheController.IsMethodCacheable(forApiMethodCall))
        {
            return new Response<DateTimeOffset>().AddErrorMessage("Request method does not have [RefitCache] attribute set.");
        }

        var cacheKey = _refitCacheController.GetCacheKey(forApiMethodCall);
        var refitCacheAttribute = _refitCacheController.GetRefitCacheAttribute(forApiMethodCall);
        var savedAtOffset = await _persistedCache.GetSavedAtTime(refitCacheAttribute.CacheAttribute.CacheLocation, cacheKey);

        return savedAtOffset.HasValue
            ? new Response<DateTimeOffset>(savedAtOffset.Value)
            : new Response<DateTimeOffset>().AddErrorMessage("Cache for requested method is empty.");
    }

    public async Task<Response<TResult>> GetCache<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> forApiMethodCall)
    {
        if (!_refitCacheController.IsMethodCacheable(forApiMethodCall))
        {
            return new Response<TResult>().AddErrorMessage("Request method does not have [RefitCache] attribute set.");
        }

        var cacheKey = _refitCacheController.GetCacheKey(forApiMethodCall);
        var cacheAttribute = _refitCacheController.GetRefitCacheAttribute(forApiMethodCall);
        var cachedValue = await _persistedCache.Get<TResult>(cacheAttribute.CacheAttribute.CacheLocation, cacheKey);

        if (cachedValue != null)
        {
            return new Response<TResult>(cachedValue);
        }

        return new Response<TResult>().AddErrorMessage("Cache for requested method is empty.");
    }

    public Task UpdateCache<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> forApiMethodCall, TResult newCacheValue)
    {
        if (!_refitCacheController.IsMethodCacheable(forApiMethodCall))
        {
            return Task.FromResult(true);
        }

        var cacheKey = _refitCacheController.GetCacheKey(forApiMethodCall);
        var refitCacheAttribute = _refitCacheController.GetRefitCacheAttribute(forApiMethodCall);

        return _persistedCache.Save(refitCacheAttribute.CacheAttribute.CacheLocation, cacheKey, newCacheValue,
            refitCacheAttribute.CacheAttribute.CacheTtl);
    }

    public Task ClearCache<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> forApiMethodCall)
    {
        if (!_refitCacheController.IsMethodCacheable(forApiMethodCall))
        {
            return Task.FromResult(true);
        }

        var cacheKey = _refitCacheController.GetCacheKey(forApiMethodCall);
        var refitCacheAttribute = _refitCacheController.GetRefitCacheAttribute(forApiMethodCall);

        return _persistedCache.Delete(refitCacheAttribute.CacheAttribute.CacheLocation, cacheKey);
    }

    /// <summary>
    ///     By default it is using Akavache - if you're writting unit tests you can substitute it with your mock.
    /// </summary>
    /// <param name="persistedCache">Persisted cache.</param>
    public void SetPersistedCache(IPersistedCache persistedCache)
    {
        _persistedCache = persistedCache;
    }
}