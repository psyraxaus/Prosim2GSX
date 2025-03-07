# Phase 4.4: GSXDoorCoordinator Implementation

## Overview

Phase 4.4 of the modularization strategy involves creating a domain-specific coordinator for aircraft door operations. The GSXDoorCoordinator will serve as a mediator between the GSXDoorManager and ProsimDoorService, coordinating door operations and state tracking between GSX and ProSim.

This implementation follows the established patterns from previous phases, particularly the facade pattern used in GSXControllerFacade and the state management patterns in GSXStateManager.

## Implementation Details

### 1. Interface Definition

Create a new file `IGSXDoorCoordinator.cs` in the `Services` directory with the following content:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates door operations between GSX and ProSim
    /// </summary>
    public interface IGSXDoorCoordinator : IDisposable
    {
        /// <summary>
        /// Event raised when a door state changes
        /// </summary>
        event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
        
        /// <summary>
        /// Gets a value indicating whether the forward right door is open
        /// </summary>
        bool IsForwardRightDoorOpen { get; }
        
        /// <summary>
        /// Gets a value indicating whether the aft right door is open
        /// </summary>
        bool IsAftRightDoorOpen { get; }
        
        /// <summary>
        /// Gets a value indicating whether the forward cargo door is open
        /// </summary>
        bool IsForwardCargoDoorOpen { get; }
        
        /// <summary>
        /// Gets a value indicating whether the aft cargo door is open
        /// </summary>
        bool IsAftCargoDoorOpen { get; }
        
        /// <summary>
        /// Initializes the door coordinator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Opens a door
        /// </summary>
        /// <param name="doorType">The type of door to open</param>
        /// <returns>True if the door was opened successfully, false otherwise</returns>
        bool OpenDoor(DoorType doorType);
        
        /// <summary>
        /// Closes a door
        /// </summary>
        /// <param name="doorType">The type of door to close</param>
        /// <returns>True if the door was closed successfully, false otherwise</returns>
        bool CloseDoor(DoorType doorType);
        
        /// <summary>
        /// Opens a door asynchronously
        /// </summary>
        /// <param name="doorType">The type of door to open</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the door was opened successfully, false otherwise</returns>
        Task<bool> OpenDoorAsync(DoorType doorType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Closes a door asynchronously
        /// </summary>
        /// <param name="doorType">The type of door to close</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the door was closed successfully, false otherwise</returns>
        Task<bool> CloseDoorAsync(DoorType doorType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synchronizes door states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SynchronizeDoorStatesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Manages doors based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task ManageDoorsForStateAsync(FlightState state, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        void RegisterForStateChanges(IGSXStateManager stateManager);
    }
}
```

### 2. Implementation Class

Create a new file `GSXDoorCoordinator.cs` in the `Services` directory with the following content:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates door operations between GSX and ProSim
    /// </summary>
    public class GSXDoorCoordinator : IGSXDoorCoordinator
    {
        private readonly IGSXDoorManager _gsxDoorManager;
        private readonly IProsimDoorService _prosimDoorService;
        private readonly ILogger _logger;
        private IGSXStateManager _stateManager;
        private bool _disposed;
        
        /// <summary>
        /// Event raised when a door state changes
        /// </summary>
        public event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
        
        /// <summary>
        /// Gets a value indicating whether the forward right door is open
        /// </summary>
        public bool IsForwardRightDoorOpen => _gsxDoorManager.IsForwardRightDoorOpen;
        
        /// <summary>
        /// Gets a value indicating whether the aft right door is open
        /// </summary>
        public bool IsAftRightDoorOpen => _gsxDoorManager.IsAftRightDoorOpen;
        
        /// <summary>
        /// Gets a value indicating whether the forward cargo door is open
        /// </summary>
        public bool IsForwardCargoDoorOpen => _gsxDoorManager.IsForwardCargoDoorOpen;
        
        /// <summary>
        /// Gets a value indicating whether the aft cargo door is open
        /// </summary>
        public bool IsAftCargoDoorOpen => _gsxDoorManager.IsAftCargoDoorOpen;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorCoordinator"/> class
        /// </summary>
        /// <param name="gsxDoorManager">The GSX door manager</param>
        /// <param name="prosimDoorService">The ProSim door service</param>
        /// <param name="logger">The logger</param>
        public GSXDoorCoordinator(IGSXDoorManager gsxDoorManager, IProsimDoorService prosimDoorService, ILogger logger)
        {
            _gsxDoorManager = gsxDoorManager ?? throw new ArgumentNullException(nameof(gsxDoorManager));
            _prosimDoorService = prosimDoorService ?? throw new ArgumentNullException(nameof(prosimDoorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Subscribe to door state change events
            _gsxDoorManager.DoorStateChanged += OnGsxDoorStateChanged;
            _prosimDoorService.DoorStateChanged += OnProsimDoorStateChanged;
        }
        
        /// <summary>
        /// Initializes the door coordinator
        /// </summary>
        public void Initialize()
        {
            _logger.Log(LogLevel.Information, "GSXDoorCoordinator:Initialize", "Initializing door coordinator");
            _gsxDoorManager.Initialize();
        }
        
        /// <summary>
        /// Opens a door
        /// </summary>
        /// <param name="doorType">The type of door to open</param>
        /// <returns>True if the door was opened successfully, false otherwise</returns>
        public bool OpenDoor(DoorType doorType)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:OpenDoor", $"Opening door: {doorType}");
                
                // Open door in GSX
                bool gsxResult = _gsxDoorManager.OpenDoor(doorType);
                
                // Open door in ProSim
                switch (doorType)
                {
                    case DoorType.ForwardRight:
                        _prosimDoorService.SetForwardRightDoor(true);
                        break;
                    case DoorType.AftRight:
                        _prosimDoorService.SetAftRightDoor(true);
                        break;
                    case DoorType.ForwardCargo:
                        _prosimDoorService.SetForwardCargoDoor(true);
                        break;
                    case DoorType.AftCargo:
                        _prosimDoorService.SetAftCargoDoor(true);
                        break;
                }
                
                return gsxResult;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:OpenDoor", $"Error opening door {doorType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Closes a door
        /// </summary>
        /// <param name="doorType">The type of door to close</param>
        /// <returns>True if the door was closed successfully, false otherwise</returns>
        public bool CloseDoor(DoorType doorType)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:CloseDoor", $"Closing door: {doorType}");
                
                // Close door in GSX
                bool gsxResult = _gsxDoorManager.CloseDoor(doorType);
                
                // Close door in ProSim
                switch (doorType)
                {
                    case DoorType.ForwardRight:
                        _prosimDoorService.SetForwardRightDoor(false);
                        break;
                    case DoorType.AftRight:
                        _prosimDoorService.SetAftRightDoor(false);
                        break;
                    case DoorType.ForwardCargo:
                        _prosimDoorService.SetForwardCargoDoor(false);
                        break;
                    case DoorType.AftCargo:
                        _prosimDoorService.SetAftCargoDoor(false);
                        break;
                }
                
                return gsxResult;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:CloseDoor", $"Error closing door {doorType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Opens a door asynchronously
        /// </summary>
        /// <param name="doorType">The type of door to open</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the door was opened successfully, false otherwise</returns>
        public async Task<bool> OpenDoorAsync(DoorType doorType, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:OpenDoorAsync", $"Opening door asynchronously: {doorType}");
                
                // Open door in GSX
                bool gsxResult = await Task.Run(() => _gsxDoorManager.OpenDoor(doorType), cancellationToken);
                
                // Open door in ProSim
                await Task.Run(() => 
                {
                    switch (doorType)
                    {
                        case DoorType.ForwardRight:
                            _prosimDoorService.SetForwardRightDoor(true);
                            break;
                        case DoorType.AftRight:
                            _prosimDoorService.SetAftRightDoor(true);
                            break;
                        case DoorType.ForwardCargo:
                            _prosimDoorService.SetForwardCargoDoor(true);
                            break;
                        case DoorType.AftCargo:
                            _prosimDoorService.SetAftCargoDoor(true);
                            break;
                    }
                }, cancellationToken);
                
                return gsxResult;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXDoorCoordinator:OpenDoorAsync", $"Operation canceled for door {doorType}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:OpenDoorAsync", $"Error opening door {doorType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Closes a door asynchronously
        /// </summary>
        /// <param name="doorType">The type of door to close</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the door was closed successfully, false otherwise</returns>
        public async Task<bool> CloseDoorAsync(DoorType doorType, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:CloseDoorAsync", $"Closing door asynchronously: {doorType}");
                
                // Close door in GSX
                bool gsxResult = await Task.Run(() => _gsxDoorManager.CloseDoor(doorType), cancellationToken);
                
                // Close door in ProSim
                await Task.Run(() => 
                {
                    switch (doorType)
                    {
                        case DoorType.ForwardRight:
                            _prosimDoorService.SetForwardRightDoor(false);
                            break;
                        case DoorType.AftRight:
                            _prosimDoorService.SetAftRightDoor(false);
                            break;
                        case DoorType.ForwardCargo:
                            _prosimDoorService.SetForwardCargoDoor(false);
                            break;
                        case DoorType.AftCargo:
                            _prosimDoorService.SetAftCargoDoor(false);
                            break;
                    }
                }, cancellationToken);
                
                return gsxResult;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXDoorCoordinator:CloseDoorAsync", $"Operation canceled for door {doorType}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:CloseDoorAsync", $"Error closing door {doorType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Synchronizes door states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task SynchronizeDoorStatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:SynchronizeDoorStatesAsync", "Synchronizing door states");
                
                await Task.Run(() => 
                {
                    // Synchronize forward right door
                    _prosimDoorService.SetForwardRightDoor(_gsxDoorManager.IsForwardRightDoorOpen);
                    
                    // Synchronize aft right door
                    _prosimDoorService.SetAftRightDoor(_gsxDoorManager.IsAftRightDoorOpen);
                    
                    // Synchronize forward cargo door
                    _prosimDoorService.SetForwardCargoDoor(_gsxDoorManager.IsForwardCargoDoorOpen);
                    
                    // Synchronize aft cargo door
                    _prosimDoorService.SetAftCargoDoor(_gsxDoorManager.IsAftCargoDoorOpen);
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXDoorCoordinator:SynchronizeDoorStatesAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:SynchronizeDoorStatesAsync", $"Error synchronizing door states: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Manages doors based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ManageDoorsForStateAsync(FlightState state, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", $"Managing doors for state: {state}");
                
                switch (state)
                {
                    case FlightState.PREFLIGHT:
                        // In preflight, ensure passenger doors are closed initially
                        await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await CloseDoorAsync(DoorType.AftRight, cancellationToken);
                        break;
                        
                    case FlightState.DEPARTURE:
                        // In departure, passenger doors should be open for boarding
                        await OpenDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await OpenDoorAsync(DoorType.AftRight, cancellationToken);
                        
                        // Cargo doors should be open for loading
                        await OpenDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await OpenDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                        
                    case FlightState.TAXIOUT:
                        // In taxiout, all doors should be closed
                        await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await CloseDoorAsync(DoorType.AftRight, cancellationToken);
                        await CloseDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await CloseDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                        
                    case FlightState.FLIGHT:
                        // In flight, all doors should remain closed
                        await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await CloseDoorAsync(DoorType.AftRight, cancellationToken);
                        await CloseDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await CloseDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                        
                    case FlightState.TAXIIN:
                        // In taxiin, all doors should remain closed
                        await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await CloseDoorAsync(DoorType.AftRight, cancellationToken);
                        await CloseDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await CloseDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                        
                    case FlightState.ARRIVAL:
                        // In arrival, passenger doors should be open for deboarding
                        await OpenDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await OpenDoorAsync(DoorType.AftRight, cancellationToken);
                        
                        // Cargo doors should be open for unloading
                        await OpenDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await OpenDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                        
                    case FlightState.TURNAROUND:
                        // In turnaround, passenger doors should be open
                        await OpenDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await OpenDoorAsync(DoorType.AftRight, cancellationToken);
                        
                        // Cargo doors should be open for unloading/loading
                        await OpenDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await OpenDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXDoorCoordinator:ManageDoorsForStateAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:ManageDoorsForStateAsync", $"Error managing doors for state {state}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        public void RegisterForStateChanges(IGSXStateManager stateManager)
        {
            if (stateManager == null)
                throw new ArgumentNullException(nameof(stateManager));
                
            _stateManager = stateManager;
            _stateManager.StateChanged += OnStateChanged;
        }
        
        /// <summary>
        /// Handles door state changes from GSX
        /// </summary>
        private void OnGsxDoorStateChanged(object sender, DoorStateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:OnGsxDoorStateChanged", 
                    $"Door {e.DoorType} state changed to {(e.IsOpen ? "open" : "closed")}");
                
                // Synchronize with ProSim
                switch (e.DoorType)
                {
                    case DoorType.ForwardRight:
                        _prosimDoorService.SetForwardRightDoor(e.IsOpen);
                        break;
                    case DoorType.AftRight:
                        _prosimDoorService.SetAftRightDoor(e.IsOpen);
                        break;
                    case DoorType.ForwardCargo:
                        _prosimDoorService.SetForwardCargoDoor(e.IsOpen);
                        break;
                    case DoorType.AftCargo:
                        _prosimDoorService.SetAftCargoDoor(e.IsOpen);
                        break;
                }
                
                // Forward the event
                OnDoorStateChanged(e);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:OnGsxDoorStateChanged", 
                    $"Error handling door state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles door state changes from ProSim
        /// </summary>
        private void OnProsimDoorStateChanged(object sender, DoorStateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:OnProsimDoorStateChanged", 
                    $"Door {e.DoorType} state changed to {(e.IsOpen ? "open" : "closed")}");
                
                // Synchronize with GSX
                switch (e.DoorType)
                {
                    case DoorType.ForwardRight:
                        if (e.IsOpen)
                            _gsxDoorManager.OpenDoor(e.DoorType);
                        else
                            _gsxDoorManager.CloseDoor(e.DoorType);
                        break;
                    case DoorType.AftRight:
                        if (e.IsOpen)
                            _gsxDoorManager.OpenDoor(e.DoorType);
                        else
                            _gsxDoorManager.CloseDoor(e.DoorType);
                        break;
                    case DoorType.ForwardCargo:
                        if (e.IsOpen)
                            _gsxDoorManager.OpenDoor(e.DoorType);
                        else
                            _gsxDoorManager.CloseDoor(e.DoorType);
                        break;
                    case DoorType.AftCargo:
                        if (e.IsOpen)
                            _gsxDoorManager.OpenDoor(e.DoorType);
                        else
                            _gsxDoorManager.CloseDoor(e.DoorType);
                        break;
                }
                
                // Forward the event
                OnDoorStateChanged(e);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:OnProsimDoorStateChanged", 
                    $"Error handling door state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles state changes from the state manager
        /// </summary>
        private async void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:OnStateChanged", 
                    $"State changed from {e.PreviousState} to {e.NewState}");
                
                // Manage doors based on the new state
                await ManageDoorsForStateAsync(e.NewState);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:OnStateChanged", 
                    $"Error handling state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Raises the DoorStateChanged event
        /// </summary>
        protected virtual void OnDoorStateChanged(DoorStateChangedEventArgs e)
        {
            DoorStateChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// Disposes resources used by the door coordinator
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources used by the door coordinator
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                // Unsubscribe from events
                _gsxDoorManager.DoorStateChanged -= OnGsxDoorStateChanged;
                _prosimDoorService.DoorStateChanged -= OnProsimDoorStateChanged;
                
                if (_stateManager != null)
                    _stateManager.StateChanged -= OnStateChanged;
            }
            
            _disposed = true;
        }
    }
}
```

### 3. Update GSXControllerFacade

Update the `GSXControllerFacade` class to use the new `GSXDoorCoordinator`:

1. Add a new field for the door coordinator:
```csharp
private readonly IGSXDoorCoordinator _doorCoordinator;
```

2. Update the constructor to accept and initialize the door coordinator:
```csharp
public GSXControllerFacade(
    IGSXStateManager stateManager,
    IGSXServiceOrchestrator serviceOrchestrator,
    IGSXDoorCoordinator doorCoordinator,
    IGSXAudioService audioService,
    ILogger logger)
{
    _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
    _serviceOrchestrator = serviceOrchestrator ?? throw new ArgumentNullException(nameof(serviceOrchestrator));
    _doorCoordinator = doorCoordinator ?? throw new ArgumentNullException(nameof(doorCoordinator));
    _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    
    // Subscribe to events
    _stateManager.StateChanged += OnStateChanged;
    _serviceOrchestrator.ServiceStatusChanged += OnServiceStatusChanged;
    
    // Register coordinators for state changes
    _doorCoordinator.RegisterForStateChanges(_stateManager);
}
```

3. Update the `Dispose` method to dispose the door coordinator:
```csharp
protected virtual void Dispose(bool disposing)
{
    if (_disposed)
        return;
        
    if (disposing)
    {
        // Unsubscribe from events
        _stateManager.StateChanged -= OnStateChanged;
        _serviceOrchestrator.ServiceStatusChanged -= OnServiceStatusChanged;
        
        // Dispose services
        _doorCoordinator.Dispose();
    }
    
    _disposed = true;
}
```

### 4. Update ServiceController

Update the `ServiceController` class to create and initialize the door coordinator:

1. Add a new method to create the door coordinator:
```csharp
private IGSXDoorCoordinator CreateDoorCoordinator()
{
    var doorManager = _serviceProvider.GetRequiredService<IGSXDoorManager>();
    var prosimDoorService = _serviceProvider.GetRequiredService<IProsimDoorService>();
    
    return new GSXDoorCoordinator(doorManager, prosimDoorService, _logger);
}
```

2. Update the `InitializeServices` method to create and register the door coordinator:
```csharp
private void InitializeServices()
{
    // Create door coordinator
    var doorCoordinator = CreateDoorCoordinator();
    doorCoordinator.Initialize();
    
    // Create GSX controller facade
    var gsxControllerFacade = new GSXControllerFacade(
        _serviceProvider.GetRequiredService<IGSXStateManager>(),
        _serviceProvider.GetRequiredService<IGSXServiceOrchestrator>(),
        doorCoordinator,
        _serviceProvider.GetRequiredService<IGSXAudioService>(),
        _logger);
    
    // Register services
    _serviceProvider.RegisterService<IGSXDoorCoordinator>(doorCoordinator);
    _serviceProvider.RegisterService<IGSXControllerFacade>(gsxControllerFacade);
}
```

## Testing Strategy

### Unit Tests

Create unit tests for the `GSXDoorCoordinator` class to verify its functionality:

1. Test door opening and closing operations
2. Test synchronization between GSX and ProSim
3. Test state-based door management
4. Test event handling for door state changes
5. Test error handling and recovery

### Integration Tests

Create integration tests to verify the interaction between the `GSXDoorCoordinator` and other components:

1. Test interaction with `GSXDoorManager`
2. Test interaction with `ProsimDoorService`
3. Test interaction with `GSXStateManager`
4. Test interaction with `GSXControllerFacade`

## Benefits

1. **Improved Separation of Concerns**
   - Door management logic is centralized in a dedicated coordinator
   - Clear responsibilities and boundaries
   - Reduced complexity in the GSXControllerFacade

2. **Enhanced Testability**
   - Door coordinator can be tested in isolation
