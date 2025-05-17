using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prosim2GSX.Services.Logging.Events;
using Prosim2GSX.Services.Logging.Interfaces;
using Prosim2GSX.Services.Logging.Models;
using Prosim2GSX.Services.Logging.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Services.Logging.Implementation
{
    /// <summary>
    /// Implementation of IUiLogListener that captures logs for display in the UI
    /// </summary>
    public class UiLogListener : IUiLogListener
    {
        private readonly ILogger<UiLogListener> _logger;
        private readonly UiLoggerOptions _options;
        private readonly ConcurrentDictionary<string, LogLevel> _categoryLevels = new ConcurrentDictionary<string, LogLevel>();
        private readonly ConcurrentBag<LogMessage> _logMessages = new ConcurrentBag<LogMessage>();
        private LogLevel _defaultLogLevel = LogLevel.Information;
        private long _nextId = 1;

        /// <summary>
        /// Event raised when a log message is received
        /// </summary>
        public event EventHandler<LogReceivedEventArgs> LogReceived;

        /// <summary>
        /// Creates a new instance of the UiLogListener
        /// </summary>
        /// <param name="options">UI logger options</param>
        /// <param name="logger">Logger for this class</param>
        public UiLogListener(IOptions<UiLoggerOptions> options, ILogger<UiLogListener> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all buffered log messages
        /// </summary>
        public IReadOnlyList<LogMessage> GetBufferedMessages()
        {
            return _logMessages.OrderBy(m => m.Id).ToList();
        }

        /// <summary>
        /// Gets buffered log messages filtered by level
        /// </summary>
        public IReadOnlyList<LogMessage> GetFilteredMessages(LogLevel minimumLevel)
        {
            return _logMessages.Where(m => m.LogLevel >= minimumLevel).OrderBy(m => m.Id).ToList();
        }

        /// <summary>
        /// Clears all buffered log messages
        /// </summary>
        public void ClearBufferedMessages()
        {
            _logMessages.Clear();
        }

        /// <summary>
        /// Updates the minimum log level for a specific category
        /// </summary>
        public void SetCategoryLogLevel(string category, LogLevel level)
        {
            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentException("Category cannot be null or empty", nameof(category));
            }

            _categoryLevels[category] = level;
        }

        /// <summary>
        /// Updates the default minimum log level
        /// </summary>
        public void SetDefaultLogLevel(LogLevel level)
        {
            _defaultLogLevel = level;
        }

        /// <summary>
        /// Logs a message to the UI
        /// </summary>
        public void Log(string category, LogLevel logLevel, EventId eventId, string message, Exception exception = null)
        {
            // First check if we should log this message based on the category and level
            if (!ShouldLog(category, logLevel))
            {
                return;
            }

            // Create a new log message
            var logMessage = new LogMessage(
                id: System.Threading.Interlocked.Increment(ref _nextId),
                timestamp: DateTimeOffset.Now,
                logLevel: logLevel,
                category: category,
                eventId: eventId,
                message: message,
                exception: exception);

            // Add the message to the buffer, limiting the size
            _logMessages.Add(logMessage);

            // Trim the buffer if necessary
            if (_logMessages.Count > _options.MaxLogEntries)
            {
                // This is inefficient but happens rarely, and ConcurrentBag doesn't support easy removal
                var toKeep = _logMessages.OrderByDescending(m => m.Id).Take(_options.MaxLogEntries).ToList();
                _logMessages.Clear();
                foreach (var msg in toKeep)
                {
                    _logMessages.Add(msg);
                }
            }

            // Raise the LogReceived event
            OnLogReceived(new LogReceivedEventArgs(logMessage));
        }

        /// <summary>
        /// Determines if a message with the given category and level should be logged
        /// </summary>
        private bool ShouldLog(string category, LogLevel logLevel)
        {
            if (_categoryLevels.TryGetValue(category, out LogLevel categoryLevel))
            {
                return logLevel >= categoryLevel;
            }

            return logLevel >= _defaultLogLevel;
        }

        /// <summary>
        /// Raises the LogReceived event
        /// </summary>
        protected virtual void OnLogReceived(LogReceivedEventArgs e)
        {
            LogReceived?.Invoke(this, e);
        }

        // Add these methods to the UiLogListener class:

        /// <summary>
        /// Gets the effective log level for a specific category
        /// </summary>
        public LogLevel GetEffectiveLogLevel(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return _defaultLogLevel;
            }

            if (_categoryLevels.TryGetValue(category, out LogLevel level))
            {
                return level;
            }

            return _defaultLogLevel;
        }

        /// <summary>
        /// Handles a log message received from a logger
        /// </summary>
        public virtual void OnLogMessageReceived(string category, LogLevel level, EventId eventId, object state, Exception exception, string formattedMessage)
        {
            // Create a new log message
            var logMessage = new LogMessage(
                id: System.Threading.Interlocked.Increment(ref _nextId),
                timestamp: DateTimeOffset.Now,
                logLevel: level,
                category: category,
                eventId: eventId,
                message: formattedMessage,
                exception: exception);

            // Add the message to the buffer and raise event
            _logMessages.Add(logMessage);

            // Trim the buffer if necessary
            if (_logMessages.Count > _options.MaxLogEntries)
            {
                // This is inefficient but happens rarely, and ConcurrentBag doesn't support easy removal
                var toKeep = _logMessages.OrderByDescending(m => m.Id).Take(_options.MaxLogEntries).ToList();
                _logMessages.Clear();
                foreach (var msg in toKeep)
                {
                    _logMessages.Add(msg);
                }
            }

            // Raise the LogReceived event
            OnLogReceived(new LogReceivedEventArgs(logMessage));
        }

        // Add this method to process a LogMessage directly:
        /// <summary>
        /// Processes a received log message
        /// </summary>
        public void OnLogMessageReceived(LogMessage logMessage)
        {
            if (logMessage == null)
                return;

            // First check if we should log this message based on the category and level
            if (!ShouldLog(logMessage.Category, logMessage.LogLevel))
            {
                return;
            }

            // Create a new log message with an ID
            var idLogMessage = new LogMessage(
                id: System.Threading.Interlocked.Increment(ref _nextId),
                timestamp: logMessage.Timestamp,
                logLevel: logMessage.LogLevel,
                category: logMessage.Category,
                eventId: logMessage.EventId,
                message: logMessage.Message,
                exception: logMessage.Exception,
                scopeInformation: logMessage.ScopeInformation);

            // Add the message to the buffer, limiting the size
            _logMessages.Add(idLogMessage);

            // Trim the buffer if necessary
            if (_logMessages.Count > _options.MaxLogEntries)
            {
                // This is inefficient but happens rarely, and ConcurrentBag doesn't support easy removal
                var toKeep = _logMessages.OrderByDescending(m => m.Id).Take(_options.MaxLogEntries).ToList();
                _logMessages.Clear();
                foreach (var msg in toKeep)
                {
                    _logMessages.Add(msg);
                }
            }

            // Raise the LogReceived event
            OnLogReceived(new LogReceivedEventArgs(idLogMessage));
        }
    }
}
