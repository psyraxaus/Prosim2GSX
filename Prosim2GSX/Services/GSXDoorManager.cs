using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Prosim2GSX.Models;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Manages aircraft doors in GSX
    /// </summary>
    public class GSXDoorManager : IGSXDoorManager
    {
        private readonly object _lockObject = new object();
        private readonly IProsimDoorService _prosimDoorService;
        private readonly ServiceModel _model;
        private readonly MobiSimConnect _simConnect;

        private bool _isForwardLeftDoorOpen;
        private bool _isForwardRightDoorOpen;
        private bool _isAftLeftDoorOpen;
        private bool _isAftRightDoorOpen;
        private bool _isForwardCargoDoorOpen;
        private bool _isAftCargoDoorOpen;
        private bool _isInitialized;

        // Service state tracking
        private bool _isForwardRightServiceActive;
        private bool _isAftRightServiceActive;
        private bool _isForwardCargoServiceActive;
        private bool _isAftCargoServiceActive;

        // Toggle-to-door mapping
        private Dictionary<int, DoorType> _toggleToDoorMapping = new Dictionary<int, DoorType>();

        // Door state change tracking for circuit breaker
        private Dictionary<DoorType, (DateTime LastChange, int ChangeCount)> _doorChangeTracking = 
            new Dictionary<DoorType, (DateTime, int)>();

        /// <summary>
        /// Gets a value indicating whether the forward left door is open
        /// </summary>
        public bool IsForwardLeftDoorOpen => _isForwardLeftDoorOpen;
        
        /// <summary>
        /// Gets a value indicating whether the forward right door is open
        /// </summary>
        public bool IsForwardRightDoorOpen => _isForwardRightDoorOpen;

        /// <summary>
        /// Gets a value indicating whether the aft left door is open
        /// </summary>
        public bool IsAftLeftDoorOpen => _isAftLeftDoorOpen;
        
        /// <summary>
        /// Gets a value indicating whether the aft right door is open
        /// </summary>
        public bool IsAftRightDoorOpen => _isAftRightDoorOpen;

        /// <summary>
        /// Gets a value indicating whether the forward cargo door is open
        /// </summary>
        public bool IsForwardCargoDoorOpen => _isForwardCargoDoorOpen;

        /// <summary>
        /// Gets a value indicating whether the aft cargo door is open
        /// </summary>
        public bool IsAftCargoDoorOpen => _isAftCargoDoorOpen;
        
        /// <summary>
        /// Gets a value indicating whether the forward right service is active
        /// </summary>
        public bool IsForwardRightServiceActive => _isForwardRightServiceActive;

        /// <summary>
        /// Gets a value indicating whether the aft right service is active
        /// </summary>
        public bool IsAftRightServiceActive => _isAftRightServiceActive;

        /// <summary>
        /// Gets a value indicating whether the forward cargo service is active
        /// </summary>
        public bool IsForwardCargoServiceActive => _isForwardCargoServiceActive;

        /// <summary>
        /// Gets a value indicating whether the aft cargo service is active
        /// </summary>
        public bool IsAftCargoServiceActive => _isAftCargoServiceActive;

        /// <summary>
        /// Occurs when a door state changes
        /// </summary>
        public event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorManager"/> class
        /// </summary>
        /// <param name="prosimDoorService">The ProSim door service</param>
        /// <param name="model">The service model</param>
        public GSXDoorManager(IProsimDoorService prosimDoorService, ServiceModel model)
        {
            _prosimDoorService = prosimDoorService ?? throw new ArgumentNullException(nameof(prosimDoorService));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _simConnect = IPCManager.SimConnect;

            // Subscribe to door state changes from ProSim
            _prosimDoorService.DoorStateChanged += OnProsimDoorStateChanged;

            Logger.Log(LogLevel.Information, "GSXDoorManager:Constructor", "GSX Door Manager initialized");
        }

        /// <summary>
        /// Initializes the door manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    return;
                }

                // Subscribe to SimConnect variables
                _simConnect.SubscribeLvar("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE");
                _simConnect.SubscribeLvar("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE");
                _simConnect.SubscribeLvar("FSDT_GSX_AIRCRAFT_CARGO_1_TOGGLE");
                _simConnect.SubscribeLvar("FSDT_GSX_AIRCRAFT_CARGO_2_TOGGLE");
                _simConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
                _simConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");

                // Initialize door states to closed
                _isForwardLeftDoorOpen = false;
                _isForwardRightDoorOpen = false;
                _isAftLeftDoorOpen = false;
                _isAftRightDoorOpen = false;
                _isForwardCargoDoorOpen = false;
                _isAftCargoDoorOpen = false;
                
                // Initialize service states to inactive
                _isForwardRightServiceActive = false;
                _isAftRightServiceActive = false;
                _isForwardCargoServiceActive = false;
                _isAftCargoServiceActive = false;

                // Ensure ProSim knows the doors are closed
                _prosimDoorService.SetForwardLeftDoor(false);
                _prosimDoorService.SetForwardRightDoor(false);
                _prosimDoorService.SetAftLeftDoor(false);
                _prosimDoorService.SetAftRightDoor(false);
                _prosimDoorService.SetForwardCargoDoor(false);
                _prosimDoorService.SetAftCargoDoor(false);

                Logger.Log(LogLevel.Information, "GSXDoorManager:Initialize", 
                    $"Initial door states - ForwardLeft: {_isForwardLeftDoorOpen}, " +
                    $"ForwardRight: {_isForwardRightDoorOpen}, " +
                    $"AftLeft: {_isAftLeftDoorOpen}, " +
                    $"AftRight: {_isAftRightDoorOpen}, " +
                    $"ForwardCargo: {_isForwardCargoDoorOpen}, " +
                    $"AftCargo: {_isAftCargoDoorOpen}");
                
                // Clear any existing toggle-to-door mappings
                _toggleToDoorMapping.Clear();
                
                // Clear door change tracking
                _doorChangeTracking.Clear();

                _isInitialized = true;
                Logger.Log(LogLevel.Information, "GSXDoorManager:Initialize", 
                    "GSX Door Manager initialized with explicit door states");
            }
        }

        /// <summary>
        /// Opens a door
        /// </summary>
        /// <param name="doorType">The type of door to open</param>
        /// <returns>True if the door was opened successfully, false otherwise</returns>
        public bool OpenDoor(DoorType doorType)
        {
            EnsureInitialized();

            try
            {
                bool result = false;

                switch (doorType)
                {
                    case DoorType.ForwardRight:
                        _prosimDoorService.SetForwardRightDoor(true);
                        if (!_isForwardRightDoorOpen)
                        {
                            SetDoorState(doorType, true);
                        }
                        result = true;
                        break;
                    case DoorType.AftRight:
                        _prosimDoorService.SetAftRightDoor(true);
                        if (!_isAftRightDoorOpen)
                        {
                            SetDoorState(doorType, true);
                        }
                        result = true;
                        break;
                    case DoorType.ForwardCargo:
                        _prosimDoorService.SetForwardCargoDoor(true);
                        if (!_isForwardCargoDoorOpen)
                        {
                            SetDoorState(doorType, true);
                        }
                        result = true;
                        break;
                    case DoorType.AftCargo:
                        _prosimDoorService.SetAftCargoDoor(true);
                        if (!_isAftCargoDoorOpen)
                        {
                            SetDoorState(doorType, true);
                        }
                        result = true;
                        break;
                    default:
                        Logger.Log(LogLevel.Warning, "GSXDoorManager:OpenDoor", $"Unknown door type: {doorType}");
                        return false;
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXDoorManager:OpenDoor", $"Error opening door {doorType}: {ex.Message}");
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
            EnsureInitialized();

            try
            {
                bool result = false;

                switch (doorType)
                {
                    case DoorType.ForwardRight:
                        _prosimDoorService.SetForwardRightDoor(false);
                        if (_isForwardRightDoorOpen)
                        {
                            SetDoorState(doorType, false);
                        }
                        result = true;
                        break;
                    case DoorType.AftRight:
                        _prosimDoorService.SetAftRightDoor(false);
                        if (_isAftRightDoorOpen)
                        {
                            SetDoorState(doorType, false);
                        }
                        result = true;
                        break;
                    case DoorType.ForwardCargo:
                        _prosimDoorService.SetForwardCargoDoor(false);
                        if (_isForwardCargoDoorOpen)
                        {
                            SetDoorState(doorType, false);
                        }
                        result = true;
                        break;
                    case DoorType.AftCargo:
                        _prosimDoorService.SetAftCargoDoor(false);
                        if (_isAftCargoDoorOpen)
                        {
                            SetDoorState(doorType, false);
                        }
                        result = true;
                        break;
                    default:
                        Logger.Log(LogLevel.Warning, "GSXDoorManager:CloseDoor", $"Unknown door type: {doorType}");
                        return false;
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXDoorManager:CloseDoor", $"Error closing door {doorType}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles a service toggle from GSX
        /// </summary>
        /// <param name="serviceNumber">The service number (1 or 2)</param>
        /// <param name="isActive">True if the service is active, false otherwise</param>
        public void HandleServiceToggle(int serviceNumber, bool isActive)
        {
            EnsureInitialized();

            try
            {
                if (!_model.SetOpenCateringDoor)
                {
                    Logger.Log(LogLevel.Information, "GSXDoorManager:HandleServiceToggle", 
                        $"Automatic door handling for catering is disabled. Service {serviceNumber} toggle ignored.");
                    return;
                }

                // Determine which door this toggle controls
                DoorType doorType = DetermineDoorForToggle(serviceNumber, isActive);
                
                // Log the current state for debugging
                int cateringState = (int)_simConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
                Logger.Log(LogLevel.Debug, "GSXDoorManager:HandleServiceToggle", 
                    $"Handling service toggle {serviceNumber}={isActive} with catering state {cateringState} for door {doorType}");

                // Get the current door state and service state
                bool isDoorOpen = IsDoorOpen(doorType);
                bool isServiceActive = IsServiceActive(doorType);

                // Check if we should prevent rapid changes to this door
                if (ShouldPreventRapidChanges(doorType))
                {
                    Logger.Log(LogLevel.Warning, "GSXDoorManager:HandleServiceToggle", 
                        $"Preventing rapid changes to {doorType}");
                    return;
                }

                // Special handling for catering state 4 (service requested)
                if (cateringState == 4 && isActive && !isDoorOpen)
                {
                    // Catering is waiting for door to open
                    Logger.Log(LogLevel.Information, "GSXDoorManager:HandleServiceToggle", 
                        $"Catering service is waiting for door to open");
                    OpenDoor(doorType);
                    SetServiceActive(doorType, true);
                    return;
                }

                // Toggle is active (1) and door is closed and service not active -> Open door and start service
                if (isActive && !isDoorOpen && !isServiceActive)
                {
                    Logger.Log(LogLevel.Information, "GSXDoorManager:HandleServiceToggle", 
                        $"Opening {doorType} door in response to GSX ground crew request");
                    OpenDoor(doorType);
                    SetServiceActive(doorType, true);
                }
                // Toggle is inactive (0) and door is open and service active -> Service in progress
                else if (!isActive && isDoorOpen && isServiceActive)
                {
                    Logger.Log(LogLevel.Debug, "GSXDoorManager:HandleServiceToggle", 
                        $"{doorType} door service in progress");
                    // No action needed, service is in progress
                }
                // Toggle is active (1) again and door is open and service active -> Close door and end service
                else if (isActive && isDoorOpen && isServiceActive)
                {
                    Logger.Log(LogLevel.Information, "GSXDoorManager:HandleServiceToggle", 
                        $"Closing {doorType} door as service is complete");
                    CloseDoor(doorType);
                    SetServiceActive(doorType, false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXDoorManager:HandleServiceToggle", 
                    $"Error handling service toggle for service {serviceNumber}: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines which door a toggle controls
        /// </summary>
        /// <param name="toggleNumber">The toggle number (1 or 2)</param>
        /// <param name="isActive">True if the toggle is active, false otherwise</param>
        /// <returns>The door type that this toggle controls</returns>
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

        /// <summary>
        /// Checks if a door is open
        /// </summary>
        /// <param name="doorType">The door type</param>
        /// <returns>True if the door is open, false otherwise</returns>
        private bool IsDoorOpen(DoorType doorType)
        {
            switch (doorType)
            {
                case DoorType.ForwardRight: return _isForwardRightDoorOpen;
                case DoorType.AftRight: return _isAftRightDoorOpen;
                case DoorType.ForwardCargo: return _isForwardCargoDoorOpen;
                case DoorType.AftCargo: return _isAftCargoDoorOpen;
                default: return false;
            }
        }

        /// <summary>
        /// Checks if a service is active for a door
        /// </summary>
        /// <param name="doorType">The door type</param>
        /// <returns>True if the service is active, false otherwise</returns>
        private bool IsServiceActive(DoorType doorType)
        {
            switch (doorType)
            {
                case DoorType.ForwardRight: return _isForwardRightServiceActive;
                case DoorType.AftRight: return _isAftRightServiceActive;
                case DoorType.ForwardCargo: return _isForwardCargoServiceActive;
                case DoorType.AftCargo: return _isAftCargoServiceActive;
                default: return false;
            }
        }

        /// <summary>
        /// Sets the service active state for a door
        /// </summary>
        /// <param name="doorType">The door type</param>
        /// <param name="active">True to set the service active, false otherwise</param>
        private void SetServiceActive(DoorType doorType, bool active)
        {
            switch (doorType)
            {
                case DoorType.ForwardRight: _isForwardRightServiceActive = active; break;
                case DoorType.AftRight: _isAftRightServiceActive = active; break;
                case DoorType.ForwardCargo: _isForwardCargoServiceActive = active; break;
                case DoorType.AftCargo: _isAftCargoServiceActive = active; break;
            }
        }

        /// <summary>
        /// Checks if rapid changes to a door should be prevented
        /// </summary>
        /// <param name="doorType">The door type</param>
        /// <returns>True if rapid changes should be prevented, false otherwise</returns>
        private bool ShouldPreventRapidChanges(DoorType doorType)
        {
            var now = DateTime.UtcNow;
            if (!_doorChangeTracking.ContainsKey(doorType))
            {
                _doorChangeTracking[doorType] = (now, 1);
                return false;
            }

            DateTime lastChange;
            int changeCount;
            (lastChange, changeCount) = _doorChangeTracking[doorType];
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
        
        /// <summary>
        /// Handles a cargo door toggle from GSX
        /// </summary>
        /// <param name="cargoNumber">The cargo door number (1 for forward, 2 for aft)</param>
        /// <param name="isActive">True if the toggle is active, false otherwise</param>
        public void HandleCargoDoorToggle(int cargoNumber, bool isActive)
        {
            EnsureInitialized();

            try
            {
                if (!_model.SetOpenCargoDoors)
                {
                    Logger.Log(LogLevel.Information, "GSXDoorManager:HandleCargoDoorToggle", 
                        $"Automatic door handling for cargo is disabled. Cargo door {cargoNumber} toggle ignored.");
                    return;
                }

                switch (cargoNumber)
                {
                    case 1: // Forward cargo door
                        // Toggle is active (1) and door is closed and service not active -> Open door and start service
                        if (isActive && !_isForwardCargoDoorOpen && !_isForwardCargoServiceActive)
                        {
                            Logger.Log(LogLevel.Information, "GSXDoorManager:HandleCargoDoorToggle", 
                                $"Opening forward cargo door in response to GSX ground crew request");
                            OpenDoor(DoorType.ForwardCargo);
                            _isForwardCargoServiceActive = true;
                        }
                        // Toggle is inactive (0) and door is open and service active -> Service in progress
                        else if (!isActive && _isForwardCargoDoorOpen && _isForwardCargoServiceActive)
                        {
                            Logger.Log(LogLevel.Debug, "GSXDoorManager:HandleCargoDoorToggle", 
                                $"Forward cargo door service in progress");
                            // No action needed, service is in progress
                        }
                        // Toggle is active (1) again and door is open and service active -> Close door and end service
                        else if (isActive && _isForwardCargoDoorOpen && _isForwardCargoServiceActive)
                        {
                            Logger.Log(LogLevel.Information, "GSXDoorManager:HandleCargoDoorToggle", 
                                $"Closing forward cargo door as service is complete");
                            CloseDoor(DoorType.ForwardCargo);
                            _isForwardCargoServiceActive = false;
                        }
                        break;

                    case 2: // Aft cargo door
                        // Toggle is active (1) and door is closed and service not active -> Open door and start service
                        if (isActive && !_isAftCargoDoorOpen && !_isAftCargoServiceActive)
                        {
                            Logger.Log(LogLevel.Information, "GSXDoorManager:HandleCargoDoorToggle", 
                                $"Opening aft cargo door in response to GSX ground crew request");
                            OpenDoor(DoorType.AftCargo);
                            _isAftCargoServiceActive = true;
                        }
                        // Toggle is inactive (0) and door is open and service active -> Service in progress
                        else if (!isActive && _isAftCargoDoorOpen && _isAftCargoServiceActive)
                        {
                            Logger.Log(LogLevel.Debug, "GSXDoorManager:HandleCargoDoorToggle", 
                                $"Aft cargo door service in progress");
                            // No action needed, service is in progress
                        }
                        // Toggle is active (1) again and door is open and service active -> Close door and end service
                        else if (isActive && _isAftCargoDoorOpen && _isAftCargoServiceActive)
                        {
                            Logger.Log(LogLevel.Information, "GSXDoorManager:HandleCargoDoorToggle", 
                                $"Closing aft cargo door as service is complete");
                            CloseDoor(DoorType.AftCargo);
                            _isAftCargoServiceActive = false;
                        }
                        break;

                    default:
                        Logger.Log(LogLevel.Warning, "GSXDoorManager:HandleCargoDoorToggle", 
                            $"Unknown cargo door number: {cargoNumber}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXDoorManager:HandleCargoDoorToggle", 
                    $"Error handling cargo door toggle for door {cargoNumber}: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles cargo loading percentage updates
        /// </summary>
        /// <param name="cargoLoadingPercent">The cargo loading percentage (0-100)</param>
        public void HandleCargoLoading(int cargoLoadingPercent)
        {
            EnsureInitialized();

            try
            {
                if (!_model.SetOpenCargoDoors)
                {
                    return;
                }

                // If cargo loading is starting, open cargo doors
                if (cargoLoadingPercent > 0 && cargoLoadingPercent < 100)
                {
                    if (!_isForwardCargoDoorOpen)
                    {
                        Logger.Log(LogLevel.Information, "GSXDoorManager:HandleCargoLoading", 
                            $"Opening forward cargo door for loading (cargo at {cargoLoadingPercent}%)");
                        OpenDoor(DoorType.ForwardCargo);
                    }

                    if (!_isAftCargoDoorOpen)
                    {
                        Logger.Log(LogLevel.Information, "GSXDoorManager:HandleCargoLoading", 
                            $"Opening aft cargo door for loading (cargo at {cargoLoadingPercent}%)");
                        OpenDoor(DoorType.AftCargo);
                    }
                }
                // If cargo loading is complete, close cargo doors
                else if (cargoLoadingPercent == 100)
                {
                    if (_isForwardCargoDoorOpen)
                    {
                        Logger.Log(LogLevel.Information, "GSXDoorManager:HandleCargoLoading", 
                            $"Closing forward cargo door after loading (cargo at 100%)");
                        CloseDoor(DoorType.ForwardCargo);
                    }

                    if (_isAftCargoDoorOpen)
                    {
                        Logger.Log(LogLevel.Information, "GSXDoorManager:HandleCargoLoading", 
                            $"Closing aft cargo door after loading (cargo at 100%)");
                        CloseDoor(DoorType.AftCargo);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXDoorManager:HandleCargoLoading", 
                    $"Error handling cargo loading percentage {cargoLoadingPercent}%: {ex.Message}");
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
            return await Task.Run(() => OpenDoor(doorType), cancellationToken);
        }

        /// <summary>
        /// Closes a door asynchronously
        /// </summary>
        /// <param name="doorType">The type of door to close</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the door was closed successfully, false otherwise</returns>
        public async Task<bool> CloseDoorAsync(DoorType doorType, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => CloseDoor(doorType), cancellationToken);
        }

        /// <summary>
        /// Handles a service toggle from GSX asynchronously
        /// </summary>
        /// <param name="serviceNumber">The service number (1 or 2)</param>
        /// <param name="isActive">True if the service is active, false otherwise</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleServiceToggleAsync(int serviceNumber, bool isActive, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => HandleServiceToggle(serviceNumber, isActive), cancellationToken);
        }
        
        /// <summary>
        /// Handles a cargo door toggle from GSX asynchronously
        /// </summary>
        /// <param name="cargoNumber">The cargo door number (1 for forward, 2 for aft)</param>
        /// <param name="isActive">True if the toggle is active, false otherwise</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleCargoDoorToggleAsync(int cargoNumber, bool isActive, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => HandleCargoDoorToggle(cargoNumber, isActive), cancellationToken);
        }

        /// <summary>
        /// Handles cargo loading percentage updates asynchronously
        /// </summary>
        /// <param name="cargoLoadingPercent">The cargo loading percentage (0-100)</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleCargoLoadingAsync(int cargoLoadingPercent, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => HandleCargoLoading(cargoLoadingPercent), cancellationToken);
        }

        /// <summary>
        /// Handles door state changes from ProSim
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void OnProsimDoorStateChanged(object sender, DoorStateChangedEventArgs e)
        {
            // Update our internal state to match ProSim's state
            switch (e.DoorType)
            {
                case DoorType.ForwardRight:
                    SetDoorState(DoorType.ForwardRight, e.IsOpen);
                    break;
                case DoorType.AftRight:
                    SetDoorState(DoorType.AftRight, e.IsOpen);
                    break;
                case DoorType.ForwardCargo:
                    SetDoorState(DoorType.ForwardCargo, e.IsOpen);
                    break;
                case DoorType.AftCargo:
                    SetDoorState(DoorType.AftCargo, e.IsOpen);
                    break;
            }
        }

        /// <summary>
        /// Sets the state of a door and raises the DoorStateChanged event if the state has changed
        /// </summary>
        /// <param name="doorType">The type of door</param>
        /// <param name="isOpen">True if the door is open, false otherwise</param>
        private void SetDoorState(DoorType doorType, bool isOpen)
        {
            bool stateChanged = false;

            lock (_lockObject)
            {
                switch (doorType)
                {
                    case DoorType.ForwardLeft:
                        stateChanged = _isForwardLeftDoorOpen != isOpen;
                        _isForwardLeftDoorOpen = isOpen;
                        break;
                    case DoorType.ForwardRight:
                        stateChanged = _isForwardRightDoorOpen != isOpen;
                        _isForwardRightDoorOpen = isOpen;
                        break;
                    case DoorType.AftLeft:
                        stateChanged = _isAftLeftDoorOpen != isOpen;
                        _isAftLeftDoorOpen = isOpen;
                        break;
                    case DoorType.AftRight:
                        stateChanged = _isAftRightDoorOpen != isOpen;
                        _isAftRightDoorOpen = isOpen;
                        break;
                    case DoorType.ForwardCargo:
                        stateChanged = _isForwardCargoDoorOpen != isOpen;
                        _isForwardCargoDoorOpen = isOpen;
                        break;
                    case DoorType.AftCargo:
                        stateChanged = _isAftCargoDoorOpen != isOpen;
                        _isAftCargoDoorOpen = isOpen;
                        break;
                }
            }

            if (stateChanged)
            {
                Logger.Log(LogLevel.Information, "GSXDoorManager:SetDoorState", 
                    $"Door {doorType} state changed to {(isOpen ? "open" : "closed")}");
                OnDoorStateChanged(new DoorStateChangedEventArgs(doorType, isOpen));
            }
        }

        /// <summary>
        /// Raises the DoorStateChanged event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDoorStateChanged(DoorStateChangedEventArgs e)
        {
            DoorStateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Ensures that the door manager is initialized
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }
    }
}
