using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prosim2GSX.Services.Logging.Interfaces;
using Prosim2GSX.Services.Logging.Options;
using System;
using System.Collections.Concurrent;

namespace Prosim2GSX.Services.Logging.Provider
{
    /// <summary>
    /// Provider for UI loggers
    /// </summary>
    public class UiLoggerProvider : ILoggerProvider
    {
        private readonly IUiLogListener _listener;
        private readonly IExternalScopeProvider _scopeProvider;
        private readonly ConcurrentDictionary<string, UiLogger> _loggers = new ConcurrentDictionary<string, UiLogger>();

        /// <summary>
        /// Initializes a new instance of the UiLoggerProvider class
        /// </summary>
        public UiLoggerProvider(IUiLogListener listener, IOptions<UiLoggerOptions> options)
        {
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _scopeProvider = new LoggerExternalScopeProvider();
        }

        /// <summary>
        /// Creates a new UI logger for the specified category
        /// </summary>
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new UiLogger(name, _listener, _scopeProvider));
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
