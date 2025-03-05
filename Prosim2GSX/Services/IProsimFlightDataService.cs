using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for accessing and managing ProSim flight data
    /// </summary>
    public interface IProsimFlightDataService
    {
        /// <summary>
        /// Event raised when flight data changes
        /// </summary>
        event EventHandler<FlightDataChangedEventArgs> FlightDataChanged;
        
        /// <summary>
        /// Gets comprehensive flight data including weights, passenger counts, and CG values
        /// </summary>
        /// <param name="loadsheetType">Type of loadsheet ("prelim" or "final")</param>
        /// <returns>Tuple containing all flight data parameters</returns>
        (string Time, string Flight, string TailNumber, string DayOfFlight, 
         string DateOfFlight, string Origin, string Destination, 
         double EstZfw, double MaxZfw, double EstTow, double MaxTow, 
         double EstLaw, double MaxLaw, int PaxInfants, int PaxAdults, 
         double MacZfw, double MacTow, int PaxZoneA, int PaxZoneB, 
         int PaxZoneC, double FuelInTanks) GetLoadedData(string loadsheetType);
        
        /// <summary>
        /// Gets the flight number from the FMS
        /// </summary>
        /// <returns>Flight number as string</returns>
        string GetFMSFlightNumber();
        
        /// <summary>
        /// Gets the Zero Fuel Weight Center of Gravity (MACZFW)
        /// </summary>
        /// <returns>The MACZFW value as a percentage</returns>
        double GetZfwCG();
        
        /// <summary>
        /// Gets the Take Off Weight Center of Gravity (MACTOW)
        /// </summary>
        /// <returns>The MACTOW value as a percentage</returns>
        double GetTowCG();
    }
}
