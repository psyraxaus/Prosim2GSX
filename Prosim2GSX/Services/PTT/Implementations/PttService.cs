using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Prosim.Implementation;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.PTT.Enums;
using Prosim2GSX.Services.PTT.Events;
using Prosim2GSX.Services.PTT.Interface;
using Prosim2GSX.Services.PTT.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Gaming.Input;

namespace Prosim2GSX.Services.PTT.Implementations
{
    /// <summary>
    /// Service that handles PTT functionality including joystick and keyboard input detection
    /// and key command sending to target applications
    /// </summary>
    public class PttService : IPttService
    {
        #region Native Methods

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion

        #region Fields

        private readonly ServiceModel _serviceModel;
        private readonly ILogger<PttService> _logger;
        private readonly Dictionary<AcpChannelType, PttChannelConfig> _channelConfigs;

        private CancellationTokenSource _monitoringCts;
        private Task _monitoringTask;
        private Action<string> _inputCaptureCallback;
        private bool _isCapturing;
        private bool _prosimConnected;
        private bool _isPttActive;
        private AcpChannelType _currentChannel = AcpChannelType.None;

        // Joystick monitoring
        private readonly List<RawGameController> _controllers = new List<RawGameController>();
        private bool[] _prevButtonStates;

        private readonly IDataRefMonitoringService _dataRefMonitoringService;
        private readonly IProsimInterface _prosimInterface;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public bool IsMonitoring { get; private set; }

        /// <inheritdoc/>
        public AcpChannelType CurrentChannel => _currentChannel;

        /// <inheritdoc/>
        public bool IsActive => _isPttActive;

        /// <inheritdoc/>
        public int MonitoredJoystickId { get; private set; }

        /// <inheritdoc/>
        public int MonitoredJoystickButton { get; private set; }

        /// <inheritdoc/>
        public bool IsUsingJoystickInput => _serviceModel.PttUseJoystick;

