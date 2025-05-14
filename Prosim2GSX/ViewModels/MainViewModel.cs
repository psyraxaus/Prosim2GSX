using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services.GSX.Enums;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.ViewModels.Base;
using Prosim2GSX.ViewModels.Commands;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Prosim2GSX.ViewModels
{
    /// <summary>
    /// ViewModel for the MainWindow, handling all UI state and logic
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region Fields

        private readonly ServiceModel _serviceModel;
        private readonly NotifyIconViewModel _notifyModel;
        private readonly DispatcherTimer _timer;
        private readonly ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
        private readonly LogLevel _uiLogLevel = LogLevel.Information;

        // Tab related fields
        private int _selectedTabIndex;

        // Connection status fields
        private Brush _msfsStatusBrush = new SolidColorBrush(Colors.Red);
        private Brush _simConnectStatusBrush = new SolidColorBrush(Colors.Red);
        private Brush _prosimStatusBrush = new SolidColorBrush(Colors.Red);
        private Brush _sessionStatusBrush = new SolidColorBrush(Colors.Red);

        // Flight state fields
        private string _flightPhaseText = "AT GATE";
        private Brush _flightPhaseBrush = new SolidColorBrush(Colors.RoyalBlue);
        private int _activeFlightPhaseIndex = 0;

        // Service status fields
        private Brush _jetwayStatusBrush = new SolidColorBrush(Colors.LightGray);
        private Brush _stairsStatusBrush = new SolidColorBrush(Colors.LightGray);
        private Brush _refuelStatusBrush = new SolidColorBrush(Colors.LightGray);
        private Brush _cateringStatusBrush = new SolidColorBrush(Colors.LightGray);
        private Brush _boardingStatusBrush = new SolidColorBrush(Colors.LightGray);
        private Brush _deboardingStatusBrush = new SolidColorBrush(Colors.LightGray);
        private Brush _gpuStatusBrush = new SolidColorBrush(Colors.LightGray);
        private Brush _pcaStatusBrush = new SolidColorBrush(Colors.LightGray);
        private Brush _pushbackStatusBrush = new SolidColorBrush(Colors.LightGray);
        private Brush _chocksStatusBrush = new SolidColorBrush(Colors.LightGray);

        // Date and flight info fields
        private string _currentDate = DateTime.Now.ToString("dd.MM.yyyy");
        private string _flightNumber = "No Flight";

        // Subscription tokens for event handling
        private readonly System.Collections.Generic.List<SubscriptionToken> _subscriptionTokens = new System.Collections.Generic.List<SubscriptionToken>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the log entries collection for display in the UI
        /// </summary>
        public ObservableCollection<LogEntry> LogEntries => _logEntries;

        /// <summary>
        /// Gets or sets the currently selected tab index
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for MSFS connection
        /// </summary>
        public Brush MsfsStatusBrush
        {
            get => _msfsStatusBrush;
            set => SetProperty(ref _msfsStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for SimConnect connection
        /// </summary>
        public Brush SimConnectStatusBrush
        {
            get => _simConnectStatusBrush;
            set => SetProperty(ref _simConnectStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Prosim connection
        /// </summary>
        public Brush ProsimStatusBrush
        {
            get => _prosimStatusBrush;
            set => SetProperty(ref _prosimStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Session status
        /// </summary>
        public Brush SessionStatusBrush
        {
            get => _sessionStatusBrush;
            set => SetProperty(ref _sessionStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the current flight phase text
        /// </summary>
        public string FlightPhaseText
        {
            get => _flightPhaseText;
            set => SetProperty(ref _flightPhaseText, value);
        }

        /// <summary>
        /// Gets or sets the color for the flight phase text
        /// </summary>
        public Brush FlightPhaseBrush
        {
            get => _flightPhaseBrush;
            set => SetProperty(ref _flightPhaseBrush, value);
        }

        /// <summary>
        /// Gets or sets the active flight phase index (0-4) for the progress bar
        /// </summary>
        public int ActiveFlightPhaseIndex
        {
            get => _activeFlightPhaseIndex;
            set => SetProperty(ref _activeFlightPhaseIndex, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Jetway service
        /// </summary>
        public Brush JetwayStatusBrush
        {
            get => _jetwayStatusBrush;
            set => SetProperty(ref _jetwayStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Stairs service
        /// </summary>
        public Brush StairsStatusBrush
        {
            get => _stairsStatusBrush;
            set => SetProperty(ref _stairsStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Refueling service
        /// </summary>
        public Brush RefuelStatusBrush
        {
            get => _refuelStatusBrush;
            set => SetProperty(ref _refuelStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Catering service
        /// </summary>
        public Brush CateringStatusBrush
        {
            get => _cateringStatusBrush;
            set => SetProperty(ref _cateringStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Boarding service
        /// </summary>
        public Brush BoardingStatusBrush
        {
            get => _boardingStatusBrush;
            set => SetProperty(ref _boardingStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Deboarding service
        /// </summary>
        public Brush DeboardingStatusBrush
        {
            get => _deboardingStatusBrush;
            set => SetProperty(ref _deboardingStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for GPU service
        /// </summary>
        public Brush GPUStatusBrush
        {
            get => _gpuStatusBrush;
            set => SetProperty(ref _gpuStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for PCA service
        /// </summary>
        public Brush PCAStatusBrush
        {
            get => _pcaStatusBrush;
            set => SetProperty(ref _pcaStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Pushback service
        /// </summary>
        public Brush PushbackStatusBrush
        {
            get => _pushbackStatusBrush;
            set => SetProperty(ref _pushbackStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Chocks service
        /// </summary>
        public Brush ChocksStatusBrush
        {
            get => _chocksStatusBrush;
            set => SetProperty(ref _chocksStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the current date display
        /// </summary>
        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        /// <summary>
        /// Gets or sets the flight number display
        /// </summary>
        public string FlightNumber
        {
            get => _flightNumber;
            set => SetProperty(ref _flightNumber, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to show the Help dialog
        /// </summary>
        public ICommand ShowHelpCommand { get; }

        /// <summary>
        /// Command to show the Settings tab
        /// </summary>
        public ICommand ShowSettingsCommand { get; }

        /// <summary>
        /// Command to show the Audio Settings tab
        /// </summary>
        public ICommand ShowAudioSettingsCommand { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the MainViewModel class
        /// </summary>
        /// <param name="serviceModel">The service model for application data</param>
        /// <param name="notifyModel">The notification icon view model</param>
        public MainViewModel(ServiceModel serviceModel, NotifyIconViewModel notifyModel)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _notifyModel = notifyModel ?? throw new ArgumentNullException(nameof(notifyModel));

            // Initialize commands
            ShowHelpCommand = new RelayCommand(_ => ShowHelp());
            ShowSettingsCommand = new RelayCommand(_ => ShowSettings());
            ShowAudioSettingsCommand = new RelayCommand(_ => ShowAudioSettings());

            // Initialize timer for UI updates
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTick;

            // Subscribe to events
            SubscribeToEvents();
        }

#if DEBUG
        /// <summary>
        /// Constructor for design-time only - should not be used at runtime
        /// </summary>
        public MainViewModel()
        {
            // Initialize commands for design-time support
            ShowHelpCommand = new RelayCommand(_ => { });
            ShowSettingsCommand = new RelayCommand(_ => { });
            ShowAudioSettingsCommand = new RelayCommand(_ => { });

            // Initialize timer
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        }
#endif

        #endregion

        #region Methods

        /// <summary>
        /// Subscribes to events from event aggregator
        /// </summary>
        private void SubscribeToEvents()
        {
            // Subscribe to connection status events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<ConnectionStatusChangedEvent>(OnConnectionStatusChanged));

            // Subscribe to service status events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<ServiceStatusChangedEvent>(OnServiceStatusChanged));

            // Subscribe to flight phase events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<FlightPhaseChangedEvent>(OnFlightPhaseChanged));

            // Subscribe to flight number events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<FlightPlanChangedEvent>(OnFlightPlanChanged));
        }

        /// <summary>
        /// Unsubscribes from all events when the ViewModel is no longer needed
        /// </summary>
        public void Cleanup()
        {
            _timer.Stop();

            // Unsubscribe from all events
            foreach (var token in _subscriptionTokens)
            {
                EventAggregator.Instance.Unsubscribe<EventBase>(token);
            }
            _subscriptionTokens.Clear();
        }

        /// <summary>
        /// Starts the update timer when the window becomes visible
        /// </summary>
        public void OnWindowVisible()
        {
            UpdateDirectConnectionStatus();
            _timer.Start();
        }

        /// <summary>
        /// Stops the update timer when the window becomes invisible
        /// </summary>
        public void OnWindowHidden()
        {
            _timer.Stop();
        }

        /// <summary>
        /// Called on timer tick to update UI elements
        /// </summary>
        private void OnTick(object sender, EventArgs e)
        {
            UpdateLogArea();
            UpdateCurrentDate();
        }

        /// <summary>
        /// Updates the connection status indicators based on the current model state
        /// </summary>
        private void UpdateDirectConnectionStatus()
        {
            if (_serviceModel == null) return;

            MsfsStatusBrush = _serviceModel.IsSimRunning ?
                new SolidColorBrush(Colors.Green) :
                new SolidColorBrush(Colors.Red);

            ProsimStatusBrush = _serviceModel.IsProsimRunning ?
                new SolidColorBrush(Colors.Green) :
                new SolidColorBrush(Colors.Red);

            SimConnectStatusBrush = IPCManager.SimConnect?.IsConnected == true ?
                new SolidColorBrush(Colors.Green) :
                new SolidColorBrush(Colors.Red);

            SessionStatusBrush = _serviceModel.IsSessionRunning ?
                new SolidColorBrush(Colors.Green) :
                new SolidColorBrush(Colors.Red);
        }

        /// <summary>
        /// Updates the current date display
        /// </summary>
        private void UpdateCurrentDate()
        {
            CurrentDate = DateTime.Now.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Updates the log messages area with new log entries
        /// </summary>
        private void UpdateLogArea()
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Add the new entry
                    _logEntries.Add(entry);

                    // Remove oldest entries if we exceed the maximum
                    while (_logEntries.Count > 5) // Max log entries
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
        /// Handler for connection status changed events
        /// </summary>
        private void OnConnectionStatusChanged(ConnectionStatusChangedEvent evt)
        {
            Application.Current.Dispatcher.Invoke(() => {
                switch (evt.ConnectionName)
                {
                    case "MSFS":
                        MsfsStatusBrush = evt.IsConnected ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red);
                        break;
                    case "SimConnect":
                        SimConnectStatusBrush = evt.IsConnected ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red);
                        break;
                    case "Prosim":
                        ProsimStatusBrush = evt.IsConnected ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red);
                        break;
                    case "Session":
                        SessionStatusBrush = evt.IsConnected ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red);
                        break;
                }
            });
        }

        /// <summary>
        /// Handler for service status changed events
        /// </summary>
        private void OnServiceStatusChanged(ServiceStatusChangedEvent evt)
        {
            Application.Current.Dispatcher.Invoke(() => {
                Brush brush;
                switch (evt.Status)
                {
                    case ServiceStatus.Completed:
                        brush = new SolidColorBrush(Colors.Green);
                        break;
                    case ServiceStatus.Active:
                        brush = new SolidColorBrush(Colors.Gold);
                        break;
                    case ServiceStatus.Waiting:
                    case ServiceStatus.Requested:
                        brush = new SolidColorBrush(Colors.Blue);
                        break;
                    case ServiceStatus.Disconnected:
                        brush = new SolidColorBrush(Colors.Red);
                        break;
                    default:
                        brush = new SolidColorBrush(Colors.LightGray);
                        break;
                }

                // Update the appropriate indicator
                switch (evt.ServiceName)
                {
                    case "Jetway":
                        JetwayStatusBrush = brush;
                        break;
                    case "Stairs":
                        StairsStatusBrush = brush;
                        break;
                    case "Refuel":
                        RefuelStatusBrush = brush;
                        break;
                    case "Catering":
                        CateringStatusBrush = brush;
                        break;
                    case "Boarding":
                        BoardingStatusBrush = brush;
                        break;
                    case "Deboarding":
                        DeboardingStatusBrush = brush;
                        break;
                    case "GPU":
                        GPUStatusBrush = brush;
                        break;
                    case "PCA":
                        PCAStatusBrush = brush;
                        break;
                    case "Pushback":
                        PushbackStatusBrush = brush;
                        break;
                    case "Chocks":
                        ChocksStatusBrush = brush;
                        break;
                }
            });
        }

        /// <summary>
        /// Handler for flight phase changed events
        /// </summary>
        private void OnFlightPhaseChanged(FlightPhaseChangedEvent evt)
        {
            Application.Current.Dispatcher.Invoke(() => {
                switch (evt.NewState)
                {
                    case FlightState.PREFLIGHT:
                    case FlightState.DEPARTURE:
                        FlightPhaseText = "AT GATE";
                        FlightPhaseBrush = new SolidColorBrush(Colors.RoyalBlue);
                        ActiveFlightPhaseIndex = 0;
                        break;
                    case FlightState.TAXIOUT:
                        FlightPhaseText = "TAXI OUT";
                        FlightPhaseBrush = new SolidColorBrush(Colors.Gold);
                        ActiveFlightPhaseIndex = 1;
                        break;
                    case FlightState.FLIGHT:
                        FlightPhaseText = "IN FLIGHT";
                        FlightPhaseBrush = new SolidColorBrush(Colors.Green);
                        ActiveFlightPhaseIndex = 2;
                        break;
                    case FlightState.TAXIIN:
                    case FlightState.ARRIVAL:
                        FlightPhaseText = "APPROACH";
                        FlightPhaseBrush = new SolidColorBrush(Colors.Purple);
                        ActiveFlightPhaseIndex = 3;
                        break;
                    case FlightState.TURNAROUND:
                        FlightPhaseText = "ARRIVED";
                        FlightPhaseBrush = new SolidColorBrush(Colors.Teal);
                        ActiveFlightPhaseIndex = 4;
                        break;
                    default:
                        FlightPhaseText = "UNKNOWN";
                        FlightPhaseBrush = new SolidColorBrush(Colors.Gray);
                        break;
                }
            });
        }

        /// <summary>
        /// Handler for flight plan changed events
        /// </summary>
        private void OnFlightPlanChanged(FlightPlanChangedEvent evt)
        {
            FlightNumber = evt.FlightNumber ?? "No Flight";
        }

        /// <summary>
        /// Shows the help dialog
        /// </summary>
        private void ShowHelp()
        {
            MessageBox.Show(
                "Prosim2GSX provides integration between Prosim A320 and GSX Pro.\n\n" +
                "For more information, please refer to the documentation.",
                "Prosim2GSX Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Shows the Settings tab
        /// </summary>
        private void ShowSettings()
        {
            SelectedTabIndex = 2; // Settings tab
        }

        /// <summary>
        /// Shows the Audio Settings tab
        /// </summary>
        private void ShowAudioSettings()
        {
            SelectedTabIndex = 1; // Audio Settings tab
        }

        #endregion
    }
}
