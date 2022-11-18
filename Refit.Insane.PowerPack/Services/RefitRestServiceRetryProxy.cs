﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Refit.Insane.PowerPack.Data;
using Refit.Insane.PowerPack.Retry;
using Polly;
using System.Linq;
using Refit.Insane.PowerPack.Caching;

namespace Refit.Insane.PowerPack.Services
{
    public class RefitRestServiceRetryProxy : IRestService
    {
        readonly IRestService proxiedRestService;
        private readonly Assembly _restApiAssembly;
        Response<RefitRetryAttribute> globallyDefinedRefitRetryAttributeResponse;

        public RefitRestServiceRetryProxy(IRestService proxiedRestService, Assembly restApiAssembly)
        {
            this.proxiedRestService = proxiedRestService;
            _restApiAssembly = restApiAssembly;
        }

        public Task<Response> Execute<TApi>(Expression<Func<TApi, Task>> executeApiMethod)
            => ExecuteMethod(() => proxiedRestService.Execute(executeApiMethod), executeApiMethod.Body as MethodCallExpression);

        private Task<TResult> ExecuteMethod<TResult>(Func<Task<TResult>> restFunc, MethodCallExpression methodCallExpression)
        {
            var refitRetryAttributeResponse = GetMethodRetryAttribute(methodCallExpression);

            if (!refitRetryAttributeResponse.IsSuccess)
                return restFunc();

            var refitRetryAttribute = refitRetryAttributeResponse.Results;

            if (refitRetryAttribute.RetryCount < 1)
                return restFunc();

            var policy =
                Policy
                .Handle<Exception>(exception =>
               {
                   var apiException = exception as ApiException;

                   if (exception is TaskCanceledException)
                       return true;
                   if (apiException == null)
                       return false;

                   return refitRetryAttribute.RetryOnStatusCodes.Any(x => x == apiException.StatusCode);
               }).WaitAndRetryAsync(refitRetryAttribute.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            return policy.ExecuteAsync(restFunc);
        }

        private Response<RefitRetryAttribute> GetMethodRetryAttribute(MethodCallExpression methodCallExpression)
        {
            var refitRetryAttribute =
                methodCallExpression
                    .Method
                    .GetCustomAttribute<RefitRetryAttribute>();

            if (refitRetryAttribute != null)
                return new Response<RefitRetryAttribute>(refitRetryAttribute);

            lock (this)
            {
                globallyDefinedRefitRetryAttributeResponse = globallyDefinedRefitRetryAttributeResponse ?? GetGloballyDefinedRefitRetryAttribute();
            }

            return globallyDefinedRefitRetryAttributeResponse;
        }

        private Response<RefitRetryAttribute> GetGloballyDefinedRefitRetryAttribute()
        {
            var refitRetryAttribute =
                _restApiAssembly
                    .GetCustomAttribute<RefitRetryAttribute>();

            if (refitRetryAttribute == null)
                return new Response<RefitRetryAttribute>().SetAsFailureResponse();

            return new Response<RefitRetryAttribute>(refitRetryAttribute);
        }


        public Task<Response<TResult>> Execute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> executeApiMethod, RefitCacheBehaviour cacheBehaviour)
            => ExecuteMethod(() => proxiedRestService.Execute(executeApiMethod, cacheBehaviour), executeApiMethod.Body as MethodCallExpression);

        public Task<Response<TResult>> Execute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> executeApiMethod,
            Func<TimeSpan?, RefitCacheBehaviour> controlCacheBehaviourBasedOnTimeSpanBetweenLastCacheUpdate)
            => ExecuteMethod(() => proxiedRestService.Execute(executeApiMethod, controlCacheBehaviourBasedOnTimeSpanBetweenLastCacheUpdate), executeApiMethod.Body as MethodCallExpression);
    }
}
