namespace Prosim2GSX.Services.Prosim.Enums
{
    /// <summary>
    /// The state of the aircraft refueling process
    /// </summary>
    public enum RefuelingState
    {
        /// <summary>
        /// Refueling is not active or has not been initiated
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// Refueling has been paused (e.g. fuel hose disconnected)
        /// </summary>
        Paused = 1,

        /// <summary>
        /// Refueling is active and fuel is being transferred
        /// </summary>
        Active = 2,

        /// <summary>
        /// Refueling has completed (target fuel reached)
        /// </summary>
        Completed = 3
    }
}