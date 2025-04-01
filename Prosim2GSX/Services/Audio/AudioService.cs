using CoreAudio;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Prosim2GSX.Services.Audio
{
    /// <summary>
    /// Service for controlling audio volume and mute state based on cockpit controls
    /// </summary>
    public class AudioService : IAudioService
    {
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
                        $"Enabling audio channel: {channel} for process: {config.ProcessName}");

                    AddAudioSource(config.ProcessName, channel.ToString(), config.VolumeDataRef, config.MuteDataRef);
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
                
                // Get audio sessions if needed
                GetAudioSessions();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Debug, "AudioService:ControlAudio", $"Exception {ex.GetType()} during Audio Control: {ex.Message}");
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

                if (_audioSources.TryGetValue(channel.ToString(), out AudioSource source) && source.Session != null)
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

                    if (volume >= 0 && volume != source.Volume)
                    {
                        source.Session.SimpleAudioVolume.MasterVolume = volume;
                        source.Volume = volume;

                        // Publish event
                        EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, source.Session.SimpleAudioVolume.Mute, volume));

                        Logger.Log(LogLevel.Debug, "AudioService:OnVolumeChanged",
                            $"Volume changed for {channel}: {volume}");
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

                if (_audioSources.TryGetValue(channel.ToString(), out AudioSource source) && source.Session != null)
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
                    if (_model.AudioChannels.TryGetValue(channel, out var config) && !config.LatchMute)
                    {
                        // If latch mute is disabled, only unmute if needed
                        if (source.Session.SimpleAudioVolume.Mute)
                        {
                            Logger.Log(LogLevel.Information, "AudioService:OnMuteChanged",
                                $"Unmuting {source.SourceName} (App muted and Mute-Option disabled)");
                            source.Session.SimpleAudioVolume.Mute = false;
                            source.MuteState = -1;

                            // Publish event
                            EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, false, source.Session.SimpleAudioVolume.MasterVolume));
                        }
                        shouldHandleMute = false;
                    }

                    // Handle mute state if needed
                    if (shouldHandleMute && muted >= 0 && muted != source.MuteState)
                    {
                        source.Session.SimpleAudioVolume.Mute = muted == 0;
                        source.MuteState = muted;

                        // Publish event
                        EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, muted == 0, source.Session.SimpleAudioVolume.MasterVolume));

                        Logger.Log(LogLevel.Debug, "AudioService:OnMuteChanged",
                            $"Mute state changed for {channel}: {(muted == 0 ? "Muted" : "Unmuted")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AudioService:OnMuteChanged",
                    $"Exception handling mute change for {channel}: {ex.Message}");
            }
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
        }
    }
}
