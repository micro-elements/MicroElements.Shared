using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MicroElements.CodeContracts;
using MicroElements.Collections.TwoLayerCache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Internal;

namespace MicroElements.Logging
{
    public class ThrottlingLoggerFactory : ILoggerFactory
    {
        private static readonly ConcurrentDictionary<string, LoggerCache> _LoggerCaches = new();

        private readonly ILoggerFactory _loggerFactory;
        private readonly ThrottlingLoggerOptions _options;

        public ThrottlingLoggerFactory(
            ILoggerFactory loggerFactory,
            ThrottlingLoggerOptions options)
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
            var loggerCache = _LoggerCaches.GetOrAdd(categoryName, static (c,o) => new LoggerCache(o), _options);
            return new ThrottlingLogger(logger, _options, loggerCache);
        }
    }

    /// <summary>
    /// Throttling options for loggers. 
    /// </summary>
    public class ThrottlingLoggerOptions
    {
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
        public TimeSpan? ThrottlingPeriod { get; set; } = TimeSpan.FromMinutes(1);

        public bool AppendMetricsToMessage { get; set; } = true;

        public Func<LogLevel, EventId, object, Exception?, string> formatter;
    }

    internal class LoggerCache
    {
        internal readonly TwoLayerCache<string, MessageMetrics> MessageCache;

        public LoggerCache(ThrottlingLoggerOptions options)
        {
            MessageCache = new TwoLayerCache<string, MessageMetrics>(maxItemCount: options.MaxMessagesForCategory ?? 64);
        }
    }

    /// <summary>
    /// Represents metrics on per message basis.
    /// </summary>
    public class MessageMetrics
    {
        private int _totalAttempts;
        private int _successAttempts;
        private int _attempts;
        
        private long _lastSuccessDateTime;
        private long _lastAttemptDateTime;
        
        /// <summary> The log message. </summary>
        public string Message { get; }
        
        /// <summary> Gets the date and time when the first message was occured. </summary>
        public DateTime FirstAttemptDateTime { get; }
        
        /// <summary> Gets the date and time when the last attempt was occured. </summary>
        public DateTime LastAttemptDateTime => new DateTime(ticks: _lastAttemptDateTime);

        /// <summary> Gets the date and time when the last successful attempt was occured. </summary>
        public DateTime LastSuccessDateTime => new DateTime(ticks: _lastSuccessDateTime);

        /// <summary> Gets the duration from last attempt.  </summary>
        public TimeSpan DurationFromLastAttempt => LastAttemptDateTime - DateTime.Now;
        
        /// <summary> Gets the duration from last successful attempt.  </summary>
        public TimeSpan DurationFromLastSuccess => LastSuccessDateTime - DateTime.Now;
        
        /// <summary> Gets the total attempts count. </summary>
        public int TotalAttempts => _totalAttempts;
        
        /// <summary> Gets the success attempts count. </summary>
        public int SuccessAttempts => _successAttempts;
        
        /// <summary> Gets the skipped attempts count. </summary>
        public int SkippedAttempts => _totalAttempts - _successAttempts;
        
        /// <summary> Gets the count of attempts from the last success. </summary>
        public int Attempts => _attempts;
        
        public int AttemptRate => 0;
        
        public MessageMetrics(string message)
        {
            Message = message;
            FirstAttemptDateTime = DateTime.Now;
            _lastSuccessDateTime = FirstAttemptDateTime.Ticks;
            _attempts = 0;
        }

        internal MessageMetrics Increment()
        {
            Interlocked.Increment(ref _totalAttempts);
            Interlocked.Increment(ref _attempts);
            Interlocked.Exchange(ref _lastAttemptDateTime, DateTime.Now.Ticks);
            return this;
        }
        
        internal MessageMetrics Success()
        {
            Interlocked.Increment(ref _successAttempts);
            Interlocked.Exchange(ref _attempts, 0);
            Interlocked.Exchange(ref _lastSuccessDateTime, DateTime.Now.Ticks);
            return this;
        }    
    }

    /// <summary>
    /// Logger that throttles messages.
    /// </summary>
    internal class ThrottlingLogger : ILogger
    {
        private readonly ILogger _logger;
        private readonly ThrottlingLoggerOptions _options;
        private readonly LoggerCache _loggerCache;

        public ThrottlingLogger(ILogger logger, ThrottlingLoggerOptions options, LoggerCache? loggerCache = null)
        {
            _logger = logger;
            _options = options;
            _loggerCache = loggerCache ?? new LoggerCache(options);
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Log message
            var message = formatter(state, exception);// state?.ToString() ?? string.Empty;
            
            // Metrics for message
            var messageMetrics = _loggerCache.MessageCache
                .GetOrAdd(message, msg => new MessageMetrics(msg))
                .Increment();
            
            if (ShouldWrite(messageMetrics))
            {
                var totalAttempts = messageMetrics.TotalAttempts;
                var attempts = messageMetrics.Attempts;
                
                // Activity
                // DiagnosticsSource
                
                //IReadOnlyList<KeyValuePair<string, object>>
                //SessionLogger
                
                //AppendMetricsToMessage
                
                //_logger.BeginScope("TotalAttempts: {totalAttempts}, ", totalAttempts);

                if (_options.AppendMetricsToMessage)
                {
                    if (state is IReadOnlyList<KeyValuePair<string, object>> logValues)
                    {
                        var newLogValues = new List<KeyValuePair<string, object>>(capacity: logValues.Count + 1);
                        newLogValues.AddRange(logValues);
                        
                        //"{OriginalFormat}" + addond
                        
                        newLogValues.Add(new KeyValuePair<string, object>("totalAttempts", messageMetrics.TotalAttempts));

                        //state = newLogValues;
                    }
                }
                
                //lock?
                messageMetrics.Success();
                _logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        protected virtual bool ShouldWrite(MessageMetrics metrics)
        {
            if (_options.ShouldWrite != null)
            {
                return _options.ShouldWrite(metrics);
            }

            if (metrics.TotalAttempts == 1)
                return true;
            
            var throttlingPeriod = _options.ThrottlingPeriod ?? TimeSpan.FromMinutes(1);
            return metrics.DurationFromLastSuccess >= throttlingPeriod;
        }
    }

    public static class OptimizedLoggerExtensions
    {
        private static ConcurrentDictionary<ILoggerFactory, ThrottlingLoggerFactory> _loggerFactories = new();
            
        public static ILoggerFactory WithWriteLimitCached(this ILoggerFactory loggerFactory, Action<ThrottlingLoggerOptions>? configure = null)
        {
            return _loggerFactories.GetOrAdd(loggerFactory, static (f, c) => WithWriteLimit(f, c), configure);
        }
        
        public static ThrottlingLoggerFactory WithWriteLimit(this ILoggerFactory loggerFactory, Action<ThrottlingLoggerOptions>? configure = null)
        {
            var options = new ThrottlingLoggerOptions();
            configure?.Invoke(options);

            return new ThrottlingLoggerFactory(loggerFactory, options);
        }

        public static IServiceCollection AddLoggingWithWriteLimit(IServiceCollection services, Action<ThrottlingLoggerOptions>? configure = null)
        {
            if (configure != null)
                services.Configure<ThrottlingLoggerOptions>(configure);
            services.Decorate<ILoggerFactory>(factory => factory.WithWriteLimitCached(configure));
            return services;
        }
        
        /// <summary>
        /// Simple Decorate implementation.
        /// </summary>
        /// <param name="services">The source service collection.</param>
        /// <param name="decorate"></param>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        internal static IServiceCollection Decorate<TService>(
            this IServiceCollection services,
            Func<TService, TService> decorate)
            where TService : class
        {
            services.AssertArgumentNotNull();
            decorate.AssertArgumentNotNull();
            
            var servicesToDecorate = services.Where(descriptor => descriptor.ServiceType == typeof(TService));

            foreach (var serviceToDecorate in servicesToDecorate)
            {
                var index = services.IndexOf(serviceToDecorate);
                
                ServiceDescriptor CreateServiceDescriptor(Func<IServiceProvider, TService> func) =>
                    ServiceDescriptor.Describe(typeof(TService), func, serviceToDecorate.Lifetime);

                ServiceDescriptor? serviceDescriptor = null;
                if (serviceToDecorate.ImplementationFactory != null)
                {
                    serviceDescriptor = CreateServiceDescriptor(provider =>
                    {
                        var service = (TService)serviceToDecorate.ImplementationFactory(provider);
                        return decorate(service);
                    });
                }
                else if (serviceToDecorate.ImplementationInstance != null)
                {
                    serviceDescriptor = CreateServiceDescriptor(provider =>
                    {
                        var service = (TService)serviceToDecorate.ImplementationInstance;
                        return decorate(service);
                    });
                }
                else if (serviceToDecorate.ImplementationType != null)
                {
                    serviceDescriptor = CreateServiceDescriptor(provider =>
                    {
                        var service = (TService)ActivatorUtilities.CreateInstance(provider, serviceToDecorate.ImplementationType);
                        return decorate(service);
                    });
                }

                if (serviceDescriptor != null)
                    services[index] = serviceDescriptor;
            }
            
            return services;
        }
    }

    public static class Usage
    {
        public static void Use()
        {
            ILoggerFactory loggerFactory = NullLoggerFactory.Instance.WithWriteLimitCached();

            ILogger logger;
            
            //new ThrottlingLogger(logger, new LoggerCache())
        }
    }
}