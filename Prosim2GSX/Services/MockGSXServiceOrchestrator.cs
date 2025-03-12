using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Mock implementation of IGSXServiceOrchestrator for use when the real service is not available
    /// </summary>
    public class MockGSXServiceOrchestrator : IGSXServiceOrchestrator
    {
        private readonly ILogger _logger;
        private FlightState _currentState = FlightState.PREFLIGHT;
        private readonly Dictionary<string, List<Action<ServiceEventArgs>>> _preServiceCallbacks = new Dictionary<string, List<Action<ServiceEventArgs>>>();
        private readonly Dictionary<string, List<Action<ServiceEventArgs>>> _postServiceCallbacks = new Dictionary<string, List<Action<ServiceEventArgs>>>();
        private readonly Dictionary<string, bool> _serviceCompletionStatus = new Dictionary<string, bool>();
        private int _passengerCount = 150;
        private bool _equipmentRemoved = false;
        private bool _pushbackComplete = false;
        private bool _preliminaryLoadsheetSent = false;
        private bool _finalLoadsheetSent = false;

        /// <summary>
        /// Event that fires when the state changes
        /// </summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>
        /// Event that fires when a service status changes
        /// </summary>
        public event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;

        /// <summary>
        /// Event raised when a service execution is predicted
        /// </summary>
        public event EventHandler<ServicePredictionEventArgs> ServicePredicted;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockGSXServiceOrchestrator"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public MockGSXServiceOrchestrator(ILogger logger)
        {
            _logger = logger;
            _logger?.Log(LogLevel.Warning, "MockGSXServiceOrchestrator:Constructor", 
                "Using mock service orchestrator. Service operations will not affect the actual aircraft.");
            
            // Initialize service completion status
            _serviceCompletionStatus["Refueling"] = false;
            _serviceCompletionStatus["Boarding"] = false;
            _serviceCompletionStatus["Catering"] = false;
            _serviceCompletionStatus["CargoLoading"] = false;
            _serviceCompletionStatus["CargoUnloading"] = false;
            _serviceCompletionStatus["Deboarding"] = false;
        }

        /// <summary>
        /// Initializes the service coordinator
        /// </summary>
        public void Initialize()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:Initialize", 
                "Initializing mock service orchestrator");
            ResetServiceStatus();
        }

        /// <summary>
        /// Gets the service coordinator
        /// </summary>
        /// <returns>The service coordinator</returns>
        public IGSXServiceCoordinator GetCoordinator()
        {
            return this;
        }

        /// <summary>
        /// Orchestrates services based on the current flight state and aircraft parameters
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="parameters">The current aircraft parameters</param>
        public void OrchestrateServices(FlightState state, AircraftParameters parameters)
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:OrchestrateServices", 
                $"Orchestrating services for state {state} (mock)");
            
            // Set the current state
            SetState(state);
            
            // Predict services based on the current state
            var predictions = PredictServices(state, parameters);
            
            // Raise the ServicePredicted event
            OnServicePredicted(predictions, state);
        }

        /// <summary>
        /// Predicts which services will be executed next based on the current state and parameters
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="parameters">The current aircraft parameters</param>
        /// <returns>A collection of predicted services with confidence levels</returns>
        public IReadOnlyCollection<ServicePrediction> PredictServices(FlightState state, AircraftParameters parameters)
        {
            var predictions = new List<ServicePrediction>();
            
            switch (state)
            {
                case FlightState.PREFLIGHT:
                    predictions.Add(new ServicePrediction("Refueling", "Requested", 0.9f, TimeSpan.FromMinutes(1)));
                    predictions.Add(new ServicePrediction("Catering", "Requested", 0.8f, TimeSpan.FromMinutes(2)));
                    predictions.Add(new ServicePrediction("Boarding", "Requested", 0.7f, TimeSpan.FromMinutes(3)));
                    break;
                    
                case FlightState.DEPARTURE:
                    predictions.Add(new ServicePrediction("LoadsheetPreliminary", "Requested", 0.9f, TimeSpan.FromMinutes(1)));
                    predictions.Add(new ServicePrediction("LoadsheetFinal", "Requested", 0.8f, TimeSpan.FromMinutes(2)));
                    predictions.Add(new ServicePrediction("EquipmentRemoval", "Requested", 0.7f, TimeSpan.FromMinutes(3)));
                    break;
                    
                case FlightState.TAXIOUT:
                    predictions.Add(new ServicePrediction("Pushback", "Requested", 0.9f, TimeSpan.FromMinutes(1)));
                    break;
                    
                case FlightState.ARRIVAL:
                    predictions.Add(new ServicePrediction("Jetway", "Requested", 0.9f, TimeSpan.FromMinutes(1)));
                    predictions.Add(new ServicePrediction("GPU", "Requested", 0.8f, TimeSpan.FromMinutes(2)));
                    predictions.Add(new ServicePrediction("PCA", "Requested", 0.7f, TimeSpan.FromMinutes(3)));
                    predictions.Add(new ServicePrediction("Chocks", "Requested", 0.6f, TimeSpan.FromMinutes(4)));
                    break;
                    
                case FlightState.TURNAROUND:
                    predictions.Add(new ServicePrediction("Deboarding", "Requested", 0.9f, TimeSpan.FromMinutes(1)));
                    predictions.Add(new ServicePrediction("CargoUnloading", "Requested", 0.8f, TimeSpan.FromMinutes(2)));
                    break;
            }
            
            return predictions;
        }

        /// <summary>
        /// Registers a callback to be executed before a specific service is run
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="callback">The callback to execute</param>
        public void RegisterPreServiceCallback(string serviceType, Action<ServiceEventArgs> callback)
        {
            if (!_preServiceCallbacks.ContainsKey(serviceType))
            {
                _preServiceCallbacks[serviceType] = new List<Action<ServiceEventArgs>>();
            }
            
            _preServiceCallbacks[serviceType].Add(callback);
            
            _logger?.Log(LogLevel.Debug, "MockGSXServiceOrchestrator:RegisterPreServiceCallback", 
                $"Registered pre-service callback for {serviceType} (mock)");
        }

        /// <summary>
        /// Registers a callback to be executed after a specific service is run
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="callback">The callback to execute</param>
        public void RegisterPostServiceCallback(string serviceType, Action<ServiceEventArgs> callback)
        {
            if (!_postServiceCallbacks.ContainsKey(serviceType))
            {
                _postServiceCallbacks[serviceType] = new List<Action<ServiceEventArgs>>();
            }
            
            _postServiceCallbacks[serviceType].Add(callback);
            
            _logger?.Log(LogLevel.Debug, "MockGSXServiceOrchestrator:RegisterPostServiceCallback", 
                $"Registered post-service callback for {serviceType} (mock)");
        }

        /// <summary>
        /// Unregisters a previously registered pre-service callback
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="callback">The callback to unregister</param>
        public void UnregisterPreServiceCallback(string serviceType, Action<ServiceEventArgs> callback)
        {
            if (_preServiceCallbacks.ContainsKey(serviceType))
            {
                _preServiceCallbacks[serviceType].Remove(callback);
                
                _logger?.Log(LogLevel.Debug, "MockGSXServiceOrchestrator:UnregisterPreServiceCallback", 
                    $"Unregistered pre-service callback for {serviceType} (mock)");
            }
        }

        /// <summary>
        /// Unregisters a previously registered post-service callback
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="callback">The callback to unregister</param>
        public void UnregisterPostServiceCallback(string serviceType, Action<ServiceEventArgs> callback)
        {
            if (_postServiceCallbacks.ContainsKey(serviceType))
            {
                _postServiceCallbacks[serviceType].Remove(callback);
                
                _logger?.Log(LogLevel.Debug, "MockGSXServiceOrchestrator:UnregisterPostServiceCallback", 
                    $"Unregistered post-service callback for {serviceType} (mock)");
            }
        }

        /// <summary>
        /// Requests boarding
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        public bool RequestBoarding()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RequestBoarding", 
                "Boarding requested (mock)");
            OnServiceStatusChanged("Boarding", "Requested", false);
            
            // Simulate boarding completion after a delay
            Task.Run(async () => {
                await Task.Delay(5000); // 5 seconds delay
                _serviceCompletionStatus["Boarding"] = true;
                OnServiceStatusChanged("Boarding", "Completed", true);
            });
            
            return true;
        }

        /// <summary>
        /// Requests deboarding
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        public bool RequestDeBoarding()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RequestDeBoarding", 
                "Deboarding requested (mock)");
            OnServiceStatusChanged("Deboarding", "Requested", false);
            
            // Simulate deboarding completion after a delay
            Task.Run(async () => {
                await Task.Delay(5000); // 5 seconds delay
                _serviceCompletionStatus["Deboarding"] = true;
                OnServiceStatusChanged("Deboarding", "Completed", true);
            });
            
            return true;
        }

        /// <summary>
        /// Requests catering
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        public bool RequestCatering()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RequestCatering", 
                "Catering requested (mock)");
            OnServiceStatusChanged("Catering", "Requested", false);
            
            // Simulate catering completion after a delay
            Task.Run(async () => {
                await Task.Delay(5000); // 5 seconds delay
                _serviceCompletionStatus["Catering"] = true;
                OnServiceStatusChanged("Catering", "Completed", true);
            });
            
            return true;
        }

        /// <summary>
        /// Requests cargo loading
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        public bool RequestCargoLoading()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RequestCargoLoading", 
                "Cargo loading requested (mock)");
            OnServiceStatusChanged("CargoLoading", "Requested", false);
            
            // Simulate cargo loading completion after a delay
            Task.Run(async () => {
                await Task.Delay(5000); // 5 seconds delay
                _serviceCompletionStatus["CargoLoading"] = true;
                OnServiceStatusChanged("CargoLoading", "Completed", true);
            });
            
            return true;
        }

        /// <summary>
        /// Requests cargo unloading
        /// </summary>
        /// <returns>True if the request was successful, false otherwise</returns>
        public bool RequestCargoUnloading()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RequestCargoUnloading", 
                "Cargo unloading requested (mock)");
            OnServiceStatusChanged("CargoUnloading", "Requested", false);
            
            // Simulate cargo unloading completion after a delay
            Task.Run(async () => {
                await Task.Delay(5000); // 5 seconds delay
                _serviceCompletionStatus["CargoUnloading"] = true;
                OnServiceStatusChanged("CargoUnloading", "Completed", true);
            });
            
            return true;
        }

        /// <summary>
        /// Requests refueling
        /// </summary>
        /// <param name="targetQuantity">The target fuel quantity in gallons</param>
        /// <returns>True if the request was successful, false otherwise</returns>
        public bool RequestRefueling(double targetQuantity = 0.0)
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RequestRefueling", 
                $"Refueling requested to {targetQuantity} gallons (mock)");
            OnServiceStatusChanged("Refueling", "Requested", false);
            
            // Simulate refueling completion after a delay
            Task.Run(async () => {
                await Task.Delay(5000); // 5 seconds delay
                _serviceCompletionStatus["Refueling"] = true;
                OnServiceStatusChanged("Refueling", "Completed", true);
            });
            
            return true;
        }

        /// <summary>
        /// Cancels boarding
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        public bool CancelBoarding()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:CancelBoarding", 
                "Boarding cancelled (mock)");
            _serviceCompletionStatus["Boarding"] = false;
            OnServiceStatusChanged("Boarding", "Cancelled", false);
            return true;
        }

        /// <summary>
        /// Cancels deboarding
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        public bool CancelDeBoarding()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:CancelDeBoarding", 
                "Deboarding cancelled (mock)");
            _serviceCompletionStatus["Deboarding"] = false;
            OnServiceStatusChanged("Deboarding", "Cancelled", false);
            return true;
        }

        /// <summary>
        /// Cancels catering
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        public bool CancelCatering()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:CancelCatering", 
                "Catering cancelled (mock)");
            _serviceCompletionStatus["Catering"] = false;
            OnServiceStatusChanged("Catering", "Cancelled", false);
            return true;
        }

        /// <summary>
        /// Cancels cargo loading
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        public bool CancelCargoLoading()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:CancelCargoLoading", 
                "Cargo loading cancelled (mock)");
            _serviceCompletionStatus["CargoLoading"] = false;
            OnServiceStatusChanged("CargoLoading", "Cancelled", false);
            return true;
        }

        /// <summary>
        /// Cancels cargo unloading
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        public bool CancelCargoUnloading()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:CancelCargoUnloading", 
                "Cargo unloading cancelled (mock)");
            _serviceCompletionStatus["CargoUnloading"] = false;
            OnServiceStatusChanged("CargoUnloading", "Cancelled", false);
            return true;
        }

        /// <summary>
        /// Cancels refueling
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        public bool CancelRefueling()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:CancelRefueling", 
                "Refueling cancelled (mock)");
            _serviceCompletionStatus["Refueling"] = false;
            OnServiceStatusChanged("Refueling", "Cancelled", false);
            return true;
        }

        /// <summary>
        /// Sets the flight state
        /// </summary>
        /// <param name="state">The new flight state</param>
        /// <returns>True if the state was set successfully, false otherwise</returns>
        public bool SetState(FlightState state)
        {
            if (_currentState == state)
                return true;

            var previousState = _currentState;
            _currentState = state;
            
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:SetState", 
                $"Flight state changed from {previousState} to {state} (mock)");
            
            OnStateChanged(previousState, state);
            
            return true;
        }

        /// <summary>
        /// Gets the current flight state
        /// </summary>
        /// <returns>The current flight state</returns>
        public FlightState GetState()
        {
            return _currentState;
        }

        /// <summary>
        /// Runs loading services (refueling, catering, boarding)
        /// </summary>
        /// <param name="refuelState">The current refuel state</param>
        /// <param name="cateringState">The current catering state</param>
        public void RunLoadingServices(int refuelState, int cateringState)
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RunLoadingServices", 
                $"Running loading services with refuelState={refuelState}, cateringState={cateringState} (mock)");
            
            // Simulate service execution
            if (refuelState > 0 && !_serviceCompletionStatus["Refueling"])
            {
                RequestRefueling();
            }
            
            if (cateringState > 0 && !_serviceCompletionStatus["Catering"])
            {
                RequestCatering();
            }
            
            if (!_serviceCompletionStatus["Boarding"])
            {
                RequestBoarding();
            }
        }

        /// <summary>
        /// Runs departure services (loadsheet, equipment removal, pushback)
        /// </summary>
        /// <param name="departureState">The current departure state</param>
        public void RunDepartureServices(int departureState)
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RunDepartureServices", 
                $"Running departure services with departureState={departureState} (mock)");
            
            // Simulate service execution
            if (departureState > 0)
            {
                if (!_preliminaryLoadsheetSent)
                {
                    _preliminaryLoadsheetSent = true;
                    OnServiceStatusChanged("LoadsheetPreliminary", "Sent", true);
                }
                
                if (!_finalLoadsheetSent)
                {
                    _finalLoadsheetSent = true;
                    OnServiceStatusChanged("LoadsheetFinal", "Sent", true);
                }
                
                if (!_equipmentRemoved)
                {
                    _equipmentRemoved = true;
                    OnServiceStatusChanged("EquipmentRemoval", "Completed", true);
                }
                
                if (!_pushbackComplete)
                {
                    _pushbackComplete = true;
                    OnServiceStatusChanged("Pushback", "Completed", true);
                }
            }
        }

        /// <summary>
        /// Runs arrival services (jetway/stairs, PCA, GPU, chocks)
        /// </summary>
        /// <param name="deboardState">The current deboard state</param>
        public void RunArrivalServices(int deboardState)
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RunArrivalServices", 
                $"Running arrival services with deboardState={deboardState} (mock)");
            
            // Simulate service execution
            if (deboardState > 0)
            {
                OnServiceStatusChanged("Jetway", "Connected", true);
                OnServiceStatusChanged("GPU", "Connected", true);
                OnServiceStatusChanged("PCA", "Connected", true);
                OnServiceStatusChanged("Chocks", "Placed", true);
            }
        }

        /// <summary>
        /// Runs deboarding service
        /// </summary>
        /// <param name="deboardState">The current deboard state</param>
        public void RunDeboardingService(int deboardState)
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:RunDeboardingService", 
                $"Running deboarding service with deboardState={deboardState} (mock)");
            
            // Simulate service execution
            if (deboardState > 0 && !_serviceCompletionStatus["Deboarding"])
            {
                RequestDeBoarding();
            }
        }

        /// <summary>
        /// Checks if refueling is complete
        /// </summary>
        /// <returns>True if refueling is complete, false otherwise</returns>
        public bool IsRefuelingComplete()
        {
            return _serviceCompletionStatus["Refueling"];
        }

        /// <summary>
        /// Checks if boarding is complete
        /// </summary>
        /// <returns>True if boarding is complete, false otherwise</returns>
        public bool IsBoardingComplete()
        {
            return _serviceCompletionStatus["Boarding"];
        }

        /// <summary>
        /// Checks if catering is complete
        /// </summary>
        /// <returns>True if catering is complete, false otherwise</returns>
        public bool IsCateringComplete()
        {
            return _serviceCompletionStatus["Catering"];
        }

        /// <summary>
        /// Checks if the loadsheet has been sent
        /// </summary>
        /// <returns>True if the loadsheet has been sent, false otherwise</returns>
        public bool IsFinalLoadsheetSent()
        {
            return _finalLoadsheetSent;
        }

        /// <summary>
        /// Checks if the preliminary loadsheet has been sent
        /// </summary>
        /// <returns>True if the preliminary loadsheet has been sent, false otherwise</returns>
        public bool IsPreliminaryLoadsheetSent()
        {
            return _preliminaryLoadsheetSent;
        }

        /// <summary>
        /// Checks if equipment has been removed
        /// </summary>
        /// <returns>True if equipment has been removed, false otherwise</returns>
        public bool IsEquipmentRemoved()
        {
            return _equipmentRemoved;
        }

        /// <summary>
        /// Checks if pushback is complete
        /// </summary>
        /// <returns>True if pushback is complete, false otherwise</returns>
        public bool IsPushbackComplete()
        {
            return _pushbackComplete;
        }

        /// <summary>
        /// Checks if deboarding is complete
        /// </summary>
        /// <returns>True if deboarding is complete, false otherwise</returns>
        public bool IsDeboardingComplete()
        {
            return _serviceCompletionStatus["Deboarding"];
        }

        /// <summary>
        /// Sets the number of passengers
        /// </summary>
        /// <param name="numPax">The number of passengers</param>
        public void SetPassengers(int numPax)
        {
            _passengerCount = numPax;
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:SetPassengers", 
                $"Passenger count set to {numPax} (mock)");
        }

        /// <summary>
        /// Calls jetway and/or stairs
        /// </summary>
        public void CallJetwayStairs()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:CallJetwayStairs", 
                "Jetway/stairs called (mock)");
            OnServiceStatusChanged("Jetway", "Called", false);
            
            // Simulate jetway/stairs connection after a delay
            Task.Run(async () => {
                await Task.Delay(3000); // 3 seconds delay
                OnServiceStatusChanged("Jetway", "Connected", true);
            });
        }

        /// <summary>
        /// Resets the service status
        /// </summary>
        public void ResetServiceStatus()
        {
            _logger?.Log(LogLevel.Information, "MockGSXServiceOrchestrator:ResetServiceStatus", 
                "Resetting service status (mock)");
            
            foreach (var key in _serviceCompletionStatus.Keys)
            {
                _serviceCompletionStatus[key] = false;
            }
            
            _equipmentRemoved = false;
            _pushbackComplete = false;
            _preliminaryLoadsheetSent = false;
            _finalLoadsheetSent = false;
        }

        /// <summary>
        /// Raises the StateChanged event
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        protected virtual void OnStateChanged(FlightState previousState, FlightState newState)
        {
            StateChanged?.Invoke(this, new StateChangedEventArgs(previousState, newState));
        }

        /// <summary>
        /// Raises the ServiceStatusChanged event
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="status">The new status</param>
        /// <param name="isCompleted">Whether the service is completed</param>
        protected virtual void OnServiceStatusChanged(string serviceType, string status, bool isCompleted)
        {
            ServiceStatusChanged?.Invoke(this, new ServiceStatusChangedEventArgs(serviceType, status, isCompleted));
        }

        /// <summary>
        /// Raises the ServicePredicted event
        /// </summary>
        /// <param name="predictions">The predicted services</param>
        /// <param name="state">The current flight state</param>
        protected virtual void OnServicePredicted(IReadOnlyCollection<ServicePrediction> predictions, FlightState state)
        {
            ServicePredicted?.Invoke(this, new ServicePredictionEventArgs(predictions, state));
        }
    }
}
