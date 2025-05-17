using Microsoft.Extensions.Logging;
using Prosim2GSX.Services.Logging.Events;
using Prosim2GSX.Services.Logging.Models;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.Logging.Interfaces
{
    /// <summary>
    /// Interface for a log listener that captures logs for display
    /// </summary>
    public interface IUiLogListener
    {
        /// <summary>
        /// Event raised when a log message is received
        /// </summary>
        event EventHandler<LogReceivedEventArgs> LogReceived;

        /// <summary>
        /// Gets all buffered log messages
        /// </summary>
        IReadOnlyList<LogMessage> GetBufferedMessages();

        /// <summary>
        /// Gets buffered log messages filtered by level
        /// </summary>
        IReadOnlyList<LogMessage> GetFilteredMessages(LogLevel minimumLevel);

        /// <summary>
        /// Clears all buffered log messages
        /// </summary>
        void ClearBufferedMessages();

        /// <summary>
        /// Updates the minimum log level for a specific category
        /// </summary>
        void SetCategoryLogLevel(string category, LogLevel level);

        /// <summary>
        /// Updates the default minimum log level
        /// </summary>
        void SetDefaultLogLevel(LogLevel level);

        /// <summary>
        /// Gets the effective log level for a specific category
        /// </summary>
        LogLevel GetEffectiveLogLevel(string category);

        /// <summary>
        /// Processes a received log message
        /// </summary>
        void OnLogMessageReceived(LogMessage logMessage);
    }
}
