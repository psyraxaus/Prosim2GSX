using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for logging functionality
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message with the specified level, context, and message
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="context">The logging context</param>
        /// <param name="message">The message to log</param>
        void Log(LogLevel level, string context, string message);
    }
}
