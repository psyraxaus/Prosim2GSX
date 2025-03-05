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
    }
    
    /// <summary>
    /// Event arguments for door state change events
    /// </summary>
    public class DoorStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the name of the door that changed state
        /// </summary>
        public string DoorName { get; }
        
        /// <summary>
        /// Gets the new state of the door
        /// </summary>
        public bool IsOpen { get; }
        
        public DoorStateChangedEventArgs(string doorName, bool isOpen)
        {
            DoorName = doorName;
            IsOpen = isOpen;
        }
    }
}
