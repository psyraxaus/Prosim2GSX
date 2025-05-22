using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.PTT.Enums;
using Prosim2GSX.Services.PTT.Events;
using Prosim2GSX.Services.PTT.Interface;
using Prosim2GSX.Services.PTT.Models;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Windows.Gaming.Input;

namespace Prosim2GSX.Services.PTT.Implementations
{
    /// <summary>
    /// Service for managing Push-to-Talk (PTT) functionality with audio control panel integration
    /// </summary>
    public class PttService : IPttService
    {
        #region Private Fields

        private readonly ServiceModel _serviceModel;
        private readonly ILogger<PttService> _logger;
        private readonly object _stateLock = new object();

        private bool _isMonitoring;
        private AcpChannelType _currentChannel;
        private bool _isActive;
        private Action<string> _inputDetectCallback;
        private bool _isProsimConnected;
        private IDataRefMonitoringService _dataRefService;
        private System.Threading.Timer _monitoringTimer;

        // List of raw game controllers
        private List<RawGameController> _controllers = new List<RawGameController>();

        #endregion

        #region IPttService Implementation

        /// <summary>
        /// Gets whether the PTT service is currently monitoring for input
        /// </summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Gets the currently active ACP channel
        /// </summary>
        public AcpChannelType CurrentChannel => _currentChannel;

        /// <summary>
        /// Gets whether PTT is currently active
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Gets the current monitored joystick ID
        /// </summary>
        public int MonitoredJoystickId => _serviceModel.PttJoystickId;

        /// <summary>
        /// Gets the current monitored joystick button ID
        /// </summary>
        public int MonitoredJoystickButton => _serviceModel.PttJoystickButton;

        /// <summary>
        /// Gets whether joystick input is being used
        /// </summary>
        public bool IsUsingJoystickInput => _serviceModel.PttUseJoystick;

        /// <summary>
        /// Gets the name of the currently monitored key
        /// </summary>
        public string MonitoredKeyName => _serviceModel.PttKeyName;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of PttService
        /// </summary>
        /// <param name="serviceModel">The service model</param>
        /// <param name="logger">Logger for this service</param>
        public PttService(ServiceModel serviceModel, ILogger<PttService> logger)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("Initializing PttService");

            // Set default values
            _currentChannel = AcpChannelType.INT;
            _isActive = false;
            _isMonitoring = false;
            _isProsimConnected = false;

            // Initialize controllers list
            RawGameController.RawGameControllerAdded += RawGameController_Added;
            RawGameController.RawGameControllerRemoved += RawGameController_Removed;
            RefreshControllerList();

            _logger.LogInformation("PttService initialized");
        }

        private void RawGameController_Added(object sender, RawGameController e)
        {
            RefreshControllerList();
        }

        private void RawGameController_Removed(object sender, RawGameController e)
        {
            RefreshControllerList();
        }

