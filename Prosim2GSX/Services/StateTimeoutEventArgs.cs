using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for state timeouts
    /// </summary>
    public class StateTimeoutEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the state that timed out
        /// </summary>
        public FlightState State { get; }
        
        /// <summary>
        /// Gets when the state was entered
        /// </summary>
        public DateTime EnteredAt { get; }
        
        /// <summary>
        /// Gets how long the state was active
        /// </summary>
        public TimeSpan Duration { get; }
        
        /// <summary>
        /// Gets when the timeout occurred
        /// </summary>
        public DateTime TimeoutAt { get; }
        
        /// <summary>
        /// Initializes a new instance of the StateTimeoutEventArgs class
        /// </summary>
        public StateTimeoutEventArgs(FlightState state, DateTime enteredAt, TimeSpan duration, DateTime timeoutAt)
        {
            State = state;
            EnteredAt = enteredAt;
            Duration = duration;
            TimeoutAt = timeoutAt;
        }
    }
}
