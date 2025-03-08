namespace Prosim2GSX.Services
{
    /// <summary>
    /// Represents the state of the refueling process
    /// </summary>
    public enum RefuelingState
    {
        /// <summary>
        /// No refueling operation is in progress
        /// </summary>
        Idle,
        
        /// <summary>
        /// Refueling has been requested but not yet started
        /// </summary>
        Requested,
        
        /// <summary>
        /// Refueling is in progress
        /// </summary>
        Refueling,
        
        /// <summary>
        /// Defueling is in progress
        /// </summary>
        Defueling,
        
        /// <summary>
        /// Refueling or defueling operation is complete
        /// </summary>
        Complete,
        
        /// <summary>
        /// An error occurred during refueling or defueling
        /// </summary>
        Error
    }
}
