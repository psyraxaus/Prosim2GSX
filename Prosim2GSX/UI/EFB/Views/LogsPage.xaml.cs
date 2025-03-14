using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.UI.EFB.ViewModels;

namespace Prosim2GSX.UI.EFB.Views
{
    /// <summary>
    /// Interaction logic for LogsPage.xaml
    /// </summary>
    public partial class LogsPage : UserControl, IEFBPage
    {
        private readonly DispatcherTimer _updateTimer;
        private readonly Dictionary<LogLevel, SolidColorBrush> _logLevelColors;
        private readonly Queue<string> _logQueue;
        private LogLevel _filterLevel = LogLevel.Debug; // Default to showing all logs
        private bool _showAllLevels = true;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsPage"/> class.
        /// </summary>
        // UI elements
        private RichTextBox _logTextBox;
        private ScrollViewer _logScrollViewer;
        private ComboBox _logLevelFilter;
        private CheckBox _autoScrollToggle;

        public LogsPage()
        {
            // Manually load the XAML
            Uri resourceLocator = new Uri("/Prosim2GSX;component/UI/EFB/Views/LogsPage.xaml", UriKind.Relative);
            System.Windows.Application.LoadComponent(this, resourceLocator);

            // Get the logger instance
            _logger = Logger.Instance;

            // Get the log queue
            _logQueue = Logger.MessageQueue;

            // Initialize log level colors
            _logLevelColors = new Dictionary<LogLevel, SolidColorBrush>
            {
                { LogLevel.Debug, new SolidColorBrush(Colors.Gray) },
                { LogLevel.Information, new SolidColorBrush(Colors.White) },
                { LogLevel.Warning, new SolidColorBrush(Colors.Yellow) },
                { LogLevel.Error, new SolidColorBrush(Colors.Red) },
                { LogLevel.Critical, new SolidColorBrush(Colors.DarkRed) }
            };

            // Initialize the update timer
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // Update every 500ms
            };
            _updateTimer.Tick += UpdateTimer_Tick;

            // Hook up the Loaded event to initialize UI elements
            this.Loaded += LogsPage_Loaded;
        }

        private void LogsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Find UI elements by name
            _logTextBox = this.FindName("LogTextBox") as RichTextBox;
            _logScrollViewer = this.FindName("LogScrollViewer") as ScrollViewer;
            _logLevelFilter = this.FindName("LogLevelFilter") as ComboBox;
            _autoScrollToggle = this.FindName("AutoScrollToggle") as CheckBox;

            // Verify UI elements were found
            if (_logTextBox == null || _logScrollViewer == null || 
                _logLevelFilter == null || _autoScrollToggle == null)
            {
                // Log error if UI elements weren't found
                _logger?.Log(LogLevel.Error, "LogsPage", "Failed to find UI elements");
                return;
            }

            // Set additional FlowDocument properties
            _logTextBox.Document.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            _logTextBox.Document.PageWidth = 3000; // Very wide to prevent word wrapping
            _logTextBox.Document.TextAlignment = TextAlignment.Left;
            _logTextBox.AcceptsReturn = false;
            _logTextBox.Document.LineHeight = Double.NaN; // Use default line height

            // Hook up event handlers
            _logLevelFilter.SelectionChanged += LogLevelFilter_SelectionChanged;

