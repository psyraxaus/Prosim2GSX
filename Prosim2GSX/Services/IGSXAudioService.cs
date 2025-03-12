using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX audio control service
    /// </summary>
    public interface IGSXAudioService
    {
        /// <summary>
        /// Gets audio sessions for GSX and VHF1
        /// </summary>
        void GetAudioSessions();
        
        /// <summary>
        /// Gets audio sessions for GSX and VHF1 asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask GetAudioSessionsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Resets audio settings to default
        /// </summary>
        void ResetAudio();
        
        /// <summary>
        /// Resets audio settings to default asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask ResetAudioAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Controls audio based on cockpit controls
        /// </summary>
        void ControlAudio();
        
        /// <summary>
        /// Controls audio based on cockpit controls asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask ControlAudioAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Number of retry attempts for getting audio sessions
        /// </summary>
        int AudioSessionRetryCount { get; set; }
        
        /// <summary>
        /// Delay between retry attempts
        /// </summary>
        TimeSpan AudioSessionRetryDelay { get; set; }
        
        /// <summary>
        /// Event raised when an audio session is found
        /// </summary>
        event EventHandler<AudioSessionEventArgs> AudioSessionFound;
        
        /// <summary>
        /// Event raised when volume is changed
        /// </summary>
        event EventHandler<AudioVolumeChangedEventArgs> VolumeChanged;
        
        /// <summary>
        /// Event raised when mute state is changed
        /// </summary>
        event EventHandler<AudioMuteChangedEventArgs> MuteChanged;
    }
}
