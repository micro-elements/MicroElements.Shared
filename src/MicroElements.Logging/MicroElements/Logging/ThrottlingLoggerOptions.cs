using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace MicroElements.Logging
{
    /// <summary>
    /// Options for customizing throttling.
    /// </summary>
    public class ThrottlingOptions : IThrottlingLoggerOptions
    {
        /// <summary>
        /// Gets or sets default values for all categories.
        /// Any value can be overriden in category or category group level.
        /// </summary>
        public ThrottlingLoggerOptions Default { get; } = new ()
        {
            CategoryName = null,
            MaxMessagesForCategory = 64,
            ThrottlingPeriod = TimeSpan.FromMinutes(1)
        };

        /// <summary>
        /// Gets category options.
        /// </summary>
        public List<ThrottlingLoggerOptions> Categories { get; } = new();
        
        #region IThrottlingLoggerOptions
        
        /// <summary>
        /// Category name or filter.
        /// </summary>
        public string CategoryName
        {
            get => Default.CategoryName;
            set => Default.CategoryName = value;
        }

        /// <inheritdoc />
        public Func<string, string>? GetMessageKey
        {
            get => Default.GetMessageKey;
            set => Default.GetMessageKey = value;
        }

        /// <inheritdoc />
        public Func<MessageMetrics, bool>? ShouldWrite
        {
            get => Default.ShouldWrite;
            set => Default.ShouldWrite = value;
        }

        /// <inheritdoc />
        public int? MaxMessagesForCategory
        {
            get => Default.MaxMessagesForCategory;
            set => Default.MaxMessagesForCategory = value;
        }

        /// <inheritdoc />
        public TimeSpan? ThrottlingPeriod
        {
            get => Default.ThrottlingPeriod;
            set => Default.ThrottlingPeriod = value;
        }

        /// <inheritdoc />
        public bool? AppendMetricsToMessage
        {
            get => Default.AppendMetricsToMessage;
            set => Default.AppendMetricsToMessage = value;
        }

        /// <inheritdoc />
        public bool? AppendMetricsToScope
        {
            get => Default.AppendMetricsToScope;
            set => Default.AppendMetricsToScope = value;
        }

        #endregion

        public ThrottlingOptions Clone()
        {
            var throttlingOptions = new ThrottlingOptions();
            throttlingOptions.ConfigureDefault(options => options.Set(Default));
            throttlingOptions.Categories.AddRange(Categories.Select(options => options.Combine(options)));
            
            return throttlingOptions;
        }
    }

    /// <summary>
    /// Throttling options for loggers. 
    /// </summary>
    public class ThrottlingLoggerOptions : IThrottlingLoggerOptions
    {
        public static ThrottlingLoggerOptions GetDefaultValues()
        {
            return new ThrottlingLoggerOptions
            {
                MaxMessagesForCategory = 64,
                ThrottlingPeriod = TimeSpan.FromMinutes(1)
            };
        }
        
        /// <summary>
        /// Category name or filter.
        /// </summary>
        public string? CategoryName { get; set; }
        
        /// <summary>
        /// Message key for matching log messages. Be default is the message itself.
        /// </summary>
        public Func<string, string>? GetMessageKey { get; set; }

        /// <summary>
        /// Gets or sets optional throttling strategy.
        /// </summary>
        public Func<MessageMetrics, bool>? ShouldWrite { get; set; }

        /// <summary>
        /// Gets or sets maximum original messages that can be stored in cache per category.
        /// </summary>
        public int? MaxMessagesForCategory { get; set; }
        
        /// <summary>
        /// Gets or sets the throttling period.
        /// </summary>
        public TimeSpan? ThrottlingPeriod { get; set; }
        
        /// <inheritdoc />
        public bool? AppendMetricsToMessage { get; set; }
        
        /// <inheritdoc />
        public bool? AppendMetricsToScope { get; set; }
    }
    
    internal sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T currentValue) => CurrentValue = currentValue;

        public IDisposable OnChange(Action<T, string> listener) => null!;

        public T Get(string name) => CurrentValue;

        public T CurrentValue { get; }
    }
}