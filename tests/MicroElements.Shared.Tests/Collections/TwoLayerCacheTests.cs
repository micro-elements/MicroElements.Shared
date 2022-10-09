using System.Collections.Concurrent;
using FluentAssertions;
using MicroElements.Collections.TwoLayerCache;
using Xunit;

namespace MicroElements.Shared.Tests.Collections
{
    public class TwoLayerCacheTests
    {
        [Fact]
        public void CacheSwap()
        {
            var cache = new TwoLayerCache<string, string>(4);

            // Add 10 items to cold cache
            for (int i = 1; i <= 10; i++)
            {
                cache.GetOrAdd(i.ToString(), s => s).Should().Be(i.ToString());
            }

            cache.Metrics.ItemsAdded.Should().Be(10);
            cache.Metrics.ColdCacheItemsCount.Should().Be(10);
            cache.Metrics.HotCacheItemsCount.Should().Be(0);
            cache.Metrics.ColdCacheHit.Should().Be(0);
            cache.Metrics.HotCacheHit.Should().Be(0);
            cache.Metrics.SwapCount.Should().Be(0);

            // Get 4 items the second try
            for (int i = 1; i <= 4; i++)
            {
                cache.GetOrAdd(i.ToString(), s => s);
            }

            // 4 items moved to hot cache
            cache.Metrics.ItemsAdded.Should().Be(10);
            cache.Metrics.ColdCacheItemsCount.Should().Be(10);
            cache.Metrics.HotCacheItemsCount.Should().Be(4);
            cache.Metrics.ColdCacheHit.Should().Be(4);
            cache.Metrics.HotCacheHit.Should().Be(0);
            cache.Metrics.SwapCount.Should().Be(0);

            // Get 2 items the second try
            for (int i = 1; i <= 2; i++)
            {
                cache.GetOrAdd(i.ToString(), s => s);
            }

            // 2 hot hit
            cache.Metrics.ItemsAdded.Should().Be(10);
            cache.Metrics.ColdCacheItemsCount.Should().Be(10);
            cache.Metrics.HotCacheItemsCount.Should().Be(4);
            cache.Metrics.ColdCacheHit.Should().Be(4);
            cache.Metrics.HotCacheHit.Should().Be(2);
            cache.Metrics.SwapCount.Should().Be(0);

            // one more item from cold cache
            for (int i = 5; i <= 5; i++)
            {
                cache.GetOrAdd(i.ToString(), s => s);
            }

            // will get swap
            cache.Metrics.ItemsAdded.Should().Be(10);
            cache.Metrics.ColdCacheItemsCount.Should().Be(5, because: "Hot cache not is cold cache.");
            cache.Metrics.HotCacheItemsCount.Should().Be(0, because: "Hot cache should be recreated.");
            cache.Metrics.ColdCacheHit.Should().Be(5);
            cache.Metrics.HotCacheHit.Should().Be(2);
            cache.Metrics.SwapCount.Should().Be(1);
        }

        record TestValue(string Value);

        [Fact]
        public void CacheGet()
        {
            TwoLayerCache<string, TestValue> cache = new ();

            var value1 = cache.GetOrAdd("value1", s => new TestValue(s));
            var value2 = cache.GetOrAdd("value2", s => new TestValue(s));

            value1.Should().Be(new TestValue("value1"));
            value1.Should().NotBeSameAs(new TestValue("value1"));

            cache.TryGetValue("value1", out var value1FromCache);
            value1FromCache.Should().BeSameAs(value1);
        }
        
        [Fact]
        public void CacheGet2()
        {
            ConcurrentDictionary<string, string> dictionary = new ConcurrentDictionary<string, string>();
            for (int i = 0; i < 31; i++)
            {
                dictionary.GetOrAdd($"key{i}", $"value{i}");
            }
            
        }   
    }
}