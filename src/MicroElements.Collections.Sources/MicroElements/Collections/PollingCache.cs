#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable All

#endregion

namespace MicroElements.Collections.PollingCache
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using MicroElements.Collections.Cache;
    using MicroElements.Collections.TwoLayerCache;

    /// <summary>
    /// Provides lazy value with an expiration time.
    /// </summary>
    /// <typeparam name="TArg">The argument type.</typeparam>
    /// <typeparam name="TValue">The result type.</typeparam>
    internal class CacheValue<TArg, TValue>
    {
        private readonly TArg _arg;
        private readonly Func<TArg, Task<TValue?>> _factory;
        private readonly TimeSpan _timeToLive;
        private readonly Action<CacheValue<TArg, TValue>>? _afterFactory;

        private Task<TValue?> _result;
        private TValue? _value;
        private ExceptionDispatchInfo? _exceptionDispatchInfo;

        /// <summary>
        /// Gets the value task.
        /// </summary>
        public Task<TValue?> Result => _result;

        /// <summary>
        /// Gets evaluated value.
        /// </summary>
        public TValue? Value => _value;

        /// <summary>
        /// Gets an exception occured on getting a value.
        /// </summary>
        public Exception? Exception => _exceptionDispatchInfo?.SourceException;

        /// <summary>
        /// Gets the expiration time for the <see cref="Value"/>.
        /// </summary>
        public DateTimeOffset AbsoluteExpiration { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheValue{TArg,TValue}"/> class.
        /// </summary>
        /// <param name="factory">Provides a factory to create a value.</param>
        /// <param name="arg">Provides an argument for the factory.</param>
        /// <param name="timeToLive">Time to live for the value.</param>
        /// <param name="afterFactory">Action that executes after factory call.</param>
        public CacheValue(
            Func<TArg, Task<TValue?>> factory,
            TArg arg,
            TimeSpan timeToLive,
            Action<CacheValue<TArg, TValue>>? afterFactory = null)
        {
            _factory = factory;
            _arg = arg;
            _timeToLive = timeToLive;
            _afterFactory = afterFactory;
            _result = null!;
            Reset();
        }

        /// <summary>
        /// Resets value so it should be evaluated by the provided factory.
        /// </summary>
        public void Reset()
        {
            SetExpireAfter(_timeToLive);
            _result = ValueFactory();
        }

        /// <summary>
        /// Sets new expiration time.
        /// </summary>
        /// <param name="timeToLive">Time to live for the value.</param>
        public void SetExpireAfter(TimeSpan timeToLive)
        {
            AbsoluteExpiration = DateTimeOffset.Now.Add(timeToLive);
        }

        /// <summary>
        /// Throws the original exception if any.
        /// </summary>
        public void Throw()
        {
            if (_exceptionDispatchInfo != null)
                _exceptionDispatchInfo.Throw();
        }

        private async Task<TValue?> ValueFactory()
        {
            try
            {
                _value = await _factory(_arg);
                _exceptionDispatchInfo = null;
            }
            catch (Exception e)
            {
                _value = default;
                _exceptionDispatchInfo = ExceptionDispatchInfo.Capture(e);
            }

            try
            {
                _afterFactory?.Invoke(this);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return _value;
        }
    }

    /// <summary>
    /// Extensible async cache with TimeToLive.
    /// </summary>
    internal static class PollingCache
    {
        public const int MaxItemsCountDefault = 100;
        public static TimeSpan DefaultTimeToLive { get; } = TimeSpan.FromSeconds(60);

        public readonly record struct CacheContext(TimeSpan TimeToLive, string? CacheName);

        public static async Task<TValue?> GetCachedAsync<TArg, TValue>(
            this TArg arg,
            Func<TArg, Task<TValue?>> factory,
            Func<TArg, Func<TArg, CacheContext, CacheValue<TArg, TValue>>, CacheContext, CacheValue<TArg, TValue>>? getOrAdd = null,
            Action<CacheValue<TArg, TValue>>? afterFactory = null,
            Func<CacheValue<TArg, TValue>, Task<TValue?>>? processResult = null,
            TimeSpan? timeToLive = null,
            string? cacheName = null,
            int maxItemsCount = MaxItemsCountDefault)
        {
            // Composite key
            var cacheKey = arg;

            // Provides only one thread per key
            using var keyLock = await Cache
                .Instance<TArg, SemaphoreSlim>()
                .GetOrAdd(cacheKey, _ => new SemaphoreSlim(1))
                .WaitAsyncAndGetLockReleaser();

            // GetOrAdd func
            getOrAdd ??= GetCache<TArg, TValue>(cacheName, maxItemsCount).GetOrAdd;

            var context = new CacheContext(TimeToLive: timeToLive ?? DefaultTimeToLive, CacheName: cacheName);
            var cacheValue = getOrAdd(cacheKey, (key, c) => new CacheValue<TArg, TValue>(FactoryAdapter, key, c.TimeToLive, afterFactory), context);

            // If value is expired then reset it
            if (DateTimeOffset.Now >= cacheValue.AbsoluteExpiration)
                cacheValue.Reset();

            // Get or create value
            var value = await cacheValue.Result;

            // Allows to postprocess cached value.
            if (processResult != null)
            {
                value = await processResult.Invoke(cacheValue);
            }

            return value;

            async Task<TValue?> FactoryAdapter(TArg args)
            {
                var factoryResult = await factory(args);
                return factoryResult;
            }
        }

        public static async Task<TValue?> GetCachedAsync<TValueProvider, TArg1, TValue>(
            this TValueProvider valueProvider,
            TArg1 arg1,
            Func<TValueProvider, TArg1, Task<TValue?>> factory,
            Action<CacheValue<(TValueProvider, TArg1), TValue>>? afterFactory = null,
            Func<CacheValue<(TValueProvider, TArg1), TValue>, Task<TValue?>>? processResult = null,
            TimeSpan? timeToLive = null,
            string? cacheName = null,
            int maxItemsCount = MaxItemsCountDefault)
        {
            // Composite key
            var cacheKey = (ValueProvider: valueProvider, Arg1: arg1);

            return await cacheKey.GetCachedAsync(FactoryAdapter, null, afterFactory, processResult, timeToLive, cacheName, maxItemsCount);

            async Task<TValue?> FactoryAdapter((TValueProvider, TArg1) args)
            {
                var value = await factory(args.Item1, args.Item2);
                return value;
            }
        }

        public static async Task<TValue?> GetCachedAsync<TValueProvider, TArg1, TArg2, TValue>(
            this TValueProvider valueProvider,
            TArg1 arg1,
            TArg2 arg2,
            Func<TValueProvider, TArg1, TArg2, Task<TValue?>> factory,
            Action<CacheValue<(TValueProvider, TArg1, TArg2), TValue>>? afterFactory = null,
            Func<CacheValue<(TValueProvider, TArg1, TArg2), TValue>, Task<TValue?>>? processResult = null,
            TimeSpan? timeToLive = null,
            string? cacheName = null,
            int maxItemsCount = MaxItemsCountDefault)
        {
            // Composite key
            var cacheKey = (ValueProvider: valueProvider, Arg1: arg1, Arg2: arg2);

            return await cacheKey.GetCachedAsync(FactoryAdapter, null, afterFactory, processResult, timeToLive, cacheName, maxItemsCount);

            async Task<TValue?> FactoryAdapter((TValueProvider, TArg1, TArg2) args)
            {
                var value = await factory(args.Item1, args.Item2, args.Item3);
                return value;
            }
        }

        internal static TwoLayerCache<TArg1, CacheValue<TArg1, TValue>> GetCache<TArg1, TValue>(string? cacheName = null, int maxItemsCount = MaxItemsCountDefault) =>
            TwoLayerCache.Instance<TArg1, CacheValue<TArg1, TValue>>(cacheName, settings =>
            {
                settings.MaxItemCount = maxItemsCount;
                settings.CheckColdCacheSize = true;
            });
    }

    /// <summary>
    /// AwaitableLock based on <see cref="SemaphoreSlim"/>.
    /// Implements <see cref="IDisposable"/> to use in using block.
    /// It waits for semaphore and releases it on dispose.
    /// </summary>
    internal static class AwaitableLock
    {
        internal readonly struct LockLease : IDisposable
        {
            private readonly SemaphoreSlim _lock;

            internal LockLease(SemaphoreSlim @lock) => _lock = @lock;

            /// <inheritdoc/>
            public void Dispose() => _lock.Release();
        }

        /// <summary>
        /// Waits async for <paramref name="semaphoreSlim"/> and returns <see cref="IDisposable"/> that will release lock on dispose.
        /// </summary>
        /// <param name="semaphoreSlim">SemaphoreSlim.</param>
        /// <returns><see cref="IDisposable"/> that will release lock on dispose.</returns>
        public static async ValueTask<LockLease> WaitAsyncAndGetLockReleaser(this SemaphoreSlim semaphoreSlim)
        {
            await semaphoreSlim.WaitAsync();
            return new LockLease(semaphoreSlim);
        }

        /// <summary>
        /// Waits for <paramref name="semaphoreSlim"/> and returns <see cref="IDisposable"/> that will release lock on dispose.
        /// </summary>
        /// <param name="semaphoreSlim">SemaphoreSlim.</param>
        /// <returns><see cref="IDisposable"/> that will release lock on dispose.</returns>
        public static LockLease WaitAndGetLockReleaser(this SemaphoreSlim semaphoreSlim)
        {
            semaphoreSlim.Wait();
            return new LockLease(semaphoreSlim);
        }
    }
}