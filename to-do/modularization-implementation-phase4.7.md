# Phase 4.7: GSXCargoCoordinator Implementation

## Overview

Phase 4.7 of the modularization strategy involves creating a domain-specific coordinator for cargo operations. The GSXCargoCoordinator coordinates cargo operations between the GSXServiceOrchestrator and ProsimCargoService, managing cargo loading and unloading based on the current flight state.

This implementation follows the established patterns from previous phases, particularly Phase 4.6 (GSXPassengerCoordinator), with cargo-specific considerations.

## Implementation Details

### 1. Create CargoStateChangedEventArgs Class

First, we created a dedicated event arguments class for cargo state changes:

```csharp
public class CargoStateChangedEventArgs : EventArgs
{
    public string OperationType { get; }
    public int CurrentPercentage { get; }
    public int PlannedAmount { get; }
    public DateTime Timestamp { get; }
    
    public CargoStateChangedEventArgs(string operationType, int currentPercentage, int plannedAmount)
    {
        OperationType = operationType;
        CurrentPercentage = currentPercentage;
        PlannedAmount = plannedAmount;
        Timestamp = DateTime.Now;
    }
}
```

### 2. Define IGSXCargoCoordinator Interface

Next, we defined the interface for the cargo coordinator:

```csharp
public interface IGSXCargoCoordinator : IDisposable
{
    event EventHandler<CargoStateChangedEventArgs> CargoStateChanged;
    
    int CargoPlanned { get; }
    int CargoCurrentPercentage { get; }
    bool IsLoadingInProgress { get; }
    bool IsUnloadingInProgress { get; }
    
    void Initialize();
    
    bool StartLoading();
    bool StopLoading();
    bool StartUnloading();
    bool StopUnloading();
    bool UpdateCargoAmount(int cargoAmount);
    bool ChangeCargoPercentage(int percentage);
    
    Task<bool> StartLoadingAsync(CancellationToken cancellationToken = default);
    Task<bool> StopLoadingAsync(CancellationToken cancellationToken = default);
    Task<bool> StartUnloadingAsync(CancellationToken cancellationToken = default);
    Task<bool> StopUnloadingAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateCargoAmountAsync(int cargoAmount, CancellationToken cancellationToken = default);
    Task<bool> ChangeCargoPercentageAsync(int percentage, CancellationToken cancellationToken = default);
    
    Task SynchronizeCargoStatesAsync(CancellationToken cancellationToken = default);
    Task ManageCargoForStateAsync(FlightState state, CancellationToken cancellationToken = default);
    Task CoordinateWithDoorsAsync(bool forLoading, CancellationToken cancellationToken = default);
    
    void RegisterForStateChanges(IGSXStateManager stateManager);
    void RegisterDoorCoordinator(IGSXDoorCoordinator doorCoordinator);
}
```

### 3. Implement GSXCargoCoordinator Class

Then, we implemented the GSXCargoCoordinator class:

```csharp
public class GSXCargoCoordinator : IGSXCargoCoordinator
{
    private readonly IProsimCargoService _prosimCargoService;
    private readonly IGSXServiceOrchestrator _serviceOrchestrator;
    private readonly ILogger _logger;
    private IGSXStateManager _stateManager;
    private IGSXDoorCoordinator _doorCoordinator;
    private bool _disposed;
    
    // Cargo state tracking
    private int _cargoPlanned;
    private int _cargoCurrentPercentage;
    private bool _isLoadingInProgress;
    private bool _isUnloadingInProgress;
    private readonly object _stateLock = new object();
    
    public event EventHandler<CargoStateChangedEventArgs> CargoStateChanged;
    
    public int CargoPlanned => _cargoPlanned;
    public int CargoCurrentPercentage => _cargoCurrentPercentage;
    public bool IsLoadingInProgress => _isLoadingInProgress;
    public bool IsUnloadingInProgress => _isUnloadingInProgress;
    
    // Constructor and method implementations...
}
```

The implementation includes:
- Synchronous and asynchronous methods for cargo loading and unloading operations
- State tracking for cargo amounts and loading/unloading progress
- Event-based communication for cargo state changes
- State-based cargo management
- Comprehensive error handling and logging
- Door coordination for cargo operations

### 4. Update ProsimController

We added a method to the ProsimController class to expose the cargo service:

```csharp
/// <summary>
/// Gets the cargo service
/// </summary>
/// <returns>The cargo service</returns>
public IProsimCargoService GetCargoService()
{
    return _cargoService;
}
```

### 5. Update ServiceController

We updated the ServiceController class to create and initialize the cargo coordinator:

```csharp
// Create cargo coordinator
var cargoCoordinator = new GSXCargoCoordinator(
    ProsimController.GetCargoService(),
    serviceOrchestrator,
    Logger.Instance);
cargoCoordinator.Initialize();
cargoCoordinator.RegisterDoorCoordinator(doorCoordinator);
```

### 6. Update GSXControllerFacade

