using Microsoft.Extensions.Logging;
using System;

namespace Prosim2GSX.Services.Logging.Models
{
    /// <summary>
    /// Log message structure for UI display
    /// </summary>
    public class LogMessage
    {
        /// <summary>
        /// Gets the unique ID of this log message
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Gets the timestamp of this log message
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the log level of this message
        /// </summary>
        public LogLevel LogLevel { get; }

        /// <summary>
        /// Gets the category of this log message
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the event ID of this log message
        /// </summary>
        public EventId EventId { get; }

        /// <summary>
        /// Gets the message text
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the exception associated with this log message, if any
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the scope information for this log message, if any
        /// </summary>
        public string ScopeInformation { get; }

        /// <summary>
        /// Initializes a new instance of the LogMessage class
        /// </summary>
        public LogMessage(
            long id,
            DateTimeOffset timestamp,
            LogLevel logLevel,
            string category,
            EventId eventId,
            string message,
            Exception exception = null,
            string scopeInformation = null)
        {
            Id = id;
            Timestamp = timestamp;
            LogLevel = logLevel;
            Category = category;
            EventId = eventId;
            Message = message;
            Exception = exception;
            ScopeInformation = scopeInformation;
        }

        /// <summary>
        /// Gets a formatted representation of this log message suitable for display
        /// </summary>
        public override string ToString()
        {
            string levelString = LogLevel switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "CRIT",
                _ => "NONE"
            };

            string result = $"[{Timestamp:HH:mm:ss.fff}] [{levelString}] {Category}: {Message}";

            if (!string.IsNullOrEmpty(ScopeInformation))
            {
                result += $" => {ScopeInformation}";
            }

            if (Exception != null)
            {
                result += $"{Environment.NewLine}{Exception}";
            }

            return result;
        }
    }
}
