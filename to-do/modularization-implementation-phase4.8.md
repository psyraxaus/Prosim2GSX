# Phase 4.8: GSXFuelCoordinator Implementation

## Overview

Phase 4.8 of the modularization strategy involves creating a domain-specific coordinator for fuel operations. The GSXFuelCoordinator coordinates fuel operations between the GSXServiceOrchestrator and ProsimFuelService, managing refueling operations based on the current flight state.

This implementation follows the established patterns from previous phases, particularly Phase 4.7 (GSXCargoCoordinator), with fuel-specific considerations.

## Implementation Details

### 1. Create FuelStateChangedEventArgs Class

First, we created a dedicated event arguments class for fuel state changes:

```csharp
public class FuelStateChangedEventArgs : EventArgs
{
    public string OperationType { get; }
    public double CurrentAmount { get; }
    public double PlannedAmount { get; }
    public string FuelUnits { get; }
    public DateTime Timestamp { get; }
    
    public FuelStateChangedEventArgs(string operationType, double currentAmount, double plannedAmount, string fuelUnits)
    {
        OperationType = operationType;
        CurrentAmount = currentAmount;
        PlannedAmount = plannedAmount;
        FuelUnits = fuelUnits;
        Timestamp = DateTime.Now;
    }
}
```

### 2. Create RefuelingProgressChangedEventArgs Class

Next, we created an event arguments class for refueling progress:

```csharp
public class RefuelingProgressChangedEventArgs : EventArgs
{
    public int ProgressPercentage { get; }
    public double CurrentAmount { get; }
    public double TargetAmount { get; }
    public string FuelUnits { get; }
    public DateTime Timestamp { get; }
    
    public RefuelingProgressChangedEventArgs(int progressPercentage, double currentAmount, double targetAmount, string fuelUnits)
    {
        ProgressPercentage = progressPercentage;
        CurrentAmount = currentAmount;
        TargetAmount = targetAmount;
        FuelUnits = fuelUnits;
        Timestamp = DateTime.Now;
    }
}
```

### 3. Define RefuelingState Enum

We defined an enum for tracking the refueling state:

```csharp
public enum RefuelingState
{
    Idle,
    Refueling,
    Defueling,
    Complete,
    Error
}
```

### 4. Define IGSXFuelCoordinator Interface

Next, we defined the interface for the fuel coordinator:

```csharp
public interface IGSXFuelCoordinator : IDisposable
{
    event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;
    event EventHandler<RefuelingProgressChangedEventArgs> RefuelingProgressChanged;
    
    RefuelingState RefuelingState { get; }
    double FuelPlanned { get; }
    double FuelCurrent { get; }
    string FuelUnits { get; }
    int RefuelingProgressPercentage { get; }
    float FuelRateKGS { get; }
    
    void Initialize();
    
    bool StartRefueling();
    bool StopRefueling();
    bool StartDefueling();
    bool StopDefueling();
    bool UpdateFuelAmount(double fuelAmount);
    
    Task<bool> StartRefuelingAsync(CancellationToken cancellationToken = default);
    Task<bool> StopRefuelingAsync(CancellationToken cancellationToken = default);
    Task<bool> StartDefuelingAsync(CancellationToken cancellationToken = default);
    Task<bool> StopDefuelingAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateFuelAmountAsync(double fuelAmount, CancellationToken cancellationToken = default);
    
    Task SynchronizeFuelQuantitiesAsync(CancellationToken cancellationToken = default);
    Task<double> CalculateRequiredFuelAsync(CancellationToken cancellationToken = default);
    Task ManageFuelForStateAsync(FlightState state, CancellationToken cancellationToken = default);
    
    void RegisterForStateChanges(IGSXStateManager stateManager);
}
```

### 5. Implement GSXFuelCoordinator Class

Then, we implemented the GSXFuelCoordinator class:

