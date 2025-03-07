# Phase 4.5: GSXEquipmentCoordinator Implementation

## Overview

Phase 4.5 of the modularization strategy involves creating a domain-specific coordinator for ground equipment operations. The GSXEquipmentCoordinator will coordinate equipment operations with the ProsimEquipmentService, managing the state of ground equipment such as GPU, PCA, and chocks based on the current flight state.

This implementation follows the established patterns from previous phases, particularly the facade pattern used in GSXControllerFacade and the state management patterns in GSXStateManager.

## Implementation Details

### 1. Define Equipment Type Enum

First, create an enum to represent the different types of ground equipment:

```csharp
namespace Prosim2GSX.Services
{
    /// <summary>
    /// Defines the types of ground equipment
    /// </summary>
    public enum EquipmentType
    {
        GPU,
        PCA,
        Chocks,
        Jetway
    }
}
```

### 2. Create Event Arguments Class

Create an event arguments class for equipment state changes:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for equipment state changes
    /// </summary>
    public class EquipmentStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of equipment that changed state
        /// </summary>
        public EquipmentType EquipmentType { get; }

        /// <summary>
        /// Gets a value indicating whether the equipment is connected
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets the timestamp when the state change occurred
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquipmentStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="equipmentType">The type of equipment that changed state</param>
        /// <param name="isConnected">A value indicating whether the equipment is connected</param>
        public EquipmentStateChangedEventArgs(EquipmentType equipmentType, bool isConnected)
        {
            EquipmentType = equipmentType;
            IsConnected = isConnected;
            Timestamp = DateTime.Now;
        }
    }
}
```

### 3. Interface Definition

Create a new file `IGSXEquipmentCoordinator.cs` in the `Services` directory with the following content:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates ground equipment operations between GSX and ProSim
    /// </summary>
    public interface IGSXEquipmentCoordinator : IDisposable
    {
        /// <summary>
        /// Event raised when equipment state changes
        /// </summary>
        event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
        
        /// <summary>
        /// Gets a value indicating whether the GPU is connected
        /// </summary>
        bool IsGpuConnected { get; }
        
        /// <summary>
        /// Gets a value indicating whether the PCA is connected
        /// </summary>
        bool IsPcaConnected { get; }
        
        /// <summary>
        /// Gets a value indicating whether the chocks are placed
        /// </summary>
        bool AreChocksPlaced { get; }
        
        /// <summary>
        /// Gets a value indicating whether the jetway is connected
        /// </summary>
        bool IsJetwayConnected { get; }
        
        /// <summary>
        /// Initializes the equipment coordinator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Connects the specified equipment
        /// </summary>
        /// <param name="equipmentType">The type of equipment to connect</param>
        /// <returns>True if the equipment was connected successfully, false otherwise</returns>
        bool ConnectEquipment(EquipmentType equipmentType);
        
        /// <summary>
        /// Disconnects the specified equipment
        /// </summary>
        /// <param name="equipmentType">The type of equipment to disconnect</param>
        /// <returns>True if the equipment was disconnected successfully, false otherwise</returns>
        bool DisconnectEquipment(EquipmentType equipmentType);
        
        /// <summary>
        /// Connects the specified equipment asynchronously
        /// </summary>
        /// <param name="equipmentType">The type of equipment to connect</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the equipment was connected successfully, false otherwise</returns>
        Task<bool> ConnectEquipmentAsync(EquipmentType equipmentType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Disconnects the specified equipment asynchronously
        /// </summary>
        /// <param name="equipmentType">The type of equipment to disconnect</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the equipment was disconnected successfully, false otherwise</returns>
        Task<bool> DisconnectEquipmentAsync(EquipmentType equipmentType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synchronizes equipment states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SynchronizeEquipmentStatesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Manages equipment based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task ManageEquipmentForStateAsync(FlightState state, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        void RegisterForStateChanges(IGSXStateManager stateManager);
    }
}
```

### 4. Implementation Class

