using CoreAudio;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Services.Audio
{
    /// <summary>
    /// Represents an audio source that can be controlled
    /// </summary>
    public class AudioSource
    {
        private string _processName;

        /// <summary>
        /// Name of the process associated with this audio source
        /// </summary>
        public string ProcessName
        {
            get => _processName;
            set
            {
                _processName = value;
                // Clear the cached process names when the process name changes
                _processNames = null;
            }
        }

        // Cache the split process names
        private List<string> _processNames;

        /// <summary>
        /// List of process names (split by comma)
        /// </summary>
        public List<string> ProcessNames
        {
            get
            {
                if (_processNames == null && !string.IsNullOrEmpty(ProcessName))
                {
                    _processNames = ProcessName.Split(',')
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                }
                return _processNames ?? new List<string>();
            }
        }

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
        /// Dataref name for the volume knob
        /// </summary>
        public string KnobDataRef { get; set; }

        /// <summary>
        /// Dataref name for the mute control
        /// </summary>
        public string MuteDataRef { get; set; }

        /// <summary>
        /// Creates a new audio source
        /// </summary>
        /// <param name="processName">Name of the process (or comma-separated list of process names)</param>
        /// <param name="sourceName">Friendly name for the source</param>
        /// <param name="knobDataRef">Dataref name for volume control</param>
        /// <param name="muteDataRef">Dataref name for mute control</param>
        public AudioSource(string processName, string sourceName, string knobDataRef, string muteDataRef)
        {
            ProcessName = processName;
            SourceName = sourceName;
            KnobDataRef = knobDataRef;
            MuteDataRef = muteDataRef;
        }
    }
}

