using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX state management
    /// </summary>
    public interface IGSXStateManager : IDisposable
    {
        #region Events
        
        /// <summary>
        /// Event raised when the flight state changes
        /// </summary>
        event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// Event raised when the predicted next state changes
        /// </summary>
        event EventHandler<PredictedStateChangedEventArgs> PredictedStateChanged;
        
        /// <summary>
        /// Event raised when a state timeout occurs
        /// </summary>
        event EventHandler<StateTimeoutEventArgs> StateTimeout;
        
        /// <summary>
        /// Event raised when state is restored from persistence
        /// </summary>
        event EventHandler<StateRestoredEventArgs> StateRestored;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        FlightState CurrentState { get; }
        
        /// <summary>
        /// Gets the history of state transitions
        /// </summary>
        IReadOnlyList<StateTransitionRecord> StateHistory { get; }
        
        /// <summary>
        /// Gets the timestamp when the current state was entered
        /// </summary>
        DateTime CurrentStateEnteredAt { get; }
        
        /// <summary>
        /// Gets the duration the system has been in the current state
        /// </summary>
        TimeSpan CurrentStateDuration { get; }
        
        /// <summary>
        /// Gets the currently predicted next state
        /// </summary>
        FlightState? PredictedNextState { get; }
        
        #endregion
        
        #region State Queries
        
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
        
        #endregion
        
        #region Basic State Transitions
        
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
        
        #endregion
        
        #region Conditional State Transitions
        
        /// <summary>
        /// Attempts to transition to the specified state if conditions are met
        /// </summary>
        /// <param name="targetState">The target state</param>
        /// <param name="aircraftParameters">Current aircraft parameters</param>
        /// <param name="reason">Optional reason for the transition</param>
        /// <returns>True if the transition was successful, false otherwise</returns>
        bool TryTransitionToState(FlightState targetState, AircraftParameters aircraftParameters, string reason = null);
        
        /// <summary>
        /// Evaluates if a transition to the specified state is valid based on current conditions
        /// </summary>
        /// <param name="targetState">The target state</param>
        /// <param name="aircraftParameters">Current aircraft parameters</param>
        /// <returns>True if the transition is valid, false otherwise with reason</returns>
        (bool IsValid, string Reason) EvaluateTransition(FlightState targetState, AircraftParameters aircraftParameters);
        
        /// <summary>
        /// Validates if a transition from the current state to the specified state is valid
        /// </summary>
        /// <param name="targetState">The target state</param>
        /// <returns>True if the transition is valid, false otherwise</returns>
        bool IsValidTransition(FlightState targetState);
        
        #endregion
        
        #region State Prediction
        
        /// <summary>
        /// Predicts the next likely state based on current aircraft conditions
        /// </summary>
        /// <param name="aircraftParameters">Current aircraft parameters</param>
        /// <returns>The predicted next state and confidence level</returns>
        (FlightState PredictedState, float Confidence) PredictNextState(AircraftParameters aircraftParameters);
        
        #endregion
        
        #region State-Specific Behavior Hooks
        
        /// <summary>
        /// Registers a callback to be executed when entering a specific state
        /// </summary>
        void RegisterStateEntryAction(FlightState state, Action<StateChangedEventArgs> action);
        
        /// <summary>
        /// Registers a callback to be executed when exiting a specific state
        /// </summary>
        void RegisterStateExitAction(FlightState state, Action<StateChangedEventArgs> action);
        
        /// <summary>
        /// Registers a callback to be executed during a specific state transition
        /// </summary>
        void RegisterStateTransitionAction(FlightState fromState, FlightState toState, Action<StateChangedEventArgs> action);
        
        /// <summary>
        /// Unregisters a previously registered state entry action
        /// </summary>
        void UnregisterStateEntryAction(FlightState state, Action<StateChangedEventArgs> action);
        
        /// <summary>
        /// Unregisters a previously registered state exit action
        /// </summary>
        void UnregisterStateExitAction(FlightState state, Action<StateChangedEventArgs> action);
        
        /// <summary>
        /// Unregisters a previously registered state transition action
        /// </summary>
        void UnregisterStateTransitionAction(FlightState fromState, FlightState toState, Action<StateChangedEventArgs> action);
        
        #endregion
        
        #region Timeout Handling
        
        /// <summary>
        /// Sets a timeout for the current state
        /// </summary>
        /// <param name="timeout">The timeout duration</param>
        /// <param name="timeoutAction">Action to execute when the timeout occurs</param>
        /// <returns>A token that can be used to cancel the timeout</returns>
        IDisposable SetStateTimeout(TimeSpan timeout, Action<StateTimeoutEventArgs> timeoutAction);
        
        #endregion
        
        #region State Persistence
        
        /// <summary>
        /// Saves the current state to the specified file
        /// </summary>
        /// <param name="filePath">Path to save the state</param>
        void SaveState(string filePath);
        
        /// <summary>
        /// Loads state from the specified file
        /// </summary>
        /// <param name="filePath">Path to load the state from</param>
        /// <returns>True if the state was loaded successfully, false otherwise</returns>
        bool TryLoadState(string filePath);
        
        #endregion
        
        #region Initialization and Reset
        
        /// <summary>
        /// Initializes the state manager
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Resets the state manager to its initial state
        /// </summary>
        void Reset();
        
        #endregion
    }
}
