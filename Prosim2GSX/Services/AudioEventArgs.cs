using CoreAudio;
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for audio session events
    /// </summary>
    public class AudioSessionEventArgs : EventArgs
    {
        /// <summary>
        /// The process name
        /// </summary>
        public string ProcessName { get; }
        
        /// <summary>
        /// The audio session
        /// </summary>
        public AudioSessionControl2 Session { get; }
        
        /// <summary>
        /// Creates a new instance of AudioSessionEventArgs
        /// </summary>
        public AudioSessionEventArgs(string processName, AudioSessionControl2 session)
        {
            ProcessName = processName;
            Session = session;
        }
    }
    
    /// <summary>
    /// Event arguments for volume changed events
    /// </summary>
    public class AudioVolumeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The process name
        /// </summary>
        public string ProcessName { get; }
        
        /// <summary>
        /// The new volume level
        /// </summary>
        public float Volume { get; }
        
        /// <summary>
        /// Creates a new instance of AudioVolumeChangedEventArgs
        /// </summary>
        public AudioVolumeChangedEventArgs(string processName, float volume)
        {
            ProcessName = processName;
            Volume = volume;
        }
    }
    
    /// <summary>
    /// Event arguments for mute changed events
    /// </summary>
    public class AudioMuteChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The process name
        /// </summary>
        public string ProcessName { get; }
        
        /// <summary>
        /// The new mute state
        /// </summary>
        public bool Muted { get; }
        
        /// <summary>
        /// Creates a new instance of AudioMuteChangedEventArgs
        /// </summary>
        public AudioMuteChangedEventArgs(string processName, bool muted)
        {
            ProcessName = processName;
            Muted = muted;
        }
    }
}
