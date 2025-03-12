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
            
            // Explicitly synchronize door states at initialization
            try
            {
                _logger.Log(LogLevel.Information, "GSXDoorCoordinator:Initialize", "Explicitly synchronizing door states");
                
                // Initialize all door states in ProSim
                _prosimDoorService.InitializeDoorStates();
                
                _logger.Log(LogLevel.Information, "GSXDoorCoordinator:Initialize", 
                    "Door states explicitly synchronized at initialization");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXDoorCoordinator:Initialize", 
                    $"Error synchronizing door states at initialization: {ex.Message}");
            }
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
                // Log detailed door management decisions with service states
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
                    $"Managing doors for state {state} - " +
                    $"ForwardRightDoor: {(_gsxDoorManager.IsForwardRightDoorOpen ? "open" : "closed")}, " +
                    $"AftRightDoor: {(_gsxDoorManager.IsAftRightDoorOpen ? "open" : "closed")}, " +
                    $"ForwardCargoDoor: {(_gsxDoorManager.IsForwardCargoDoorOpen ? "open" : "closed")}, " +
                    $"AftCargoDoor: {(_gsxDoorManager.IsAftCargoDoorOpen ? "open" : "closed")}");
                
                _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
                    $"Service states - " +
                    $"ForwardRightServiceActive: {_gsxDoorManager.IsForwardRightServiceActive}, " +
                    $"AftRightServiceActive: {_gsxDoorManager.IsAftRightServiceActive}, " +
                    $"ForwardCargoServiceActive: {_gsxDoorManager.IsForwardCargoServiceActive}, " +
                    $"AftCargoServiceActive: {_gsxDoorManager.IsAftCargoServiceActive}");
                
                switch (state)
                {
                    case FlightState.PREFLIGHT:
                        _logger.Log(LogLevel.Information, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
                            "PREFLIGHT state: Ensuring passenger doors are closed initially");
                        
                        // In preflight, ensure passenger doors are closed initially
                        await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await CloseDoorAsync(DoorType.AftRight, cancellationToken);
                        break;
                        
case FlightState.DEPARTURE:
    _logger.Log(LogLevel.Information, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
        "DEPARTURE state: Ensuring doors are closed unless service is active");
    
    // Do NOT automatically open doors
    // Instead, ensure doors are closed initially and wait for GSX requests
    // Only close doors if no service is active
    if (!_gsxDoorManager.IsForwardRightServiceActive) {
        _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
            "Closing forward right door (no active service)");
        await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
    } else {
        _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
            "Skipping forward right door (service active)");
    }
    
    if (!_gsxDoorManager.IsAftRightServiceActive) {
        _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
            "Closing aft right door (no active service)");
        await CloseDoorAsync(DoorType.AftRight, cancellationToken);
    } else {
        _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
            "Skipping aft right door (service active)");
    }
    
    // Cargo doors should also remain closed until explicitly requested
    if (!_gsxDoorManager.IsForwardCargoServiceActive) {
        _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
            "Closing forward cargo door (no active service)");
        await CloseDoorAsync(DoorType.ForwardCargo, cancellationToken);
    } else {
        _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
            "Skipping forward cargo door (service active)");
    }
    
    if (!_gsxDoorManager.IsAftCargoServiceActive) {
        _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
            "Closing aft cargo door (no active service)");
        await CloseDoorAsync(DoorType.AftCargo, cancellationToken);
    } else {
        _logger.Log(LogLevel.Debug, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
            "Skipping aft cargo door (service active)");
    }
    break;
                        
                    case FlightState.TAXIOUT:
                        _logger.Log(LogLevel.Information, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
                            "TAXIOUT state: Ensuring all doors are closed");
                        
                        // In taxiout, all doors should be closed
                        await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await CloseDoorAsync(DoorType.AftRight, cancellationToken);
                        await CloseDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await CloseDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                        
                    case FlightState.FLIGHT:
                        _logger.Log(LogLevel.Information, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
                            "FLIGHT state: Ensuring all doors remain closed");
                        
                        // In flight, all doors should remain closed
                        await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await CloseDoorAsync(DoorType.AftRight, cancellationToken);
                        await CloseDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await CloseDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                        
                    case FlightState.TAXIIN:
                        _logger.Log(LogLevel.Information, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
                            "TAXIIN state: Ensuring all doors remain closed");
                        
                        // In taxiin, all doors should remain closed
                        await CloseDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await CloseDoorAsync(DoorType.AftRight, cancellationToken);
                        await CloseDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await CloseDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                        
                    case FlightState.ARRIVAL:
                        _logger.Log(LogLevel.Information, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
                            "ARRIVAL state: Opening passenger and cargo doors for deboarding/unloading");
                        
                        // In arrival, passenger doors should be open for deboarding
                        await OpenDoorAsync(DoorType.ForwardRight, cancellationToken);
                        await OpenDoorAsync(DoorType.AftRight, cancellationToken);
                        
                        // Cargo doors should be open for unloading
                        await OpenDoorAsync(DoorType.ForwardCargo, cancellationToken);
                        await OpenDoorAsync(DoorType.AftCargo, cancellationToken);
                        break;
                        
                    case FlightState.TURNAROUND:
                        _logger.Log(LogLevel.Information, "GSXDoorCoordinator:ManageDoorsForStateAsync", 
                            "TURNAROUND state: Opening passenger and cargo doors for servicing");
                        
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
