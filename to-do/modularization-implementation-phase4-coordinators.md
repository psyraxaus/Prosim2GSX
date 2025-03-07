# Domain-Specific Coordinators Implementation Strategy (Phases 4.4-4.8)

## Overview

Phases 4.4-4.8 of the modularization strategy involve creating domain-specific coordinators for various aspects of the GSX integration. These coordinators serve as mediators between GSX and ProSim services, coordinating operations and state tracking for specific domains.

This document provides a comprehensive overview of the implementation strategy for all five domain-specific coordinators:

1. **GSXDoorCoordinator (Phase 4.4)** - Coordinates door operations
2. **GSXEquipmentCoordinator (Phase 4.5)** - Coordinates ground equipment operations
3. **GSXPassengerCoordinator (Phase 4.6)** - Coordinates passenger boarding/deboarding
4. **GSXCargoCoordinator (Phase 4.7)** - Coordinates cargo loading/unloading
5. **GSXFuelCoordinator (Phase 4.8)** - Coordinates refueling operations

## Common Implementation Pattern

All five coordinators follow a consistent implementation pattern:

1. **Interface Definition**:
   - Define a clear interface with methods for operations and state tracking
   - Include event declarations for state changes
   - Follow Interface Segregation Principle for focused interfaces
   - Implement IDisposable for proper resource cleanup

2. **Implementation Class**:
   - Implement the interface with proper dependency injection
   - Coordinate between GSX and ProSim services
   - Manage state tracking with thread-safe operations
   - Provide event-based communication
   - Include comprehensive error handling and logging
   - Implement proper resource cleanup in Dispose method

3. **State-Based Management**:
   - Implement methods to manage domain-specific operations based on flight state
   - Register for state change notifications from GSXStateManager
   - React to state changes with appropriate domain-specific actions

4. **Asynchronous Operations**:
   - Provide both synchronous and asynchronous methods
   - Support cancellation through CancellationToken
   - Implement proper error handling for asynchronous operations

5. **Integration with GSXControllerFacade**:
   - Update GSXControllerFacade to use the new coordinator
   - Register the coordinator for state changes
   - Dispose the coordinator properly

6. **Integration with ServiceController**:
   - Update ServiceController to create and initialize the coordinator
   - Register the coordinator with the service provider

## Coordinator-Specific Details

### 1. GSXDoorCoordinator (Phase 4.4)

**Purpose**: Coordinates door operations between GSX and ProSim.

**Key Components**:
- Interface: `IGSXDoorCoordinator`
- Implementation: `GSXDoorCoordinator`
- Dependencies: `IGSXDoorManager`, `IProsimDoorService`

**Key Functionality**:
- Open/close doors in both GSX and ProSim
- Track door states
- Synchronize door states between GSX and ProSim
- Manage doors based on flight state

**State-Based Door Management**:
- PREFLIGHT: Close passenger doors initially
- DEPARTURE: Open passenger and cargo doors for boarding/loading
- TAXIOUT: Close all doors
- FLIGHT: Ensure all doors remain closed
- TAXIIN: Ensure all doors remain closed
- ARRIVAL: Open passenger and cargo doors for deboarding/unloading
- TURNAROUND: Open passenger and cargo doors

**Implementation Details**: See `to-do/modularization-implementation-phase4.4.md`

### 2. GSXEquipmentCoordinator (Phase 4.5)

**Purpose**: Coordinates ground equipment operations between GSX and ProSim.

**Key Components**:
- Interface: `IGSXEquipmentCoordinator`
- Implementation: `GSXEquipmentCoordinator`
- Dependencies: `IProsimEquipmentService`

**Key Functionality**:
- Connect/disconnect ground equipment (GPU, PCA, chocks, jetway)
- Track equipment states
- Manage equipment based on flight state

**State-Based Equipment Management**:
- PREFLIGHT: Connect all ground equipment
- DEPARTURE: Keep ground equipment connected
- TAXIOUT: Disconnect all ground equipment
- FLIGHT: Ensure all ground equipment is disconnected
- TAXIIN: Ensure all ground equipment is disconnected
- ARRIVAL: Connect all ground equipment
- TURNAROUND: Keep ground equipment connected

**Implementation Details**: See `to-do/modularization-implementation-phase4.5.md`

### 3. GSXPassengerCoordinator (Phase 4.6)

**Purpose**: Coordinates passenger boarding/deboarding operations between GSX and ProSim.

**Key Components**:
- Interface: `IGSXPassengerCoordinator`
- Implementation: `GSXPassengerCoordinator`
- Dependencies: `IGSXServiceOrchestrator`, `IProsimPassengerService`

**Key Functionality**:
- Start/stop passenger boarding and deboarding
- Track passenger counts and boarding/deboarding progress
- Synchronize passenger data between GSX and ProSim
- Manage passenger operations based on flight state

