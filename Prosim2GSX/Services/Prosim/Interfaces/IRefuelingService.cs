namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for aircraft refueling in ProSim
    /// </summary>
    public interface IRefuelingService
    {
        /// <summary>
        /// Current fuel amount in kg
        /// </summary>
        double CurrentFuel { get; }

        /// <summary>
        /// Planned fuel amount in kg
        /// </summary>
        double PlannedFuel { get; }

        /// <summary>
        /// Target fuel amount in kg
        /// </summary>
        double TargetFuel { get; }

        /// <summary>
        /// Fuel units (KG or LBS)
        /// </summary>
        string FuelUnits { get; }

        /// <summary>
        /// Get the current fuel amount
        /// </summary>
        /// <returns>Current fuel amount in kg</returns>
        double GetFuelAmount();

        /// <summary>
        /// Set initial fuel amount
        /// </summary>
        void SetInitialFuel();

        /// <summary>
        /// Set initial hydraulic fluid levels
        /// </summary>
        void SetInitialFluids();

        /// <summary>
        /// Get hydraulic fluid values
        /// </summary>
        /// <returns>Tuple of (blue, green, yellow) hydraulic quantities</returns>
        (double, double, double) GetHydraulicFluidValues();

        /// <summary>
        /// Start refueling process
        /// </summary>
        void StartRefueling();

        /// <summary>
        /// Process refueling
        /// </summary>
        /// <returns>True if refueling complete</returns>
        bool ProcessRefueling();

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
        /// Update fuel data from the flight plan
        /// </summary>
        /// <param name="flightPlan">The flight plan</param>
        void UpdateFuelData(FlightPlan flightPlan);
    }
}