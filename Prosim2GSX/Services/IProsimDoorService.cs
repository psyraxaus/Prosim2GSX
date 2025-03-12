using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for aircraft door control operations
    /// </summary>
    public interface IProsimDoorService
    {
        /// <summary>
        /// Sets the state of the aft right door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        void SetAftRightDoor(bool open);
        
        /// <summary>
        /// Sets the state of the forward right door
        /// </summary>
        /// <param name="open">True to open the door, false to close it</param>
        void SetForwardRightDoor(bool open);
        
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
}
}
