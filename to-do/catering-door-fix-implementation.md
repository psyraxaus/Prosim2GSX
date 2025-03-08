# Catering Door Fix Implementation Plan

## Issue Description

The current implementation has an issue where after a flight plan is loaded into the MCDU, the forward right passenger door is being opened immediately and going into a loop. The expected behavior is that doors should remain closed until the catering service specifically requests the door to be opened.

## Root Cause Analysis

The issue stems from multiple architectural components interacting in unexpected ways:

1. **Conflicting Door Management Systems**:
   - We have three layers managing doors: `GSXDoorManager`, `GSXDoorCoordinator`, and `GSXServiceCoordinator`
   - The `GSXDoorCoordinator.ManageDoorsForStateAsync()` method is automatically opening doors based on flight state
   - This conflicts with the reactive door handling in `GSXDoorManager.HandleServiceToggle()`

2. **Automatic State-Based Door Management**:
   - In `GSXDoorCoordinator.ManageDoorsForStateAsync()`, there's code that automatically opens doors in DEPARTURE state:
   ```csharp
   case FlightState.DEPARTURE:
       // In departure, passenger doors should be open for boarding
       await OpenDoorAsync(DoorType.ForwardRight, cancellationToken);
       await OpenDoorAsync(DoorType.AftRight, cancellationToken);
       // ...
   ```
   - This is causing doors to open immediately when transitioning to DEPARTURE state after loading a flight plan

3. **Missing Toggle State Tracking**:
   - In `GSXServiceOrchestrator.CheckAllDoorToggles()`, there's no tracking of previous toggle states
   - This causes repeated calls to `HandleServiceToggle()` as long as the toggle is active

4. **Redundant Door State Variables**:
   - `GSXServiceCoordinator` maintains its own door state variables that duplicate those in `GSXDoorManager`
   - This creates potential for state inconsistencies

## Implementation Plan

The implementation will be divided into three phases to address the issue systematically:

### Phase 1: Critical Fixes

This phase focuses on the most critical changes needed to fix the immediate issue:

1. **Remove Automatic Door Opening in DEPARTURE State**:
   - Modify `GSXDoorCoordinator.ManageDoorsForStateAsync()` to remove automatic door opening
   - Replace with code that ensures doors are closed initially and wait for GSX requests

   ```csharp
   case FlightState.DEPARTURE:
       // Do NOT automatically open doors
       // Instead, ensure doors are closed initially and wait for GSX requests
       await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
       await CloseDoorAsync(DoorType.AftRight, cancellationToken);
       // Cargo doors should also remain closed until explicitly requested
       await CloseDoorAsync(DoorType.ForwardCargo, cancellationToken);
       await CloseDoorAsync(DoorType.AftCargo, cancellationToken);
       break;
   ```

2. **Implement Toggle State Tracking in GSXServiceOrchestrator**:
   - Add class-level variables to track previous toggle states
   - Modify `CheckAllDoorToggles()` to only process toggle changes when the value actually changes

   ```csharp
   // Add class-level variables
   private bool _previousService1Toggle = false;
   private bool _previousService2Toggle = false;
   private bool _previousCargo1Toggle = false;
   private bool _previousCargo2Toggle = false;

   // In CheckAllDoorToggles method
   bool service1Toggle = _simConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE") == 1;
   if (service1Toggle != _previousService1Toggle)
   {
       Logger.Log(LogLevel.Debug, "GSXServiceOrchestrator:CheckAllDoorToggles", 
           $"Service 1 toggle changed from {_previousService1Toggle} to {service1Toggle}");
       doorManager.HandleServiceToggle(1, service1Toggle);
       _previousService1Toggle = service1Toggle;
   }

   bool service2Toggle = _simConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE") == 1;
   if (service2Toggle != _previousService2Toggle)
   {
       Logger.Log(LogLevel.Debug, "GSXServiceOrchestrator:CheckAllDoorToggles", 
           $"Service 2 toggle changed from {_previousService2Toggle} to {service2Toggle}");
       doorManager.HandleServiceToggle(2, service2Toggle);
       _previousService2Toggle = service2Toggle;
   }

   // Similar changes for cargo door toggles
   ```

### Phase 2: Enhanced Robustness

This phase adds additional safeguards and improvements to make the door handling more robust:

1. **Add Flight State Awareness to Door Operations**:
   - Modify `GSXDoorManager.HandleServiceToggle()` to check the current flight state
   - Only allow door operations in appropriate flight states

   ```csharp
   // Add a reference to IGSXStateManager
   private readonly IGSXStateManager _stateManager;

   // In constructor
   public GSXDoorManager(IProsimDoorService prosimDoorService, ServiceModel model, IGSXStateManager stateManager)
   {
       _prosimDoorService = prosimDoorService ?? throw new ArgumentNullException(nameof(prosimDoorService));
       _model = model ?? throw new ArgumentNullException(nameof(model));
       _stateManager = stateManager; // Can be null, handle accordingly
       _simConnect = IPCManager.SimConnect;

       // Subscribe to door state changes from ProSim
       _prosimDoorService.DoorStateChanged += OnProsimDoorStateChanged;

       Logger.Log(LogLevel.Information, "GSXDoorManager:Constructor", "GSX Door Manager initialized");
   }

   // In HandleServiceToggle method
   if (_stateManager != null && 
       _stateManager.CurrentState != FlightState.DEPARTURE && 
       _stateManager.CurrentState != FlightState.ARRIVAL && 
       _stateManager.CurrentState != FlightState.TURNAROUND)
   {
       Logger.Log(LogLevel.Information, "GSXDoorManager:HandleServiceToggle", 
           $"Ignoring service toggle in inappropriate flight state: {_stateManager.CurrentState}");
       return;
   }
   ```

