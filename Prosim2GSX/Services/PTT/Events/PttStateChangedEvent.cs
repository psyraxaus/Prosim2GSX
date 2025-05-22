using Prosim2GSX.Events;
using Prosim2GSX.Services.PTT.Enums;
using Prosim2GSX.Services.PTT.Models;

namespace Prosim2GSX.Services.PTT.Events
{
    /// <summary>
    /// Event that is raised when the PTT state changes
    /// </summary>
    public class PttStateChangedEvent : EventBase
    {
        /// <summary>
        /// Gets the current ACP channel type
        /// </summary>
        public AcpChannelType ChannelType { get; }

        /// <summary>
        /// Gets whether PTT is currently active
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Gets the configuration for the current channel
        /// </summary>
        public PttChannelConfig ChannelConfig { get; }

        /// <summary>
        /// Creates a new instance of PttStateChangedEvent
        /// </summary>
        /// <param name="channelType">The current ACP channel type</param>
        /// <param name="isActive">Whether PTT is active</param>
        /// <param name="channelConfig">The channel configuration</param>
        public PttStateChangedEvent(AcpChannelType channelType, bool isActive, PttChannelConfig channelConfig)
        {
            ChannelType = channelType;
            IsActive = isActive;
            ChannelConfig = channelConfig;
        }
    }
}
