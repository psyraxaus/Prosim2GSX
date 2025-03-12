using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for state restoration
    /// </summary>
    public class StateRestoredEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the restored state
        /// </summary>
        public FlightState RestoredState { get; }
        
        /// <summary>
        /// Initializes a new instance of the StateRestoredEventArgs class
        /// </summary>
        public StateRestoredEventArgs(FlightState restoredState)
        {
            RestoredState = restoredState;
        }
    }
}
