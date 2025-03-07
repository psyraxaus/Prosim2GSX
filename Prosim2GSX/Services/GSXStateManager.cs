using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX state management
    /// </summary>
    public class GSXStateManager : IGSXStateManager
    {
        #region Fields
        
        private FlightState state = FlightState.PREFLIGHT;
        private readonly Dictionary<FlightState, HashSet<FlightState>> validTransitions;
        private readonly List<StateTransitionRecord> _stateHistory = new List<StateTransitionRecord>();
        private DateTime _currentStateEnteredAt = DateTime.Now;
        
        private FlightState? _predictedNextState;
        private float _predictionConfidence;
        
        private readonly Dictionary<FlightState, List<Action<StateChangedEventArgs>>> _entryActions = new Dictionary<FlightState, List<Action<StateChangedEventArgs>>>();
        private readonly Dictionary<FlightState, List<Action<StateChangedEventArgs>>> _exitActions = new Dictionary<FlightState, List<Action<StateChangedEventArgs>>>();
        private readonly Dictionary<(FlightState From, FlightState To), List<Action<StateChangedEventArgs>>> _transitionActions = new Dictionary<(FlightState, FlightState), List<Action<StateChangedEventArgs>>>();
        
        private readonly List<TimeoutRegistration> _timeoutRegistrations = new List<TimeoutRegistration>();
        private readonly Timer _timeoutTimer;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event raised when the flight state changes
        /// </summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// Event raised when the predicted next state changes
        /// </summary>
        public event EventHandler<PredictedStateChangedEventArgs> PredictedStateChanged;
        
        /// <summary>
        /// Event raised when a state timeout occurs
        /// </summary>
        public event EventHandler<StateTimeoutEventArgs> StateTimeout;
        
        /// <summary>
        /// Event raised when state is restored from persistence
        /// </summary>
        public event EventHandler<StateRestoredEventArgs> StateRestored;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        public FlightState CurrentState => state;
        
        /// <summary>
        /// Gets the history of state transitions
        /// </summary>
        public IReadOnlyList<StateTransitionRecord> StateHistory => _stateHistory.AsReadOnly();
        
        /// <summary>
        /// Gets the timestamp when the current state was entered
        /// </summary>
        public DateTime CurrentStateEnteredAt => _currentStateEnteredAt;
        
        /// <summary>
        /// Gets the duration the system has been in the current state
        /// </summary>
        public TimeSpan CurrentStateDuration => DateTime.Now - _currentStateEnteredAt;
        
        /// <summary>
        /// Gets the currently predicted next state
        /// </summary>
        public FlightState? PredictedNextState => _predictedNextState;
        
        #endregion
        
        #region Constructor and Initialization
        
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
            
            // Initialize timeout timer
            _timeoutTimer = new Timer(CheckTimeouts, null, Timeout.Infinite, Timeout.Infinite);
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
            // Clear state history
            _stateHistory.Clear();
            
            // Reset predicted state
            _predictedNextState = null;
            _predictionConfidence = 0;
            
            // Reset current state
            TransitionToState(FlightState.PREFLIGHT, "Reset");
            
            Logger.Log(LogLevel.Information, "GSXStateManager:Reset", "State manager reset to PREFLIGHT");
        }
        
        /// <summary>
        /// Disposes resources used by the GSXStateManager
        /// </summary>
        public void Dispose()
        {
            _timeoutTimer?.Dispose();
        }
        
        #endregion
        
        #region State Queries
        
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
        
        #endregion
        
        #region Basic State Transitions
        
        /// <summary>
        /// Transitions to PREFLIGHT state
        /// </summary>
        public void TransitionToPreflight() => TransitionToState(FlightState.PREFLIGHT, "Manual transition");
        
        /// <summary>
        /// Transitions to DEPARTURE state
        /// </summary>
        public void TransitionToDeparture() => TransitionToState(FlightState.DEPARTURE, "Manual transition");
        
        /// <summary>
        /// Transitions to TAXIOUT state
        /// </summary>
        public void TransitionToTaxiout() => TransitionToState(FlightState.TAXIOUT, "Manual transition");
        
        /// <summary>
        /// Transitions to FLIGHT state
        /// </summary>
        public void TransitionToFlight() => TransitionToState(FlightState.FLIGHT, "Manual transition");
        
        /// <summary>
        /// Transitions to TAXIIN state
        /// </summary>
        public void TransitionToTaxiin() => TransitionToState(FlightState.TAXIIN, "Manual transition");
        
        /// <summary>
        /// Transitions to ARRIVAL state
        /// </summary>
        public void TransitionToArrival() => TransitionToState(FlightState.ARRIVAL, "Manual transition");
        
        /// <summary>
        /// Transitions to TURNAROUND state
        /// </summary>
        public void TransitionToTurnaround() => TransitionToState(FlightState.TURNAROUND, "Manual transition");
        
        /// <summary>
        /// Transitions to the specified state
        /// </summary>
        /// <param name="newState">The new state</param>
        /// <param name="reason">Optional reason for the transition</param>
        private void TransitionToState(FlightState newState, string reason = null)
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
            DateTime now = DateTime.Now;
            TimeSpan duration = now - _currentStateEnteredAt;
            
            // Record the transition
            _stateHistory.Add(new StateTransitionRecord(previousState, newState, now, duration, reason));
            
            // Update the state
            state = newState;
            _currentStateEnteredAt = now;
            
            // Log the transition
            Logger.Log(LogLevel.Information, "GSXStateManager:TransitionToState", $"State changed from {previousState} to {newState}" + (reason != null ? $" - Reason: {reason}" : ""));
            
            // Raise the StateChanged event
            OnStateChanged(previousState, newState);
        }
        
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
        
        #endregion
        
        #region Conditional State Transitions
        
        /// <summary>
        /// Attempts to transition to the specified state if conditions are met
        /// </summary>
        /// <param name="targetState">The target state</param>
        /// <param name="aircraftParameters">Current aircraft parameters</param>
        /// <param name="reason">Optional reason for the transition</param>
        /// <returns>True if the transition was successful, false otherwise</returns>
        public bool TryTransitionToState(FlightState targetState, AircraftParameters aircraftParameters, string reason = null)
        {
            var evaluation = EvaluateTransition(targetState, aircraftParameters);
            
            if (!evaluation.IsValid)
            {
                Logger.Log(LogLevel.Warning, "GSXStateManager:TryTransitionToState", 
                    $"Transition from {CurrentState} to {targetState} not valid: {evaluation.Reason}");
                return false;
            }
            
            // Combine reasons if both are provided
            string transitionReason = reason;
            if (string.IsNullOrEmpty(reason) && !string.IsNullOrEmpty(evaluation.Reason))
                transitionReason = evaluation.Reason;
            else if (!string.IsNullOrEmpty(reason) && !string.IsNullOrEmpty(evaluation.Reason))
                transitionReason = $"{reason} ({evaluation.Reason})";
            
            TransitionToState(targetState, transitionReason);
            return true;
        }
        
        /// <summary>
        /// Evaluates if a transition to the specified state is valid based on current conditions
        /// </summary>
        /// <param name="targetState">The target state</param>
        /// <param name="aircraftParameters">Current aircraft parameters</param>
        /// <returns>True if the transition is valid, false otherwise with reason</returns>
        public (bool IsValid, string Reason) EvaluateTransition(FlightState targetState, AircraftParameters aircraftParameters)
        {
            // First check basic state machine rules
            if (!IsValidTransition(targetState))
                return (false, "Invalid state transition according to state machine rules");
            
            // Then check aircraft parameter conditions
            switch (CurrentState)
            {
                case FlightState.PREFLIGHT when targetState == FlightState.DEPARTURE:
                    if (!aircraftParameters.FlightPlanLoaded)
                        return (false, "Flight plan not loaded");
                    return (true, "Flight plan loaded");
                    
                case FlightState.DEPARTURE when targetState == FlightState.TAXIOUT:
                    if (aircraftParameters.GroundEquipmentConnected)
                        return (false, "Ground equipment still connected");
                    if (!aircraftParameters.BeaconOn)
                        return (false, "Beacon not turned on");
                    if (aircraftParameters.ParkingBrakeSet)
                        return (false, "Parking brake still set");
                    return (true, "Ready for taxi");
                    
                case FlightState.TAXIOUT when targetState == FlightState.FLIGHT:
                    if (aircraftParameters.OnGround)
                        return (false, "Aircraft still on ground");
                    return (true, "Aircraft airborne");
                    
                case FlightState.FLIGHT when targetState == FlightState.TAXIIN:
                    if (!aircraftParameters.OnGround)
                        return (false, "Aircraft not on ground");
                    return (true, "Aircraft landed");
                    
                case FlightState.TAXIIN when targetState == FlightState.ARRIVAL:
                    if (!aircraftParameters.OnGround)
                        return (false, "Aircraft not on ground");
                    if (!aircraftParameters.ParkingBrakeSet)
                        return (false, "Parking brake not set");
                    if (aircraftParameters.EnginesRunning)
                        return (false, "Engines still running");
                    return (true, "Aircraft parked");
                    
                case FlightState.ARRIVAL when targetState == FlightState.TURNAROUND:
                    // This is typically triggered by completion of deboarding
                    // No specific aircraft parameters to check
                    return (true, "Deboarding complete");
                    
                case FlightState.TURNAROUND when targetState == FlightState.DEPARTURE:
                    if (!aircraftParameters.FlightPlanLoaded)
                        return (false, "Flight plan not loaded");
                    return (true, "New flight plan loaded");
                    
                default:
                    // For transitions not covered by specific rules, fall back to basic state machine validation
                    return (IsValidTransition(targetState), "Basic state transition rules");
            }
        }
        
        #endregion
        
        #region State Prediction
        
        /// <summary>
        /// Predicts the next likely state based on current aircraft conditions
        /// </summary>
        /// <param name="aircraftParameters">Current aircraft parameters</param>
        /// <returns>The predicted next state and confidence level</returns>
        public (FlightState PredictedState, float Confidence) PredictNextState(AircraftParameters aircraftParameters)
        {
            var prediction = PredictNextStateInternal(aircraftParameters);
            
            // If the prediction has changed, update and raise event
            if (_predictedNextState != prediction.PredictedState || Math.Abs(_predictionConfidence - prediction.Confidence) > 0.1f)
            {
                var previousPrediction = _predictedNextState;
                _predictedNextState = prediction.PredictedState;
                _predictionConfidence = prediction.Confidence;
                
                OnPredictedStateChanged(previousPrediction, prediction.PredictedState, prediction.Confidence);
            }
            
            return prediction;
        }
        
        /// <summary>
        /// Internal implementation of state prediction
        /// </summary>
        private (FlightState PredictedState, float Confidence) PredictNextStateInternal(AircraftParameters parameters)
        {
            // Rule-based decision tree for state prediction
            switch (CurrentState)
            {
                case FlightState.PREFLIGHT:
                    if (parameters.FlightPlanLoaded)
                        return (FlightState.DEPARTURE, 0.9f);
                    return (FlightState.PREFLIGHT, 1.0f);
                    
                case FlightState.DEPARTURE:
                    if (!parameters.GroundEquipmentConnected && parameters.BeaconOn && !parameters.ParkingBrakeSet)
                        return (FlightState.TAXIOUT, 0.85f);
                    return (FlightState.DEPARTURE, 0.95f);
                    
                case FlightState.TAXIOUT:
                    if (!parameters.OnGround)
                        return (FlightState.FLIGHT, 0.9f);
                    if (parameters.EnginesRunning && parameters.GroundSpeed > 5)
                        return (FlightState.TAXIOUT, 0.95f);
                    return (FlightState.TAXIOUT, 0.8f);
                    
                case FlightState.FLIGHT:
                    if (parameters.OnGround && parameters.GroundSpeed < 100)
                        return (FlightState.TAXIIN, 0.9f);
                    return (FlightState.FLIGHT, 0.95f);
                    
                case FlightState.TAXIIN:
                    if (parameters.OnGround && parameters.GroundSpeed < 1 && parameters.ParkingBrakeSet && !parameters.EnginesRunning)
                        return (FlightState.ARRIVAL, 0.9f);
                    return (FlightState.TAXIIN, 0.85f);
                    
                case FlightState.ARRIVAL:
                    // No automatic prediction for transition to TURNAROUND
                    // This is typically triggered by completion of deboarding
                    return (FlightState.ARRIVAL, 0.95f);
                    
                case FlightState.TURNAROUND:
                    if (parameters.FlightPlanLoaded)
                        return (FlightState.DEPARTURE, 0.85f);
                    return (FlightState.TURNAROUND, 0.9f);
                    
                default:
                    return (CurrentState, 0.5f);
            }
        }
        
        /// <summary>
        /// Raises the PredictedStateChanged event
        /// </summary>
        protected virtual void OnPredictedStateChanged(FlightState? previousPrediction, FlightState newPrediction, float confidence)
        {
            PredictedStateChanged?.Invoke(this, new PredictedStateChangedEventArgs(previousPrediction, newPrediction, confidence));
        }
        
        #endregion
        
        #region State-Specific Behavior Hooks
        
        /// <summary>
        /// Registers a callback to be executed when entering a specific state
        /// </summary>
        public void RegisterStateEntryAction(FlightState state, Action<StateChangedEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            lock (_entryActions)
            {
                if (!_entryActions.ContainsKey(state))
                    _entryActions[state] = new List<Action<StateChangedEventArgs>>();
                    
                _entryActions[state].Add(action);
            }
        }
        
        /// <summary>
        /// Registers a callback to be executed when exiting a specific state
        /// </summary>
        public void RegisterStateExitAction(FlightState state, Action<StateChangedEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            lock (_exitActions)
            {
                if (!_exitActions.ContainsKey(state))
                    _exitActions[state] = new List<Action<StateChangedEventArgs>>();
                    
                _exitActions[state].Add(action);
            }
        }
        
        /// <summary>
        /// Registers a callback to be executed during a specific state transition
        /// </summary>
        public void RegisterStateTransitionAction(FlightState fromState, FlightState toState, Action<StateChangedEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            var key = (fromState, toState);
            
            lock (_transitionActions)
            {
                if (!_transitionActions.ContainsKey(key))
                    _transitionActions[key] = new List<Action<StateChangedEventArgs>>();
                    
                _transitionActions[key].Add(action);
            }
        }
        
        /// <summary>
        /// Unregisters a previously registered state entry action
        /// </summary>
        public void UnregisterStateEntryAction(FlightState state, Action<StateChangedEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            lock (_entryActions)
            {
                if (_entryActions.ContainsKey(state))
                    _entryActions[state].Remove(action);
            }
        }
        
        /// <summary>
        /// Unregisters a previously registered state exit action
        /// </summary>
        public void UnregisterStateExitAction(FlightState state, Action<StateChangedEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            lock (_exitActions)
            {
                if (_exitActions.ContainsKey(state))
                    _exitActions[state].Remove(action);
            }
        }
        
        /// <summary>
        /// Unregisters a previously registered state transition action
        /// </summary>
        public void UnregisterStateTransitionAction(FlightState fromState, FlightState toState, Action<StateChangedEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            var key = (fromState, toState);
            
            lock (_transitionActions)
            {
                if (_transitionActions.ContainsKey(key))
                    _transitionActions[key].Remove(action);
            }
        }
        
        /// <summary>
        /// Raises the StateChanged event
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        protected virtual void OnStateChanged(FlightState previousState, FlightState newState)
        {
            var args = new StateChangedEventArgs(previousState, newState);
            
            // Execute exit actions for the previous state
            ExecuteActions(_exitActions, previousState, args);
            
            // Execute transition actions
            var transitionKey = (previousState, newState);
            ExecuteActions(_transitionActions, transitionKey, args);
            
            // Execute entry actions for the new state
            ExecuteActions(_entryActions, newState, args);
            
            // Raise the StateChanged event
            StateChanged?.Invoke(this, args);
        }
        
        private void ExecuteActions<T>(Dictionary<T, List<Action<StateChangedEventArgs>>> actionDict, T key, StateChangedEventArgs args)
        {
            List<Action<StateChangedEventArgs>> actions = null;
            
            lock (actionDict)
            {
                if (actionDict.ContainsKey(key))
                    actions = new List<Action<StateChangedEventArgs>>(actionDict[key]);
            }
            
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    try
                    {
                        action(args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "GSXStateManager:ExecuteActions", $"Error executing action: {ex.Message}");
                    }
                }
            }
        }
        
        #endregion
        
        #region Timeout Handling
        
        /// <summary>
        /// Timeout registration class for state timeouts
        /// </summary>
        private class TimeoutRegistration : IDisposable
        {
            public Guid Id { get; } = Guid.NewGuid();
            public FlightState State { get; init; }
            public DateTime ExpiresAt { get; init; }
            public Action<StateTimeoutEventArgs> TimeoutAction { get; init; }
            public bool IsCancelled { get; private set; }
            
            public void Dispose() => IsCancelled = true;
        }
        
        /// <summary>
        /// Event raised when a state timeout occurs
        /// </summary>
        public event EventHandler<StateTimeoutEventArgs> StateTimeout;
        
        /// <summary>
        /// Sets a timeout for the current state
        /// </summary>
        /// <param name="timeout">The timeout duration</param>
        /// <param name="timeoutAction">Action to execute when the timeout occurs</param>
        /// <returns>A token that can be used to cancel the timeout</returns>
        public IDisposable SetStateTimeout(TimeSpan timeout, Action<StateTimeoutEventArgs> timeoutAction)
        {
            if (timeoutAction == null)
                throw new ArgumentNullException(nameof(timeoutAction));
                
            var registration = new TimeoutRegistration
            {
                State = CurrentState,
                ExpiresAt = DateTime.Now.Add(timeout),
                TimeoutAction = timeoutAction
            };
            
            lock (_timeoutRegistrations)
            {
                _timeoutRegistrations.Add(registration);
                
                // Ensure timer is running
                _timeoutTimer.Change(100, 100); // Check every 100ms
            }
            
            return registration; // Caller can dispose to cancel
        }
        
        private void CheckTimeouts(object state)
        {
            var now = DateTime.Now;
            List<TimeoutRegistration> expiredTimeouts = null;
            
            lock (_timeoutRegistrations)
            {
                expiredTimeouts = _timeoutRegistrations
                    .Where(t => !t.IsCancelled && t.State == CurrentState && t.ExpiresAt <= now)
                    .ToList();
                    
                foreach (var timeout in expiredTimeouts)
                {
                    _timeoutRegistrations.Remove(timeout);
                }
                
                // Stop timer if no more timeouts
                if (_timeoutRegistrations.Count == 0)
                {
                    _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
            
            // Execute timeout actions outside the lock
            foreach (var timeout in expiredTimeouts)
            {
                try
                {
                    var args = new StateTimeoutEventArgs(
                        timeout.State,
                        CurrentStateEnteredAt,
                        now - CurrentStateEnteredAt,
                        timeout.ExpiresAt);
                        
                    timeout.TimeoutAction(args);
                    OnStateTimeout(args);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "GSXStateManager:CheckTimeouts", 
                        $"Error executing timeout action: {ex.Message}");
                }
            }
        }
        
        protected virtual void OnStateTimeout(StateTimeoutEventArgs args)
        {
            StateTimeout?.Invoke(this, args);
        }
        
        #endregion
        
        #region State Persistence
        
        /// <summary>
        /// Event raised when state is restored from persistence
        /// </summary>
        public event EventHandler<StateRestoredEventArgs> StateRestored;
        
        /// <summary>
        /// Saves the current state to the specified file
        /// </summary>
        /// <param name="filePath">Path to save the state</param>
        public void SaveState(string filePath)
        {
            var stateData = new StateData
            {
                CurrentState = CurrentState,
                CurrentStateEnteredAt = CurrentStateEnteredAt,
                StateHistory = _stateHistory.ToList(),
                PredictedNextState = PredictedNextState,
                LastUpdated = DateTime.Now
            };
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            
            var json = JsonSerializer.Serialize(stateData, options);
            File.WriteAllText(filePath, json);
            
            Logger.Log(LogLevel.Information, "GSXStateManager:SaveState", 
                $"State saved to {filePath}");
        }
        
        /// <summary>
        /// Loads state from the specified file
        /// </summary>
        /// <param name="filePath">Path to load the state from</param>
        /// <returns>True if the state was loaded successfully, false otherwise</returns>
        public bool TryLoadState(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;
                    
                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };
                
                var stateData = JsonSerializer.Deserialize<StateData>(json, options);
                
                // Validate the loaded state
                if (stateData == null || !Enum.IsDefined(typeof(FlightState), stateData.CurrentState))
                    return false;
                    
                // Apply the loaded state
                _stateHistory.Clear();
                foreach (var record in stateData.StateHistory)
                {
                    _stateHistory.Add(record);
                }
                
                // Set the current state without triggering normal transition logic
                state = stateData.CurrentState;
                _currentStateEnteredAt = stateData.CurrentStateEnteredAt;
                _predictedNextState = stateData.PredictedNextState;
                
                Logger.Log(LogLevel.Information, "GSXStateManager:TryLoadState", 
                    $"State loaded from {filePath}");
                    
                // Notify listeners of the restored state
                OnStateRestored(new StateRestoredEventArgs(stateData.CurrentState));
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXStateManager:TryLoadState", 
                    $"Error loading state: {ex.Message}");
                return false;
            }
        }
        
        protected virtual void OnStateRestored(StateRestoredEventArgs args)
        {
            StateRestored?.Invoke(this, args);
        }
        
        #endregion
    }
}
