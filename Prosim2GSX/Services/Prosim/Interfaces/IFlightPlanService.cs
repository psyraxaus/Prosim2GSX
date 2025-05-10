namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for managing flight plans in ProSim
    /// </summary>
    public interface IFlightPlanService
    {
        /// <summary>
        /// Current flight plan ID
        /// </summary>
        string FlightPlanID { get; }

        /// <summary>
        /// Current flight number
        /// </summary>
        string FlightNumber { get; }

        /// <summary>
        /// Set the flight plan
        /// </summary>
        /// <param name="flightPlan">The flight plan to set</param>
        void SetFlightPlan(FlightPlan flightPlan);

        /// <summary>
        /// Check if a flight plan is loaded
        /// </summary>
        /// <returns>True if a flight plan is loaded</returns>
        bool IsFlightplanLoaded();

        /// <summary>
        /// Get the flight number from the FMS
        /// </summary>
        /// <returns>The flight number</returns>
        string GetFMSFlightNumber();

        /// <summary>
        /// Update flight plan information
        /// </summary>
        /// <param name="forceCurrent">Whether to force update of current values</param>
        void Update(bool forceCurrent);

        /// <summary>
        /// Check if a loadsheet is available
        /// </summary>
        /// <param name="type">Type of loadsheet ("Preliminary" or "Final")</param>
        /// <returns>True if loadsheet is available</returns>
        bool IsLoadsheetAvailable(string type);

        /// <summary>
        /// Get loadsheet data
        /// </summary>
        /// <param name="type">Type of loadsheet ("Preliminary" or "Final")</param>
        /// <returns>The loadsheet data</returns>
        dynamic GetLoadsheetData(string type);

        /// <summary>
        /// Whether engines are running
        /// </summary>
        bool EnginesRunning { get; }
    }
}