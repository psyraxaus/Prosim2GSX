using Prosim2GSX.Services.Logger.Enums;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        // Active log categories (bit flags for efficient filtering)
        private static LogCategory _activeCategories = LogCategory.All;

        // Configured log level from settings
        private static LogLevel _configuredLogLevel = LogLevel.Debug;

        private static HashSet<LogCategory> _debugVerbosityCategories = new HashSet<LogCategory>();

        // For backward compatibility
        public static Queue MessageQueue = new();

        // New queue for LogEntry objects
        public static ConcurrentQueue<LogEntry> LogEntryQueue = new ConcurrentQueue<LogEntry>();

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

            // Special handling for Debug level - apply verbosity filter
            if (level == LogLevel.Debug)
            {
                // If category is All (uncategorized debug log)
                if (category == LogCategory.All)
                {
                    // Only show uncategorized debug logs if ALL categories are enabled
                    if (!_debugVerbosityCategories.Contains(LogCategory.All))
                    {
                        // Skip uncategorized debug messages if not showing All categories
                        return;
                    }
                }
                // For categorized debug logs
                else
                {
                    // Skip if this specific category is not in the allowed list
                    // and "All" is not in the list either
                    if (!_debugVerbosityCategories.Contains(LogCategory.All) &&
                        !_debugVerbosityCategories.Contains(category))
                    {
                        return;
                    }
                }
            }

            // Skip logging if not All and this category isn't included in active categories
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

        /// <summary>
        /// Updates the debug verbosity filter from a string configuration value
        /// </summary>
        /// <param name="verbosityConfig">String configuration (e.g., "All" or "Refueling,Prosim,SimConnect")</param>
        public static void SetDebugVerbosity(string verbosityConfig)
        {
            _debugVerbosityCategories.Clear();

            if (string.IsNullOrWhiteSpace(verbosityConfig) || verbosityConfig.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                // Special case: "All" means include all categories for debug logs
                _debugVerbosityCategories.Add(LogCategory.All);
                Log(LogLevel.Information, nameof(LogService),
                    "Debug verbosity set to ALL categories", LogCategory.All);
                return;
            }

            // Force log level to Information to ensure these messages are seen
            LogLevel originalLevel = _configuredLogLevel;
            _configuredLogLevel = LogLevel.Information;

            try
            {
                // Parse comma-separated list
                string[] categoryNames = verbosityConfig.Split(',', StringSplitOptions.RemoveEmptyEntries);

                Log(LogLevel.Debug, nameof(LogService),
                    $"Parsing categories: {string.Join(", ", categoryNames)}", LogCategory.All);

                foreach (string originalCategoryName in categoryNames)
                {
                    // Clean up the category name
                    string categoryName = originalCategoryName.Trim();

                    // Try direct enum parsing first
                    if (Enum.TryParse<LogCategory>(categoryName, true, out var category))
                    {
                        _debugVerbosityCategories.Add(category);
                        Log(LogLevel.Information, nameof(LogService),
                            $"Added category: {category} (value: {(int)category})", LogCategory.All);
                    }
                    else
                    {
                        // Check if it's one of our known friendly names
                        LogCategory? resolvedCategory = ResolveCategoryFromFriendlyName(categoryName);

                        if (resolvedCategory.HasValue)
                        {
                            _debugVerbosityCategories.Add(resolvedCategory.Value);
                            Log(LogLevel.Information, nameof(LogService),
                                $"Added category from friendly name '{categoryName}': {resolvedCategory.Value} (value: {(int)resolvedCategory.Value})",
                                LogCategory.All);
                        }
                        else
                        {
                            Log(LogLevel.Warning, nameof(LogService),
                                $"Unknown category '{categoryName}' in debug verbosity config", LogCategory.All);
                        }
                    }
                }

                // Dump the full contents of the HashSet for debugging
                Log(LogLevel.Information, nameof(LogService),
                    $"Debug verbosity categories count: {_debugVerbosityCategories.Count}", LogCategory.All);

                foreach (var cat in _debugVerbosityCategories)
                {
                    Log(LogLevel.Debug, nameof(LogService),
                        $"Active debug category: {cat} (value: {(int)cat})", LogCategory.All);
                }

                // Log the result
                if (_debugVerbosityCategories.Count > 0)
                {
                    Log(LogLevel.Information, nameof(LogService),
                        $"Debug verbosity set to categories: {string.Join(", ", _debugVerbosityCategories)}",
                        LogCategory.All);
                }
                else
                {
                    // If no valid categories were found, default to All
                    _debugVerbosityCategories.Add(LogCategory.All);
                    Log(LogLevel.Warning, nameof(LogService),
                        "No valid categories found in debug verbosity config, defaulting to ALL",
                        LogCategory.All);
                }
            }
            finally
            {
                // Restore original log level
                _configuredLogLevel = originalLevel;
            }
        }

        /// <summary>
        /// Resolves a friendly category name to the corresponding LogCategory enum value
        /// </summary>
        private static LogCategory? ResolveCategoryFromFriendlyName(string friendlyName)
        {
            // Handle case variations
            switch (friendlyName.ToLowerInvariant())
            {
                case "all":
                case "all categories":
                    return LogCategory.All;

                case "gsx":
                case "gsxcontroller":
                    return LogCategory.GsxController;

                case "refuel":
                case "refueling":
                    return LogCategory.Refueling;

                case "board":
                case "boarding":
                    return LogCategory.Boarding;

                case "cater":
                case "catering":
                    return LogCategory.Catering;

                case "ground":
                case "groundservices":
                case "ground services":
                    return LogCategory.GroundServices;

                case "sim":
                case "simconnect":
                    return LogCategory.SimConnect;

                case "ps":
                case "prosim":
                    return LogCategory.Prosim;

                case "event":
                case "events":
                    return LogCategory.Events;

                case "menu":
                case "menus":
                    return LogCategory.Menu;

                case "audio":
                case "sound":
                    return LogCategory.Audio;

                case "config":
                case "configuration":
                    return LogCategory.Configuration;

                case "door":
                case "doors":
                    return LogCategory.Doors;

                case "cargo":
                    return LogCategory.Cargo;

                case "load":
                case "loadsheet":
                    return LogCategory.Loadsheet;

                default:
                    return null;
            }
        }
    }
}
