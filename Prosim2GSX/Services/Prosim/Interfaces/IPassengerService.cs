namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for managing passengers in ProSim
    /// </summary>
    public interface IPassengerService
    {
        /// <summary>
        /// Number of passengers in zone 1
        /// </summary>
        int PassengersZone1 { get; }

        /// <summary>
        /// Number of passengers in zone 2
        /// </summary>
        int PassengersZone2 { get; }

        /// <summary>
        /// Number of passengers in zone 3
        /// </summary>
        int PassengersZone3 { get; }

        /// <summary>
        /// Number of passengers in zone 4
        /// </summary>
        int PassengersZone4 { get; }

        /// <summary>
        /// Get planned number of passengers
        /// </summary>
        /// <returns>Planned number of passengers</returns>
        int GetPlannedPassengers();

        /// <summary>
        /// Get current number of passengers
        /// </summary>
        /// <returns>Current number of passengers</returns>
        int GetCurrentPassengers();

        /// <summary>
        /// Start boarding process
        /// </summary>
        void StartBoarding();

        /// <summary>
        /// Process boarding for a specified number of passengers
        /// </summary>
        /// <param name="paxCurrent">Current passenger count</param>
        /// <param name="cargoCurrent">Current cargo percentage</param>
        /// <returns>True if boarding complete</returns>
        bool ProcessBoarding(int paxCurrent, int cargoCurrent);

        /// <summary>
        /// Stop boarding process
        /// </summary>
        void StopBoarding();

        /// <summary>
        /// Start deboarding process
        /// </summary>
        void StartDeboarding();

        /// <summary>
        /// Process deboarding for a specified number of passengers
        /// </summary>
        /// <param name="paxCurrent">Current passenger count</param>
        /// <param name="cargoCurrent">Current cargo percentage</param>
        /// <returns>True if deboarding complete</returns>
        bool ProcessDeboarding(int paxCurrent, int cargoCurrent);

        /// <summary>
        /// Stop deboarding process
        /// </summary>
        void StopDeboarding();

        /// <summary>
        /// Generate random seating arrangement
        /// </summary>
        /// <param name="trueCount">Number of occupied seats</param>
        /// <returns>Seat occupation boolean array</returns>
        bool[] RandomizePassengerSeating(int trueCount);

        /// <summary>
        /// Update passenger data from the flight plan
        /// </summary>
        /// <param name="flightPlan">The flight plan</param>
        /// <param name="forceCurrent">Whether to force current values</param>
        void UpdatePassengerData(FlightPlan flightPlan, bool forceCurrent);

        /// <summary>
        /// Updates the passenger statistics object in Prosim to support loadsheet generation.
        /// 
        /// This method creates and sets the "efb.passengerStatistics" object that Prosim's
        /// loadsheet generation requires. It attempts to read passenger distribution data from
        /// zone amounts, but gracefully handles cases where zone data is not yet available by
        /// implementing a fallback distribution algorithm.
        /// </summary>
        void UpdatePassengerStatistics();
    }
}