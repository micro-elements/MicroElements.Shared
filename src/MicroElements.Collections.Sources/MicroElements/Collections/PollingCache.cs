
namespace MicroElements.Collections.Caching
{
    using System;
    using System.Threading.Tasks;
    using MicroElements.Collections.Cache;
    using MicroElements.Collections.TwoLayerCache;

    /// <summary>
    /// Provides lazy value with expiration time.
    /// </summary>
    /// <typeparam name="TArg">The argument type.</typeparam>
    /// <typeparam name="TValue">The result type.</typeparam>
    internal class ValueWithExpiration<TArg, TValue>
    {
        private readonly TArg _arg;
        private readonly Func<TArg, TValue> _factory;
        private readonly TimeSpan _timeToLive;
        private Lazy<TValue> _value;

        /// <summary>
        /// Gets (or evaluates) the value.
        /// </summary>
        public TValue Value => _value.Value;

        /// <summary>
        /// Gets the expiration time for the <see cref="Value"/>.
        /// </summary>
        public DateTimeOffset AbsoluteExpiration { get; private set; }

        /// <summary>
        /// aa.
        /// </summary>
        /// <param name="factory">Provides a factory to create a value.</param>
        /// <param name="arg">Provides an argument for the factory.</param>
        /// <param name="timeToLive">Time to live for the value.</param>
        public ValueWithExpiration(
            Func<TArg, TValue> factory,
            TArg arg,
            TimeSpan timeToLive)
        {
            _factory = factory;
            _arg = arg;
            _timeToLive = timeToLive;
            _value = null!;
            Reset();
        }

        public void Reset()
        {
            AbsoluteExpiration = DateTimeOffset.Now.Add(_timeToLive);
            _value = new Lazy<TValue>(ValueFactory);
        }

        private TValue ValueFactory()
        {
            TValue result = _factory(_arg);
            return result;
        }
    }

    internal static class PollingCache
    {
        public static TimeSpan DefaultTimeToLive { get; } = TimeSpan.FromSeconds(10);

        readonly record struct CacheContext(TimeSpan TimeToLive, string? CacheName);

        public static TValue GetCached<TArg1, TValue>(
            this TArg1 arg1,
            Func<TArg1, TValue> factory,
            TimeSpan? timeToLive = null,
            string? cacheName = null,
            bool useWeakCache = false)
            where TArg1 : class
        {
            var cacheKey = arg1;
            CacheContext context = new(TimeToLive: timeToLive ?? DefaultTimeToLive, CacheName: cacheName);

            Func<TArg1, Func<TArg1, CacheContext, ValueWithExpiration<TArg1, TValue>>, CacheContext, ValueWithExpiration<TArg1, TValue>> getOrAdd;

            if (useWeakCache)
            {
                getOrAdd = arg1
                    .GetWeakCache<TArg1, ValueWithExpiration<TArg1, TValue>>(cacheName)
                    .GetOrAdd;
            }
            else
            {
                getOrAdd = TwoLayerCache
                    .Instance<TArg1, ValueWithExpiration<TArg1, TValue>>(cacheName)
                    .GetOrAdd;
            }

            var valueWithExpiration = getOrAdd(
                cacheKey, (k, a) => new ValueWithExpiration<TArg1, TValue>(FactoryAdapter, k, a.TimeToLive), context);

            if (DateTimeOffset.Now >= valueWithExpiration.AbsoluteExpiration)
                valueWithExpiration.Reset();

            return valueWithExpiration.Value;

            TValue FactoryAdapter(TArg1 arg)
            {
                return factory(arg);
            }
        }

        public static Task<TValue> GetCachedAsync<TArg1, TValue>(
            this TArg1 arg1,
            Func<TArg1, Task<TValue>> factory,
            TimeSpan? timeToLive = null,
            string? cacheName = null)
        {
            var cacheKey = arg1;
            CacheContext context = new(TimeToLive: timeToLive ?? DefaultTimeToLive, CacheName: cacheName);
            var valueWithExpiration = TwoLayerCache
                .Instance<TArg1, ValueWithExpiration<TArg1, Task<TValue>>>(cacheName)
                .GetOrAdd(cacheKey, (k, a) => new ValueWithExpiration<TArg1, Task<TValue>>(FactoryAdapter, k, a.TimeToLive), context);

            if (DateTimeOffset.Now >= valueWithExpiration.AbsoluteExpiration)
                valueWithExpiration.Reset();

            return valueWithExpiration.Value;

            Task<TValue> FactoryAdapter(TArg1 arg)
            {
                return factory(arg);
            }
        }

