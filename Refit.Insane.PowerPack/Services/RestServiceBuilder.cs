using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using Refit.Insane.PowerPack.Caching.Internal;

namespace Refit.Insane.PowerPack.Services;

public class RestServiceBuilder
{
    private bool _isAutoRetryEnabled = true;
    private bool _isCacheEnabled = true;
    private RefitSettings _refitSettings;

    public RestServiceBuilder WithAutoRetry(bool shouldEnableAutoRetry = true)
    {
        _isAutoRetryEnabled = shouldEnableAutoRetry;
        return this;
    }

    public RestServiceBuilder WithCaching(bool shouldEnableCache = true)
    {
        _isCacheEnabled = shouldEnableCache;
        return this;
    }

    public RestServiceBuilder WithRefitSettings(RefitSettings refitSettingsForService)
    {
        _refitSettings = refitSettingsForService;
        return this;
    }

    public IRestService BuildRestService(Assembly restApiAssembly)
    {
        var refitRestService = _refitSettings != null ? new RefitRestService(_refitSettings) : new RefitRestService();

        return BuildRestService(refitRestService, restApiAssembly);
    }

    public IRestService BuildRestService(IReadOnlyDictionary<Type, DelegatingHandler> handlerImplementations, Assembly restApiAssembly)
    {
        var refitRestService = _refitSettings != null
            ? new RefitRestService(handlerImplementations, _refitSettings)
            : new RefitRestService(handlerImplementations);
        return BuildRestService(refitRestService, restApiAssembly);
    }

    public IRestService BuildRestService(IReadOnlyDictionary<Type, Func<DelegatingHandler>> handlerFactories, Assembly restApiAssembly)
    {
        var refitRestService = _refitSettings != null
            ? new RefitRestService(handlerFactories, _refitSettings)
            : new RefitRestService(handlerFactories);

        return BuildRestService(refitRestService, restApiAssembly);
    }

    public IRestService BuildRestService(RefitRestService restService, Assembly restApiAssembly)
    {
        IRestService refitRestService = restService;

        if (_isAutoRetryEnabled)
        {
            refitRestService = new RefitRestServiceRetryProxy(refitRestService, restApiAssembly);
        }

        if (_isCacheEnabled)
        {
            refitRestService = new RefitRestServiceCachingDecorator(refitRestService, new RefitCacheController());
        }

        return refitRestService;
    }
}