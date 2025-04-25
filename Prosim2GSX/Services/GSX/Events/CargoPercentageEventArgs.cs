using System;

namespace Prosim2GSX.Services.GSX.Events
{
    /// <summary>
    /// Event arguments for cargo percentage changes
    /// </summary>
    public class CargoPercentageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether this is a loading (true) or unloading (false) operation
        /// </summary>
        public bool IsLoading { get; }

        /// <summary>
        /// Gets the percentage value
        /// </summary>
        public int Percentage { get; }

        /// <summary>
        /// Creates a new instance of the event arguments
        /// </summary>
        public CargoPercentageEventArgs(bool isLoading, int percentage)
        {
            IsLoading = isLoading;
            Percentage = percentage;
        }
    }
}
