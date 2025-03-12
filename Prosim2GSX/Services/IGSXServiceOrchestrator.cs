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
        
        /// <summary>
        /// Requests refueling service
        /// </summary>
        /// <param name="targetFuelAmount">The target fuel amount in kg</param>
        /// <returns>True if the request was successful, false otherwise</returns>
        bool RequestRefueling(double targetFuelAmount);
        
        /// <summary>
        /// Cancels the current refueling service
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        bool CancelRefueling();
        
        /// <summary>
        /// Requests catering service
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        bool RequestCatering();
        
        /// <summary>
        /// Cancels the current catering service
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        bool CancelCatering();
        
        /// <summary>
        /// Requests passenger boarding service
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        bool RequestBoarding();
        
        /// <summary>
        /// Cancels the current boarding service
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        bool CancelBoarding();
        
        /// <summary>
        /// Requests passenger deboarding service
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        bool RequestDeBoarding();
        
        /// <summary>
        /// Cancels the current deboarding service
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        bool CancelDeBoarding();
        
        /// <summary>
        /// Requests cargo loading service
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        bool RequestCargoLoading();
        
        /// <summary>
        /// Cancels the current cargo loading service
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        bool CancelCargoLoading();
        
        /// <summary>
        /// Requests cargo unloading service
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        bool RequestCargoUnloading();
        
        /// <summary>
        /// Cancels the current cargo unloading service
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        bool CancelCargoUnloading();
    }
}
