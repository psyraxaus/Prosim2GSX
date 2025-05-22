using Prosim2GSX.Services.PTT.Enums;
using Prosim2GSX.Services.PTT.Models;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.PTT.Interface
{
    /// <summary>
    /// Interface for the PTT service
    /// </summary>
    public interface IPttService
    {
        /// <summary>
        /// Gets whether the PTT service is currently monitoring for input
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Gets the currently active ACP channel
        /// </summary>
        AcpChannelType CurrentChannel { get; }

        /// <summary>
        /// Gets whether PTT is currently active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the current monitored joystick ID
        /// </summary>
        int MonitoredJoystickId { get; }

        /// <summary>
        /// Gets the current monitored joystick button ID
        /// </summary>
        int MonitoredJoystickButton { get; }

        /// <summary>
        /// Gets whether joystick input is being used
        /// </summary>
        bool IsUsingJoystickInput { get; }

        /// <summary>
        /// Gets the name of the currently monitored key
        /// </summary>
        string MonitoredKeyName { get; }

        /// <summary>
        /// Starts monitoring for PTT input and ACP channel changes
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stops monitoring for PTT input and ACP channel changes
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Sets the key mapping for a specific ACP channel
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <param name="keyMapping">The key mapping string</param>
        void SetChannelKeyMapping(AcpChannelType channelType, string keyMapping);

        /// <summary>
        /// Sets whether a specific ACP channel is enabled for PTT
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <param name="enabled">Whether the channel is enabled</param>
        void SetChannelEnabled(AcpChannelType channelType, bool enabled);

        /// <summary>
        /// Sets the target application for a specific ACP channel
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <param name="applicationName">The target application name</param>
        void SetChannelTargetApplication(AcpChannelType channelType, string applicationName);

        /// <summary>
        /// Sets whether a specific ACP channel uses toggle mode
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <param name="toggleMode">Whether to use toggle mode</param>
        void SetChannelToggleMode(AcpChannelType channelType, bool toggleMode);

        /// <summary>
        /// Gets the configuration for a specific ACP channel
        /// </summary>
        /// <param name="channelType">The ACP channel type</param>
        /// <returns>The channel configuration</returns>
        PttChannelConfig GetChannelConfig(AcpChannelType channelType);

        /// <summary>
        /// Gets all channel configurations
        /// </summary>
        /// <returns>Dictionary of channel configurations by channel type</returns>
        Dictionary<AcpChannelType, PttChannelConfig> GetAllChannelConfigs();

        /// <summary>
        /// Manually activates PTT for the current ACP channel
        /// </summary>
        void ActivatePtt();

        /// <summary>
        /// Manually deactivates PTT for the current ACP channel
        /// </summary>
        void DeactivatePtt();

        /// <summary>
        /// Starts capturing input for configuration purposes
        /// </summary>
        /// <param name="callback">Callback to be called when input is detected</param>
        /// <param name="isForChannelKey">Whether this input capture is for a channel-specific key</param>
        void StartInputCapture(Action<string> callback, bool isForChannelKey = false);

        /// <summary>
        /// Stops capturing input for PTT activation
        /// </summary>
        void StopInputCapture();

        /// <summary>
        /// Sets the key to monitor for PTT activation
        /// </summary>
        /// <param name="keyName">The key name to monitor</param>
        void SetMonitoredKey(string keyName);

        /// <summary>
        /// Sets the joystick and button to monitor for PTT activation
        /// </summary>
        /// <param name="joystickId">The joystick ID</param>
        /// <param name="buttonId">The button ID</param>
        void SetMonitoredJoystickButton(int joystickId, int buttonId);

        /// <summary>
        /// Gets the current joystick configuration
        /// </summary>
        /// <returns>The joystick configuration, or null if not using joystick input</returns>
        JoystickConfig GetJoystickConfiguration();

        /// <summary>
        /// Gets available joysticks
        /// </summary>
        /// <returns>Dictionary of joystick IDs and names</returns>
        Dictionary<int, string> GetAvailableJoysticks();

        /// <summary>
        /// Gets the number of buttons for a specific joystick
        /// </summary>
        /// <param name="joystickId">The joystick ID</param>
        /// <returns>Number of buttons, or 0 if joystick not found</returns>
        int GetJoystickButtonCount(int joystickId);

        /// <summary>
        /// Notifies the service that ProSim connection state has changed
        /// </summary>
        /// <param name="isConnected">Whether ProSim is now connected</param>
        void SetProsimConnectionState(bool isConnected);

        /// <summary>
        /// Saves a channel configuration to settings
        /// </summary>
        void SaveChannelConfig(AcpChannelType channelType);
    }
}
