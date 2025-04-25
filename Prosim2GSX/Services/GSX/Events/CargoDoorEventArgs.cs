using System;

namespace Prosim2GSX.Services.GSX.Events
{
    /// <summary>
    /// Event arguments for cargo door state changes
    /// </summary>
    public class CargoDoorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether this is the forward (true) or aft (false) cargo door
        /// </summary>
        public bool IsForwardDoor { get; }

        /// <summary>
        /// Gets whether the door is open
        /// </summary>
        public bool IsOpen { get; }

        /// <summary>
        /// Creates a new instance of the event arguments
        /// </summary>
        public CargoDoorEventArgs(bool isForwardDoor, bool isOpen)
        {
            IsForwardDoor = isForwardDoor;
            IsOpen = isOpen;
        }
    }
}
