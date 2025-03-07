using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Record of a state transition
    /// </summary>
    public class StateTransitionRecord
    {
        /// <summary>
        /// Gets the state transitioned from
        /// </summary>
        public FlightState FromState { get; }
        
        /// <summary>
        /// Gets the state transitioned to
        /// </summary>
        public FlightState ToState { get; }
        
        /// <summary>
        /// Gets the timestamp of the transition
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Gets the duration spent in the previous state
        /// </summary>
        public TimeSpan Duration { get; }
        
        /// <summary>
        /// Gets the reason for the transition, if provided
        /// </summary>
        public string Reason { get; }
        
        /// <summary>
        /// Initializes a new instance of the StateTransitionRecord class
        /// </summary>
        public StateTransitionRecord(FlightState fromState, FlightState toState, DateTime timestamp, TimeSpan duration, string reason = null)
        {
            FromState = fromState;
            ToState = toState;
            Timestamp = timestamp;
            Duration = duration;
            Reason = reason;
        }
    }
}