```csharp
public class GSXFuelCoordinator : IGSXFuelCoordinator
{
    private readonly IProsimFuelService _prosimFuelService;
    private readonly IGSXServiceOrchestrator _serviceOrchestrator;
    private readonly ILogger _logger;
    private IGSXStateManager _stateManager;
    private bool _disposed;
    
    // Fuel state tracking
    private RefuelingState _refuelingState;
    private double _fuelPlanned;
    private double _fuelCurrent;
    private string _fuelUnits;
    private int _refuelingProgressPercentage;
    private readonly object _stateLock = new object();
    
    // Events and properties...
    
    // Constructor and method implementations...
}
```

The implementation includes:
- Synchronous and asynchronous methods for fuel operations
- State tracking for fuel quantities and refueling progress
- Event-based communication for fuel state changes
- State-based fuel management
- Comprehensive error handling and logging

### 6. Update ProsimController

We added a method to the ProsimController class to expose the fuel service:

```csharp
/// <summary>
/// Gets the fuel service
/// </summary>
/// <returns>The fuel service</returns>
public IProsimFuelService GetFuelService()
{
    return _fuelService;
}
```

### 7. Update ServiceController

We updated the ServiceController class to create and initialize the fuel coordinator:

```csharp
// Create fuel coordinator
var fuelCoordinator = new GSXFuelCoordinator(
    ProsimController.GetFuelService(),
    serviceOrchestrator,
    Logger.Instance);
fuelCoordinator.Initialize();
```

### 8. Update GSXControllerFacade

Finally, we updated the GSXControllerFacade class to use the new fuel coordinator:

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
    IGSXFuelCoordinator fuelCoordinator,
    ILogger logger)
{
    // Initialize fields and register for state changes
    _fuelCoordinator = fuelCoordinator;
    _fuelCoordinator.RegisterForStateChanges(_stateManager);
}
```

## Key Implementation Features

### 1. State-Based Fuel Management

The fuel coordinator manages fuel operations based on the current flight state:

```csharp
public async Task ManageFuelForStateAsync(FlightState state, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:ManageFuelForStateAsync", $"Managing fuel for state: {state}");
        
        switch (state)
        {
            case FlightState.PREFLIGHT:
                // In preflight, ensure fuel quantities are synchronized and set initial fuel
                await SynchronizeFuelQuantitiesAsync(cancellationToken);
                _prosimFuelService.SetInitialFuel();
                break;
                
            case FlightState.DEPARTURE:
                // In departure, start refueling if not already in progress
                if (_refuelingState == RefuelingState.Idle)
                {
                    await StartRefuelingAsync(cancellationToken);
                }
                break;
                
            case FlightState.TAXIOUT:
                // In taxiout, stop refueling if in progress
                if (_refuelingState == RefuelingState.Refueling)
                {
                    await StopRefuelingAsync(cancellationToken);
                }
                break;
                
            case FlightState.FLIGHT:
                // In flight, ensure refueling is stopped
                if (_refuelingState == RefuelingState.Refueling)
                {
                    await StopRefuelingAsync(cancellationToken);
                }
                break;
                
            case FlightState.TAXIIN:
                // In taxiin, no fuel operations
                break;
                
            case FlightState.ARRIVAL:
                // In arrival, no fuel operations
                break;
                
            case FlightState.TURNAROUND:
                // In turnaround, calculate required fuel for next flight
                double requiredFuel = await CalculateRequiredFuelAsync(cancellationToken);
                await UpdateFuelAmountAsync(requiredFuel, cancellationToken);
                break;
        }
    }
    catch (OperationCanceledException)
    {
        _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:ManageFuelForStateAsync", "Operation canceled");
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Error, "GSXFuelCoordinator:ManageFuelForStateAsync", 
            $"Error managing fuel for state {state}: {ex.Message}");
    }
}
```

### 2. Thread Safety

The implementation includes thread safety measures to ensure proper synchronization:

```csharp
private readonly object _stateLock = new object();

