namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for managing GSX refueling
    /// </summary>
    public interface IGsxRefuelingService
    {
        /// <summary>
        /// Whether refueling was requested from GSX
        /// </summary>
        bool IsRefuelingRequested { get; }

        /// <summary>
        /// Whether fuel hose is connected
        /// </summary>
        bool IsFuelHoseConnected { get; }

        /// <summary>
        /// Whether the refueling process is complete
        /// </summary>
        bool IsRefuelingComplete { get; }

        /// <summary>
        /// Request refueling service through GSX menu
        /// </summary>
        void RequestRefuelingService();

        /// <summary>
        /// Process the current refueling state
        /// </summary>
        /// <returns>True if refueling is complete</returns>
        bool ProcessRefueling();

        /// <summary>
        /// Set initial fuel target
        /// </summary>
        void SetInitialFuel();

        /// <summary>
        /// Get the current fuel amount
        /// </summary>
        double GetCurrentFuelAmount();
    }
}