        private void RefreshControllerList()
        {
            try
            {
                _controllers.Clear();
                foreach (var controller in RawGameController.RawGameControllers)
                {
                    _controllers.Add(controller);
                }
                _logger.LogDebug("Found {Count} controllers", _controllers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing controller list");
            }
        }

        #endregion

        #region IPttService Methods

        /// <summary>
        /// Starts monitoring for PTT input and ACP channel changes
        /// </summary>
        public void StartMonitoring()
        {
            lock (_stateLock)
            {
                if (_isMonitoring) return;

                _logger.LogInformation("Starting PTT monitoring");
                _isMonitoring = true;

                // Start a timer to poll for input
                _monitoringTimer = new System.Threading.Timer(
                    MonitorInput, null, 0, 50); // Poll every 50ms

                // Publish initial state
                PublishPttStateChanged(_currentChannel, false);
            }
        }

        /// <summary>
        /// Stops monitoring for PTT input and ACP channel changes
        /// </summary>
        public void StopMonitoring()
        {
            lock (_stateLock)
            {
                if (!_isMonitoring) return;

                _logger.LogInformation("Stopping PTT monitoring");
                _isMonitoring = false;

                // Stop the monitoring timer
                _monitoringTimer?.Dispose();
                _monitoringTimer = null;

                // Ensure PTT is deactivated
                DeactivatePtt();
            }
        }

        /// <summary>
        /// Sets the key mapping for a specific ACP channel
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <param name="keyMapping">The key mapping string</param>
        public void SetChannelKeyMapping(AcpChannelType channelType, string keyMapping)
        {
            if (!_serviceModel.PttChannelConfigurations.TryGetValue(channelType, out var config))
            {
                _logger.LogWarning("Attempted to set key mapping for unknown channel: {Channel}", channelType);
                return;
            }

            _logger.LogInformation("Setting key mapping for channel {Channel} to {KeyMapping}", channelType, keyMapping);
            config.KeyMapping = keyMapping;
            _serviceModel.SavePttChannelConfig(channelType);
        }

        /// <summary>
        /// Sets whether a specific ACP channel is enabled for PTT
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <param name="enabled">Whether the channel is enabled</param>
        public void SetChannelEnabled(AcpChannelType channelType, bool enabled)
        {
            if (!_serviceModel.PttChannelConfigurations.TryGetValue(channelType, out var config))
            {
                _logger.LogWarning("Attempted to set enabled state for unknown channel: {Channel}", channelType);
                return;
            }

            _logger.LogInformation("Setting enabled state for channel {Channel} to {Enabled}", channelType, enabled);
            config.Enabled = enabled;
            _serviceModel.SavePttChannelConfig(channelType);
        }

        /// <summary>
        /// Sets the target application for a specific ACP channel
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <param name="applicationName">The target application name</param>
        public void SetChannelTargetApplication(AcpChannelType channelType, string applicationName)
        {
            if (!_serviceModel.PttChannelConfigurations.TryGetValue(channelType, out var config))
            {
                _logger.LogWarning("Attempted to set target application for unknown channel: {Channel}", channelType);
                return;
            }

            _logger.LogInformation("Setting target application for channel {Channel} to {Application}", channelType, applicationName);
            config.TargetApplication = applicationName;
            _serviceModel.SavePttChannelConfig(channelType);
        }

        /// <summary>
        /// Sets whether a specific ACP channel uses toggle mode
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <param name="toggleMode">Whether to use toggle mode</param>
        public void SetChannelToggleMode(AcpChannelType channelType, bool toggleMode)
        {
            if (!_serviceModel.PttChannelConfigurations.TryGetValue(channelType, out var config))
            {
                _logger.LogWarning("Attempted to set toggle mode for unknown channel: {Channel}", channelType);
                return;
            }

            _logger.LogInformation("Setting toggle mode for channel {Channel} to {ToggleMode}", channelType, toggleMode);
            config.ToggleMode = toggleMode;
            _serviceModel.SavePttChannelConfig(channelType);
        }

        /// <summary>
        /// Gets the configuration for a specific ACP channel
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <returns>The channel configuration</returns>
        public PttChannelConfig GetChannelConfig(AcpChannelType channelType)
        {
            if (_serviceModel.PttChannelConfigurations.TryGetValue(channelType, out var config))
            {
                return config;
            }

            _logger.LogWarning("Requested configuration for unknown channel: {Channel}", channelType);
            return new PttChannelConfig(channelType);
        }

        /// <summary>
        /// Gets all channel configurations
        /// </summary>
        /// <returns>Dictionary of channel configurations by channel type</returns>
        public Dictionary<AcpChannelType, PttChannelConfig> GetAllChannelConfigs()
        {
            return new Dictionary<AcpChannelType, PttChannelConfig>(_serviceModel.PttChannelConfigurations);
        }

        /// <summary>
        /// Manually activates PTT for the current ACP channel
        /// </summary>
        public void ActivatePtt()
        {
            lock (_stateLock)
            {
                if (!_isMonitoring || _isActive) return;

                _logger.LogInformation("Manually activating PTT for channel {Channel}", _currentChannel);
                _isActive = true;

                // Get the channel configuration
                if (_serviceModel.PttChannelConfigurations.TryGetValue(_currentChannel, out var config) && config.Enabled)
                {
                    // TODO: Implement sending key press to target application
                    _logger.LogDebug("Would send key {Key} to application {App}",
                        config.KeyMapping, config.TargetApplication);
                }

                // Publish state change
                PublishPttStateChanged(_currentChannel, true);

                // Publish button state change
                PublishPttButtonStateChanged(true);
            }
        }

        /// <summary>
        /// Manually deactivates PTT for the current ACP channel
        /// </summary>
        public void DeactivatePtt()
        {
            lock (_stateLock)
            {
                if (!_isMonitoring || !_isActive) return;

                _logger.LogInformation("Manually deactivating PTT for channel {Channel}", _currentChannel);
                _isActive = false;

                // Get the channel configuration
                if (_serviceModel.PttChannelConfigurations.TryGetValue(_currentChannel, out var config) && config.Enabled)
                {
                    // TODO: Implement sending key release to target application
                    _logger.LogDebug("Would release key {Key} to application {App}",
                        config.KeyMapping, config.TargetApplication);
                }

                // Publish state change
                PublishPttStateChanged(_currentChannel, false);

                // Publish button state change
                PublishPttButtonStateChanged(false);
            }
        }

        /// <summary>
        /// Starts input capture mode to detect keyboard or joystick input
        /// </summary>
        /// <param name="callback">Callback to execute when input is detected</param>
        public void StartInputCapture(Action<string> callback)
        {
            _logger.LogInformation("Starting input capture");
            _inputDetectCallback = callback;

            // TODO: Implement actual key/button detection for both keyboard and joystick

            // For now, simulate a detection after a delay for testing
            System.Threading.Tasks.Task.Delay(3000).ContinueWith(t => {
                if (_inputDetectCallback != null)
                {
                    _inputDetectCallback("Space");
                    SetMonitoredKey("Space");
                    _inputDetectCallback = null;
                }
            });
        }

        /// <summary>
        /// Stops capturing input for PTT activation
        /// </summary>
        public void StopInputCapture()
        {
            _logger.LogInformation("Stopping input capture");
            _inputDetectCallback = null;

            // TODO: Stop detection mode for both keyboard and joystick
        }

        /// <summary>
        /// Sets the key to monitor for PTT activation
        /// </summary>
        /// <param name="keyName">The key name to monitor</param>
        public void SetMonitoredKey(string keyName)
        {
            _logger.LogInformation("Setting monitored key to {Key}", keyName);
            _serviceModel.SetPttInputMethod(false, keyName);
        }

        /// <summary>
        /// Saves a channel configuration to settings
        /// </summary>
        public void SaveChannelConfig(AcpChannelType channelType)
        {
            _serviceModel.SavePttChannelConfig(channelType);
        }

        /// <summary>
        /// Notifies the service that ProSim connection state has changed
        /// </summary>
        /// <param name="isConnected">Whether ProSim is now connected</param>
        public void SetProsimConnectionState(bool isConnected)
        {
            _logger.LogInformation("Prosim connection state changed to {Connected}", isConnected);
            _isProsimConnected = isConnected;

            if (isConnected)
            {
                // Get DataRefService from ServiceLocator
                _dataRefService = ServiceLocator.DataRefService;

                // Subscribe to the ACP channel dataref
                if (_dataRefService != null && _dataRefService.IsMonitoringActive)
                {
                    _dataRefService.SubscribeToDataRef("system.switches.S_ASP_SEND_CHANNEL", OnAcpChannelChanged);
                    _logger.LogDebug("Subscribed to ACP channel dataref");
                }
                else
                {
                    _logger.LogWarning("DataRefService not available or not monitoring, cannot subscribe to ACP channel dataref");
                }
            }
            else
            {
                // Stop monitoring when Prosim disconnects
                StopMonitoring();
            }
        }

        /// <summary>
        /// Gets available joysticks
        /// </summary>
        /// <returns>Dictionary of joystick IDs and names</returns>
        public Dictionary<int, string> GetAvailableJoysticks()
        {
            var joysticks = new Dictionary<int, string>();

            try
            {
                for (int i = 0; i < _controllers.Count; i++)
                {
                    joysticks[i] = _controllers[i].DisplayName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available joysticks");
            }

            return joysticks;
        }

        /// <summary>
        /// Gets the number of buttons for a specific joystick
        /// </summary>
        /// <param name="joystickId">The joystick ID</param>
        /// <returns>Number of buttons, or 0 if joystick not found</returns>
        public int GetJoystickButtonCount(int joystickId)
        {
            try
            {
                if (joystickId >= 0 && joystickId < _controllers.Count)
                {
                    return _controllers[joystickId].ButtonCount;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting joystick button count");
            }

            return 0;
        }

        /// <summary>
        /// Sets the joystick and button to monitor for PTT activation
        /// </summary>
        /// <param name="joystickId">The joystick ID</param>
        /// <param name="buttonId">The button ID</param>
        public void SetMonitoredJoystickButton(int joystickId, int buttonId)
        {
            _logger.LogInformation("Setting monitored joystick to {JoystickId}:{ButtonId}", joystickId, buttonId);
            _serviceModel.SetPttInputMethod(true, string.Empty, joystickId, buttonId);
        }

        /// <summary>
        /// Gets the current joystick configuration
        /// </summary>
        /// <returns>The joystick configuration, or null if not using joystick input</returns>
        public JoystickConfig GetJoystickConfiguration()
        {
            if (!_serviceModel.PttUseJoystick || _serviceModel.PttJoystickId < 0 || _serviceModel.PttJoystickButton < 0)
            {
                return null;
            }

            var joysticks = GetAvailableJoysticks();
            if (joysticks.TryGetValue(_serviceModel.PttJoystickId, out var name))
            {
                return new JoystickConfig(_serviceModel.PttJoystickId, _serviceModel.PttJoystickButton, name);
            }

            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when the ACP channel dataref changes
        /// </summary>
        private void OnAcpChannelChanged(string dataref, dynamic oldValue, dynamic newValue)
        {
            try
            {
                if (newValue is int channelIndex && Enum.IsDefined(typeof(AcpChannelType), channelIndex))
                {
                    var newChannel = (AcpChannelType)channelIndex;

                    if (_currentChannel != newChannel)
                    {
                        _logger.LogDebug("ACP channel changed from {OldChannel} to {NewChannel}", _currentChannel, newChannel);

                        // If PTT is active, deactivate it for the old channel
                        if (_isActive)
                        {
                            DeactivatePtt();
                        }

                        // Update current channel
                        _currentChannel = newChannel;

                        // Publish state change for new channel
                        PublishPttStateChanged(_currentChannel, false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ACP channel change");
            }
        }

        /// <summary>
        /// Monitors input devices for PTT activation
        /// </summary>
        private void MonitorInput(object state)
        {
            if (!_isMonitoring || !_isProsimConnected) return;

            try
            {
                bool inputActive = false;

                // Check for input based on configuration
                if (_serviceModel.PttUseJoystick)
                {
                    inputActive = CheckJoystickInput();
                }
                else
                {
                    inputActive = CheckKeyboardInput();
                }

                // Handle state change if needed
                if (inputActive && !_isActive)
                {
                    ActivatePtt();
                }
                else if (!inputActive && _isActive)
                {
                    DeactivatePtt();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring input");
            }
        }

        /// <summary>
        /// Checks if the configured joystick button is pressed
        /// </summary>
        private bool CheckJoystickInput()
        {
            try
            {
                int joystickId = _serviceModel.PttJoystickId;
                int buttonId = _serviceModel.PttJoystickButton;

                if (joystickId >= 0 && joystickId < _controllers.Count && buttonId >= 0)
                {
                    var controller = _controllers[joystickId];
                    if (buttonId < controller.ButtonCount)
                    {
                        // Create a buffer to store button states
                        bool[] buttonStates = new bool[controller.ButtonCount];
                        controller.GetCurrentReading(buttonStates, null, null);

                        return buttonStates[buttonId];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking joystick input");
            }

            return false;
        }

        /// <summary>
        /// Checks if the configured keyboard key is pressed
        /// </summary>
        private bool CheckKeyboardInput()
        {
            try
            {
                string keyName = _serviceModel.PttKeyName;

                if (!string.IsNullOrEmpty(keyName))
                {
                    // Try to parse the key name
                    if (Enum.TryParse<Keys>(keyName, true, out var key))
                    {
                        // Get key state using user32.dll interop
                        return (GetAsyncKeyState((int)key) & 0x8000) != 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking keyboard input");
            }

            return false;
        }

        /// <summary>
        /// Publishes a PTT state changed event
        /// </summary>
        private void PublishPttStateChanged(AcpChannelType channelType, bool isActive)
        {
            if (_serviceModel.PttChannelConfigurations.TryGetValue(channelType, out var config))
            {
                var evt = new PttStateChangedEvent(channelType, isActive, config);
                EventAggregator.Instance.Publish(evt);
            }
        }

        /// <summary>
        /// Publishes a PTT button state changed event
        /// </summary>
        private void PublishPttButtonStateChanged(bool isPressed)
        {
            string channelName = _currentChannel.ToString();
            string appName = string.Empty;

            if (_serviceModel.PttChannelConfigurations.TryGetValue(_currentChannel, out var config))
            {
                appName = config.TargetApplication;
            }

            var evt = new PttButtonStateChangedEvent(isPressed, channelName, appName);
            EventAggregator.Instance.Publish(evt);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        #endregion
    }
}
