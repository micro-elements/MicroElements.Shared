#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable CheckNamespace

#endregion

namespace MicroElements.Collections.Cache
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
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
        private static readonly ConcurrentDictionary<string, object> _caches = new();

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
        internal static ConcurrentDictionary<TKey, TValue> Instance<TKey, TValue>(string name, Action<CacheSettings<TKey, TValue>>? configure = null)
        {
            return (ConcurrentDictionary<TKey, TValue>)_caches.GetOrAdd(name, static (name, action) => CreateCacheInstance(name, action), configure);

            static ConcurrentDictionary<TKey, TValue> CreateCacheInstance(string name, Action<CacheSettings<TKey, TValue>>? configure)
            {
                var settings = new CacheSettings<TKey, TValue>(name);
                configure?.Invoke(settings);

                return new ConcurrentDictionary<TKey, TValue>(
                    collection: settings.InitialValues ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>(),
                    comparer: settings.Comparer ?? EqualityComparer<TKey>.Default);
            }
        }
    }

    public static class CacheExample
    {
        public static void test()
        {
            var value1 = Cache.Instance<string, string>("Example").GetOrAdd("key1", k => "value1");
        }
    }
}