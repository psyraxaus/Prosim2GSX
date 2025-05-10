namespace Prosim2GSX.Services.GSX.Enums
{
    /// <summary>
    /// Represents the state of the refueling process
    /// </summary>
    public enum RefuelingState
    {
        /// <summary>
        /// Not refueling
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// Refueling has been requested but not started
        /// </summary>
        Requested = 1,

        /// <summary>
        /// Refueling is active and fuel is being transferred
        /// </summary>
        Active = 2,

        /// <summary>
        /// Refueling is paused (e.g., fuel hose disconnected)
        /// </summary>
        Paused = 3,

        /// <summary>
        /// Refueling has completed successfully
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Refueling was aborted
        /// </summary>
        Aborted = 5
    }
}