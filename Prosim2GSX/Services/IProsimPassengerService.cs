using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for managing passenger operations in ProSim
    /// </summary>
    public interface IProsimPassengerService
    {
        /// <summary>
        /// Event raised when passenger state changes
        /// </summary>
        event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
        
        /// <summary>
        /// Gets the number of passengers in Zone 1
        /// </summary>
        int PaxZone1 { get; }
        
        /// <summary>
        /// Gets the number of passengers in Zone 2
        /// </summary>
        int PaxZone2 { get; }
        
        /// <summary>
        /// Gets the number of passengers in Zone 3
        /// </summary>
        int PaxZone3 { get; }
        
        /// <summary>
        /// Gets the number of passengers in Zone 4
        /// </summary>
        int PaxZone4 { get; }
        
        /// <summary>
        /// Creates a randomized seating arrangement for the specified number of passengers
        /// </summary>
        /// <param name="trueCount">The number of passengers to seat</param>
        /// <returns>A boolean array representing the seating arrangement</returns>
        bool[] RandomizePaxSeating(int trueCount);
        
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
        /// Processes boarding for the specified number of passengers and cargo percentage
        /// </summary>
        /// <param name="paxCurrent">The current number of boarded passengers</param>
        /// <param name="cargoCurrent">The current cargo percentage</param>
        /// <param name="cargoChangeCallback">Callback to handle cargo changes</param>
        /// <returns>True if boarding is complete, false otherwise</returns>
        bool Boarding(int paxCurrent, int cargoCurrent, Action<int> cargoChangeCallback);
        
        /// <summary>
        /// Stops the boarding process
        /// </summary>
        void BoardingStop();
        
        /// <summary>
        /// Starts the deboarding process
        /// </summary>
        void DeboardingStart();
        
        /// <summary>
        /// Processes deboarding for the specified number of passengers and cargo percentage
        /// </summary>
        /// <param name="paxCurrent">The current number of remaining passengers</param>
        /// <param name="cargoCurrent">The current cargo percentage</param>
        /// <param name="cargoChangeCallback">Callback to handle cargo changes</param>
        /// <returns>True if deboarding is complete, false otherwise</returns>
        bool Deboarding(int paxCurrent, int cargoCurrent, Action<int> cargoChangeCallback);
        
        /// <summary>
        /// Stops the deboarding process
        /// </summary>
        void DeboardingStop();
        
        /// <summary>
        /// Gets the planned number of passengers
        /// </summary>
        /// <returns>The planned number of passengers</returns>
        int GetPaxPlanned();
        
        /// <summary>
        /// Gets the current number of passengers
        /// </summary>
        /// <returns>The current number of passengers</returns>
        int GetPaxCurrent();
        
        /// <summary>
        /// Checks if passenger seating has been randomized
        /// </summary>
        /// <returns>True if seating has been randomized, false otherwise</returns>
        bool HasRandomizedSeating();
        
        /// <summary>
        /// Resets the randomization flag
        /// </summary>
        void ResetRandomization();
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
