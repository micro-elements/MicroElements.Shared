#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable All

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MicroElements.Collections.TwoLayerCache
{
    /// <summary id="TwoLayerCache">
    /// <![CDATA[
    /// ### TwoLayerCache
    /// Represents ThreadSafe cache that holds only limited number of items. Can be used as drop in replacement for `ConcurrentDictionary`.
    ///
    /// Use it when you need simple cache but with memory limit.
    ///
    /// Notes:
    ///
    /// - Cache organized in two layers: hot and cold.
    /// - Items first added to cold cache.
    /// - GetValue first checks hot cache. If value not found in hot cache than cold cache uses for search.
    /// - If value exists in cold cache than item moves to hot cache.
    /// - If hot cache exceeds item limit then hot cache became cold cache and new hot cache creates.
    /// ]]>
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    internal partial class TwoLayerCache<TKey, TValue>
        where TKey : notnull
    {
        private readonly int _maxItemCount;
        private readonly bool _checkColdCacheSize;
        private readonly object _sync = new();
        private readonly IEqualityComparer<TKey> _comparer;
        private ConcurrentDictionary<TKey, TValue> _hotCache;
        private ConcurrentDictionary<TKey, TValue> _coldCache;
        private readonly CacheMetrics _metrics = new();

        /// <summary>
        /// Cache metrics.
        /// </summary>
        public class CacheMetrics
        {
            public int ItemsAdded;
            public int SwapCount;
            public int HotCacheHit;
            public int ColdCacheHit;
            public int HotCacheItemsCount;
            public int ColdCacheItemsCount;
            public CacheMetrics Copy() => (CacheMetrics)MemberwiseClone();
        }

        /// <summary>
        /// Gets cache metrics copy.
        /// </summary>
        public CacheMetrics Metrics
        {
            get
            {
                var metrics = _metrics.Copy();
                metrics.HotCacheItemsCount = _hotCache.Count;
                metrics.ColdCacheItemsCount = _coldCache.Count;
                return metrics;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoLayerCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="maxItemCount">Max item count.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys.</param>
        public TwoLayerCache(int maxItemCount = 256, IEqualityComparer<TKey>? comparer = null, bool checkColdCacheSize = false)
        {
            if (maxItemCount <= 0)
                throw new ArgumentException($"maxItemCount should be non negative number but was {maxItemCount}");

            _maxItemCount = maxItemCount;
            _checkColdCacheSize = checkColdCacheSize;
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _hotCache = CreateCache();
            _coldCache = CreateCache();
        }

        /// <summary>
        /// Attempts to add the specified key and value to internal cold cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns>true if the key/value pair was added.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            // Add items only to cold cache.
            var isAdded = _coldCache.TryAdd(key, value);

            if (isAdded)
                OnColdCacheValueAdded(key);

            return isAdded;
        }

        /// <summary>
        /// Gets item by key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">Found value or default.</param>
        /// <returns>true if value found by key.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            // Try get a value from the hot cache.
            if (_hotCache.TryGetValue(key, out value))
            {
                // Cache hit, no more actions.
                Interlocked.Increment(ref _metrics.HotCacheHit);
                return true;
            }

            // Check whether the value exists in the cold cache.
            if (_coldCache.TryGetValue(key, out value))
            {
                // Value exists in cold cache so move to hot cache.
                _hotCache.TryAdd(key, value);

                // If not remove from cold then cold cache size can be twice as hot.
                // Remove omitted for performance reason.
                // _coldCache.TryRemove(key, out _);

                // If hot cache exceeds limit then move all to cold cache and create new hot cache.
                if (_hotCache.Count > _maxItemCount)
                {
                    lock (_sync)
                    {
                        if (_hotCache.Count > _maxItemCount)
                        {
                            // move hot cache to cold and create new hot cache
                            _coldCache = _hotCache;
                            _hotCache = CreateCache();

                            Interlocked.Increment(ref _metrics.SwapCount);
                        }
                    }
                }

                Interlocked.Increment(ref _metrics.ColdCacheHit);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Gets value from cache or uses <paramref name="valueFactory"/> to create item in cache.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="valueFactory">The function used to generate a value for the key.</param>
        /// <returns>The value for the key. This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by valueFactory
        /// if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            // Try get value from hot and cold caches.
            if (TryGetValue(key, out TValue? value))
            {
                // Cache hit, no more actions.
                return value;
            }

            var valueFromCache = _coldCache.GetOrAdd(key, valueFactory);

            OnColdCacheValueAdded(key);

            return valueFromCache;
        }

        /// <summary>
        /// Gets value from cache or uses <paramref name="valueFactory"/> to create item in cache.
        /// This overload allows to reduce allocations in closure.
        /// </summary>
        /// <typeparam name="TArg">Factory argument type.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="valueFactory">The function used to generate a value for the key.</param>
        /// <param name="factoryArg">Factory argument.</param>
        /// <returns>The value for the key. This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by valueFactory
        /// if the key was not in the dictionary.</returns>
        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArg)
        {
            // Try get value from hot and cold caches.
            if (TryGetValue(key, out TValue? value))
            {
                // Cache hit, no more actions.
                return value;
            }

            var valueFromCache = _coldCache.GetOrAdd(key, valueFactory, factoryArg);

            OnColdCacheValueAdded(key);

            return valueFromCache;
        }

        // Creates new dictionary instance.
        private ConcurrentDictionary<TKey, TValue> CreateCache() => new(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: _maxItemCount,
            comparer: _comparer);
    }

    internal partial class TwoLayerCache<TKey, TValue>
    {
        private readonly Lazy<Queue<TKey>> _keysQueue = new Lazy<Queue<TKey>>(() => new Queue<TKey>());

        private void OnColdCacheValueAdded(TKey key)
        {
            if (_checkColdCacheSize)
            {
                lock (_keysQueue)
                {
                    if (_coldCache.Count > _maxItemCount)
                    {
                        // Get the oldest key and remove it from the cold cache
                        var theOldestKey = _keysQueue.Value.Dequeue();
                        _coldCache.TryRemove(theOldestKey, out var removed);
                    }

                    // Store added key
                    _keysQueue.Value.Enqueue(key);
                }
            }

            Interlocked.Increment(ref _metrics.ItemsAdded);
        }
    }

    /// <summary>
    /// TwoLayerCache extensions.
    /// </summary>
    internal static class TwoLayerCache
    {
        static class Caches
        {
            public static class ForType<TKey, TValue>
            {
                public static readonly ConcurrentDictionary<string, TwoLayerCache<TKey, TValue>> ByName = new();
                public static TwoLayerCache<TKey, TValue>? Singleton;
                public static readonly object Lock = new ();
            }
        }

        /// <summary> Cache settings that can be used on cache creation. </summary>
        internal class CacheSettings<TKey, TValue>
        {
            public CacheSettings(string name) => Name = name;

            /// <summary> Gets cache name. </summary>
            public string Name { get; }

            /// <summary> Gets or sets max items count per cache. </summary>
            public int MaxItemCount { get; set; } = 256;

            /// <summary> Gets or sets a value that indicates that MaxItemCount also used for cold cache. </summary>
            public bool CheckColdCacheSize { get; set; } = false;

            /// <summary> Gets or sets optional key comparer for cache. </summary>
            public IEqualityComparer<TKey>? Comparer { get; set; }
        }

        /// <summary>
        /// Gets global static instance of cache by name.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="name">Cache instance name.</param>
        /// <param name="configure">Optional cache configure action.</param>
        /// <returns>Cache instance.</returns>
        internal static TwoLayerCache<TKey, TValue> Instance<TKey, TValue>(string? name = null, Action<CacheSettings<TKey, TValue>>? configure = null)
        {
            if (name == null)
                return InstancePerType<TKey, TValue>(configure);

            return Caches.ForType<TKey, TValue>.ByName
                .GetOrAdd(name, static (name, action) => CreateCacheInstance(name, action), configure);
        }

        private static TwoLayerCache<TKey, TValue> InstancePerType<TKey, TValue>(Action<CacheSettings<TKey, TValue>>? configure = null)
        {
            if (Caches.ForType<TKey, TValue>.Singleton == null)
            {
                lock (Caches.ForType<TKey, TValue>.Lock)
                {
                    if (Caches.ForType<TKey, TValue>.Singleton == null)
                    {
                        Caches.ForType<TKey, TValue>.Singleton = CreateCacheInstance($"TwoLayerCache<{nameof(TKey)},{nameof(TValue)}>", configure);
                    }
                }
            }

            return Caches.ForType<TKey, TValue>.Singleton;
        }

        private static TwoLayerCache<TKey, TValue> CreateCacheInstance<TKey, TValue>(string name, Action<CacheSettings<TKey, TValue>>? configure)
        {
            var settings = new CacheSettings<TKey, TValue>(name);
            configure?.Invoke(settings);

            return new TwoLayerCache<TKey, TValue>(
                maxItemCount: settings.MaxItemCount,
                checkColdCacheSize: settings.CheckColdCacheSize,
                comparer: settings.Comparer ?? EqualityComparer<TKey>.Default);
        }
    }
}
