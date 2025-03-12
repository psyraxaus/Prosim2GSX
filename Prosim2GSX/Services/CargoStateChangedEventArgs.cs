using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for cargo state changes
    /// </summary>
    public class CargoStateChangedEventArgs : BaseEventArgs
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
        /// Initializes a new instance of the <see cref="CargoStateChangedEventArgs"/> class
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
