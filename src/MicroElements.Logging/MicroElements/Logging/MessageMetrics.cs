using System;
using System.Threading;

namespace MicroElements.Logging
{
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
        public TimeSpan DurationFromLastAttempt => DateTime.Now - LastAttemptDateTime;
        
        /// <summary> Gets the duration from last successful attempt.  </summary>
        public TimeSpan DurationFromLastSuccess => DateTime.Now - LastSuccessDateTime;
        
        /// <summary> Gets the total attempts count. </summary>
        public int TotalAttempts => _totalAttempts;
        
        /// <summary> Gets the success attempts count. </summary>
        public int SuccessAttempts => _successAttempts;
        
        /// <summary> Gets the skipped attempts count. </summary>
        public int SkippedAttempts => _totalAttempts - _successAttempts;
        
        /// <summary> Gets the count of attempts from the last success. </summary>
        public int Attempts => _attempts;
        
        public double AttemptRate
        {
            get
            {
                var timeSpan = DateTime.Now - FirstAttemptDateTime;
                return _totalAttempts / timeSpan.TotalMinutes;
            }
        }
        
        public double AttemptRateFromSuccess => _totalAttempts / DurationFromLastSuccess.TotalMinutes;

        public MessageMetrics(string message, IThrottlingLoggerOptions options)
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
}