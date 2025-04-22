namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for managing GSX refueling
    /// </summary>
    public interface IGsxRefuelingService
    {
        /// <summary>
        /// Whether refueling is active
        /// </summary>
        bool IsRefueling { get; }

        /// <summary>
        /// Whether refueling is complete
        /// </summary>
        bool IsRefuelingComplete { get; }

        /// <summary>
        /// Whether refueling is paused
        /// </summary>
        bool IsRefuelingPaused { get; }

        /// <summary>
        /// Start refueling process
        /// </summary>
        void StartRefueling();

        /// <summary>
        /// Stop refueling process
        /// </summary>
        void StopRefueling();

        /// <summary>
        /// Pause refueling process
        /// </summary>
        void PauseRefueling();

        /// <summary>
        /// Resume refueling process
        /// </summary>
        void ResumeRefueling();

        /// <summary>
        /// Process refueling step
        /// </summary>
        /// <returns>True if refueling complete</returns>
        bool ProcessRefueling();

        /// <summary>
        /// Handle fuel hose state change
        /// </summary>
        /// <param name="connected">True if connected, false if disconnected</param>
        void HandleFuelHoseStateChange(bool connected);

        /// <summary>
        /// Request refueling service from GSX
        /// </summary>
        void RequestRefuelingService();

        /// <summary>
        /// Set initial fuel amount
        /// </summary>
        void SetInitialFuel();

        /// <summary>
        /// Get the current fuel amount
        /// </summary>
        /// <returns>Current fuel amount in appropriate units</returns>
        double GetCurrentFuelAmount();
    }
}