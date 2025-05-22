using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.PTT.Events;
using Prosim2GSX.Services.PTT.Interface;
using Prosim2GSX.ViewModels.Base;
using System;
using System.Collections.Generic;

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
            set => SetProperty(ref _isPttActive, value);
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
            _logger.LogDebug("Subscribing to events");

            // Subscribe to PTT state changed events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<PttStateChangedEvent>(OnPttStateChanged));

            // Subscribe to PTT button state changed events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<PttButtonStateChangedEvent>(OnPttButtonStateChanged));
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

                // Update the active application
                ActiveApplication = evt.ChannelConfig?.Enabled == true ?
                    evt.ChannelConfig.TargetApplication : "Not Configured";

                // Update PTT active state
                IsPttActive = evt.IsActive;

                // Update status message
                UpdateStatusMessage();
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

                // Update PTT active state
                IsPttActive = evt.IsPressed;

                // Update active channel if provided
                if (!string.IsNullOrEmpty(evt.ChannelName))
                {
                    ActiveChannel = evt.ChannelName;
                }

                // Update active application if provided
                if (!string.IsNullOrEmpty(evt.ApplicationName))
                {
                    ActiveApplication = evt.ApplicationName;
                }

                // Update status message
                UpdateStatusMessage();
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
                ActiveApplication = channelConfig?.Enabled == true ?
                    channelConfig.TargetApplication : "Not Configured";

                IsPttActive = _pttService.IsActive;
                UpdateStatusMessage();
            }

            // Subscribe to events now that the service is available
            SubscribeToEvents();
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

        #endregion
    }
}
