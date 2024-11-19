using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Refit.Insane.PowerPack.Caching;
using Refit.Insane.PowerPack.Caching.Internal;
using Refit.Insane.PowerPack.Data;

namespace Refit.Insane.PowerPack.Services;

public class RefitRestServiceCachingDecorator(
    IRestService decoratedRestService,
    IPersistedCache persistedCache,
    RefitCacheController refitCacheController)
    : IRestService
{
    public RefitRestServiceCachingDecorator(IRestService decoratedRestService, RefitCacheController cacheController)
        : this(decoratedRestService, new AkavachePersistedCache(), cacheController)
    {
    }

    public async Task<Response<TResult>> Execute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> executeApiMethod,
        RefitCacheBehaviour cacheBehaviour = RefitCacheBehaviour.Default)
    {
        if (!refitCacheController.IsMethodCacheable(executeApiMethod))
        {
            return await decoratedRestService.Execute(executeApiMethod, cacheBehaviour).ConfigureAwait(false);
        }

        var cacheKey = refitCacheController.GetCacheKey(executeApiMethod);
        var refitCacheAttribute = refitCacheController.GetRefitCacheAttribute(executeApiMethod);
        var cachedValue = await persistedCache.Get<TResult>(refitCacheAttribute.CacheAttribute.CacheLocation, cacheKey);

        if (cachedValue != null && cacheBehaviour == RefitCacheBehaviour.Default) // if cache behavior is default - always return cache if exists
        {
            return new Response<TResult>(cachedValue);
        }

        // otherwise call api
        var restResponse = await decoratedRestService.Execute(executeApiMethod, cacheBehaviour);

        if (restResponse.IsSuccess)
        {
            // if response is successful - update cache
            var refitCacheAttributes = refitCacheController.GetRefitCacheAttribute(executeApiMethod);
            await persistedCache.Save(refitCacheAttributes.CacheAttribute.CacheLocation, cacheKey, restResponse.Results,
                refitCacheAttributes.CacheAttribute.CacheTtl);
        }
        else
        {
            // if FallbackToCache mode is used, response failed and there is something in cache - return it instead of failed response
            if (cachedValue != null && cacheBehaviour == RefitCacheBehaviour.ForceUpdateFallbackToCache)
            {
                return new Response<TResult>(cachedValue);
            }
        }

        return restResponse;
    }

    public async Task<Response<TResult>> Execute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> executeApiMethod,
        Func<TimeSpan?, RefitCacheBehaviour> controlCacheBehaviourBasedOnTimeSpanBetweenLastCacheUpdate)
    {
        var refitCacheBehaviour = RefitCacheBehaviour.Default;
        if (refitCacheController.IsMethodCacheable(executeApiMethod))
        {
            var cacheKey = refitCacheController.GetCacheKey(executeApiMethod);
            var refitCacheAttribute = refitCacheController.GetRefitCacheAttribute(executeApiMethod);
            var lastSaveDate = await persistedCache.GetSavedAtTime(refitCacheAttribute.CacheAttribute.CacheLocation, cacheKey);

            TimeSpan? timeDifference = null;
            if (lastSaveDate.HasValue)
            {
                timeDifference = DateTimeOffset.UtcNow - lastSaveDate.Value;
            }

            refitCacheBehaviour = controlCacheBehaviourBasedOnTimeSpanBetweenLastCacheUpdate(timeDifference);
        }

        return await Execute(executeApiMethod, refitCacheBehaviour);
    }

    public Task<Response> Execute<TApi>(Expression<Func<TApi, Task>> executeApiMethod)
    {
        return decoratedRestService.Execute(executeApiMethod);
    }
}