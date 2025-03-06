using CoreAudio;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for audio session management
    /// </summary>
    public interface IAudioSessionManager
    {
        /// <summary>
        /// Gets an audio session for a specific process
        /// </summary>
        /// <param name="processName">The name of the process</param>
        /// <returns>The audio session, or null if not found</returns>
        AudioSessionControl2 GetSessionForProcess(string processName);
        
        /// <summary>
        /// Gets an audio session for a specific process with retry mechanism
        /// </summary>
        /// <param name="processName">The name of the process</param>
        /// <param name="retryCount">Number of retry attempts</param>
        /// <param name="retryDelay">Delay between retry attempts</param>
        /// <returns>The audio session, or null if not found after all retries</returns>
        AudioSessionControl2 GetSessionForProcessWithRetry(string processName, int retryCount, TimeSpan retryDelay);
        
        /// <summary>
        /// Gets an audio session for a specific process asynchronously
        /// </summary>
        /// <param name="processName">The name of the process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The audio session, or null if not found</returns>
        Task<AudioSessionControl2> GetSessionForProcessAsync(string processName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sets the volume for an audio session
        /// </summary>
        /// <param name="session">The audio session</param>
        /// <param name="volume">The volume level (0.0 to 1.0)</param>
        void SetVolume(AudioSessionControl2 session, float volume);
        
        /// <summary>
        /// Sets the mute state for an audio session
        /// </summary>
        /// <param name="session">The audio session</param>
        /// <param name="mute">True to mute, false to unmute</param>
        void SetMute(AudioSessionControl2 session, bool mute);
        
        /// <summary>
        /// Resets an audio session to default settings
        /// </summary>
        /// <param name="session">The audio session</param>
        void ResetSession(AudioSessionControl2 session);
        
        /// <summary>
        /// Checks if a process is running
        /// </summary>
        /// <param name="processName">The name of the process</param>
        /// <returns>True if the process is running, false otherwise</returns>
        bool IsProcessRunning(string processName);
    }
}
