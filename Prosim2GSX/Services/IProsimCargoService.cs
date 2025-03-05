using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for managing cargo operations in ProSim
    /// </summary>
    public interface IProsimCargoService
    {
        /// <summary>
        /// Event raised when cargo state changes
        /// </summary>
        event EventHandler<CargoStateChangedEventArgs> CargoStateChanged;
        
        /// <summary>
        /// Gets the planned cargo amount
        /// </summary>
        int CargoPlanned { get; }
        
        /// <summary>
        /// Gets the current cargo amount as a percentage of planned
        /// </summary>
        int CargoCurrentPercentage { get; }
        
        /// <summary>
        /// Updates cargo data from a flight plan
        /// </summary>
        /// <param name="cargoAmount">The cargo amount from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current cargo state to match planned</param>
        void UpdateFromFlightPlan(int cargoAmount, bool forceCurrentUpdate = false);
        
        /// <summary>
        /// Changes the cargo amount to the specified percentage of the planned amount
        /// </summary>
        /// <param name="percentage">The percentage of planned cargo to load (0-100)</param>
        void ChangeCargo(int percentage);
        
        /// <summary>
        /// Gets the planned cargo amount
        /// </summary>
        /// <returns>The planned cargo amount</returns>
        int GetCargoPlanned();
        
        /// <summary>
        /// Gets the current cargo amount as a percentage of planned
        /// </summary>
        /// <returns>The current cargo percentage (0-100)</returns>
        int GetCargoCurrentPercentage();
    }
    
    /// <summary>
    /// Event arguments for cargo state changes
    /// </summary>
    public class CargoStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current cargo percentage
        /// </summary>
        public int CurrentPercentage { get; }
        
        /// <summary>
        /// Gets the planned cargo amount
        /// </summary>
        public int PlannedAmount { get; }
        
        /// <summary>
        /// Creates a new instance of CargoStateChangedEventArgs
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentPercentage">The current cargo percentage</param>
        /// <param name="plannedAmount">The planned cargo amount</param>
        public CargoStateChangedEventArgs(string operationType, int currentPercentage, int plannedAmount)
        {
            OperationType = operationType;
            CurrentPercentage = currentPercentage;
            PlannedAmount = plannedAmount;
        }
    }
}
