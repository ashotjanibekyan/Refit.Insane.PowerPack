using System;
using System.Reflection;

namespace Refit.Insane.PowerPack.Caching.Internal;

internal class MethodCacheDetails(Type apiInterfaceType, MethodInfo methodInfo)
{
    public Type ApiInterfaceType { get; } = apiInterfaceType;

    public MethodInfo MethodInfo { get; } = methodInfo;

    public RefitCacheAttribute CacheAttribute { get; internal set; }

    public override int GetHashCode()
    {
        return ApiInterfaceType.GetHashCode() * 23 * MethodInfo.GetHashCode() * 23 * 29;
    }

    public override bool Equals(object obj)
    {
        var other = obj as MethodCacheDetails;
        return other != null &&
               other.ApiInterfaceType == ApiInterfaceType &&
               other.MethodInfo.Equals(MethodInfo);
    }
}