namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for managing GSX refueling
    /// </summary>
    public interface IGsxRefuelingService
    {
        /// <summary>
        /// Whether the refueling process is complete
        /// </summary>
        bool IsRefuelingComplete { get; }

        /// <summary>
        /// Whether the initial fuel amount has been set
        /// </summary>
        bool IsInitialFuelSet { get; }

        /// <summary>
        /// Whether the initial hydraulic fluid levels have been set
        /// </summary>
        bool IsHydraulicFluidsSet { get; }

        /// <summary>
        /// Whether refueling was requested from GSX
        /// </summary>
        bool IsRefuelingRequested { get; }

        /// <summary>
        /// Whether refueling is active
        /// </summary>
        bool IsRefuelingActive { get; }

        /// <summary>
        /// Whether refueling is paused
        /// </summary>
        bool IsRefuelingPaused { get; }

        /// <summary>
        /// Whether refueling hose connected
        /// </summary>
        bool IsFuelHoseConnected { get; }

        /// <summary>
        /// Set initial fuel target
        /// </summary>
        void SetInitialFuel();

        /// <summary>
        /// Set hydraulic fluid levels
        /// </summary>
        void SetHydraulicFluidLevels();

        /// <summary>
        /// Request refueling
        /// </summary>
        void RequestRefueling();

        /// <summary>
        /// Set refueling active
        /// </summary>
        void SetRefuelingActive();

        /// <summary>
        /// Process refueling
        /// </summary>
        void ProcessRefueling();

        /// <summary>
        /// Stop Refueling
        /// </summary>
        void StopRefueling();
    }
}