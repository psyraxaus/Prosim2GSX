using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for flight plan loaded events
    /// </summary>
    public class FlightPlanLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the flight plan ID
        /// </summary>
        public string FlightPlanID { get; }
        
        /// <summary>
        /// Gets the flight number
        /// </summary>
        public string FlightNumber { get; }
        
        /// <summary>
        /// Gets the departure airport
        /// </summary>
        public string DepartureAirport { get; }
        
        /// <summary>
        /// Gets the arrival airport
        /// </summary>
        public string ArrivalAirport { get; }
        
        /// <summary>
        /// Gets the timestamp when the flight plan was loaded
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FlightPlanLoadedEventArgs"/> class
        /// </summary>
        /// <param name="flightPlanID">The flight plan ID</param>
        /// <param name="flightNumber">The flight number</param>
        /// <param name="departureAirport">The departure airport</param>
        /// <param name="arrivalAirport">The arrival airport</param>
        public FlightPlanLoadedEventArgs(string flightPlanID, string flightNumber, string departureAirport, string arrivalAirport)
        {
            FlightPlanID = flightPlanID;
            FlightNumber = flightNumber;
            DepartureAirport = departureAirport;
            ArrivalAirport = arrivalAirport;
            Timestamp = DateTime.UtcNow;
        }
    }
}
