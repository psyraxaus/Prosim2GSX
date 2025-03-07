using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for service execution events
    /// </summary>
    public class ServiceEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of service
        /// </summary>
        public string ServiceType { get; }
        
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        public FlightState FlightState { get; }
        
        /// <summary>
        /// Gets the current aircraft parameters
        /// </summary>
        public AircraftParameters Parameters { get; }
        
        /// <summary>
        /// Gets the timestamp of the event
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Creates a new instance of ServiceEventArgs
        /// </summary>
        public ServiceEventArgs(string serviceType, FlightState flightState, AircraftParameters parameters)
        {
            ServiceType = serviceType;
            FlightState = flightState;
            Parameters = parameters;
            Timestamp = DateTime.Now;
        }
    }
}
