using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for service prediction events
    /// </summary>
    public class ServicePredictionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the predicted services
        /// </summary>
        public IReadOnlyCollection<ServicePrediction> PredictedServices { get; }
        
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        public FlightState FlightState { get; }
        
        /// <summary>
        /// Gets the timestamp of the prediction
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Creates a new instance of ServicePredictionEventArgs
        /// </summary>
        public ServicePredictionEventArgs(IReadOnlyCollection<ServicePrediction> predictedServices, FlightState flightState)
        {
            PredictedServices = predictedServices;
            FlightState = flightState;
            Timestamp = DateTime.Now;
        }
    }
}
