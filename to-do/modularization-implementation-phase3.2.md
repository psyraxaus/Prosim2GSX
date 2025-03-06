# Phase 3.2: GSXStateManager Implementation

## Overview

This document outlines the implementation plan for Phase 3.2 of the Prosim2GSX modularization strategy. In this phase, we'll extract state management functionality from the GsxController into a dedicated service.

## Implementation Steps

### 1. Create FlightStateChangedEventArgs.cs

Create a new event args class in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for flight state changes
    /// </summary>
    public class FlightStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previous flight state
        /// </summary>
        public FlightState PreviousState { get; }
        
        /// <summary>
        /// Gets the new flight state
        /// </summary>
        public FlightState NewState { get; }
        
        /// <summary>
        /// Gets the timestamp of the state change
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the FlightStateChangedEventArgs class
        /// </summary>
        /// <param name="previousState">The previous flight state</param>
        /// <param name="newState">The new flight state</param>
        public FlightStateChangedEventArgs(FlightState previousState, FlightState newState)
        {
            PreviousState = previousState;
            NewState = newState;
            Timestamp = DateTime.Now;
        }
    }
}
```

### 2. Create IGSXStateManager.cs

Create a new interface file in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX state management service
    /// </summary>
    public interface IGSXStateManager
    {
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        FlightState CurrentState { get; }
        
        /// <summary>
        /// Event raised when the flight state changes
        /// </summary>
        event EventHandler<FlightStateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// Initializes the state manager
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Updates the flight state based on current conditions
        /// </summary>
        /// <param name="simOnGround">Whether the aircraft is on the ground</param>
        /// <param name="enginesRunning">Whether the engines are running</param>
        /// <param name="batteryOn">Whether the battery is on</param>
        /// <param name="flightPlanLoaded">Whether a flight plan is loaded</param>
        /// <param name="flightPlanId">The ID of the loaded flight plan</param>
        /// <param name="isTestArrival">Whether this is a test arrival</param>
        /// <returns>True if the state was updated, false otherwise</returns>
        bool UpdateState(bool simOnGround, bool enginesRunning, bool batteryOn, bool flightPlanLoaded, string flightPlanId, bool isTestArrival);
        
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
        /// Transitions to the DEPARTURE state
        /// </summary>
        /// <param name="flightPlanId">The ID of the loaded flight plan</param>
        void TransitionToDeparture(string flightPlanId);
        
        /// <summary>
        /// Transitions to the TAXIOUT state
        /// </summary>
        void TransitionToTaxiout();
        
        /// <summary>
        /// Transitions to the FLIGHT state
        /// </summary>
        void TransitionToFlight();
        
        /// <summary>
        /// Transitions to the TAXIIN state
        /// </summary>
        void TransitionToTaxiin();
        
        /// <summary>
        /// Transitions to the ARRIVAL state
        /// </summary>
        void TransitionToArrival();
        
        /// <summary>
        /// Transitions to the TURNAROUND state
        /// </summary>
        void TransitionToTurnaround();
    }
}
```

### 3. Create GSXStateManager.cs

