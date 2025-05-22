namespace Prosim2GSX.Services.PTT.Models
{
    /// <summary>
    /// Represents a mapping between an audio channel and a keyboard shortcut
    /// </summary>
    public class PttChannelMapping
    {
        /// <summary>
        /// Gets or sets whether this channel mapping is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the keyboard shortcut to send when PTT is activated
        /// </summary>
        public string KeyboardShortcut { get; set; }

        /// <summary>
        /// Gets or sets the target application name
        /// </summary>
        public string ApplicationName { get; set; }
    }
}
