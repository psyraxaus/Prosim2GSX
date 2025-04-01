using CoreAudio;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        private string _lastVhf1App;
        
        private bool _fcuTrackFpaMode = false;
        private bool _fcuHeadingVsMode = false;

        private DataRefChangedHandler _trackFpaModeHandler;
        private DataRefChangedHandler _headingVsModeHandler;
        private DataRefChangedHandler _intVolumeHandler;
        private DataRefChangedHandler _intRecHandler;
        private DataRefChangedHandler _vhf1VolumeHandler;
        private DataRefChangedHandler _vhf1RecHandler;

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
            
            if (!string.IsNullOrEmpty(_model.Vhf1VolumeApp))
                _lastVhf1App = _model.Vhf1VolumeApp;

            _trackFpaModeHandler = new DataRefChangedHandler(OnTrackFpaModeChanged);
            _headingVsModeHandler = new DataRefChangedHandler(OnHeadingVsModeChanged);
            _intVolumeHandler = new DataRefChangedHandler(OnIntVolumeChanged);
            _intRecHandler = new DataRefChangedHandler(OnIntRecChanged);
            _vhf1VolumeHandler = new DataRefChangedHandler(OnVhf1VolumeChanged);
            _vhf1RecHandler = new DataRefChangedHandler(OnVhf1RecChanged);
        }
        
        /// <summary>
        /// Initializes the audio service and sets up audio sources
        /// </summary>
        public void Initialize()
        {
            _prosimController.SubscribeToDataRef("system.indicators.I_FCU_TRACK_FPA_MODE", _trackFpaModeHandler);
            _prosimController.SubscribeToDataRef("system.indicators.I_FCU_HEADING_VS_MODE", _headingVsModeHandler);
            
            // Initialize default audio sources based on ServiceModel settings
            if (_model.GsxVolumeControl)
            {
                AddAudioSource("Couatl64_MSFS", "GSX", "system.analog.A_ASP_INT_VOLUME", "system.indicators.I_ASP_INT_REC");
                _prosimController.SubscribeToDataRef("system.analog.A_ASP_INT_VOLUME", _intVolumeHandler);
                _prosimController.SubscribeToDataRef("system.indicators.I_ASP_INT_REC", _intRecHandler);
            }
            
            if (_model.IsVhf1Controllable())
            {
                AddAudioSource(_model.Vhf1VolumeApp, "VHF1", "system.analog.A_ASP_VHF_1_VOLUME", "system.indicators.I_ASP_VHF_1_REC");
                _prosimController.SubscribeToDataRef("system.analog.A_ASP_VHF_1_VOLUME", _vhf1VolumeHandler);
                _prosimController.SubscribeToDataRef("system.indicators.I_ASP_VHF_1_REC", _vhf1RecHandler);
            }

            _fcuTrackFpaMode = _prosimController.Interface.GetProsimVariable("system.indicators.I_FCU_TRACK_FPA_MODE");
            _fcuHeadingVsMode = _prosimController.Interface.GetProsimVariable("system.indicators.I_FCU_HEADING_VS_MODE");

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

        private void OnIntVolumeChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (!_fcuTrackFpaMode && !_fcuHeadingVsMode)
                return; // Ignore changes when FCU is not in the correct mode
                
            if (_audioSources.TryGetValue("GSX", out AudioSource source) && source.Session != null)
            {
                float volume = Convert.ToSingle(newValue);
                if (volume >= 0 && volume != source.Volume)
                {
                    source.Session.SimpleAudioVolume.MasterVolume = volume;
                    source.Volume = volume;
                    
                    // Publish event
                    EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, source.Session.SimpleAudioVolume.Mute, volume));
                }
            }
        }
        
        private void OnIntRecChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (!_fcuTrackFpaMode && !_fcuHeadingVsMode)
                return; // Ignore changes when FCU is not in the correct mode
                
            if (_audioSources.TryGetValue("GSX", out AudioSource source) && source.Session != null)
            {
                int muted = Convert.ToBoolean(newValue) ? 1 : 0;
                if (muted >= 0 && muted != source.MuteState)
                {
                    source.Session.SimpleAudioVolume.Mute = muted == 0;
                    source.MuteState = muted;
                    
                    // Publish event
                    EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, muted == 0, source.Session.SimpleAudioVolume.MasterVolume));
                }
            }
        }
        
        private void OnVhf1VolumeChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (!_fcuTrackFpaMode && !_fcuHeadingVsMode)
                return; // Ignore changes when FCU is not in the correct mode
                
            if (_audioSources.TryGetValue("VHF1", out AudioSource source) && source.Session != null)
            {
                float volume = Convert.ToSingle(newValue);
                if (volume >= 0 && volume != source.Volume)
                {
                    source.Session.SimpleAudioVolume.MasterVolume = volume;
                    source.Volume = volume;
                    
                    // Publish event
                    EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, source.Session.SimpleAudioVolume.Mute, volume));
                }
            }
        }
        
        private void OnVhf1RecChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (!_fcuTrackFpaMode && !_fcuHeadingVsMode)
                return; // Ignore changes when FCU is not in the correct mode
                
            if (_audioSources.TryGetValue("VHF1", out AudioSource source) && source.Session != null)
            {
                bool shouldHandleMute = true;
                int muted = Convert.ToBoolean(newValue) ? 1 : 0;
                
                // Special handling for VHF1
                if (!_model.Vhf1LatchMute)
                {
                    // If latch mute is disabled for VHF1, only unmute if needed
                    if (source.Session.SimpleAudioVolume.Mute)
                    {
                        Logger.Log(LogLevel.Information, "AudioService:OnVhf1RecChanged", $"Unmuting {source.SourceName} (App muted and Mute-Option disabled)");
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
                }
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
                                    if (p.ProcessName == source.ProcessName)
                                    {
                                        source.Session = session;
                                        Logger.Log(LogLevel.Information, "AudioService:GetAudioSessions", $"Found Audio Session for {source.SourceName} ({source.ProcessName})");
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LogLevel.Debug, "AudioService:GetAudioSessions", $"Error getting process: {ex.Message}");
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
                Logger.Log(LogLevel.Error, "AudioService:GetAudioSessions", $"Error enumerating audio devices: {ex.Message}");
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
            // Handle VHF1 app change
            if (_lastVhf1App != _model.Vhf1VolumeApp)
            {
                // Remove old VHF1 source
                RemoveAudioSource("VHF1");
                
                // Add new VHF1 source if enabled
                if (_model.IsVhf1Controllable())
                {
                    AddAudioSource(_model.Vhf1VolumeApp, "VHF1", "system.analog.A_ASP_VHF_1_VOLUME", "system.indicators.I_ASP_VHF_1_REC");
                }
                
                _lastVhf1App = _model.Vhf1VolumeApp;
            }
            
            // Handle GSX volume control setting change
            if (_model.GsxVolumeControl && !_audioSources.ContainsKey("GSX"))
            {
                AddAudioSource("Couatl64_MSFS", "GSX", "system.analog.A_ASP_INT_VOLUME", "system.indicators.I_ASP_INT_REC");
            }
            else if (!_model.GsxVolumeControl && _audioSources.ContainsKey("GSX"))
            {
                RemoveAudioSource("GSX");
            }
            
            // Handle VHF1 volume control setting change
            if (_model.IsVhf1Controllable() && !_audioSources.ContainsKey("VHF1"))
            {
                AddAudioSource(_model.Vhf1VolumeApp, "VHF1", "system.analog.A_ASP_VHF_1_VOLUME", "system.indicators.I_ASP_VHF_1_REC");
            }
            else if (!_model.IsVhf1Controllable() && _audioSources.ContainsKey("VHF1"))
            {
                RemoveAudioSource("VHF1");
            }
            
            // Check if processes are still running
            List<string> sourcesToRemove = new List<string>();
            
            foreach (var source in _audioSources.Values)
            {
                if (source.Session != null && !IPCManager.IsProcessRunning(source.ProcessName))
                {
                    sourcesToRemove.Add(source.SourceName);
                }
            }
            
            foreach (var sourceName in sourcesToRemove)
            {
                RemoveAudioSource(sourceName);
            }
            
            // Check if GSX Couatl engine is running
            if (_audioSources.TryGetValue("GSX", out AudioSource gsxSource) && 
                gsxSource.Session != null && 
                _simConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
            {
                gsxSource.Session.SimpleAudioVolume.MasterVolume = 1.0f;
                gsxSource.Session.SimpleAudioVolume.Mute = false;
                gsxSource.Session = null;
                gsxSource.Volume = -1;
                gsxSource.MuteState = -1;
                Logger.Log(LogLevel.Information, "AudioService:HandleAppChanges", $"Disabled Audio Session for GSX (Couatl Engine not started)");
            }
        }

        public void Dispose()
        {
            // Unsubscribe from all datarefs
            _prosimController.UnsubscribeFromDataRef("system.indicators.I_FCU_TRACK_FPA_MODE", _trackFpaModeHandler);
            _prosimController.UnsubscribeFromDataRef("system.indicators.I_FCU_HEADING_VS_MODE", _headingVsModeHandler);
            _prosimController.UnsubscribeFromDataRef("system.analog.A_ASP_INT_VOLUME", _intVolumeHandler);
            _prosimController.UnsubscribeFromDataRef("system.indicators.I_ASP_INT_REC", _intRecHandler);
            _prosimController.UnsubscribeFromDataRef("system.analog.A_ASP_VHF_1_VOLUME", _vhf1VolumeHandler);
            _prosimController.UnsubscribeFromDataRef("system.indicators.I_ASP_VHF_1_REC", _vhf1RecHandler);
        }
    }
}
