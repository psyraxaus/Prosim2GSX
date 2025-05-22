using Microsoft.Extensions.Logging;
using Prosim2GSX.Models;
using Prosim2GSX.Services.PTT.Enums;
using Prosim2GSX.Services.PTT.Interface;
using Prosim2GSX.Services.PTT.Models;
using Prosim2GSX.Services;
using System.Windows.Input;
using System;
using Prosim2GSX.ViewModels.Base;
using Prosim2GSX.ViewModels.Commands;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for a single channel mapping
    /// </summary>
    public class ChannelMappingViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly AcpChannelType _channelType;
        private readonly ServiceModel _serviceModel;
        private IPttService _pttService;
        private readonly ILogger _logger;

        private bool _enabled;
        private string _applicationName;
        private string _keyboardShortcut;
        private bool _isExpanded;
        private string _detectionStatusText;
        private bool _isDetecting;

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
                    // Direct update to the ServiceModel, skipping the PttService
                    if (_serviceModel.PttChannelConfigurations.TryGetValue(_channelType, out var configExplicit))
                    {
                        configExplicit.Enabled = value;
                        _serviceModel.SavePttChannelConfig(_channelType);
                        _logger?.LogDebug("Saved Enabled={Enabled} for channel {Channel}", value, _channelType);
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
                    // Direct update to the ServiceModel, skipping the PttService
                    if (_serviceModel.PttChannelConfigurations.TryGetValue(_channelType, out var configExplicit))
                    {
                        configExplicit.TargetApplication = value;
                        _serviceModel.SavePttChannelConfig(_channelType);
                        _logger?.LogDebug("Saved ApplicationName={App} for channel {Channel}", value, _channelType);
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

        /// <summary>
        /// Gets or sets the status text for key detection
        /// </summary>
        public string DetectionStatusText
        {
            get => _detectionStatusText;
            set => SetProperty(ref _detectionStatusText, value);
        }

        /// <summary>
        /// Gets or sets whether key detection is currently active
        /// </summary>
        public bool IsDetecting
        {
            get => _isDetecting;
            set => SetProperty(ref _isDetecting, value);
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
            _logger = ServiceLocator.GetService<ILoggerFactory>()?.CreateLogger($"ChannelMappingViewModel.{channelType}");

            Channel = channelType.ToString();
            _enabled = config.Enabled;
            _applicationName = config.TargetApplication;
            _keyboardShortcut = config.KeyMapping;
            _isExpanded = config.Enabled;
            _detectionStatusText = "Click 'Set Key' to configure a shortcut key";

            // Initialize commands
            SetKeyCommand = new RelayCommand(_ => SetKey(), _ => _pttService != null && !IsDetecting);
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
            if (_pttService == null || IsDetecting) return;

            _logger?.LogInformation("Starting key detection for channel {Channel}", Channel);
            IsDetecting = true;
            DetectionStatusText = "Press a key...";

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
                _logger?.LogInformation("Input detected for channel {Channel}: {Input}", Channel, displayName);

                // First update our local property
                _keyboardShortcut = displayName;
                OnPropertyChanged(nameof(KeyboardShortcut));

                // Add this explicit call to update and save the config
                if (_serviceModel.PttChannelConfigurations.TryGetValue(_channelType, out var config))
                {
                    config.KeyMapping = displayName;
                    _serviceModel.SavePttChannelConfig(_channelType);
                    _logger?.LogDebug("Explicitly saved channel config for {Channel} with key {Key}", _channelType, displayName);
                }

                DetectionStatusText = $"Configured to use: {displayName}";
                IsDetecting = false;

                // Ensure the command can execute state is updated
                (SetKeyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            });
        }

        #endregion
    }
}
