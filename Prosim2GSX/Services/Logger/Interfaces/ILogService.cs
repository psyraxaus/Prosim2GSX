using Prosim2GSX.Services.Logger.Enums;

namespace Prosim2GSX.Services.Logger.Interfaces
{
    /// <summary>
    /// Interface for the logging service that provides categorized logging functionality
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// Log a message with the specified level, context, and category
        /// </summary>
        /// <param name="level">The severity level of the log message</param>
        /// <param name="context">The source context (typically class name) of the log message</param>
        /// <param name="message">The log message content</param>
        /// <param name="category">The category of the log message for filtering</param>
        void Log(LogLevel level, string context, string message, LogCategory category = LogCategory.All);

        /// <summary>
        /// Set which categories to actively log (replaces any previously active categories)
        /// </summary>
        /// <param name="categories">One or more categories to log</param>
        void SetActiveCategories(params LogCategory[] categories);

        /// <summary>
        /// Add more categories to the active logging set
        /// </summary>
        /// <param name="categories">One or more categories to add to active logging</param>
        void AddActiveCategories(params LogCategory[] categories);

        /// <summary>
        /// Remove categories from the active logging set
        /// </summary>
        /// <param name="categories">One or more categories to remove from active logging</param>
        void RemoveActiveCategories(params LogCategory[] categories);

        /// <summary>
        /// Set the minimum severity level for logging
        /// </summary>
        /// <param name="level">The minimum log level to show</param>
        void SetLogLevel(LogLevel level);

        /// <summary>
        /// Setup predefined logging configuration for a service type
        /// </summary>
        /// <param name="serviceName">The name of the service or logging preset</param>
        /// <remarks>
        /// Common values include:
        /// - "refueling": Focus on refueling and SimConnect
        /// - "boarding": Focus on boarding and doors
        /// - "catering": Focus on catering and doors
        /// - "all": Log everything
        /// - "critical": Log only critical errors
        /// </remarks>
        void SetupServiceLogging(string serviceName);

        /// <summary>
        /// Check if a category is currently active for logging
        /// </summary>
        /// <param name="category">The category to check</param>
        /// <returns>True if the category is active, false otherwise</returns>
        bool IsCategoryActive(LogCategory category);
    }
}