Finally, we updated the GSXControllerFacade class to use the new cargo coordinator:

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
    IGSXCargoCoordinator cargoCoordinator,
    ILogger logger)
{
    // Initialize fields and register for state changes
    _cargoCoordinator = cargoCoordinator;
    _cargoCoordinator.RegisterForStateChanges(_stateManager);
}
```

## Key Implementation Features

### 1. Door Coordination

A key enhancement in the GSXCargoCoordinator is coordination with the door coordinator for cargo operations:

```csharp
public async Task CoordinateWithDoorsAsync(bool forLoading, CancellationToken cancellationToken = default)
{
    try
    {
        if (_doorCoordinator == null)
        {
            _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:CoordinateWithDoorsAsync", "Door coordinator not available");
            return;
        }
        
        _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:CoordinateWithDoorsAsync", 
            $"Coordinating cargo doors for {(forLoading ? "loading" : "unloading")}");
        
        // For A320, cargo doors are typically:
        // - Forward cargo door
        // - Aft cargo door
        
        if (forLoading)
        {
            // Open cargo doors for loading
            await _doorCoordinator.OpenDoorAsync("FWD_CARGO", cancellationToken);
            await _doorCoordinator.OpenDoorAsync("AFT_CARGO", cancellationToken);
        }
        else
        {
            // Close cargo doors after loading/unloading
            await _doorCoordinator.CloseDoorAsync("FWD_CARGO", cancellationToken);
            await _doorCoordinator.CloseDoorAsync("AFT_CARGO", cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:CoordinateWithDoorsAsync", "Operation canceled");
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Error, "GSXCargoCoordinator:CoordinateWithDoorsAsync", 
            $"Error coordinating with doors: {ex.Message}");
    }
}
```

### 2. State-Based Cargo Management

The cargo coordinator manages cargo operations based on the current flight state:

```csharp
public async Task ManageCargoForStateAsync(FlightState state, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:ManageCargoForStateAsync", $"Managing cargo for state: {state}");
        
        switch (state)
        {
            case FlightState.PREFLIGHT:
                // In preflight, ensure cargo amounts are synchronized
                await SynchronizeCargoStatesAsync(cancellationToken);
                break;
                
            case FlightState.DEPARTURE:
                // In departure, start loading if not already in progress
                if (!_isLoadingInProgress && !_isUnloadingInProgress)
                {
                    await StartLoadingAsync(cancellationToken);
                }
                break;
                
            case FlightState.TAXIOUT:
                // In taxiout, stop loading if in progress and ensure cargo doors are closed
                if (_isLoadingInProgress)
                {
                    await StopLoadingAsync(cancellationToken);
                    
                    // Ensure cargo doors are closed
                    if (_doorCoordinator != null)
                    {
                        await CoordinateWithDoorsAsync(false, cancellationToken);
                    }
                }
                break;
                
            case FlightState.FLIGHT:
                // In flight, ensure loading is stopped and cargo doors are closed
                if (_isLoadingInProgress)
                {
                    await StopLoadingAsync(cancellationToken);
                }
                
                // Double-check cargo doors are closed
                if (_doorCoordinator != null)
                {
                    await CoordinateWithDoorsAsync(false, cancellationToken);
                }
                break;
                
            case FlightState.TAXIIN:
                // In taxiin, no cargo operations
                break;
                
            case FlightState.ARRIVAL:
                // In arrival, start unloading if not already in progress
                if (!_isUnloadingInProgress && !_isLoadingInProgress)
                {
                    await StartUnloadingAsync(cancellationToken);
                }
                break;
                
            case FlightState.TURNAROUND:
                // In turnaround, stop unloading if in progress
                if (_isUnloadingInProgress)
                {
                    await StopUnloadingAsync(cancellationToken);
                }
                break;
        }
    }
    catch (OperationCanceledException)
    {
        _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:ManageCargoForStateAsync", "Operation canceled");
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Error, "GSXCargoCoordinator:ManageCargoForStateAsync", 
            $"Error managing cargo for state {state}: {ex.Message}");
    }
}
```

### 3. Thread Safety

The implementation includes thread safety measures to ensure proper synchronization:

```csharp
private readonly object _stateLock = new object();

public bool StartLoading()
{
    try
    {
        _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:StartLoading", "Starting cargo loading process");
        
        lock (_stateLock)
        {
            if (_isLoadingInProgress)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StartLoading", "Loading already in progress");
                return false;
            }
            
            if (_isUnloadingInProgress)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StartLoading", "Cannot start loading while unloading is in progress");
                return false;
            }
            
            _isLoadingInProgress = true;
        }
        
        // Rest of the implementation...
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Error, "GSXCargoCoordinator:StartLoading", $"Error starting loading: {ex.Message}");
        return false;
    }
}
```

## Benefits

1. **Improved Separation of Concerns**
   - Cargo management logic is centralized in a dedicated coordinator
   - Clear responsibilities and boundaries
   - Reduced complexity in the GSXControllerFacade

2. **Enhanced Testability**
   - Cargo coordinator can be tested in isolation
   - Mock implementations can be used for testing
   - Unit tests can verify cargo management logic

3. **Better State Management**
   - Cargo states are managed based on flight state
   - Consistent cargo management across the application
   - Improved reliability of cargo operations

4. **Improved Safety**
   - Coordination with doors ensures proper operation
   - State validation prevents invalid operations
   - Comprehensive logging for troubleshooting

5. **Thread Safety**
   - Lock-based synchronization for state changes
   - Thread-safe event raising
   - Proper cancellation support for async operations

## Next Steps

1. **Phase 4.8: Create GSXFuelCoordinator**
   - Create IGSXFuelCoordinator interface
   - Implement GSXFuelCoordinator class
   - Update GSXControllerFacade to use the new coordinator
   - Modify ServiceController to initialize the coordinator

2. **Phase 4.9: Comprehensive Testing**
   - Create unit tests for all new coordinators
   - Create integration tests for coordinator interactions
   - Create performance tests for critical paths
