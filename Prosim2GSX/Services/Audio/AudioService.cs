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
        private readonly MobiSimConnect _simConnect;
        private readonly Dictionary<string, AudioSource> _audioSources = new Dictionary<string, AudioSource>();
        private string _lastVhf1App;
        
        /// <summary>
        /// Creates a new AudioService
        /// </summary>
        /// <param name="model">Service model containing settings</param>
        /// <param name="simConnect">SimConnect instance for reading cockpit controls</param>
        public AudioService(ServiceModel model, MobiSimConnect simConnect)
        {
            _model = model;
            _simConnect = simConnect;
            
            if (!string.IsNullOrEmpty(_model.Vhf1VolumeApp))
                _lastVhf1App = _model.Vhf1VolumeApp;
        }
        
        /// <summary>
        /// Initializes the audio service and sets up audio sources
        /// </summary>
        public void Initialize()
        {
            // Initialize default audio sources based on ServiceModel settings
            if (_model.GsxVolumeControl)
            {
                AddAudioSource("Couatl64_MSFS", "GSX", "A_ASP_INT_VOLUME", "I_ASP_INT_REC");
            }
            
            if (_model.IsVhf1Controllable())
            {
                AddAudioSource(_model.Vhf1VolumeApp, "VHF1", "A_ASP_VHF_1_VOLUME", "I_ASP_VHF_1_REC");
            }
        }
        
        /// <summary>
        /// Adds a new audio source to be controlled
        /// </summary>
        /// <param name="processName">Name of the process to control</param>
        /// <param name="sourceName">Friendly name for the audio source</param>
        /// <param name="knobLvarName">LVAR name for the volume knob</param>
        /// <param name="muteLvarName">LVAR name for the mute control</param>
        public void AddAudioSource(string processName, string sourceName, string knobLvarName, string muteLvarName)
        {
            if (!_audioSources.ContainsKey(sourceName))
            {
                _audioSources[sourceName] = new AudioSource(processName, sourceName, knobLvarName, muteLvarName);
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
                // Check if FCU is in the correct mode
                if (_simConnect.ReadLvar("I_FCU_TRACK_FPA_MODE") == 0 && _simConnect.ReadLvar("I_FCU_HEADING_VS_MODE") == 0)
                {
                    // Reset audio if FCU is not in the correct mode
                    if (_audioSources.Count > 0)
                        ResetAudio();
                    return;
                }
                
                // Handle app changes
                HandleAppChanges();
                
                // Get audio sessions if needed
                GetAudioSessions();
                
                // Control each audio source
                foreach (var source in _audioSources.Values)
                {
                    if (source.Session != null)
                    {
                        // Read volume and mute state from cockpit controls
                        float volume = _simConnect.ReadLvar(source.KnobLvarName);
                        int muted = (int)_simConnect.ReadLvar(source.MuteLvarName);
                        
                        // Update volume if changed
                        if (volume >= 0 && volume != source.Volume)
                        {
                            source.Session.SimpleAudioVolume.MasterVolume = volume;
                            source.Volume = volume;
                            
                            // Publish event
                            EventAggregator.Instance.Publish(new AudioStateChangedEvent(source.SourceName, source.Session.SimpleAudioVolume.Mute, volume));
                        }
                        
                        // Update mute state if changed
                        bool shouldHandleMute = true;
                        
                        // Special handling for VHF1
                        if (source.SourceName == "VHF1" && !_model.Vhf1LatchMute)
                        {
                            // If latch mute is disabled for VHF1, only unmute if needed
                            if (source.Session.SimpleAudioVolume.Mute)
                            {
                                Logger.Log(LogLevel.Information, "AudioService:ControlAudio", $"Unmuting {source.SourceName} (App muted and Mute-Option disabled)");
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
                    AddAudioSource(_model.Vhf1VolumeApp, "VHF1", "A_ASP_VHF_1_VOLUME", "I_ASP_VHF_1_REC");
                }
                
                _lastVhf1App = _model.Vhf1VolumeApp;
            }
            
            // Handle GSX volume control setting change
            if (_model.GsxVolumeControl && !_audioSources.ContainsKey("GSX"))
            {
                AddAudioSource("Couatl64_MSFS", "GSX", "A_ASP_INT_VOLUME", "I_ASP_INT_REC");
            }
            else if (!_model.GsxVolumeControl && _audioSources.ContainsKey("GSX"))
            {
                RemoveAudioSource("GSX");
            }
            
            // Handle VHF1 volume control setting change
            if (_model.IsVhf1Controllable() && !_audioSources.ContainsKey("VHF1"))
            {
                AddAudioSource(_model.Vhf1VolumeApp, "VHF1", "A_ASP_VHF_1_VOLUME", "I_ASP_VHF_1_REC");
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
    }
}
