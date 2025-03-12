using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Mock implementation of IProsimDoorService for use when the real service is not available
    /// </summary>
    public class MockProsimDoorService : IProsimDoorService
    {
        private readonly ILogger _logger;
        private bool _forwardLeftDoorOpen = false;
        private bool _forwardRightDoorOpen = false;
        private bool _aftLeftDoorOpen = false;
        private bool _aftRightDoorOpen = false;
        private bool _forwardCargoDoorOpen = false;
        private bool _aftCargoDoorOpen = false;

        /// <summary>
        /// Event that fires when a door state changes
        /// </summary>
        public event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockProsimDoorService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public MockProsimDoorService(ILogger logger)
        {
            _logger = logger;
            _logger?.Log(LogLevel.Warning, "MockProsimDoorService:Constructor", 
                "Using mock door service. Door operations will not affect the actual aircraft.");
        }

        /// <summary>
        /// Initializes all door states to a known state (closed)
        /// </summary>
        public void InitializeDoorStates()
        {
            _forwardLeftDoorOpen = false;
            _forwardRightDoorOpen = false;
            _aftLeftDoorOpen = false;
            _aftRightDoorOpen = false;
            _forwardCargoDoorOpen = false;
            _aftCargoDoorOpen = false;
            _logger?.Log(LogLevel.Debug, "MockProsimDoorService:InitializeDoorStates", "All doors initialized to closed state (mock)");
        }

        /// <summary>
        /// Gets a value indicating whether the forward left door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsForwardLeftDoorOpen()
        {
            return _forwardLeftDoorOpen;
        }

        /// <summary>
        /// Gets a value indicating whether the forward right door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsForwardRightDoorOpen()
        {
            return _forwardRightDoorOpen;
        }

        /// <summary>
        /// Gets a value indicating whether the aft left door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsAftLeftDoorOpen()
        {
            return _aftLeftDoorOpen;
        }

        /// <summary>
        /// Gets a value indicating whether the aft right door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsAftRightDoorOpen()
        {
            return _aftRightDoorOpen;
        }

        /// <summary>
        /// Gets a value indicating whether the forward cargo door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsForwardCargoDoorOpen()
        {
            return _forwardCargoDoorOpen;
        }

        /// <summary>
        /// Gets a value indicating whether the aft cargo door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsAftCargoDoorOpen()
        {
            return _aftCargoDoorOpen;
        }

        /// <summary>
        /// Sets the state of the forward left door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        public void SetForwardLeftDoor(bool open)
        {
            if (_forwardLeftDoorOpen != open)
            {
                _forwardLeftDoorOpen = open;
                _logger?.Log(LogLevel.Debug, "MockProsimDoorService:SetForwardLeftDoor", 
                    $"Forward left door set to {(open ? "open" : "closed")} (mock)");
                OnDoorStateChanged(DoorType.ForwardLeft, open);
            }
        }

        /// <summary>
        /// Sets the state of the forward right door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        public void SetForwardRightDoor(bool open)
        {
            if (_forwardRightDoorOpen != open)
            {
                _forwardRightDoorOpen = open;
                _logger?.Log(LogLevel.Debug, "MockProsimDoorService:SetForwardRightDoor", 
                    $"Forward right door set to {(open ? "open" : "closed")} (mock)");
                OnDoorStateChanged(DoorType.ForwardRight, open);
            }
        }

        /// <summary>
        /// Sets the state of the aft left door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        public void SetAftLeftDoor(bool open)
        {
            if (_aftLeftDoorOpen != open)
            {
                _aftLeftDoorOpen = open;
                _logger?.Log(LogLevel.Debug, "MockProsimDoorService:SetAftLeftDoor", 
                    $"Aft left door set to {(open ? "open" : "closed")} (mock)");
                OnDoorStateChanged(DoorType.AftLeft, open);
            }
        }

        /// <summary>
        /// Sets the state of the aft right door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        public void SetAftRightDoor(bool open)
        {
            if (_aftRightDoorOpen != open)
            {
                _aftRightDoorOpen = open;
                _logger?.Log(LogLevel.Debug, "MockProsimDoorService:SetAftRightDoor", 
                    $"Aft right door set to {(open ? "open" : "closed")} (mock)");
                OnDoorStateChanged(DoorType.AftRight, open);
            }
        }

        /// <summary>
        /// Sets the state of the forward cargo door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        public void SetForwardCargoDoor(bool open)
        {
            if (_forwardCargoDoorOpen != open)
            {
                _forwardCargoDoorOpen = open;
                _logger?.Log(LogLevel.Debug, "MockProsimDoorService:SetForwardCargoDoor", 
                    $"Forward cargo door set to {(open ? "open" : "closed")} (mock)");
                OnDoorStateChanged(DoorType.ForwardCargo, open);
            }
        }

        /// <summary>
        /// Sets the state of the aft cargo door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        public void SetAftCargoDoor(bool open)
        {
            if (_aftCargoDoorOpen != open)
            {
                _aftCargoDoorOpen = open;
                _logger?.Log(LogLevel.Debug, "MockProsimDoorService:SetAftCargoDoor", 
                    $"Aft cargo door set to {(open ? "open" : "closed")} (mock)");
                OnDoorStateChanged(DoorType.AftCargo, open);
            }
        }

        /// <summary>
        /// Toggles the state of the forward left door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleForwardLeftDoor()
        {
            _forwardLeftDoorOpen = !_forwardLeftDoorOpen;
            _logger?.Log(LogLevel.Debug, "MockProsimDoorService:ToggleForwardLeftDoor", 
                $"Forward left door toggled to {(_forwardLeftDoorOpen ? "open" : "closed")} (mock)");
            OnDoorStateChanged(DoorType.ForwardLeft, _forwardLeftDoorOpen);
            return _forwardLeftDoorOpen;
        }

        /// <summary>
        /// Toggles the state of the forward right door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleForwardRightDoor()
        {
            _forwardRightDoorOpen = !_forwardRightDoorOpen;
            _logger?.Log(LogLevel.Debug, "MockProsimDoorService:ToggleForwardRightDoor", 
                $"Forward right door toggled to {(_forwardRightDoorOpen ? "open" : "closed")} (mock)");
            OnDoorStateChanged(DoorType.ForwardRight, _forwardRightDoorOpen);
            return _forwardRightDoorOpen;
        }

        /// <summary>
        /// Toggles the state of the aft left door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleAftLeftDoor()
        {
            _aftLeftDoorOpen = !_aftLeftDoorOpen;
            _logger?.Log(LogLevel.Debug, "MockProsimDoorService:ToggleAftLeftDoor", 
                $"Aft left door toggled to {(_aftLeftDoorOpen ? "open" : "closed")} (mock)");
            OnDoorStateChanged(DoorType.AftLeft, _aftLeftDoorOpen);
            return _aftLeftDoorOpen;
        }

        /// <summary>
        /// Toggles the state of the aft right door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleAftRightDoor()
        {
            _aftRightDoorOpen = !_aftRightDoorOpen;
            _logger?.Log(LogLevel.Debug, "MockProsimDoorService:ToggleAftRightDoor", 
                $"Aft right door toggled to {(_aftRightDoorOpen ? "open" : "closed")} (mock)");
            OnDoorStateChanged(DoorType.AftRight, _aftRightDoorOpen);
            return _aftRightDoorOpen;
        }

        /// <summary>
        /// Toggles the state of the forward cargo door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleForwardCargoDoor()
        {
            _forwardCargoDoorOpen = !_forwardCargoDoorOpen;
            _logger?.Log(LogLevel.Debug, "MockProsimDoorService:ToggleForwardCargoDoor", 
                $"Forward cargo door toggled to {(_forwardCargoDoorOpen ? "open" : "closed")} (mock)");
            OnDoorStateChanged(DoorType.ForwardCargo, _forwardCargoDoorOpen);
            return _forwardCargoDoorOpen;
        }

        /// <summary>
        /// Toggles the state of the aft cargo door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleAftCargoDoor()
        {
            _aftCargoDoorOpen = !_aftCargoDoorOpen;
            _logger?.Log(LogLevel.Debug, "MockProsimDoorService:ToggleAftCargoDoor", 
                $"Aft cargo door toggled to {(_aftCargoDoorOpen ? "open" : "closed")} (mock)");
            OnDoorStateChanged(DoorType.AftCargo, _aftCargoDoorOpen);
            return _aftCargoDoorOpen;
        }

        /// <summary>
        /// Raises the DoorStateChanged event
        /// </summary>
        /// <param name="doorType">The type of door that changed state</param>
        /// <param name="isOpen">The new state of the door</param>
        protected virtual void OnDoorStateChanged(DoorType doorType, bool isOpen)
        {
            DoorStateChanged?.Invoke(this, new DoorStateChangedEventArgs(doorType, isOpen));
        }
    }
}