**State-Based Passenger Management**:
- PREFLIGHT: Initialize passenger counts
- DEPARTURE: Start boarding process
- TAXIOUT: Ensure boarding is complete
- FLIGHT: No passenger operations
- TAXIIN: No passenger operations
- ARRIVAL: Start deboarding process
- TURNAROUND: Ensure deboarding is complete

**Implementation Strategy**:

1. **Interface Definition**:
```csharp
public interface IGSXPassengerCoordinator : IDisposable
{
    event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
    event EventHandler<BoardingProgressChangedEventArgs> BoardingProgressChanged;
    event EventHandler<BoardingProgressChangedEventArgs> DeboardingProgressChanged;
    
    BoardingState BoardingState { get; }
    PassengerCounts PassengerCounts { get; }
    int BoardingProgressPercentage { get; }
    int DeboardingProgressPercentage { get; }
    
    void Initialize();
    bool StartBoarding();
    void StopBoarding();
    bool StartDeboarding();
    void StopDeboarding();
    Task<bool> StartBoardingAsync(CancellationToken cancellationToken = default);
    Task<bool> StartDeboardingAsync(CancellationToken cancellationToken = default);
    Task SynchronizePassengerCountsAsync(CancellationToken cancellationToken = default);
    Task ManagePassengersForStateAsync(FlightState state, CancellationToken cancellationToken = default);
    void RegisterForStateChanges(IGSXStateManager stateManager);
}
```

2. **Implementation Class**:
   - Implement the interface with proper dependency injection
   - Coordinate between GSXServiceOrchestrator and ProsimPassengerService
   - Track passenger counts and boarding/deboarding progress
   - Provide event-based communication for passenger state changes
   - Implement state-based passenger management

3. **Integration with GSXControllerFacade and ServiceController**:
   - Update GSXControllerFacade to use the new coordinator
   - Update ServiceController to create and initialize the coordinator

### 4. GSXCargoCoordinator (Phase 4.7)

**Purpose**: Coordinates cargo loading/unloading operations between GSX and ProSim.

**Key Components**:
- Interface: `IGSXCargoCoordinator`
- Implementation: `GSXCargoCoordinator`
- Dependencies: `IGSXServiceOrchestrator`, `IProsimCargoService`

**Key Functionality**:
- Start/stop cargo loading and unloading
- Track cargo weights and loading/unloading progress
- Synchronize cargo data between GSX and ProSim
- Manage cargo operations based on flight state

**State-Based Cargo Management**:
- PREFLIGHT: Initialize cargo weights
- DEPARTURE: Start cargo loading process
- TAXIOUT: Ensure cargo loading is complete
- FLIGHT: No cargo operations
- TAXIIN: No cargo operations
- ARRIVAL: Start cargo unloading process
- TURNAROUND: Ensure cargo unloading is complete

**Implementation Strategy**:

1. **Interface Definition**:
```csharp
public interface IGSXCargoCoordinator : IDisposable
{
    event EventHandler<CargoStateChangedEventArgs> CargoStateChanged;
    event EventHandler<CargoProgressChangedEventArgs> LoadingProgressChanged;
    event EventHandler<CargoProgressChangedEventArgs> UnloadingProgressChanged;
    
    CargoLoadingState LoadingState { get; }
    CargoWeights CargoWeights { get; }
    int LoadingProgressPercentage { get; }
    int UnloadingProgressPercentage { get; }
    
    void Initialize();
    bool StartLoading();
    void StopLoading();
    bool StartUnloading();
    void StopUnloading();
    Task<bool> StartLoadingAsync(CancellationToken cancellationToken = default);
    Task<bool> StartUnloadingAsync(CancellationToken cancellationToken = default);
    Task SynchronizeCargoWeightsAsync(CancellationToken cancellationToken = default);
    Task ManageCargoForStateAsync(FlightState state, CancellationToken cancellationToken = default);
    void RegisterForStateChanges(IGSXStateManager stateManager);
}
```

2. **Implementation Class**:
   - Implement the interface with proper dependency injection
   - Coordinate between GSXServiceOrchestrator and ProsimCargoService
   - Track cargo weights and loading/unloading progress
   - Provide event-based communication for cargo state changes
   - Implement state-based cargo management

3. **Integration with GSXControllerFacade and ServiceController**:
   - Update GSXControllerFacade to use the new coordinator
   - Update ServiceController to create and initialize the coordinator

### 5. GSXFuelCoordinator (Phase 4.8)

**Purpose**: Coordinates refueling operations between GSX and ProSim.

**Key Components**:
- Interface: `IGSXFuelCoordinator`
- Implementation: `GSXFuelCoordinator`
- Dependencies: `IGSXServiceOrchestrator`, `IProsimFuelService`

