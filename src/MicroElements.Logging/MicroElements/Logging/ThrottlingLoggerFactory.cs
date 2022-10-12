using System.Collections.Concurrent;
using MicroElements.Collections.Extensions.WildCard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroElements.Logging
{
    /// <summary>
    /// LoggerFactory that creates ThrottlingLoggers.
    /// </summary>
    public class ThrottlingLoggerFactory : ILoggerFactory
    {
        private static readonly ConcurrentDictionary<string, LoggerState> _loggerCaches = new();
        
        private readonly IOptionsMonitor<ThrottlingOptions> _options;
        
        /// <summary>
        /// Gets wrapped logger factory.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets throttling options.
        /// </summary>
        public ThrottlingOptions Options => _options.CurrentValue;

        /// <summary>
        /// Creates a new <see cref="ThrottlingLoggerFactory"/> instance.
        /// </summary>
        /// <param name="loggerFactory">The decorated factory instance.</param>
        /// <param name="options">The options to use.</param>
        public ThrottlingLoggerFactory(ILoggerFactory loggerFactory, ThrottlingOptions options)
            : this(loggerFactory, new StaticOptionsMonitor<ThrottlingOptions>(options))
        {
        }
        
        /// <summary>
        /// Creates a new <see cref="ThrottlingLoggerFactory"/> instance.
        /// </summary>
        /// <param name="loggerFactory">The decorated factory instance.</param>
        /// <param name="options">The options to use.</param>
        [ActivatorUtilitiesConstructor]
        public ThrottlingLoggerFactory(ILoggerFactory loggerFactory, IOptionsMonitor<ThrottlingOptions> options)
        {
            LoggerFactory = loggerFactory;
            _options = options;
        }

        /// <inheritdoc />
        public void Dispose() => LoggerFactory.Dispose();

        /// <inheritdoc />
        public void AddProvider(ILoggerProvider provider) => LoggerFactory.AddProvider(provider);

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            var loggerState = _loggerCaches.GetOrAdd(categoryName, GetLoggerState, _options.CurrentValue);
            
            var logger = LoggerFactory.CreateLogger(categoryName);
            
            if (loggerState != LoggerState.NoThrottle)
            {
                return new ThrottlingLogger(logger, loggerState);
            }
            
            return logger;
        }
        
        private static LoggerState GetLoggerState(string categoryName, ThrottlingOptions throttlingOptions)
        {
            ThrottlingLoggerOptions? categoryOptions = throttlingOptions.GetBestMatch(categoryName);

            if (categoryOptions != null)
            {
                if (categoryOptions != throttlingOptions.Default)
                {
                    categoryOptions = categoryOptions.Combine(throttlingOptions.Default);
                }

                return new LoggerState(categoryName, categoryOptions);
            }

            return LoggerState.NoThrottle;
        }
    }
}