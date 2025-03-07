using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Data class for state persistence
    /// </summary>
    public class StateData
    {
        /// <summary>
        /// Gets or sets the current state
        /// </summary>
        public FlightState CurrentState { get; set; }
        
        /// <summary>
        /// Gets or sets when the current state was entered
        /// </summary>
        public DateTime CurrentStateEnteredAt { get; set; }
        
        /// <summary>
        /// Gets or sets the state history
        /// </summary>
        public List<StateTransitionRecord> StateHistory { get; set; }
        
        /// <summary>
        /// Gets or sets the predicted next state
        /// </summary>
        public FlightState? PredictedNextState { get; set; }
        
        /// <summary>
        /// Gets or sets when the state was last updated
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
}
