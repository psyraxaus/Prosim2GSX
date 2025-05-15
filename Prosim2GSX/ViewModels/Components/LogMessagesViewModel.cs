using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the log messages display
    /// </summary>
    public class LogMessagesViewModel : ViewModelBase
    {
        #region Fields

        private readonly ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
        private readonly LogLevel _uiLogLevel = LogLevel.Information;
        private readonly int _maxLogEntries = 5;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the collection of log entries to display
        /// </summary>
        public ObservableCollection<LogEntry> LogEntries => _logEntries;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the LogMessagesViewModel class
        /// </summary>
        public LogMessagesViewModel()
        {
            // Initial setup if needed
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the log messages area with new log entries
        /// </summary>
        public void UpdateLogArea()
        {
            // Process all available log entries from the LogEntryQueue
            while (LogService.LogEntryQueue.TryDequeue(out LogEntry entry))
            {
                // Only show Information, Warning, Error, and Critical messages in the UI
                if (entry.Level <= LogLevel.Debug)  // Debug is 1, Verbose is 0
                {
                    // Skip Debug and Verbose messages
                    continue;
                }

                // Add to the observable collection on the UI thread
                ExecuteOnUIThread(() =>
                {
                    // Add the new entry
                    _logEntries.Add(entry);

                    // Remove oldest entries if we exceed the maximum
                    while (_logEntries.Count > _maxLogEntries)
                    {
                        _logEntries.RemoveAt(0);
                    }
                });
            }

            // Process the old MessageQueue for backward compatibility
            while (LogService.MessageQueue.Count > 0)
            {
                // Just dequeue the messages to keep the queue from growing
                LogService.MessageQueue.Dequeue();
            }
        }

        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            // Currently nothing to clean up, but the method is here for consistency
        }

        #endregion
    }
}
