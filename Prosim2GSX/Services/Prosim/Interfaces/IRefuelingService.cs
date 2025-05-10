using Prosim2GSX.Services.Prosim.Enums;

namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for managing aircraft refueling
    /// </summary>
    public interface IRefuelingService
    {
        /// <summary>
        /// The current fuel amount in kg
        /// </summary>
        double CurrentFuel { get; }

        /// <summary>
        /// The planned or target fuel amount in kg
        /// </summary>
        double PlannedFuel { get; }

        /// <summary>
        /// The fuel units (KG or LBS)
        /// </summary>
        string FuelUnits { get; }

        /// <summary>
        /// Gets the current refueling state
        /// </summary>
        RefuelingState State { get; }

        /// <summary>
        /// Whether refueling is currently active
        /// </summary>
        bool IsRefuelingActive { get; }

        /// <summary>
        /// Whether the refueling process is complete
        /// </summary>
        bool IsRefuelingComplete { get; }

        /// <summary>
        /// Update the fuel data with the latest from the flight plan
        /// </summary>
        /// <param name="flightPlan">The current flight plan</param>
        void UpdateFuelData(FlightPlan flightPlan);

        /// <summary>
        /// Get the current fuel amount from the simulator
        /// </summary>
        /// <returns>Current fuel in kg</returns>
        double GetFuelAmount();

        /// <summary>
        /// Set the initial fuel target and state
        /// </summary>
        void SetInitialFuel();

        /// <summary>
        /// Set the fuel target amount
        /// </summary>
        /// <param name="amount">Target amount in kg</param>
        void SetFuelTarget(double amount);

        /// <summary>
        /// Set the initial hydraulic fluids
        /// </summary>
        void SetInitialFluids();

        /// <summary>
        /// Get the hydraulic fluid values
        /// </summary>
        /// <returns>Tuple of blue, green, yellow hydraulic fluid amounts</returns>
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
        /// Process one iteration of refueling
        /// </summary>
        /// <returns>True if refueling is complete, false otherwise</returns>
        bool ProcessRefueling();
    }
}