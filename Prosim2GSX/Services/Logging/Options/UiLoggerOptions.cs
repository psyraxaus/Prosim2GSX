namespace Prosim2GSX.Services.Logging.Options
{
    /// <summary>
    /// Options for the UI logger
    /// </summary>
    public class UiLoggerOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of log entries to keep in the UI
        /// </summary>
        public int MaxLogEntries { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to show timestamps in the UI
        /// </summary>
        public bool ShowTimestamps { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show log levels in the UI
        /// </summary>
        public bool ShowLogLevels { get; set; } = true;

        /// <summary>
        /// Gets or sets whether debug messages are shown in the UI by default
        /// </summary>
        public bool ShowDebugMessages { get; set; } = false;
    }
}
