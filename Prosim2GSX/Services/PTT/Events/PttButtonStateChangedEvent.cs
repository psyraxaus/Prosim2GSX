using Prosim2GSX.Events;

namespace Prosim2GSX.Services.PTT.Events
{
    /// <summary>
    /// Event that is raised when the PTT button state changes
    /// </summary>
    public class PttButtonStateChangedEvent : EventBase
    {
        /// <summary>
        /// Gets whether the button is currently pressed
        /// </summary>
        public bool IsPressed { get; }

        /// <summary>
        /// Gets the channel name (may be null if no channel is active)
        /// </summary>
        public string ChannelName { get; }

        /// <summary>
        /// Gets the application name (may be null if no application is targeted)
        /// </summary>
        public string ApplicationName { get; }

        /// <summary>
        /// Creates a new instance of PttButtonStateChangedEvent
        /// </summary>
        /// <param name="isPressed">Whether the button is pressed</param>
        /// <param name="channelName">The channel name</param>
        /// <param name="applicationName">The target application name</param>
        public PttButtonStateChangedEvent(bool isPressed, string channelName = null, string applicationName = null)
        {
            IsPressed = isPressed;
            ChannelName = channelName;
            ApplicationName = applicationName;
        }
    }
}
