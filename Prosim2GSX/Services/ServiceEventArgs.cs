using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for service execution events
    /// </summary>
public class ServiceEventArgs : BaseEventArgs
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
        /// Creates a new instance of ServiceEventArgs
        /// </summary>
        public ServiceEventArgs(string serviceType, FlightState flightState, AircraftParameters parameters)
        {
            ServiceType = serviceType;
            FlightState = flightState;
            Parameters = parameters;
        }
    }
}
