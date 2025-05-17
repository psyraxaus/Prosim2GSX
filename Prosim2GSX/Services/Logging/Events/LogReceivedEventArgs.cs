using Prosim2GSX.Services.Logging.Models;
using System;

namespace Prosim2GSX.Services.Logging.Events
{
    /// <summary>
    /// Event arguments for the LogReceived event
    /// </summary>
    public class LogReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the log message
        /// </summary>
        public LogMessage LogMessage { get; }

        /// <summary>
        /// Creates a new instance of LogReceivedEventArgs
        /// </summary>
        /// <param name="logMessage">The log message</param>
        public LogReceivedEventArgs(LogMessage logMessage)
        {
            LogMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
        }
    }
}
