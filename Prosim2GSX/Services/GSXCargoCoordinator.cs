using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates cargo operations between GSX and ProSim
    /// </summary>
    public class GSXCargoCoordinator : IGSXCargoCoordinator
    {
        private readonly IProsimCargoService _prosimCargoService;
        private readonly IGSXServiceOrchestrator _serviceOrchestrator;
        private readonly ILogger _logger;
        private IGSXStateManager _stateManager;
        private IGSXDoorCoordinator _doorCoordinator;
        private bool _disposed;
        
        // Cargo state tracking
        private int _cargoPlanned;
        private int _cargoCurrentPercentage;
        private bool _isLoadingInProgress;
        private bool _isUnloadingInProgress;
        private readonly object _stateLock = new object();
        
        /// <summary>
        /// Event raised when cargo state changes
        /// </summary>
        public event EventHandler<CargoStateChangedEventArgs> CargoStateChanged;
        
        /// <summary>
        /// Gets the planned cargo amount
        /// </summary>
        public int CargoPlanned => _cargoPlanned;
        
        /// <summary>
        /// Gets the current cargo percentage
        /// </summary>
        public int CargoCurrentPercentage => _cargoCurrentPercentage;
        
        /// <summary>
        /// Gets a value indicating whether loading is in progress
        /// </summary>
        public bool IsLoadingInProgress => _isLoadingInProgress;
        
        /// <summary>
        /// Gets a value indicating whether unloading is in progress
        /// </summary>
        public bool IsUnloadingInProgress => _isUnloadingInProgress;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GSXCargoCoordinator"/> class
        /// </summary>
        /// <param name="prosimCargoService">The ProSim cargo service</param>
        /// <param name="serviceOrchestrator">The GSX service orchestrator</param>
        /// <param name="logger">The logger</param>
        public GSXCargoCoordinator(
            IProsimCargoService prosimCargoService,
            IGSXServiceOrchestrator serviceOrchestrator,
            ILogger logger)
        {
            _prosimCargoService = prosimCargoService ?? throw new ArgumentNullException(nameof(prosimCargoService));
            _serviceOrchestrator = serviceOrchestrator ?? throw new ArgumentNullException(nameof(serviceOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Subscribe to cargo state change events
            _prosimCargoService.CargoStateChanged += OnProsimCargoStateChanged;
        }
        
        /// <summary>
        /// Initializes the cargo coordinator
        /// </summary>
        public void Initialize()
        {
            _logger.Log(LogLevel.Information, "GSXCargoCoordinator:Initialize", "Initializing cargo coordinator");
            
            // Initialize cargo amounts
            _cargoPlanned = _prosimCargoService.GetCargoPlanned();
            _cargoCurrentPercentage = _prosimCargoService.GetCargoCurrentPercentage();
        }
        
        /// <summary>
        /// Starts the cargo loading process
        /// </summary>
        /// <returns>True if loading was started successfully, false otherwise</returns>
        public bool StartLoading()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:StartLoading", "Starting cargo loading process");
                
                lock (_stateLock)
                {
                    if (_isLoadingInProgress)
                    {
                        _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StartLoading", "Loading already in progress");
                        return false;
                    }
                    
                    if (_isUnloadingInProgress)
                    {
                        _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StartLoading", "Cannot start loading while unloading is in progress");
                        return false;
                    }
                    
                    _isLoadingInProgress = true;
                }
                
                // Update cargo state
                _cargoPlanned = _prosimCargoService.GetCargoPlanned();
                _cargoCurrentPercentage = _prosimCargoService.GetCargoCurrentPercentage();
                
                // Coordinate with cargo doors if door coordinator is available
                if (_doorCoordinator != null)
                {
                    // Request cargo doors to be opened
                    Task.Run(async () => await CoordinateWithDoorsAsync(true)).Wait();
                }
                
                OnCargoStateChanged("LoadingStarted", _cargoCurrentPercentage, _cargoPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:StartLoading", $"Error starting loading: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the cargo loading process
        /// </summary>
        /// <returns>True if loading was stopped successfully, false otherwise</returns>
        public bool StopLoading()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:StopLoading", "Stopping cargo loading process");
                
                lock (_stateLock)
                {
                    if (!_isLoadingInProgress)
                    {
                        _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StopLoading", "No loading in progress");
                        return false;
                    }
                    
                    _isLoadingInProgress = false;
                }
                
                // Update cargo state
                _cargoPlanned = _prosimCargoService.GetCargoPlanned();
                _cargoCurrentPercentage = _prosimCargoService.GetCargoCurrentPercentage();
                
                // Coordinate with cargo doors if door coordinator is available
                if (_doorCoordinator != null)
                {
                    // Request cargo doors to be closed
                    Task.Run(async () => await CoordinateWithDoorsAsync(false)).Wait();
                }
                
                OnCargoStateChanged("LoadingStopped", _cargoCurrentPercentage, _cargoPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:StopLoading", $"Error stopping loading: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the cargo unloading process
        /// </summary>
        /// <returns>True if unloading was started successfully, false otherwise</returns>
        public bool StartUnloading()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:StartUnloading", "Starting cargo unloading process");
                
                lock (_stateLock)
                {
                    if (_isUnloadingInProgress)
                    {
                        _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StartUnloading", "Unloading already in progress");
                        return false;
                    }
                    
                    if (_isLoadingInProgress)
                    {
                        _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StartUnloading", "Cannot start unloading while loading is in progress");
                        return false;
                    }
                    
                    _isUnloadingInProgress = true;
                }
                
                // Update cargo state
                _cargoPlanned = _prosimCargoService.GetCargoPlanned();
                _cargoCurrentPercentage = _prosimCargoService.GetCargoCurrentPercentage();
                
                // Coordinate with cargo doors if door coordinator is available
                if (_doorCoordinator != null)
                {
                    // Request cargo doors to be opened
                    Task.Run(async () => await CoordinateWithDoorsAsync(true)).Wait();
                }
                
                OnCargoStateChanged("UnloadingStarted", _cargoCurrentPercentage, _cargoPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:StartUnloading", $"Error starting unloading: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the cargo unloading process
        /// </summary>
        /// <returns>True if unloading was stopped successfully, false otherwise</returns>
        public bool StopUnloading()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:StopUnloading", "Stopping cargo unloading process");
                
                lock (_stateLock)
                {
                    if (!_isUnloadingInProgress)
                    {
                        _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StopUnloading", "No unloading in progress");
                        return false;
                    }
                    
                    _isUnloadingInProgress = false;
                }
                
                // Update cargo state
                _cargoPlanned = _prosimCargoService.GetCargoPlanned();
                _cargoCurrentPercentage = _prosimCargoService.GetCargoCurrentPercentage();
                
                // Coordinate with cargo doors if door coordinator is available
                if (_doorCoordinator != null)
                {
                    // Request cargo doors to be closed
                    Task.Run(async () => await CoordinateWithDoorsAsync(false)).Wait();
                }
                
                OnCargoStateChanged("UnloadingStopped", _cargoCurrentPercentage, _cargoPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:StopUnloading", $"Error stopping unloading: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Updates the cargo amount
        /// </summary>
        /// <param name="cargoAmount">The new cargo amount</param>
        /// <returns>True if the cargo amount was updated successfully, false otherwise</returns>
        public bool UpdateCargoAmount(int cargoAmount)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:UpdateCargoAmount", $"Updating cargo amount to {cargoAmount}");
                
                if (cargoAmount < 0)
                {
                    _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:UpdateCargoAmount", $"Invalid cargo amount: {cargoAmount}");
                    return false;
                }
                
                _prosimCargoService.UpdateFromFlightPlan(cargoAmount, true);
                
                // Update cargo state
                _cargoPlanned = _prosimCargoService.GetCargoPlanned();
                _cargoCurrentPercentage = _prosimCargoService.GetCargoCurrentPercentage();
                
                OnCargoStateChanged("CargoAmountUpdated", _cargoCurrentPercentage, _cargoPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:UpdateCargoAmount", $"Error updating cargo amount: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Changes the cargo percentage
        /// </summary>
        /// <param name="percentage">The new cargo percentage (0-100)</param>
        /// <returns>True if the cargo percentage was changed successfully, false otherwise</returns>
        public bool ChangeCargoPercentage(int percentage)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:ChangeCargoPercentage", $"Changing cargo percentage to {percentage}%");
                
                if (percentage < 0 || percentage > 100)
                {
                    _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:ChangeCargoPercentage", $"Invalid percentage: {percentage}");
                    return false;
                }
                
                _prosimCargoService.ChangeCargo(percentage);
                
                // Update cargo state
                _cargoPlanned = _prosimCargoService.GetCargoPlanned();
                _cargoCurrentPercentage = _prosimCargoService.GetCargoCurrentPercentage();
                
                OnCargoStateChanged("CargoPercentageChanged", _cargoCurrentPercentage, _cargoPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:ChangeCargoPercentage", $"Error changing cargo percentage: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the cargo loading process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if loading was started successfully, false otherwise</returns>
        public async Task<bool> StartLoadingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:StartLoadingAsync", "Starting cargo loading process asynchronously");
                
                return await Task.Run(() => StartLoading(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StartLoadingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:StartLoadingAsync", $"Error starting loading: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the cargo loading process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if loading was stopped successfully, false otherwise</returns>
        public async Task<bool> StopLoadingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:StopLoadingAsync", "Stopping cargo loading process asynchronously");
                
                return await Task.Run(() => StopLoading(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StopLoadingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:StopLoadingAsync", $"Error stopping loading: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the cargo unloading process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if unloading was started successfully, false otherwise</returns>
        public async Task<bool> StartUnloadingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:StartUnloadingAsync", "Starting cargo unloading process asynchronously");
                
                return await Task.Run(() => StartUnloading(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StartUnloadingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:StartUnloadingAsync", $"Error starting unloading: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the cargo unloading process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if unloading was stopped successfully, false otherwise</returns>
        public async Task<bool> StopUnloadingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:StopUnloadingAsync", "Stopping cargo unloading process asynchronously");
                
                return await Task.Run(() => StopUnloading(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:StopUnloadingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:StopUnloadingAsync", $"Error stopping unloading: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Updates the cargo amount asynchronously
        /// </summary>
        /// <param name="cargoAmount">The new cargo amount</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the cargo amount was updated successfully, false otherwise</returns>
        public async Task<bool> UpdateCargoAmountAsync(int cargoAmount, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:UpdateCargoAmountAsync", $"Updating cargo amount to {cargoAmount} asynchronously");
                
                return await Task.Run(() => UpdateCargoAmount(cargoAmount), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:UpdateCargoAmountAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:UpdateCargoAmountAsync", $"Error updating cargo amount: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Changes the cargo percentage asynchronously
        /// </summary>
        /// <param name="percentage">The new cargo percentage (0-100)</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the cargo percentage was changed successfully, false otherwise</returns>
        public async Task<bool> ChangeCargoPercentageAsync(int percentage, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:ChangeCargoPercentageAsync", $"Changing cargo percentage to {percentage}% asynchronously");
                
                return await Task.Run(() => ChangeCargoPercentage(percentage), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:ChangeCargoPercentageAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:ChangeCargoPercentageAsync", $"Error changing cargo percentage: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Synchronizes cargo states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task SynchronizeCargoStatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:SynchronizeCargoStatesAsync", "Synchronizing cargo states");
                
                // Update cargo state
                _cargoPlanned = _prosimCargoService.GetCargoPlanned();
                _cargoCurrentPercentage = _prosimCargoService.GetCargoCurrentPercentage();
                
                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:SynchronizeCargoStatesAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:SynchronizeCargoStatesAsync", $"Error synchronizing cargo states: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Manages cargo based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ManageCargoForStateAsync(FlightState state, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:ManageCargoForStateAsync", $"Managing cargo for state: {state}");
                
                switch (state)
                {
                    case FlightState.PREFLIGHT:
                        // In preflight, ensure cargo amounts are synchronized
                        await SynchronizeCargoStatesAsync(cancellationToken);
                        break;
                        
                    case FlightState.DEPARTURE:
                        // In departure, start loading if not already in progress
                        if (!_isLoadingInProgress && !_isUnloadingInProgress)
                        {
                            await StartLoadingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.TAXIOUT:
                        // In taxiout, stop loading if in progress and ensure cargo doors are closed
                        if (_isLoadingInProgress)
                        {
                            await StopLoadingAsync(cancellationToken);
                            
                            // Ensure cargo doors are closed
                            if (_doorCoordinator != null)
                            {
                                await CoordinateWithDoorsAsync(false, cancellationToken);
                            }
                        }
                        break;
                        
                    case FlightState.FLIGHT:
                        // In flight, ensure loading is stopped and cargo doors are closed
                        if (_isLoadingInProgress)
                        {
                            await StopLoadingAsync(cancellationToken);
                        }
                        
                        // Double-check cargo doors are closed
                        if (_doorCoordinator != null)
                        {
                            await CoordinateWithDoorsAsync(false, cancellationToken);
                        }
                        break;
                        
                    case FlightState.TAXIIN:
                        // In taxiin, no cargo operations
                        break;
                        
                    case FlightState.ARRIVAL:
                        // In arrival, start unloading if not already in progress
                        if (!_isUnloadingInProgress && !_isLoadingInProgress)
                        {
                            await StartUnloadingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.TURNAROUND:
                        // In turnaround, stop unloading if in progress
                        if (_isUnloadingInProgress)
                        {
                            await StopUnloadingAsync(cancellationToken);
                        }
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:ManageCargoForStateAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:ManageCargoForStateAsync", 
                    $"Error managing cargo for state {state}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Coordinates with doors for cargo operations
        /// </summary>
        /// <param name="forLoading">True for loading operations, false for unloading or closing</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task CoordinateWithDoorsAsync(bool forLoading, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_doorCoordinator == null)
                {
                    _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:CoordinateWithDoorsAsync", "Door coordinator not available");
                    return;
                }
                
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:CoordinateWithDoorsAsync", 
                    $"Coordinating cargo doors for {(forLoading ? "loading" : "unloading")}");
                
                // For A320, cargo doors are typically:
                // - Forward cargo door
                // - Aft cargo door
                
                if (forLoading)
                {
                    // Open cargo doors for loading
                    await _doorCoordinator.OpenDoorAsync("FWD_CARGO", cancellationToken);
                    await _doorCoordinator.OpenDoorAsync("AFT_CARGO", cancellationToken);
                }
                else
                {
                    // Close cargo doors after loading/unloading
                    await _doorCoordinator.CloseDoorAsync("FWD_CARGO", cancellationToken);
                    await _doorCoordinator.CloseDoorAsync("AFT_CARGO", cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXCargoCoordinator:CoordinateWithDoorsAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:CoordinateWithDoorsAsync", 
                    $"Error coordinating with doors: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        public void RegisterForStateChanges(IGSXStateManager stateManager)
        {
            if (stateManager == null)
                throw new ArgumentNullException(nameof(stateManager));
                
            _stateManager = stateManager;
            _stateManager.StateChanged += OnStateChanged;
        }
        
        /// <summary>
        /// Registers a door coordinator for cargo door operations
        /// </summary>
        /// <param name="doorCoordinator">The door coordinator to register</param>
        public void RegisterDoorCoordinator(IGSXDoorCoordinator doorCoordinator)
        {
            _doorCoordinator = doorCoordinator ?? throw new ArgumentNullException(nameof(doorCoordinator));
            _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:RegisterDoorCoordinator", "Door coordinator registered");
        }
        
        /// <summary>
        /// Handles cargo state changes from ProSim
        /// </summary>
        private void OnProsimCargoStateChanged(object sender, CargoStateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:OnProsimCargoStateChanged", 
                    $"Cargo state changed: {e.OperationType}, Current: {e.CurrentPercentage}%, Planned: {e.PlannedAmount}");
                
                // Update our internal state
                _cargoPlanned = e.PlannedAmount;
                _cargoCurrentPercentage = e.CurrentPercentage;
                
                // Forward the event
                OnCargoStateChanged(e.OperationType, e.CurrentPercentage, e.PlannedAmount);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:OnProsimCargoStateChanged", 
                    $"Error handling cargo state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles state changes from the state manager
        /// </summary>
        private async void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXCargoCoordinator:OnStateChanged", 
                    $"State changed from {e.PreviousState} to {e.NewState}");
                
                // Manage cargo based on the new state
                await ManageCargoForStateAsync(e.NewState);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXCargoCoordinator:OnStateChanged", 
                    $"Error handling state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Raises the CargoStateChanged event
        /// </summary>
        protected virtual void OnCargoStateChanged(string operationType, int currentPercentage, int plannedAmount)
        {
            CargoStateChanged?.Invoke(this, new CargoStateChangedEventArgs(operationType, currentPercentage, plannedAmount));
        }
        
        /// <summary>
        /// Disposes resources used by the cargo coordinator
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources used by the cargo coordinator
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                // Unsubscribe from events
                _prosimCargoService.CargoStateChanged -= OnProsimCargoStateChanged;
                
                if (_stateManager != null)
                    _stateManager.StateChanged -= OnStateChanged;
            }
            
            _disposed = true;
        }
    }
}
