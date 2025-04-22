namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for controlling aircraft doors in ProSim
    /// </summary>
    public interface IDoorControlService
    {
        /// <summary>
        /// Set forward right door state
        /// </summary>
        /// <param name="open">True to open, false to close</param>
        void SetForwardRightDoor(bool open);

        /// <summary>
        /// Get forward right door state
        /// </summary>
        /// <returns>Door state ("open" or "closed")</returns>
        string GetForwardRightDoor();

        /// <summary>
        /// Set aft right door state
        /// </summary>
        /// <param name="open">True to open, false to close</param>
        void SetAftRightDoor(bool open);

        /// <summary>
        /// Get aft right door state
        /// </summary>
        /// <returns>Door state ("open" or "closed")</returns>
        string GetAftRightDoor();

        /// <summary>
        /// Set forward cargo door state
        /// </summary>
        /// <param name="open">True to open, false to close</param>
        void SetForwardCargoDoor(bool open);

        /// <summary>
        /// Get forward cargo door state
        /// </summary>
        /// <returns>Door state ("open" or "closed")</returns>
        string GetForwardCargoDoor();

        /// <summary>
        /// Set aft cargo door state
        /// </summary>
        /// <param name="open">True to open, false to close</param>
        void SetAftCargoDoor(bool open);

        /// <summary>
        /// Get aft cargo door state
        /// </summary>
        /// <returns>Door state ("open" or "closed")</returns>
        string GetAftCargoDoor();
    }
}