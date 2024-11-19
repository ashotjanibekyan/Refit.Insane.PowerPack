using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;

namespace Refit.Insane.PowerPack.Caching.Internal;

public class AkavachePersistedCache : IPersistedCache
{
    public async Task<DateTimeOffset?> GetSavedAtTime(RefitCacheLocation cacheLocation, string atKey)
    {
        return await GetBlobCache(cacheLocation).GetCreatedAt(atKey);
    }

    public async Task Delete(RefitCacheLocation refitCacheLocation, string cachedValueAtKey)
    {
        await GetBlobCache(refitCacheLocation).Invalidate(cachedValueAtKey);
    }

    public async Task<TResult> Get<TResult>(RefitCacheLocation refitCacheLocation, string atKey)
    {
        return await GetBlobCache(refitCacheLocation).GetObject<TResult>(atKey).Catch(Observable.Return(default(TResult)));
    }

    public async Task Save<T>(RefitCacheLocation refitCacheLocation, string atKey, T valueToCache, TimeSpan? timeToLive)
    {
        if (timeToLive.HasValue)
        {
            await GetBlobCache(refitCacheLocation).InsertObject(atKey, valueToCache, timeToLive.Value);
        }
        else
        {
            await GetBlobCache(refitCacheLocation).InsertObject(atKey, valueToCache);
        }
    }

    private IBlobCache GetBlobCache(RefitCacheLocation refitCacheLocation)
    {
        return refitCacheLocation switch
        {
            RefitCacheLocation.Local => BlobCache.LocalMachine,
            RefitCacheLocation.Secure => BlobCache.Secure,
            RefitCacheLocation.InMemory => BlobCache.InMemory,
            RefitCacheLocation.UserAccount => BlobCache.UserAccount,
            _ => BlobCache.LocalMachine
        };
    }
}