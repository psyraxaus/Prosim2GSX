using System;
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

        private bool _isForwardRightDoorOpen;
        private bool _isAftRightDoorOpen;
        private bool _isForwardCargoDoorOpen;
        private bool _isAftCargoDoorOpen;
        private bool _isInitialized;

        /// <summary>
        /// Gets a value indicating whether the forward right door is open
        /// </summary>
        public bool IsForwardRightDoorOpen => _isForwardRightDoorOpen;

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
                _simConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
                _simConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");

                // Initialize door states to closed
                _isForwardRightDoorOpen = false;
                _isAftRightDoorOpen = false;
                _isForwardCargoDoorOpen = false;
                _isAftCargoDoorOpen = false;

                _isInitialized = true;
                Logger.Log(LogLevel.Information, "GSXDoorManager:Initialize", "GSX Door Manager initialized");
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
                        $"Automatic door opening for catering is disabled. Service {serviceNumber} toggle ignored.");
                    return;
                }

                switch (serviceNumber)
                {
                    case 1:
                        if (isActive)
                        {
                            if (!_isForwardRightDoorOpen)
                            {
                                Logger.Log(LogLevel.Information, "GSXDoorManager:HandleServiceToggle", 
                                    $"Opening forward right door for service {serviceNumber}");
                                OpenDoor(DoorType.ForwardRight);
                            }
                        }
                        else
                        {
                            if (_isForwardRightDoorOpen)
                            {
                                Logger.Log(LogLevel.Information, "GSXDoorManager:HandleServiceToggle", 
                                    $"Closing forward right door after service {serviceNumber}");
                                CloseDoor(DoorType.ForwardRight);
                            }
                        }
                        break;
                    case 2:
                        if (isActive)
                        {
                            if (!_isAftRightDoorOpen)
                            {
                                Logger.Log(LogLevel.Information, "GSXDoorManager:HandleServiceToggle", 
                                    $"Opening aft right door for service {serviceNumber}");
                                OpenDoor(DoorType.AftRight);
                            }
                        }
                        else
                        {
                            if (_isAftRightDoorOpen)
                            {
                                Logger.Log(LogLevel.Information, "GSXDoorManager:HandleServiceToggle", 
                                    $"Closing aft right door after service {serviceNumber}");
                                CloseDoor(DoorType.AftRight);
                            }
                        }
                        break;
                    default:
                        Logger.Log(LogLevel.Warning, "GSXDoorManager:HandleServiceToggle", 
                            $"Unknown service number: {serviceNumber}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXDoorManager:HandleServiceToggle", 
                    $"Error handling service toggle for service {serviceNumber}: {ex.Message}");
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
                    case DoorType.ForwardRight:
                        stateChanged = _isForwardRightDoorOpen != isOpen;
                        _isForwardRightDoorOpen = isOpen;
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
            EventHandler<DoorStateChangedEventArgs> handler = DoorStateChanged;
            if (handler != null)
            {
                try
                {
                    handler(this, e);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "GSXDoorManager:OnDoorStateChanged", 
                        $"Error raising DoorStateChanged event: {ex.Message}");
                }
            }
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