2. **Remove Redundant Door State Variables**:
   - Remove the redundant door state tracking variables from `GSXServiceCoordinator`
   - Update any code that references these variables to use the `GSXDoorManager` properties instead

   ```csharp
   // Remove these variables from GSXServiceCoordinator
   private bool aftCargoDoorOpened = false;
   private bool aftRightDoorOpened = false;
   private bool forwardRightDoorOpened = false;
   private bool forwardCargoDoorOpened = false;

   // Update OnDoorStateChanged method to not update these variables
   private void OnDoorStateChanged(object sender, DoorStateChangedEventArgs e)
   {
       // Just log and forward the event, don't update local state
       Logger.Log(LogLevel.Information, "GSXServiceCoordinator:OnDoorStateChanged", 
           $"Door {e.DoorType} state changed to {(e.IsOpen ? "open" : "closed")}");
       OnServiceStatusChanged("Door", $"{e.DoorType} door {(e.IsOpen ? "opened" : "closed")}", true);
   }
   ```

3. **Implement Debounce Logic for Toggle Changes**:
   - Add debounce mechanism to prevent rapid toggle changes
   - This helps avoid potential race conditions or unintended door operations

   ```csharp
   // Add class-level variables
   private DateTime _lastService1ToggleTime = DateTime.MinValue;
   private DateTime _lastService2ToggleTime = DateTime.MinValue;
   private DateTime _lastCargo1ToggleTime = DateTime.MinValue;
   private DateTime _lastCargo2ToggleTime = DateTime.MinValue;
   private TimeSpan _toggleDebounceTime = TimeSpan.FromSeconds(2);

   // In CheckAllDoorToggles method
   bool service1Toggle = _simConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE") == 1;
   if (service1Toggle != _previousService1Toggle && 
       (DateTime.Now - _lastService1ToggleTime) > _toggleDebounceTime)
   {
       Logger.Log(LogLevel.Debug, "GSXServiceOrchestrator:CheckAllDoorToggles", 
           $"Service 1 toggle changed from {_previousService1Toggle} to {service1Toggle}");
       doorManager.HandleServiceToggle(1, service1Toggle);
       _previousService1Toggle = service1Toggle;
       _lastService1ToggleTime = DateTime.Now;
   }

   // Similar changes for other toggles
   ```

### Phase 3: Improved Diagnostics

This phase focuses on improving logging and diagnostics to make it easier to troubleshoot door-related issues:

1. **Enhance Logging for Door Operations**:
   - Add more detailed logging to track door operations and toggle states
   - This will make it easier to diagnose issues in the future

   ```csharp
   // In GSXServiceOrchestrator.CheckAllDoorToggles
   Logger.Log(LogLevel.Debug, "GSXServiceOrchestrator:CheckAllDoorToggles", 
       $"Door toggle states - Service1: {service1Toggle} (prev: {_previousService1Toggle}), " +
       $"Service2: {service2Toggle} (prev: {_previousService2Toggle}), " +
       $"Cargo1: {cargo1Toggle} (prev: {_previousCargo1Toggle}), " +
       $"Cargo2: {cargo2Toggle} (prev: {_previousCargo2Toggle})");
   ```

2. **Implement Explicit Door State Initialization**:
   - Add explicit initialization of door states during startup
   - This ensures a consistent starting state for all door-related operations

   ```csharp
   // In GSXDoorManager.Initialize
   _isForwardRightDoorOpen = false;
   _isAftRightDoorOpen = false;
   _isForwardCargoDoorOpen = false;
   _isAftCargoDoorOpen = false;
   _isForwardRightServiceActive = false;
   _isAftRightServiceActive = false;
   _isForwardCargoServiceActive = false;
   _isAftCargoServiceActive = false;

   // Ensure ProSim knows the doors are closed
   _prosimDoorService.SetForwardRightDoor(false);
   _prosimDoorService.SetAftRightDoor(false);
   _prosimDoorService.SetForwardCargoDoor(false);
   _prosimDoorService.SetAftCargoDoor(false);

   Logger.Log(LogLevel.Information, "GSXDoorManager:Initialize", 
       "Door states explicitly initialized to closed");
   ```

## Testing Strategy

After implementing each phase, the following tests should be performed:

1. **Basic Functionality Test**:
   - Load a flight plan into the MCDU
   - Verify that doors remain closed
   - Verify that doors only open when explicitly requested by GSX

2. **Toggle Response Test**:
   - Manually trigger door toggles in GSX
   - Verify that doors respond correctly to toggle changes
   - Verify that doors don't open/close in a loop

3. **Flight State Transition Test**:
   - Test door behavior during transitions between different flight states
   - Verify that doors behave correctly in each state

4. **Edge Case Tests**:
   - Test rapid toggle changes to verify debounce logic
   - Test door operations in inappropriate flight states
   - Test door operations during connection/disconnection events

## Conclusion

This implementation plan addresses the catering door opening issue by fixing the root causes and adding safeguards to prevent similar issues in the future. The phased approach allows for incremental improvements while ensuring that the most critical issues are addressed first.
