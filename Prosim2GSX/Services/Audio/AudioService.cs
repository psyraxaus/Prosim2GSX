using CoreAudio;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
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
        private readonly ProsimController _prosimController;
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
        public AudioService(ServiceModel model, ProsimController prosimController, MobiSimConnect simConnect)
        {
            _model = model;
            _prosimController = prosimController;
            _simConnect = simConnect;
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
            if (_model.AudioApiType == AudioApiType.VoiceMeeter)
            {
                _voiceMeeterApi.Initialize();
            }
        }
        
        /// <summary>
        /// Initializes the audio service and sets up audio sources
        /// </summary>
        public void Initialize()
        {
            Logger.Log(LogLevel.Information, "AudioService:Initialize", "Initializing audio service");

            // Subscribe to FCU mode datarefs
            _prosimController.SubscribeToDataRef("system.indicators.I_FCU_TRACK_FPA_MODE", _trackFpaModeHandler);
            _prosimController.SubscribeToDataRef("system.indicators.I_FCU_HEADING_VS_MODE", _headingVsModeHandler);

            // Initialize audio sources and subscribe to datarefs for each enabled channel
            foreach (var channelEntry in _model.AudioChannels)
            {
                var channel = channelEntry.Key;
                var config = channelEntry.Value;

                if (config.Enabled)
                {
                    Logger.Log(LogLevel.Information, "AudioService:Initialize",
                        $"Enabling audio channel: {channel}");

                    if (_model.AudioApiType == AudioApiType.CoreAudio)
                    {
                        // Core Audio initialization
                        AddAudioSource(config.ProcessName, channel.ToString(), config.VolumeDataRef, config.MuteDataRef);
                    }

                    // Subscribe to datarefs regardless of API type
                    _prosimController.SubscribeToDataRef(config.VolumeDataRef, _volumeHandlers[channel]);
                    _prosimController.SubscribeToDataRef(config.MuteDataRef, _muteHandlers[channel]);
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "AudioService:Initialize",
                        $"Audio channel {channel} is disabled");
                }
            }

            // Get initial FCU mode state
            try
            {
                _fcuTrackFpaMode = _prosimController.Interface.GetProsimVariable("system.indicators.I_FCU_TRACK_FPA_MODE");
                _fcuHeadingVsMode = _prosimController.Interface.GetProsimVariable("system.indicators.I_FCU_HEADING_VS_MODE");

                Logger.Log(LogLevel.Debug, "AudioService:Initialize",
                    $"Initial FCU mode state: TrackFpaMode={_fcuTrackFpaMode}, HeadingVsMode={_fcuHeadingVsMode}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AudioService:Initialize",
                    $"Error getting initial FCU mode state: {ex.Message}");

                // Default to false if we can't get the initial state
                _fcuTrackFpaMode = false;
                _fcuHeadingVsMode = false;
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
                Logger.Log(LogLevel.Information, "AudioService:AddAudioSource", $"Added audio source: {sourceName} ({processName})");
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
                Logger.Log(LogLevel.Information, "AudioService:RemoveAudioSource", $"Removed audio source: {sourceName}");
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
                                        Logger.Log(LogLevel.Information, "AudioService:GetAudioSessions",
                                            $"Found Audio Session for {source.SourceName} ({p.ProcessName})");
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LogLevel.Debug, "AudioService:GetAudioSessions",
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
                Logger.Log(LogLevel.Error, "AudioService:GetAudioSessions",
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
                    Logger.Log(LogLevel.Information, "AudioService:ResetAudio", $"Audio reset for {source.SourceName}");
                    
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
                    // Update VoiceMeeter parameters if they've changed
                    if (_voiceMeeterApi.AreParametersDirty())
                    {
                        UpdateVoiceMeeterParameters();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Debug, "AudioService:ControlAudio", $"Exception {ex.GetType()} during Audio Control: {ex.Message}");
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
                        // Get current values from VoiceMeeter
                        float gain = _voiceMeeterApi.GetStripGain(config.VoiceMeeterStrip);
                        bool mute = _voiceMeeterApi.GetStripMute(config.VoiceMeeterStrip);

                        // Update cached values
                        _voiceMeeterVolumes[channel] = gain;
                        _voiceMeeterMutes[channel] = mute;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "AudioService:UpdateVoiceMeeterParameters",
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
                    _prosimController.SubscribeToDataRef(config.VolumeDataRef, _volumeHandlers[channel]);
                    _prosimController.SubscribeToDataRef(config.MuteDataRef, _muteHandlers[channel]);
                }
                // If channel is disabled but in audio sources, remove it
                else if (!config.Enabled && _audioSources.ContainsKey(channelName))
                {
                    _prosimController.UnsubscribeFromDataRef(config.VolumeDataRef, _volumeHandlers[channel]);
                    _prosimController.UnsubscribeFromDataRef(config.MuteDataRef, _muteHandlers[channel]);
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
                Logger.Log(LogLevel.Information, "AudioService:HandleAppChanges",
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
                    return; // Ignore changes when FCU is not in the correct mode

                if (_model.AudioChannels.TryGetValue(channel, out var config) && config.Enabled)
                {
                    float volume;
                    try
                    {
                        volume = Convert.ToSingle(newValue);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "AudioService:OnVolumeChanged",
                            $"Error converting volume value for {channel}: {ex.Message}");
                        return;
                    }

                    if (volume >= 0)
                    {
                        if (_model.AudioApiType == AudioApiType.CoreAudio)
                        {
                            // Core Audio volume control
                            if (_audioSources.TryGetValue(channel.ToString(), out AudioSource source) &&
                                source.Session != null &&
                                volume != source.Volume)
                            {
                                source.Session.SimpleAudioVolume.MasterVolume = volume;
                                source.Volume = volume;

                                // Publish event
                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, source.Session.SimpleAudioVolume.Mute, volume));

                                Logger.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
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
                                // Convert 0-1 range to VoiceMeeter gain range (-60 to 12 dB)
                                float gain = _voiceMeeterApi.VolumeToGain(volume);

                                if (deviceType == VoiceMeeterDeviceType.Strip)
                                {
                                    // VoiceMeeter strip volume control
                                    _voiceMeeterApi.SetStripGain(deviceName, gain);
                                    Logger.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                                        $"VoiceMeeter strip gain changed for {channel}: {gain} dB");
                                }
                                else
                                {
                                    // VoiceMeeter bus volume control
                                    _voiceMeeterApi.SetBusGain(deviceName, gain);
                                    Logger.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                                        $"VoiceMeeter bus gain changed for {channel}: {gain} dB");
                                }

                                // Publish event
                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(channel.ToString(), false, volume));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AudioService:OnVolumeChanged",
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
                    int muted;

                    try
                    {
                        muted = Convert.ToBoolean(newValue) ? 1 : 0;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "AudioService:OnMuteChanged",
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
                                Logger.Log(LogLevel.Information, "AudioService:OnMuteChanged",
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
                                        Logger.Log(LogLevel.Information, "AudioService:OnMuteChanged",
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
                                        Logger.Log(LogLevel.Information, "AudioService:OnMuteChanged",
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
                    if (shouldHandleMute && muted >= 0)
                    {
                        bool shouldMute = muted == 0;

                        if (_model.AudioApiType == AudioApiType.CoreAudio)
                        {
                            if (_audioSources.TryGetValue(channel.ToString(), out AudioSource source) &&
                                source.Session != null &&
                                muted != source.MuteState)
                            {
                                source.Session.SimpleAudioVolume.Mute = shouldMute;
                                source.MuteState = muted;

                                // Publish event
                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, shouldMute, source.Session.SimpleAudioVolume.MasterVolume));

                                Logger.Log(LogLevel.Debug, "AudioService:OnMuteChanged",
                                    $"Mute state changed for {channel}: {(shouldMute ? "Muted" : "Unmuted")}");
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
                                    _voiceMeeterApi.SetStripMute(deviceName, shouldMute);
                                    Logger.Log(LogLevel.Debug, "AudioService:OnMuteChanged",
                                        $"VoiceMeeter strip mute state changed for {channel}: {(shouldMute ? "Muted" : "Unmuted")}");
                                }
                                else
                                {
                                    // VoiceMeeter bus mute control
                                    _voiceMeeterApi.SetBusMute(deviceName, shouldMute);
                                    Logger.Log(LogLevel.Debug, "AudioService:OnMuteChanged",
                                        $"VoiceMeeter bus mute state changed for {channel}: {(shouldMute ? "Muted" : "Unmuted")}");
                                }

                                // Publish event
                                EventAggregator.Instance.Publish(new AudioStateChangedEvent(channel.ToString(), shouldMute, 0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AudioService:OnMuteChanged",
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

        public void Dispose()
        {
            // Unsubscribe from FCU mode datarefs
            _prosimController.UnsubscribeFromDataRef("system.indicators.I_FCU_TRACK_FPA_MODE", _trackFpaModeHandler);
            _prosimController.UnsubscribeFromDataRef("system.indicators.I_FCU_HEADING_VS_MODE", _headingVsModeHandler);

            // Unsubscribe from all channel datarefs
            foreach (var channelEntry in _model.AudioChannels)
            {
                var channel = channelEntry.Key;
                var config = channelEntry.Value;

                // Unsubscribe regardless of whether the channel is currently enabled
                _prosimController.UnsubscribeFromDataRef(config.VolumeDataRef, _volumeHandlers[channel]);
                _prosimController.UnsubscribeFromDataRef(config.MuteDataRef, _muteHandlers[channel]);
            }

            // Shutdown VoiceMeeter API if it was initialized
            if (_model.AudioApiType == AudioApiType.VoiceMeeter)
            {
                _voiceMeeterApi.Shutdown();
            }
        }
    }
}
