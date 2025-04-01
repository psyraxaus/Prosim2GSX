using CoreAudio;

namespace Prosim2GSX.Services.Audio
{
    /// <summary>
    /// Represents an audio source that can be controlled
    /// </summary>
    public class AudioSource
    {
        /// <summary>
        /// Name of the process associated with this audio source
        /// </summary>
        public string ProcessName { get; set; }
        
        /// <summary>
        /// Friendly name for the audio source
        /// </summary>
        public string SourceName { get; set; }
        
        /// <summary>
        /// Audio session for this source
        /// </summary>
        public AudioSessionControl2 Session { get; set; }
        
        /// <summary>
        /// Current volume level (-1 if not set)
        /// </summary>
        public float Volume { get; set; } = -1;
        
        /// <summary>
        /// Current mute state (-1 if not set)
        /// </summary>
        public int MuteState { get; set; } = -1;
        
        /// <summary>
        /// LVAR name for the volume knob
        /// </summary>
        public string KnobLvarName { get; set; }
        
        /// <summary>
        /// LVAR name for the mute control
        /// </summary>
        public string MuteLvarName { get; set; }
        
        /// <summary>
        /// Creates a new audio source
        /// </summary>
        /// <param name="processName">Name of the process</param>
        /// <param name="sourceName">Friendly name for the source</param>
        /// <param name="knobLvarName">LVAR name for volume control</param>
        /// <param name="muteLvarName">LVAR name for mute control</param>
        public AudioSource(string processName, string sourceName, string knobLvarName, string muteLvarName)
        {
            ProcessName = processName;
            SourceName = sourceName;
            KnobLvarName = knobLvarName;
            MuteLvarName = muteLvarName;
        }
    }
}
