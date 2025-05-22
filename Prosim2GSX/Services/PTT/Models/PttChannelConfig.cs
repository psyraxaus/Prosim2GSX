using Prosim2GSX.Services.PTT.Enums;

namespace Prosim2GSX.Services.PTT.Models
{
    /// <summary>
    /// Configuration for a PTT channel
    /// </summary>
    public class PttChannelConfig
    {
        /// <summary>
        /// Gets the channel type
        /// </summary>
        public AcpChannelType ChannelType { get; }

        /// <summary>
        /// Gets or sets whether this channel is enabled for PTT
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the keyboard shortcut to send when PTT is activated
        /// </summary>
        public string KeyMapping { get; set; }

        /// <summary>
        /// Gets or sets the target application to send the keyboard shortcut to
        /// </summary>
        public string TargetApplication { get; set; }

        /// <summary>
        /// Gets or sets whether this channel uses toggle mode (press once to activate, press again to deactivate)
        /// </summary>
        public bool ToggleMode { get; set; }

        /// <summary>
        /// Creates a new instance of PttChannelConfig
        /// </summary>
        /// <param name="channelType">The channel type</param>
        public PttChannelConfig(AcpChannelType channelType)
        {
            ChannelType = channelType;
            Enabled = false;
            KeyMapping = string.Empty;
            TargetApplication = string.Empty;
            ToggleMode = false;
        }
    }
}
