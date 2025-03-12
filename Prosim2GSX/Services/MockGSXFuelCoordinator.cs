using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Mock implementation of IGSXFuelCoordinator for use when the real service is not available
    /// </summary>
    public class MockGSXFuelCoordinator : IGSXFuelCoordinator
    {
        private readonly ILogger _logger;
        private bool _isRefuelingInProgress = false;
        private double _refuelingProgress = 0.0;
        private double _targetFuelQuantity = 10000.0; // Default to 10000 kg
        private double _currentFuelQuantity = 5000.0; // Default to 5000 kg
        private string _fuelUnits = "KG";
        private RefuelingState _refuelingState = RefuelingState.Idle;
        private float _fuelRateKGS = 10.0f; // 10 kg/s
        private IGSXServiceOrchestrator _serviceOrchestrator;
        private IGSXStateManager _stateManager;
        private IEventAggregator _eventAggregator;

        /// <summary>
        /// Gets the current refueling state
        /// </summary>
        public RefuelingState RefuelingState => _refuelingState;

        /// <summary>
        /// Gets the planned fuel amount in kg
        /// </summary>
        public double FuelPlanned => _targetFuelQuantity;

        /// <summary>
        /// Gets the current fuel amount in kg
        /// </summary>
        public double FuelCurrent => _currentFuelQuantity;

        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        public string FuelUnits => _fuelUnits;

        /// <summary>
        /// Gets the refueling progress percentage (0-100)
        /// </summary>
        public int RefuelingProgressPercentage => (int)(_refuelingProgress * 100);

        /// <summary>
        /// Gets a value indicating whether refueling is in progress
        /// </summary>
        public bool IsRefuelingInProgress => _isRefuelingInProgress;

        /// <summary>
        /// Gets the fuel rate in kg/s
        /// </summary>
        public float FuelRateKGS => _fuelRateKGS;

        /// <summary>
        /// Event that fires when refueling state changes
        /// </summary>
        public event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;

        /// <summary>
        /// Event that fires when refueling progress changes
        /// </summary>
        public event EventHandler<RefuelingProgressChangedEventArgs> RefuelingProgressChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockGSXFuelCoordinator"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public MockGSXFuelCoordinator(ILogger logger)
        {
            _logger = logger;
            _logger?.Log(LogLevel.Warning, "MockGSXFuelCoordinator:Constructor", 
                "Using mock fuel coordinator. Fuel operations will not affect the actual aircraft.");
        }

        /// <summary>
        /// Initializes the fuel coordinator
        /// </summary>
        public void Initialize()
        {
            _logger?.Log(LogLevel.Information, "MockGSXFuelCoordinator:Initialize", 
                "Initializing mock fuel coordinator");
        }

        /// <summary>
        /// Starts the refueling process
        /// </summary>
        /// <returns>True if refueling was started successfully, false otherwise</returns>
        public bool StartRefueling()
        {
            if (_isRefuelingInProgress)
            {
                _logger?.Log(LogLevel.Warning, "MockGSXFuelCoordinator:StartRefueling", 
                    "Refueling already in progress (mock)");
                return false;
            }

            _refuelingState = RefuelingState.Refueling;
            _isRefuelingInProgress = true;
            _refuelingProgress = 0.0;

            _logger?.Log(LogLevel.Information, "MockGSXFuelCoordinator:StartRefueling", 
                $"Refueling started to {_targetFuelQuantity} {_fuelUnits} (mock)");

            // Raise events
            OnFuelStateChanged("Refueling", _currentFuelQuantity, _targetFuelQuantity, _fuelUnits, true);
            OnRefuelingProgressChanged(0);

            return true;
        }

        /// <summary>
        /// Stops the refueling process
        /// </summary>
        /// <returns>True if refueling was stopped successfully, false otherwise</returns>
        public bool StopRefueling()
        {
            if (!_isRefuelingInProgress)
            {
                _logger?.Log(LogLevel.Warning, "MockGSXFuelCoordinator:StopRefueling", 
                    "No refueling in progress to stop (mock)");
                return false;
            }

            _refuelingState = RefuelingState.Idle;
            _isRefuelingInProgress = false;

            _logger?.Log(LogLevel.Information, "MockGSXFuelCoordinator:StopRefueling", 
                "Refueling stopped (mock)");

            // Raise event
            OnFuelStateChanged("RefuelingStopped", _currentFuelQuantity, _targetFuelQuantity, _fuelUnits, false);

            return true;
        }

        /// <summary>
        /// Starts the defueling process
        /// </summary>
        /// <returns>True if defueling was started successfully, false otherwise</returns>
        public bool StartDefueling()
        {
            if (_isRefuelingInProgress)
            {
                _logger?.Log(LogLevel.Warning, "MockGSXFuelCoordinator:StartDefueling", 
                    "Refueling already in progress, cannot start defueling (mock)");
                return false;
            }

            _refuelingState = RefuelingState.Defueling;
            _isRefuelingInProgress = true;
            _refuelingProgress = 0.0;

            _logger?.Log(LogLevel.Information, "MockGSXFuelCoordinator:StartDefueling", 
                $"Defueling started from {_currentFuelQuantity} {_fuelUnits} (mock)");

            // Raise events
            OnFuelStateChanged("Defueling", _currentFuelQuantity, 0, _fuelUnits, true);
            OnRefuelingProgressChanged(0);

            return true;
        }

        /// <summary>
        /// Stops the defueling process
        /// </summary>
        /// <returns>True if defueling was stopped successfully, false otherwise</returns>
        public bool StopDefueling()
        {
            if (!_isRefuelingInProgress || _refuelingState != RefuelingState.Defueling)
            {
                _logger?.Log(LogLevel.Warning, "MockGSXFuelCoordinator:StopDefueling", 
                    "No defueling in progress to stop (mock)");
                return false;
            }

            _refuelingState = RefuelingState.Idle;
            _isRefuelingInProgress = false;

            _logger?.Log(LogLevel.Information, "MockGSXFuelCoordinator:StopDefueling", 
                "Defueling stopped (mock)");

            // Raise event
            OnFuelStateChanged("DefuelingStopped", _currentFuelQuantity, 0, _fuelUnits, false);

            return true;
        }

        /// <summary>
        /// Updates the fuel amount
        /// </summary>
        /// <param name="fuelAmount">The new fuel amount in kg</param>
        /// <returns>True if the fuel amount was updated successfully, false otherwise</returns>
        public bool UpdateFuelAmount(double fuelAmount)
        {
            _currentFuelQuantity = fuelAmount;

            _logger?.Log(LogLevel.Information, "MockGSXFuelCoordinator:UpdateFuelAmount", 
                $"Fuel amount updated to {_currentFuelQuantity} {_fuelUnits} (mock)");

            // Raise event
            OnFuelStateChanged("FuelUpdated", _currentFuelQuantity, _targetFuelQuantity, _fuelUnits, _isRefuelingInProgress);

            return true;
        }

        /// <summary>
        /// Gets the current refueling progress
        /// </summary>
        /// <returns>The refueling progress percentage (0-100)</returns>
        public int GetRefuelingProgress()
        {
            return RefuelingProgressPercentage;
        }

        /// <summary>
        /// Requests refueling to the specified quantity
        /// </summary>
        /// <param name="targetQuantity">The target fuel quantity in gallons</param>
        /// <returns>True if the request was successful, false otherwise</returns>
        public bool RequestRefueling(double targetQuantity)
        {
            if (_isRefuelingInProgress)
            {
                _logger?.Log(LogLevel.Warning, "MockGSXFuelCoordinator:RequestRefueling", 
                    "Refueling already in progress (mock)");
                return false;
            }

            _targetFuelQuantity = targetQuantity > 0 ? targetQuantity : 10000.0; // Default to 10000 kg if not specified
            _refuelingProgress = 0.0;
            _refuelingState = RefuelingState.Requested;

            _logger?.Log(LogLevel.Information, "MockGSXFuelCoordinator:RequestRefueling", 
                $"Refueling requested to {_targetFuelQuantity} {_fuelUnits} (mock)");

            // Raise events
            OnFuelStateChanged("RefuelingRequested", _currentFuelQuantity, _targetFuelQuantity, _fuelUnits, false);

            return true;
        }

        /// <summary>
        /// Cancels the current refueling operation
        /// </summary>
        /// <returns>True if the cancellation was successful, false otherwise</returns>
        public bool CancelRefueling()
        {
            if (!_isRefuelingInProgress)
            {
                _logger?.Log(LogLevel.Warning, "MockGSXFuelCoordinator:CancelRefueling", 
                    "No refueling in progress to cancel (mock)");
                return false;
            }

            _isRefuelingInProgress = false;
            _refuelingState = RefuelingState.Idle;

            _logger?.Log(LogLevel.Information, "MockGSXFuelCoordinator:CancelRefueling", 
                "Refueling cancelled (mock)");

            // Raise event
            OnFuelStateChanged("RefuelingCancelled", _currentFuelQuantity, _targetFuelQuantity, _fuelUnits, false);

            return true;
        }

        /// <summary>
        /// Updates the refueling progress (for simulation purposes)
        /// </summary>
        /// <param name="progress">The new progress value between 0.0 and 1.0</param>
        public void UpdateRefuelingProgress(double progress)
        {
            if (!_isRefuelingInProgress)
                return;

            _refuelingProgress = Math.Clamp(progress, 0.0, 1.0);
            
            if (_refuelingState == RefuelingState.Refueling)
            {
                _currentFuelQuantity = _currentFuelQuantity + (_targetFuelQuantity - _currentFuelQuantity) * _refuelingProgress;
            }
            else if (_refuelingState == RefuelingState.Defueling)
            {
                _currentFuelQuantity = _currentFuelQuantity * (1 - _refuelingProgress);
            }

            _logger?.Log(LogLevel.Debug, "MockGSXFuelCoordinator:UpdateRefuelingProgress", 
                $"Refueling progress updated to {_refuelingProgress:P0}, current fuel: {_currentFuelQuantity:F0} {_fuelUnits} (mock)");

            // Raise event
            OnRefuelingProgressChanged(RefuelingProgressPercentage);

            // If refueling is complete, update the state
            if (_refuelingProgress >= 1.0)
            {
                _isRefuelingInProgress = false;
                _refuelingState = RefuelingState.Complete;
                _logger?.Log(LogLevel.Information, "MockGSXFuelCoordinator:UpdateRefuelingProgress", 
                    "Refueling completed (mock)");
                OnFuelStateChanged("RefuelingCompleted", _currentFuelQuantity, _targetFuelQuantity, _fuelUnits, false);
            }
        }

        /// <summary>
        /// Starts the refueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if refueling was started successfully, false otherwise</returns>
        public async Task<bool> StartRefuelingAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(StartRefueling());
        }

        /// <summary>
        /// Stops the refueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if refueling was stopped successfully, false otherwise</returns>
        public async Task<bool> StopRefuelingAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(StopRefueling());
        }

        /// <summary>
        /// Starts the defueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if defueling was started successfully, false otherwise</returns>
        public async Task<bool> StartDefuelingAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(StartDefueling());
        }

        /// <summary>
        /// Stops the defueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if defueling was stopped successfully, false otherwise</returns>
        public async Task<bool> StopDefuelingAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(StopDefueling());
        }

        /// <summary>
        /// Updates the fuel amount asynchronously
        /// </summary>
        /// <param name="fuelAmount">The new fuel amount in kg</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the fuel amount was updated successfully, false otherwise</returns>
        public async Task<bool> UpdateFuelAmountAsync(double fuelAmount, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(UpdateFuelAmount(fuelAmount));
        }

        /// <summary>
        /// Synchronizes fuel quantities between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task SynchronizeFuelQuantitiesAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Calculates the required fuel based on the flight plan
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the required fuel amount in kg</returns>
        public async Task<double> CalculateRequiredFuelAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(10000.0); // Default to 10000 kg
        }

        /// <summary>
        /// Manages fuel based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ManageFuelForStateAsync(FlightState state, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Sets the service orchestrator
        /// </summary>
        /// <param name="serviceOrchestrator">The GSX service orchestrator</param>
        public void SetServiceOrchestrator(IGSXServiceOrchestrator serviceOrchestrator)
        {
            _serviceOrchestrator = serviceOrchestrator;
        }

        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        public void RegisterForStateChanges(IGSXStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        /// <summary>
        /// Sets the event aggregator for publishing events
        /// </summary>
        /// <param name="eventAggregator">The event aggregator</param>
        public void SetEventAggregator(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// Raises the FuelStateChanged event
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentAmount">The current fuel amount</param>
        /// <param name="plannedAmount">The planned fuel amount</param>
        /// <param name="fuelUnits">The fuel units (KG or LBS)</param>
        /// <param name="isRefueling">Whether refueling is in progress</param>
        protected virtual void OnFuelStateChanged(string operationType, double currentAmount, double plannedAmount, string fuelUnits, bool isRefueling)
        {
            FuelStateChanged?.Invoke(this, new FuelStateChangedEventArgs(operationType, currentAmount, plannedAmount, fuelUnits, isRefueling));
        }

        /// <summary>
        /// Raises the RefuelingProgressChanged event
        /// </summary>
        /// <param name="progressPercentage">The progress percentage (0-100)</param>
        protected virtual void OnRefuelingProgressChanged(int progressPercentage)
        {
            RefuelingProgressChanged?.Invoke(this, new RefuelingProgressChangedEventArgs(progressPercentage, _currentFuelQuantity, _targetFuelQuantity, _fuelUnits));
        }

        /// <summary>
        /// Disposes resources used by the coordinator
        /// </summary>
        public void Dispose()
        {
            // No resources to dispose
        }
    }
}
