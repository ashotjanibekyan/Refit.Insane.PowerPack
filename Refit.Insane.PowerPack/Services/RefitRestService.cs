﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Refit.Insane.PowerPack.Data;
using System.Net;
using Refit.Insane.PowerPack.Attributes;
using Refit.Insane.PowerPack.Configuration;

namespace Refit.Insane.PowerPack.Services
{
	public class RefitRestService : IRestService
    {
	    private readonly IDictionary<Type, Func<DelegatingHandler>> _handlerFactories;
	    private readonly IDictionary<Type, DelegatingHandler> _handlerImplementations;
	    private readonly IDictionary<Type, object> _implementations = new Dictionary<Type, object>();
	    private readonly RefitSettings _refitSettings;

	    public RefitRestService(RefitSettings refitSettings) : this()
	    {
		    _refitSettings = refitSettings;
	    }
	    
	    public RefitRestService()
	    {
		    _handlerImplementations = new Dictionary<Type, DelegatingHandler>();
		    _handlerFactories = new Dictionary<Type, Func<DelegatingHandler>>();
	    }

	    public RefitRestService(IDictionary<Type, DelegatingHandler> handlerImplementations, RefitSettings refitSettings) : this(handlerImplementations)
	    {
		    _refitSettings = refitSettings;
	    }
		
	    public RefitRestService(IDictionary<Type, Func<DelegatingHandler>> handlerFactories, RefitSettings refitSettings) : this(handlerFactories)
	    {
		    _handlerImplementations = new Dictionary<Type, DelegatingHandler>();
		    _handlerFactories = handlerFactories;
		    _refitSettings = refitSettings;
	    }

	    public RefitRestService(IDictionary<Type, DelegatingHandler> handlerImplementations)
	    {
		    _handlerImplementations = handlerImplementations;
		    _handlerFactories = new Dictionary<Type, Func<DelegatingHandler>>();
	    }
		
	    public RefitRestService(IDictionary<Type, Func<DelegatingHandler>> handlerFactories)
	    {
		    _handlerImplementations = new Dictionary<Type, DelegatingHandler>();
		    _handlerFactories = handlerFactories;
	    }

		public async Task<Response<TResult>> Execute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> executeApiMethod, bool forceExecuteEvenIfResponseIsInCache = false)
		{
			var restApi = GetRestApiImplementation<TApi>();

			try
			{
				var responseData = await executeApiMethod.Compile()(restApi).ConfigureAwait(false);
				return new Response<TResult>(responseData);
			}
			catch (ApiException refitApiException)
			{
				if (await CanPrepareResponse(refitApiException))
					return await GetResponse<TResult>(refitApiException);

				throw;
			}
		}

		public Task<Response<TResult>> Execute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> executeApiMethod,
			Func<TimeSpan?, bool> shouldForceExecuteEvenIfResponseIsInCacheBasedOnTimeSpanBetweenLastCacheUpdate) => Execute(executeApiMethod, false);


		public async Task<Response> Execute<TApi>(Expression<Func<TApi, Task>> executeApiMethod)
		{
			var restApi = GetRestApiImplementation<TApi>();

			try
			{
				await executeApiMethod.Compile()(restApi).ConfigureAwait(false);
				return new Response();
			}
			catch (ApiException refitApiException)
			{
				if (await CanPrepareResponse(refitApiException))
					return await GetResponse(refitApiException);

				throw;
			}
		}
	    
	    private TApi GetRestApiImplementation<TApi>()
	    {
		    if (_implementations.ContainsKey(typeof(TApi))) 
			    return (TApi)_implementations[typeof(TApi)];
			
		    var httpClientHandlerType = ApiDefinitionAttributeExtension.GetHttpClientHandlerType<TApi>();
		    var httpClientMessageHandler = GetHandler(httpClientHandlerType);
		    
		    // note is not an issue HttpClient is not singleton, the only thing that matters is that underlying HttpClientHandler has to be resued
		    var httpClient = new HttpClient(httpClientMessageHandler) 
		    {
			    BaseAddress = ApiDefinitionAttributeExtension.GetUri<TApi>(), 
			    Timeout = ApiDefinitionAttributeExtension.GetTimeout<TApi>()
		    };
		    
		    if (httpClient.BaseAddress == null)
			    throw new InvalidOperationException($"HttpClient Base Address has not been set. " +
			                                        $"In case you are not using {nameof(ApiDefinitionAttribute)} on your interface, set global constant in:" +
			                                        $"{nameof(BaseApiConfiguration)} class.");
			    
			
		    var restApi = default(TApi);
		    try
		    {
			    restApi = _refitSettings != null
				    ? RestService.For<TApi>(httpClient, _refitSettings)
				    : RestService.For<TApi>(httpClient);
			    
			    _implementations.Add(typeof(TApi), restApi);
		    }
		    catch (Exception ex)
		    {
			    System.Diagnostics.Debug.WriteLine(ex);
		    }

		    return restApi;
	    }

	    private DelegatingHandler GetHandler(Type httpClientHandlerType)
	    {
		    var httpClientMessageHandler = default(DelegatingHandler);

		    if (_handlerFactories.ContainsKey(httpClientHandlerType) && !_handlerImplementations.ContainsKey(httpClientHandlerType))
		    {
			    var factory = _handlerFactories[httpClientHandlerType];
			    _handlerImplementations.Add(httpClientHandlerType, factory());
		    }

		    if (_handlerImplementations.ContainsKey(httpClientHandlerType))
			    httpClientMessageHandler = _handlerImplementations[httpClientHandlerType];
		    else
		    {
			    httpClientMessageHandler = Activator.CreateInstance(httpClientHandlerType) as DelegatingHandler;
			    _handlerImplementations.Add(httpClientHandlerType, httpClientMessageHandler);
		    }

		    return httpClientMessageHandler;
	    }

        protected virtual Task<bool> CanPrepareResponse(ApiException fromApiException) => Task.FromResult(false);

        protected virtual Task<Response> GetResponse(ApiException fromApiException) {
            throw new InvalidOperationException($"If you are returning true in CanPrepareResponse method " +
                                                "you have to override GetResponse methods.");
        }

        protected virtual Task<Response<TResult>> GetResponse<TResult>(ApiException fromApiException){
			throw new InvalidOperationException($"If you are returning true in CanPrepareResponse method " +
												"you have to override GetResponse methods.");
        }

		 
    }
}
