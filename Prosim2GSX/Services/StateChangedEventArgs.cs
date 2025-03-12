using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for state changes
    /// </summary>
public class StateChangedEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the previous state
        /// </summary>
        public FlightState PreviousState { get; }
        
        /// <summary>
        /// Gets the new state
        /// </summary>
        public FlightState NewState { get; }
        
        /// <summary>
        /// Gets the reason for the state change
        /// </summary>
        public string Reason { get; }
        
        /// <summary>
        /// Initializes a new instance of the StateChangedEventArgs class
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        /// <param name="reason">The reason for the state change</param>
        public StateChangedEventArgs(FlightState previousState, FlightState newState, string reason = null)
        {
            PreviousState = previousState;
            NewState = newState;
            Reason = reason ?? "State changed";
        }
    }
    
    /// <summary>
    /// Flight states for GSX integration
    /// </summary>
    public enum FlightState
    {
        /// <summary>
        /// Pre-flight state (before departure)
        /// </summary>
        PREFLIGHT,
        
        /// <summary>
        /// Departure state (boarding, refueling, etc.)
        /// </summary>
        DEPARTURE,
        
        /// <summary>
        /// Taxi-out state (after pushback, before takeoff)
        /// </summary>
        TAXIOUT,
        
        /// <summary>
        /// Flight state (in the air)
        /// </summary>
        FLIGHT,
        
        /// <summary>
        /// Taxi-in state (after landing, before arrival)
        /// </summary>
        TAXIIN,
        
        /// <summary>
        /// Arrival state (deboarding, etc.)
        /// </summary>
        ARRIVAL,
        
        /// <summary>
        /// Turnaround state (between flights)
        /// </summary>
        TURNAROUND
    }
}
