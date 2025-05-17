using Microsoft.Extensions.Logging;
using Prosim2GSX.Services.Logging;
using System;

namespace Prosim2GSX.Services.Logging
{
    /// <summary>
    /// Extension methods for ILogger to provide compatibility with the old logging system
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Maps the custom LogLevel to Microsoft.Extensions.Logging.LogLevel
        /// </summary>
        /// <param name="level">Custom log level</param>
        /// <returns>Standard log level</returns>
        public static LogLevel ToMicrosoftLogLevel(this LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => LogLevel.Debug,
                LogLevel.Information => LogLevel.Information,
                LogLevel.Warning => LogLevel.Warning,
                LogLevel.Error => LogLevel.Error,
                LogLevel.Critical => LogLevel.Critical,
                _ => LogLevel.Information
            };
        }

        /// <summary>
        /// Logs a message with the specified log level, category, and message
        /// This method provides compatibility with the old LogService.Log method
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="level">Log level</param>
        /// <param name="category">Log category</param>
        /// <param name="message">Log message</param>
        public static void Log<T>(this ILogger<T> logger, LogLevel level, string category, string message)
        {
            var msLogLevel = level.ToMicrosoftLogLevel();
            logger.Log(msLogLevel, "[{Category}] {Message}", category, message);
        }

        /// <summary>
        /// Logs a message with the specified log level, category, and message
        /// This method provides compatibility with the old LogService.Log method
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="level">Log level</param>
        /// <param name="category">Log category</param>
        /// <param name="message">Log message</param>
        /// <param name="args">Optional arguments for formatting the message</param>
        public static void Log<T>(this ILogger<T> logger, LogLevel level, string category, string message, params object[] args)
        {
            var msLogLevel = level.ToMicrosoftLogLevel();

            // Format the message if args are provided
            if (args != null && args.Length > 0)
            {
                message = string.Format(message, args);
            }

            logger.Log(msLogLevel, "[{Category}] {Message}", category, message);
        }

        /// <summary>
        /// Logs an exception with the specified log level, category, and message
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="level">Log level</param>
        /// <param name="category">Log category</param>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">Log message</param>
        public static void Log<T>(this ILogger<T> logger, LogLevel level, string category, Exception exception, string message)
        {
            var msLogLevel = level.ToMicrosoftLogLevel();
            logger.Log(msLogLevel, exception, "[{Category}] {Message}", category, message);
        }

        /// <summary>
        /// Creates a static logger class for backward compatibility with the old LogService
        /// </summary>
        public class StaticLoggerAdapter
        {
            private static ILoggerFactory _factory;

            /// <summary>
            /// Initializes the static logger with a logger factory
            /// </summary>
            /// <param name="factory">The logger factory to use</param>
            public static void Initialize(ILoggerFactory factory)
            {
                _factory = factory;
            }

            /// <summary>
            /// Logs a message with the specified log level, category, and message
            /// </summary>
            /// <param name="level">Log level</param>
            /// <param name="category">Log category</param>
            /// <param name="message">Log message</param>
            public static void Log(LogLevel level, string category, string message)
            {
                if (_factory == null)
                    return;

                var logger = _factory.CreateLogger(category);
                var msLogLevel = level.ToMicrosoftLogLevel();
                logger.Log(msLogLevel, "[{Category}] {Message}", category, message);
            }

            /// <summary>
            /// Logs a message with the specified log level, category, and message
            /// </summary>
            /// <param name="level">Log level</param>
            /// <param name="category">Log category</param>
            /// <param name="message">Log message</param>
            /// <param name="args">Optional arguments for formatting the message</param>
            public static void Log(LogLevel level, string category, string message, params object[] args)
            {
                if (_factory == null)
                    return;

                var logger = _factory.CreateLogger(category);
                var msLogLevel = level.ToMicrosoftLogLevel();

                // Format the message if args are provided
                if (args != null && args.Length > 0)
                {
                    message = string.Format(message, args);
                }

                logger.Log(msLogLevel, "[{Category}] {Message}", category, message);
            }

            /// <summary>
            /// Logs an exception with the specified log level, category, and message
            /// </summary>
            /// <param name="level">Log level</param>
            /// <param name="category">Log category</param>
            /// <param name="exception">The exception to log</param>
            /// <param name="message">Log message</param>
            public static void Log(LogLevel level, string category, Exception exception, string message)
            {
                if (_factory == null)
                    return;

                var logger = _factory.CreateLogger(category);
                var msLogLevel = level.ToMicrosoftLogLevel();
                logger.Log(msLogLevel, exception, "[{Category}] {Message}", category, message);
            }
        }
    }
}