Create a new implementation file in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX state management
    /// </summary>
    public class GSXStateManager : IGSXStateManager
    {
        private FlightState state = FlightState.PREFLIGHT;
        private string currentFlightPlanId = "0";
        
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        public FlightState CurrentState => state;
        
        /// <summary>
        /// Event raised when the flight state changes
        /// </summary>
        public event EventHandler<FlightStateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// Initializes a new instance of the GSXStateManager class
        /// </summary>
        public GSXStateManager()
        {
        }
        
        /// <summary>
        /// Initializes the state manager
        /// </summary>
        public void Initialize()
        {
            state = FlightState.PREFLIGHT;
            currentFlightPlanId = "0";
            Logger.Log(LogLevel.Information, "GSXStateManager:Initialize", $"State initialized to {state}");
        }
        
        /// <summary>
        /// Updates the flight state based on current conditions
        /// </summary>
        public bool UpdateState(bool simOnGround, bool enginesRunning, bool batteryOn, bool flightPlanLoaded, string flightPlanId, bool isTestArrival)
        {
            FlightState previousState = state;
            bool stateChanged = false;
            
            // Special case: Test arrival
            if (state == FlightState.PREFLIGHT && isTestArrival)
            {
                state = FlightState.FLIGHT;
                currentFlightPlanId = flightPlanId;
                Logger.Log(LogLevel.Information, "GSXStateManager:UpdateState", $"Test Arrival - Plane is in 'Flight'");
                OnStateChanged(previousState, state);
                return true;
            }
            
            // Special case: Loaded in flight or with engines running
            if (state == FlightState.PREFLIGHT && (!simOnGround || enginesRunning))
            {
                state = FlightState.FLIGHT;
                currentFlightPlanId = flightPlanId;
                Logger.Log(LogLevel.Information, "GSXStateManager:UpdateState", $"Starting in flight or with engines running - State set to Flight");
                OnStateChanged(previousState, state);
                return true;
            }
            
            // Normal state transitions
            switch (state)
            {
                case FlightState.PREFLIGHT:
                    if (simOnGround && !enginesRunning && batteryOn && flightPlanLoaded)
                    {
                        state = FlightState.DEPARTURE;
                        currentFlightPlanId = flightPlanId;
                        Logger.Log(LogLevel.Information, "GSXStateManager:UpdateState", $"State Change: Preparation -> DEPARTURE (Waiting for Refueling and Boarding)");
                        stateChanged = true;
                    }
                    break;
                    
                case FlightState.DEPARTURE:
                    // Transition to TAXIOUT happens in GsxController when equipment is removed
                    break;
                    
                case FlightState.TAXIOUT:
                    if (!simOnGround)
                    {
                        state = FlightState.FLIGHT;
                        Logger.Log(LogLevel.Information, "GSXStateManager:UpdateState", $"State Change: Taxi-Out -> Flight");
                        stateChanged = true;
                    }
                    break;
                    
                case FlightState.FLIGHT:
                    if (simOnGround)
                    {
                        state = FlightState.TAXIIN;
                        Logger.Log(LogLevel.Information, "GSXStateManager:UpdateState", $"State Change: Flight -> Taxi-In (Waiting for Engines stopped and Beacon off)");
                        stateChanged = true;
                    }
                    break;
                    
                case FlightState.TAXIIN:
                    // Transition to ARRIVAL happens in GsxController when engines are stopped and parking brake is set
                    break;
                    
                case FlightState.ARRIVAL:
                    // Transition to TURNAROUND happens in GsxController when deboarding is complete
                    break;
                    
                case FlightState.TURNAROUND:
                    if (flightPlanLoaded && flightPlanId != currentFlightPlanId)
                    {
                        state = FlightState.DEPARTURE;
                        currentFlightPlanId = flightPlanId;
                        Logger.Log(LogLevel.Information, "GSXStateManager:UpdateState", $"State Change: Turn-Around -> DEPARTURE (Waiting for Refueling and Boarding)");
                        stateChanged = true;
                    }
                    break;
            }
            
            if (stateChanged)
            {
                OnStateChanged(previousState, state);
            }
            
            return stateChanged;
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
        /// Transitions to the DEPARTURE state
        /// </summary>
        public void TransitionToDeparture(string flightPlanId)
        {
            if (state != FlightState.DEPARTURE)
            {
                FlightState previousState = state;
                state = FlightState.DEPARTURE;
                currentFlightPlanId = flightPlanId;
                Logger.Log(LogLevel.Information, "GSXStateManager:TransitionToDeparture", $"State Change: {previousState} -> DEPARTURE");
                OnStateChanged(previousState, state);
            }
        }
        
        /// <summary>
        /// Transitions to the TAXIOUT state
        /// </summary>
        public void TransitionToTaxiout()
        {
            if (state != FlightState.TAXIOUT)
            {
                FlightState previousState = state;
                state = FlightState.TAXIOUT;
                Logger.Log(LogLevel.Information, "GSXStateManager:TransitionToTaxiout", $"State Change: {previousState} -> TAXIOUT");
                OnStateChanged(previousState, state);
            }
        }
        
        /// <summary>
        /// Transitions to the FLIGHT state
        /// </summary>
        public void TransitionToFlight()
        {
            if (state != FlightState.FLIGHT)
            {
                FlightState previousState = state;
                state = FlightState.FLIGHT;
                Logger.Log(LogLevel.Information, "GSXStateManager:TransitionToFlight", $"State Change: {previousState} -> FLIGHT");
                OnStateChanged(previousState, state);
            }
        }
        
        /// <summary>
        /// Transitions to the TAXIIN state
        /// </summary>
        public void TransitionToTaxiin()
        {
            if (state != FlightState.TAXIIN)
            {
                FlightState previousState = state;
                state = FlightState.TAXIIN;
                Logger.Log(LogLevel.Information, "GSXStateManager:TransitionToTaxiin", $"State Change: {previousState} -> TAXIIN");
                OnStateChanged(previousState, state);
            }
        }
        
        /// <summary>
        /// Transitions to the ARRIVAL state
        /// </summary>
        public void TransitionToArrival()
        {
            if (state != FlightState.ARRIVAL)
            {
                FlightState previousState = state;
                state = FlightState.ARRIVAL;
                Logger.Log(LogLevel.Information, "GSXStateManager:TransitionToArrival", $"State Change: {previousState} -> ARRIVAL");
                OnStateChanged(previousState, state);
            }
        }
        
        /// <summary>
        /// Transitions to the TURNAROUND state
        /// </summary>
        public void TransitionToTurnaround()
        {
            if (state != FlightState.TURNAROUND)
            {
                FlightState previousState = state;
                state = FlightState.TURNAROUND;
                Logger.Log(LogLevel.Information, "GSXStateManager:TransitionToTurnaround", $"State Change: {previousState} -> TURNAROUND");
                OnStateChanged(previousState, state);
            }
        }
        
        /// <summary>
        /// Raises the StateChanged event
        /// </summary>
        protected virtual void OnStateChanged(FlightState previousState, FlightState newState)
        {
            StateChanged?.Invoke(this, new FlightStateChangedEventArgs(previousState, newState));
        }
    }
}
```

### 4. Update GsxController.cs

Update the GsxController class to use the new service:

```csharp
// Add new field
private readonly IGSXStateManager stateManager;

