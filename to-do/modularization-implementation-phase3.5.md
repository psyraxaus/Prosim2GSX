# Phase 3.5: GSXDoorManager Implementation

## Overview

Phase 3.5 of the modularization strategy involved implementing the GSXDoorManager service, which is responsible for managing aircraft doors in GSX. This service extracts door management logic from the GsxController, providing a clean separation of concerns and improving maintainability and testability.

## Implementation Details

### 1. Interface Definition

Created the `IGSXDoorManager` interface with the following components:

- **Door State Properties**:
  - `IsForwardRightDoorOpen`: Gets whether the forward right door is open
  - `IsAftRightDoorOpen`: Gets whether the aft right door is open
  - `IsForwardCargoDoorOpen`: Gets whether the forward cargo door is open
  - `IsAftCargoDoorOpen`: Gets whether the aft cargo door is open

- **Synchronous Methods**:
  - `Initialize()`: Initializes the door manager
  - `OpenDoor(DoorType)`: Opens a door
  - `CloseDoor(DoorType)`: Closes a door
  - `HandleServiceToggle(int, bool)`: Handles a service toggle from GSX
  - `HandleCargoLoading(int)`: Handles cargo loading percentage updates

- **Asynchronous Methods**:
  - `OpenDoorAsync(DoorType, CancellationToken)`: Opens a door asynchronously
  - `CloseDoorAsync(DoorType, CancellationToken)`: Closes a door asynchronously
  - `HandleServiceToggleAsync(int, bool, CancellationToken)`: Handles a service toggle from GSX asynchronously
  - `HandleCargoLoadingAsync(int, CancellationToken)`: Handles cargo loading percentage updates asynchronously

- **Events**:
  - `DoorStateChanged`: Occurs when a door state changes

- **Supporting Types**:
  - `DoorType` enum: Defines the types of doors available on the aircraft
  - `DoorStateChangedEventArgs` class: Event arguments for door state changes

### 2. Implementation

Implemented the `GSXDoorManager` class with the following features:

- **Thread Safety**:
  - Used a lock object for critical sections
  - Implemented thread-safe event raising
  - Ensured state consistency across threads

- **Error Handling**:
  - Added comprehensive try-catch blocks
  - Implemented detailed logging
  - Provided fallback behavior when possible

- **Integration with ProsimDoorService**:
  - Used the ProsimDoorService for actual door operations
  - Subscribed to door state changes from ProsimDoorService
  - Synchronized door states between GSX and ProSim

- **SimConnect Integration**:
  - Subscribed to relevant SimConnect variables
  - Handled service toggles from GSX
  - Monitored cargo loading percentage

- **Configuration Support**:
  - Respected user settings from ServiceModel
  - Implemented conditional door operations based on settings
  - Added logging for configuration-related decisions

### 3. GsxController Integration

Updated the GsxController to use the new GSXDoorManager service:

- **Dependency Injection**:
  - Added IGSXDoorManager parameter to the constructor
  - Stored the service as a private readonly field
  - Added null check with ArgumentNullException

- **Event Subscription**:
  - Subscribed to the DoorStateChanged event
  - Updated local state variables in the event handler
  - Added logging for door state changes

- **Method Delegation**:
  - Replaced direct door manipulation code with calls to the door manager
  - Updated RunLoadingServices method to use the door manager
  - Delegated service toggle handling to the door manager
  - Delegated cargo loading handling to the door manager

- **Cleanup**:
  - Updated Dispose method to unsubscribe from the DoorStateChanged event
  - Maintained backward compatibility with existing code

### 4. ServiceController Integration

Updated the ServiceController to create and initialize the GSXDoorManager:

- **Service Creation**:
  - Added GSXDoorManager creation in the InitializeServices method
  - Used ProsimController.GetDoorService() to get the door service
  - Passed the ServiceModel to the GSXDoorManager constructor

- **Service Initialization**:
  - Called Initialize() on the door manager
  - Added the door manager to the GsxController constructor

### 5. ProsimController Enhancement

Added a method to expose the door service:

- **GetDoorService Method**:
  - Added public GetDoorService() method to ProsimController
  - Returns the private _doorService field
  - Added XML documentation

## Benefits

1. **Improved Separation of Concerns**:
   - Door management logic is now isolated in a dedicated service
   - GsxController is simplified and more focused on coordination
   - Clear responsibilities for each component

2. **Enhanced Testability**:
   - Door management can be tested in isolation
   - Mock implementations can be used for testing
   - Reduced dependencies in GsxController

3. **Better Error Handling**:
   - Centralized error handling for door operations
   - Consistent logging and recovery strategies
   - Improved resilience to failures

4. **Thread Safety**:
   - Proper synchronization for door state access
   - Thread-safe event raising
   - Consistent state across threads

5. **Improved Maintainability**:
   - Smaller, focused components are easier to understand and modify
   - Clear separation of concerns reduces side effects
   - Better organization makes code navigation easier

## Next Steps

1. **Phase 3.4: Implement GSXServiceCoordinator**:
   - Create IGSXServiceCoordinator interface and implementation
   - Extract service coordination logic from GsxController
   - Add methods for running various GSX services
   - Add event-based communication for service status changes

2. **Phase 3.7: Refine GsxController**:
   - Refactor GsxController to be a thin facade
   - Delegate responsibilities to specialized services
   - Improve event handling and state management
   - Enhance error handling and logging

3. **Comprehensive Testing**:
   - Create unit tests for GSXDoorManager
   - Test door operations in isolation
   - Test integration with GsxController
   - Verify all door-related workflows