public bool StartRefueling()
{
    try
    {
        _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StartRefueling", "Starting refueling process");
        
        lock (_stateLock)
        {
            if (_refuelingState == RefuelingState.Refueling)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartRefueling", "Refueling already in progress");
                return false;
            }
            
            if (_refuelingState == RefuelingState.Defueling)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartRefueling", "Cannot start refueling while defueling is in progress");
                return false;
            }
            
            _refuelingState = RefuelingState.Refueling;
        }
        
        // Rest of the implementation...
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StartRefueling", $"Error starting refueling: {ex.Message}");
        return false;
    }
}
```

### 3. Asynchronous Refueling Monitoring

The implementation includes asynchronous monitoring of refueling progress:

```csharp
public async Task<bool> StartRefuelingAsync(CancellationToken cancellationToken = default)
{
    try
    {
        _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StartRefuelingAsync", "Starting refueling process asynchronously");
        
        // Check if cancellation is requested
        cancellationToken.ThrowIfCancellationRequested();
        
        // Use the synchronous method for the actual operation
        bool result = StartRefueling();
        
        if (result)
        {
            // Monitor refueling progress asynchronously
            await Task.Run(async () => {
                while (_refuelingState == RefuelingState.Refueling && !cancellationToken.IsCancellationRequested)
                {
                    // Update current fuel amount
                    _fuelCurrent = _prosimFuelService.GetFuelAmount();
                    
                    // Calculate progress percentage
                    if (_fuelPlanned > 0)
                    {
                        _refuelingProgressPercentage = (int)((_fuelCurrent / _fuelPlanned) * 100);
                        _refuelingProgressPercentage = Math.Min(100, _refuelingProgressPercentage);
                    }
                    
                    // Raise progress event
                    OnRefuelingProgressChanged(_refuelingProgressPercentage, _fuelCurrent, _fuelPlanned);
                    
                    // Check if refueling is complete
                    bool isComplete = _prosimFuelService.Refuel();
                    if (isComplete)
                    {
                        lock (_stateLock)
                        {
                            _refuelingState = RefuelingState.Complete;
                            _refuelingProgressPercentage = 100;
                        }
                        
                        OnFuelStateChanged("RefuelingComplete", _fuelCurrent, _fuelPlanned);
                        break;
                    }
                    
                    // Wait before checking again
                    await Task.Delay(1000, cancellationToken);
                }
            }, cancellationToken);
        }
        
        return result;
    }
    catch (OperationCanceledException)
    {
        _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartRefuelingAsync", "Operation canceled");
        
        // Stop refueling if it was started
        if (_refuelingState == RefuelingState.Refueling)
        {
            StopRefueling();
        }
        
        return false;
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StartRefuelingAsync", $"Error starting refueling asynchronously: {ex.Message}");
        
        lock (_stateLock)
        {
            _refuelingState = RefuelingState.Error;
        }
        
        return false;
    }
}
```

## Benefits

1. **Improved Separation of Concerns**
   - Fuel management logic is centralized in a dedicated coordinator
   - Clear responsibilities and boundaries
   - Reduced complexity in the GSXControllerFacade

2. **Enhanced Testability**
   - Fuel coordinator can be tested in isolation
   - Mock implementations can be used for testing
   - Unit tests can verify fuel management logic

3. **Better State Management**
   - Fuel states are managed based on flight state
   - Consistent fuel management across the application
   - Improved reliability of fuel operations

4. **Improved Safety**
   - State validation prevents invalid operations
   - Comprehensive logging for troubleshooting
   - Thread-safe operations

5. **Thread Safety**
   - Lock-based synchronization for state changes
   - Thread-safe event raising
   - Proper cancellation support for async operations

## Next Steps

1. **Phase 4.9: Comprehensive Testing**
   - Create unit tests for all new coordinators
   - Create integration tests for coordinator interactions
   - Create performance tests for critical paths
