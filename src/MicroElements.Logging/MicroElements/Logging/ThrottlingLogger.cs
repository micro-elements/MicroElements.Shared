using System;
using System.Collections.Generic;
using MicroElements.CodeContracts;
using Microsoft.Extensions.Logging;

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
        
        public ThrottlingLogger(ILogger logger, LoggerState loggerState)
        {
            _logger = logger.AssertArgumentNotNull(nameof(logger));
            _loggerState = loggerState.AssertArgumentNotNull(nameof(loggerState));
            
            _options = loggerState.Options.Combine(ThrottlingLoggerOptions.GetDefaultValues());
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

            // Message key for caching
            var messageKey = _options.GetMessageKey?.Invoke(message) ?? message;

            // Metrics for message
            var messageMetrics = _loggerState.MessageCache
                .GetOrAdd(messageKey, (msg, opt) => new MessageMetrics(msg, opt), _options)
                .Increment();
            
            if (ShouldWrite(messageMetrics))
            {
                // Activity
                // DiagnosticsSource
                // SessionLogger
                // lock?
                
                var totalAttempts = messageMetrics.TotalAttempts;
                var attemptRate = messageMetrics.AttemptRate;

                // AppendMetricsToScope
                using var logMetricsScope = _options.AppendMetricsToScope is true ? Scope.LogMetricsScope(_logger, totalAttempts) : null;
                
                // AppendMetricsToMessage
                if (_options.AppendMetricsToMessage is true)
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
                
                // Count as Success and log
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

            if (_options.ThrottlingPeriod is { } throttlingPeriod)
            {
                return metrics.DurationFromLastSuccess >= throttlingPeriod;
            }

            return true;
        }
    }
}