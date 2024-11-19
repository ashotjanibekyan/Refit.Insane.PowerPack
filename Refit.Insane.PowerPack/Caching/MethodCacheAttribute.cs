using System;

namespace Refit.Insane.PowerPack.Caching;

public class MethodCacheAttributes(
    RefitCacheAttribute cacheAttribute,
    RefitCachePrimaryKeyAttribute primaryKeyAttribute,
    string paramName,
    Type paramType,
    int paramOrder)
{
    public int ParameterOrder { get; } = paramOrder;

    public RefitCacheAttribute CacheAttribute { get; } = cacheAttribute;

    public RefitCachePrimaryKeyAttribute CachePrimaryKeyAttribute { get; } = primaryKeyAttribute;

    public string ParameterName { get; } = paramName;

    public Type ParameterType { get; } = paramType;
}