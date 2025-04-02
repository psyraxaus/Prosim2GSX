using System;

namespace Prosim2GSX.Events
{
    /// <summary>
    /// Event raised when an audio source's state changes
    /// </summary>
    public class AudioStateChangedEvent : EventBase
    {
        /// <summary>
        /// Name of the audio source
        /// </summary>
        public string SourceName { get; }
        
        /// <summary>
        /// Whether the audio source is muted
        /// </summary>
        public bool Muted { get; }
        
        /// <summary>
        /// Current volume level of the audio source
        /// </summary>
        public float Volume { get; }
        
        /// <summary>
        /// Creates a new AudioStateChangedEvent
        /// </summary>
        /// <param name="sourceName">Name of the audio source</param>
        /// <param name="muted">Whether the audio source is muted</param>
        /// <param name="volume">Current volume level</param>
        public AudioStateChangedEvent(string sourceName, bool muted, float volume)
        {
            SourceName = sourceName;
            Muted = muted;
            Volume = volume;
        }
    }
}
