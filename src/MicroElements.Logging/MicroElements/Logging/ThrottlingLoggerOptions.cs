using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace MicroElements.Logging
{
    public class ThrottlingLoggerOptions2
    {
        public ThrottlingLoggerOptions Default { get; set; }
        public List<ThrottlingLoggerOptions> Categories { get; } = new();
    }

    /// <summary>
    /// Throttling options for loggers. 
    /// </summary>
    public class ThrottlingLoggerOptions
    {
        /// <summary>
        /// Category name or filter.
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// Gets or sets optional throttling strategy.
        /// </summary>
        public Func<MessageMetrics, bool>? ShouldWrite { get; set; }

        /// <summary>
        /// Gets or sets maximum original messages that can be stored in cache per category.
        /// </summary>
        public int? MaxMessagesForCategory { get; set; } = 64;
        
        /// <summary>
        /// Gets or sets the throttling period.
        /// </summary>
        public TimeSpan? ThrottlingPeriod { get; set; }
        
        public TimeSpan ThrottlingPeriodOrDefault => ThrottlingPeriod ?? TimeSpan.FromMinutes(1);

        public bool? AppendMetricsToMessage { get; set; }
        
        public bool? AppendMetricsToScope { get; set; }
        
        public List<ThrottlingLoggerOptions> CategoryFilters { get; } = new();

        public ThrottlingLoggerOptions ThrottleCategory(string category, Action<ThrottlingLoggerOptions>? configure = null)
        {
            var categoryOptions = new ThrottlingLoggerOptions { CategoryName = category };
            configure?.Invoke(categoryOptions);
            CategoryFilters.Add(categoryOptions);
            return this;
        }
    }
    
    internal sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T currentValue) => CurrentValue = currentValue;

        public IDisposable OnChange(Action<T, string> listener) => null!;

        public T Get(string name) => CurrentValue;

        public T CurrentValue { get; }
    }
}