// Update constructor
public GsxController(ServiceModel model, ProsimController prosimController, FlightPlan flightPlan, IAcarsService acarsService, IGSXMenuService menuService, IGSXAudioService audioService, IGSXStateManager stateManager)
{
    Model = model;
    ProsimController = prosimController;
    FlightPlan = flightPlan;
    this.acarsService = acarsService;
    this.menuService = menuService;
    this.audioService = audioService;
    this.stateManager = stateManager;

    SimConnect = IPCManager.SimConnect;
    // Subscribe to SimConnect variables...
    
    // Initialize state manager
    stateManager.Initialize();
    
    // Subscribe to state change events
    stateManager.StateChanged += OnStateChanged;
    
    if (Model.TestArrival)
        ProsimController.Update(true);
}

// Add event handler for state changes
private void OnStateChanged(object sender, FlightStateChangedEventArgs e)
{
    // Handle state changes
    switch (e.NewState)
    {
        case FlightState.FLIGHT:
            Interval = 180000; // Longer interval during flight
            break;
        case FlightState.TAXIIN:
            Interval = 2500; // Shorter interval during taxi
            if (Model.TestArrival)
                flightPlanID = ProsimController.flightPlanID;
            pcaCalled = false;
            connectCalled = false;
            break;
        default:
            Interval = 1000; // Default interval
            break;
    }
}

// Replace CurrentFlightState property with call to service
public FlightState CurrentFlightState => stateManager.CurrentState;

