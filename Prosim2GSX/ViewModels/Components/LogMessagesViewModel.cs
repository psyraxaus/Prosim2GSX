using Microsoft.Extensions.Logging;
using Prosim2GSX.Services.Logging.Events;
using Prosim2GSX.Services.Logging.Interfaces;
using Prosim2GSX.Services.Logging.Models;
using Prosim2GSX.ViewModels.Base;
using System;
using System.Collections.ObjectModel;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the log messages display
    /// </summary>
    public class LogMessagesViewModel : ViewModelBase
    {
        #region Fields

        private readonly ObservableCollection<LogMessage> _logEntries = new ObservableCollection<LogMessage>();
        private readonly LogLevel _minimumLogLevel;
        private readonly int _maxDisplayEntries = 5;
        private readonly IUiLogListener _logListener;
        private readonly ILogger<LogMessagesViewModel> _logger;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the collection of log entries to display
        /// </summary>
        public ObservableCollection<LogMessage> LogEntries => _logEntries;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the LogMessagesViewModel class
        /// </summary>
        /// <param name="logListener">The log listener that captures logs</param>
        /// <param name="logger">Logger for this view model</param>
        public LogMessagesViewModel(IUiLogListener logListener, ILogger<LogMessagesViewModel> logger)
        {
            _logListener = logListener ?? throw new ArgumentNullException(nameof(logListener));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _minimumLogLevel = LogLevel.Information; // Show Info, Warning, Error, Critical in UI

            // Subscribe to log events
            _logListener.LogReceived += OnLogReceived;

            // Initialize with existing messages
            _logger.LogDebug("Initializing LogMessagesViewModel");
            var existingLogs = _logListener.GetFilteredMessages(_minimumLogLevel);
            if (existingLogs.Count > 0)
            {
                int startIndex = Math.Max(0, existingLogs.Count - _maxDisplayEntries);
                int count = Math.Min(_maxDisplayEntries, existingLogs.Count - startIndex);

                _logger.LogDebug("Loading {Count} existing log messages", count);

                for (int i = startIndex; i < startIndex + count; i++)
                {
                    _logEntries.Add(existingLogs[i]);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the log received event
        /// </summary>
        private void OnLogReceived(object sender, LogReceivedEventArgs e)
        {
            // Only show messages at or above the minimum level
            if (e.LogMessage.LogLevel < _minimumLogLevel)
            {
                return;
            }

            // Add to the observable collection on the UI thread
            ExecuteOnUIThread(() =>
            {
                // Add the new entry
                _logEntries.Add(e.LogMessage);

                // Remove oldest entries if we exceed the maximum
                while (_logEntries.Count > _maxDisplayEntries)
                {
                    _logEntries.RemoveAt(0);
                }
            });
        }

        /// <summary>
        /// Updates the log messages area with new log entries
        /// </summary>
        public void UpdateLogArea()
        {
            // This method is now a no-op as we're using events
            // It's kept for backward compatibility with any code that calls it
            _logger.LogTrace("UpdateLogArea called - no action needed with event-based logging");
        }

        /// <summary>
        /// Clears all log entries from the view
        /// </summary>
        public void ClearDisplay()
        {
            _logger.LogDebug("Clearing log display");
            ExecuteOnUIThread(() => _logEntries.Clear());
        }

        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            _logger.LogDebug("LogMessagesViewModel cleanup");

            // Unsubscribe from log events
            if (_logListener != null)
            {
                _logListener.LogReceived -= OnLogReceived;
            }
        }

        #endregion
    }
}
