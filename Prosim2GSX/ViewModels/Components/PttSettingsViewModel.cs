using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.PTT.Enums;
using Prosim2GSX.Services.PTT.Events;
using Prosim2GSX.Services.PTT.Interface;
using Prosim2GSX.Services.PTT.Models;
using Prosim2GSX.ViewModels.Base;
using Prosim2GSX.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the PTT Settings panel
    /// </summary>
    public class PttSettingsViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly ServiceModel _serviceModel;
        private IPttService _pttService;
        private readonly ILogger<PttSettingsViewModel> _logger;
        private readonly List<SubscriptionToken> _subscriptionTokens = new List<SubscriptionToken>();

        private bool _isPttEnabled;
        private string _currentButtonText;
        private string _detectionStatusText;
        private bool _isDetecting;
        private ObservableCollection<ChannelMappingViewModel> _channelMappings;
        private string _activeChannel;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether PTT functionality is enabled
        /// </summary>
        public bool IsPttEnabled
        {
            get => _isPttEnabled;
            set
            {
                if (SetProperty(ref _isPttEnabled, value))
                {
                    _logger?.LogInformation("PTT enabled changed to: {Enabled}", value);
                    _serviceModel.SetPttEnabled(value);

                    if (_pttService != null)
                    {
                        if (value)
                        {
                            _pttService.StartMonitoring();
                        }
                        else
                        {
                            _pttService.StopMonitoring();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the text displaying the currently configured input button
        /// </summary>
        public string CurrentButtonText
        {
            get => _currentButtonText;
            set => SetProperty(ref _currentButtonText, value);
        }

        /// <summary>
        /// Gets or sets the status text for button detection
        /// </summary>
        public string DetectionStatusText
        {
            get => _detectionStatusText;
            set => SetProperty(ref _detectionStatusText, value);
        }

        /// <summary>
        /// Gets whether button detection is currently active
        /// </summary>
        public bool IsDetecting
        {
            get => _isDetecting;
            set => SetProperty(ref _isDetecting, value);
        }

        /// <summary>
        /// Gets the collection of channel mappings for display in the UI
        /// </summary>
        public ObservableCollection<ChannelMappingViewModel> ChannelMappings
        {
            get => _channelMappings;
            set => SetProperty(ref _channelMappings, value);
        }

        /// <summary>
        /// Gets or sets the current active channel
        /// </summary>
        public string ActiveChannel
        {
            get => _activeChannel;
            set => SetProperty(ref _activeChannel, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to start detecting button input
        /// </summary>
        public ICommand DetectButtonCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of PttSettingsViewModel
        /// </summary>
        /// <param name="serviceModel">Service model for application state</param>
        /// <param name="pttService">PTT service for handling PTT functionality</param>
        /// <param name="logger">Logger for this view model</param>
        public PttSettingsViewModel(ServiceModel serviceModel, IPttService pttService, ILogger<PttSettingsViewModel> logger)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _pttService = pttService; // May be null initially
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogDebug("Initializing PttSettingsViewModel");

            // Initialize commands
            DetectButtonCommand = new RelayCommand(_ => StartButtonDetection(), _ => _pttService != null);

            // Initialize properties from ServiceModel
            _isPttEnabled = _serviceModel.PttEnabled;

            // Initialize UI with current settings
            UpdateButtonDisplay();
            InitializeChannelMappings();

            // Set a default detection status
            DetectionStatusText = "Click 'Detect Input' to configure a button or key";

            // Set default active channel
            ActiveChannel = "None";

            // Subscribe to events if service is available
            if (_pttService != null)
            {
                SubscribeToEvents();
            }

            _logger.LogDebug("PttSettingsViewModel initialized");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the UI to show the current button configuration
        /// </summary>
        private void UpdateButtonDisplay()
        {
            if (_serviceModel.PttUseJoystick)
            {
                // Try to get joystick name
                int joystickId = _serviceModel.PttJoystickId;
                int buttonId = _serviceModel.PttJoystickButton;

                if (joystickId >= 0 && buttonId >= 0)
                {
                    // If we have a service, try to get the actual name
                    if (_pttService != null)
                    {
                        var joystickConfig = _pttService.GetJoystickConfiguration();
                        if (joystickConfig != null)
                        {
                            CurrentButtonText = joystickConfig.ToString();
                            return;
                        }
                    }

                    // Otherwise just show the IDs
                    CurrentButtonText = $"Joystick {joystickId}, Button {buttonId + 1}";
                }
                else
                {
                    CurrentButtonText = "Not configured";
                }
            }
            else
            {
                CurrentButtonText = string.IsNullOrEmpty(_serviceModel.PttKeyName)
                    ? "Not configured"
                    : _serviceModel.PttKeyName;
            }
        }

        /// <summary>
        /// Initializes the channel mappings collection
        /// </summary>
        private void InitializeChannelMappings()
        {
            _logger.LogDebug("Initializing channel mappings");

            ChannelMappings = new ObservableCollection<ChannelMappingViewModel>();

            // Create a mapping view model for each ACP channel type
            foreach (AcpChannelType channelType in Enum.GetValues(typeof(AcpChannelType)))
            {
                if (channelType == AcpChannelType.None) continue;

                // Get the channel configuration from the service model
                if (_serviceModel.PttChannelConfigurations.TryGetValue(channelType, out var config))
                {
                    // Create a view model for this channel
                    var channelVM = new ChannelMappingViewModel(
                        channelType,
                        config,
                        _pttService,
                        _serviceModel);

                    // Add to collection
                    ChannelMappings.Add(channelVM);
                }
            }
        }

        /// <summary>
        /// Updates mappings to use the PTT service when it becomes available
        /// </summary>
        private void UpdateChannelMappingsWithService()
        {
            foreach (var mapping in ChannelMappings)
            {
                mapping.SetPttService(_pttService);
            }

            // Update active channel if available
            if (_pttService != null)
            {
                UpdateActiveChannel(_pttService.CurrentChannel);
            }
        }

        /// <summary>
        /// Starts the button detection process
        /// </summary>
        private void StartButtonDetection()
        {
            if (IsDetecting || _pttService == null) return;

            _logger.LogInformation("Starting button detection");
            IsDetecting = true;
            DetectionStatusText = "Press a key or button...";

            // Start input capture with callback for when input is detected
            _pttService.StartInputCapture(OnInputDetected);
        }

        /// <summary>
        /// Called when input is detected during detection mode
        /// </summary>
        /// <param name="displayName">The display name of the detected input</param>
        private void OnInputDetected(string displayName)
        {
            ExecuteOnUIThread(() =>
            {
                _logger.LogInformation("Input detected: {Input}", displayName);
                CurrentButtonText = displayName;
                DetectionStatusText = $"Configured to use: {displayName}";
                IsDetecting = false;
            });
        }

        /// <summary>
        /// Updates the active channel display
        /// </summary>
        /// <param name="channelType">The active channel type</param>
        private void UpdateActiveChannel(AcpChannelType channelType)
        {
            ActiveChannel = channelType.ToString();

            // Update expanded state of channel mappings
            foreach (var mapping in ChannelMappings)
            {
                if (mapping.Channel == channelType.ToString())
                {
                    mapping.IsExpanded = true;
                }
            }
        }

        /// <summary>
        /// Subscribes to PTT-related events
        /// </summary>
        private void SubscribeToEvents()
        {
            _logger.LogDebug("Subscribing to PTT events");

            // Subscribe to active channel changed events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<PttStateChangedEvent>(OnPttStateChanged));
        }

        /// <summary>
        /// Handler for PTT state changed events
        /// </summary>
        private void OnPttStateChanged(PttStateChangedEvent evt)
        {
            ExecuteOnUIThread(() =>
            {
                // Update active channel when it changes
                UpdateActiveChannel(evt.ChannelType);
            });
        }

        /// <summary>
        /// Sets the PTT service when it becomes available
        /// </summary>
        /// <param name="pttService">The PTT service</param>
        public void SetPttService(IPttService pttService)
        {
            if (pttService == null || _pttService == pttService) return;

            _pttService = pttService;
            _logger.LogInformation("PTT service connected to ViewModel");

            // Update the button display with service
            UpdateButtonDisplay();

            // Update channel mappings with service
            UpdateChannelMappingsWithService();

            // Subscribe to events now that the service is available
            SubscribeToEvents();
        }

        /// <summary>
        /// Cleans up resources used by the view model
        /// </summary>
        public void Cleanup()
        {
            _logger?.LogDebug("Cleaning up PttSettingsViewModel");

            // Unsubscribe from events
            foreach (var token in _subscriptionTokens)
            {
                EventAggregator.Instance.Unsubscribe<EventBase>(token);
            }
            _subscriptionTokens.Clear();
        }

        #endregion
    }

    /// <summary>
    /// ViewModel for a single channel mapping
    /// </summary>
    public class ChannelMappingViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly AcpChannelType _channelType;
        private readonly ServiceModel _serviceModel;
        private IPttService _pttService;

        private bool _enabled;
        private string _applicationName;
        private string _keyboardShortcut;
        private bool _isExpanded;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the channel name
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Gets or sets whether this channel mapping is enabled
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (SetProperty(ref _enabled, value))
                {
                    if (_pttService != null)
                    {
                        _pttService.SetChannelEnabled(_channelType, value);
                    }
                    else
                    {
                        // Update ServiceModel directly
                        if (_serviceModel.PttChannelConfigurations.TryGetValue(_channelType, out var config))
                        {
                            config.Enabled = value;
                            _serviceModel.SavePttChannelConfig(_channelType);
                        }
                    }

                    // Update the IsExpanded property based on enabled state
                    if (value) IsExpanded = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the application name
        /// </summary>
        public string ApplicationName
        {
            get => _applicationName;
            set
            {
                if (SetProperty(ref _applicationName, value))
                {
                    if (_pttService != null)
                    {
                        _pttService.SetChannelTargetApplication(_channelType, value);
                    }
                    else
                    {
                        // Update ServiceModel directly
                        if (_serviceModel.PttChannelConfigurations.TryGetValue(_channelType, out var config))
                        {
                            config.TargetApplication = value;
                            _serviceModel.SavePttChannelConfig(_channelType);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the keyboard shortcut
        /// </summary>
        public string KeyboardShortcut
        {
            get => _keyboardShortcut;
            set
            {
                if (SetProperty(ref _keyboardShortcut, value))
                {
                    if (_pttService != null)
                    {
                        _pttService.SetChannelKeyMapping(_channelType, value);
                    }
                    else
                    {
                        // Update ServiceModel directly
                        if (_serviceModel.PttChannelConfigurations.TryGetValue(_channelType, out var config))
                        {
                            config.KeyMapping = value;
                            _serviceModel.SavePttChannelConfig(_channelType);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether this channel mapping is expanded in the UI
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to set the key for this channel
        /// </summary>
        public ICommand SetKeyCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of ChannelMappingViewModel
        /// </summary>
        /// <param name="channelType">The type of ACP channel</param>
        /// <param name="config">The channel configuration</param>
        /// <param name="pttService">The PTT service</param>
        /// <param name="serviceModel">The service model</param>
        public ChannelMappingViewModel(
            AcpChannelType channelType,
            PttChannelConfig config,
            IPttService pttService,
            ServiceModel serviceModel)
        {
            _channelType = channelType;
            _pttService = pttService; // May be null initially
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));

            Channel = channelType.ToString();
            _enabled = config.Enabled;
            _applicationName = config.TargetApplication;
            _keyboardShortcut = config.KeyMapping;
            _isExpanded = config.Enabled;

            // Initialize commands
            SetKeyCommand = new RelayCommand(_ => SetKey(), _ => _pttService != null);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the PTT service when it becomes available
        /// </summary>
        /// <param name="pttService">The PTT service</param>
        public void SetPttService(IPttService pttService)
        {
            _pttService = pttService;

            // Refresh command can execute state
            (SetKeyCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Starts detection of a key for this channel
        /// </summary>
        private void SetKey()
        {
            // To be implemented in Phase 2
            // This would start a detection mode specifically for this channel
        }

        #endregion
    }
}
