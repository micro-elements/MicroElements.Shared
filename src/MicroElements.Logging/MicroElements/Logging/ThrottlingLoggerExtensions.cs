using System;
using System.Collections.Concurrent;
using MicroElements.Collections.Cache;
using MicroElements.Collections.TwoLayerCache;
using Microsoft.Extensions.Logging;

namespace MicroElements.Logging
{
    public static class ThrottlingLoggerExtensions
    {
        public static ILoggerFactory WithThrottling(this ILoggerFactory loggerFactory, Action<ThrottlingOptions>? configure = null)
        {
            if (loggerFactory is ThrottlingLoggerFactory throttlingLoggerFactory)
            {
                var wrappedFactory = throttlingLoggerFactory.LoggerFactory;
                var throttlingOptionsCopy = throttlingLoggerFactory.Options.Clone();
                
                return Cache
                    .Instance<ILoggerFactory, ThrottlingLoggerFactory>()
                    .GetOrAdd(wrappedFactory, static (_, state) =>
                    {
                        // Reconfigure existing ThrottlingOptions
                        state.Configure?.Invoke(state.ThrottlingOptions);
                        return new ThrottlingLoggerFactory(state.WrappedFactory, state.ThrottlingOptions);
                    }, (WrappedFactory: wrappedFactory, ThrottlingOptions: throttlingOptionsCopy, Configure: configure));
            }

            return Cache
                .Instance<ILoggerFactory, ThrottlingLoggerFactory>()
                .GetOrAdd(loggerFactory, static (_, state) =>
                {
                    var throttlingOptions = new ThrottlingOptions();
                    state.Configure?.Invoke(throttlingOptions);
                    return new ThrottlingLoggerFactory(state.LoggerFactory, throttlingOptions);
                }, (LoggerFactory: loggerFactory, Configure: configure));
        }

        public static ILogger WithThrottling(this ILogger logger, Action<ThrottlingLoggerOptions>? configure = null)
        {
            return TwoLayerCache
                .Instance<ILogger, ThrottlingLogger>()
                .GetOrAdd(logger, static (_, state) =>
                {
                    var options = ThrottlingLoggerOptions.GetDefaultValues();
                    state.Configure?.Invoke(options);
                    return new ThrottlingLogger(state.Logger, new LoggerState(options.CategoryName ?? "*", options));
                }, (Logger: logger, Configure: configure));
        }
    }
}