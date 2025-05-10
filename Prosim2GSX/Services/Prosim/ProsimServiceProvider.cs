using ProSimSDK;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Connection.Implementation;
using Prosim2GSX.Services.Connection.Interfaces;
using Prosim2GSX.Services.Prosim.Implementation;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.GSX.Implementation;
using Prosim2GSX.Services.GSX.Interfaces;
using System;
using Prosim2GSX.Services.GSX;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Factory class for creating and managing services
    /// </summary>
    public class ProsimServiceProvider
    {
        private readonly ServiceModel _model;
        private readonly ProSimConnect _connection;

        // Original services
        private readonly IProsimInterface _prosimInterface;
        private readonly IProsimConnectionService _prosimConnectionService; // Note: renamed from _connectionService to avoid confusion
        private readonly IDataRefMonitoringService _dataRefService;
        private readonly IFlightPlanService _flightPlanService;
        private readonly IPassengerService _passengerService;
        private readonly ICargoService _cargoService;
        private readonly IDoorControlService _doorControlService;
        private readonly IRefuelingService _refuelingService;
        private readonly IGroundServiceInterface _groundService;
        private readonly ILoadsheetService _loadsheetService;

        // New connection service
        private readonly IConnectionService _applicationConnectionService;

        // GSX services
        private IGsxFlightStateService _gsxFlightStateService;
        private IGsxMenuService _gsxMenuService;
        private IGsxGroundServicesService _gsxGroundServicesService;
        private IGsxBoardingService _gsxBoardingService;
        private IGsxRefuelingService _gsxRefuelingService;
        private IGsxSimConnectService _gsxSimConnectService;
        private IGsxCateringService _gsxCateringService;
        private IGsxCargoService _gsxCargoService;

        /// <summary>
        /// Creates a new instance of the service provider
        /// </summary>
        /// <param name="model">Service model with configuration</param>
        public ProsimServiceProvider(ServiceModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _connection = new ProSimConnect();

            // Create core services first
            _prosimInterface = new ProsimInterface(_model, _connection);
            _prosimConnectionService = new ProsimConnectionService(_prosimInterface, _model);
            _dataRefService = new DataRefMonitoringService(_prosimInterface);

            // Create the application connection service
            _applicationConnectionService = new ApplicationConnectionService(_model, _prosimConnectionService);

            // Create domain services
            _flightPlanService = new FlightPlanService(_prosimInterface, _model);
            _cargoService = new CargoService(_prosimInterface, _model);
            _passengerService = new PassengerService(_prosimInterface, _model, _cargoService);
            _doorControlService = new DoorControlService(_prosimInterface);
            _refuelingService = new RefuelingService(_prosimInterface, _model);
            _groundService = new GroundServiceImplementation(_prosimInterface);
            _loadsheetService = new LoadsheetService(_prosimInterface, _flightPlanService);

            // Create GSX services if SimConnect is available
            if (IPCManager.SimConnect != null)
            {
                try
                {
                    UpdateGsxServices(IPCManager.SimConnect);
                }
                catch (Exception ex)
                {
                    LogService.Log(LogLevel.Error, nameof(ProsimServiceProvider),
                        $"Error creating GSX services: {ex.Message}");
                }
            }

            LogService.Log(LogLevel.Information, nameof(ProsimServiceProvider),
                "Service provider initialized");
        }

        /// <summary>
        /// Updates the GSX services with the current SimConnect instance
        /// </summary>
        /// <param name="simConnect">The MobiSimConnect instance to use</param>
        public void UpdateGsxServices(MobiSimConnect simConnect)
        {
            if (simConnect == null)
                return;

            try
            {
                // Create SimConnect service first as other services depend on it
                _gsxSimConnectService = new GsxSimConnectService(simConnect);

                // Create the menu service
                string menuFile = GsxHelpers.GetGsxMenuFilePath();
                _gsxMenuService = new GsxMenuService(_gsxSimConnectService, menuFile, _model, simConnect);

                // Create the flight state service
                _gsxFlightStateService = new GsxFlightStateService();

                // Create ground services service
                _gsxGroundServicesService = new GsxGroundServicesService(
                    _prosimInterface,
                    _gsxSimConnectService,
                    _gsxMenuService,
                    _dataRefService,
                    _model);

                // Create boarding service
                _gsxBoardingService = new GsxBoardingService(
                    _prosimInterface,
                    _gsxSimConnectService,
                    _gsxMenuService,
                    _doorControlService,
                    _model);

                // Create refueling service
                _gsxRefuelingService = new GsxRefuelingService(
                    _prosimInterface,
                    _gsxSimConnectService,
                    _gsxMenuService,
                    _refuelingService);

                // Create catering service
                _gsxCateringService = new GsxCateringService(
                    _prosimInterface,
                    _gsxSimConnectService,
                    _gsxMenuService,
                    _doorControlService,
                    _model);

                // Create cargo service
                _gsxCargoService = new GsxCargoService(
                    _prosimInterface,
                    _gsxSimConnectService,
                    _doorControlService,
                    _model);

                // Subscribe to service toggles
                _gsxCateringService.SubscribeToServiceToggles();

                // Subscribe to cargo events
                _gsxCargoService.SubscribeToCargoEvents();

                LogService.Log(LogLevel.Information, nameof(ProsimServiceProvider),
                    "GSX services updated successfully");
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(ProsimServiceProvider),
                    $"Error updating GSX services: {ex.Message}");
                throw; // Rethrow to let the caller handle it
            }
        }

        // Original service getters
        public IProsimInterface GetProsimInterface() => _prosimInterface;
        public IProsimConnectionService GetProsimConnectionService() => _prosimConnectionService;
        public IDataRefMonitoringService GetDataRefService() => _dataRefService;
        public IFlightPlanService GetFlightPlanService() => _flightPlanService;
        public IPassengerService GetPassengerService() => _passengerService;
        public ICargoService GetCargoService() => _cargoService;
        public IDoorControlService GetDoorControlService() => _doorControlService;
        public IRefuelingService GetRefuelingService() => _refuelingService;
        public IGroundServiceInterface GetGroundService() => _groundService;
        public ILoadsheetService GetLoadsheetService() => _loadsheetService;

        // New service getter
        public IConnectionService GetApplicationConnectionService() => _applicationConnectionService;

        // GSX service getters
        public IGsxFlightStateService GetGsxFlightStateService() => _gsxFlightStateService;
        public IGsxMenuService GetGsxMenuService() => _gsxMenuService;
        public IGsxGroundServicesService GetGsxGroundServicesService() => _gsxGroundServicesService;
        public IGsxBoardingService GetGsxBoardingService() => _gsxBoardingService;
        public IGsxRefuelingService GetGsxRefuelingService() => _gsxRefuelingService;
        public IGsxSimConnectService GetGsxSimConnectService() => _gsxSimConnectService;
        public IGsxCateringService GetGsxCateringService() => _gsxCateringService;

        /// <summary>
        /// Gets the GSX cargo service
        /// </summary>
        /// <returns>The GSX cargo service</returns>
        public IGsxCargoService GetGsxCargoService() => _gsxCargoService;

    }
}