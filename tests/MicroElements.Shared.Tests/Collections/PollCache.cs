
namespace MicroElements.Shared.Tests.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;
    using Xunit.Abstractions;
    using MicroElements.Collections.Caching;

    public class ValueProvider
    {
        private readonly string _value;
        private readonly int _delayInMilliseconds;

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

    public class PollingTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public PollingTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Main()
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
                await Task.Delay(1000);
                _testOutputHelper.WriteLine(result);
            }


            results.Distinct().Count().Should().Be(3);
        }

        //[Fact]
        public async Task TimeToLiveWithEviction()
        {
            var storageManager = new ValueProvider("test", delayInMilliseconds: 1);
            var twoLayerCache = PollingCache.GetCache<ValueProvider, string, DateTime, string>(maxItemsCount: 3);
            twoLayerCache.TryGetValue(default, out var aa);

            async Task<string> Operation(string ten)
            {
                var result = await storageManager.GetCachedAsync(ten, DateTime.Today,
                    (manager, tenant, date) => manager.GetValue(tenant, date),
                    timeToLive: TimeSpan.FromSeconds(2));

                result.Should().StartWith("test");
                return result;
            }

            List<string> results = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                var result = await Operation($"tenant_{i}");
                results.Add(result);
                await Task.Delay(1000);
                _testOutputHelper.WriteLine(result);
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

