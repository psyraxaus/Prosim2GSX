using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for passenger state changes
    /// </summary>
public class PassengerStateChangedEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current number of passengers
        /// </summary>
        public int CurrentCount { get; }
        
        /// <summary>
        /// Gets the planned number of passengers
        /// </summary>
        public int PlannedCount { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PassengerStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentCount">The current number of passengers</param>
        /// <param name="plannedCount">The planned number of passengers</param>
        public PassengerStateChangedEventArgs(string operationType, int currentCount, int plannedCount)
        {
            OperationType = operationType;
            CurrentCount = currentCount;
            PlannedCount = plannedCount;
        }
    }
}
