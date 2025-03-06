using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX state management
    /// </summary>
    public class GSXStateManager : IGSXStateManager
    {
        private FlightState state = FlightState.PREFLIGHT;
        private readonly Dictionary<FlightState, HashSet<FlightState>> validTransitions;
        
        /// <summary>
        /// Event raised when the flight state changes
        /// </summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        public FlightState CurrentState => state;
        
        /// <summary>
        /// Initializes a new instance of the GSXStateManager class
        /// </summary>
        public GSXStateManager()
        {
            // Initialize valid state transitions
            validTransitions = new Dictionary<FlightState, HashSet<FlightState>>
            {
                { FlightState.PREFLIGHT, new HashSet<FlightState> { FlightState.DEPARTURE } },
                { FlightState.DEPARTURE, new HashSet<FlightState> { FlightState.TAXIOUT } },
                { FlightState.TAXIOUT, new HashSet<FlightState> { FlightState.FLIGHT } },
                { FlightState.FLIGHT, new HashSet<FlightState> { FlightState.TAXIIN } },
                { FlightState.TAXIIN, new HashSet<FlightState> { FlightState.ARRIVAL } },
                { FlightState.ARRIVAL, new HashSet<FlightState> { FlightState.TURNAROUND } },
                { FlightState.TURNAROUND, new HashSet<FlightState> { FlightState.DEPARTURE } }
            };
        }
        
        /// <summary>
        /// Initializes the state manager
        /// </summary>
        public void Initialize()
        {
            Reset();
            Logger.Log(LogLevel.Information, "GSXStateManager:Initialize", "State manager initialized");
        }
        
        /// <summary>
        /// Resets the state manager to its initial state
        /// </summary>
        public void Reset()
        {
            TransitionToState(FlightState.PREFLIGHT);
            Logger.Log(LogLevel.Information, "GSXStateManager:Reset", "State manager reset to PREFLIGHT");
        }
        
        /// <summary>
        /// Checks if the current state is PREFLIGHT
        /// </summary>
        public bool IsPreflight() => state == FlightState.PREFLIGHT;
        
        /// <summary>
        /// Checks if the current state is DEPARTURE
        /// </summary>
        public bool IsDeparture() => state == FlightState.DEPARTURE;
        
        /// <summary>
        /// Checks if the current state is TAXIOUT
        /// </summary>
        public bool IsTaxiout() => state == FlightState.TAXIOUT;
        
        /// <summary>
        /// Checks if the current state is FLIGHT
        /// </summary>
        public bool IsFlight() => state == FlightState.FLIGHT;
        
        /// <summary>
        /// Checks if the current state is TAXIIN
        /// </summary>
        public bool IsTaxiin() => state == FlightState.TAXIIN;
        
        /// <summary>
        /// Checks if the current state is ARRIVAL
        /// </summary>
        public bool IsArrival() => state == FlightState.ARRIVAL;
        
        /// <summary>
        /// Checks if the current state is TURNAROUND
        /// </summary>
        public bool IsTurnaround() => state == FlightState.TURNAROUND;
        
        /// <summary>
        /// Transitions to PREFLIGHT state
        /// </summary>
        public void TransitionToPreflight() => TransitionToState(FlightState.PREFLIGHT);
        
        /// <summary>
        /// Transitions to DEPARTURE state
        /// </summary>
        public void TransitionToDeparture() => TransitionToState(FlightState.DEPARTURE);
        
        /// <summary>
        /// Transitions to TAXIOUT state
        /// </summary>
        public void TransitionToTaxiout() => TransitionToState(FlightState.TAXIOUT);
        
        /// <summary>
        /// Transitions to FLIGHT state
        /// </summary>
        public void TransitionToFlight() => TransitionToState(FlightState.FLIGHT);
        
        /// <summary>
        /// Transitions to TAXIIN state
        /// </summary>
        public void TransitionToTaxiin() => TransitionToState(FlightState.TAXIIN);
        
        /// <summary>
        /// Transitions to ARRIVAL state
        /// </summary>
        public void TransitionToArrival() => TransitionToState(FlightState.ARRIVAL);
        
        /// <summary>
        /// Transitions to TURNAROUND state
        /// </summary>
        public void TransitionToTurnaround() => TransitionToState(FlightState.TURNAROUND);
        
        /// <summary>
        /// Validates if a transition from the current state to the specified state is valid
        /// </summary>
        /// <param name="targetState">The target state</param>
        /// <returns>True if the transition is valid, false otherwise</returns>
        public bool IsValidTransition(FlightState targetState)
        {
            // Allow transition to the same state
            if (state == targetState)
                return true;
                
            // Check if the transition is valid
            return validTransitions.ContainsKey(state) && validTransitions[state].Contains(targetState);
        }
        
        /// <summary>
        /// Transitions to the specified state
        /// </summary>
        /// <param name="newState">The new state</param>
        private void TransitionToState(FlightState newState)
        {
            // Skip if already in the target state
            if (state == newState)
                return;
                
            // Check if the transition is valid
            if (!IsValidTransition(newState))
            {
                Logger.Log(LogLevel.Warning, "GSXStateManager:TransitionToState", $"Invalid state transition from {state} to {newState}");
                return;
            }
            
            // Store the previous state
            FlightState previousState = state;
            
            // Update the state
            state = newState;
            
            // Log the transition
            Logger.Log(LogLevel.Information, "GSXStateManager:TransitionToState", $"State changed from {previousState} to {newState}");
            
            // Raise the StateChanged event
            OnStateChanged(previousState, newState);
        }
        
        /// <summary>
        /// Raises the StateChanged event
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        protected virtual void OnStateChanged(FlightState previousState, FlightState newState)
        {
            StateChanged?.Invoke(this, new StateChangedEventArgs(previousState, newState));
        }
    }
}
