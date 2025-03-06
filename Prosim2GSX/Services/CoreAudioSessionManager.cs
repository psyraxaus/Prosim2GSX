using CoreAudio;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Implementation of IAudioSessionManager using CoreAudio
    /// </summary>
    public class CoreAudioSessionManager : IAudioSessionManager
    {
        private readonly object sessionLock = new object();
        
        /// <summary>
        /// Gets an audio session for a specific process
        /// </summary>
        public AudioSessionControl2 GetSessionForProcess(string processName)
        {
            try
            {
                lock (sessionLock)
                {
                    MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
                    var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                    foreach (var device in devices)
                    {
                        foreach (var session in device.AudioSessionManager2.Sessions)
                        {
                            try
                            {
                                var process = Process.GetProcessById((int)session.ProcessID);
                                if (process.ProcessName == processName)
                                {
                                    Logger.Log(LogLevel.Information, "CoreAudioSessionManager:GetSessionForProcess", 
                                        $"Found Audio Session for {processName}");
                                    return session;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogLevel.Debug, "CoreAudioSessionManager:GetSessionForProcess", 
                                    $"Error getting process for session: {ex.Message}");
                                // Continue to next session
                            }
                        }
                    }
                    
                    Logger.Log(LogLevel.Debug, "CoreAudioSessionManager:GetSessionForProcess", 
                        $"No Audio Session found for {processName}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "CoreAudioSessionManager:GetSessionForProcess", 
                    $"Exception getting audio session: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets an audio session for a specific process with retry mechanism
        /// </summary>
        public AudioSessionControl2 GetSessionForProcessWithRetry(string processName, int retryCount, TimeSpan retryDelay)
        {
            for (int attempt = 0; attempt < retryCount; attempt++)
            {
                var session = GetSessionForProcess(processName);
                if (session != null)
                    return session;
                
                if (attempt < retryCount - 1)
                {
                    Logger.Log(LogLevel.Debug, "CoreAudioSessionManager:GetSessionForProcessWithRetry", 
                        $"Retry {attempt + 1}/{retryCount} for {processName}");
                    Thread.Sleep(retryDelay);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets an audio session for a specific process asynchronously
        /// </summary>
        public async Task<AudioSessionControl2> GetSessionForProcessAsync(string processName, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => GetSessionForProcess(processName), cancellationToken);
        }
        
        /// <summary>
        /// Sets the volume for an audio session
        /// </summary>
        public void SetVolume(AudioSessionControl2 session, float volume)
        {
            if (session == null)
                return;
                
            try
            {
                lock (sessionLock)
                {
                    session.SimpleAudioVolume.MasterVolume = volume;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "CoreAudioSessionManager:SetVolume", 
                    $"Exception setting volume: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets the mute state for an audio session
        /// </summary>
        public void SetMute(AudioSessionControl2 session, bool mute)
        {
            if (session == null)
                return;
                
            try
            {
                lock (sessionLock)
                {
                    session.SimpleAudioVolume.Mute = mute;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "CoreAudioSessionManager:SetMute", 
                    $"Exception setting mute: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Resets an audio session to default settings
        /// </summary>
        public void ResetSession(AudioSessionControl2 session)
        {
            if (session == null)
                return;
                
            try
            {
                lock (sessionLock)
                {
                    session.SimpleAudioVolume.MasterVolume = 1.0f;
                    session.SimpleAudioVolume.Mute = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "CoreAudioSessionManager:ResetSession", 
                    $"Exception resetting session: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if a process is running
        /// </summary>
        public bool IsProcessRunning(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "CoreAudioSessionManager:IsProcessRunning", 
                    $"Exception checking process: {ex.Message}");
                return false;
            }
        }
    }
}
