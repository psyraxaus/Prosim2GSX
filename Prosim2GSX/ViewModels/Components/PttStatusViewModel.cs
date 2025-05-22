using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.PTT.Enums;
using Prosim2GSX.Services.PTT.Events;
using Prosim2GSX.Services.PTT.Interface;
using Prosim2GSX.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for displaying PTT status
    /// </summary>
    public class PttStatusViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly ServiceModel _serviceModel;
        private IPttService _pttService;
        private readonly ILogger<PttStatusViewModel> _logger;
        private readonly List<SubscriptionToken> _subscriptionTokens = new List<SubscriptionToken>();

        private string _activeChannel;
        private string _activeApplication;
        private string _statusMessage;
        private bool _isPttActive;
        private bool _isChannelDisabled;
        private Brush _statusMessageColor;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the currently active channel
        /// </summary>
        public string ActiveChannel
        {
            get => _activeChannel;
            set => SetProperty(ref _activeChannel, value);
        }

        /// <summary>
        /// Gets or sets the active application
        /// </summary>
        public string ActiveApplication
        {
            get => _activeApplication;
            set => SetProperty(ref _activeApplication, value);
        }

        /// <summary>
        /// Gets or sets the status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets or sets whether PTT is currently active
        /// </summary>
        public bool IsPttActive
        {
            get => _isPttActive;
            set
            {
                if (SetProperty(ref _isPttActive, value))
                {
                    UpdateStatusMessageColor();
                }
            }
        }

        /// <summary>
        /// Gets the color of the status message text based on current state
        /// </summary>
        public Brush StatusMessageColor
        {
            get => _statusMessageColor;
            private set => SetProperty(ref _statusMessageColor, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of PttStatusViewModel
        /// </summary>
        /// <param name="serviceModel">Service model for application state</param>
        /// <param name="pttService">PTT service for handling PTT functionality</param>
        /// <param name="logger">Logger for this view model</param>
        public PttStatusViewModel(ServiceModel serviceModel, IPttService pttService, ILogger<PttStatusViewModel> logger)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _pttService = pttService; // May be null initially
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogDebug("Initializing PttStatusViewModel");

            // Initialize properties from ServiceModel
            ActiveChannel = "None";
            ActiveApplication = "Not Configured";
            StatusMessage = _serviceModel.PttEnabled ? "PTT Inactive" : "PTT Disabled";
            IsPttActive = false;

            // Subscribe to events if service is available
            if (_pttService != null)
            {
                SubscribeToEvents();
            }

            _logger.LogDebug("PttStatusViewModel initialized");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Subscribes to PTT-related events
        /// </summary>
        private void SubscribeToEvents()
        {
            _logger.LogDebug("Subscribing to PTT events");

            // Subscribe to active channel changed events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<PttStateChangedEvent>(OnPttStateChanged));

            // Subscribe to PTT button state changed events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<PttButtonStateChangedEvent>(OnPttButtonStateChanged));

            // Subscribe to channel configuration changes
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<PttChannelConfigChangedEvent>(OnPttChannelConfigChanged));
        }

        /// <summary>
        /// Handler for PTT state changed events
        /// </summary>
        private void OnPttStateChanged(PttStateChangedEvent evt)
        {
            ExecuteOnUIThread(() =>
            {
                _logger.LogDebug("PTT state changed: Channel={Channel}, Active={Active}", evt.ChannelType, evt.IsActive);

                // Update the active channel
                ActiveChannel = evt.ChannelType.ToString();

                // Check if this channel is explicitly disabled
                IsChannelDisabled = evt.ChannelConfig != null && !evt.ChannelConfig.Enabled;

                // Update the active application
                if (evt.ChannelType == AcpChannelType.None)
                {
                    ActiveApplication = "None";
                }
                else if (evt.ChannelConfig == null || !evt.ChannelConfig.Enabled)
                {
                    // For disabled channels, still set "Not Configured" for back-compatibility
                    ActiveApplication = "Not Configured";
                }
                else
                {
                    ActiveApplication = !string.IsNullOrEmpty(evt.ChannelConfig.TargetApplication)
                        ? evt.ChannelConfig.TargetApplication
                        : "Not Configured";
                }

                // Update PTT active state
                IsPttActive = evt.IsActive;

                // Update status message
                UpdateStatusMessage();
                UpdateStatusMessageColor();
            });
        }

        /// <summary>
        /// Handler for PTT button state changed events
        /// </summary>
        private void OnPttButtonStateChanged(PttButtonStateChangedEvent evt)
        {
            ExecuteOnUIThread(() =>
            {
                _logger.LogDebug("PTT button state changed: Pressed={Pressed}", evt.IsPressed);

                // If channel is disabled, ignore PTT pressed events
                if (IsChannelDisabled && evt.IsPressed)
                {
                    _logger.LogDebug("Ignoring PTT press for disabled channel");
                    return;
                }

                // Update PTT active state
                IsPttActive = evt.IsPressed;

                // Update active channel if provided
                if (!string.IsNullOrEmpty(evt.ChannelName))
                {
                    ActiveChannel = evt.ChannelName;
                }

                // Update active application if provided, otherwise keep current
                if (!string.IsNullOrEmpty(evt.ApplicationName))
                {
                    ActiveApplication = evt.ApplicationName;
                }
                // If application name is not provided but we're in "None" channel, set to "Not Configured"
                else if (ActiveChannel == "None" || string.IsNullOrEmpty(ActiveApplication))
                {
                    ActiveApplication = "Not Configured";
                }

                // Update status message and color
                UpdateStatusMessage();
                UpdateStatusMessageColor();
            });
        }

        /// <summary>
        /// Updates the status message based on current state
        /// </summary>
        private void UpdateStatusMessage()
        {
            if (!_serviceModel.PttEnabled)
            {
                StatusMessage = "PTT Disabled";
            }
            else if (IsChannelDisabled)
            {
                StatusMessage = $"Channel {ActiveChannel} is disabled in PTT Settings";
            }
            else if (IsPttActive)
            {
                StatusMessage = $"PTT Active on {ActiveChannel}";
            }
            else
            {
                StatusMessage = "PTT Inactive";
            }
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

            // Initialize with current service state
            if (_pttService != null)
            {
                ActiveChannel = _pttService.CurrentChannel.ToString();
                var channelConfig = _pttService.GetChannelConfig(_pttService.CurrentChannel);

                // Set the disabled state
                IsChannelDisabled = channelConfig != null && !channelConfig.Enabled;

                // More explicit handling of the application name
                if (_pttService.CurrentChannel == AcpChannelType.None)
                {
                    ActiveApplication = "None";
                }
                else if (channelConfig == null || !channelConfig.Enabled)
                {
                    ActiveApplication = "Not Configured";
                }
                else
                {
                    ActiveApplication = !string.IsNullOrEmpty(channelConfig.TargetApplication)
                        ? channelConfig.TargetApplication
                        : "Not Configured";
                }

                IsPttActive = _pttService.IsActive;
                UpdateStatusMessage();
                UpdateStatusMessageColor();
            }

            // Subscribe to events now that the service is available
            SubscribeToEvents();
        }

        /// <summary>
        /// Gets or sets whether the current channel is explicitly disabled in settings
        /// </summary>
        public bool IsChannelDisabled
        {
            get => _isChannelDisabled;
            set
            {
                if (SetProperty(ref _isChannelDisabled, value))
                {
                    UpdateStatusMessageColor();
                }
            }
        }

        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            _logger.LogDebug("Cleaning up PttStatusViewModel");

            // Unsubscribe from all events
            foreach (var token in _subscriptionTokens)
            {
                EventAggregator.Instance.Unsubscribe<EventBase>(token);
            }
            _subscriptionTokens.Clear();
        }

        /// <summary>
        /// Handler for PTT channel configuration changed events
        /// </summary>
        private void OnPttChannelConfigChanged(PttChannelConfigChangedEvent evt)
        {
            ExecuteOnUIThread(() =>
            {
                _logger.LogDebug("PTT channel config changed: Channel={Channel}, Enabled={Enabled}",
                    evt.ChannelType, evt.IsEnabled);

                // Only update if this is the currently active channel
                if (_pttService != null && _pttService.CurrentChannel == evt.ChannelType)
                {
                    // Important: Update the IsChannelDisabled property to reflect new state
                    IsChannelDisabled = !evt.IsEnabled;

                    // Get the updated channel config
                    var config = _pttService.GetChannelConfig(evt.ChannelType);

                    // Update application name based on new state
                    if (!evt.IsEnabled)
                    {
                        ActiveApplication = "Not Configured";
                        // Ensure PTT is not active for disabled channels
                        IsPttActive = false;
                    }
                    else if (config != null && !string.IsNullOrEmpty(config.TargetApplication))
                    {
                        ActiveApplication = config.TargetApplication;
                    }
                    else
                    {
                        ActiveApplication = "Not Configured";
                    }

                    // Update status message and color
                    UpdateStatusMessage();
                    UpdateStatusMessageColor();
                }
            });
        }

        /// <summary>
        /// Updates the status message color based on current state
        /// </summary>
        private void UpdateStatusMessageColor()
        {
            if (IsChannelDisabled)
            {
                StatusMessageColor = new SolidColorBrush(Colors.OrangeRed);
            }
            else if (IsPttActive)
            {
                StatusMessageColor = new SolidColorBrush(Colors.LimeGreen);
            }
            else
            {
                StatusMessageColor = new SolidColorBrush(Colors.Gray);
            }
        }

        #endregion
    }
}
