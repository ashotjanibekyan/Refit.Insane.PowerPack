﻿using System;

namespace Refit.Insane.PowerPack.Caching;

public class RefitCacheAttribute : Attribute
{
    public RefitCacheAttribute()
    {
    }

    public RefitCacheAttribute(int ttlInSeconds) : this(TimeSpan.FromSeconds(ttlInSeconds))
    {
    }

    public RefitCacheAttribute(int ttlHours, int ttlSeconds) : this(TimeSpan.FromHours(ttlHours).Add(TimeSpan.FromSeconds(ttlSeconds)))
    {
    }

    public RefitCacheAttribute(TimeSpan cacheTtl)
    {
        CacheTtl = cacheTtl;
    }

    /// <summary>
    ///     Cache Response Cache Time To Live Duration
    /// </summary>
    public TimeSpan? CacheTtl { get; }

    public RefitCacheLocation CacheLocation { get; set; } = RefitCacheLocation.Local;
}