using Prosim2GSX.Services.Logger.Enums;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Windows.Media;

namespace Prosim2GSX.Services.Logger.Implementation
{
    public class LogEntry
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Context { get; set; }
        public LogCategory Category { get; set; }

        // Active log categories (bit flags for efficient filtering)
        private static LogCategory _activeCategories = LogCategory.All;

        // Configured log level from settings
        private static LogLevel _configuredLogLevel = LogLevel.Debug;


        public LogEntry(string message, LogLevel level, string context, LogCategory category = LogCategory.All)
        {
            Message = message;
            Timestamp = DateTime.Now;
            Level = level;
            Context = context;
            Category = category;
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] [{Category}] {Message}";
        }

        public Brush LevelBrush
        {
            get
            {
                return Level switch
                {
                    LogLevel.Critical => new SolidColorBrush(Colors.Red),
                    LogLevel.Error => new SolidColorBrush(Colors.OrangeRed),
                    LogLevel.Warning => new SolidColorBrush(Colors.Orange),
                    LogLevel.Information => new SolidColorBrush(Colors.Black),
                    LogLevel.Debug => new SolidColorBrush(Colors.Gray),
                    LogLevel.Verbose => new SolidColorBrush(Colors.DarkGray),
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
        }
    }

    public static class LogService
    {
        // For backward compatibility
        public static Queue MessageQueue = new();

        // New queue for LogEntry objects
        public static ConcurrentQueue<LogEntry> LogEntryQueue = new ConcurrentQueue<LogEntry>();

        // Active log categories (bit flags for efficient filtering)
        private static LogCategory _activeCategories = LogCategory.All;

        // Configured log level from settings
        private static LogLevel _configuredLogLevel = LogLevel.Debug;

        /// <summary>
        /// Sets the configured log level
        /// </summary>
        public static void SetLogLevel(LogLevel level)
        {
            _configuredLogLevel = level;
        }

        /// <summary>
        /// Set specific categories to log (replaces existing categories)
        /// </summary>
        public static void SetActiveCategories(params LogCategory[] categories)
        {
            if (categories == null || categories.Length == 0)
            {
                // Default to all if no categories specified
                _activeCategories = LogCategory.All;
                return;
            }

            _activeCategories = 0; // Clear existing categories

            foreach (var category in categories)
            {
                _activeCategories |= category; // Bitwise OR to combine flags
            }

            // Log the change
            Log(LogLevel.Information, "LogService", $"Logging categories set to: {_activeCategories}", LogCategory.All);
        }

        /// <summary>
        /// Add categories to the active logging set
        /// </summary>
        public static void AddActiveCategories(params LogCategory[] categories)
        {
            if (categories == null || categories.Length == 0)
                return;

            foreach (var category in categories)
            {
                _activeCategories |= category; // Bitwise OR to add flag
            }

            // Log the change
            Log(LogLevel.Information, "LogService", $"Updated logging categories to: {_activeCategories}", LogCategory.All);
        }

        /// <summary>
        /// Remove categories from the active logging set
        /// </summary>
        public static void RemoveActiveCategories(params LogCategory[] categories)
        {
            if (categories == null || categories.Length == 0)
                return;

            foreach (var category in categories)
            {
                _activeCategories &= ~category; // Bitwise complement and AND to remove flag
            }

            // Log the change
            Log(LogLevel.Information, "LogService", $"Updated logging categories to: {_activeCategories}", LogCategory.All);
        }

        /// <summary>
        /// Check if a category is active for logging
        /// </summary>
        public static bool IsCategoryActive(LogCategory category)
        {
            // If All is set, or the specific category is set
            return (_activeCategories == LogCategory.All || (_activeCategories & category) == category);
        }

        /// <summary>
        /// Sets up standard service category combinations
        /// </summary>
        public static void SetupServiceLogging(string serviceName)
        {
            switch (serviceName.ToLower())
            {
                case "refueling":
                    SetActiveCategories(LogCategory.Refueling, LogCategory.SimConnect);
                    break;
                case "boarding":
                    SetActiveCategories(LogCategory.Boarding, LogCategory.Doors);
                    break;
                case "catering":
                    SetActiveCategories(LogCategory.Catering, LogCategory.Doors);
                    break;
                case "cargo":
                    SetActiveCategories(LogCategory.Cargo, LogCategory.Doors);
                    break;
                case "doors":
                    SetActiveCategories(LogCategory.Doors);
                    break;
                case "prosim":
                    SetActiveCategories(LogCategory.Prosim);
                    break;
                case "critical":
                    // Only log critical errors and up across all categories
                    SetActiveCategories(LogCategory.All);
                    SetLogLevel(LogLevel.Critical);
                    break;
                case "all":
                    SetActiveCategories(LogCategory.All);
                    SetLogLevel(LogLevel.Debug);
                    break;
                default:
                    // Try to match a category directly
                    if (Enum.TryParse<LogCategory>(serviceName, true, out var category))
                    {
                        SetActiveCategories(category);
                    }
                    else
                    {
                        // Default to all if not recognized
                        SetActiveCategories(LogCategory.All);
                    }
                    break;
            }
        }

        /// <summary>
        /// Logs a message with the specified level, context, and optional category
        /// </summary>
        public static void Log(LogLevel level, string context, string message, LogCategory category = LogCategory.All)
        {
            // Skip if level is below configured level
            if (level < _configuredLogLevel)
                return;

            // Skip logging if not All and this category isn't included
            if (category != LogCategory.All && !IsCategoryActive(category))
                return;

            // Create a LogEntry object
            var logEntry = new LogEntry(message, level, context, category);

            // Format for Serilog (keeping existing functionality)
            string categoryStr = category != LogCategory.All ? $"[{category}] " : "";
            string entry = string.Format("[ {0,-32} ] {1}{2}",
                (context.Length <= 32 ? context : context[0..32]),
                categoryStr,
                message.Replace("\n", "").Replace("\r", "").Replace("\t", ""));

            switch (level)
            {
                case LogLevel.Critical:
                    Serilog.Log.Logger.Fatal(entry);
                    break;
                case LogLevel.Error:
                    Serilog.Log.Logger.Error(entry);
                    break;
                case LogLevel.Warning:
                    Serilog.Log.Logger.Warning(entry);
                    break;
                case LogLevel.Information:
                    Serilog.Log.Logger.Information(entry);
                    break;
                case LogLevel.Debug:
                    Serilog.Log.Logger.Debug(entry);
                    break;
                case LogLevel.Verbose:
                    Serilog.Log.Logger.Verbose(entry);
                    break;
                default:
                    Serilog.Log.Logger.Debug(entry);
                    break;
            }

            // Add to both queues for backward compatibility
            // Only add non-debug messages to MessageQueue for backward compatibility
            if (level != LogLevel.Debug)
            {
                MessageQueue.Enqueue(message);
            }

            // Always add to the LogEntryQueue, though
            LogEntryQueue.Enqueue(logEntry);
        }
    }
}
