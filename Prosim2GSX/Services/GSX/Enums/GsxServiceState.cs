namespace Prosim2GSX.Services.GSX.Enums
{
    /// <summary>
    /// Represents the states of GSX services
    /// </summary>
    public enum GsxServiceState
    {
        /// <summary>
        /// Service is not in a defined state
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// Service is available but not requested
        /// </summary>
        Available = 1,

        /// <summary>
        /// Service is unavailable
        /// </summary>
        Unavailable = 2,

        /// <summary>
        /// Service has been bypassed
        /// </summary>
        Bypassed = 3,

        /// <summary>
        /// Service has been requested but not yet active
        /// </summary>
        Requested = 4,

        /// <summary>
        /// Service is active
        /// </summary>
        Active = 5,

        /// <summary>
        /// Service has completed
        /// </summary>
        Completed = 6
    }
}