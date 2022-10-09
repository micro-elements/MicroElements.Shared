using System.Collections.Concurrent;
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

        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptionsMonitor<ThrottlingLoggerOptions> _options;

        /// <summary>
        /// Creates a new <see cref="ThrottlingLoggerFactory"/> instance.
        /// </summary>
        /// <param name="loggerFactory">The decorated factory instance.</param>
        /// <param name="options">The options to use.</param>
        public ThrottlingLoggerFactory(ILoggerFactory loggerFactory, ThrottlingLoggerOptions options)
            : this(loggerFactory, new StaticOptionsMonitor<ThrottlingLoggerOptions>(options))
        {
        }

        /// <summary>
        /// Creates a new <see cref="ThrottlingLoggerFactory"/> instance.
        /// </summary>
        /// <param name="loggerFactory">The decorated factory instance.</param>
        /// <param name="options">The options to use.</param>
        public ThrottlingLoggerFactory(ILoggerFactory loggerFactory, IOptionsMonitor<ThrottlingLoggerOptions> options)
        {
            _loggerFactory = loggerFactory;
            _options = options;
        }

        /// <inheritdoc />
        public void Dispose() => _loggerFactory.Dispose();

        /// <inheritdoc />
        public void AddProvider(ILoggerProvider provider) => _loggerFactory.AddProvider(provider);

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            var logger = _loggerFactory.CreateLogger(categoryName);
            
            var loggerState = _loggerCaches.GetOrAdd(categoryName, static (c,o) => LoggerState.Create(c, o), _options);
            
            if (loggerState.ShouldThrottle)
                return new ThrottlingLogger(logger, _options, loggerState);
            
            return logger;
        }
    }
}