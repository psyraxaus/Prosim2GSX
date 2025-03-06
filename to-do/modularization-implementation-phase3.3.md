# Phase 3.3: GSXStateManager Implementation

## Overview

This document outlines the implementation plan for Phase 3.3 of the Prosim2GSX modularization strategy. In this phase, we've extracted flight state management functionality from the GsxController into a dedicated service.

## Implementation Steps

### 1. Create StateChangedEventArgs.cs

Created a new event args class in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for state changes
    /// </summary>
    public class StateChangedEventArgs : EventArgs
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
        /// Gets the timestamp of the state change
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the StateChangedEventArgs class
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        public StateChangedEventArgs(FlightState previousState, FlightState newState)
        {
            PreviousState = previousState;
            NewState = newState;
            Timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Flight states for GSX integration
    /// </summary>
    public enum FlightState
    {
        PREFLIGHT,
        DEPARTURE,
        TAXIOUT,
        FLIGHT,
        TAXIIN,
        ARRIVAL,
        TURNAROUND
    }
}
```

### 2. Create IGSXStateManager.cs

Created a new interface file in the Services folder:

```csharp
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
```

### 3. Create GSXStateManager.cs

Created a new implementation file in the Services folder:

```csharp
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
```

### 4. Update GsxController.cs

Updated the GsxController class to use the new state manager:

1. Removed the FlightState enum from GsxController since it's now defined in StateChangedEventArgs.cs
2. Added a field for the IGSXStateManager
3. Updated the constructor to accept and initialize the IGSXStateManager
4. Added an event handler for state changes
5. Updated the RunServices method to use the state manager instead of directly managing the state
6. Added a Dispose method to clean up event subscriptions

### 5. Update ServiceController.cs

Updated the ServiceController class to initialize the new state manager:

```csharp
// Step 7: Create GSX services
var menuService = new GSXMenuService(Model, IPCManager.SimConnect);
var audioService = new GSXAudioService(Model, IPCManager.SimConnect, audioSessionManager);
var stateManager = new GSXStateManager();

// Configure audio service properties
audioService.AudioSessionRetryCount = 5; // Increase retry count for better reliability
audioService.AudioSessionRetryDelay = TimeSpan.FromSeconds(1); // Shorter delay between retries

// Step 8: Create GsxController
var gsxController = new GsxController(Model, ProsimController, FlightPlan, acarsService, menuService, audioService, stateManager);
```

## Benefits

1. **Improved State Management**
   - Clear state transitions with validation
   - Event-based notification for state changes
   - Centralized state management logic

2. **Enhanced Testability**
   - State transitions can be tested in isolation
   - Event raising can be verified
   - Invalid transitions can be tested

3. **Better Maintainability**
   - State management is now in a dedicated service
   - GsxController is simplified
   - State-specific behavior can be encapsulated

4. **Clearer Flight Phase Tracking**
   - Explicit states for each flight phase
   - Validation prevents invalid state transitions
   - State queries are more readable (e.g., `stateManager.IsDeparture()`)

5. **Improved Error Handling**
   - Invalid state transitions are logged and prevented
   - State changes are logged for better debugging
   - Event-based communication for state changes

## Next Steps

After implementing the GSXStateManager in Phase 3.3, we'll proceed with Phase 3.4 to implement the GSXServiceCoordinator. This will further modularize the GsxController by extracting service coordination functionality into a dedicated service.

The GSXServiceCoordinator will be responsible for:
- Coordinating GSX services (boarding, refueling, etc.)
- Managing service timing and sequencing
- Raising events for service status changes
- Centralizing service operation logic

This will continue our modularization effort, making the codebase more maintainable, testable, and extensible.
