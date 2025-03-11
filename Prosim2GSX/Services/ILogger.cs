using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for logging services
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="source">The source of the log message</param>
        /// <param name="message">The log message</param>
        void Log(LogLevel level, string source, string message);
        
        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="source">The source of the log message</param>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">An optional message to include with the exception</param>
        void Log(LogLevel level, string source, Exception exception, string message = null);
    }
    
    /// <summary>
    /// Log levels
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level for detailed information
        /// </summary>
        Debug,
        
        /// <summary>
        /// Information level for general information
        /// </summary>
        Information,
        
        /// <summary>
        /// Warning level for potential issues
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error level for errors that don't stop the application
        /// </summary>
        Error,
        
        /// <summary>
        /// Critical level for errors that stop the application
        /// </summary>
        Critical
    }
}
