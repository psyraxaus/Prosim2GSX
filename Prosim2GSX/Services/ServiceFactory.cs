using System;
using System.Collections.Generic;
using Prosim2GSX.Models;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Factory for creating and managing services
    /// </summary>
    public class ServiceFactory
    {
        private readonly ServiceModel _model;
        private readonly ILogger _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFactory"/> class
        /// </summary>
        /// <param name="model">The service model</param>
        /// <param name="logger">The logger</param>
        public ServiceFactory(ServiceModel model, ILogger logger)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventAggregator = new EventAggregator(logger);
            
            // Register the event aggregator
            RegisterService<IEventAggregator>(_eventAggregator);
            
            // Register the event aggregator with the service model
            _model.SetService<IEventAggregator>(_eventAggregator);
            
            RegisterService<ILogger>(logger);
        }
        
        /// <summary>
        /// Registers a service
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <param name="service">The service instance</param>
        public void RegisterService<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
                
            _services[typeof(T)] = service;
            _logger.Log(LogLevel.Debug, "ServiceFactory:RegisterService", $"Registered service of type {typeof(T).Name}");
        }
        
        /// <summary>
        /// Gets a service
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <returns>The service instance, or null if not registered</returns>
        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            
            _logger.Log(LogLevel.Warning, "ServiceFactory:GetService", $"Service of type {typeof(T).Name} not found");
            return null;
        }
        
        /// <summary>
        /// Creates a ProSim controller
        /// </summary>
        /// <returns>The ProSim controller</returns>
        public IProsimController CreateProsimController()
        {
            try
            {
                _logger.Log(LogLevel.Information, "ServiceFactory:CreateProsimController", "Creating ProSim controller");
                
                // Create ProSim service
                var prosimService = new ProsimService(_model);
                RegisterService<IProsimService>(prosimService);
                
                // Create door service
                var doorService = new ProsimDoorService(prosimService);
                RegisterService<IProsimDoorService>(doorService);
                
                // Create equipment service
                var equipmentService = new ProsimEquipmentService(prosimService);
                RegisterService<IProsimEquipmentService>(equipmentService);
                
                // Create passenger service
                var passengerService = new ProsimPassengerService(prosimService);
                RegisterService<IProsimPassengerService>(passengerService);
                
                // Create cargo service
                var cargoService = new ProsimCargoService(prosimService);
                RegisterService<IProsimCargoService>(cargoService);
                
                // Create fuel service
                var fuelService = new ProsimFuelService(prosimService, _model);
                RegisterService<IProsimFuelService>(fuelService);
                
                // Create fluid service
                var fluidService = new ProsimFluidService(prosimService, _model);
                RegisterService<IProsimFluidService>(fluidService);
                
                // Create flight plan service
                var flightPlanService = new FlightPlanService(_model);
                RegisterService<IFlightPlanService>(flightPlanService);
                
                // Create the controller
                IProsimController controller = new ProsimControllerFacade(
                    _model,
                    _logger,
                    _eventAggregator,
                    prosimService,
                    doorService,
                    equipmentService,
                    passengerService,
                    cargoService,
                    fuelService,
                    fluidService,
                    flightPlanService);
                
                RegisterService<IProsimController>(controller);
                
                return controller;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Critical, "ServiceFactory:CreateProsimController", $"Error creating ProSim controller: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Creates a GSX controller facade
        /// </summary>
        /// <param name="prosimController">The ProSim controller</param>
        /// <param name="flightPlan">The flight plan</param>
        /// <returns>The GSX controller facade</returns>
        public IGSXControllerFacade CreateGSXControllerFacade(IProsimController prosimController, FlightPlan flightPlan)
        {
            try
            {
                _logger.Log(LogLevel.Information, "ServiceFactory:CreateGSXControllerFacade", "Creating GSX controller facade");
                
                // Create ACARS service
                var acarsService = new AcarsService(_model.AcarsNetworkUrl, _model.AcarsSecret);
                RegisterService<IAcarsService>(acarsService);
                
                // Create audio session manager
                var audioSessionManager = new CoreAudioSessionManager();
                RegisterService<IAudioSessionManager>(audioSessionManager);
                
                // Create GSX services
                var menuService = new GSXMenuService(_model, IPCManager.SimConnect);
                RegisterService<IGSXMenuService>(menuService);
                
                var audioService = new GSXAudioService(_model, IPCManager.SimConnect, audioSessionManager);
                RegisterService<IGSXAudioService>(audioService);
                
                var stateManager = new GSXStateManager();
                RegisterService<IGSXStateManager>(stateManager);
                
                var doorManager = new GSXDoorManager(prosimController.GetDoorService(), _model);
                RegisterService<IGSXDoorManager>(doorManager);
                doorManager.Initialize();
                
                // Create door coordinator
                var doorCoordinator = new GSXDoorCoordinator(doorManager, prosimController.GetDoorService(), _logger);
                RegisterService<IGSXDoorCoordinator>(doorCoordinator);
                doorCoordinator.Initialize();
                
                // Create equipment coordinator
                var equipmentCoordinator = new GSXEquipmentCoordinator(prosimController.GetEquipmentService(), _logger);
                RegisterService<IGSXEquipmentCoordinator>(equipmentCoordinator);
                equipmentCoordinator.Initialize();
                
                // Configure audio service properties
                audioService.AudioSessionRetryCount = 5; // Increase retry count for better reliability
                audioService.AudioSessionRetryDelay = TimeSpan.FromSeconds(1); // Shorter delay between retries
                
                // Create GSXLoadsheetManager
                var flightDataService = prosimController.GetFlightDataService();
                var loadsheetManager = new GSXLoadsheetManager(acarsService, flightDataService, flightPlan, _model);
                RegisterService<IGSXLoadsheetManager>(loadsheetManager);
                loadsheetManager.Initialize();
                
                // Create cargo coordinator first (before service orchestrator)
                var cargoCoordinator = new GSXCargoCoordinator(
                    prosimController.GetCargoService(),
                    null, // Will set serviceOrchestrator later
                    _logger);
                RegisterService<IGSXCargoCoordinator>(cargoCoordinator);
                
                // Create service orchestrator with cargo coordinator
                var serviceOrchestrator = new GSXServiceOrchestrator(
                    _model, 
                    IPCManager.SimConnect, 
                    (ProsimController)prosimController, 
                    menuService, 
                    loadsheetManager, 
                    doorManager, 
                    acarsService,
                    stateManager);
                RegisterService<IGSXServiceOrchestrator>(serviceOrchestrator);
                
                // Now initialize the cargo coordinator with the service orchestrator
                cargoCoordinator.SetServiceOrchestrator(serviceOrchestrator);
                cargoCoordinator.Initialize();
                cargoCoordinator.RegisterDoorCoordinator(doorCoordinator);
                cargoCoordinator.RegisterForStateChanges(stateManager);
                
                // Set the cargo coordinator in the service coordinator
                var serviceCoordinator = serviceOrchestrator.GetCoordinator() as GSXServiceCoordinator;
                if (serviceCoordinator != null)
                {
                    serviceCoordinator.SetCargoCoordinator(cargoCoordinator);
                    _logger.Log(LogLevel.Information, "ServiceFactory:CreateGSXControllerFacade", "Set cargo coordinator in service coordinator");
                }
                else
                {
                    _logger.Log(LogLevel.Warning, "ServiceFactory:CreateGSXControllerFacade", "Could not set cargo coordinator in service coordinator");
                }
                
                // Create passenger coordinator first (before setting service orchestrator)
                var passengerCoordinator = new GSXPassengerCoordinator(
                    prosimController.GetPassengerService(), 
                    null, // Will set serviceOrchestrator later
                    _logger);
                RegisterService<IGSXPassengerCoordinator>(passengerCoordinator);
                
                // Now set the service orchestrator in the passenger coordinator
                passengerCoordinator.SetServiceOrchestrator(serviceOrchestrator);
                passengerCoordinator.Initialize();
                
                // Create fuel coordinator first (before setting service orchestrator)
                var fuelCoordinator = new GSXFuelCoordinator(
                    prosimController.GetFuelService(),
                    null, // Will set serviceOrchestrator later
                    IPCManager.SimConnect,
                    _logger,
                    _eventAggregator); // Pass the event aggregator
                RegisterService<IGSXFuelCoordinator>(fuelCoordinator);
                
                // Now set the service orchestrator in the fuel coordinator
                fuelCoordinator.SetServiceOrchestrator(serviceOrchestrator);
                fuelCoordinator.Initialize();
                fuelCoordinator.RegisterForStateChanges(stateManager);
                
                // Create GSX controller facade
                IGSXControllerFacade gsxControllerFacade = new GSXControllerFacade(
                    _model, 
                    (ProsimController)prosimController, 
                    flightPlan, 
                    acarsService, 
                    menuService, 
                    audioService, 
                    stateManager, 
                    loadsheetManager, 
                    doorManager, 
                    serviceOrchestrator,
                    doorCoordinator,
                    equipmentCoordinator,
                    passengerCoordinator,
                    cargoCoordinator,
                    fuelCoordinator,
                    _logger);
                RegisterService<IGSXControllerFacade>(gsxControllerFacade);
                
                return gsxControllerFacade;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Critical, "ServiceFactory:CreateGSXControllerFacade", $"Error creating GSX controller facade: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Disposes all services
        /// </summary>
        public void DisposeAll()
        {
            _logger.Log(LogLevel.Information, "ServiceFactory:DisposeAll", "Disposing all services");
            
            foreach (var service in _services.Values)
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, "ServiceFactory:DisposeAll", $"Error disposing service {service.GetType().Name}: {ex.Message}");
                    }
                }
            }
            
            _services.Clear();
        }
    }
}
