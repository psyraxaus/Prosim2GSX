# Phase 4.6: GSXPassengerCoordinator Implementation

## Overview

Phase 4.6 of the modularization strategy involves creating a domain-specific coordinator for passenger operations. The GSXPassengerCoordinator will coordinate passenger operations between the GSXServiceOrchestrator and ProsimPassengerService, managing passenger boarding and deboarding based on the current flight state.

This implementation follows the established patterns from previous phases, particularly Phase 4.4 (GSXDoorCoordinator) and Phase 4.5 (GSXEquipmentCoordinator).

## Implementation Details

### 1. Create PassengerStateChangedEventArgs Class

First, we created a dedicated event arguments class for passenger state changes:

```csharp
public class PassengerStateChangedEventArgs : EventArgs
{
    public string OperationType { get; }
    public int CurrentCount { get; }
    public int PlannedCount { get; }
    public DateTime Timestamp { get; }
    
    public PassengerStateChangedEventArgs(string operationType, int currentCount, int plannedCount)
    {
        OperationType = operationType;
        CurrentCount = currentCount;
        PlannedCount = plannedCount;
        Timestamp = DateTime.Now;
    }
}
```

### 2. Define IGSXPassengerCoordinator Interface

Next, we defined the interface for the passenger coordinator:

```csharp
public interface IGSXPassengerCoordinator : IDisposable
{
    event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
    
    int PassengersPlanned { get; }
    int PassengersCurrent { get; }
    bool IsBoardingInProgress { get; }
    bool IsDeboardingInProgress { get; }
    
    void Initialize();
    
    bool StartBoarding();
    bool StopBoarding();
    bool StartDeboarding();
    bool StopDeboarding();
    bool UpdatePassengerCount(int passengerCount);
    
    Task<bool> StartBoardingAsync(CancellationToken cancellationToken = default);
    Task<bool> StopBoardingAsync(CancellationToken cancellationToken = default);
    Task<bool> StartDeboardingAsync(CancellationToken cancellationToken = default);
    Task<bool> StopDeboardingAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdatePassengerCountAsync(int passengerCount, CancellationToken cancellationToken = default);
    
    Task SynchronizePassengerStatesAsync(CancellationToken cancellationToken = default);
    Task ManagePassengersForStateAsync(FlightState state, CancellationToken cancellationToken = default);
    
    void RegisterForStateChanges(IGSXStateManager stateManager);
}
```

### 3. Implement GSXPassengerCoordinator Class

Then, we implemented the GSXPassengerCoordinator class:

```csharp
public class GSXPassengerCoordinator : IGSXPassengerCoordinator
{
    private readonly IProsimPassengerService _prosimPassengerService;
    private readonly IGSXServiceOrchestrator _serviceOrchestrator;
    private readonly ILogger _logger;
    private IGSXStateManager _stateManager;
    private bool _disposed;
    
    // Passenger state tracking
    private int _passengersPlanned;
    private int _passengersCurrent;
    private bool _isBoardingInProgress;
    private bool _isDeboardingInProgress;
    
    public event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
    
    public int PassengersPlanned => _passengersPlanned;
    public int PassengersCurrent => _passengersCurrent;
    public bool IsBoardingInProgress => _isBoardingInProgress;
    public bool IsDeboardingInProgress => _isDeboardingInProgress;
    
    // Constructor and method implementations...
}
```

The implementation includes:
- Synchronous and asynchronous methods for boarding and deboarding operations
- State tracking for passenger counts and boarding/deboarding progress
- Event-based communication for passenger state changes
- State-based passenger management
- Comprehensive error handling and logging

### 4. Update ProsimController

We added a method to the ProsimController class to expose the passenger service:

```csharp
/// <summary>
/// Gets the passenger service
/// </summary>
/// <returns>The passenger service</returns>
public IProsimPassengerService GetPassengerService()
{
    return _passengerService;
}
```

### 5. Update ServiceController

We updated the ServiceController class to create and initialize the passenger coordinator:

```csharp
// Create passenger coordinator
var passengerCoordinator = new GSXPassengerCoordinator(
    ProsimController.GetPassengerService(), 
    serviceOrchestrator, 
    Logger.Instance);
passengerCoordinator.Initialize();
```

### 6. Update GSXControllerFacade

Finally, we updated the GSXControllerFacade class to use the new passenger coordinator:

```csharp
public GSXControllerFacade(
    ServiceModel model, 
    ProsimController prosimController, 
    FlightPlan flightPlan, 
    IAcarsService acarsService, 
    IGSXMenuService menuService, 
    IGSXAudioService audioService, 
    IGSXStateManager stateManager, 
    IGSXLoadsheetManager loadsheetManager, 
    IGSXDoorManager doorManager, 
    IGSXServiceOrchestrator serviceOrchestrator,
    IGSXDoorCoordinator doorCoordinator,
    IGSXEquipmentCoordinator equipmentCoordinator,
    IGSXPassengerCoordinator passengerCoordinator,
    ILogger logger)
{
    // Initialize fields and register for state changes
    _passengerCoordinator = passengerCoordinator;
    _passengerCoordinator.RegisterForStateChanges(_stateManager);
}
```

## Benefits

1. **Improved Separation of Concerns**
   - Passenger management logic is centralized in a dedicated coordinator
   - Clear responsibilities and boundaries
   - Reduced complexity in the GSXControllerFacade

2. **Enhanced Testability**
   - Passenger coordinator can be tested in isolation
   - Mock implementations can be used for testing
   - Unit tests can verify passenger management logic

3. **Better State Management**
   - Passenger states are managed based on flight state
   - Consistent passenger management across the application
   - Improved reliability of passenger operations

4. **Improved Error Handling**
   - Centralized error handling for passenger operations
   - Consistent logging and error recovery

## Next Steps

1. **Phase 4.7: Create GSXCargoCoordinator**
   - Create IGSXCargoCoordinator interface
   - Implement GSXCargoCoordinator class
   - Update GSXControllerFacade to use the new coordinator
   - Modify ServiceController to initialize the coordinator

2. **Phase 4.8: Create GSXFuelCoordinator**
   - Create IGSXFuelCoordinator interface
   - Implement GSXFuelCoordinator class
   - Update GSXControllerFacade to use the new coordinator
   - Modify ServiceController to initialize the coordinator

3. **Phase 4.9: Comprehensive Testing**
   - Create unit tests for all new coordinators
   - Create integration tests for coordinator interactions
   - Create performance tests for critical paths
