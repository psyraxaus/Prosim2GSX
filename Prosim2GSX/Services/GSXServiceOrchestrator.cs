using Prosim2GSX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Orchestrates GSX services based on flight state
    /// </summary>
    public class GSXServiceOrchestrator : IGSXServiceOrchestrator, IDisposable
    {
        private readonly IGSXServiceCoordinator _coordinator;
        private readonly IGSXStateManager _stateManager;
        private readonly MobiSimConnect _simConnect;
        private readonly ProsimController _prosimController;
        private readonly ServiceModel _model;
        
        private readonly Dictionary<string, List<Action<ServiceEventArgs>>> _preServiceCallbacks = new Dictionary<string, List<Action<ServiceEventArgs>>>();
        private readonly Dictionary<string, List<Action<ServiceEventArgs>>> _postServiceCallbacks = new Dictionary<string, List<Action<ServiceEventArgs>>>();
        private IReadOnlyCollection<ServicePrediction> _lastPredictions = Array.Empty<ServicePrediction>();
        private bool _disposed = false;
        
        /// <summary>
        /// Event raised when a service status changes
        /// </summary>
        public event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;
        
        /// <summary>
        /// Event raised when a service execution is predicted
        /// </summary>
        public event EventHandler<ServicePredictionEventArgs> ServicePredicted;
        
        /// <summary>
        /// Initializes a new instance of the GSXServiceOrchestrator class
        /// </summary>
        public GSXServiceOrchestrator(
            ServiceModel model,
            MobiSimConnect simConnect,
            ProsimController prosimController,
            IGSXMenuService menuService,
            IGSXLoadsheetManager loadsheetManager,
            IGSXDoorManager doorManager,
            IAcarsService acarsService,
            IGSXStateManager stateManager)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _simConnect = simConnect ?? throw new ArgumentNullException(nameof(simConnect));
            _prosimController = prosimController ?? throw new ArgumentNullException(nameof(prosimController));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            
            // Create the coordinator using composition
            _coordinator = new GSXServiceCoordinator(
                model, 
                simConnect, 
                prosimController, 
                menuService, 
                loadsheetManager, 
                doorManager, 
                acarsService);
            
            // Forward the ServiceStatusChanged event
            _coordinator.ServiceStatusChanged += OnCoordinatorServiceStatusChanged;
            
            // Subscribe to state manager events
            _stateManager.StateChanged += OnStateChanged;
            _stateManager.PredictedStateChanged += OnPredictedStateChanged;
            
            Logger.Log(LogLevel.Information, "GSXServiceOrchestrator:Constructor", "GSX Service Orchestrator initialized");
        }
        
        // IGSXServiceCoordinator implementation (delegating to _coordinator)
        
        /// <summary>
        /// Initializes the service coordinator
        /// </summary>
        public void Initialize()
        {
            _coordinator.Initialize();
        }
        
        /// <summary>
        /// Runs loading services (refueling, catering, boarding)
        /// </summary>
        public void RunLoadingServices(int refuelState, int cateringState)
        {
            _coordinator.RunLoadingServices(refuelState, cateringState);
        }
        
        /// <summary>
        /// Runs departure services (loadsheet, equipment removal, pushback)
        /// </summary>
        public void RunDepartureServices(int departureState)
        {
            _coordinator.RunDepartureServices(departureState);
        }
        
        /// <summary>
        /// Runs arrival services (jetway/stairs, PCA, GPU, chocks)
        /// </summary>
        public void RunArrivalServices(int deboardState)
        {
            _coordinator.RunArrivalServices(deboardState);
        }
        
        /// <summary>
        /// Runs deboarding service
        /// </summary>
        public void RunDeboardingService(int deboardState)
        {
            _coordinator.RunDeboardingService(deboardState);
        }
        
        /// <summary>
        /// Checks if refueling is complete
        /// </summary>
        public bool IsRefuelingComplete()
        {
            return _coordinator.IsRefuelingComplete();
        }
        
        /// <summary>
        /// Checks if boarding is complete
        /// </summary>
        public bool IsBoardingComplete()
        {
            return _coordinator.IsBoardingComplete();
        }
        
        /// <summary>
        /// Checks if catering is complete
        /// </summary>
        public bool IsCateringComplete()
        {
            return _coordinator.IsCateringComplete();
        }
        
        /// <summary>
        /// Checks if the loadsheet has been sent
        /// </summary>
        public bool IsFinalLoadsheetSent()
        {
            return _coordinator.IsFinalLoadsheetSent();
        }
        
        /// <summary>
        /// Checks if the preliminary loadsheet has been sent
        /// </summary>
        public bool IsPreliminaryLoadsheetSent()
        {
            return _coordinator.IsPreliminaryLoadsheetSent();
        }
        
        /// <summary>
        /// Checks if equipment has been removed
        /// </summary>
        public bool IsEquipmentRemoved()
        {
            return _coordinator.IsEquipmentRemoved();
        }
        
        /// <summary>
        /// Checks if pushback is complete
        /// </summary>
        public bool IsPushbackComplete()
        {
            return _coordinator.IsPushbackComplete();
        }
        
        /// <summary>
        /// Checks if deboarding is complete
        /// </summary>
        public bool IsDeboardingComplete()
        {
            return _coordinator.IsDeboardingComplete();
        }
        
        /// <summary>
        /// Sets the number of passengers
        /// </summary>
        public void SetPassengers(int numPax)
        {
            _coordinator.SetPassengers(numPax);
        }
        
        /// <summary>
        /// Calls jetway and/or stairs
        /// </summary>
        public void CallJetwayStairs()
        {
            _coordinator.CallJetwayStairs();
        }
        
        /// <summary>
        /// Resets the service status
        /// </summary>
        public void ResetServiceStatus()
        {
            _coordinator.ResetServiceStatus();
        }
        
        // IGSXServiceOrchestrator implementation
        
        /// <summary>
        /// Orchestrates services based on the current flight state and aircraft parameters
        /// </summary>
        public void OrchestrateServices(FlightState state, AircraftParameters parameters)
        {
            try
            {
                // Predict services before execution
                var predictions = PredictServices(state, parameters);
                
                // Execute services based on the current state
                switch (state)
                {
                    case FlightState.PREFLIGHT:
                        OrchestratePreflightServices(parameters);
                        break;
                    case FlightState.DEPARTURE:
                        OrchestrateDepartureServices(parameters);
                        break;
                    case FlightState.TAXIOUT:
                        OrchestrateTaxioutServices(parameters);
                        break;
                    case FlightState.FLIGHT:
                        OrchestrateFlightServices(parameters);
                        break;
                    case FlightState.TAXIIN:
                        OrchestrateTaxiinServices(parameters);
                        break;
                    case FlightState.ARRIVAL:
                        OrchestrateArrivalServices(parameters);
                        break;
                    case FlightState.TURNAROUND:
                        OrchestrateTurnaroundServices(parameters);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OrchestrateServices", $"Error orchestrating services: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Predicts which services will be executed next based on the current state and parameters
        /// </summary>
        public IReadOnlyCollection<ServicePrediction> PredictServices(FlightState state, AircraftParameters parameters)
        {
            try
            {
                var predictions = new List<ServicePrediction>();
                
                // Predict services based on the current state
                switch (state)
                {
                    case FlightState.PREFLIGHT:
                        PredictPreflightServices(parameters, predictions);
                        break;
                    case FlightState.DEPARTURE:
                        PredictDepartureServices(parameters, predictions);
                        break;
                    case FlightState.TAXIOUT:
                        PredictTaxioutServices(parameters, predictions);
                        break;
                    case FlightState.FLIGHT:
                        PredictFlightServices(parameters, predictions);
                        break;
                    case FlightState.TAXIIN:
                        PredictTaxiinServices(parameters, predictions);
                        break;
                    case FlightState.ARRIVAL:
                        PredictArrivalServices(parameters, predictions);
                        break;
                    case FlightState.TURNAROUND:
                        PredictTurnaroundServices(parameters, predictions);
                        break;
                }
                
                // Check if predictions have changed
                if (!ArePredictionsSame(predictions, _lastPredictions))
                {
                    _lastPredictions = predictions;
                    OnServicePredicted(predictions, state);
                }
                
                return predictions;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:PredictServices", $"Error predicting services: {ex.Message}");
                return Array.Empty<ServicePrediction>();
            }
        }
        
        /// <summary>
        /// Registers a callback to be executed before a specific service is run
        /// </summary>
        public void RegisterPreServiceCallback(string serviceType, Action<ServiceEventArgs> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
                
            lock (_preServiceCallbacks)
            {
                if (!_preServiceCallbacks.ContainsKey(serviceType))
                    _preServiceCallbacks[serviceType] = new List<Action<ServiceEventArgs>>();
                    
                _preServiceCallbacks[serviceType].Add(callback);
            }
        }
        
        /// <summary>
        /// Registers a callback to be executed after a specific service is run
        /// </summary>
        public void RegisterPostServiceCallback(string serviceType, Action<ServiceEventArgs> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
                
            lock (_postServiceCallbacks)
            {
                if (!_postServiceCallbacks.ContainsKey(serviceType))
                    _postServiceCallbacks[serviceType] = new List<Action<ServiceEventArgs>>();
                    
                _postServiceCallbacks[serviceType].Add(callback);
            }
        }
        
        /// <summary>
        /// Unregisters a previously registered pre-service callback
        /// </summary>
        public void UnregisterPreServiceCallback(string serviceType, Action<ServiceEventArgs> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
                
            lock (_preServiceCallbacks)
            {
                if (_preServiceCallbacks.ContainsKey(serviceType))
                    _preServiceCallbacks[serviceType].Remove(callback);
            }
        }
        
        /// <summary>
        /// Unregisters a previously registered post-service callback
        /// </summary>
        public void UnregisterPostServiceCallback(string serviceType, Action<ServiceEventArgs> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
                
            lock (_postServiceCallbacks)
            {
                if (_postServiceCallbacks.ContainsKey(serviceType))
                    _postServiceCallbacks[serviceType].Remove(callback);
            }
        }
        
        // Implementation of orchestration methods for each flight state
        
        private void OrchestratePreflightServices(AircraftParameters parameters)
        {
            try
            {
                // Execute pre-service callbacks
                ExecutePreServiceCallbacks("Preflight", FlightState.PREFLIGHT, parameters);
                
                // Check if GSX is running
                if (_simConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceOrchestrator:OrchestratePreflightServices", "Couatl Engine not running");
                    return;
                }
                
                // Handle plane repositioning if configured
                if (_model.RepositionPlane && !IsPlanePositioned())
                {
                    ExecuteServiceWithCallbacks("Repositioning", FlightState.PREFLIGHT, parameters, () => {
                        Logger.Log(LogLevel.Information, "GSXServiceOrchestrator:OrchestratePreflightServices", $"Repositioning plane");
                        // This would be implemented in the coordinator
                    });
                    return;
                }
                
                // Handle jetway/stairs connection if configured
                if (_model.AutoConnect && !IsJetwayConnected())
                {
                    ExecuteServiceWithCallbacks("JetwayConnection", FlightState.PREFLIGHT, parameters, () => {
                        CallJetwayStairs();
                    });
                    return;
                }
                
                // Handle ground equipment if needed
                if (NeedsGroundEquipment())
                {
                    ExecuteServiceWithCallbacks("GroundEquipment", FlightState.PREFLIGHT, parameters, () => {
                        // Set ground equipment through coordinator
                    });
                }
                
                // Execute post-service callbacks
                ExecutePostServiceCallbacks("Preflight", FlightState.PREFLIGHT, parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OrchestratePreflightServices", $"Error orchestrating preflight services: {ex.Message}");
            }
        }
        
        private void OrchestrateDepartureServices(AircraftParameters parameters)
        {
            try
            {
                // Execute pre-service callbacks
                ExecutePreServiceCallbacks("Departure", FlightState.DEPARTURE, parameters);
                
                // Get current service states
                int refuelState = (int)_simConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
                int cateringState = (int)_simConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
                int departureState = (int)_simConnect.ReadLvar("FSDT_GSX_DEPARTURE_STATE");
                
                // Handle loading services if not complete
                if (!IsRefuelingComplete() || !IsBoardingComplete())
                {
                    ExecuteServiceWithCallbacks("LoadingServices", FlightState.DEPARTURE, parameters, () => {
                        RunLoadingServices(refuelState, cateringState);
                    });
                    return;
                }
                
                // Handle departure services
                ExecuteServiceWithCallbacks("DepartureServices", FlightState.DEPARTURE, parameters, () => {
                    RunDepartureServices(departureState);
                });
                
                // Execute post-service callbacks
                ExecutePostServiceCallbacks("Departure", FlightState.DEPARTURE, parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OrchestrateDepartureServices", $"Error orchestrating departure services: {ex.Message}");
            }
        }
        
        private void OrchestrateTaxioutServices(AircraftParameters parameters)
        {
            try
            {
                // Execute pre-service callbacks
                ExecutePreServiceCallbacks("Taxiout", FlightState.TAXIOUT, parameters);
                
                // Not much to do during taxiout
                
                // Execute post-service callbacks
                ExecutePostServiceCallbacks("Taxiout", FlightState.TAXIOUT, parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OrchestrateTaxioutServices", $"Error orchestrating taxiout services: {ex.Message}");
            }
        }
        
        private void OrchestrateFlightServices(AircraftParameters parameters)
        {
            try
            {
                // Execute pre-service callbacks
                ExecutePreServiceCallbacks("Flight", FlightState.FLIGHT, parameters);
                
                // Not much to do during flight
                
                // Execute post-service callbacks
                ExecutePostServiceCallbacks("Flight", FlightState.FLIGHT, parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OrchestrateFlightServices", $"Error orchestrating flight services: {ex.Message}");
            }
        }
        
        private void OrchestrateTaxiinServices(AircraftParameters parameters)
        {
            try
            {
                // Execute pre-service callbacks
                ExecutePreServiceCallbacks("Taxiin", FlightState.TAXIIN, parameters);
                
                // Check if we should transition to arrival
                if (_simConnect.ReadLvar("FSDT_VAR_EnginesStopped") == 1 && 
                    _simConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && 
                    _simConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 0)
                {
                    int deboardState = (int)_simConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");
                    
                    ExecuteServiceWithCallbacks("ArrivalServices", FlightState.TAXIIN, parameters, () => {
                        RunArrivalServices(deboardState);
                    });
                }
                
                // Execute post-service callbacks
                ExecutePostServiceCallbacks("Taxiin", FlightState.TAXIIN, parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OrchestrateTaxiinServices", $"Error orchestrating taxiin services: {ex.Message}");
            }
        }
        
        private void OrchestrateArrivalServices(AircraftParameters parameters)
        {
            try
            {
                // Execute pre-service callbacks
                ExecutePreServiceCallbacks("Arrival", FlightState.ARRIVAL, parameters);
                
                // Get current deboard state
                int deboardState = (int)_simConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");
                
                // Handle deboarding if in progress
                if (deboardState >= 4)
                {
                    ExecuteServiceWithCallbacks("Deboarding", FlightState.ARRIVAL, parameters, () => {
                        RunDeboardingService(deboardState);
                    });
                }
                
                // Execute post-service callbacks
                ExecutePostServiceCallbacks("Arrival", FlightState.ARRIVAL, parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OrchestrateArrivalServices", $"Error orchestrating arrival services: {ex.Message}");
            }
        }
        
        private void OrchestrateTurnaroundServices(AircraftParameters parameters)
        {
            try
            {
                // Execute pre-service callbacks
                ExecutePreServiceCallbacks("Turnaround", FlightState.TURNAROUND, parameters);
                
                // Check if a new flight plan is loaded
                if (_prosimController.IsFlightplanLoaded() && 
                    _prosimController.flightPlanID != GetCurrentFlightPlanId())
                {
                    // A new flight plan is loaded, we'll transition to DEPARTURE in the state manager
                    // This is handled in the GsxController
                }
                
                // Execute post-service callbacks
                ExecutePostServiceCallbacks("Turnaround", FlightState.TURNAROUND, parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OrchestrateTurnaroundServices", $"Error orchestrating turnaround services: {ex.Message}");
            }
        }
        
        // Prediction methods with concrete implementations
        
        private void PredictPreflightServices(AircraftParameters parameters, List<ServicePrediction> predictions)
        {
            // Predict repositioning
            if (_model.RepositionPlane && !IsPlanePositioned())
            {
                predictions.Add(new ServicePrediction(
                    "Repositioning",
                    "Will reposition aircraft at gate",
                    0.9f,
                    TimeSpan.FromSeconds(_model.RepositionDelay)
                ));
            }
            
            // Predict jetway/stairs connection
            if (_model.AutoConnect && !IsJetwayConnected())
            {
                predictions.Add(new ServicePrediction(
                    "JetwayConnection",
                    "Will connect jetway/stairs",
                    0.9f,
                    TimeSpan.FromSeconds(5)
                ));
            }
            
            // Predict ground equipment
            if (NeedsGroundEquipment())
            {
                predictions.Add(new ServicePrediction(
                    "GroundEquipment",
                    "Will set ground equipment",
                    0.9f,
                    TimeSpan.FromSeconds(2)
                ));
            }
            
            // Predict state transition
            if (parameters.FlightPlanLoaded)
            {
                predictions.Add(new ServicePrediction(
                    "StateTransition",
                    "Will transition to DEPARTURE state",
                    0.9f,
                    TimeSpan.FromSeconds(1)
                ));
            }
        }
        
        private void PredictDepartureServices(AircraftParameters parameters, List<ServicePrediction> predictions)
        {
            // Get current service states
            int refuelState = (int)_simConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
            int cateringState = (int)_simConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
            
            // Predict refueling
            if (_model.AutoRefuel && !IsRefuelingComplete())
            {
                float confidence = refuelState == 0 ? 0.9f : 0.7f;
                string status = refuelState == 0 ? "Will request refueling" : "Refueling in progress";
                
                predictions.Add(new ServicePrediction(
                    "Refueling",
                    status,
                    confidence,
                    refuelState == 0 ? TimeSpan.FromSeconds(5) : null
                ));
            }
            
            // Predict catering
            if (_model.CallCatering && !IsCateringComplete() && IsRefuelingComplete())
            {
                float confidence = cateringState == 0 ? 0.9f : 0.7f;
                string status = cateringState == 0 ? "Will request catering" : "Catering in progress";
                
                predictions.Add(new ServicePrediction(
                    "Catering",
                    status,
                    confidence,
                    cateringState == 0 ? TimeSpan.FromSeconds(5) : null
                ));
            }
            
            // Predict boarding
            if (_model.AutoBoarding && !IsBoardingComplete() && IsRefuelingComplete() && 
                (IsCateringComplete() || !_model.CallCatering))
            {
                predictions.Add(new ServicePrediction(
                    "Boarding",
                    "Will request boarding",
                    0.9f,
                    TimeSpan.FromSeconds(90) // Delay before boarding
                ));
            }
            
            // Predict loadsheet
            if (IsRefuelingComplete() && IsBoardingComplete() && !IsFinalLoadsheetSent())
            {
                predictions.Add(new ServicePrediction(
                    "Loadsheet",
                    "Will generate final loadsheet",
                    0.9f,
                    TimeSpan.FromSeconds(new Random().Next(90, 150))
                ));
            }
            
            // Predict equipment removal
            if (IsRefuelingComplete() && IsBoardingComplete() && IsFinalLoadsheetSent() && !IsEquipmentRemoved())
            {
                bool readyForRemoval = parameters.ParkingBrakeSet && parameters.BeaconOn && !parameters.GroundEquipmentConnected;
                
                predictions.Add(new ServicePrediction(
                    "EquipmentRemoval",
                    readyForRemoval ? "Will remove ground equipment" : "Waiting for conditions to remove equipment",
                    readyForRemoval ? 0.9f : 0.5f,
                    readyForRemoval ? TimeSpan.FromSeconds(5) : null
                ));
            }
        }
        
        private void PredictTaxioutServices(AircraftParameters parameters, List<ServicePrediction> predictions)
        {
            // Predict transition to FLIGHT
            if (!parameters.OnGround)
            {
                predictions.Add(new ServicePrediction(
                    "StateTransition",
                    "Will transition to FLIGHT state",
                    0.9f,
                    TimeSpan.FromSeconds(1)
                ));
            }
        }
        
        private void PredictFlightServices(AircraftParameters parameters, List<ServicePrediction> predictions)
        {
            // Predict transition to TAXIIN
            if (parameters.OnGround)
            {
                predictions.Add(new ServicePrediction(
                    "StateTransition",
                    "Will transition to TAXIIN state",
                    0.9f,
                    TimeSpan.FromSeconds(1)
                ));
            }
        }
        
        private void PredictTaxiinServices(AircraftParameters parameters, List<ServicePrediction> predictions)
        {
            // Predict transition to ARRIVAL
            bool readyForArrival = _simConnect.ReadLvar("FSDT_VAR_EnginesStopped") == 1 && 
                                  _simConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && 
                                  _simConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 0;
            
            if (readyForArrival)
            {
                predictions.Add(new ServicePrediction(
                    "StateTransition",
                    "Will transition to ARRIVAL state",
                    0.9f,
                    TimeSpan.FromSeconds(1)
                ));
                
                // Predict arrival services
                predictions.Add(new ServicePrediction(
                    "ArrivalServices",
                    "Will set up arrival services",
                    0.9f,
                    TimeSpan.FromSeconds(2)
                ));
            }
        }
        
        private void PredictArrivalServices(AircraftParameters parameters, List<ServicePrediction> predictions)
        {
            // Get current deboard state
            int deboardState = (int)_simConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");
            
            // Predict deboarding
            if (_model.AutoDeboarding && deboardState < 4)
            {
                predictions.Add(new ServicePrediction(
                    "Deboarding",
                    "Will request deboarding",
                    0.9f,
                    TimeSpan.FromSeconds(5)
                ));
            }
            else if (deboardState >= 4 && deboardState < 6)
            {
                predictions.Add(new ServicePrediction(
                    "Deboarding",
                    "Deboarding in progress",
                    0.9f,
                    null
                ));
            }
            
            // Predict transition to TURNAROUND
            if (IsDeboardingComplete())
            {
                predictions.Add(new ServicePrediction(
                    "StateTransition",
                    "Will transition to TURNAROUND state",
                    0.9f,
                    TimeSpan.FromSeconds(1)
                ));
            }
        }
        
        private void PredictTurnaroundServices(AircraftParameters parameters, List<ServicePrediction> predictions)
        {
            // Predict transition to DEPARTURE
            if (parameters.FlightPlanLoaded)
            {
                predictions.Add(new ServicePrediction(
                    "StateTransition",
                    "Will transition to DEPARTURE state",
                    0.9f,
                    TimeSpan.FromSeconds(1)
                ));
            }
        }
        
        // Helper methods
        
        private bool IsPlanePositioned()
        {
            // This would be implemented based on the existing code
            // For now, return a placeholder value
            return false;
        }
        
        private bool IsJetwayConnected()
        {
            // Check if the jetway is connected
            return _simConnect.ReadLvar("FSDT_GSX_JETWAY") == 2 || _simConnect.ReadLvar("FSDT_GSX_JETWAY") == 5;
        }
        
        private bool NeedsGroundEquipment()
        {
            // Check if ground equipment needs to be set
            // This would be implemented based on the existing code
            // For now, return a placeholder value
            return false;
        }
        
        private string GetCurrentFlightPlanId()
        {
            // This would be implemented based on the existing code
            // For now, return a placeholder value
            return "0";
        }
        
        private bool ArePredictionsSame(IReadOnlyCollection<ServicePrediction> a, IReadOnlyCollection<ServicePrediction> b)
        {
            if (a.Count != b.Count)
                return false;
                
            var aSorted = a.OrderBy(p => p.ServiceType).ToList();
            var bSorted = b.OrderBy(p => p.ServiceType).ToList();
            
            for (int i = 0; i < aSorted.Count; i++)
            {
                var predA = aSorted[i];
                var predB = bSorted[i];
                
                if (predA.ServiceType != predB.ServiceType ||
                    predA.PredictedStatus != predB.PredictedStatus ||
                    Math.Abs(predA.Confidence - predB.Confidence) > 0.1f)
                    return false;
            }
            
            return true;
        }
        
        private void ExecuteServiceWithCallbacks(string serviceType, FlightState state, AircraftParameters parameters, Action serviceAction)
        {
            try
            {
                // Execute pre-service callbacks
                ExecutePreServiceCallbacks(serviceType, state, parameters);
                
                // Execute the service action
                serviceAction();
                
                // Execute post-service callbacks
                ExecutePostServiceCallbacks(serviceType, state, parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:ExecuteServiceWithCallbacks", $"Error executing service {serviceType}: {ex.Message}");
            }
        }
        
        // Event handlers
        
        private void OnCoordinatorServiceStatusChanged(object sender, ServiceStatusChangedEventArgs e)
        {
            try
            {
                // Forward the event
                ServiceStatusChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OnCoordinatorServiceStatusChanged", $"Error forwarding service status changed event: {ex.Message}");
            }
        }
        
        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            try
            {
                Logger.Log(LogLevel.Information, "GSXServiceOrchestrator:OnStateChanged", 
                    $"Flight state changed from {e.PreviousState} to {e.NewState}");
                    
                // Create aircraft parameters from current state
                var parameters = CreateAircraftParametersFromCurrentState();
                
                // Orchestrate services for the new state
                OrchestrateServices(e.NewState, parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OnStateChanged", $"Error handling state change: {ex.Message}");
            }
        }
        
        private void OnPredictedStateChanged(object sender, PredictedStateChangedEventArgs e)
        {
            try
            {
                Logger.Log(LogLevel.Information, "GSXServiceOrchestrator:OnPredictedStateChanged", 
                    $"Predicted state changed from {e.PreviousPrediction} to {e.NewPrediction} (Confidence: {e.Confidence})");
                    
                // Create aircraft parameters from current state
                var parameters = CreateAircraftParametersFromCurrentState();
                
                // Predict services for the new predicted state
                if (e.NewPrediction != null)
                {
                    var predictions = PredictServices(e.NewPrediction, parameters);
                    
                    // Log predictions
                    foreach (var prediction in predictions)
                    {
                        Logger.Log(LogLevel.Debug, "GSXServiceOrchestrator:OnPredictedStateChanged", 
                            $"Predicted service: {prediction.ServiceType} - {prediction.PredictedStatus} (Confidence: {prediction.Confidence})");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OnPredictedStateChanged", $"Error handling predicted state change: {ex.Message}");
            }
        }
        
        private AircraftParameters CreateAircraftParametersFromCurrentState()
        {
            try
            {
                return new AircraftParameters
                {
                    OnGround = _simConnect.ReadSimVar("SIM ON GROUND", "Bool") != 0.0f,
                    EnginesRunning = _simConnect.ReadLvar("FSDT_VAR_EnginesStopped") == 0,
                    ParkingBrakeSet = _simConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1,
                    BeaconOn = _simConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1,
                    GroundSpeed = (float)_simConnect.ReadSimVar("GPS GROUND SPEED", "Meters per second"),
                    Altitude = 0, // Would need to read from SimConnect
                    GroundEquipmentConnected = _simConnect.ReadLvar("I_OH_ELEC_EXT_PWR_L") == 1,
                    FlightPlanLoaded = _prosimController.IsFlightplanLoaded()
                };
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:CreateAircraftParametersFromCurrentState", $"Error creating aircraft parameters: {ex.Message}");
                return new AircraftParameters();
            }
        }
        
        // Event raisers
        
        protected virtual void OnServicePredicted(IReadOnlyCollection<ServicePrediction> predictions, FlightState state)
        {
            try
            {
                ServicePredicted?.Invoke(this, new ServicePredictionEventArgs(predictions, state));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:OnServicePredicted", $"Error raising ServicePredicted event: {ex.Message}");
            }
        }
        
        // Callback execution
        
        protected virtual void ExecutePreServiceCallbacks(string serviceType, FlightState state, AircraftParameters parameters)
        {
            List<Action<ServiceEventArgs>> callbacks = null;
            
            lock (_preServiceCallbacks)
            {
                if (_preServiceCallbacks.ContainsKey(serviceType))
                    callbacks = new List<Action<ServiceEventArgs>>(_preServiceCallbacks[serviceType]);
            }
            
            if (callbacks != null)
            {
                var args = new ServiceEventArgs(serviceType, state, parameters);
                
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback(args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:ExecutePreServiceCallbacks", $"Error executing callback: {ex.Message}");
                    }
                }
            }
        }
        
        protected virtual void ExecutePostServiceCallbacks(string serviceType, FlightState state, AircraftParameters parameters)
        {
            List<Action<ServiceEventArgs>> callbacks = null;
            
            lock (_postServiceCallbacks)
            {
                if (_postServiceCallbacks.ContainsKey(serviceType))
                    callbacks = new List<Action<ServiceEventArgs>>(_postServiceCallbacks[serviceType]);
            }
            
            if (callbacks != null)
            {
                var args = new ServiceEventArgs(serviceType, state, parameters);
                
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback(args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "GSXServiceOrchestrator:ExecutePostServiceCallbacks", $"Error executing callback: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Disposes resources used by the GSXServiceOrchestrator
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources used by the GSXServiceOrchestrator
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events
                    if (_coordinator != null)
                    {
                        _coordinator.ServiceStatusChanged -= OnCoordinatorServiceStatusChanged;
                    }
                    
                    if (_stateManager != null)
                    {
                        _stateManager.StateChanged -= OnStateChanged;
                        _stateManager.PredictedStateChanged -= OnPredictedStateChanged;
                    }
                    
                    // Dispose the coordinator if it's IDisposable
                    if (_coordinator is IDisposable disposableCoordinator)
                    {
                        disposableCoordinator.Dispose();
                    }
                }
                
                _disposed = true;
            }
        }
    }
}