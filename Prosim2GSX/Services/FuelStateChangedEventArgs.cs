using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for fuel state changes
    /// </summary>
public class FuelStateChangedEventArgs : BaseEventArgs
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
        /// Gets a value indicating whether refueling is in progress
        /// </summary>
        public bool IsRefueling { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FuelStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentAmount">The current fuel amount</param>
        /// <param name="plannedAmount">The planned fuel amount</param>
        /// <param name="fuelUnits">The fuel units (KG or LBS)</param>
        public FuelStateChangedEventArgs(string operationType, double currentAmount, double plannedAmount, string fuelUnits, bool isRefueling = false)
        {
            OperationType = operationType;
            CurrentAmount = currentAmount;
            PlannedAmount = plannedAmount;
            FuelUnits = fuelUnits;
            IsRefueling = isRefueling;
        }
    }
}