            // Load initial logs
            LoadLogs();
        }

        #region IEFBPage Implementation

        /// <summary>
        /// Gets the title of the page.
        /// </summary>
        public string Title => "Logs";

        /// <summary>
        /// Gets the icon of the page.
        /// </summary>
        public string Icon => "\uE9D9";

        /// <summary>
        /// Gets the page content.
        /// </summary>
        public UserControl Content => this;

        /// <summary>
        /// Gets a value indicating whether the page is visible in the navigation menu.
        /// </summary>
        public bool IsVisibleInMenu => true;

        /// <summary>
        /// Gets a value indicating whether the page can be navigated to.
        /// </summary>
        public bool CanNavigateTo => true;

        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        public void OnNavigatedTo()
        {
            // Start the update timer
            _updateTimer.Start();
        }

        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public void OnNavigatedFrom()
        {
            // Stop the update timer
            _updateTimer.Stop();
        }

        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public void OnActivated()
        {
            // No additional activation logic needed
        }

        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public void OnDeactivated()
        {
            // No additional deactivation logic needed
        }

        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public void OnRefresh()
        {
            // Clear the log display and reload logs
            if (_logTextBox != null)
            {
                _logTextBox.Document.Blocks.Clear();
                LoadLogs();
            }
        }

        #endregion

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // Check if there are new logs to display
            if (_logQueue != null && _logQueue.Count > 0)
            {
                // Get all new logs
                var newLogs = new List<string>();
                while (_logQueue != null && _logQueue.Count > 0)
                {
                    newLogs.Add(_logQueue.Dequeue());
                }

                // Add new logs to the display
                foreach (var log in newLogs)
                {
                    AddLogEntry(log);
                }

                // Auto-scroll if enabled
                if (_autoScrollToggle != null && _autoScrollToggle.IsChecked == true && 
                    _logScrollViewer != null)
                {
                    _logScrollViewer.ScrollToEnd();
                }
            }
        }

        private void LoadLogs()
        {
            // Check if UI elements are initialized
            if (_logTextBox == null)
                return;

            // Clear the log display
            _logTextBox.Document.Blocks.Clear();

            // Add a message indicating the logs are being loaded
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run("Loading logs...") { Foreground = new SolidColorBrush(Colors.Gray) });
            _logTextBox.Document.Blocks.Add(paragraph);

            // In a real implementation, we would load logs from a log file or database
            // For now, we'll just display a message
            paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run("Log display initialized. New logs will appear here.") 
            { 
                Foreground = new SolidColorBrush(Colors.White) 
            });
            _logTextBox.Document.Blocks.Add(paragraph);

            // Log a test message
            _logger.Log(LogLevel.Information, "LogsPage", "Log display initialized");
        }

        private void AddLogEntry(string logEntry)
        {
            // Parse the log entry to determine its level
            LogLevel level = DetermineLogLevel(logEntry);

            // Check if the log should be displayed based on the filter
            if (!_showAllLevels && level < _filterLevel)
            {
                return;
            }

            // Create a new paragraph for the log entry
            var paragraph = new Paragraph();
            paragraph.Margin = new Thickness(0, 0, 0, 5); // Ensure margin is set programmatically
            
            // Add a timestamp
            paragraph.Inlines.Add(new Run($"[{DateTime.Now:HH:mm:ss.fff}] ") 
            { 
                Foreground = new SolidColorBrush(Colors.Gray) 
            });
            
            // Add the log entry with the appropriate color
            paragraph.Inlines.Add(new Run(logEntry) 
            { 
                Foreground = _logLevelColors.TryGetValue(level, out var brush) ? brush : new SolidColorBrush(Colors.White) 
            });
            
            // Add the paragraph to the document
            if (_logTextBox != null)
            {
                _logTextBox.Document.Blocks.Add(paragraph);
            }
        }

        private LogLevel DetermineLogLevel(string logEntry)
        {
            // Simple heuristic to determine log level based on content
            // In a real implementation, this would parse the log entry format
            if (logEntry.Contains("ERROR") || logEntry.Contains("Exception"))
            {
                return LogLevel.Error;
            }
            else if (logEntry.Contains("WARN"))
            {
                return LogLevel.Warning;
            }
            else if (logEntry.Contains("INFO"))
            {
                return LogLevel.Information;
            }
            else if (logEntry.Contains("DEBUG"))
            {
                return LogLevel.Debug;
            }
            else if (logEntry.Contains("CRITICAL") || logEntry.Contains("FATAL"))
            {
                return LogLevel.Critical;
            }
            
            // Default to Information
            return LogLevel.Information;
        }

        private void LogLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_logLevelFilter == null)
                return;

            // Update the filter level based on the selection
            switch (_logLevelFilter.SelectedIndex)
            {
                case 0: // All Levels
                    _showAllLevels = true;
                    break;
                case 1: // Debug
                    _showAllLevels = false;
                    _filterLevel = LogLevel.Debug;
                    break;
                case 2: // Information
                    _showAllLevels = false;
                    _filterLevel = LogLevel.Information;
                    break;
                case 3: // Warning
                    _showAllLevels = false;
                    _filterLevel = LogLevel.Warning;
                    break;
                case 4: // Error
                    _showAllLevels = false;
                    _filterLevel = LogLevel.Error;
                    break;
                case 5: // Critical
                    _showAllLevels = false;
                    _filterLevel = LogLevel.Critical;
                    break;
            }

            // Refresh the logs
            OnRefresh();
        }
    }
}
