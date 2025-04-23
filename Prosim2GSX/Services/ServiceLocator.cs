using Prosim2GSX.Models;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.GSX.Interfaces;
using System;
using Prosim2GSX.Services.Connection.Interfaces;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Provides centralized access to all ProSim services throughout the application
    /// </summary>
    public static class ServiceLocator
    {
        private static ProsimServiceProvider _serviceProvider;
        private static ServiceModel _serviceModel;

        /// <summary>
        /// Initialize the service locator
        /// </summary>
        /// <param name="model">Service model</param>
        public static void Initialize(ServiceModel model)
        {
            try
            {
                if (model == null)
                    throw new ArgumentNullException(nameof(model));

                _serviceModel = model;
                _serviceProvider = new ProsimServiceProvider(model);

                // Verify that critical services are available
                // This will throw if any of them are null
                var test = ProsimInterface;
                var test2 = FlightPlanService;

                Logger.Log(LogLevel.Information, nameof(ServiceLocator), "ServiceLocator initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, nameof(ServiceLocator),
                    $"Failed to initialize ServiceLocator: {ex.Message}");
                throw; // Rethrow to let caller handle it
            }
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
            _serviceProvider?.GetProsimConnectionService() ?? // Changed from GetConnectionService to GetProsimConnectionService
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
        /// Get the GSX loadsheet service
        /// </summary>
        public static IGsxLoadsheetService GsxLoadsheetService =>
            _serviceProvider?.GetGsxLoadsheetService() ??
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
                Logger.Log(LogLevel.Error, nameof(ServiceLocator), $"Exception during UpdateAllServices: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the application connection service
        /// </summary>
        public static IConnectionService ConnectionService =>
            _serviceProvider?.GetApplicationConnectionService() ??
            throw new InvalidOperationException("ServiceLocator not initialized");
    }
}