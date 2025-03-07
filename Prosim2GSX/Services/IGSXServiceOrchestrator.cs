using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for orchestrating GSX services based on flight state
    /// </summary>
    public interface IGSXServiceOrchestrator : IGSXServiceCoordinator
    {
        /// <summary>
        /// Event raised when a service execution is predicted
        /// </summary>
        event EventHandler<ServicePredictionEventArgs> ServicePredicted;
        
        /// <summary>
        /// Orchestrates services based on the current flight state and aircraft parameters
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="parameters">The current aircraft parameters</param>
        void OrchestrateServices(FlightState state, AircraftParameters parameters);
        
        /// <summary>
        /// Predicts which services will be executed next based on the current state and parameters
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="parameters">The current aircraft parameters</param>
        /// <returns>A collection of predicted services with confidence levels</returns>
        IReadOnlyCollection<ServicePrediction> PredictServices(FlightState state, AircraftParameters parameters);
        
        /// <summary>
        /// Registers a callback to be executed before a specific service is run
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="callback">The callback to execute</param>
        void RegisterPreServiceCallback(string serviceType, Action<ServiceEventArgs> callback);
        
        /// <summary>
        /// Registers a callback to be executed after a specific service is run
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="callback">The callback to execute</param>
        void RegisterPostServiceCallback(string serviceType, Action<ServiceEventArgs> callback);
        
        /// <summary>
        /// Unregisters a previously registered pre-service callback
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="callback">The callback to unregister</param>
        void UnregisterPreServiceCallback(string serviceType, Action<ServiceEventArgs> callback);
        
        /// <summary>
        /// Unregisters a previously registered post-service callback
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="callback">The callback to unregister</param>
        void UnregisterPostServiceCallback(string serviceType, Action<ServiceEventArgs> callback);
    }
}
