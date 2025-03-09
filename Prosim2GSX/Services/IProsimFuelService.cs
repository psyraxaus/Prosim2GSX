using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for managing fuel operations in ProSim
    /// </summary>
    public interface IProsimFuelService
    {
        /// <summary>
        /// Event raised when fuel state changes
        /// </summary>
        event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;
        
        /// <summary>
        /// Gets the planned fuel amount in kg
        /// </summary>
        double FuelPlanned { get; }
        
        /// <summary>
        /// Gets the current fuel amount in kg
        /// </summary>
        double FuelCurrent { get; }
        
        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        string FuelUnits { get; }
        
        /// <summary>
        /// Sets the initial fuel based on configuration settings
        /// </summary>
        void SetInitialFuel();
        
        /// <summary>
        /// Prepares the refueling process by setting up the target fuel amount
        /// </summary>
        void PrepareRefueling();
        
        /// <summary>
        /// Starts the fuel transfer by setting refuelingPower to true
        /// </summary>
        void StartFuelTransfer();
        
        /// <summary>
        /// Starts the refueling process (combines PrepareRefueling and StartFuelTransfer)
        /// </summary>
        void RefuelStart();
        
        /// <summary>
        /// Continues the refueling process
        /// </summary>
        /// <returns>True if refueling is complete, false otherwise</returns>
        bool Refuel();
        
        /// <summary>
        /// Stops the refueling process
        /// </summary>
        void RefuelStop();
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        /// <returns>The current fuel amount in kg</returns>
        double GetFuelAmount();
        
        /// <summary>
        /// Gets the fuel rate in kg/s
        /// </summary>
        /// <returns>The fuel rate in kg/s</returns>
        float GetFuelRateKGS();
        
        /// <summary>
        /// Updates planned fuel data from a flight plan without changing the current fuel amount
        /// </summary>
        /// <param name="plannedFuel">The planned fuel amount from the flight plan</param>
        void UpdatePlannedFuel(double plannedFuel);
        
        /// <summary>
        /// Sets the current fuel amount to the specified value
        /// </summary>
        /// <param name="fuelAmount">The fuel amount to set</param>
        void SetCurrentFuel(double fuelAmount);
        
        /// <summary>
        /// Updates fuel data from a flight plan
        /// </summary>
        /// <param name="plannedFuel">The planned fuel amount from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current fuel state to match planned</param>
        void UpdateFromFlightPlan(double plannedFuel, bool forceCurrentUpdate = false);
        
        /// <summary>
        /// Gets the planned fuel amount
        /// </summary>
        /// <returns>The planned fuel amount in kg</returns>
        double GetFuelPlanned();
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        /// <returns>The current fuel amount in kg</returns>
        double GetFuelCurrent();
    }
}
