using Microsoft.Extensions.Logging;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services.Connection.Interfaces;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Logging.Interfaces;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Provides centralized access to all ProSim services throughout the application.
    /// Acts as a service locator pattern implementation for singleton services.
    /// </summary>
    public static class ServiceLocator
    {
        private static ProsimServiceProvider _serviceProvider;
        private static ServiceModel _serviceModel;
        private static ILoggerFactory _loggerFactory;
        private static ILogger _logger;

        /// <summary>
        /// Dictionary of manually registered services to avoid circular dependencies in DI
        /// </summary>
        private static readonly Dictionary<Type, object> _manualServices = new Dictionary<Type, object>();

        /// <summary>
        /// Initialize the service locator
        /// </summary>
        /// <param name="loggerFactory">Logger factory for creating loggers</param>
        /// <param name="model">Service model</param>
        public static void Initialize(ILoggerFactory loggerFactory, ServiceModel model)
        {
            try
            {
                if (loggerFactory == null)
                    throw new ArgumentNullException(nameof(loggerFactory));

                if (model == null)
                    throw new ArgumentNullException(nameof(model));

                _loggerFactory = loggerFactory;
                _logger = loggerFactory.CreateLogger(nameof(ServiceLocator));
                _serviceModel = model;

                // Initialize DllLoader with a logger
                DllLoader.Initialize(loggerFactory.CreateLogger(nameof(DllLoader)));

                // Pass the logger factory to the service provider
                _serviceProvider = new ProsimServiceProvider(loggerFactory, model);

                // Verify that critical services are available
                // This will throw if any of them are null
                var test = ProsimInterface;
                var test2 = FlightPlanService;

                _logger.LogInformation("ServiceLocator initialized successfully");
            }
            catch (Exception ex)
            {
                // We may not have a logger yet if the initialization fails early,
                // so check if it's available before using it
                _logger?.LogCritical(ex, "Failed to initialize ServiceLocator");
                throw; // Rethrow to let caller handle it
            }
        }

        /// <summary>
        /// Manually registers a service instance to be returned by GetService.
        /// This is useful for avoiding circular dependencies in the DI container.
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <param name="service">The service implementation instance</param>
        public static void RegisterService<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            _manualServices[typeof(T)] = service;
            _logger?.LogDebug("Manually registered service of type {ServiceType}", typeof(T).Name);
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// Checks manually registered services first, then falls back to known service types.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>The service instance, or null if not found</returns>
        public static T GetService<T>() where T : class
        {
            // First check in manually registered services
            if (_manualServices.TryGetValue(typeof(T), out var manualService))
            {
                return (T)manualService;
            }

            // Next check for specific service types that we know how to create
            if (typeof(T) == typeof(ILoggerFactory))
            {
                return _loggerFactory as T;
            }

            // Try to get a logger if requested
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ILogger<>))
            {
                Type loggedType = typeof(T).GetGenericArguments()[0];
                var loggerMethod = typeof(ServiceLocator).GetMethod(nameof(GetLogger))
                    .MakeGenericMethod(loggedType);
                return loggerMethod.Invoke(null, null) as T;
            }

            // Add other specific interface mappings here
            if (typeof(T) == typeof(IAudioService))
            {
                // This would require a reference to the audio service to be stored
                _logger?.LogWarning("GetService<IAudioService>() called but no direct accessor is available");
                return null;
            }

            if (typeof(T) == typeof(IUiLogListener))
            {
                _logger?.LogWarning("GetService<IUiLogListener>() called but not found in manual services");
                return null;
            }

            _logger?.LogWarning("GetService<{Type}>() called but no implementation is available", typeof(T).Name);
            return null;
        }


        /// <summary>
        /// Get the ProSim interface
        /// </summary>
        public static IProsimInterface ProsimInterface =>
            _serviceProvider?.GetProsimInterface() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the Prosim connection service
        /// </summary>
        public static IProsimConnectionService ProsimConnectionService =>
            _serviceProvider?.GetProsimConnectionService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the dataref monitoring service
        /// </summary>
        public static IDataRefMonitoringService DataRefService =>
            _serviceProvider?.GetDataRefService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the flight plan service
        /// </summary>
        public static IFlightPlanService FlightPlanService =>
            _serviceProvider?.GetFlightPlanService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the passenger service
        /// </summary>
        public static IPassengerService PassengerService =>
            _serviceProvider?.GetPassengerService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the cargo service
        /// </summary>
        public static ICargoService CargoService =>
            _serviceProvider?.GetCargoService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the door control service
        /// </summary>
        public static IDoorControlService DoorControlService =>
            _serviceProvider?.GetDoorControlService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Gets the central service model that provides application-wide configuration and settings.
        /// </summary>
        public static ServiceModel Model =>
            _serviceModel ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the refueling service
        /// </summary>
        public static IRefuelingService RefuelingService =>
            _serviceProvider?.GetRefuelingService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Weight conversion factor from KG to LBS
        /// </summary>
        public static readonly float WeightConversion = 2.205f;

        /// <summary>
        /// Get the ground service interface
        /// </summary>
        public static IGroundServiceInterface GroundService =>
            _serviceProvider?.GetGroundService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the loadsheet service interface
        /// </summary>
        public static ILoadsheetService LoadsheetService =>
            _serviceProvider?.GetLoadsheetService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the GSX flight state service
        /// </summary>
        public static IGsxFlightStateService GsxFlightStateService =>
            _serviceProvider?.GetGsxFlightStateService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the GSX menu service
        /// </summary>
        public static IGsxMenuService GsxMenuService =>
            _serviceProvider?.GetGsxMenuService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the GSX ground services service
        /// </summary>
        public static IGsxGroundServicesService GsxGroundServicesService =>
            _serviceProvider?.GetGsxGroundServicesService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the GSX boarding service
        /// </summary>
        public static IGsxBoardingService GsxBoardingService =>
            _serviceProvider?.GetGsxBoardingService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the GSX refueling service
        /// </summary>
        public static IGsxRefuelingService GsxRefuelingService =>
             _serviceProvider?.GetGsxRefuelingService() ??
             throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the GSX SimConnect service
        /// </summary>
        public static IGsxSimConnectService GsxSimConnectService =>
            _serviceProvider?.GetGsxSimConnectService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Update ProSim data across all services
        /// </summary>
        /// <param name="forceCurrent">Whether to force current values</param>
        /// <param name="flightPlan">The current flight plan</param>
        public static void UpdateAllServices(bool forceCurrent, FlightPlan flightPlan)
        {
            try
            {
                // Check engine status
                double engine1 = ProsimInterface.GetProsimVariable("aircraft.engine1.raw");
                double engine2 = ProsimInterface.GetProsimVariable("aircraft.engine2.raw");

                // Update all services that need updating
                RefuelingService.UpdateFuelData(flightPlan);
                FlightPlanService.Update(forceCurrent);
                PassengerService.UpdatePassengerData(flightPlan, forceCurrent);
                CargoService.UpdateCargoData(flightPlan);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception during UpdateAllServices");
            }
        }

        /// <summary>
        /// Get the application connection service
        /// </summary>
        public static IConnectionService ConnectionService =>
            _serviceProvider?.GetApplicationConnectionService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Updates the GSX services with the current SimConnect instance
        /// </summary>
        /// <param name="simConnect">The MobiSimConnect instance to use</param>
        public static void UpdateGsxServices(MobiSimConnect simConnect)
        {
            if (simConnect == null)
            {
                _logger?.LogWarning("Cannot update GSX services: SimConnect is null");
                return;
            }

            _logger?.LogInformation("Updating GSX services with current SimConnect instance");

            try
            {
                _serviceProvider.UpdateGsxServices(simConnect);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating GSX services");
            }
        }

        /// <summary>
        /// Get the GSX catering service
        /// </summary>
        public static IGsxCateringService GsxCateringService =>
            _serviceProvider?.GetGsxCateringService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get the GSX cargo service
        /// </summary>
        public static IGsxCargoService GsxCargoService =>
            _serviceProvider?.GetGsxCargoService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get a logger for the specified category
        /// </summary>
        /// <typeparam name="T">The class to create a logger for</typeparam>
        /// <returns>An ILogger instance</returns>
        public static ILogger<T> GetLogger<T>() =>
            _loggerFactory?.CreateLogger<T>() ??
            throw new InvalidOperationException("ServiceLocator not initialized");

        /// <summary>
        /// Get a logger with the specified name
        /// </summary>
        /// <param name="categoryName">The category name for the logger</param>
        /// <returns>An ILogger instance</returns>
        public static ILogger GetLogger(string categoryName) =>
            _loggerFactory?.CreateLogger(categoryName) ??
            throw new InvalidOperationException("ServiceLocator not initialized");
    }
}
