#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

using System.Runtime.CompilerServices;

#pragma warning disable
// ReSharper disable All

#endregion

namespace MicroElements.Collections.Cache
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary id="Cache">
    ///<![CDATA[
    /// ### Cache
    /// Global ambient cache extensions.
    /// Represents `ConcurrentDictionary` of some type that is accessed by it's name.
    ///
    /// Reason: Use cache from any place of your code without declaring cache (that's so boring and noisy).
    /// Best suited for global caches of immutable objects that never changes in application lifecycle.
    ///
    /// #### Usage
    ///
    /// ```csharp
    /// var value1 = Cache.Instance<string, string>("Example").GetOrAdd("key1", k => VeryLongGetValue(k));
    /// ```
    ///
    /// #### Notes
    /// - Cache instance is global so use proper cache instance names.
    /// - For static cache use some const name for example class name or method name
    /// - Can cause to memory leak if cache grows permanently. For such cases use caches that clears by time or size for example `TwoLayerCache`
    /// - Adds one more operation to find cache instance
    /// - It can be treated as some kind of an "AMBIENT CONTEXT" that becomes an anti-pattern. See: https://freecontent.manning.com/the-ambient-context-anti-pattern/
    /// ]]>
    /// </summary>
    internal static class Cache
    {
        static class Caches
        {
            internal static class ForType<TKey, TValue>
            {
                internal static ConcurrentDictionary<TKey, TValue>? Singleton;
                internal static readonly ConcurrentDictionary<string, ConcurrentDictionary<TKey, TValue>> ByName = new();
                internal static readonly ConditionalWeakTable<object, ConcurrentDictionary<TKey, TValue>> ByInstance = new();
                internal static readonly ConditionalWeakTable<object, ConcurrentDictionary<string, ConcurrentDictionary<TKey, TValue>>> ByInstanceAndName = new();
                internal static readonly object Lock = new();
            }
        }

        /// <summary> Cache settings that can be used on cache creation. </summary>
        internal class CacheSettings<TKey, TValue>
        {
            public CacheSettings(string name) => Name = name;

            /// <summary> Gets cache name. </summary>
            public string Name { get; }

            /// <summary> Gets or sets optional key comparer for cache. </summary>
            public IEqualityComparer<TKey>? Comparer { get; set; }

            /// <summary> Gets or sets optional initial values for cache. </summary>
            public IEnumerable<KeyValuePair<TKey, TValue>>? InitialValues { get; set; }
        }

        /// <summary>
        /// Gets or creates new cache instance.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="name">Cache name.</param>
        /// <param name="configure">Optional cache configure action.</param>
        /// <returns>Cache instance.</returns>
        internal static ConcurrentDictionary<TKey, TValue> Instance<TKey, TValue>(string? name = null, Action<CacheSettings<TKey, TValue>>? configure = null)
        {
            if (name == null)
                return InstancePerType<TKey, TValue>(configure);

            return Caches.ForType<TKey, TValue>.ByName
                .GetOrAdd(name, static (name, action) => CreateCacheInstance(name, action), configure);
        }

        /// <summary>
        /// Gets weak referenced cache that attached to the <paramref name="hostObject"/>.
        /// Cache will be released after the object dispose.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="hostObject">The object that "hosts" attached cache.</param>
        /// <param name="name">Optional cache name to get.</param>
        /// <param name="configure">Optional cache configure action.</param>
        /// <returns></returns>
        internal static ConcurrentDictionary<TKey, TValue> GetWeakCache<TKey, TValue>(this object hostObject,
            string? name = null, Action<CacheSettings<TKey, TValue>>? configure = null)
        {
            if (name == null)
                return Caches
                    .ForType<TKey, TValue>
                    .ByInstance
                    .GetOrAdd(hostObject, static (_, a) => CreateCacheInstance(a.name ?? a.hostObject.GetHashCode().ToString(), a.configure), (hostObject, name, configure));

            return Caches
                .ForType<TKey, TValue>
                .ByInstanceAndName
                .GetOrAdd(hostObject, (_, _) => new ConcurrentDictionary<string, ConcurrentDictionary<TKey, TValue>>(), hostObject)
                .GetOrAdd(name, static (name, action) => CreateCacheInstance(name, action), configure);
        }

        private static TValue GetOrAdd<TKey, TValue, TArg>(
            this ConditionalWeakTable<TKey, TValue> weakTable,
            TKey key,
            Func<TKey, TArg, TValue> factory,
            TArg arg)
            where TValue : class where TKey : class
        {
            if (weakTable.TryGetValue(key, out var attachedValue))
            {
                return attachedValue;
            }

            lock (weakTable)
            {
                if (weakTable.TryGetValue(key, out attachedValue))
                {
                    return attachedValue;
                }

                attachedValue = factory(key, arg);
                weakTable.Add(key, attachedValue);

                return attachedValue;
            }
        }

        private static ConcurrentDictionary<TKey, TValue> InstancePerType<TKey, TValue>(Action<CacheSettings<TKey, TValue>>? configure = null)
        {
            if (Caches.ForType<TKey, TValue>.Singleton == null)
            {
                lock (Caches.ForType<TKey, TValue>.Lock)
                {
                    if (Caches.ForType<TKey, TValue>.Singleton == null)
                    {
                        Caches.ForType<TKey, TValue>.Singleton = CreateCacheInstance($"ConcurrentDictionary<{nameof(TKey)},{nameof(TValue)}>", configure);
                    }
                }
            }

            return Caches.ForType<TKey, TValue>.Singleton;
        }

        private static ConcurrentDictionary<TKey, TValue> CreateCacheInstance<TKey, TValue>(string name, Action<CacheSettings<TKey, TValue>>? configure)
        {
            var settings = new CacheSettings<TKey, TValue>(name);
            configure?.Invoke(settings);

            return new ConcurrentDictionary<TKey, TValue>(
                collection: settings.InitialValues ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>(),
                comparer: settings.Comparer ?? EqualityComparer<TKey>.Default);
        }
    }
}