using CoreAudio;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Prosim2GSX.Services.Audio.AudioChannelConfig;

namespace Prosim2GSX.Services.Audio
{
    /// <summary>
    /// Service for controlling audio volume and mute state based on cockpit controls
    /// </summary>
    public class AudioService : IAudioService
    {
        private readonly VoiceMeeterApi _voiceMeeterApi;
        private Dictionary<AudioChannel, float> _voiceMeeterVolumes = new Dictionary<AudioChannel, float>();
        private Dictionary<AudioChannel, bool> _voiceMeeterMutes = new Dictionary<AudioChannel, bool>();

        private readonly ServiceModel _model;
        private readonly IProsimInterface _prosimInterface;
        private readonly IDataRefMonitoringService _dataRefService;
        private readonly MobiSimConnect _simConnect;
        private readonly Dictionary<string, AudioSource> _audioSources = new Dictionary<string, AudioSource>();

        private bool _fcuTrackFpaMode = false;
        private bool _fcuHeadingVsMode = false;

        private DataRefChangedHandler _trackFpaModeHandler;
        private DataRefChangedHandler _headingVsModeHandler;

        // Dictionary to store handlers for each channel
        private Dictionary<AudioChannel, DataRefChangedHandler> _volumeHandlers = new Dictionary<AudioChannel, DataRefChangedHandler>();
        private Dictionary<AudioChannel, DataRefChangedHandler> _muteHandlers = new Dictionary<AudioChannel, DataRefChangedHandler>();

        /// <summary>
        /// Creates a new AudioService
        /// </summary>
        /// <param name="model">Service model containing settings</param>
        /// <param name="prosimController">Prosim instance for reading cockpit controls</param>

        public AudioService(IProsimInterface prosimInterface, IDataRefMonitoringService dataRefService, MobiSimConnect simConnect, ServiceModel model)
        {
            _prosimInterface = prosimInterface;
            _dataRefService = dataRefService;
            _simConnect = simConnect;
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _voiceMeeterApi = new VoiceMeeterApi();

            // Initialize FCU mode handlers
            _trackFpaModeHandler = new DataRefChangedHandler(OnTrackFpaModeChanged);
            _headingVsModeHandler = new DataRefChangedHandler(OnHeadingVsModeChanged);

            // Initialize handlers for each audio channel
            foreach (var channel in Enum.GetValues(typeof(AudioChannel)).Cast<AudioChannel>())
            {
                _volumeHandlers[channel] = new DataRefChangedHandler((dataRef, oldValue, newValue) =>
                    OnVolumeChanged(channel, dataRef, oldValue, newValue));

                _muteHandlers[channel] = new DataRefChangedHandler((dataRef, oldValue, newValue) =>
                    OnMuteChanged(channel, dataRef, oldValue, newValue));

                // Initialize VoiceMeeter volumes and mutes
                _voiceMeeterVolumes[channel] = 0.0f;
                _voiceMeeterMutes[channel] = false;
            }

            // Initialize VoiceMeeter API if needed
            if (ServiceLocator.Model.AudioApiType == AudioApiType.VoiceMeeter)
            {
                _voiceMeeterApi.Initialize();
            }
        }
        
        /// <summary>
        /// Initializes the audio service and sets up audio sources
        /// </summary>
        public void Initialize()
        {
            LogService.Log(LogLevel.Information, "AudioService:Initialize", "Initializing audio service");

            // Subscribe to FCU mode datarefs
            _dataRefService.SubscribeToDataRef("system.indicators.I_FCU_TRACK_FPA_MODE", _trackFpaModeHandler);
            _dataRefService.SubscribeToDataRef("system.indicators.I_FCU_HEADING_VS_MODE", _headingVsModeHandler);

            // Initialize audio sources and subscribe to datarefs for each enabled channel
            foreach (var channelEntry in _model.AudioChannels)
            {
                var channel = channelEntry.Key;
                var config = channelEntry.Value;

                if (config.Enabled)
                {
                    LogService.Log(LogLevel.Information, "AudioService:Initialize",
                        $"Enabling audio channel: {channel}");

                    if (_model.AudioApiType == AudioApiType.CoreAudio)
                    {
                        // Core Audio initialization
                        AddAudioSource(config.ProcessName, channel.ToString(), config.VolumeDataRef, config.MuteDataRef);
                    }
                    else if (_model.AudioApiType == AudioApiType.VoiceMeeter)
                    {
                        // Initialize VoiceMeeter cached values
                        if (_model is ServiceModel serviceModel &&
                            serviceModel.VoiceMeeterDeviceTypes.TryGetValue(channel, out var deviceType) &&
                            serviceModel.VoiceMeeterStrips.TryGetValue(channel, out var deviceName) &&
                            !string.IsNullOrEmpty(deviceName))
                        {
                            try
                            {
                                // Get initial values from VoiceMeeter
                                if (deviceType == VoiceMeeterDeviceType.Strip)
                                {
                                    _voiceMeeterVolumes[channel] = _voiceMeeterApi.GetStripGain(deviceName);
                                    _voiceMeeterMutes[channel] = _voiceMeeterApi.GetStripMute(deviceName);
                                }
                                else
                                {
                                    _voiceMeeterVolumes[channel] = _voiceMeeterApi.GetBusGain(deviceName);
                                    _voiceMeeterMutes[channel] = _voiceMeeterApi.GetBusMute(deviceName);
                                }

                                LogService.Log(LogLevel.Debug, "AudioService:Initialize",
                                    $"Initialized VoiceMeeter values for {channel}: Gain={_voiceMeeterVolumes[channel]}, Mute={_voiceMeeterMutes[channel]}");
                            }
                            catch (Exception ex)
                            {
                                LogService.Log(LogLevel.Error, "AudioService:Initialize",
                                    $"Error initializing VoiceMeeter values for {channel}: {ex.Message}");
                            }
                        }
                    }

                    // Subscribe to datarefs regardless of API type
                    _dataRefService.SubscribeToDataRef(config.VolumeDataRef, _volumeHandlers[channel]);
                    _dataRefService.SubscribeToDataRef(config.MuteDataRef, _muteHandlers[channel]);
                }
                else
                {
                    LogService.Log(LogLevel.Debug, "AudioService:Initialize",
                        $"Audio channel {channel} is disabled");
                }
            }

            // Get initial FCU mode state
            try
            {
                _fcuTrackFpaMode = ServiceLocator.ProsimInterface.GetProsimVariable("system.indicators.I_FCU_TRACK_FPA_MODE");
                _fcuHeadingVsMode = ServiceLocator.ProsimInterface.GetProsimVariable("system.indicators.I_FCU_HEADING_VS_MODE");

                LogService.Log(LogLevel.Debug, "AudioService:Initialize",
                    $"Initial FCU mode state: TrackFpaMode={_fcuTrackFpaMode}, HeadingVsMode={_fcuHeadingVsMode}");
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, "AudioService:Initialize",
                    $"Error getting initial FCU mode state: {ex.Message}");

                // Default to false if we can't get the initial state
                _fcuTrackFpaMode = false;
                _fcuHeadingVsMode = false;
            }

