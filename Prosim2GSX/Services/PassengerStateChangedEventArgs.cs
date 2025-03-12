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
        /// Gets a value indicating whether boarding is in progress
        /// </summary>
        public bool IsBoardingInProgress { get; }
        
        /// <summary>
        /// Gets the boarding progress percentage (0-100)
        /// </summary>
        public int BoardingProgress { get; }
        
        /// <summary>
        /// Gets a value indicating whether deboarding is in progress
        /// </summary>
        public bool IsDeBoardingInProgress { get; }
        
        /// <summary>
        /// Gets the deboarding progress percentage (0-100)
        /// </summary>
        public int DeBoardingProgress { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PassengerStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentCount">The current number of passengers</param>
        /// <param name="plannedCount">The planned number of passengers</param>
        public PassengerStateChangedEventArgs(
            string operationType, 
            int currentCount, 
            int plannedCount, 
            bool isBoardingInProgress = false, 
            int boardingProgress = 0, 
            bool isDeBoardingInProgress = false, 
            int deBoardingProgress = 0)
        {
            OperationType = operationType;
            CurrentCount = currentCount;
            PlannedCount = plannedCount;
            IsBoardingInProgress = isBoardingInProgress;
            BoardingProgress = boardingProgress;
            IsDeBoardingInProgress = isDeBoardingInProgress;
            DeBoardingProgress = deBoardingProgress;
        }
    }
}
