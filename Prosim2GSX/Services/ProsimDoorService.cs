using System;

namespace Prosim2GSX.Services
{
    public class ProsimDoorService : IProsimDoorService
    {
        private readonly IProsimService _prosimService;
        
        public event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
        
        public ProsimDoorService(IProsimService prosimService)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
        }
        
        /// <summary>
        /// Sets the state of the forward left door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        public void SetForwardLeftDoor(bool open)
        {
            // Check current state before changing
            var currentState = _prosimService.ReadDataRef("doors.entry.left.fwd");
            if ((bool)currentState == open)
            {
                // Door is already in the requested state, don't change it
                Logger.Log(LogLevel.Debug, "ProsimDoorService:SetForwardLeftDoor", 
                    $"Door already {(open ? "open" : "closed")}, no change needed");
                return;
            }

            _prosimService.SetVariable("doors.entry.left.fwd", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetForwardLeftDoor", $"Forward left door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("ForwardLeftDoor", open);
        }
        
        /// <summary>
        /// Sets the state of the aft left door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        public void SetAftLeftDoor(bool open)
        {
            // Check current state before changing
            var currentState = _prosimService.ReadDataRef("doors.entry.left.aft");
            if ((bool)currentState == open)
            {
                // Door is already in the requested state, don't change it
                Logger.Log(LogLevel.Debug, "ProsimDoorService:SetAftLeftDoor", 
                    $"Door already {(open ? "open" : "closed")}, no change needed");
                return;
            }

            _prosimService.SetVariable("doors.entry.left.aft", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetAftLeftDoor", $"Aft left door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("AftLeftDoor", open);
        }
        
        /// <summary>
        /// Initializes all door states to a known state (closed)
        /// </summary>
        public void InitializeDoorStates()
        {
            try
            {
                // Read current states from ProSim
                var forwardRightState = (bool)_prosimService.ReadDataRef("doors.entry.right.fwd");
                var aftRightState = (bool)_prosimService.ReadDataRef("doors.entry.right.aft");
                var forwardCargoState = (bool)_prosimService.ReadDataRef("doors.cargo.forward");
                var aftCargoState = (bool)_prosimService.ReadDataRef("doors.cargo.aft");
                
                Logger.Log(LogLevel.Information, "ProsimDoorService:InitializeDoorStates", 
                    $"Current door states - ForwardRight: {forwardRightState}, " +
                    $"AftRight: {aftRightState}, " +
                    $"ForwardCargo: {forwardCargoState}, " +
                    $"AftCargo: {aftCargoState}");
                
                // Set all doors to closed state
                if (forwardRightState)
                    SetForwardRightDoor(false);
                
                if (aftRightState)
                    SetAftRightDoor(false);
                
                if (forwardCargoState)
                    SetForwardCargoDoor(false);
                
                if (aftCargoState)
                    SetAftCargoDoor(false);
                
                Logger.Log(LogLevel.Information, "ProsimDoorService:InitializeDoorStates", 
                    "All doors explicitly initialized to closed state");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:InitializeDoorStates", 
                    $"Error initializing door states: {ex.Message}");
            }
        }
        
        public void SetAftRightDoor(bool open)
        {
            // Check current state before changing
            var currentState = _prosimService.ReadDataRef("doors.entry.right.aft");
            if ((bool)currentState == open)
            {
                // Door is already in the requested state, don't change it
                Logger.Log(LogLevel.Debug, "ProsimDoorService:SetAftRightDoor", 
                    $"Door already {(open ? "open" : "closed")}, no change needed");
                return;
            }

            _prosimService.SetVariable("doors.entry.right.aft", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetAftRightDoor", $"Aft right door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("AftRightDoor", open);
        }
        
        public void SetForwardRightDoor(bool open)
        {
            // Check current state before changing
            var currentState = _prosimService.ReadDataRef("doors.entry.right.fwd");
            if ((bool)currentState == open)
            {
                // Door is already in the requested state, don't change it
                Logger.Log(LogLevel.Debug, "ProsimDoorService:SetForwardRightDoor", 
                    $"Door already {(open ? "open" : "closed")}, no change needed");
                return;
            }

            _prosimService.SetVariable("doors.entry.right.fwd", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetForwardRightDoor", $"Forward right door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("ForwardRightDoor", open);
        }
        
        public void SetForwardCargoDoor(bool open)
        {
            // Check current state before changing
            var currentState = _prosimService.ReadDataRef("doors.cargo.forward");
            if ((bool)currentState == open)
            {
                // Door is already in the requested state, don't change it
                Logger.Log(LogLevel.Debug, "ProsimDoorService:SetForwardCargoDoor", 
                    $"Door already {(open ? "open" : "closed")}, no change needed");
                return;
            }

            _prosimService.SetVariable("doors.cargo.forward", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetForwardCargoDoor", $"Forward cargo door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("ForwardCargoDoor", open);
        }
        
        public void SetAftCargoDoor(bool open)
        {
            // Check current state before changing
            var currentState = _prosimService.ReadDataRef("doors.cargo.aft");
            if ((bool)currentState == open)
            {
                // Door is already in the requested state, don't change it
                Logger.Log(LogLevel.Debug, "ProsimDoorService:SetAftCargoDoor", 
                    $"Door already {(open ? "open" : "closed")}, no change needed");
                return;
            }

            _prosimService.SetVariable("doors.cargo.aft", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetAftCargoDoor", $"Aft cargo door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("AftCargoDoor", open);
        }
        
        /// <summary>
        /// Gets a value indicating whether the forward left door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsForwardLeftDoorOpen()
        {
            try
            {
                var state = _prosimService.ReadDataRef("doors.entry.left.fwd");
                return (bool)state;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:IsForwardLeftDoorOpen", 
                    $"Error reading door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the forward right door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsForwardRightDoorOpen()
        {
            try
            {
                var state = _prosimService.ReadDataRef("doors.entry.right.fwd");
                return (bool)state;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:IsForwardRightDoorOpen", 
                    $"Error reading door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the aft left door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsAftLeftDoorOpen()
        {
            try
            {
                var state = _prosimService.ReadDataRef("doors.entry.left.aft");
                return (bool)state;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:IsAftLeftDoorOpen", 
                    $"Error reading door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the aft right door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsAftRightDoorOpen()
        {
            try
            {
                var state = _prosimService.ReadDataRef("doors.entry.right.aft");
                return (bool)state;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:IsAftRightDoorOpen", 
                    $"Error reading door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the forward cargo door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsForwardCargoDoorOpen()
        {
            try
            {
                var state = _prosimService.ReadDataRef("doors.cargo.forward");
                return (bool)state;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:IsForwardCargoDoorOpen", 
                    $"Error reading door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the aft cargo door is open
        /// </summary>
        /// <returns>True if the door is open, false otherwise</returns>
        public bool IsAftCargoDoorOpen()
        {
            try
            {
                var state = _prosimService.ReadDataRef("doors.cargo.aft");
                return (bool)state;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:IsAftCargoDoorOpen", 
                    $"Error reading door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Toggles the state of the forward left door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleForwardLeftDoor()
        {
            try
            {
                bool currentState = IsForwardLeftDoorOpen();
                SetForwardLeftDoor(!currentState);
                return !currentState;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:ToggleForwardLeftDoor", 
                    $"Error toggling door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Toggles the state of the forward right door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleForwardRightDoor()
        {
            try
            {
                bool currentState = IsForwardRightDoorOpen();
                SetForwardRightDoor(!currentState);
                return !currentState;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:ToggleForwardRightDoor", 
                    $"Error toggling door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Toggles the state of the aft left door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleAftLeftDoor()
        {
            try
            {
                bool currentState = IsAftLeftDoorOpen();
                SetAftLeftDoor(!currentState);
                return !currentState;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:ToggleAftLeftDoor", 
                    $"Error toggling door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Toggles the state of the aft right door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleAftRightDoor()
        {
            try
            {
                bool currentState = IsAftRightDoorOpen();
                SetAftRightDoor(!currentState);
                return !currentState;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:ToggleAftRightDoor", 
                    $"Error toggling door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Toggles the state of the forward cargo door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleForwardCargoDoor()
        {
            try
            {
                bool currentState = IsForwardCargoDoorOpen();
                SetForwardCargoDoor(!currentState);
                return !currentState;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:ToggleForwardCargoDoor", 
                    $"Error toggling door state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Toggles the state of the aft cargo door
        /// </summary>
        /// <returns>The new state of the door (true if open, false if closed)</returns>
        public bool ToggleAftCargoDoor()
        {
            try
            {
                bool currentState = IsAftCargoDoorOpen();
                SetAftCargoDoor(!currentState);
                return !currentState;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimDoorService:ToggleAftCargoDoor", 
                    $"Error toggling door state: {ex.Message}");
                return false;
            }
        }
        
        protected virtual void OnDoorStateChanged(string doorName, bool isOpen)
        {
            DoorType doorType;
            switch (doorName)
            {
                case "ForwardLeftDoor":
                    doorType = DoorType.ForwardLeft;
                    break;
                case "ForwardRightDoor":
                    doorType = DoorType.ForwardRight;
                    break;
                case "AftLeftDoor":
                    doorType = DoorType.AftLeft;
                    break;
                case "AftRightDoor":
                    doorType = DoorType.AftRight;
                    break;
                case "ForwardCargoDoor":
                    doorType = DoorType.ForwardCargo;
                    break;
                case "AftCargoDoor":
                    doorType = DoorType.AftCargo;
                    break;
                default:
                    Logger.Log(LogLevel.Warning, "ProsimDoorService:OnDoorStateChanged", $"Unknown door name: {doorName}");
                    return;
            }
            
            DoorStateChanged?.Invoke(this, new DoorStateChangedEventArgs(doorType, isOpen));
        }
    }
}
