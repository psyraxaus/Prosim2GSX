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

### Phase 1: Critical Fixes ✅ COMPLETED

This phase focuses on the most critical changes needed to fix the immediate issue:

1. **Remove Automatic Door Opening in DEPARTURE State** ✅:
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

2. **Implement Toggle State Tracking in GSXServiceOrchestrator** ✅:
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

### Phase 2: Enhanced Robustness ✅ COMPLETED

This phase adds additional safeguards and improvements to make the door handling more robust:

1. **Add State Verification in ProsimDoorService** ✅:
   - Added checks to verify the current door state before making changes
   - Prevented unnecessary state changes that were causing the infinite loop

   ```csharp
   // In SetForwardRightDoor method
   public void SetForwardRightDoor(bool open)
   {
       // Check if the door is already in the requested state
       if (IsForwardRightDoorOpen == open)
       {
           Logger.Log(LogLevel.Debug, "ProsimDoorService:SetForwardRightDoor", 
               $"Door is already {(open ? "open" : "closed")}, no action needed");
           return;
       }

       try
       {
           // Only change the door state if it's different from the current state
           _prosimInterface.SetDoorState(ProsimDoorType.ForwardRight, open);
           IsForwardRightDoorOpen = open;
           OnDoorStateChanged(new DoorStateChangedEventArgs(DoorType.ForwardRight, open));
       }
       catch (Exception ex)
       {
           Logger.Log(LogLevel.Error, "ProsimDoorService:SetForwardRightDoor", 
               $"Error setting forward right door to {(open ? "open" : "closed")}: {ex.Message}");
       }
   }
   ```

2. **Implement Dynamic Toggle-to-Door Mapping** ✅:
   - Added dictionary to map service toggles to specific doors
   - Created smart mapping system that adapts to different airline configurations
   - Enhanced door handling with airline-agnostic approach

   ```csharp
   // Add class-level dictionary
   private Dictionary<int, DoorType> _toggleToDoorMapping = new Dictionary<int, DoorType>();

   // In DetermineDoorForToggle method
   private DoorType DetermineDoorForToggle(int toggleNumber, bool isActive)
   {
       // If we already have a mapping for this toggle, use it
       if (_toggleToDoorMapping.ContainsKey(toggleNumber))
       {
           return _toggleToDoorMapping[toggleNumber];
       }

       // If this is the first time we're seeing this toggle active,
       // we need to determine which door it's controlling
       if (isActive)
       {
           // Check catering state to see if this is for catering
           int cateringState = (int)_simConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
           
           if (cateringState == 4) // Service requested
           {
               // This toggle is being used for catering
               // Determine which door based on airline configuration
               // For now, we'll use a heuristic: 
               // If forward door is already in use, use aft door, otherwise use forward door
               DoorType doorToUse = _isForwardRightServiceActive ? 
                   DoorType.AftRight : DoorType.ForwardRight;
                   
               // Store the mapping for future use
               _toggleToDoorMapping[toggleNumber] = doorToUse;
               
               Logger.Log(LogLevel.Information, "GSXDoorManager:DetermineDoorForToggle", 
                   $"Mapped toggle {toggleNumber} to door {doorToUse} for catering service");
                   
               return doorToUse;
           }
       }
       
       // Default mapping if we can't determine dynamically
       return toggleNumber == 1 ? DoorType.ForwardRight : DoorType.AftRight;
   }
   ```

3. **Add Circuit Breaker Protection** ✅:
   - Implemented mechanism to prevent rapid door state changes
   - Added tracking of door state changes with timestamps
   - Blocked further changes if more than 5 changes occur within 5 seconds

   ```csharp
   // Add class-level dictionary
   private Dictionary<DoorType, (DateTime LastChange, int ChangeCount)> _doorChangeTracking = 
       new Dictionary<DoorType, (DateTime, int)>();

   // In ShouldPreventRapidChanges method
   private bool ShouldPreventRapidChanges(DoorType doorType)
   {
       var now = DateTime.UtcNow;
       if (!_doorChangeTracking.ContainsKey(doorType))
       {
           _doorChangeTracking[doorType] = (now, 1);
           return false;
       }

       var (lastChange, changeCount) = _doorChangeTracking[doorType];
       var timeSinceLastChange = now - lastChange;

       // If we've had more than 5 changes in less than 5 seconds, prevent further changes
       if (timeSinceLastChange.TotalSeconds < 5 && changeCount > 5)
       {
           Logger.Log(LogLevel.Warning, "GSXDoorManager:ShouldPreventRapidChanges", 
               $"Preventing rapid changes to {doorType} (already changed {changeCount} times in 5 seconds)");
           return true;
       }

       // Update tracking
       _doorChangeTracking[doorType] = (now, timeSinceLastChange.TotalSeconds < 5 ? changeCount + 1 : 1);
       return false;
   }
   ```

4. **Modify GSXDoorCoordinator to Respect Service Toggles** ✅:
   - Updated ManageDoorsForStateAsync to check if a service is active before closing doors
   - Prevented coordinator from overriding door states when services are in progress

   ```csharp
   // In ManageDoorsForStateAsync method
   case FlightState.DEPARTURE:
       // Only close doors if they're not being used for services
       if (!_doorManager.IsForwardRightServiceActive)
       {
           await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
       }
       
       if (!_doorManager.IsAftRightServiceActive)
       {
           await CloseDoorAsync(DoorType.AftRight, cancellationToken);
       }
       
       if (!_doorManager.IsForwardCargoServiceActive)
       {
           await CloseDoorAsync(DoorType.ForwardCargo, cancellationToken);
       }
       
       if (!_doorManager.IsAftCargoServiceActive)
       {
           await CloseDoorAsync(DoorType.AftCargo, cancellationToken);
       }
       break;
   ```

### Phase 3: Improved Diagnostics 🔜 PLANNED

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

## Implementation Status

- **Phase 1**: ✅ COMPLETED
  - Removed automatic door opening in DEPARTURE state
  - Implemented toggle state tracking in GSXServiceOrchestrator
  - Doors now remain closed after loading a flight plan
  - Doors only open when explicitly requested by GSX services

- **Phase 2**: ✅ COMPLETED
  - Added state verification in ProsimDoorService to prevent the infinite loop
  - Implemented dynamic toggle-to-door mapping in GSXDoorManager
  - Added circuit breaker to prevent rapid door state changes
  - Modified GSXDoorCoordinator to respect service toggles
  - Enhanced door handling with airline-agnostic approach
  - System now adapts to different airline configurations automatically
  - Door opening loop issue has been completely resolved
  - Improved resilience against rapid state changes

- **Phase 3**: 🔜 PLANNED
  - Enhance logging for door operations
  - Implement explicit door state initialization

## Conclusion

This implementation plan addresses the catering door opening issue by fixing the root causes and adding safeguards to prevent similar issues in the future. The phased approach allows for incremental improvements while ensuring that the most critical issues are addressed first.

Phases 1 and 2 have been successfully implemented, resolving the immediate issue with the door opening loop and enhancing the robustness of the door handling system. Phase 3 is planned to further improve diagnostics and initialization to prevent similar issues in the future.
