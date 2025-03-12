using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for aircraft door control operations
    /// </summary>
    public interface IProsimDoorService
    {
        /// <summary>
        /// Sets the state of the forward left door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        void SetForwardLeftDoor(bool open);
        
        /// <summary>
        /// Sets the state of the forward right door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        void SetForwardRightDoor(bool open);
        
        /// <summary>
        /// Sets the state of the aft left door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        void SetAftLeftDoor(bool open);
        
        /// <summary>
        /// Sets the state of the aft right door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        void SetAftRightDoor(bool open);
        
        /// <summary>
        /// Sets the state of the forward cargo door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        void SetForwardCargoDoor(bool open);
        
        /// <summary>
        /// Sets the state of the aft cargo door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        void SetAftCargoDoor(bool open);
        
    /// <summary>
    /// Event that fires when a door state changes
    /// </summary>
    event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
    
    /// <summary>
    /// Initializes all door states to a known state (closed)
    /// </summary>
    void InitializeDoorStates();
    
    /// <summary>
    /// Gets a value indicating whether the forward left door is open
    /// </summary>
    /// <returns>True if the door is open, false otherwise</returns>
    bool IsForwardLeftDoorOpen();
    
    /// <summary>
    /// Gets a value indicating whether the forward right door is open
    /// </summary>
    /// <returns>True if the door is open, false otherwise</returns>
    bool IsForwardRightDoorOpen();
    
    /// <summary>
    /// Gets a value indicating whether the aft left door is open
    /// </summary>
    /// <returns>True if the door is open, false otherwise</returns>
    bool IsAftLeftDoorOpen();
    
    /// <summary>
    /// Gets a value indicating whether the aft right door is open
    /// </summary>
    /// <returns>True if the door is open, false otherwise</returns>
    bool IsAftRightDoorOpen();
    
    /// <summary>
    /// Gets a value indicating whether the forward cargo door is open
    /// </summary>
    /// <returns>True if the door is open, false otherwise</returns>
    bool IsForwardCargoDoorOpen();
    
    /// <summary>
    /// Gets a value indicating whether the aft cargo door is open
    /// </summary>
    /// <returns>True if the door is open, false otherwise</returns>
    bool IsAftCargoDoorOpen();
    
    /// <summary>
    /// Toggles the state of the forward left door
    /// </summary>
    /// <returns>The new state of the door (true if open, false if closed)</returns>
    bool ToggleForwardLeftDoor();
    
    /// <summary>
    /// Toggles the state of the forward right door
    /// </summary>
    /// <returns>The new state of the door (true if open, false if closed)</returns>
    bool ToggleForwardRightDoor();
    
    /// <summary>
    /// Toggles the state of the aft left door
    /// </summary>
    /// <returns>The new state of the door (true if open, false if closed)</returns>
    bool ToggleAftLeftDoor();
    
    /// <summary>
    /// Toggles the state of the aft right door
    /// </summary>
    /// <returns>The new state of the door (true if open, false if closed)</returns>
    bool ToggleAftRightDoor();
    
    /// <summary>
    /// Toggles the state of the forward cargo door
    /// </summary>
    /// <returns>The new state of the door (true if open, false if closed)</returns>
    bool ToggleForwardCargoDoor();
    
    /// <summary>
    /// Toggles the state of the aft cargo door
    /// </summary>
    /// <returns>The new state of the door (true if open, false if closed)</returns>
    bool ToggleAftCargoDoor();
}
}
