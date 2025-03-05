using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for managing passenger operations across different systems
    /// </summary>
    public interface IPassengerService
    {
        /// <summary>
        /// Event raised when passenger state changes
        /// </summary>
        event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
        
        /// <summary>
        /// Gets the planned number of passengers
        /// </summary>
        int PassengersPlanned { get; }
        
        /// <summary>
        /// Gets the current number of passengers
        /// </summary>
        int PassengersCurrent { get; }
        
        /// <summary>
        /// Updates passenger data from a flight plan
        /// </summary>
        /// <param name="passengerCount">The number of passengers from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current passenger state to match planned</param>
        void UpdateFromFlightPlan(int passengerCount, bool forceCurrentUpdate = false);
        
        /// <summary>
        /// Starts the boarding process
        /// </summary>
        void BoardingStart();
        
        /// <summary>
        /// Processes boarding
        /// </summary>
        /// <returns>True if boarding is complete, false otherwise</returns>
        bool Boarding();
        
        /// <summary>
        /// Stops the boarding process
        /// </summary>
        void BoardingStop();
        
        /// <summary>
        /// Starts the deboarding process
        /// </summary>
        void DeboardingStart();
        
        /// <summary>
        /// Processes deboarding
        /// </summary>
        /// <returns>True if deboarding is complete, false otherwise</returns>
        bool Deboarding();
        
        /// <summary>
        /// Stops the deboarding process
        /// </summary>
        void DeboardingStop();
    }
    
    /// <summary>
    /// Event arguments for passenger state changes
    /// </summary>
    public class PassengerStateChangedEventArgs : EventArgs
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
        /// Creates a new instance of PassengerStateChangedEventArgs
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
