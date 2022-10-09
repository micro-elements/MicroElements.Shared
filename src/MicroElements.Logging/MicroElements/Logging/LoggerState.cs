using System;
using System.Linq;
using MicroElements.Collections.Extensions.WildCard;
using MicroElements.Collections.TwoLayerCache;
using Microsoft.Extensions.Options;

namespace MicroElements.Logging
{
    internal class LoggerState : IDisposable
    {
        private readonly IDisposable? _changeTokenRegistration;
        public string CategoryName { get; }
        public bool ShouldThrottle { get; private set; }

        internal readonly TwoLayerCache<string, MessageMetrics> MessageCache;

        public LoggerState(string categoryName, IOptionsMonitor<ThrottlingLoggerOptions> options)
        {
            CategoryName = categoryName;
            MessageCache = new TwoLayerCache<string, MessageMetrics>(maxItemCount: options.CurrentValue.MaxMessagesForCategory ?? 64);

            _changeTokenRegistration = options.OnChange(RefreshFilter);
            RefreshFilter(options.CurrentValue);
        }

        private void RefreshFilter(ThrottlingLoggerOptions options)
        {
            ShouldThrottle = false;
            
            if (options.CategoryFilters is { } categoryFilters)
            {
                //categoryFilters.Select(opt => opt.CategoryName).Inc
                var loggerOptions = categoryFilters.FirstOrDefault(categoryOptions => CategoryName.IsMatchesWildcard(categoryOptions.CategoryName));
                if (loggerOptions != null)
                {
                    ShouldThrottle = true;
                }
            }
        }

        public static LoggerState Create(string categoryName, IOptionsMonitor<ThrottlingLoggerOptions> options)
        {
            var loggerState = new LoggerState(categoryName, options);
            return loggerState;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _changeTokenRegistration?.Dispose();
        }
    }
}