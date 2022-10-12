using System;
using MicroElements.CodeContracts;
using MicroElements.Collections.TwoLayerCache;

namespace MicroElements.Logging
{
    /// <summary>
    /// Per logger or category state that holds message cache and other logger related things.
    /// </summary>
    internal class LoggerState
    {
        public static LoggerState NoThrottle = new ("[NoThrottle]", ThrottlingLoggerOptions.GetDefaultValues());
            
        private readonly Lazy<TwoLayerCache<string, MessageMetrics>> _messageCache;
        
        public string CategoryName { get; }
        
        public ThrottlingLoggerOptions Options { get; }
        
        public TwoLayerCache<string, MessageMetrics> MessageCache => _messageCache.Value;

        public LoggerState(string categoryName, ThrottlingLoggerOptions options)
        {
            CategoryName = categoryName.AssertArgumentNotNull(nameof(categoryName));
            Options = options.AssertArgumentNotNull(nameof(options));

            _messageCache = new Lazy<TwoLayerCache<string, MessageMetrics>>(() =>
                new TwoLayerCache<string, MessageMetrics>(maxItemCount: options.MaxMessagesForCategory ?? 64));
        }

        /// <inheritdoc />
        public override string ToString() => $"{CategoryName}";
    }
}