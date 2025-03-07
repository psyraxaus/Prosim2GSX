using Prosim2GSX.Models;
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Implementation of the GSX controller facade that coordinates GSX integration with ProsimA320
    /// </summary>
    public class GSXControllerFacade : IGSXControllerFacade
    {
        private readonly GsxController _controller;
        private readonly IGSXStateManager _stateManager;
        private readonly IGSXServiceOrchestrator _serviceOrchestrator;
        private readonly IGSXDoorCoordinator _doorCoordinator;
        private readonly IGSXEquipmentCoordinator _equipmentCoordinator;
        private readonly IGSXPassengerCoordinator _passengerCoordinator;
        private readonly IGSXCargoCoordinator _cargoCoordinator;
        private readonly IGSXAudioService _audioService;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Event raised when the flight state changes
        /// </summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// Event raised when a service status changes
        /// </summary>
        public event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;
        
        /// <summary>
        /// Initializes a new instance of the GSXControllerFacade class
        /// </summary>
        public GSXControllerFacade(
            ServiceModel model, 
            ProsimController prosimController, 
            FlightPlan flightPlan, 
            IAcarsService acarsService, 
            IGSXMenuService menuService, 
            IGSXAudioService audioService, 
            IGSXStateManager stateManager, 
            IGSXLoadsheetManager loadsheetManager, 
            IGSXDoorManager doorManager, 
            IGSXServiceOrchestrator serviceOrchestrator,
            IGSXDoorCoordinator doorCoordinator,
            IGSXEquipmentCoordinator equipmentCoordinator,
            IGSXPassengerCoordinator passengerCoordinator,
            IGSXCargoCoordinator cargoCoordinator,
            ILogger logger)
        {
            try
            {
                // Store references to services that we need to subscribe to events
                _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
                _serviceOrchestrator = serviceOrchestrator ?? throw new ArgumentNullException(nameof(serviceOrchestrator));
                _doorCoordinator = doorCoordinator ?? throw new ArgumentNullException(nameof(doorCoordinator));
                _equipmentCoordinator = equipmentCoordinator ?? throw new ArgumentNullException(nameof(equipmentCoordinator));
                _passengerCoordinator = passengerCoordinator ?? throw new ArgumentNullException(nameof(passengerCoordinator));
                _cargoCoordinator = cargoCoordinator ?? throw new ArgumentNullException(nameof(cargoCoordinator));
                _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                
                // Create the controller
                _controller = new GsxController(
                    model, 
                    prosimController, 
                    flightPlan, 
                    acarsService, 
                    menuService, 
                    audioService, 
                    stateManager, 
                    loadsheetManager, 
                    doorManager, 
                    serviceOrchestrator);
                
                // Subscribe to events
                _stateManager.StateChanged += OnStateChanged;
                _serviceOrchestrator.ServiceStatusChanged += OnServiceStatusChanged;
                
                // Register coordinators for state changes
                _doorCoordinator.RegisterForStateChanges(_stateManager);
                _equipmentCoordinator.RegisterForStateChanges(_stateManager);
                _passengerCoordinator.RegisterForStateChanges(_stateManager);
                _cargoCoordinator.RegisterForStateChanges(_stateManager);
                
                _logger.Log(LogLevel.Information, "GSXControllerFacade:Constructor", "GSX Controller Facade initialized");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Critical, "GSXControllerFacade:Constructor", $"Error initializing GSX Controller Facade: {ex.Message}");
                throw;
            }
        }
        
        /// <inheritdoc/>
        public FlightState CurrentFlightState => _controller.CurrentFlightState;
        
        /// <inheritdoc/>
        public int Interval 
        { 
            get => _controller.Interval; 
            set => _controller.Interval = value; 
        }
        
        /// <inheritdoc/>
        public void RunServices()
        {
            try
            {
                _controller.RunServices();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXControllerFacade:RunServices", $"Error running services: {ex.Message}");
                throw;
            }
        }
        
        /// <inheritdoc/>
        public void ResetAudio()
        {
            try
            {
                _controller.ResetAudio();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXControllerFacade:ResetAudio", $"Error resetting audio: {ex.Message}");
                throw;
            }
        }
        
        /// <inheritdoc/>
        public void ControlAudio()
        {
            try
            {
                _controller.ControlAudio();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXControllerFacade:ControlAudio", $"Error controlling audio: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Handles the StateChanged event from the state manager
        /// </summary>
        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            try
            {
                StateChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXControllerFacade:OnStateChanged", $"Error handling state changed event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles the ServiceStatusChanged event from the service orchestrator
        /// </summary>
        private void OnServiceStatusChanged(object sender, ServiceStatusChangedEventArgs e)
        {
            try
            {
                ServiceStatusChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXControllerFacade:OnServiceStatusChanged", $"Error handling service status changed event: {ex.Message}");
            }
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                // Unsubscribe from events
                if (_stateManager != null)
                    _stateManager.StateChanged -= OnStateChanged;
                if (_serviceOrchestrator != null)
                    _serviceOrchestrator.ServiceStatusChanged -= OnServiceStatusChanged;
                
                // Dispose services
                _doorCoordinator?.Dispose();
                _equipmentCoordinator?.Dispose();
                _passengerCoordinator?.Dispose();
                _cargoCoordinator?.Dispose();
                
                // Dispose the controller
                _controller.Dispose();
                
                _logger.Log(LogLevel.Information, "GSXControllerFacade:Dispose", "GSX Controller Facade disposed");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXControllerFacade:Dispose", $"Error disposing facade: {ex.Message}");
            }
        }
    }
}
