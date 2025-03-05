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
    
    /// <summary>
    /// Event arguments for fuel state changes
    /// </summary>
    public class FuelStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        public double CurrentAmount { get; }
        
        /// <summary>
        /// Gets the planned fuel amount
        /// </summary>
        public double PlannedAmount { get; }
        
        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        public string FuelUnits { get; }
        
        /// <summary>
        /// Creates a new instance of FuelStateChangedEventArgs
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentAmount">The current fuel amount</param>
        /// <param name="plannedAmount">The planned fuel amount</param>
        /// <param name="fuelUnits">The fuel units (KG or LBS)</param>
        public FuelStateChangedEventArgs(string operationType, double currentAmount, double plannedAmount, string fuelUnits)
        {
            OperationType = operationType;
            CurrentAmount = currentAmount;
            PlannedAmount = plannedAmount;
            FuelUnits = fuelUnits;
        }
    }
}
