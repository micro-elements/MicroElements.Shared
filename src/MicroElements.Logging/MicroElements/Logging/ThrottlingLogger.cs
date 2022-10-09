using System;
using System.Collections;
using System.Collections.Generic;
using MicroElements.CodeContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroElements.Logging
{
    /// <summary>
    /// Logger that throttles messages.
    /// </summary>
    internal class ThrottlingLogger : ILogger
    {
        private readonly ILogger _logger;
        private readonly ThrottlingLoggerOptions _options;
        private readonly LoggerState _loggerState;
        
        static class Scope
        {
            public static Func<ILogger,int,IDisposable> LogMetricsScope = LoggerMessage.DefineScope<int>("totalAttempts: {totalAttempts}");
        }
        
        public ThrottlingLogger(ILogger logger, IOptionsMonitor<ThrottlingLoggerOptions> options, LoggerState loggerCache)
        {
            logger.AssertArgumentNotNull();
            options.AssertArgumentNotNull();
            loggerCache.AssertArgumentNotNull();
                
            _logger = logger;
            _options = options.CurrentValue;
            _options.ThrottlingPeriod ??= TimeSpan.FromMinutes(1);
            _loggerState = loggerCache;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Log message
            var message = formatter(state, exception);
            
            // Metrics for message
            var messageMetrics = _loggerState.MessageCache
                .GetOrAdd(message, msg => new MessageMetrics(msg, _options.ThrottlingPeriodOrDefault))
                .Increment();
            
            
            if (ShouldWrite(messageMetrics))
            {
                // Activity
                // DiagnosticsSource
                // SessionLogger
                // lock?
                
                var totalAttempts = messageMetrics.TotalAttempts;
                var attemptRate = messageMetrics.AttemptRate;

                var appendMetricsToScope = _options.AppendMetricsToScope ?? false;
                var appendMetricsToMessage = _options.AppendMetricsToMessage ?? false;
                
                using var logMetricsScope = appendMetricsToScope ? Scope.LogMetricsScope(_logger, totalAttempts) : null;
                
                if (appendMetricsToMessage)
                {
                    if (state is IReadOnlyList<KeyValuePair<string, object>> logValues)
                    {
                        int newArgsToAdd = 1;
                        int valuesLength = logValues.Count - 1 + newArgsToAdd;
                        object[] values = new object[valuesLength];
                        int iVal = 0;
                        
                        string? originalFormat = null;
                        for (int i = 0; i < logValues.Count; i++)
                        {
                            var logValue = logValues[i];
                            if (logValue.Key == "{OriginalFormat}")
                            {
                                originalFormat = logValue.Value.ToString();
                                originalFormat = originalFormat + " | totalAttempts: {totalAttempts}";
                                continue;
                            }

                            values[iVal++] = logValue.Value;
                        }

                        // Add new args
                        values[iVal++] = totalAttempts;
                        //values[iVal++] = attemptRate;
                        
                        if (originalFormat != null)
                        {
                            messageMetrics.Success();
                            _logger.Log(logLevel, eventId, originalFormat, values);
                            return;
                        }
                    }
                }
                
                messageMetrics.Success();
                _logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        protected virtual bool ShouldWrite(MessageMetrics metrics)
        {
            if (_options.ShouldWrite != null)
            {
                // Use user provided condition.
                return _options.ShouldWrite(metrics);
            }

            if (metrics.TotalAttempts == 1)
            {
                // First attempt => always write.
                return true;
            }
            
            var throttlingPeriod = _options.ThrottlingPeriodOrDefault;
            return metrics.DurationFromLastSuccess >= throttlingPeriod;
        }
    }

    internal struct LogValues : IReadOnlyList<KeyValuePair<string, object>>
    {
        private readonly IReadOnlyList<KeyValuePair<string, object>> _values;

        /// <inheritdoc />
        public LogValues(IReadOnlyList<KeyValuePair<string, object>> values) : this()
        {
            _values = values;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _values.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public int Count => _values.Count;

        /// <inheritdoc />
        public KeyValuePair<string, object> this[int index] => _values[index];
    }
}