using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates passenger operations between GSX and ProSim
    /// </summary>
    public class GSXPassengerCoordinator : IGSXPassengerCoordinator
    {
        private readonly IProsimPassengerService _prosimPassengerService;
        private readonly IGSXServiceOrchestrator _serviceOrchestrator;
        private readonly ILogger _logger;
        private IGSXStateManager _stateManager;
        private bool _disposed;
        
        // Passenger state tracking
        private int _passengersPlanned;
        private int _passengersCurrent;
        private bool _isBoardingInProgress;
        private bool _isDeboardingInProgress;
        
        /// <summary>
        /// Event raised when passenger state changes
        /// </summary>
        public event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
        
        /// <summary>
        /// Gets the planned number of passengers
        /// </summary>
        public int PassengersPlanned => _passengersPlanned;
        
        /// <summary>
        /// Gets the current number of passengers
        /// </summary>
        public int PassengersCurrent => _passengersCurrent;
        
        /// <summary>
        /// Gets a value indicating whether boarding is in progress
        /// </summary>
        public bool IsBoardingInProgress => _isBoardingInProgress;
        
        /// <summary>
        /// Gets a value indicating whether deboarding is in progress
        /// </summary>
        public bool IsDeboardingInProgress => _isDeboardingInProgress;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GSXPassengerCoordinator"/> class
        /// </summary>
        /// <param name="prosimPassengerService">The ProSim passenger service</param>
        /// <param name="serviceOrchestrator">The GSX service orchestrator (can be null and set later)</param>
        /// <param name="logger">The logger</param>
        public GSXPassengerCoordinator(
            IProsimPassengerService prosimPassengerService,
            IGSXServiceOrchestrator serviceOrchestrator,
            ILogger logger)
        {
            _prosimPassengerService = prosimPassengerService ?? throw new ArgumentNullException(nameof(prosimPassengerService));
            _serviceOrchestrator = serviceOrchestrator; // Can be null initially, set later via SetServiceOrchestrator
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Subscribe to passenger state change events
            _prosimPassengerService.PassengerStateChanged += OnProsimPassengerStateChanged;
        }
        
        /// <summary>
        /// Sets the service orchestrator
        /// </summary>
        /// <param name="serviceOrchestrator">The GSX service orchestrator</param>
        public void SetServiceOrchestrator(IGSXServiceOrchestrator serviceOrchestrator)
        {
            _serviceOrchestrator = serviceOrchestrator ?? throw new ArgumentNullException(nameof(serviceOrchestrator));
            _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:SetServiceOrchestrator", "Service orchestrator set");
        }
        
        /// <summary>
        /// Initializes the passenger coordinator
        /// </summary>
        public void Initialize()
        {
            _logger.Log(LogLevel.Information, "GSXPassengerCoordinator:Initialize", "Initializing passenger coordinator");
            
            // Initialize passenger counts
            _passengersPlanned = _prosimPassengerService.GetPaxPlanned();
            _passengersCurrent = _prosimPassengerService.GetPaxCurrent();
        }
        
        /// <summary>
        /// Starts the boarding process
        /// </summary>
        /// <returns>True if boarding was started successfully, false otherwise</returns>
        public bool StartBoarding()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:StartBoarding", "Starting boarding process");
                
                if (_isBoardingInProgress)
                {
                    _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StartBoarding", "Boarding already in progress");
                    return false;
                }
                
                if (_isDeboardingInProgress)
                {
                    _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StartBoarding", "Cannot start boarding while deboarding is in progress");
                    return false;
                }
                
                _prosimPassengerService.BoardingStart();
                _isBoardingInProgress = true;
                
                // Update passenger counts
                _passengersPlanned = _prosimPassengerService.GetPaxPlanned();
                _passengersCurrent = _prosimPassengerService.GetPaxCurrent();
                
                OnPassengerStateChanged("BoardingStarted", _passengersCurrent, _passengersPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:StartBoarding", $"Error starting boarding: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the boarding process
        /// </summary>
        /// <returns>True if boarding was stopped successfully, false otherwise</returns>
        public bool StopBoarding()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:StopBoarding", "Stopping boarding process");
                
                if (!_isBoardingInProgress)
                {
                    _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StopBoarding", "No boarding in progress");
                    return false;
                }
                
                _prosimPassengerService.BoardingStop();
                _isBoardingInProgress = false;
                
                // Update passenger counts
                _passengersPlanned = _prosimPassengerService.GetPaxPlanned();
                _passengersCurrent = _prosimPassengerService.GetPaxCurrent();
                
                OnPassengerStateChanged("BoardingStopped", _passengersCurrent, _passengersPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:StopBoarding", $"Error stopping boarding: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the deboarding process
        /// </summary>
        /// <returns>True if deboarding was started successfully, false otherwise</returns>
        public bool StartDeboarding()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:StartDeboarding", "Starting deboarding process");
                
                if (_isDeboardingInProgress)
                {
                    _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StartDeboarding", "Deboarding already in progress");
                    return false;
                }
                
                if (_isBoardingInProgress)
                {
                    _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StartDeboarding", "Cannot start deboarding while boarding is in progress");
                    return false;
                }
                
                _prosimPassengerService.DeboardingStart();
                _isDeboardingInProgress = true;
                
                // Update passenger counts
                _passengersPlanned = _prosimPassengerService.GetPaxPlanned();
                _passengersCurrent = _prosimPassengerService.GetPaxCurrent();
                
                OnPassengerStateChanged("DeboardingStarted", _passengersCurrent, _passengersPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:StartDeboarding", $"Error starting deboarding: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the deboarding process
        /// </summary>
        /// <returns>True if deboarding was stopped successfully, false otherwise</returns>
        public bool StopDeboarding()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:StopDeboarding", "Stopping deboarding process");
                
                if (!_isDeboardingInProgress)
                {
                    _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StopDeboarding", "No deboarding in progress");
                    return false;
                }
                
                _prosimPassengerService.DeboardingStop();
                _isDeboardingInProgress = false;
                
                // Update passenger counts
                _passengersPlanned = _prosimPassengerService.GetPaxPlanned();
                _passengersCurrent = _prosimPassengerService.GetPaxCurrent();
                
                OnPassengerStateChanged("DeboardingStopped", _passengersCurrent, _passengersPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:StopDeboarding", $"Error stopping deboarding: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Updates the passenger count
        /// </summary>
        /// <param name="passengerCount">The new passenger count</param>
        /// <returns>True if the passenger count was updated successfully, false otherwise</returns>
        public bool UpdatePassengerCount(int passengerCount)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:UpdatePassengerCount", $"Updating passenger count to {passengerCount}");
                
                if (passengerCount < 0 || passengerCount > 132)
                {
                    _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:UpdatePassengerCount", $"Invalid passenger count: {passengerCount}");
                    return false;
                }
                
                _prosimPassengerService.UpdateFromFlightPlan(passengerCount, true);
                
                // Update passenger counts
                _passengersPlanned = _prosimPassengerService.GetPaxPlanned();
                _passengersCurrent = _prosimPassengerService.GetPaxCurrent();
                
                OnPassengerStateChanged("PassengerCountUpdated", _passengersCurrent, _passengersPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:UpdatePassengerCount", $"Error updating passenger count: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the boarding process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if boarding was started successfully, false otherwise</returns>
        public async Task<bool> StartBoardingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:StartBoardingAsync", "Starting boarding process asynchronously");
                
                return await Task.Run(() => StartBoarding(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StartBoardingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:StartBoardingAsync", $"Error starting boarding: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the boarding process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if boarding was stopped successfully, false otherwise</returns>
        public async Task<bool> StopBoardingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:StopBoardingAsync", "Stopping boarding process asynchronously");
                
                return await Task.Run(() => StopBoarding(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StopBoardingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:StopBoardingAsync", $"Error stopping boarding: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the deboarding process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if deboarding was started successfully, false otherwise</returns>
        public async Task<bool> StartDeboardingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:StartDeboardingAsync", "Starting deboarding process asynchronously");
                
                return await Task.Run(() => StartDeboarding(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StartDeboardingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:StartDeboardingAsync", $"Error starting deboarding: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the deboarding process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if deboarding was stopped successfully, false otherwise</returns>
        public async Task<bool> StopDeboardingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:StopDeboardingAsync", "Stopping deboarding process asynchronously");
                
                return await Task.Run(() => StopDeboarding(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:StopDeboardingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:StopDeboardingAsync", $"Error stopping deboarding: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Updates the passenger count asynchronously
        /// </summary>
        /// <param name="passengerCount">The new passenger count</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the passenger count was updated successfully, false otherwise</returns>
        public async Task<bool> UpdatePassengerCountAsync(int passengerCount, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:UpdatePassengerCountAsync", $"Updating passenger count to {passengerCount} asynchronously");
                
                return await Task.Run(() => UpdatePassengerCount(passengerCount), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:UpdatePassengerCountAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:UpdatePassengerCountAsync", $"Error updating passenger count: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Synchronizes passenger states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task SynchronizePassengerStatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:SynchronizePassengerStatesAsync", "Synchronizing passenger states");
                
                // Update passenger counts
                _passengersPlanned = _prosimPassengerService.GetPaxPlanned();
                _passengersCurrent = _prosimPassengerService.GetPaxCurrent();
                
                // Set the passenger count in the service orchestrator if available
                if (_serviceOrchestrator != null)
                {
                    _serviceOrchestrator.SetPassengers(_passengersPlanned);
                }
                else
                {
                    _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:SynchronizePassengerStatesAsync", 
                        "Service orchestrator not available, passenger count not set");
                }
                
                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:SynchronizePassengerStatesAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:SynchronizePassengerStatesAsync", $"Error synchronizing passenger states: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Manages passengers based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ManagePassengersForStateAsync(FlightState state, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:ManagePassengersForStateAsync", $"Managing passengers for state: {state}");
                
                switch (state)
                {
                    case FlightState.PREFLIGHT:
                        // In preflight, ensure passenger counts are synchronized
                        await SynchronizePassengerStatesAsync(cancellationToken);
                        break;
                        
                    case FlightState.DEPARTURE:
                        // In departure, start boarding if not already in progress
                        if (!_isBoardingInProgress && !_isDeboardingInProgress)
                        {
                            await StartBoardingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.TAXIOUT:
                        // In taxiout, stop boarding if in progress
                        if (_isBoardingInProgress)
                        {
                            await StopBoardingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.FLIGHT:
                        // In flight, ensure boarding is stopped
                        if (_isBoardingInProgress)
                        {
                            await StopBoardingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.TAXIIN:
                        // In taxiin, no passenger operations
                        break;
                        
                    case FlightState.ARRIVAL:
                        // In arrival, start deboarding if not already in progress
                        if (!_isDeboardingInProgress && !_isBoardingInProgress)
                        {
                            await StartDeboardingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.TURNAROUND:
                        // In turnaround, stop deboarding if in progress
                        if (_isDeboardingInProgress)
                        {
                            await StopDeboardingAsync(cancellationToken);
                        }
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXPassengerCoordinator:ManagePassengersForStateAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:ManagePassengersForStateAsync", $"Error managing passengers for state {state}: {ex.Message}");
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
        /// Handles passenger state changes from ProSim
        /// </summary>
        private void OnProsimPassengerStateChanged(object sender, PassengerStateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:OnProsimPassengerStateChanged", 
                    $"Passenger state changed: {e.OperationType}, Current: {e.CurrentCount}, Planned: {e.PlannedCount}");
                
                // Update our internal state
                _passengersPlanned = e.PlannedCount;
                _passengersCurrent = e.CurrentCount;
                
                // Forward the event
                OnPassengerStateChanged(e.OperationType, e.CurrentCount, e.PlannedCount);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:OnProsimPassengerStateChanged", 
                    $"Error handling passenger state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles state changes from the state manager
        /// </summary>
        private async void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXPassengerCoordinator:OnStateChanged", 
                    $"State changed from {e.PreviousState} to {e.NewState}");
                
                // Manage passengers based on the new state
                await ManagePassengersForStateAsync(e.NewState);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXPassengerCoordinator:OnStateChanged", 
                    $"Error handling state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Raises the PassengerStateChanged event
        /// </summary>
        protected virtual void OnPassengerStateChanged(string operationType, int currentCount, int plannedCount)
        {
            PassengerStateChanged?.Invoke(this, new PassengerStateChangedEventArgs(operationType, currentCount, plannedCount));
        }
        
        /// <summary>
        /// Disposes resources used by the passenger coordinator
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources used by the passenger coordinator
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                // Unsubscribe from events
                _prosimPassengerService.PassengerStateChanged -= OnProsimPassengerStateChanged;
                
                if (_stateManager != null)
                    _stateManager.StateChanged -= OnStateChanged;
            }
            
            _disposed = true;
        }
    }
}
