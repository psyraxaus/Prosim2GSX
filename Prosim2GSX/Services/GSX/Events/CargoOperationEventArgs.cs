using System;

namespace Prosim2GSX.Services.GSX.Events
{
    /// <summary>
    /// Event arguments for cargo operations
    /// </summary>
    public class CargoOperationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether this is a loading (true) or unloading (false) operation
        /// </summary>
        public bool IsLoading { get; }

        /// <summary>
        /// Gets whether the operation is active
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Creates a new instance of the event arguments
        /// </summary>
        public CargoOperationEventArgs(bool isLoading, bool isActive)
        {
            IsLoading = isLoading;
            IsActive = isActive;
        }
    }
}