            // Perform initial synchronization for VoiceMeeter
            if (_model.AudioApiType == AudioApiType.VoiceMeeter)
            {
                SynchronizeDatarefsWithVoiceMeeter();
            }
        }
        
        private void OnTrackFpaModeChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            _fcuTrackFpaMode = Convert.ToBoolean(newValue);
            CheckFcuMode();
        }

        private void OnHeadingVsModeChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            _fcuHeadingVsMode = Convert.ToBoolean(newValue);
            CheckFcuMode();
        }

        private void CheckFcuMode()
        {
            // If FCU is not in the correct mode, reset audio
            if (!_fcuTrackFpaMode && !_fcuHeadingVsMode)
            {
                if (_audioSources.Count > 0)
                    ResetAudio();
            }
            else
            {
                // FCU is in correct mode, ensure audio sessions are available
                GetAudioSessions();
            }
        }

        /// <summary>
        /// Adds a new audio source to be controlled
        /// </summary>
        /// <param name="processName">Name of the process to control</param>
        /// <param name="sourceName">Friendly name for the audio source</param>
        /// <param name="knobDataRef">LVAR name for the volume knob</param>
        /// <param name="muteDataRef">LVAR name for the mute control</param>
        public void AddAudioSource(string processName, string sourceName, string knobDataRef, string muteDataRef)
        {
            if (!_audioSources.ContainsKey(sourceName))
            {
                _audioSources[sourceName] = new AudioSource(processName, sourceName, knobDataRef, muteDataRef);
                LogService.Log(LogLevel.Information, "AudioService:AddAudioSource", $"Added audio source: {sourceName} ({processName})");
            }
        }
        
        /// <summary>
        /// Removes an audio source
        /// </summary>
        /// <param name="sourceName">Friendly name of the audio source to remove</param>
        public void RemoveAudioSource(string sourceName)
        {
            if (_audioSources.TryGetValue(sourceName, out AudioSource source))
            {
                if (source.Session != null)
                {
                    // Reset audio settings before removing
                    source.Session.SimpleAudioVolume.MasterVolume = 1.0f;
                    source.Session.SimpleAudioVolume.Mute = false;
                }
                
                _audioSources.Remove(sourceName);
                LogService.Log(LogLevel.Information, "AudioService:RemoveAudioSource", $"Removed audio source: {sourceName}");
            }
        }
        
        /// <summary>
        /// Gets audio sessions for all configured audio sources
        /// </summary>
        private void GetAudioSessions()
        {
            try
            {
                MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
                var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                foreach (var source in _audioSources.Values)
                {
                    if (source.Session == null)
                    {
                        foreach (var device in devices)
                        {
                            foreach (var session in device.AudioSessionManager2.Sessions)
                            {
                                try
                                {
                                    Process p = Process.GetProcessById((int)session.ProcessID);
                                    // Check if the process name matches any of the names in the list
                                    if (source.ProcessNames.Contains(p.ProcessName))
                                    {
                                        source.Session = session;
                                        LogService.Log(LogLevel.Information, "AudioService:GetAudioSessions",
                                            $"Found Audio Session for {source.SourceName} ({p.ProcessName})");
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogService.Log(LogLevel.Debug, "AudioService:GetAudioSessions",
                                        $"Error getting process: {ex.Message}");
                                }
                            }

                            if (source.Session != null)
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, "AudioService:GetAudioSessions",
                    $"Error enumerating audio devices: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Resets audio settings to default values
        /// </summary>
        public void ResetAudio()
        {
            foreach (var source in _audioSources.Values)
            {
                if (source.Session != null && (source.Session.SimpleAudioVolume.MasterVolume != 1.0f || source.Session.SimpleAudioVolume.Mute))
                {
                    source.Session.SimpleAudioVolume.MasterVolume = 1.0f;
                    source.Session.SimpleAudioVolume.Mute = false;
                    LogService.Log(LogLevel.Information, "AudioService:ResetAudio", $"Audio reset for {source.SourceName}");
                    
                    // Publish event
                    EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, false, 1.0f));
                }
            }
        }
        
        /// <summary>
        /// Controls audio volume and mute state based on cockpit controls
        /// </summary>
        public void ControlAudio()
        {
            try
            {
                // Handle app changes
                HandleAppChanges();

                if (_model.AudioApiType == AudioApiType.CoreAudio)
                {
                    // Get audio sessions if using Core Audio
                    GetAudioSessions();
                }
                else if (_model.AudioApiType == AudioApiType.VoiceMeeter)
                {
                    // First, synchronize datarefs with VoiceMeeter to ensure VoiceMeeter reflects the current dataref values
                    SynchronizeDatarefsWithVoiceMeeter();

                    // Then check if VoiceMeeter parameters have changed (e.g., from manual adjustment in VoiceMeeter)
                    if (_voiceMeeterApi.AreParametersDirty())
                    {
                        UpdateVoiceMeeterParameters();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Debug, "AudioService:ControlAudio", $"Exception {ex.GetType()} during Audio Control: {ex.Message}");
            }
        }

        private void UpdateVoiceMeeterParameters()
        {
            foreach (var channelEntry in _model.AudioChannels)
            {
                var channel = channelEntry.Key;
                var config = channelEntry.Value;

                if (config.Enabled && !string.IsNullOrEmpty(config.VoiceMeeterStrip))
                {
                    try
                    {
                        if (_model is ServiceModel serviceModel &&
                            serviceModel.VoiceMeeterDeviceTypes.TryGetValue(channel, out var deviceType) &&
                            serviceModel.VoiceMeeterStrips.TryGetValue(channel, out var deviceName) &&
                            !string.IsNullOrEmpty(deviceName))
                        {
                            // Get current values from VoiceMeeter
                            float gain;
                            bool mute;

                            if (deviceType == VoiceMeeterDeviceType.Strip)
                            {
                                gain = _voiceMeeterApi.GetStripGain(deviceName);
                                mute = _voiceMeeterApi.GetStripMute(deviceName);
                            }
                            else
                            {
                                gain = _voiceMeeterApi.GetBusGain(deviceName);
                                mute = _voiceMeeterApi.GetBusMute(deviceName);
                            }

                            // Check if values have changed
                            bool gainChanged = !_voiceMeeterVolumes.TryGetValue(channel, out float cachedGain) ||
                                              Math.Abs(cachedGain - gain) > 0.01f;

                            bool muteChanged = !_voiceMeeterMutes.TryGetValue(channel, out bool cachedMute) ||
                                              cachedMute != mute;

                            // Update cached values
                            if (gainChanged)
                            {
                                _voiceMeeterVolumes[channel] = gain;

                                // Convert gain to dataref value
                                float datarefValue = ConvertVoiceMeeterGainToDataref(gain);

                                // Update the dataref if it doesn't match the current VoiceMeeter value
                                try
                                {
                                    float currentDataref = ServiceLocator.ProsimInterface.GetProsimVariable(config.VolumeDataRef);
                                    float threshold = 5f; // Larger threshold due to larger value range

                                    if (Math.Abs(currentDataref - datarefValue) > threshold)
                                    {
                                        ServiceLocator.ProsimInterface.SetProsimVariable(config.VolumeDataRef, datarefValue);
                                        LogService.Log(LogLevel.Debug, "AudioService:UpdateVoiceMeeterParameters",
                                            $"Updated dataref {config.VolumeDataRef} to match VoiceMeeter gain for {channel}: {datarefValue}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogService.Log(LogLevel.Error, "AudioService:UpdateVoiceMeeterParameters",
                                        $"Error updating dataref for {channel}: {ex.Message}");
                                }
                            }

                            if (muteChanged)
                            {
                                _voiceMeeterMutes[channel] = mute;

                                // Invert the mute state for all channels before setting the dataref
                                bool datarefMute = !mute;

                                // We don't update the dataref because it's a read-only indicator
                                try
                                {
                                    bool currentMute = Convert.ToBoolean(ServiceLocator.ProsimInterface.GetProsimVariable(config.MuteDataRef));
                                    LogService.Log(LogLevel.Debug, "AudioService:UpdateVoiceMeeterParameters",
                                        $"Current mute state for {channel}: {currentMute}");
                                }
                                catch (Exception ex)
                                {
                                    LogService.Log(LogLevel.Error, "AudioService:UpdateVoiceMeeterParameters",
                                        $"Error reading mute dataref for {channel}: {ex.Message}");
                                }
                            }

                            // If either value changed, publish an event
                            if (gainChanged || muteChanged)
                            {
                                // Calculate normalized volume (0-1) for the event
                                float datarefValue = ConvertVoiceMeeterGainToDataref(gain);
                                float normalizedVolume = (datarefValue - 176f) / (1020f - 176f);

                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(
                                    channel.ToString(),
                                    mute,
                                    normalizedVolume));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Log(LogLevel.Error, "AudioService:UpdateVoiceMeeterParameters",
                            $"Error updating VoiceMeeter parameters for {channel}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Handles changes to audio sources, such as app changes or process termination
        /// </summary>
        private void HandleAppChanges()
        {
            // Check for changes in enabled channels
            foreach (var channelEntry in _model.AudioChannels)
            {
                var channel = channelEntry.Key;
                var config = channelEntry.Value;
                string channelName = channel.ToString();

                // If channel is enabled but not in audio sources, add it
                if (config.Enabled && !_audioSources.ContainsKey(channelName))
                {
                    AddAudioSource(config.ProcessName, channelName, config.VolumeDataRef, config.MuteDataRef);
                    _dataRefService.SubscribeToDataRef(config.VolumeDataRef, _volumeHandlers[channel]);
                    _dataRefService.SubscribeToDataRef(config.MuteDataRef, _muteHandlers[channel]);
                }
                // If channel is disabled but in audio sources, remove it
                else if (!config.Enabled && _audioSources.ContainsKey(channelName))
                {
                    _dataRefService.UnsubscribeFromDataRef(config.VolumeDataRef, _volumeHandlers[channel]);
                    _dataRefService.UnsubscribeFromDataRef(config.MuteDataRef, _muteHandlers[channel]);
                    RemoveAudioSource(channelName);
                }
            }

            // Check if processes are still running
            List<string> sourcesToRemove = new List<string>();

            foreach (var source in _audioSources.Values)
            {
                // Check if the session is active and if any of the processes are running
                if (source.Session != null &&
                    !source.ProcessNames.Any(processName => IPCManager.IsProcessRunning(processName)))
                {
                    sourcesToRemove.Add(source.SourceName);
                }
            }

            foreach (var sourceName in sourcesToRemove)
            {
                RemoveAudioSource(sourceName);
            }

            // Special handling for GSX Couatl engine
            if (_audioSources.TryGetValue(AudioChannel.INT.ToString(), out AudioSource gsxSource) &&
                gsxSource.Session != null &&
                _simConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
            {
                gsxSource.Session.SimpleAudioVolume.MasterVolume = 1.0f;
                gsxSource.Session.SimpleAudioVolume.Mute = false;
                gsxSource.Session = null;
                gsxSource.Volume = -1;
                gsxSource.MuteState = -1;
                LogService.Log(LogLevel.Information, "AudioService:HandleAppChanges",
                    $"Disabled Audio Session for GSX (Couatl Engine not started)");
            }
        }

        /// <summary>
        /// Handles volume changes for any audio channel
        /// </summary>
        /// <param name="channel">The audio channel that changed</param>
        /// <param name="dataRef">The dataref that changed</param>
        /// <param name="oldValue">The previous value</param>
        /// <param name="newValue">The new value</param>
        private void OnVolumeChanged(AudioChannel channel, string dataRef, dynamic oldValue, dynamic newValue)
        {
            try
            {
                if (!_fcuTrackFpaMode && !_fcuHeadingVsMode)
                {
                    LogService.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                        $"Ignoring volume change for {channel}: FCU not in correct mode");
                    return; // Ignore changes when FCU is not in the correct mode
                }

                if (_model.AudioChannels.TryGetValue(channel, out var config) && config.Enabled)
                {
                    float datarefValue;
                    try
                    {
                        datarefValue = Convert.ToSingle(newValue);
                        LogService.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                            $"Volume value for {channel} changed from {oldValue} to {datarefValue}");
                    }
                    catch (Exception ex)
                    {
                        LogService.Log(LogLevel.Error, "AudioService:OnVolumeChanged",
                            $"Error converting volume value for {channel}: {ex.Message}");
                        return;
                    }

                    if (datarefValue >= 0)
                    {
                        if (_model.AudioApiType == AudioApiType.CoreAudio)
                        {
                            // For Core Audio, normalize to 0-1 range
                            float volume = (datarefValue - 176f) / (1020f - 176f);

                            // Core Audio volume control
                            if (_audioSources.TryGetValue(channel.ToString(), out AudioSource source) &&
                                source.Session != null &&
                                volume != source.Volume)
                            {
                                source.Session.SimpleAudioVolume.MasterVolume = volume;
                                source.Volume = volume;

                                // Publish event
                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, source.Session.SimpleAudioVolume.Mute, volume));

                                LogService.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                                    $"Volume changed for {channel}: {volume}");
                            }
                        }
                        else if (_model.AudioApiType == AudioApiType.VoiceMeeter)
                        {
                            // Get the device type and name
                            if (_model is ServiceModel serviceModel &&
                                serviceModel.VoiceMeeterDeviceTypes.TryGetValue(channel, out var deviceType) &&
                                serviceModel.VoiceMeeterStrips.TryGetValue(channel, out var deviceName) &&
                                !string.IsNullOrEmpty(deviceName))
                            {
                                // Convert dataref value to VoiceMeeter gain
                                float gain = ConvertDatarefToVoiceMeeterGain(datarefValue);
                                LogService.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                                    $"Converted dataref {datarefValue} to gain {gain} dB for {channel}");

                                // Check if the gain has actually changed to avoid unnecessary updates
                                if (!_voiceMeeterVolumes.TryGetValue(channel, out float currentGain) ||
                                    Math.Abs(currentGain - gain) > 0.01f)
                                {
                                    bool success = false;

                                    if (deviceType == VoiceMeeterDeviceType.Strip)
                                    {
                                        // VoiceMeeter strip volume control
                                        _voiceMeeterApi.SetStripGain(deviceName, gain);

                                        // Verify the change was applied
                                        float actualGain = _voiceMeeterApi.GetStripGain(deviceName);
                                        success = Math.Abs(actualGain - gain) < 0.1f;

                                        LogService.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                                            $"VoiceMeeter strip gain changed for {channel}: {gain} dB (success: {success})");
                                    }
                                    else
                                    {
                                        // VoiceMeeter bus volume control
                                        _voiceMeeterApi.SetBusGain(deviceName, gain);

                                        // Verify the change was applied
                                        float actualGain = _voiceMeeterApi.GetBusGain(deviceName);
                                        success = Math.Abs(actualGain - gain) < 0.1f;

                                        LogService.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                                            $"VoiceMeeter bus gain changed for {channel}: {gain} dB (success: {success})");
                                    }

                                    if (success)
                                    {
                                        // Update cached value only if the change was successful
                                        _voiceMeeterVolumes[channel] = gain;

                                        // Calculate normalized volume (0-1) for the event
                                        float normalizedVolume = (datarefValue - 176f) / (1020f - 176f);

                                        // Publish event
                                        EventAggregator.Instance.Publish(new AudioStateChangedEvent(
                                            channel.ToString(),
                                            _voiceMeeterMutes.TryGetValue(channel, out bool muted) ? muted : false,
                                            normalizedVolume));
                                    }
                                    else
                                    {
                                        LogService.Log(LogLevel.Warning, "AudioService:OnVolumeChanged",
                                            $"Failed to change VoiceMeeter gain for {channel}");
                                    }
                                }
                                else
                                {
                                    LogService.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                                        $"Skipping VoiceMeeter gain update for {channel} - value unchanged");
                                }
                            }
                            else
                            {
                                LogService.Log(LogLevel.Warning, "AudioService:OnVolumeChanged",
                                    $"Missing VoiceMeeter configuration for {channel}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, "AudioService:OnVolumeChanged",
                    $"Exception handling volume change for {channel}: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles mute state changes for any audio channel
        /// </summary>
        /// <param name="channel">The audio channel that changed</param>
        /// <param name="dataRef">The dataref that changed</param>
        /// <param name="oldValue">The previous value</param>
        /// <param name="newValue">The new value</param>
        private void OnMuteChanged(AudioChannel channel, string dataRef, dynamic oldValue, dynamic newValue)
        {
            try
            {
                if (!_fcuTrackFpaMode && !_fcuHeadingVsMode)
                    return; // Ignore changes when FCU is not in the correct mode

                if (_model.AudioChannels.TryGetValue(channel, out var config) && config.Enabled)
                {
                    bool shouldHandleMute = true;
                    bool muteValue;

                    try
                    {
                        // Invert the mute state for all channels
                        muteValue = !Convert.ToBoolean(newValue);
                        LogService.Log(LogLevel.Debug, "AudioService:OnMuteChanged",
                            $"Inverted mute value for {channel}: dataref={newValue}, used={muteValue}");
                    }
                    catch (Exception ex)
                    {
                        LogService.Log(LogLevel.Error, "AudioService:OnMuteChanged",
                            $"Error converting mute value for {channel}: {ex.Message}");
                        return;
                    }

                    // Check if latch mute is disabled for this channel
                    if (!config.LatchMute)
                    {
                        shouldHandleMute = false;

                        if (_model.AudioApiType == AudioApiType.CoreAudio)
                        {
                            // If latch mute is disabled, only unmute if needed
                            if (_audioSources.TryGetValue(channel.ToString(), out AudioSource source) &&
                                source.Session != null &&
                                source.Session.SimpleAudioVolume.Mute)
                            {
                                LogService.Log(LogLevel.Information, "AudioService:OnMuteChanged",
                                    $"Unmuting {source.SourceName} (App muted and Mute-Option disabled)");
                                source.Session.SimpleAudioVolume.Mute = false;
                                source.MuteState = -1;

                                // Publish event
                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, false, source.Session.SimpleAudioVolume.MasterVolume));
                            }
                        }
                        else if (_model.AudioApiType == AudioApiType.VoiceMeeter)
                        {
                            // Get the device type and name
                            if (_model is ServiceModel serviceModel &&
                                serviceModel.VoiceMeeterDeviceTypes.TryGetValue(channel, out var deviceType) &&
                                serviceModel.VoiceMeeterStrips.TryGetValue(channel, out var deviceName) &&
                                !string.IsNullOrEmpty(deviceName))
                            {
                                bool isMuted = false;

                                if (deviceType == VoiceMeeterDeviceType.Strip)
                                {
                                    // Check if the strip is muted
                                    isMuted = _voiceMeeterApi.GetStripMute(deviceName);

                                    // If latch mute is disabled, only unmute if needed
                                    if (isMuted)
                                    {
                                        LogService.Log(LogLevel.Information, "AudioService:OnMuteChanged",
                                            $"Unmuting VoiceMeeter strip {deviceName} (Mute-Option disabled)");
                                        _voiceMeeterApi.SetStripMute(deviceName, false);
                                    }
                                }
                                else
                                {
                                    // Check if the bus is muted
                                    isMuted = _voiceMeeterApi.GetBusMute(deviceName);

                                    // If latch mute is disabled, only unmute if needed
                                    if (isMuted)
                                    {
                                        LogService.Log(LogLevel.Information, "AudioService:OnMuteChanged",
                                            $"Unmuting VoiceMeeter bus {deviceName} (Mute-Option disabled)");
                                        _voiceMeeterApi.SetBusMute(deviceName, false);
                                    }
                                }

                                // Publish event
                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(channel.ToString(), false, 0));
                            }
                        }
                    }

                    // Handle mute state if needed
                    if (shouldHandleMute)
                    {
                        if (_model.AudioApiType == AudioApiType.CoreAudio)
                        {
                            if (_audioSources.TryGetValue(channel.ToString(), out AudioSource source) &&
                                source.Session != null)
                            {
                                source.Session.SimpleAudioVolume.Mute = muteValue;
                                source.MuteState = muteValue ? 1 : 0;

                                // Publish event
                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, muteValue, source.Session.SimpleAudioVolume.MasterVolume));

                                LogService.Log(LogLevel.Debug, "AudioService:OnMuteChanged",
                                    $"Mute state changed for {channel}: {(muteValue ? "Muted" : "Unmuted")}");
                            }
                        }
                        else if (_model.AudioApiType == AudioApiType.VoiceMeeter)
                        {
                            // Get the device type and name
                            if (_model is ServiceModel serviceModel &&
                                serviceModel.VoiceMeeterDeviceTypes.TryGetValue(channel, out var deviceType) &&
                                serviceModel.VoiceMeeterStrips.TryGetValue(channel, out var deviceName) &&
                                !string.IsNullOrEmpty(deviceName))
                            {
                                if (deviceType == VoiceMeeterDeviceType.Strip)
                                {
                                    // VoiceMeeter strip mute control
                                    _voiceMeeterApi.SetStripMute(deviceName, muteValue);
                                    LogService.Log(LogLevel.Debug, "AudioService:OnMuteChanged",
                                        $"VoiceMeeter strip mute state changed for {channel}: {(muteValue ? "Muted" : "Unmuted")}");
                                }
                                else
                                {
                                    // VoiceMeeter bus mute control
                                    _voiceMeeterApi.SetBusMute(deviceName, muteValue);
                                    LogService.Log(LogLevel.Debug, "AudioService:OnMuteChanged",
                                        $"VoiceMeeter bus mute state changed for {channel}: {(muteValue ? "Muted" : "Unmuted")}");
                                }

                                // Update cached value
                                _voiceMeeterMutes[channel] = muteValue;

                                // Publish event
                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(channel.ToString(), muteValue, 0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, "AudioService:OnMuteChanged",
                    $"Exception handling mute change for {channel}: {ex.Message}");
            }
        }

        public List<KeyValuePair<string, string>> GetAvailableVoiceMeeterStrips()
        {
            if (_voiceMeeterApi.Initialize())
            {
                return _voiceMeeterApi.GetAvailableStripsWithLabels();
            }
            return new List<KeyValuePair<string, string>>();
        }

        public bool EnsureVoiceMeeterIsRunning()
        {
            return _voiceMeeterApi.EnsureVoiceMeeterIsRunning();
        }

        public bool IsVoiceMeeterRunning()
        {
            return _voiceMeeterApi.IsVoiceMeeterRunning();
        }

        public List<KeyValuePair<string, string>> GetAvailableVoiceMeeterBuses()
        {
            if (_voiceMeeterApi.Initialize())
            {
                return _voiceMeeterApi.GetAvailableBusesWithLabels();
            }
            return new List<KeyValuePair<string, string>>();
        }

        /// <summary>
        /// Performs a diagnostic check of the VoiceMeeter API and logs detailed information
        /// </summary>
        /// <returns>True if all checks pass, false otherwise</returns>
        public bool PerformVoiceMeeterDiagnostics()
        {
            if (_model.AudioApiType != AudioApiType.VoiceMeeter)
            {
                LogService.Log(LogLevel.Warning, "AudioService", "Cannot perform VoiceMeeter diagnostics: Audio API type is not set to VoiceMeeter");
                return false;
            }

            LogService.Log(LogLevel.Information, "AudioService", "Starting VoiceMeeter diagnostics");
            
            // First, run the VoiceMeeter API diagnostics
            bool apiDiagnosticsSuccess = _voiceMeeterApi.PerformDiagnostics();
            
            // Then, check the FCU mode
            LogService.Log(LogLevel.Information, "AudioService", "Checking FCU mode...");
            LogService.Log(LogLevel.Information, "AudioService", $"FCU Track/FPA Mode: {_fcuTrackFpaMode}");
            LogService.Log(LogLevel.Information, "AudioService", $"FCU Heading/VS Mode: {_fcuHeadingVsMode}");
            
            if (!_fcuTrackFpaMode && !_fcuHeadingVsMode)
            {
                LogService.Log(LogLevel.Warning, "AudioService", "FCU is not in the correct mode for audio control");
            }
            
            // Check the VoiceMeeter strip configuration
            LogService.Log(LogLevel.Information, "AudioService", "Checking VoiceMeeter strip configuration...");
            
            foreach (var channelEntry in _model.AudioChannels)
            {
                var channel = channelEntry.Key;
                var config = channelEntry.Value;
                
                if (config.Enabled)
                {
                    LogService.Log(LogLevel.Information, "AudioService", $"Channel {channel} is enabled");
                    
                    // Check if the channel has a VoiceMeeter strip configured
                    if (_model is ServiceModel serviceModel)
                    {
                        bool hasDeviceType = serviceModel.VoiceMeeterDeviceTypes.TryGetValue(channel, out var deviceType);
                        bool hasStrip = serviceModel.VoiceMeeterStrips.TryGetValue(channel, out var stripName);
                        
                        LogService.Log(LogLevel.Information, "AudioService", 
                            $"  Device Type: {(hasDeviceType ? deviceType.ToString() : "Not configured")}");
                        LogService.Log(LogLevel.Information, "AudioService", 
                            $"  Strip Name: {(hasStrip ? stripName : "Not configured")}");
                        
                        if (hasStrip && !string.IsNullOrEmpty(stripName))
                        {
                            // Check if the strip exists in VoiceMeeter
                            var strips = _voiceMeeterApi.GetAvailableStripsWithLabels();
                            var buses = _voiceMeeterApi.GetAvailableBusesWithLabels();
                            
                            bool stripExists = false;
                            
                            if (deviceType == VoiceMeeterDeviceType.Strip)
                            {
                                stripExists = strips.Any(s => s.Key == stripName);
                            }
                            else
                            {
                                stripExists = buses.Any(b => b.Key == stripName);
                            }
                            
                            LogService.Log(LogLevel.Information, "AudioService", 
                                $"  Strip exists in VoiceMeeter: {stripExists}");
                            
                            if (!stripExists)
                            {
                                LogService.Log(LogLevel.Warning, "AudioService", 
                                    $"  Strip {stripName} does not exist in VoiceMeeter");
                            }
                        }
                        else
                        {
                            LogService.Log(LogLevel.Warning, "AudioService", 
                                $"  No VoiceMeeter strip configured for channel {channel}");
                        }
                    }
                    
                    // Check if the channel is controllable
                    bool isControllable = false;
                    
                    switch (channel)
                    {
                        case AudioChannel.VHF1:
                            isControllable = _model.IsVhf1Controllable();
                            break;
                        case AudioChannel.VHF2:
                            isControllable = _model.IsVhf2Controllable();
                            break;
                        case AudioChannel.VHF3:
                            isControllable = _model.IsVhf3Controllable();
                            break;
                        case AudioChannel.CAB:
                            isControllable = _model.IsCabControllable();
                            break;
                        case AudioChannel.PA:
                            isControllable = _model.IsPaControllable();
                            break;
                        case AudioChannel.INT:
                            isControllable = _model.GsxVolumeControl;
                            break;
                    }
                    
                    LogService.Log(LogLevel.Information, "AudioService", $"  Is controllable: {isControllable}");
                    
                    if (!isControllable)
                    {
                        LogService.Log(LogLevel.Warning, "AudioService", $"  Channel {channel} is not controllable");
                    }
                }
                else
                {
                    LogService.Log(LogLevel.Information, "AudioService", $"Channel {channel} is disabled");
                }
            }
            
            // Check the cached VoiceMeeter volumes and mutes
            LogService.Log(LogLevel.Information, "AudioService", "Checking cached VoiceMeeter volumes and mutes...");
            
            foreach (var channelEntry in _voiceMeeterVolumes)
            {
                LogService.Log(LogLevel.Information, "AudioService", 
                    $"  {channelEntry.Key}: Volume={channelEntry.Value} dB, " +
                    $"Mute={(_voiceMeeterMutes.TryGetValue(channelEntry.Key, out bool muted) ? muted : false)}");
            }
            
            LogService.Log(LogLevel.Information, "AudioService", "VoiceMeeter diagnostics completed");
            
            return apiDiagnosticsSuccess;
        }

        /// <summary>
        /// Synchronizes the dataref values with VoiceMeeter parameters
        /// </summary>
        private void SynchronizeDatarefsWithVoiceMeeter()
        {
            if (_model.AudioApiType != AudioApiType.VoiceMeeter)
                return;

            foreach (var channelEntry in _model.AudioChannels)
            {
                var channel = channelEntry.Key;
                var config = channelEntry.Value;

                if (config.Enabled && !string.IsNullOrEmpty(config.VoiceMeeterStrip))
                {
                    try
                    {
                        if (_model is ServiceModel serviceModel &&
                            serviceModel.VoiceMeeterDeviceTypes.TryGetValue(channel, out var deviceType) &&
                            serviceModel.VoiceMeeterStrips.TryGetValue(channel, out var deviceName) &&
                            !string.IsNullOrEmpty(deviceName))
                        {
                            // Get current dataref values
                            float datarefValue = ServiceLocator.ProsimInterface.GetProsimVariable(config.VolumeDataRef);
                            bool muteDataref = Convert.ToBoolean(ServiceLocator.ProsimInterface.GetProsimVariable(config.MuteDataRef));

                            // Invert the mute state for all channels
                            muteDataref = !muteDataref;

                            // Convert dataref to gain
                            float gainDataref = ConvertDatarefToVoiceMeeterGain(datarefValue);

                            // Get current VoiceMeeter values
                            float gainVoiceMeeter;
                            bool muteVoiceMeeter;

                            if (deviceType == VoiceMeeterDeviceType.Strip)
                            {
                                gainVoiceMeeter = _voiceMeeterApi.GetStripGain(deviceName);
                                muteVoiceMeeter = _voiceMeeterApi.GetStripMute(deviceName);
                            }
                            else
                            {
                                gainVoiceMeeter = _voiceMeeterApi.GetBusGain(deviceName);
                                muteVoiceMeeter = _voiceMeeterApi.GetBusMute(deviceName);
                            }

                            // Check if values are different
                            bool gainDifferent = Math.Abs(gainDataref - gainVoiceMeeter) > 0.1f;
                            bool muteDifferent = muteDataref != muteVoiceMeeter && config.LatchMute;

                            // If different, update VoiceMeeter to match dataref
                            if (gainDifferent)
                            {
                                if (deviceType == VoiceMeeterDeviceType.Strip)
                                {
                                    _voiceMeeterApi.SetStripGain(deviceName, gainDataref);
                                    LogService.Log(LogLevel.Debug, "AudioService:SynchronizeDatarefsWithVoiceMeeter",
                                        $"Updated VoiceMeeter strip gain for {channel} to match dataref: {gainDataref} dB");
                                }
                                else
                                {
                                    _voiceMeeterApi.SetBusGain(deviceName, gainDataref);
                                    LogService.Log(LogLevel.Debug, "AudioService:SynchronizeDatarefsWithVoiceMeeter",
                                        $"Updated VoiceMeeter bus gain for {channel} to match dataref: {gainDataref} dB");
                                }

                                // Update cached value
                                _voiceMeeterVolumes[channel] = gainDataref;
                            }

                            if (muteDifferent)
                            {
                                if (deviceType == VoiceMeeterDeviceType.Strip)
                                {
                                    _voiceMeeterApi.SetStripMute(deviceName, muteDataref);
                                    LogService.Log(LogLevel.Debug, "AudioService:SynchronizeDatarefsWithVoiceMeeter",
                                        $"Updated VoiceMeeter strip mute for {channel} to match dataref: {muteDataref}");
                                }
                                else
                                {
                                    _voiceMeeterApi.SetBusMute(deviceName, muteDataref);
                                    LogService.Log(LogLevel.Debug, "AudioService:SynchronizeDatarefsWithVoiceMeeter",
                                        $"Updated VoiceMeeter bus mute for {channel} to match dataref: {muteDataref}");
                                }

                                // Update cached value
                                _voiceMeeterMutes[channel] = muteDataref;
                            }

                            // If either value changed, publish an event
                            if (gainDifferent || muteDifferent)
                            {
                                // Calculate normalized volume (0-1) for the event
                                float normalizedVolume = (datarefValue - 176f) / (1020f - 176f);

                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(
                                    channel.ToString(),
                                    muteDataref,
                                    normalizedVolume));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Log(LogLevel.Error, "AudioService:SynchronizeDatarefsWithVoiceMeeter",
                            $"Error synchronizing dataref with VoiceMeeter for {channel}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Converts a Prosim dataref volume value (176-1020) to VoiceMeeter gain (-60 to 12 dB)
        /// </summary>
        /// <param name="datarefValue">The raw dataref value from Prosim</param>
        /// <returns>VoiceMeeter gain value in dB</returns>
        private float ConvertDatarefToVoiceMeeterGain(float datarefValue)
        {
            // Clamp the input value to the valid range
            float clampedValue = Math.Max(176f, Math.Min(1020f, datarefValue));

            // Normalize to 0-1 range
            float normalizedValue = (clampedValue - 176f) / (1020f - 176f);

            // Convert to VoiceMeeter gain range (-60 to 12 dB)
            float gain = (normalizedValue * 72f) - 60f;

            return gain;
        }

        /// <summary>
        /// Converts a VoiceMeeter gain value (-60 to 12 dB) to Prosim dataref volume (176-1020)
        /// </summary>
        /// <param name="gain">The gain value from VoiceMeeter in dB</param>
        /// <returns>Prosim dataref value</returns>
        private float ConvertVoiceMeeterGainToDataref(float gain)
        {
            // Clamp the input value to the valid range
            float clampedGain = Math.Max(-60f, Math.Min(12f, gain));

            // Normalize to 0-1 range
            float normalizedValue = (clampedGain + 60f) / 72f;

            // Convert to Prosim dataref range (176-1020)
            float datarefValue = (normalizedValue * (1020f - 176f)) + 176f;

            return datarefValue;
        }

        public void Dispose()
        {
            // Unsubscribe from FCU mode datarefs
            _dataRefService.UnsubscribeFromDataRef("system.indicators.I_FCU_TRACK_FPA_MODE", _trackFpaModeHandler);
            _dataRefService.UnsubscribeFromDataRef("system.indicators.I_FCU_HEADING_VS_MODE", _headingVsModeHandler);

            // Unsubscribe from all channel datarefs
            foreach (var channelEntry in _model.AudioChannels)
            {
                var channel = channelEntry.Key;
                var config = channelEntry.Value;

                // Unsubscribe regardless of whether the channel is currently enabled
                _dataRefService.UnsubscribeFromDataRef(config.VolumeDataRef, _volumeHandlers[channel]);
                _dataRefService.UnsubscribeFromDataRef(config.MuteDataRef, _muteHandlers[channel]);
            }

            // Shutdown VoiceMeeter API if it was initialized
            if (_model.AudioApiType == AudioApiType.VoiceMeeter)
            {
                _voiceMeeterApi.Shutdown();
            }
        }
    }
}
