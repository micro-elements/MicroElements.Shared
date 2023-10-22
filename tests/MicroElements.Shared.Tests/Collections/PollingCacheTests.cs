using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using System.Threading;
using MicroElements.Collections.PollingCache;

namespace MicroElements.Shared.Tests.Collections
{
    public class ValueProvider
    {
        private readonly string _value;
        private readonly int _delayInMilliseconds;

        /// <inheritdoc />
        public override string ToString() => $"{_value}";

        public ValueProvider(string value, int delayInMilliseconds = 1000)
        {
            _value = value;
            _delayInMilliseconds = delayInMilliseconds;
        }

        public async Task<string> GetValue(string tenant, DateTime lookupDate)
        {
            await Task.Delay(_delayInMilliseconds);
            return $"{_value}_{DateTime.Now:s}";
        }
    }

    public class PollingCacheTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public PollingCacheTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task parallel_access_to_cache()
        {
            var storageManager = new ValueProvider("test", delayInMilliseconds: 1000);

            async Task<string> Operation()
            {
                var result = await storageManager.GetCachedAsync("default", DateTime.Today,
                    (manager, ten, date) => manager.GetValue(ten, date),
                    timeToLive: TimeSpan.FromSeconds(3));

                return result;
            }

            Task<string>[] tasks = Enumerable
                .Repeat(Operation(), 10)
                .ToArray();

            await Task.WhenAll(tasks);
            var results = tasks.Select(task => task.Result).ToArray();
            results.Distinct().Count().Should().Be(1);
        }

        [Fact]
        public async Task sequential_access_to_cache()
        {
            var storageManager = new ValueProvider("test", delayInMilliseconds: 1000);

            async Task<string> Operation()
            {
                var result = await storageManager.GetCachedAsync("default", DateTime.Today,
                    (manager, ten, date) => manager.GetValue(ten, date),
                    timeToLive: TimeSpan.FromSeconds(3));

                return result;
            }

            string[] results = new string[10];

            for (int i = 0; i < 10; i++)
            {
                results[i] = await Operation();
            }

            results.Distinct().Count().Should().Be(1);
        }

        [Fact]
        public async Task TimeToLive()
        {
            var storageManager = new ValueProvider("test", delayInMilliseconds: 1);

            async Task<string> Operation()
            {
                var result = await storageManager.GetCachedAsync("default", DateTime.Today,
                    (manager, tenant, date) => manager.GetValue(tenant, date),
                    timeToLive: TimeSpan.FromSeconds(2));

                result.Should().StartWith("test");
                return result;
            }

            List<string> results = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                var result = await Operation();
                results.Add(result);
                _testOutputHelper.WriteLine(result);
                await Task.Delay(1000);
            }

            results.Distinct().Count().Should().Be(3);
        }

        [Fact]
        public async Task CacheWithError()
        {
            var storageManager = new ValueProvider("test", delayInMilliseconds: 1000);

            int throwException = 2;
            async Task<string> Operation()
            {
                await Task.Yield();

                var result = await storageManager.GetCachedAsync("default", DateTime.Today,
                    async (manager, ten, date) =>
                    {
                        await Task.Delay(1000);
                        if (Volatile.Read(ref throwException) > 0)
                        {
                            Interlocked.Decrement(ref throwException);
                            _testOutputHelper.WriteLine("Temp error");
                            throw new Exception("Temp error");
                        }
                        var value = await manager.GetValue(ten, date);
                        _testOutputHelper.WriteLine($"Got real value '{value}'");
                        return value;
                    },
                    afterFactory: cacheValue =>
                    {
                        if (cacheValue.Exception != null)
                        {
                            _testOutputHelper.WriteLine("Reset on error");
                            cacheValue.Reset();
                            cacheValue.SetExpireAfter(TimeSpan.FromSeconds(1));
                        }
                    },
                    processResult: async cacheValue =>
                    {
                        if (cacheValue.Exception != null)
                        {
                            return null;
                        }

                        return cacheValue.Value;
                    },
                timeToLive: TimeSpan.FromSeconds(600));

                _testOutputHelper.WriteLine($"Result: '{result}'");

                return result;
            }

            Task<string>[] tasks =
            {
                Operation(),
                Operation(),
                Operation(),
                Operation(),
                Operation()
            };

            await Task.WhenAll(tasks);
            var results = tasks.Select(task => task.Result).ToArray();
            results.Where(s => s is null).Count().Should().Be(2);
            results.Where(s => s != null).Count().Should().Be(3);
        }

        [Fact]
        public async Task TimeToLiveWithEviction()
        {
            var storageManager = new ValueProvider("test", delayInMilliseconds: 1);
            var twoLayerCache = PollingCache.GetCache<(ValueProvider, string, DateTime), string>(cacheName: "TimeToLiveWithEviction", maxItemsCount: 3);
            twoLayerCache.TryGetValue(default, out var aa);

            async Task<string> Operation(string ten)
            {
                var result = await storageManager.GetCachedAsync(ten, DateTime.Today,
                    (manager, tenant, date) => manager.GetValue(tenant, date),
                    timeToLive: TimeSpan.FromSeconds(2),
                    cacheName: "TimeToLiveWithEviction");

                result.Should().StartWith("test");
                return result;
            }

            List<string> results = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                var result = await Operation($"tenant_{i}");
                results.Add(result);
                _testOutputHelper.WriteLine(result);
                await Task.Delay(1000);
            }

            var cacheMetrics = twoLayerCache.Metrics;

            cacheMetrics.ColdCacheItemsCount.Should().Be(3);
            results.Distinct().Count().Should().Be(8);
        }
    }

    public class TestException
    {
        [Fact]
        public static void Main2()
        {
            void Foo() => throw new InvalidOperationException ("foo");

            Exception original = null;
            ExceptionDispatchInfo dispatchInfo = null;
            try
            {
                try
                {
                    Foo();
                }
                catch (Exception ex)
                {
                    original = ex;
                    dispatchInfo = ExceptionDispatchInfo.Capture (ex);
                    throw ex;
                }
            }
            catch (Exception ex2)
            {
                // ex2 is the same object as ex. But with a mutated StackTrace.
                Console.WriteLine (ex2 == original);  // True
            }

// So now "original" has lost the StackTrace containing "Foo":
            Console.WriteLine (original.StackTrace.Contains ("Foo"));  // False

// But dispatchInfo still has it:
            try
            {
                dispatchInfo.Throw ();
            }
            catch (Exception ex)
            {
                Console.WriteLine (ex.StackTrace.Contains ("Foo"));   // True
            }
        }
    }
}

