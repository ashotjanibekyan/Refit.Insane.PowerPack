﻿using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Refit.Insane.PowerPack.Caching;
using Refit.Insane.PowerPack.Data;

namespace Refit.Insane.PowerPack.Services;

public interface IRestService
{
	/// <summary>
	///     Calls Refit API.
	/// </summary>
	/// <param name="executeApiMethod">Expression to Refit interface method.</param>
	/// <param name="cacheBehaviour">
	///     Control cache behaviour (read enum descriptions). Useful in scenario when you want to
	///     optionally force refresh - typical use-case: pull to refresh in Mobile App.
	/// </param>
	/// <typeparam name="TApi">Refit API interface</typeparam>
	/// <typeparam name="TResult">Method result type.</typeparam>
	/// <returns></returns>
	Task<Response<TResult>> Execute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> executeApiMethod,
        RefitCacheBehaviour cacheBehaviour = RefitCacheBehaviour.Default);

	/// <summary>
	///     Calls Refit API.
	/// </summary>
	/// <param name="executeApiMethod">Expression to Refit interface method.</param>
	/// <param name="controlCacheBehaviourBasedOnTimeSpanBetweenLastCacheUpdate">
	///     Delegate to control cache behaviour - (you can control it so your API could be called - even if the value is
	///     already in cache)
	///     Your delegate will receive TimeSpan between last cache save.
	///     Useful when you keep your cache with infinite time to live - however, after for ex. ~4 hours - you would like to
	///     update it when it's called.
	///     Typical use-case: app settings - infinite time to live in cache - should be updated (but never cleared!) after 24
	///     hours passed
	/// </param>
	/// <typeparam name="TApi">Refit API interface</typeparam>
	/// <typeparam name="TResult">Method result type</typeparam>
	/// <returns></returns>
	Task<Response<TResult>> Execute<TApi, TResult>(Expression<Func<TApi, Task<TResult>>> executeApiMethod,
        Func<TimeSpan?, RefitCacheBehaviour> controlCacheBehaviourBasedOnTimeSpanBetweenLastCacheUpdate);

    Task<Response> Execute<TApi>(Expression<Func<TApi, Task>> executeApiMethod);
}