**Key Functionality**:
- Start/stop refueling and defueling
- Track fuel quantities and refueling progress
- Calculate required fuel based on flight plan
- Synchronize fuel data between GSX and ProSim
- Manage fuel operations based on flight state

**State-Based Fuel Management**:
- PREFLIGHT: Initialize fuel quantities
- DEPARTURE: Start refueling process
- TAXIOUT: Ensure refueling is complete
- FLIGHT: No fuel operations
- TAXIIN: No fuel operations
- ARRIVAL: No fuel operations
- TURNAROUND: Calculate required fuel for next flight

**Implementation Strategy**:

1. **Interface Definition**:
```csharp
public interface IGSXFuelCoordinator : IDisposable
{
    event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;
    event EventHandler<RefuelingProgressChangedEventArgs> RefuelingProgressChanged;
    
    RefuelingState RefuelingState { get; }
    FuelQuantities FuelQuantities { get; }
    int RefuelingProgressPercentage { get; }
    float FuelRateKGS { get; }
    
    void Initialize();
    bool StartRefueling();
    void StopRefueling();
    bool StartDefueling();
    void StopDefueling();
    Task<bool> StartRefuelingAsync(CancellationToken cancellationToken = default);
    Task<bool> StartDefuelingAsync(CancellationToken cancellationToken = default);
    Task SynchronizeFuelQuantitiesAsync(CancellationToken cancellationToken = default);
    Task<FuelQuantities> CalculateRequiredFuelAsync(CancellationToken cancellationToken = default);
    Task ManageFuelForStateAsync(FlightState state, CancellationToken cancellationToken = default);
    void RegisterForStateChanges(IGSXStateManager stateManager);
}
```

2. **Implementation Class**:
   - Implement the interface with proper dependency injection
   - Coordinate between GSXServiceOrchestrator and ProsimFuelService
   - Track fuel quantities and refueling progress
   - Provide event-based communication for fuel state changes
   - Implement state-based fuel management

3. **Integration with GSXControllerFacade and ServiceController**:
   - Update GSXControllerFacade to use the new coordinator
   - Update ServiceController to create and initialize the coordinator

## Testing Strategy

### Unit Tests

Create unit tests for each coordinator to verify its functionality:

1. Test basic operations (e.g., opening/closing doors, connecting/disconnecting equipment)
2. Test state-based management
3. Test event handling for state changes
4. Test error handling and recovery
5. Test synchronization between GSX and ProSim

### Integration Tests

Create integration tests to verify the interaction between coordinators and other components:

1. Test interaction with GSX services
2. Test interaction with ProSim services
3. Test interaction with GSXStateManager
4. Test interaction with GSXControllerFacade

## Implementation Timeline

| Phase | Coordinator | Estimated Time | Dependencies |
|-------|-------------|----------------|--------------|
| 4.4   | GSXDoorCoordinator | 3-4 days | IGSXDoorManager, IProsimDoorService |
| 4.5   | GSXEquipmentCoordinator | 3-4 days | IProsimEquipmentService |
| 4.6   | GSXPassengerCoordinator | 4-5 days | IGSXServiceOrchestrator, IProsimPassengerService |
| 4.7   | GSXCargoCoordinator | 3-4 days | IGSXServiceOrchestrator, IProsimCargoService |
| 4.8   | GSXFuelCoordinator | 3-4 days | IGSXServiceOrchestrator, IProsimFuelService |

Total estimated time: 16-21 days for all coordinators

## Benefits

1. **Improved Separation of Concerns**
   - Each coordinator focuses on a specific domain
   - Clear responsibilities and boundaries
   - Reduced complexity in the GSXControllerFacade

2. **Enhanced Testability**
   - Coordinators can be tested in isolation
   - Mock implementations can be used for testing
   - Unit tests can verify domain-specific logic

3. **Better State Management**
   - Domain-specific operations are managed based on flight state
   - Consistent behavior across the application
   - Improved reliability of operations

4. **Improved Error Handling**
   - Centralized error handling for domain-specific operations
   - Consistent logging and recovery strategies

5. **Enhanced Event Communication**
   - Domain-specific events for state changes
   - Improved communication between components
   - Better decoupling of event producers and consumers

## Conclusion

The implementation of domain-specific coordinators in Phases 4.4-4.8 will significantly improve the architecture of Prosim2GSX by breaking down the monolithic GSXController into smaller, more focused components. This modularization will enhance maintainability, testability, and extensibility, making it easier to add new features, fix bugs, and maintain the codebase over time.

Each coordinator follows a consistent implementation pattern, making the codebase more uniform and easier to understand. The state-based management approach ensures that domain-specific operations are handled appropriately based on the current flight state, improving the reliability and predictability of the application.

The detailed implementation plans for each coordinator provide a clear roadmap for completing Phases 4.4-4.8, with estimated timelines and dependencies. Following these plans will ensure a smooth and successful implementation of the domain-specific coordinators.
