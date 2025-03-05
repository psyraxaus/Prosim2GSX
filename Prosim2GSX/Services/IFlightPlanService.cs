using System;
using System.Threading.Tasks;
using System.Xml;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for flight plan service operations
    /// </summary>
    public interface IFlightPlanService
    {
        /// <summary>
        /// Loads a flight plan from the configured source
        /// </summary>
        /// <returns>True if a new flight plan was loaded, false otherwise</returns>
        Task<bool> LoadFlightPlanAsync();
        
        /// <summary>
        /// Gets the flight plan data as an XML node
        /// </summary>
        /// <returns>XML node containing flight plan data, or null if unavailable</returns>
        Task<XmlNode> GetFlightPlanDataAsync();
        
        /// <summary>
        /// Fetches flight plan data from an online source
        /// </summary>
        /// <returns>XML node containing flight plan data, or null if unavailable</returns>
        Task<XmlNode> FetchOnlineFlightPlanAsync();
        
        /// <summary>
        /// Event that fires when a new flight plan is loaded
        /// </summary>
        event EventHandler<FlightPlanEventArgs> FlightPlanLoaded;
    }
    
    /// <summary>
    /// Event arguments for flight plan events
    /// </summary>
    public class FlightPlanEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the ID of the flight plan
        /// </summary>
        public string FlightPlanId { get; }
        
        /// <summary>
        /// Gets the flight number
        /// </summary>
        public string FlightNumber { get; }
        
        /// <summary>
        /// Gets the origin airport code
        /// </summary>
        public string Origin { get; }
        
        /// <summary>
        /// Gets the destination airport code
        /// </summary>
        public string Destination { get; }
        
        public FlightPlanEventArgs(string flightPlanId, string flightNumber, string origin, string destination)
        {
            FlightPlanId = flightPlanId;
            FlightNumber = flightNumber;
            Origin = origin;
            Destination = destination;
        }
    }
}
