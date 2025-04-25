namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for managing aircraft refueling
    /// </summary>
    public interface IRefuelingService
    {
        /// <summary>
        /// Gets the planned fuel amount in kg
        /// </summary>
        double PlannedFuel { get; }

        /// <summary>
        /// Gets the current fuel amount in kg
        /// </summary>
        double CurrentFuel { get; }

        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        string FuelUnits { get; }

        /// <summary>
        /// Gets whether the refueling is active
        /// </summary>
        bool IsRefuelingActive { get; }

        /// <summary>
        /// Gets whether the refueling is completed
        /// </summary>
        bool IsRefuelingComplete { get; }

        /// <summary>
        /// Update fuel data from the flight plan
        /// </summary>
        /// <param name="flightPlan">The flight plan</param>
        void UpdateFuelData(FlightPlan flightPlan);

        /// <summary>
        /// Get the current fuel amount
        /// </summary>
        /// <returns>Current fuel amount in kg</returns>
        double GetFuelAmount();

        /// <summary>
        /// Set the initial fuel amount
        /// </summary>
        void SetInitialFuel();

        /// <summary>
        /// Set initial hydraulic fluid levels
        /// </summary>
        void SetInitialFluids();

        /// <summary>
        /// Get the hydraulic fluid values
        /// </summary>
        /// <returns>Tuple of blue, green, and yellow hydraulic fluid values</returns>
        (double, double, double) GetHydraulicFluidValues();

        /// <summary>
        /// Start the refueling process
        /// </summary>
        void StartRefueling();

        /// <summary>
        /// Stop the refueling process
        /// </summary>
        void StopRefueling();

        /// <summary>
        /// Pause the refueling process
        /// </summary>
        void PauseRefueling();

        /// <summary>
        /// Resume the refueling process
        /// </summary>
        void ResumeRefueling();

        /// <summary>
        /// Process one step of the refueling
        /// </summary>
        /// <returns>True if refueling is complete</returns>
        bool ProcessRefueling();

        /// <summary>
        /// Set the target fuel amount
        /// </summary>
        /// <param name="amount">The target amount in kg</param>
        void SetFuelTarget(double amount);
    }
}