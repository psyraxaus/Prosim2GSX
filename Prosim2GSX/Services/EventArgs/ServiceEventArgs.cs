using System;

namespace Prosim2GSX.Services.EventArgs
{
    /// <summary>
    /// Base class for all service-related event arguments
    /// </summary>
    public class ServiceEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the service type
        /// </summary>
        public string ServiceType { get; }
        
        /// <summary>
        /// Gets the flight state
        /// </summary>
        public FlightState State { get; }
        
        /// <summary>
        /// Gets the timestamp when the event occurred
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Gets the correlation ID for tracking related events
        /// </summary>
        public string CorrelationId { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceEventArgs"/> class
        /// </summary>
        /// <param name="serviceType">The service type</param>
        /// <param name="state">The flight state</param>
        public ServiceEventArgs(string serviceType, FlightState state)
            : this(serviceType, state, Guid.NewGuid().ToString())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceEventArgs"/> class
        /// </summary>
        /// <param name="serviceType">The service type</param>
        /// <param name="state">The flight state</param>
        /// <param name="correlationId">The correlation ID for tracking related events</param>
        public ServiceEventArgs(string serviceType, FlightState state, string correlationId)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            State = state;
            Timestamp = DateTime.UtcNow;
            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        }
    }
}
