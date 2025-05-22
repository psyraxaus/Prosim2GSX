using Prosim2GSX.Events;
using Prosim2GSX.Services.Audio;

namespace Prosim2GSX.Services.PTT.Events
{
    /// <summary>
    /// Event published when the active ACP channel changes
    /// </summary>
    public class PttActiveChannelChangedEvent : EventBase
    {
        /// <summary>
        /// The new active channel
        /// </summary>
        public AudioChannel Channel { get; private set; }

        /// <summary>
        /// Creates a new PttActiveChannelChangedEvent
        /// </summary>
        public PttActiveChannelChangedEvent(AudioChannel channel)
        {
            Channel = channel;
        }
    }
}
