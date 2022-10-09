using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MicroElements.Logging
{
    public static class ThrottlingLoggerExtensions
    {
        private static readonly ConcurrentDictionary<ILoggerFactory, ThrottlingLoggerFactory> _loggerFactories = new();
            
        public static ILoggerFactory WithThrottling(this ILoggerFactory loggerFactory, Action<ThrottlingLoggerOptions>? configure = null)
        {
            return _loggerFactories.GetOrAdd(loggerFactory, static (f, c) => WithThrottlingInPlace(f, c), configure);
        }
        
        public static ThrottlingLoggerFactory WithThrottlingInPlace(this ILoggerFactory loggerFactory, Action<ThrottlingLoggerOptions>? configure = null)
        {
            var options = new ThrottlingLoggerOptions();
            configure?.Invoke(options);

            return new ThrottlingLoggerFactory(loggerFactory, options);
        }
        
        public static IServiceCollection AddThrottlingLogging(this IServiceCollection services, Action<ThrottlingLoggerOptions>? configure = null)
        {
            if (configure != null)
                services.Configure<ThrottlingLoggerOptions>(configure);
            services.Decorate<ILoggerFactory>(factory => factory.WithThrottling(configure));
            return services;
        }
        
        public static ILogger WithThrottlingInPlace(this ILogger logger, Action<ThrottlingLoggerOptions>? configure = null)
        {
            var options = new ThrottlingLoggerOptions();
            configure?.Invoke(options);
            var optionsMonitor = new StaticOptionsMonitor<ThrottlingLoggerOptions>(options);
            return new ThrottlingLogger(logger, optionsMonitor, LoggerState.Create("[InPlaceThrottling]", optionsMonitor));
        }
    }
}