Create a new file `GSXEquipmentCoordinator.cs` in the `Services` directory with the following content:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates ground equipment operations between GSX and ProSim
    /// </summary>
    public class GSXEquipmentCoordinator : IGSXEquipmentCoordinator
    {
        private readonly IProsimEquipmentService _prosimEquipmentService;
        private readonly ILogger _logger;
        private IGSXStateManager _stateManager;
        private bool _disposed;
        
        // Equipment state tracking
        private bool _isGpuConnected;
        private bool _isPcaConnected;
        private bool _areChocksPlaced;
        private bool _isJetwayConnected;
        
        /// <summary>
        /// Event raised when equipment state changes
        /// </summary>
        public event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
        
        /// <summary>
        /// Gets a value indicating whether the GPU is connected
        /// </summary>
        public bool IsGpuConnected => _isGpuConnected;
        
        /// <summary>
        /// Gets a value indicating whether the PCA is connected
        /// </summary>
        public bool IsPcaConnected => _isPcaConnected;
        
        /// <summary>
        /// Gets a value indicating whether the chocks are placed
        /// </summary>
        public bool AreChocksPlaced => _areChocksPlaced;
        
        /// <summary>
        /// Gets a value indicating whether the jetway is connected
        /// </summary>
        public bool IsJetwayConnected => _isJetwayConnected;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GSXEquipmentCoordinator"/> class
        /// </summary>
        /// <param name="prosimEquipmentService">The ProSim equipment service</param>
        /// <param name="logger">The logger</param>
        public GSXEquipmentCoordinator(IProsimEquipmentService prosimEquipmentService, ILogger logger)
        {
            _prosimEquipmentService = prosimEquipmentService ?? throw new ArgumentNullException(nameof(prosimEquipmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Subscribe to equipment state change events
            _prosimEquipmentService.EquipmentStateChanged += OnProsimEquipmentStateChanged;
        }
        
        /// <summary>
        /// Initializes the equipment coordinator
        /// </summary>
        public void Initialize()
        {
            _logger.Log(LogLevel.Information, "GSXEquipmentCoordinator:Initialize", "Initializing equipment coordinator");
        }
        
        /// <summary>
        /// Connects the specified equipment
        /// </summary>
        /// <param name="equipmentType">The type of equipment to connect</param>
        /// <returns>True if the equipment was connected successfully, false otherwise</returns>
        public bool ConnectEquipment(EquipmentType equipmentType)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:ConnectEquipment", $"Connecting equipment: {equipmentType}");
                
                switch (equipmentType)
                {
                    case EquipmentType.GPU:
                        _prosimEquipmentService.SetServiceGPU(true);
                        UpdateEquipmentState(EquipmentType.GPU, true);
                        return true;
                        
                    case EquipmentType.PCA:
                        _prosimEquipmentService.SetServicePCA(true);
                        UpdateEquipmentState(EquipmentType.PCA, true);
                        return true;
                        
                    case EquipmentType.Chocks:
                        _prosimEquipmentService.SetServiceChocks(true);
                        UpdateEquipmentState(EquipmentType.Chocks, true);
                        return true;
                        
                    case EquipmentType.Jetway:
                        // Jetway is handled differently as it's not directly controlled by ProsimEquipmentService
                        UpdateEquipmentState(EquipmentType.Jetway, true);
                        return true;
                        
                    default:
                        _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:ConnectEquipment", $"Unknown equipment type: {equipmentType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:ConnectEquipment", $"Error connecting equipment {equipmentType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Disconnects the specified equipment
        /// </summary>
        /// <param name="equipmentType">The type of equipment to disconnect</param>
        /// <returns>True if the equipment was disconnected successfully, false otherwise</returns>
        public bool DisconnectEquipment(EquipmentType equipmentType)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:DisconnectEquipment", $"Disconnecting equipment: {equipmentType}");
                
                switch (equipmentType)
                {
                    case EquipmentType.GPU:
                        _prosimEquipmentService.SetServiceGPU(false);
                        UpdateEquipmentState(EquipmentType.GPU, false);
                        return true;
                        
                    case EquipmentType.PCA:
                        _prosimEquipmentService.SetServicePCA(false);
                        UpdateEquipmentState(EquipmentType.PCA, false);
                        return true;
                        
                    case EquipmentType.Chocks:
                        _prosimEquipmentService.SetServiceChocks(false);
                        UpdateEquipmentState(EquipmentType.Chocks, false);
                        return true;
                        
                    case EquipmentType.Jetway:
                        // Jetway is handled differently as it's not directly controlled by ProsimEquipmentService
                        UpdateEquipmentState(EquipmentType.Jetway, false);
                        return true;
                        
                    default:
                        _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:DisconnectEquipment", $"Unknown equipment type: {equipmentType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:DisconnectEquipment", $"Error disconnecting equipment {equipmentType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Connects the specified equipment asynchronously
        /// </summary>
        /// <param name="equipmentType">The type of equipment to connect</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the equipment was connected successfully, false otherwise</returns>
        public async Task<bool> ConnectEquipmentAsync(EquipmentType equipmentType, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:ConnectEquipmentAsync", $"Connecting equipment asynchronously: {equipmentType}");
                
                return await Task.Run(() => ConnectEquipment(equipmentType), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:ConnectEquipmentAsync", $"Operation canceled for equipment {equipmentType}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:ConnectEquipmentAsync", $"Error connecting equipment {equipmentType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Disconnects the specified equipment asynchronously
        /// </summary>
        /// <param name="equipmentType">The type of equipment to disconnect</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the equipment was disconnected successfully, false otherwise</returns>
        public async Task<bool> DisconnectEquipmentAsync(EquipmentType equipmentType, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:DisconnectEquipmentAsync", $"Disconnecting equipment asynchronously: {equipmentType}");
                
                return await Task.Run(() => DisconnectEquipment(equipmentType), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:DisconnectEquipmentAsync", $"Operation canceled for equipment {equipmentType}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:DisconnectEquipmentAsync", $"Error disconnecting equipment {equipmentType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Synchronizes equipment states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task SynchronizeEquipmentStatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:SynchronizeEquipmentStatesAsync", "Synchronizing equipment states");
                
                // No direct way to query equipment states from ProSim, so we'll just update based on our tracked states
                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:SynchronizeEquipmentStatesAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:SynchronizeEquipmentStatesAsync", $"Error synchronizing equipment states: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Manages equipment based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ManageEquipmentForStateAsync(FlightState state, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:ManageEquipmentForStateAsync", $"Managing equipment for state: {state}");
                
                switch (state)
                {
                    case FlightState.PREFLIGHT:
                        // In preflight, connect all ground equipment
                        await ConnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.DEPARTURE:
                        // In departure, keep ground equipment connected
                        await ConnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.TAXIOUT:
                        // In taxiout, disconnect all ground equipment
                        await DisconnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.FLIGHT:
                        // In flight, ensure all ground equipment is disconnected
                        await DisconnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.TAXIIN:
                        // In taxiin, ensure all ground equipment is disconnected
                        await DisconnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.ARRIVAL:
                        // In arrival, connect all ground equipment
                        await ConnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.TURNAROUND:
                        // In turnaround, keep ground equipment connected
                        await ConnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:ManageEquipmentForStateAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:ManageEquipmentForStateAsync", $"Error managing equipment for state {state}: {ex.Message}");
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
        /// Updates the equipment state and raises the EquipmentStateChanged event
        /// </summary>
        /// <param name="equipmentType">The type of equipment</param>
        /// <param name="isConnected">A value indicating whether the equipment is connected</param>
        private void UpdateEquipmentState(EquipmentType equipmentType, bool isConnected)
        {
            bool stateChanged = false;
            
            switch (equipmentType)
            {
                case EquipmentType.GPU:
                    stateChanged = _isGpuConnected != isConnected;
                    _isGpuConnected = isConnected;
                    break;
                    
                case EquipmentType.PCA:
                    stateChanged = _isPcaConnected != isConnected;
                    _isPcaConnected = isConnected;
                    break;
                    
                case EquipmentType.Chocks:
                    stateChanged = _areChocksPlaced != isConnected;
                    _areChocksPlaced = isConnected;
                    break;
                    
                case EquipmentType.Jetway:
                    stateChanged = _isJetwayConnected != isConnected;
                    _isJetwayConnected = isConnected;
                    break;
            }
            
            if (stateChanged)
            {
                var args = new EquipmentStateChangedEventArgs(equipmentType, isConnected);
                OnEquipmentStateChanged(args);
            }
        }
        
        /// <summary>
        /// Handles equipment state changes from ProSim
        /// </summary>
        private void OnProsimEquipmentStateChanged(object sender, EquipmentStateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:OnProsimEquipmentStateChanged", 
                    $"Equipment {e.EquipmentType} state changed to {(e.IsConnected ? "connected" : "disconnected")}");
                
                // Update our internal state
                UpdateEquipmentState(e.EquipmentType, e.IsConnected);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:OnProsimEquipmentStateChanged", 
                    $"Error handling equipment state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles state changes from the state manager
        /// </summary>
        private async void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:OnStateChanged", 
                    $"State changed from {e.PreviousState} to {e.NewState}");
                
                // Manage equipment based on the new state
                await ManageEquipmentForStateAsync(e.NewState);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:OnStateChanged", 
                    $"Error handling state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Raises the EquipmentStateChanged event
        /// </summary>
        protected virtual void OnEquipmentStateChanged(EquipmentStateChangedEventArgs e)
        {
            EquipmentStateChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// Disposes resources used by the equipment coordinator
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources used by the equipment coordinator
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                // Unsubscribe from events
                _prosimEquipmentService.EquipmentStateChanged -= OnProsimEquipmentStateChanged;
                
                if (_stateManager != null)
                    _stateManager.StateChanged -= OnStateChanged;
            }
            
            _disposed = true;
        }
    }
}
```

### 5. Update GSXControllerFacade

Update the `GSXControllerFacade` class to use the new `GSXEquipmentCoordinator`:

1. Add a new field for the equipment coordinator:
```csharp
private readonly IGSXEquipmentCoordinator _equipmentCoordinator;
```

2. Update the constructor to accept and initialize the equipment coordinator:
```csharp
public GSXControllerFacade(
    IGSXStateManager stateManager,
    IGSXServiceOrchestrator serviceOrchestrator,
    IGSXDoorCoordinator doorCoordinator,
    IGSXEquipmentCoordinator equipmentCoordinator,
    IGSXAudioService audioService,
    ILogger logger)
{
    _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
    _serviceOrchestrator = serviceOrchestrator ?? throw new ArgumentNullException(nameof(serviceOrchestrator));
    _doorCoordinator = doorCoordinator ?? throw new ArgumentNullException(nameof(doorCoordinator));
    _equipmentCoordinator = equipmentCoordinator ?? throw new ArgumentNullException(nameof(equipmentCoordinator));
    _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    
    // Subscribe to events
    _stateManager.StateChanged += OnStateChanged;
    _serviceOrchestrator.ServiceStatusChanged += OnServiceStatusChanged;
    
    // Register coordinators for state changes
    _doorCoordinator.RegisterForStateChanges(_stateManager);
    _equipmentCoordinator.RegisterForStateChanges(_stateManager);
}
```

3. Update the `Dispose` method to dispose the equipment coordinator:
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
        _equipmentCoordinator.Dispose();
    }
    
    _disposed = true;
}
```

### 6. Update ServiceController

Update the `ServiceController` class to create and initialize the equipment coordinator:

1. Add a new method to create the equipment coordinator:
```csharp
private IGSXEquipmentCoordinator CreateEquipmentCoordinator()
{
    var prosimEquipmentService = _serviceProvider.GetRequiredService<IProsimEquipmentService>();
    
    return new GSXEquipmentCoordinator(prosimEquipmentService, _logger);
}
```

2. Update the `InitializeServices` method to create and register the equipment coordinator:
```csharp
private void InitializeServices()
{
    // Create door coordinator
    var doorCoordinator = CreateDoorCoordinator();
    doorCoordinator.Initialize();
    
    // Create equipment coordinator
    var equipmentCoordinator = CreateEquipmentCoordinator();
    equipmentCoordinator.Initialize();
    
    // Create GSX controller facade
    var gsxControllerFacade = new GSXControllerFacade(
        _serviceProvider.GetRequiredService<IGSXStateManager>(),
        _serviceProvider.GetRequiredService<IGSXServiceOrchestrator>(),
        doorCoordinator,
        equipmentCoordinator,
        _serviceProvider.GetRequiredService<IGSXAudioService>(),
        _logger);
    
    // Register services
    _serviceProvider.RegisterService<IGSXDoorCoordinator>(doorCoordinator);
    _serviceProvider.RegisterService<IGSXEquipmentCoordinator>(equipmentCoordinator);
    _serviceProvider.RegisterService<IGSXControllerFacade>(gsxControllerFacade);
}
```

## Testing Strategy

### Unit Tests

Create unit tests for the `GSXEquipmentCoordinator` class to verify its functionality:

1. Test equipment connection and disconnection operations
2. Test state-based equipment management
3. Test event handling for equipment state changes
4. Test error handling and recovery

### Integration Tests

Create integration tests to verify the interaction between the `GSXEquipmentCoordinator` and other components:

1. Test interaction with `ProsimEquipmentService`
2. Test interaction with `GSXStateManager`
3. Test interaction with `GSXControllerFacade`

## Benefits

1. **Improved Separation of Concerns**
   - Equipment management logic is centralized in a dedicated coordinator
   - Clear responsibilities and boundaries
   - Reduced complexity in the GSXControllerFacade

2. **Enhanced Testability**
   - Equipment coordinator can be tested in isolation
   - Mock implementations can be used for testing
   - Unit tests can verify equipment management logic

3. **Better State Management**
   - Equipment states are managed based on flight state
   - Consistent equipment management across the application
   - Improved reliability of equipment operations

4. **Improved Error Handling**
   - Centralized error handling for equipment operations
   - Consistent logging
