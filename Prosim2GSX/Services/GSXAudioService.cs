using CoreAudio;
using Prosim2GSX.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX audio control
    /// </summary>
    public class GSXAudioService : IGSXAudioService
    {
        private readonly string gsxProcess = "Couatl64_MSFS";
        private AudioSessionControl2 gsxAudioSession = null;
        private float gsxAudioVolume = -1;
        private int gsxAudioMute = -1;
        private AudioSessionControl2 vhf1AudioSession = null;
        private float vhf1AudioVolume = -1;
        private int vhf1AudioMute = -1;
        private string lastVhf1App;
        
        private readonly ServiceModel model;
        private readonly MobiSimConnect simConnect;
        private readonly IAudioSessionManager audioSessionManager;
        private readonly object audioLock = new object();
        
        /// <summary>
        /// Number of retry attempts for getting audio sessions
        /// </summary>
        public int AudioSessionRetryCount { get; set; } = 3;
        
        /// <summary>
        /// Delay between retry attempts
        /// </summary>
        public TimeSpan AudioSessionRetryDelay { get; set; } = TimeSpan.FromSeconds(2);
        
        /// <summary>
        /// Event raised when an audio session is found
        /// </summary>
        public event EventHandler<AudioSessionEventArgs> AudioSessionFound;
        
        /// <summary>
        /// Event raised when volume is changed
        /// </summary>
        public event EventHandler<AudioVolumeChangedEventArgs> VolumeChanged;
        
        /// <summary>
        /// Event raised when mute state is changed
        /// </summary>
        public event EventHandler<AudioMuteChangedEventArgs> MuteChanged;
        
        /// <summary>
        /// Creates a new instance of GSXAudioService
        /// </summary>
        public GSXAudioService(ServiceModel model, MobiSimConnect simConnect, IAudioSessionManager audioSessionManager)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.simConnect = simConnect ?? throw new ArgumentNullException(nameof(simConnect));
            this.audioSessionManager = audioSessionManager ?? throw new ArgumentNullException(nameof(audioSessionManager));
            
            if (!string.IsNullOrEmpty(model.Vhf1VolumeApp))
                lastVhf1App = model.Vhf1VolumeApp;
        }
        
        /// <summary>
        /// Gets audio sessions for GSX and VHF1
        /// </summary>
        public void GetAudioSessions()
        {
            try
            {
                lock (audioLock)
                {
                    GetGsxAudioSession();
                    GetVhf1AudioSession();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXAudioService:GetAudioSessions", 
                    $"Exception getting audio sessions: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets audio sessions for GSX and VHF1 asynchronously
        /// </summary>
        public async Task GetAudioSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Run(() => 
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    GetAudioSessions();
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.Log(LogLevel.Information, "GSXAudioService:GetAudioSessionsAsync", 
                    "Operation was canceled");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXAudioService:GetAudioSessionsAsync", 
                    $"Exception getting audio sessions: {ex.Message}");
            }
        }
        
        private void GetGsxAudioSession()
        {
            if (model.GsxVolumeControl && gsxAudioSession == null)
            {
                gsxAudioSession = audioSessionManager.GetSessionForProcessWithRetry(
                    gsxProcess, AudioSessionRetryCount, AudioSessionRetryDelay);
                
                if (gsxAudioSession != null)
                {
                    OnAudioSessionFound(gsxProcess, gsxAudioSession);
                }
            }
        }
        
        private void GetVhf1AudioSession()
        {
            if (model.IsVhf1Controllable() && vhf1AudioSession == null)
            {
                vhf1AudioSession = audioSessionManager.GetSessionForProcessWithRetry(
                    model.Vhf1VolumeApp, AudioSessionRetryCount, AudioSessionRetryDelay);
                
                if (vhf1AudioSession != null)
                {
                    OnAudioSessionFound(model.Vhf1VolumeApp, vhf1AudioSession);
                }
            }
        }
        
        /// <summary>
        /// Resets audio settings to default
        /// </summary>
        public void ResetAudio()
        {
            try
            {
                lock (audioLock)
                {
                    if (gsxAudioSession != null)
                    {
                        audioSessionManager.ResetSession(gsxAudioSession);
                        gsxAudioVolume = 1.0f;
                        gsxAudioMute = 0;
                        OnVolumeChanged(gsxProcess, 1.0f);
                        OnMuteChanged(gsxProcess, false);
                        Logger.Log(LogLevel.Information, "GSXAudioService:ResetAudio", 
                            $"Audio reset for GSX");
                    }

                    if (vhf1AudioSession != null)
                    {
                        audioSessionManager.ResetSession(vhf1AudioSession);
                        vhf1AudioVolume = 1.0f;
                        vhf1AudioMute = 0;
                        OnVolumeChanged(model.Vhf1VolumeApp, 1.0f);
                        OnMuteChanged(model.Vhf1VolumeApp, false);
                        Logger.Log(LogLevel.Information, "GSXAudioService:ResetAudio", 
                            $"Audio reset for {model.Vhf1VolumeApp}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXAudioService:ResetAudio", 
                    $"Exception resetting audio: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Resets audio settings to default asynchronously
        /// </summary>
        public async Task ResetAudioAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Run(() => 
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ResetAudio();
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.Log(LogLevel.Information, "GSXAudioService:ResetAudioAsync", 
                    "Operation was canceled");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXAudioService:ResetAudioAsync", 
                    $"Exception resetting audio: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Controls audio based on cockpit controls
        /// </summary>
        public void ControlAudio()
        {
            try
            {
                if (simConnect.ReadLvar("I_FCU_TRACK_FPA_MODE") == 0 && 
                    simConnect.ReadLvar("I_FCU_HEADING_VS_MODE") == 0)
                {
                    if (model.GsxVolumeControl || model.IsVhf1Controllable())
                        ResetAudio();
                    return;
                }

                lock (audioLock)
                {
                    // GSX Audio Control
                    ControlGsxAudio();
                    
                    // VHF1 Audio Control
                    ControlVhf1Audio();
                    
                    // App Change Handling
                    HandleAppChange();
                    
                    // Process Exit Handling
                    HandleProcessExits();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Debug, "GSXAudioService:ControlAudio", 
                    $"Exception {ex.GetType()} during Audio Control: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Controls audio based on cockpit controls asynchronously
        /// </summary>
        public async Task ControlAudioAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Run(() => 
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ControlAudio();
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudioAsync", 
                    "Operation was canceled");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXAudioService:ControlAudioAsync", 
                    $"Exception controlling audio: {ex.Message}");
            }
        }
        
        private void ControlGsxAudio()
        {
            if (model.GsxVolumeControl && gsxAudioSession != null)
            {
                float volume = simConnect.ReadLvar("A_ASP_INT_VOLUME");
                int muted = (int)simConnect.ReadLvar("I_ASP_INT_REC");
                
                if (volume >= 0 && volume != gsxAudioVolume)
                {
                    audioSessionManager.SetVolume(gsxAudioSession, volume);
                    gsxAudioVolume = volume;
                    OnVolumeChanged(gsxProcess, volume);
                }

                if (muted >= 0 && muted != gsxAudioMute)
                {
                    bool muteState = muted == 0;
                    audioSessionManager.SetMute(gsxAudioSession, muteState);
                    gsxAudioMute = muted;
                    OnMuteChanged(gsxProcess, muteState);
                }
            }
            else if (model.GsxVolumeControl && gsxAudioSession == null)
            {
                GetGsxAudioSession();
                gsxAudioVolume = -1;
                gsxAudioMute = -1;
            }
            else if (!model.GsxVolumeControl && gsxAudioSession != null)
            {
                audioSessionManager.ResetSession(gsxAudioSession);
                gsxAudioSession = null;
                gsxAudioVolume = -1;
                gsxAudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", 
                    $"Disabled Audio Session for GSX (Setting disabled)");
            }
        }
        
        private void ControlVhf1Audio()
        {
            if (model.IsVhf1Controllable() && vhf1AudioSession != null)
            {
                float volume = simConnect.ReadLvar("A_ASP_VHF_1_VOLUME");
                int muted = (int)simConnect.ReadLvar("I_ASP_VHF_1_REC");
                
                if (volume >= 0 && volume != vhf1AudioVolume)
                {
                    audioSessionManager.SetVolume(vhf1AudioSession, volume);
                    vhf1AudioVolume = volume;
                    OnVolumeChanged(model.Vhf1VolumeApp, volume);
                }

                if (model.Vhf1LatchMute && muted >= 0 && muted != vhf1AudioMute)
                {
                    bool muteState = muted == 0;
                    audioSessionManager.SetMute(vhf1AudioSession, muteState);
                    vhf1AudioMute = muted;
                    OnMuteChanged(model.Vhf1VolumeApp, muteState);
                }
                else if (!model.Vhf1LatchMute && vhf1AudioSession.SimpleAudioVolume.Mute)
                {
                    Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", 
                        $"Unmuting {lastVhf1App} (App muted and Mute-Option disabled)");
                    audioSessionManager.SetMute(vhf1AudioSession, false);
                    vhf1AudioMute = -1;
                    OnMuteChanged(model.Vhf1VolumeApp, false);
                }
            }
            else if (model.IsVhf1Controllable() && vhf1AudioSession == null)
            {
                GetVhf1AudioSession();
                vhf1AudioVolume = -1;
                vhf1AudioMute = -1;
            }
            else if (!model.Vhf1VolumeControl && !string.IsNullOrEmpty(lastVhf1App) && vhf1AudioSession != null)
            {
                audioSessionManager.ResetSession(vhf1AudioSession);
                vhf1AudioSession = null;
                vhf1AudioVolume = -1;
                vhf1AudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", 
                    $"Disabled Audio Session for {lastVhf1App} (Setting disabled)");
            }
        }
        
        private void HandleAppChange()
        {
            if (lastVhf1App != model.Vhf1VolumeApp)
            {
                if (vhf1AudioSession != null)
                {
                    audioSessionManager.ResetSession(vhf1AudioSession);
                    vhf1AudioSession = null;
                    vhf1AudioVolume = -1;
                    vhf1AudioMute = -1;
                    Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", 
                        $"Disabled Audio Session for {lastVhf1App} (App changed)");
                }
                GetVhf1AudioSession();
            }
            lastVhf1App = model.Vhf1VolumeApp;
        }
        
        private void HandleProcessExits()
        {
            // GSX exited
            if (model.GsxVolumeControl && gsxAudioSession != null && 
                !audioSessionManager.IsProcessRunning(gsxProcess))
            {
                gsxAudioSession = null;
                gsxAudioVolume = -1;
                gsxAudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", 
                    $"Disabled Audio Session for GSX (App not running)");
            }

            // COUATL
            if (model.GsxVolumeControl && gsxAudioSession != null && 
                simConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
            {
                audioSessionManager.ResetSession(gsxAudioSession);
                gsxAudioSession = null;
                gsxAudioVolume = -1;
                gsxAudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", 
                    $"Disabled Audio Session for GSX (Couatl Engine not started)");
            }

            // VHF1 exited
            if (model.IsVhf1Controllable() && vhf1AudioSession != null && 
                !audioSessionManager.IsProcessRunning(model.Vhf1VolumeApp))
            {
                vhf1AudioSession = null;
                vhf1AudioVolume = -1;
                vhf1AudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", 
                    $"Disabled Audio Session for {model.Vhf1VolumeApp} (App not running)");
            }
        }
        
        // Event raising methods
        protected virtual void OnAudioSessionFound(string processName, AudioSessionControl2 session)
        {
            AudioSessionFound?.Invoke(this, new AudioSessionEventArgs(processName, session));
        }
        
        protected virtual void OnVolumeChanged(string processName, float volume)
        {
            VolumeChanged?.Invoke(this, new AudioVolumeChangedEventArgs(processName, volume));
        }
        
        protected virtual void OnMuteChanged(string processName, bool muted)
        {
            MuteChanged?.Invoke(this, new AudioMuteChangedEventArgs(processName, muted));
        }
    }
}
