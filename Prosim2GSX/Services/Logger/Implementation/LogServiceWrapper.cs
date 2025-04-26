using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Interfaces;

namespace Prosim2GSX.Services.Logger.Implementation
{
    /// <summary>
    /// Wrapper for the static LogService that implements the ILogService interface
    /// </summary>
    /// <remarks>
    /// This class allows the LogService to be used through dependency injection
    /// while maintaining backward compatibility with the static implementation.
    /// </remarks>
    public class LogServiceWrapper : ILogService
    {
        /// <summary>
        /// Log a message with the specified level, context, and category
        /// </summary>
        /// <param name="level">The severity level of the log message</param>
        /// <param name="context">The source context (typically class name) of the log message</param>
        /// <param name="message">The log message content</param>
        /// <param name="category">The category of the log message for filtering</param>
        public void Log(LogLevel level, string context, string message, LogCategory category = LogCategory.All)
        {
            // Forward to the static implementation
            LogService.Log(level, context, message, category);
        }

        /// <summary>
        /// Set which categories to actively log (replaces any previously active categories)
        /// </summary>
        /// <param name="categories">One or more categories to log</param>
        public void SetActiveCategories(params LogCategory[] categories)
        {
            // Forward to the static implementation
            LogService.SetActiveCategories(categories);
        }

        /// <summary>
        /// Add more categories to the active logging set
        /// </summary>
        /// <param name="categories">One or more categories to add to active logging</param>
        public void AddActiveCategories(params LogCategory[] categories)
        {
            // Forward to the static implementation
            LogService.AddActiveCategories(categories);
        }

        /// <summary>
        /// Remove categories from the active logging set
        /// </summary>
        /// <param name="categories">One or more categories to remove from active logging</param>
        public void RemoveActiveCategories(params LogCategory[] categories)
        {
            // Forward to the static implementation
            LogService.RemoveActiveCategories(categories);
        }

        /// <summary>
        /// Set the minimum severity level for logging
        /// </summary>
        /// <param name="level">The minimum log level to show</param>
        public void SetLogLevel(LogLevel level)
        {
            // Forward to the static implementation
            LogService.SetLogLevel(level);
        }

        /// <summary>
        /// Setup predefined logging configuration for a service type
        /// </summary>
        /// <param name="serviceName">The name of the service or logging preset</param>
        public void SetupServiceLogging(string serviceName)
        {
            // Forward to the static implementation
            LogService.SetupServiceLogging(serviceName);
        }

        /// <summary>
        /// Check if a category is currently active for logging
        /// </summary>
        /// <param name="category">The category to check</param>
        /// <returns>True if the category is active, false otherwise</returns>
        public bool IsCategoryActive(LogCategory category)
        {
            // Forward to the static implementation
            return LogService.IsCategoryActive(category);
        }
    }
}