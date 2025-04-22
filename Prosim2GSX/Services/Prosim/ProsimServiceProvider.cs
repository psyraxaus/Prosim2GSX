using ProSimSDK;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Prosim.Implementation;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.GSX.Implementation;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Factory class for creating and managing ProSim services
    /// </summary>
    public class ProsimServiceProvider
    {
        private readonly ServiceModel _model;
        private readonly ProSimConnect _connection;
        private readonly IProsimInterface _prosimInterface;
        private readonly IProsimConnectionService _connectionService;
        private readonly IDataRefMonitoringService _dataRefService;
        private readonly IFlightPlanService _flightPlanService;
        private readonly IPassengerService _passengerService;
        private readonly ICargoService _cargoService;
        private readonly IDoorControlService _doorControlService;
        private readonly IRefuelingService _refuelingService;
        private readonly IGroundServiceInterface _groundService;

        /// <summary>
        /// Creates a new instance of the ProSim service provider
        /// </summary>
        /// <param name="model">Service model with configuration</param>
        public ProsimServiceProvider(ServiceModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _connection = new ProSimConnect();
            _groundService = new GroundServiceImplementation(_prosimInterface);

            // Create all services
            _prosimInterface = new ProsimInterface(_model, _connection);
            _connectionService = new ProsimConnectionService(_prosimInterface, _model);
            _dataRefService = new DataRefMonitoringService(_prosimInterface);
            _flightPlanService = new FlightPlanService(_prosimInterface, _model);
            _cargoService = new CargoService(_prosimInterface, _model);
            _passengerService = new PassengerService(_prosimInterface, _model, _cargoService);
            _doorControlService = new DoorControlService(_prosimInterface);
            _refuelingService = new RefuelingService(_prosimInterface, _model);

            Logger.Log(LogLevel.Information, nameof(ProsimServiceProvider),
                "ProSim service provider initialized");
        }

        /// <summary>
        /// Gets the ProSim interface for direct SDK interactions
        /// </summary>
        public IProsimInterface GetProsimInterface() => _prosimInterface;

        /// <summary>
        /// Gets the connection service
        /// </summary>
        public IProsimConnectionService GetConnectionService() => _connectionService;

        /// <summary>
        /// Gets the dataref monitoring service
        /// </summary>
        public IDataRefMonitoringService GetDataRefService() => _dataRefService;

        /// <summary>
        /// Gets the flight plan service
        /// </summary>
        public IFlightPlanService GetFlightPlanService() => _flightPlanService;

        /// <summary>
        /// Gets the passenger service
        /// </summary>
        public IPassengerService GetPassengerService() => _passengerService;

        /// <summary>
        /// Gets the cargo service
        /// </summary>
        public ICargoService GetCargoService() => _cargoService;

        /// <summary>
        /// Gets the door control service
        /// </summary>
        public IDoorControlService GetDoorControlService() => _doorControlService;

        /// <summary>
        /// Gets the refueling service
        /// </summary>
        public IRefuelingService GetRefuelingService() => _refuelingService;

        /// <summary>
        /// Gets the ground service
        /// </summary>
        public IGroundServiceInterface GetGroundService() => _groundService;
    }
}