        /// <inheritdoc/>
        public string MonitoredKeyName => _serviceModel.PttKeyName;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of PttService
        /// </summary>
        /// <param name="serviceModel">The service model for application state</param>
        /// <param name="logger">Logger for this service</param>
        public PttService(ServiceModel serviceModel, ILogger<PttService> logger, IDataRefMonitoringService dataRefMonitoringService, IProsimInterface prosimInterface)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataRefMonitoringService = dataRefMonitoringService ?? throw new ArgumentNullException(nameof(dataRefMonitoringService));
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));

            // Initialize channel configurations
            _channelConfigs = _serviceModel.PttChannelConfigurations;
            if (_channelConfigs == null || !_channelConfigs.Any())
            {
                _logger.LogInformation("No PTT channel configurations found, initializing defaults");
                _channelConfigs = InitializeDefaultChannelConfigs();
            }

            // Initialize joystick settings
            MonitoredJoystickId = _serviceModel.PttJoystickId;
            MonitoredJoystickButton = _serviceModel.PttJoystickButton;

            _logger.LogInformation("PTT Service initialized");

            // Register for game controller added/removed events
            RawGameController.RawGameControllerAdded += RawGameController_Added;
            RawGameController.RawGameControllerRemoved += RawGameController_Removed;

            // Initialize controllers list
            RefreshControllersList();

            // Start monitoring if enabled in settings
            if (_serviceModel.PttEnabled)
            {
                StartMonitoring();
            }
        }

        #endregion

        #region IPttService Implementation

        /// <inheritdoc/>
        public void StartMonitoring()
        {
            if (IsMonitoring) return;

            _logger.LogInformation("Starting PTT monitoring");
            IsMonitoring = true;

            // Cancel previous task if needed
            _monitoringCts?.Cancel();
            _monitoringCts = new CancellationTokenSource();

            // Start monitoring task
            _monitoringTask = Task.Run(() => MonitoringLoop(_monitoringCts.Token), _monitoringCts.Token);

            _logger.LogDebug("PTT monitoring started");
        }

        /// <inheritdoc/>
        public void StopMonitoring()
        {
            if (!IsMonitoring) return;

            _logger.LogInformation("Stopping PTT monitoring");
            IsMonitoring = false;

            // Cancel monitoring task
            _monitoringCts?.Cancel();
            _monitoringCts = null;
            _monitoringTask = null;

            // Ensure PTT is deactivated when stopping
            DeactivatePtt();

            _logger.LogDebug("PTT monitoring stopped");
        }

        /// <inheritdoc/>
        public void SetChannelKeyMapping(AcpChannelType channelType, string keyMapping)
        {
            if (!_channelConfigs.TryGetValue(channelType, out var config))
                return;

            _logger.LogInformation("Setting key mapping for channel {Channel}: {KeyMapping}",
                channelType, keyMapping);

            config.KeyMapping = keyMapping;
            _serviceModel.SavePttChannelConfig(channelType);
        }

        /// <inheritdoc/>
        public void SetChannelEnabled(AcpChannelType channelType, bool enabled)
        {
            if (!_channelConfigs.TryGetValue(channelType, out var config))
                return;

            _logger.LogInformation("Setting channel {Channel} enabled: {Enabled}",
                channelType, enabled);

            config.Enabled = enabled;
            _serviceModel.SavePttChannelConfig(channelType);
        }

        /// <inheritdoc/>
        public void SetChannelTargetApplication(AcpChannelType channelType, string applicationName)
        {
            if (!_channelConfigs.TryGetValue(channelType, out var config))
                return;

            _logger.LogInformation("Setting target application for channel {Channel}: {Application}",
                channelType, applicationName);

            config.TargetApplication = applicationName;
            _serviceModel.SavePttChannelConfig(channelType);
        }

        /// <inheritdoc/>
        public void SetChannelToggleMode(AcpChannelType channelType, bool toggleMode)
        {
            if (!_channelConfigs.TryGetValue(channelType, out var config))
                return;

            _logger.LogInformation("Setting toggle mode for channel {Channel}: {ToggleMode}",
                channelType, toggleMode);

            config.ToggleMode = toggleMode;
            _serviceModel.SavePttChannelConfig(channelType);
        }

        /// <inheritdoc/>
        public PttChannelConfig GetChannelConfig(AcpChannelType channelType)
        {
            return _channelConfigs.TryGetValue(channelType, out var config) ? config : null;
        }

        /// <inheritdoc/>
        public Dictionary<AcpChannelType, PttChannelConfig> GetAllChannelConfigs()
        {
            return new Dictionary<AcpChannelType, PttChannelConfig>(_channelConfigs);
        }

        /// <inheritdoc/>
        public void ActivatePtt()
        {
            if (_isPttActive || _currentChannel == AcpChannelType.None)
                return;

            _logger.LogDebug("Manually activating PTT for channel {Channel}", _currentChannel);

            if (_channelConfigs.TryGetValue(_currentChannel, out var config) && config.Enabled)
            {
                _isPttActive = true;

                // Send keyboard shortcut
                SendShortcutToApplication(config.TargetApplication, config.KeyMapping, true);

                // Raise events
                RaisePttStateChanged(true);
                RaisePttButtonStateChanged(true, _currentChannel.ToString(), config.TargetApplication);
            }
        }

        /// <inheritdoc/>
        public void DeactivatePtt()
        {
            if (!_isPttActive || _currentChannel == AcpChannelType.None)
                return;

            _logger.LogDebug("Manually deactivating PTT for channel {Channel}", _currentChannel);

            if (_channelConfigs.TryGetValue(_currentChannel, out var config) && config.Enabled)
            {
                _isPttActive = false;

                // Don't send key release if in toggle mode
                if (!config.ToggleMode)
                {
                    // Send keyboard shortcut release
                    SendShortcutToApplication(config.TargetApplication, config.KeyMapping, false);
                }

                // Raise events
                RaisePttStateChanged(false);
                RaisePttButtonStateChanged(false, _currentChannel.ToString(), config.TargetApplication);
            }
        }

        /// <inheritdoc/>
        public void StartInputCapture(Action<string> callback)
        {
            if (_isCapturing)
                return;

            _logger.LogInformation("Starting input capture");
            _inputCaptureCallback = callback;
            _isCapturing = true;

            // Start a separate task for input capture
            Task.Run(InputCaptureLoop);
        }

        /// <inheritdoc/>
        public void StopInputCapture()
        {
            _logger.LogInformation("Stopping input capture");
            _isCapturing = false;
            _inputCaptureCallback = null;
        }

        /// <inheritdoc/>
        public void SetMonitoredKey(string keyName)
        {
            _logger.LogInformation("Setting monitored key to: {Key}", keyName);
            _serviceModel.SetPttInputMethod(false, keyName: keyName);
        }

        /// <inheritdoc/>
        public void SetMonitoredJoystickButton(int joystickId, int buttonId)
        {
            _logger.LogInformation("Setting monitored joystick to: Joystick {JoystickId}, Button {ButtonId}",
                joystickId, buttonId);

            MonitoredJoystickId = joystickId;
            MonitoredJoystickButton = buttonId;
            _serviceModel.SetPttInputMethod(true, joystickId: joystickId, buttonId: buttonId);
        }

        /// <inheritdoc/>
        public JoystickConfig GetJoystickConfiguration()
        {
            if (!_serviceModel.PttUseJoystick || MonitoredJoystickId < 0 || MonitoredJoystickButton < 0)
                return null;

            if (MonitoredJoystickId >= _controllers.Count)
                RefreshControllersList();

            if (MonitoredJoystickId < _controllers.Count)
            {
                var controller = _controllers[MonitoredJoystickId];
                return new JoystickConfig(
                    MonitoredJoystickId,
                    MonitoredJoystickButton,
                    controller.DisplayName);
            }

            return null;
        }

        /// <inheritdoc/>
        public Dictionary<int, string> GetAvailableJoysticks()
        {
            RefreshControllersList();

            var joysticks = new Dictionary<int, string>();
            for (int i = 0; i < _controllers.Count; i++)
            {
                joysticks[i] = _controllers[i].DisplayName;
            }

            return joysticks;
        }

        /// <inheritdoc/>
        public int GetJoystickButtonCount(int joystickId)
        {
            if (joystickId < 0 || joystickId >= _controllers.Count)
                return 0;

            return _controllers[joystickId].ButtonCount;
        }

        /// <inheritdoc/>
        public void SetProsimConnectionState(bool isConnected)
        {
            _prosimConnected = isConnected;

            if (isConnected && IsMonitoring)
            {
                _logger.LogInformation("ProSim connected, starting monitoring of ACP channel selection");

                // Subscribe to the ACP channel dataref
                _dataRefMonitoringService.SubscribeToDataRef("system.switches.S_ASP_SEND_CHANNEL", OnAcpChannelChanged);
            }
            else if (!isConnected)
            {
                // Unsubscribe when disconnected
                _dataRefMonitoringService.UnsubscribeFromDataRef("system.switches.S_ASP_SEND_CHANNEL", OnAcpChannelChanged);
            }
        }

        /// <inheritdoc/>
        public void SaveChannelConfig(AcpChannelType channelType)
        {
            _serviceModel.SavePttChannelConfig(channelType);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Main monitoring loop that runs in a background task
        /// </summary>
        private void MonitoringLoop(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Monitoring loop started");

            try
            {
                bool pttWasPressed = false;

                while (!cancellationToken.IsCancellationRequested && IsMonitoring)
                {
                    // Monitor ProSim channel datarefs if connected
                    if (_prosimConnected)
                    {
                        // In full implementation, we would monitor dataref:
                        // "system.switches.S_ASP_SEND_CHANNEL"
                        UpdateChannelFromProSim();
                    }

                    // Check for PTT input
                    bool pttPressed = CheckPttInput();

                    // Handle state change
                    if (pttPressed != pttWasPressed)
                    {
                        if (pttPressed)
                        {
                            HandlePttPressed();
                        }
                        else
                        {
                            HandlePttReleased();
                        }

                        pttWasPressed = pttPressed;
                    }

                    // Avoid using too much CPU
                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PTT monitoring loop");
            }

            _logger.LogDebug("Monitoring loop ended");
        }

        /// <summary>
        /// Updates the current channel based on ProSim dataref
        /// </summary>
        private void UpdateChannelFromProSim()
        {
            // Since we're now using the dataref subscription to update the channel,
            // this method is less important, but we'll keep it for manual polling if needed

            try
            {
                if (_dataRefMonitoringService != null && _prosimConnected)
                {
                    // Try to get the current value directly
                    var channelValue = _prosimInterface.GetProsimVariable("system.switches.S_ASP_SEND_CHANNEL");
                    if (channelValue != null)
                    {
                        int intValue = Convert.ToInt32(channelValue);
                        AcpChannelType newChannel = GetChannelTypeFromValue(intValue);

                        if (_currentChannel != newChannel)
                        {
                            _currentChannel = newChannel;
                            _logger.LogDebug("Active channel changed to: {Channel}", _currentChannel);

                            // Raise state changed event with current active state
                            RaisePttStateChanged(_isPttActive);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating channel from ProSim");
            }
        }

        /// <summary>
        /// Maps a dataref integer value to an AcpChannelType enum value
        /// </summary>
        private AcpChannelType GetChannelTypeFromValue(int value)
        {
            // ProSim dataref value is 0-8, which corresponds directly to our enum values
            if (Enum.IsDefined(typeof(AcpChannelType), value))
            {
                return (AcpChannelType)value;
            }

            return AcpChannelType.None;
        }

        /// <summary>
        /// Checks if PTT input is currently active
        /// </summary>
        private bool CheckPttInput()
        {
            if (_serviceModel.PttUseJoystick)
            {
                return CheckJoystickInput();
            }
            else
            {
                return CheckKeyboardInput();
            }
        }

        /// <summary>
        /// Checks if the configured joystick button is pressed
        /// </summary>
        private bool CheckJoystickInput()
        {
            if (MonitoredJoystickId < 0 || MonitoredJoystickButton < 0)
                return false;

            if (_controllers.Count <= MonitoredJoystickId)
                RefreshControllersList();

            if (_controllers.Count <= MonitoredJoystickId)
                return false;

            var controller = _controllers[MonitoredJoystickId];
            if (controller.ButtonCount <= MonitoredJoystickButton)
                return false;

            // Get button state
            bool[] buttonStates = new bool[controller.ButtonCount];
            GameControllerSwitchPosition[] switchStates = new GameControllerSwitchPosition[controller.SwitchCount];
            double[] axisValues = new double[controller.AxisCount];

            // Get the current reading with the proper parameters
            controller.GetCurrentReading(buttonStates, switchStates, axisValues);

            // Check if the button is pressed
            return buttonStates[MonitoredJoystickButton];
        }

        /// <summary>
        /// Checks if the configured keyboard key is pressed
        /// </summary>
        private bool CheckKeyboardInput()
        {
            if (string.IsNullOrEmpty(_serviceModel.PttKeyName))
                return false;

            // Parse the key name and check if it's pressed
            if (Enum.TryParse<Keys>(_serviceModel.PttKeyName, out var key))
            {
                return IsKeyDown(key);
            }

            return false;
        }

        /// <summary>
        /// Check if a specific key is currently pressed
        /// </summary>
        private bool IsKeyDown(Keys key)
        {
            // Using GetAsyncKeyState to check key state
            // High bit (0x8000) is set if key is currently pressed
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        /// <summary>
        /// Handle PTT button being pressed
        /// </summary>
        private void HandlePttPressed()
        {
            if (_currentChannel == AcpChannelType.None || !_channelConfigs.TryGetValue(_currentChannel, out var config) || !config.Enabled)
                return;

            _logger.LogDebug("PTT pressed for channel: {Channel}", _currentChannel);
            _isPttActive = true;

            // Send keyboard shortcut to application
            SendShortcutToApplication(config.TargetApplication, config.KeyMapping, true);

            // Raise events
            RaisePttStateChanged(true);
            RaisePttButtonStateChanged(true, _currentChannel.ToString(), config.TargetApplication);
        }

        /// <summary>
        /// Handle PTT button being released
        /// </summary>
        private void HandlePttReleased()
        {
            if (_currentChannel == AcpChannelType.None || !_channelConfigs.TryGetValue(_currentChannel, out var config) || !config.Enabled)
                return;

            // Skip deactivation if in toggle mode
            if (config.ToggleMode)
                return;

            _logger.LogDebug("PTT released for channel: {Channel}", _currentChannel);
            _isPttActive = false;

            // Send keyboard shortcut to application
            SendShortcutToApplication(config.TargetApplication, config.KeyMapping, false);

            // Raise events
            RaisePttStateChanged(false);
            RaisePttButtonStateChanged(false, _currentChannel.ToString(), config.TargetApplication);
        }

        /// <summary>
        /// Send a keyboard shortcut without requiring a specific application window
        /// </summary>
        /// <summary>
        /// Send a keyboard shortcut to an application
        /// </summary>
        private void SendShortcutToApplication(string applicationName, string shortcut, bool keyDown)
        {
            if (string.IsNullOrEmpty(shortcut))
                return;

            try
            {
                // Parse the shortcut
                if (Enum.TryParse<Keys>(shortcut, out var key))
                {
                    // Send key using keybd_event which is more reliable for global keypresses
                    if (keyDown)
                    {
                        keybd_event((byte)key, 0, 0, 0);
                    }
                    else
                    {
                        keybd_event((byte)key, 0, KEYEVENTF_KEYUP, 0);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not parse shortcut key: {Shortcut}", shortcut);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending shortcut key: {Shortcut}", shortcut);
            }
        }


        /// <summary>
        /// Find the window handle for an application
        /// </summary>
        private IntPtr FindApplicationWindow(string applicationName)
        {
            try
            {
                var processes = Process.GetProcessesByName(applicationName);
                if (processes.Length > 0)
                {
                    return processes[0].MainWindowHandle;
                }

                // Try direct window finding
                return FindWindow(null, applicationName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding application window: {Application}", applicationName);
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Loop that captures keyboard or joystick input for configuration
        /// </summary>
        private void InputCaptureLoop()
        {
            _logger.LogDebug("Input capture loop started");

            try
            {
                // Refresh controllers list
                RefreshControllersList();

                // Store initial state of joystick buttons
                Dictionary<int, bool[]> initialButtonStates = new Dictionary<int, bool[]>();
                foreach (var controller in _controllers)
                {
                    int controllerIdx = _controllers.IndexOf(controller);

                    bool[] buttonStates = new bool[controller.ButtonCount];
                    GameControllerSwitchPosition[] switchStates = new GameControllerSwitchPosition[controller.SwitchCount];
                    double[] axisValues = new double[controller.AxisCount];

                    // Get the current reading with the proper parameters
                    controller.GetCurrentReading(buttonStates, switchStates, axisValues);

                    initialButtonStates[controllerIdx] = buttonStates;
                }

                // Main capture loop
                while (_isCapturing)
                {
                    // Check for keyboard input
                    foreach (Keys key in Enum.GetValues(typeof(Keys)))
                    {
                        if (IsKeyDown(key) && !IsSystemKey(key))
                        {
                            _logger.LogDebug("Detected keyboard input: {Key}", key);

                            // Set as monitored key
                            SetMonitoredKey(key.ToString());

                            // Notify callback
                            _inputCaptureCallback?.Invoke(key.ToString());

                            _isCapturing = false;
                            return;
                        }
                    }

                    // Check for joystick input
                    for (int joystickId = 0; joystickId < _controllers.Count; joystickId++)
                    {
                        var controller = _controllers[joystickId];
                        bool[] initialStates = initialButtonStates[joystickId];
                        bool[] currentStates = new bool[controller.ButtonCount];
                        GameControllerSwitchPosition[] switchStates = new GameControllerSwitchPosition[controller.SwitchCount];
                        double[] axisValues = new double[controller.AxisCount];

                        // Get the current reading with the proper parameters
                        controller.GetCurrentReading(currentStates, switchStates, axisValues);

                        // Check if any button was newly pressed
                        for (int buttonId = 0; buttonId < currentStates.Length; buttonId++)
                        {
                            if (currentStates[buttonId] && !initialStates[buttonId])
                            {
                                _logger.LogDebug("Detected joystick input: Joystick {JoystickId}, Button {ButtonId}",
                                    joystickId, buttonId);

                                // Set as monitored joystick button
                                SetMonitoredJoystickButton(joystickId, buttonId);

                                // Notify callback
                                string displayName = $"{controller.DisplayName} (Button {buttonId + 1})";
                                _inputCaptureCallback?.Invoke(displayName);

                                _isCapturing = false;
                                return;
                            }
                        }
                    }

                    // Avoid using too much CPU
                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in input capture loop");
                _isCapturing = false;
            }

            _logger.LogDebug("Input capture loop ended");
        }

        /// <summary>
        /// Check if a key is a system key that should be ignored for input capture
        /// </summary>
        private bool IsSystemKey(Keys key)
        {
            // Skip modifier keys, windows keys, etc.
            return key == Keys.LWin || key == Keys.RWin ||
                   key == Keys.Apps || key == Keys.Sleep ||
                   key == Keys.ShiftKey || key == Keys.ControlKey ||
                   key == Keys.Menu || key == Keys.Alt ||
                   key == Keys.Tab || key == Keys.Escape;
        }

        /// <summary>
        /// Refreshes the list of connected controllers
        /// </summary>
        private void RefreshControllersList()
        {
            _controllers.Clear();

            try
            {
                foreach (var controller in RawGameController.RawGameControllers)
                {
                    _controllers.Add(controller);
                }

                _logger.LogDebug("Refreshed controllers list: {Count} controllers found", _controllers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing controllers list");
            }
        }

        /// <summary>
        /// Raises a PttStateChangedEvent
        /// </summary>
        private void RaisePttStateChanged(bool isActive)
        {
            if (_currentChannel == AcpChannelType.None)
                return;

            try
            {
                var config = GetChannelConfig(_currentChannel);
                var evt = new PttStateChangedEvent(_currentChannel, isActive, config);
                EventAggregator.Instance.Publish(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising PTT state changed event");
            }
        }

        /// <summary>
        /// Raises a PttButtonStateChangedEvent
        /// </summary>
        private void RaisePttButtonStateChanged(bool isPressed, string channelName = null, string applicationName = null)
        {
            try
            {
                var evt = new PttButtonStateChangedEvent(isPressed, channelName, applicationName);
                EventAggregator.Instance.Publish(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising PTT button state changed event");
            }
        }

        /// <summary>
        /// Initializes the default channel configurations
        /// </summary>
        private Dictionary<AcpChannelType, PttChannelConfig> InitializeDefaultChannelConfigs()
        {
            var configs = new Dictionary<AcpChannelType, PttChannelConfig>();

            foreach (AcpChannelType channelType in Enum.GetValues(typeof(AcpChannelType)))
            {
                if (channelType == AcpChannelType.None) continue;

                configs[channelType] = new PttChannelConfig(channelType)
                {
                    Enabled = false,
                    TargetApplication = string.Empty,
                    KeyMapping = "T", // Default to T key
                    ToggleMode = false
                };
            }

            return configs;
        }

        /// <summary>
        /// Handler for RawGameController added event
        /// </summary>
        private void RawGameController_Added(object sender, RawGameController controller)
        {
            _logger.LogInformation("Game controller added: {Name}", controller.DisplayName);
            RefreshControllersList();
        }

        /// <summary>
        /// Handler for RawGameController removed event
        /// </summary>
        private void RawGameController_Removed(object sender, RawGameController controller)
        {
            _logger.LogInformation("Game controller removed: {Name}", controller.DisplayName);
            RefreshControllersList();
        }

        private void OnAcpChannelChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            try
            {
                int channelValue = Convert.ToInt32(newValue);
                AcpChannelType newChannel = GetChannelTypeFromValue(channelValue);

                if (_currentChannel != newChannel)
                {
                    _logger.LogInformation("ACP Channel changed from {OldChannel} to {NewChannel}",
                                           _currentChannel, newChannel);

                    _currentChannel = newChannel;

                    // Raise state changed event with current active state
                    RaisePttStateChanged(_isPttActive);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ACP channel dataref change");
            }
        }

        #endregion
    }
}
