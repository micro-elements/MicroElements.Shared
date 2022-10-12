using System;

namespace MicroElements.Logging
{
    /// <summary>
    /// Throttling options for loggers. 
    /// </summary>
    public interface IThrottlingLoggerOptions
    {
        /// <summary>
        /// Gets category name or filter.
        /// </summary>
        public string? CategoryName { get; }
        
        /// <summary>
        /// Message key for matching log messages. Be default is the message itself.
        /// </summary>
        Func<string, string>? GetMessageKey { get; }

        /// <summary>
        /// Gets or sets optional throttling strategy.
        /// </summary>
        Func<MessageMetrics, bool>? ShouldWrite { get; }

        /// <summary>
        /// Gets or sets maximum original messages that can be stored in cache per category.
        /// </summary>
        int? MaxMessagesForCategory { get; }
        
        /// <summary>
        /// Gets or sets the throttling period.
        /// </summary>
        TimeSpan? ThrottlingPeriod { get; }
        
        /// <summary>
        /// Gets the value indication whether or not metrics should be appended to log message.
        /// </summary>
        bool? AppendMetricsToMessage { get; }
        
        /// <summary>
        /// Gets the value indication whether or not metrics should be appended to log scope.
        /// </summary>
        bool? AppendMetricsToScope { get; }
    }
}