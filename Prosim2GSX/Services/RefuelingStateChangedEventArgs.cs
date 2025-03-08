using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for refueling state changes
    /// </summary>
    public class RefuelingStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the new refueling state
        /// </summary>
        public RefuelingState NewState { get; }
        
        /// <summary>
        /// Gets the previous refueling state
        /// </summary>
        public RefuelingState PreviousState { get; }
        
        /// <summary>
        /// Gets a value indicating whether refueling is in progress
        /// </summary>
        public bool IsRefuelingInProgress => 
            NewState == RefuelingState.Requested || 
            NewState == RefuelingState.Refueling;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RefuelingStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="newState">The new refueling state</param>
        /// <param name="previousState">The previous refueling state</param>
        public RefuelingStateChangedEventArgs(RefuelingState newState, RefuelingState previousState)
        {
            NewState = newState;
            PreviousState = previousState;
        }
    }
}
