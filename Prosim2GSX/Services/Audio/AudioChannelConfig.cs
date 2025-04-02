namespace Prosim2GSX.Services.Audio
{
    /// <summary>
    /// Configuration for an audio channel
    /// </summary>
    public class AudioChannelConfig
    {
        /// <summary>
        /// Name of the process associated with this audio channel
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// Dataref for the volume control
        /// </summary>
        public string VolumeDataRef { get; set; }

        /// <summary>
        /// Dataref for the mute control
        /// </summary>
        public string MuteDataRef { get; set; }

        /// <summary>
        /// Whether this channel is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Whether mute state should be latched (true) or momentary (false)
        /// </summary>
        public bool LatchMute { get; set; } = true;

        public enum VoiceMeeterDeviceType
        {
            Strip,
            Bus
        }

        public string VoiceMeeterStrip { get; set; }
    }
}
