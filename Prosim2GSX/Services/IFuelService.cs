using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for managing fuel operations across different systems
    /// </summary>
    public interface IFuelService
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
        /// Updates fuel data from a flight plan
        /// </summary>
        /// <param name="plannedFuel">The planned fuel amount from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current fuel state to match planned</param>
        void UpdateFromFlightPlan(double plannedFuel, bool forceCurrentUpdate = false);
        
        /// <summary>
        /// Starts the refueling process
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
        /// Gets the fuel rate in kg/s
        /// </summary>
        /// <returns>The fuel rate in kg/s</returns>
        float GetFuelRate();
    }
}