        internal static TwoLayerCache<TArg1, ValueWithExpiration<TArg1, Task<TValue>>> GetCache<TArg1, TValue>(string? cacheName = null, int maxItemsCount = 16) =>
            TwoLayerCache.Instance<TArg1, ValueWithExpiration<TArg1, Task<TValue>>>(cacheName, settings => settings.MaxItemCount = maxItemsCount);

        internal static TwoLayerCache<(TValueProvider ValueProvider, TArg1 Arg1), ValueWithExpiration<(TValueProvider, TArg1), Task<TValue>>> GetCache<TValueProvider, TArg1, TValue>(string? cacheName = null, int maxItemsCount = 16) =>
            TwoLayerCache.Instance<(TValueProvider ValueProvider, TArg1 Arg1), ValueWithExpiration<(TValueProvider, TArg1), Task<TValue>>>(cacheName, settings => settings.MaxItemCount = maxItemsCount);

        internal static TwoLayerCache<(TValueProvider ValueProvider, TArg1 Arg1, TArg2 Arg2), ValueWithExpiration<(TValueProvider, TArg1, TArg2), Task<TValue>>> GetCache<TValueProvider, TArg1, TArg2, TValue>(string? cacheName = null, int maxItemsCount = 16) =>
            TwoLayerCache.Instance<(TValueProvider ValueProvider, TArg1 Arg1, TArg2 Arg2), ValueWithExpiration<(TValueProvider, TArg1, TArg2), Task<TValue>>>(cacheName, settings => settings.MaxItemCount = maxItemsCount);

        public static Task<TValue> GetCachedAsync<TValueProvider, TArg1, TValue>(
            this TValueProvider valueProvider,
            TArg1 arg1,
            Func<TValueProvider, TArg1, Task<TValue>> factory,
            TimeSpan? timeToLive = null,
            string? cacheName = null)
        {
            var cacheKey = (ValueProvider: valueProvider, Arg1: arg1);
            CacheContext context = new(TimeToLive: timeToLive ?? DefaultTimeToLive, CacheName: cacheName);
            var valueWithExpiration = GetCache<TValueProvider, TArg1, TValue>(cacheName)
                .GetOrAdd(cacheKey, (key, a)
                    => new ValueWithExpiration<(TValueProvider, TArg1), Task<TValue>>(FactoryAdapter, key, a.TimeToLive), context);

            if (DateTimeOffset.Now >= valueWithExpiration.AbsoluteExpiration)
                valueWithExpiration.Reset();

            return valueWithExpiration.Value;

            Task<TValue> FactoryAdapter((TValueProvider, TArg1) args)
            {
                return factory(args.Item1, args.Item2);
            }
        }

        public static Task<TValue> GetCachedAsync<TValueProvider, TArg1, TArg2, TValue>(
            this TValueProvider valueProvider,
            TArg1 arg1,
            TArg2 arg2,
            Func<TValueProvider, TArg1, TArg2, Task<TValue>> factory,
            TimeSpan? timeToLive = null,
            string? cacheName = null)
        {
            var cacheKey = (ValueProvider: valueProvider, Arg1: arg1, Arg2: arg2);
            CacheContext context = new(TimeToLive: timeToLive ?? DefaultTimeToLive, CacheName: cacheName);
            var valueWithExpiration = GetCache<TValueProvider, TArg1, TArg2, TValue>(cacheName)
                .GetOrAdd(cacheKey, (k, a) => new ValueWithExpiration<(TValueProvider, TArg1, TArg2), Task<TValue>>(FactoryAdapter, k, a.TimeToLive), context);

            if (DateTimeOffset.Now >= valueWithExpiration.AbsoluteExpiration)
                valueWithExpiration.Reset();

            return valueWithExpiration.Value;

            Task<TValue> FactoryAdapter((TValueProvider, TArg1, TArg2) args)
            {
                return factory(args.Item1, args.Item2, args.Item3);
            }
        }
    }
}