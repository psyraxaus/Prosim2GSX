using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX state management
    /// </summary>
    public interface IGSXStateManager
    {
        /// <summary>
        /// Event raised when the flight state changes
        /// </summary>
        event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// Initializes the state manager
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        FlightState CurrentState { get; }
        
        /// <summary>
        /// Checks if the current state is PREFLIGHT
        /// </summary>
        bool IsPreflight();
        
        /// <summary>
        /// Checks if the current state is DEPARTURE
        /// </summary>
        bool IsDeparture();
        
        /// <summary>
        /// Checks if the current state is TAXIOUT
        /// </summary>
        bool IsTaxiout();
        
        /// <summary>
        /// Checks if the current state is FLIGHT
        /// </summary>
        bool IsFlight();
        
        /// <summary>
        /// Checks if the current state is TAXIIN
        /// </summary>
        bool IsTaxiin();
        
        /// <summary>
        /// Checks if the current state is ARRIVAL
        /// </summary>
        bool IsArrival();
        
        /// <summary>
        /// Checks if the current state is TURNAROUND
        /// </summary>
        bool IsTurnaround();
        
        /// <summary>
        /// Transitions to PREFLIGHT state
        /// </summary>
        void TransitionToPreflight();
        
        /// <summary>
        /// Transitions to DEPARTURE state
        /// </summary>
        void TransitionToDeparture();
        
        /// <summary>
        /// Transitions to TAXIOUT state
        /// </summary>
        void TransitionToTaxiout();
        
        /// <summary>
        /// Transitions to FLIGHT state
        /// </summary>
        void TransitionToFlight();
        
        /// <summary>
        /// Transitions to TAXIIN state
        /// </summary>
        void TransitionToTaxiin();
        
        /// <summary>
        /// Transitions to ARRIVAL state
        /// </summary>
        void TransitionToArrival();
        
        /// <summary>
        /// Transitions to TURNAROUND state
        /// </summary>
        void TransitionToTurnaround();
        
        /// <summary>
        /// Validates if a transition from the current state to the specified state is valid
        /// </summary>
        /// <param name="targetState">The target state</param>
        /// <returns>True if the transition is valid, false otherwise</returns>
        bool IsValidTransition(FlightState targetState);
        
        /// <summary>
        /// Resets the state manager to its initial state
        /// </summary>
        void Reset();
    }
}
