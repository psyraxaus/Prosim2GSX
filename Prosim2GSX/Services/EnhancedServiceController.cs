using System;
using System.Threading;
using Prosim2GSX.Models;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Enhanced service controller that manages the application's service lifecycle
    /// </summary>
    public class EnhancedServiceController : BaseController
    {
        private readonly ServiceFactory _serviceFactory;
        private IProsimController _prosimController;
        private IGSXControllerFacade _gsxControllerFacade;
        private FlightPlan _flightPlan;
        private int _interval = 1000;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedServiceController"/> class
        /// </summary>
        /// <param name="model">The service model</param>
        /// <param name="logger">The logger</param>
        /// <param name="eventAggregator">The event aggregator</param>
        public EnhancedServiceController(ServiceModel model, ILogger logger, IEventAggregator eventAggregator)
            : base(model, logger, eventAggregator)
        {
            _serviceFactory = new ServiceFactory(model, logger);
        }
        
        /// <summary>
        /// Runs the service controller
        /// </summary>
        public void Run()
        {
            try
            {
                Logger.Log(LogLevel.Information, "EnhancedServiceController:Run", "Service starting...");
                
                while (!Model.CancellationRequested)
                {
                    if (Wait())
                    {
                        ServiceLoop();
                    }
                    else
                    {
                        if (!IPCManager.IsSimRunning())
                        {
                            Model.CancellationRequested = true;
                            Model.ServiceExited = true;
                            Logger.Log(LogLevel.Critical, "EnhancedServiceController:Run", "Session aborted, Retry not possible - exiting Program");
                            return;
                        }
                        else
                        {
                            Reset();
                            Logger.Log(LogLevel.Information, "EnhancedServiceController:Run", "Session aborted, Retry possible - Waiting for new Session");
                        }
                    }
                }
                
                IPCManager.CloseSafe();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "EnhancedServiceController:Run", $"Critical Exception occurred: {ex.Source} - {ex.Message}");
            }
            finally
            {
                // Clean up resources
                Dispose();
            }
        }
        
        /// <summary>
        /// Waits for the simulator and ProSim to be ready
        /// </summary>
        /// <returns>True if the simulator and ProSim are ready, false otherwise</returns>
        protected bool Wait()
        {
            return ExecuteSafely(() => {
                // Wait for simulator
                if (!IPCManager.WaitForSimulator(Model))
                    return false;
                else
                    Model.IsSimRunning = true;
                
                // Wait for connection
                if (!IPCManager.WaitForConnection(Model))
                    return false;
                
                // Create ProSim controller
                _prosimController = _serviceFactory.CreateProsimController();
                
                // Connect to ProSim
                if (!_prosimController.Connect(Model))
                    return false;
                else
                    Model.IsProsimRunning = true;
                
                // Wait for session ready
                if (!IPCManager.WaitForSessionReady(Model))
                    return false;
                else
                    Model.IsSessionRunning = true;
                
                return true;
            }, "Wait");
        }
        
        /// <summary>
        /// Resets the service controller
        /// </summary>
        protected void Reset()
        {
            ExecuteSafely(() => {
                try
                {
                    IPCManager.SimConnect?.Disconnect();
                    IPCManager.SimConnect = null;
                    Model.IsSessionRunning = false;
                    Model.IsProsimRunning = false;
                    
                    // Dispose services
                    _serviceFactory.DisposeAll();
                    _prosimController = null;
                    _gsxControllerFacade = null;
                    _flightPlan = null;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Critical, "EnhancedServiceController:Reset", $"Exception during Reset: {ex.Source} - {ex.Message}");
                }
            }, "Reset");
        }
        
        /// <summary>
        /// Runs the service loop
        /// </summary>
        protected void ServiceLoop()
        {
            ExecuteSafely(() => {
                // Initialize services
                InitializeServices();
                
                // Get reference to GSX controller
                var gsxController = _gsxControllerFacade;
                
                // Initialize timing variables
                int elapsedMS = gsxController?.Interval ?? 1000;
                int delay = 100;
                
                Thread.Sleep(1000);
                Logger.Log(LogLevel.Information, "EnhancedServiceController:ServiceLoop", "Starting Service Loop");
                
                // Main service loop
                while (!Model.CancellationRequested && 
                       Model.IsProsimRunning && 
                       IPCManager.IsSimRunning() && 
                       IPCManager.IsCamReady() && 
                       gsxController != null)
                {
                    try
                    {
                        if (elapsedMS >= gsxController.Interval)
                        {
                            gsxController.RunServices();
                            elapsedMS = 0;
                        }
                        
                        if (Model.GsxVolumeControl || Model.IsVhf1Controllable())
                            gsxController.ControlAudio();
                        
                        Thread.Sleep(delay);
                        elapsedMS += delay;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Critical, "EnhancedServiceController:ServiceLoop", 
                            $"Critical Exception during ServiceLoop() {ex.GetType()} {ex.Message} {ex.Source}");
                    }
                }
                
                Logger.Log(LogLevel.Information, "EnhancedServiceController:ServiceLoop", "ServiceLoop ended");
                
                if (Model.GsxVolumeControl || Model.IsVhf1Controllable())
                {
                    Logger.Log(LogLevel.Information, "EnhancedServiceController:ServiceLoop", "Resetting GSX/VHF1 Audio");
                    gsxController?.ResetAudio();
                }
                
                // Clean up services
                CleanupServices();
            }, "ServiceLoop");
        }
        
        /// <summary>
        /// Initializes all services
        /// </summary>
        protected void InitializeServices()
        {
            ExecuteSafely(() => {
                Logger.Log(LogLevel.Information, "EnhancedServiceController:InitializeServices", "Initializing services...");
                
                // Create FlightPlan
                _flightPlan = new FlightPlan(Model, _serviceFactory.GetService<IFlightPlanService>());
                
                // Load flight plan
                if (!_flightPlan.Load())
                {
                    Logger.Log(LogLevel.Warning, "EnhancedServiceController:InitializeServices", 
                        "Could not load flight plan, will retry in service loop");
                    // We'll continue even if flight plan isn't loaded yet, as it might be loaded later
                }
                
                // Initialize FlightPlan in ProsimController
                _prosimController.InitializeFlightPlan(_flightPlan);
                
                // Create GSX controller facade
                _gsxControllerFacade = _serviceFactory.CreateGSXControllerFacade(_prosimController, _flightPlan);
                
                // Store the GSXControllerFacade in IPCManager so it can be accessed by the MainWindow
                IPCManager.GsxController = _gsxControllerFacade;
                
                // Subscribe to events
                _prosimController.ConnectionStateChanged += OnProsimConnectionStateChanged;
                _prosimController.FlightPlanLoaded += OnFlightPlanLoaded;
                _gsxControllerFacade.StateChanged += OnGsxStateChanged;
                _gsxControllerFacade.ServiceStatusChanged += OnGsxServiceStatusChanged;
                
                Logger.Log(LogLevel.Information, "EnhancedServiceController:InitializeServices", "Services initialized successfully");
            }, "InitializeServices");
        }
        
        /// <summary>
        /// Cleans up services
        /// </summary>
        protected void CleanupServices()
        {
            ExecuteSafely(() => {
                Logger.Log(LogLevel.Information, "EnhancedServiceController:CleanupServices", "Cleaning up services...");
                
                // Unsubscribe from events
                if (_prosimController != null)
                {
                    _prosimController.ConnectionStateChanged -= OnProsimConnectionStateChanged;
                    _prosimController.FlightPlanLoaded -= OnFlightPlanLoaded;
                }
                
                if (_gsxControllerFacade != null)
                {
                    _gsxControllerFacade.StateChanged -= OnGsxStateChanged;
                    _gsxControllerFacade.ServiceStatusChanged -= OnGsxServiceStatusChanged;
                }
                
                // Clear the GsxController reference when the service loop ends
                IPCManager.GsxController = null;
                
                // Clear other service references
                _flightPlan = null;
                
                // Dispose services
                _serviceFactory.DisposeAll();
                
                Logger.Log(LogLevel.Information, "EnhancedServiceController:CleanupServices", "Services cleaned up");
            }, "CleanupServices");
        }
        
        /// <summary>
        /// Handles the ConnectionStateChanged event from the ProSim controller
        /// </summary>
        private void OnProsimConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            ExecuteSafely(() => {
                Logger.Log(LogLevel.Information, "EnhancedServiceController:OnProsimConnectionStateChanged", 
                    $"ProSim connection state changed: {e.IsConnected}");
                
                // Publish event to event aggregator
                EventAggregator.Publish(e);
            }, "OnProsimConnectionStateChanged");
        }
        
        /// <summary>
        /// Handles the FlightPlanLoaded event from the ProSim controller
        /// </summary>
        private void OnFlightPlanLoaded(object sender, FlightPlanLoadedEventArgs e)
        {
            ExecuteSafely(() => {
                Logger.Log(LogLevel.Information, "EnhancedServiceController:OnFlightPlanLoaded", 
                    $"Flight plan loaded: {e.FlightNumber} from {e.DepartureAirport} to {e.ArrivalAirport}");
                
                // Publish event to event aggregator
                EventAggregator.Publish(e);
            }, "OnFlightPlanLoaded");
        }
        
        /// <summary>
        /// Handles the StateChanged event from the GSX controller
        /// </summary>
        private void OnGsxStateChanged(object sender, StateChangedEventArgs e)
        {
            ExecuteSafely(() => {
                Logger.Log(LogLevel.Information, "EnhancedServiceController:OnGsxStateChanged", 
                    $"GSX state changed: {e.PreviousState} -> {e.NewState}");
                
                // Publish event to event aggregator
                EventAggregator.Publish(e);
            }, "OnGsxStateChanged");
        }
        
        /// <summary>
        /// Handles the ServiceStatusChanged event from the GSX controller
        /// </summary>
        private void OnGsxServiceStatusChanged(object sender, ServiceStatusChangedEventArgs e)
        {
            ExecuteSafely(() => {
                Logger.Log(LogLevel.Information, "EnhancedServiceController:OnGsxServiceStatusChanged", 
                    $"GSX service status changed: {e.ServiceType} - {e.Status}");
                
                // Publish event to event aggregator
                EventAggregator.Publish(e);
            }, "OnGsxServiceStatusChanged");
        }
        
        /// <summary>
        /// Disposes resources used by the controller
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
                return;
                
            try
            {
                // Clean up services
                CleanupServices();
                
                // Dispose service factory
                _serviceFactory.DisposeAll();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "EnhancedServiceController:Dispose", $"Error disposing controller: {ex.Message}");
            }
            
            base.Dispose();
        }
    }
}
