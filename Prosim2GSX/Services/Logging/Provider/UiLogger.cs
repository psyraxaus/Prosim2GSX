using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prosim2GSX.Services.Logging.Implementation;
using Prosim2GSX.Services.Logging.Interfaces;
using Prosim2GSX.Services.Logging.Models;
using System;
using System.Text;

namespace Prosim2GSX.Services.Logging.Provider
{
    /// <summary>
    /// Logger implementation for UI logging
    /// </summary>
    internal class UiLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly IUiLogListener _listener;
        private readonly UiLoggerScopeProvider _scopeProvider;
        private readonly IExternalScopeProvider _externalScopeProvider;

        /// <summary>
        /// Initializes a new instance of the UiLogger class
        /// </summary>
        public UiLogger(
            string categoryName,
            IUiLogListener listener,
            IExternalScopeProvider externalScopeProvider,
            UiLoggerScopeProvider scopeProvider = null)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _externalScopeProvider = externalScopeProvider;
            _scopeProvider = scopeProvider;
        }

        /// <summary>
        /// Begins a logical operation scope
        /// </summary>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            if (_externalScopeProvider != null)
            {
                return _externalScopeProvider.Push(state);
            }

            return _scopeProvider?.Push(state) ?? NullScope.Instance;
        }

        /// <summary>
        /// Checks if logging is enabled for the specified level
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None)
                return false;

            return logLevel >= _listener.GetEffectiveLogLevel(_categoryName);
        }

        /// <summary>
        /// Writes a log entry
        /// </summary>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            string message = formatter(state, exception);

            if (string.IsNullOrEmpty(message) && exception == null)
                return;

            // Get scope information if available
            string scopeInfo = null;
            if (_externalScopeProvider != null)
            {
                StringBuilder sb = new StringBuilder();
                _externalScopeProvider.ForEachScope((scope, builder) =>
                {
                    builder.Append(" => ");
                    builder.Append(scope);
                }, sb);

                if (sb.Length > 0)
                {
                    scopeInfo = sb.ToString();
                }
            }
            else if (_scopeProvider != null)
            {
                scopeInfo = _scopeProvider.GetScopeInformation();
            }

            var logMessage = new LogMessage(
                0, // ID will be assigned when added to the buffer
                DateTimeOffset.Now,
                logLevel,
                _categoryName,
                eventId,
                message,
                exception,
                scopeInfo);

            _listener.OnLogMessageReceived(logMessage);
        }
    }
}