// Update RunServices method to use stateManager
public void RunServices()
{
    // Check if SimConnect is available
    if (SimConnect == null)
    {
        Logger.Log(LogLevel.Warning, "GsxController:RunServices", "SimConnect not available");
        return;
    }
    
    // Check if FlightPlan is available when needed
    if (FlightPlan == null)
    {
        Logger.Log(LogLevel.Warning, "GsxController:RunServices", "FlightPlan not available");
        return;
    }
    
    // Mark as initialized on first successful run
    if (!_isInitialized)
    {
        _isInitialized = true;
        Logger.Log(LogLevel.Information, "GsxController:RunServices", "GSX Controller initialized and ready");
    }
    
    bool simOnGround = SimConnect.ReadSimVar("SIM ON GROUND", "Bool") != 0.0f;
    ProsimController.Update(false);

    if (menuService.OperatorWasSelected)
    {
        MenuOpen();
        menuService.OperatorWasSelected = false;
    }

    // Update state based on current conditions
    bool batteryOn = ProsimController.Interface.ReadDataRef("system.switches.S_OH_ELEC_BAT1") == 1;
    bool flightPlanLoaded = ProsimController.IsFlightplanLoaded();
    stateManager.UpdateState(simOnGround, ProsimController.enginesRunning, batteryOn, flightPlanLoaded, ProsimController.flightPlanID, Model.TestArrival);

    // Handle ACARS initialization in PREFLIGHT state
    if (stateManager.IsPreflight() && simOnGround && !ProsimController.enginesRunning && batteryOn)
    {
        if (Model.UseAcars && !opsCallsignSet)
        {
            try
            {
                opsCallsign = acarsService.FlightCallsignToOpsCallsign(ProsimController.flightNumber);
                acarsService.Initialize(ProsimController.flightNumber);
                opsCallsignSet = true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GsxController:RunServices", $"Unable to set opsCallSign - Error: {ex.Message}");
            }
        }

        if (SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
        {
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Couatl Engine not running");

            if (Model.GsxVolumeControl && gsxAudioSession != null)
            {
                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Resetting GSX Audio (Engine not running)");
                gsxAudioSession = null;
            }

            return;
        }

        if (Model.RepositionPlane && !planePositioned)
        {
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Waiting {Model.RepositionDelay}s before Repositioning ...");
            ProsimController.SetServiceChocks(true);
            Thread.Sleep((int)(Model.RepositionDelay * 1000.0f));
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Repositioning Plane");
            MenuOpen();
            Thread.Sleep(100);
            MenuItem(10);
            Thread.Sleep(250);
            MenuItem(1);
            planePositioned = true;
            Thread.Sleep(1500);
            return;
        }
        else if (!Model.RepositionPlane && !planePositioned)
        {
            planePositioned = true;
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Repositioning was skipped (disabled in Settings)");
        }

        if (Model.AutoConnect && !connectCalled)
        {
            CallJetwayStairs();
            connectCalled = true;
            return;
        }

        if (Model.ConnectPCA && !pcaCalled && (!Model.PcaOnlyJetways || (Model.PcaOnlyJetways && SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
        {
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Connecting PCA");
            ProsimController.SetServicePCA(true);
            pcaCalled = true;
            return;
        }

        if (firstRun)
        {
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Setting GPU and Chocks");
            ProsimController.SetServiceChocks(true);
            ProsimController.SetServiceGPU(true);
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State: Preparation (Waiting for Flightplan import)");
            firstRun = false;
        }

        if (flightPlanLoaded)
        {
            stateManager.TransitionToDeparture(ProsimController.flightPlanID);
            flightPlanID = ProsimController.flightPlanID;
            SetPassengers(ProsimController.GetPaxPlanned());
            if (!prelimFlightData)
            {
                macZfw = ProsimController.GetZfwCG();
                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"MACZFW: {macZfw} %");

                prelimFlightData = true;
            }
        }
    }

    // Handle DEPARTURE state
    if (stateManager.IsDeparture())
    {
        // Get sim Zulu Time and send Prelim Loadsheet
        if (!prelimLoadsheet)
        {
            var simTime = SimConnect.ReadEnvVar("ZULU TIME", "Seconds");
            TimeSpan time = TimeSpan.FromSeconds(simTime);
            Logger.Log(LogLevel.Debug, "GsxController:RunServices", $"ZULU time - {simTime}");

            string flightNumber = ProsimController.GetFMSFlightNumber();

            if (Model.UseAcars && !string.IsNullOrEmpty(flightNumber))
            {
                var prelimLoadedData = ProsimController.GetLoadedData("prelim");
                try
                {
                    System.Threading.Tasks.Task task = acarsService.SendPreliminaryLoadsheetAsync(ProsimController.GetFMSFlightNumber(), prelimLoadedData);
                    prelimLoadsheet = true;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Debug, "GsxController:RunServices", $"Error Sending ACARS - {ex.Message}");
                }
            }
        }

        // Boarding & Refueling
        int refuelState = (int)SimConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
        int cateringState = (int)SimConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
        if (!refuelFinished || !boardFinished)
        {
            RunLoadingServices(refuelState, cateringState);
            return;
        }

        // Loadsheet & Ground-Equipment
        if (refuelFinished && boardFinished)
        {
            RunDEPARTUREServices();
            return;
        }
    }

    // Handle TAXIOUT -> FLIGHT transition
    if (stateManager.IsTaxiout() && !simOnGround)
    {
        stateManager.TransitionToFlight();
        return;
    }

    // Handle FLIGHT -> TAXIIN transition
    if (stateManager.IsFlight() && simOnGround)
    {
        stateManager.TransitionToTaxiin();
        return;
    }

    // Handle TAXIIN -> ARRIVAL transition
    int deboard_state = (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");
    if (stateManager.IsTaxiin() && SimConnect.ReadLvar("FSDT_VAR_EnginesStopped") == 1 && SimConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && SimConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 0)
    {
        RunArrivalServices(deboard_state);
        return;
    }

    // Handle ARRIVAL - Deboarding
    if (stateManager.IsArrival() && deboard_state >= 4)
    {
        RunDeboardingService(deboard_state);
    }
}

// Update RunDEPARTUREServices method to use stateManager
private void RunDEPARTUREServices()
{
    // ... existing code ...
    
    // Update state transition
    if (!pushFinished)
    {
        // ... existing code ...
    }
    else // DEPARTURE -> TAXIOUT
    {
        stateManager.TransitionToTaxiout();
        delay = 0;
        delayCounter = 0;
    }
}

// Update RunArrivalServices method to use stateManager
private void RunArrivalServices(int deboard_state)
{
    // ... existing code ...
    
    // Update state transition
    stateManager.TransitionToArrival();
    
    // ... existing code ...
}

// Update RunDeboardingService method to use stateManager
private void RunDeboardingService(int deboard_state)
{
    // ... existing code ...
    
    if (deboarding)
    {
        // ... existing code ...
        
        if (ProsimController.Deboarding(paxCurrent, (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT")) || deboard_state == 6 || deboard_state == 1)
        {
            deboarding = false;
            Logger.Log(LogLevel.Information, "GsxController:RunDeboardingService", $"Deboarding finished (GSX State {deboard_state})");
            ProsimController.DeboardingStop();
            
            // Update state transition
            stateManager.TransitionToTurnaround();
            
            Interval = 10000;
            return;
        }
    }
}

// Clean up resources
public void Dispose()
{
    // Unsubscribe from events
    if (stateManager != null)
    {
        stateManager.StateChanged -= OnStateChanged;
    }
    
    // ... other cleanup code ...
}
```

### 5. Update ServiceController.cs

Update the ServiceController class to initialize the new service:

```csharp
protected void InitializeServices()
{
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Initializing services...");
    
    // Step 1: Create FlightPlanService
    var flightPlanService = new FlightPlanService(Model);
    
    // Step 2: Create FlightPlan
    FlightPlan = new FlightPlan(Model, flightPlanService);
    
    // Step 3: Load flight plan
    if (!FlightPlan.Load())
    {
        Logger.Log(LogLevel.Warning, "ServiceController:InitializeServices", "Could not load flight plan, will retry in service loop");
    }
    
    // Step 4: Initialize FlightPlan in ProsimController
    ProsimController.InitializeFlightPlan(FlightPlan);
    
    // Step 5: Create AcarsService
    var acarsService = new AcarsService(Model.AcarsSecret, Model.AcarsNetworkUrl);
    
    // Step 6: Create GSX services
    var menuService = new GSXMenuService(Model, IPCManager.SimConnect);
    var audioService = new GSXAudioService(Model, IPCManager.SimConnect);
    var stateManager = new GSXStateManager();
    
    // Step 7: Create GsxController
    var gsxController = new GsxController(Model, ProsimController, FlightPlan, acarsService, menuService, audioService, stateManager);
    
    // Store the GsxController in IPCManager
    IPCManager.GsxController = gsxController;
    
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Services initialized successfully");
}
```

### 6. Add Unit Tests

Create unit tests for the new service in the Tests folder:

```csharp
[TestClass]
public class GSXStateManagerTests
{
    [TestMethod]
    public void Initialize_SetsStateToPreFlight()
    {
        // Arrange
        var stateManager = new GSXStateManager();
        
        // Act
        stateManager.Initialize();
        
        // Assert
        Assert.AreEqual(FlightState.PREFLIGHT, stateManager.CurrentState);
    }
    
    [TestMethod]
    public void UpdateState_TestArrival_TransitionsToFlight()
    {
        // Arrange
        var stateManager = new GSXStateManager();
        stateManager.Initialize();
        bool stateChanged = false;
        stateManager.StateChanged += (sender, e) => stateChanged = true;
        
        // Act
        bool result = stateManager.UpdateState(true, false, true, true, "123", true);
        
        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(stateChanged);
        Assert.AreEqual(FlightState.FLIGHT, stateManager.CurrentState);
    }
    
    // Add more tests for other state transitions
}
```

### 7. Test the Implementation

Test the implementation to ensure it works correctly.

## Benefits

1. **Improved Separation of Concerns**
   - State management is now handled by a dedicated service
   - State transitions are centralized and consistent
   - GsxController is simplified and more focused

2. **Enhanced Testability**
   - State transitions can be tested in isolation
   - Dependencies are explicit and can be mocked
   - Unit tests can be written for each state transition

3. **Better Maintainability**
   - Changes to state management can be made without affecting other parts of the system
   - Code is more organized and easier to understand
   - New states or transitions can be added without modifying GsxController

4. **Event-Based Communication**
   - Components can subscribe to state changes
   - Reduces tight coupling between components
   - Makes the system more extensible

## Next Steps

After implementing Phase 3.2, we'll proceed with Phase 3.3 to extract service coordination functionality into a dedicated GSXServiceCoordinator service.
