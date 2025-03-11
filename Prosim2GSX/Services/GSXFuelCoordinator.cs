using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates fuel operations between GSX and ProSim
    /// </summary>
    public class GSXFuelCoordinator : IGSXFuelCoordinator
    {
        private readonly IProsimFuelService _prosimFuelService;
        private IGSXServiceOrchestrator _serviceOrchestrator;
        private readonly ILogger _logger;
        private readonly MobiSimConnect _simConnect;
        private IGSXStateManager _stateManager;
        private readonly IEventAggregator _eventAggregator;
        private bool _disposed;
        
        // New components
        private readonly RefuelingStateManager _refuelingStateManager;
        private readonly RefuelingProgressTracker _progressTracker;
        private readonly FuelHoseConnectionMonitor _hoseMonitor;
        private readonly RefuelingCommandFactory _commandFactory;
        
        // State processing tracking to prevent redundant operations
        private readonly HashSet<FlightState> _processedStates = new HashSet<FlightState>();
        
        /// <summary>
        /// Event raised when fuel state changes
        /// </summary>
        public event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;
        
        /// <summary>
        /// Event raised when refueling progress changes
        /// </summary>
        public event EventHandler<RefuelingProgressChangedEventArgs> RefuelingProgressChanged;
        
        /// <summary>
        /// Gets the current refueling state
        /// </summary>
        public RefuelingState RefuelingState => _refuelingStateManager.State;
        
        /// <summary>
        /// Gets the planned fuel amount in kg
        /// </summary>
        public double FuelPlanned => _prosimFuelService.FuelPlanned;
        
        /// <summary>
        /// Gets the current fuel amount in kg
        /// </summary>
        public double FuelCurrent => _prosimFuelService.FuelCurrent;
        
        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        public string FuelUnits => _prosimFuelService.FuelUnits;
        
        /// <summary>
        /// Gets the refueling progress percentage (0-100)
        /// </summary>
        public int RefuelingProgressPercentage => _progressTracker.ProgressPercentage;
        
        /// <summary>
        /// Gets the fuel rate in kg/s
        /// </summary>
        public float FuelRateKGS => _prosimFuelService.GetFuelRateKGS();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelCoordinator"/> class
        /// </summary>
        /// <param name="prosimFuelService">The ProSim fuel service</param>
        /// <param name="serviceOrchestrator">The GSX service orchestrator (can be null and set later)</param>
        /// <param name="simConnect">The SimConnect instance</param>
        /// <param name="logger">The logger</param>
        /// <param name="eventAggregator">The event aggregator for publishing events</param>
        public GSXFuelCoordinator(
            IProsimFuelService prosimFuelService,
            IGSXServiceOrchestrator serviceOrchestrator,
            MobiSimConnect simConnect,
            ILogger logger,
            IEventAggregator eventAggregator = null)
        {
            _prosimFuelService = prosimFuelService ?? throw new ArgumentNullException(nameof(prosimFuelService));
            _serviceOrchestrator = serviceOrchestrator; // Can be null, will be set later
            _simConnect = simConnect ?? throw new ArgumentNullException(nameof(simConnect));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize components
            _refuelingStateManager = new RefuelingStateManager(_logger);
            _progressTracker = new RefuelingProgressTracker(_logger);
            _hoseMonitor = new FuelHoseConnectionMonitor(_simConnect, _logger);
            _commandFactory = new RefuelingCommandFactory(_prosimFuelService, _refuelingStateManager, _logger);
            
            // Wire up events
            _refuelingStateManager.StateChanged += OnRefuelingStateChanged;
            _progressTracker.ProgressChanged += OnProgressChanged;
            _hoseMonitor.ConnectionChanged += OnHoseConnectionChanged;
            _prosimFuelService.FuelStateChanged += OnProsimFuelStateChanged;
        }
        
        /// <summary>
        /// Initializes the fuel coordinator
        /// </summary>
        public void Initialize()
        {
            _logger.Log(LogLevel.Information, "GSXFuelCoordinator:Initialize", "Initializing fuel coordinator");
            
            // Initialize fuel amounts
            double fuelPlanned = _prosimFuelService.GetFuelPlanned();
            double fuelCurrent = _prosimFuelService.GetFuelCurrent();
            string fuelUnits = _prosimFuelService.FuelUnits;
            
            // Update progress tracker
            _progressTracker.UpdateProgress(fuelCurrent, fuelPlanned, fuelUnits);
            
            _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:Initialize", 
                $"Initial fuel state: Planned: {fuelPlanned} {fuelUnits}, Current: {fuelCurrent} {fuelUnits}");
        }
        
        /// <summary>
        /// Starts the refueling process
        /// </summary>
        /// <returns>True if refueling was started successfully, false otherwise</returns>
        public bool StartRefueling()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StartRefueling", "Starting refueling process");
                
                var command = _commandFactory.CreateStartRefuelingCommand();
                return command.ExecuteAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StartRefueling", $"Error starting refueling: {ex.Message}");
                _refuelingStateManager.TransitionTo(RefuelingState.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Stops the refueling process
        /// </summary>
        /// <returns>True if refueling was stopped successfully, false otherwise</returns>
        public bool StopRefueling()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StopRefueling", "Stopping refueling process");
                
                var command = _commandFactory.CreateStopRefuelingCommand();
                return command.ExecuteAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StopRefueling", $"Error stopping refueling: {ex.Message}");
                _refuelingStateManager.TransitionTo(RefuelingState.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Starts the defueling process
        /// </summary>
        /// <returns>True if defueling was started successfully, false otherwise</returns>
        public bool StartDefueling()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StartDefueling", "Starting defueling process");
                
                // Currently, ProSim doesn't have a direct defueling method
                // We'll implement this by setting a lower target fuel amount
                // and using the refueling mechanism
                
                if (_refuelingStateManager.State == RefuelingState.Defueling)
                {
                    _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartDefueling", "Defueling already in progress");
                    return false;
                }
                
                if (_refuelingStateManager.State == RefuelingState.Refueling)
                {
                    _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartDefueling", "Cannot start defueling while refueling is in progress");
                    return false;
                }
                
                // Transition to Defueling state
                if (!_refuelingStateManager.TransitionTo(RefuelingState.Defueling))
                {
                    _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartDefueling", "Invalid state transition");
                    return false;
                }
                
                // Update fuel state
                var command = _commandFactory.CreateUpdateFuelAmountCommand(0); // Target zero fuel for defueling
                return command.ExecuteAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StartDefueling", $"Error starting defueling: {ex.Message}");
                _refuelingStateManager.TransitionTo(RefuelingState.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Stops the defueling process
        /// </summary>
        /// <returns>True if defueling was stopped successfully, false otherwise</returns>
        public bool StopDefueling()
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StopDefueling", "Stopping defueling process");
                
                if (_refuelingStateManager.State != RefuelingState.Defueling)
                {
                    _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StopDefueling", "No defueling in progress");
                    return false;
                }
                
                // Transition to Idle state
                if (!_refuelingStateManager.TransitionTo(RefuelingState.Idle))
                {
                    _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StopDefueling", "Invalid state transition");
                    return false;
                }
                
                // Update progress tracker
                _progressTracker.UpdateProgress(
                    _prosimFuelService.GetFuelCurrent(),
                    _prosimFuelService.GetFuelPlanned(),
                    _prosimFuelService.FuelUnits);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StopDefueling", $"Error stopping defueling: {ex.Message}");
                _refuelingStateManager.TransitionTo(RefuelingState.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Updates the fuel amount
        /// </summary>
        /// <param name="fuelAmount">The new fuel amount in kg</param>
        /// <returns>True if the fuel amount was updated successfully, false otherwise</returns>
        public bool UpdateFuelAmount(double fuelAmount)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:UpdateFuelAmount", $"Updating fuel amount to {fuelAmount} kg");
                
                if (fuelAmount < 0)
                {
                    _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:UpdateFuelAmount", $"Invalid fuel amount: {fuelAmount}");
                    return false;
                }
                
                var command = _commandFactory.CreateUpdateFuelAmountCommand(fuelAmount);
                bool result = command.ExecuteAsync().GetAwaiter().GetResult();
                
                if (result)
                {
                    // Update progress tracker
                    _progressTracker.UpdateProgress(
                        _prosimFuelService.GetFuelCurrent(),
                        _prosimFuelService.GetFuelPlanned(),
                        _prosimFuelService.FuelUnits);
                    
                    // Raise event
                    OnFuelStateChanged("FuelAmountUpdated", _prosimFuelService.GetFuelCurrent(), _prosimFuelService.GetFuelPlanned());
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:UpdateFuelAmount", $"Error updating fuel amount: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the refueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if refueling was started successfully, false otherwise</returns>
        public async Task<bool> StartRefuelingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StartRefuelingAsync", "Starting refueling process asynchronously");
                
                // Check if cancellation is requested
                cancellationToken.ThrowIfCancellationRequested();
                
                // Execute the command
                var command = _commandFactory.CreateStartRefuelingCommand();
                bool result = await command.ExecuteAsync(cancellationToken);
                
                if (result)
                {
                    // Monitor refueling progress asynchronously
                    await MonitorRefuelingProgressAsync(cancellationToken);
                }
                
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartRefuelingAsync", "Operation canceled");
                
                // Stop refueling if it was started
                if (_refuelingStateManager.State == RefuelingState.Refueling)
                {
                    StopRefueling();
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StartRefuelingAsync", $"Error starting refueling asynchronously: {ex.Message}");
                _refuelingStateManager.TransitionTo(RefuelingState.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Stops the refueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if refueling was stopped successfully, false otherwise</returns>
        public async Task<bool> StopRefuelingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StopRefuelingAsync", "Stopping refueling process asynchronously");
                
                return await Task.Run(() => StopRefueling(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StopRefuelingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StopRefuelingAsync", $"Error stopping refueling asynchronously: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the defueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if defueling was started successfully, false otherwise</returns>
        public async Task<bool> StartDefuelingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StartDefuelingAsync", "Starting defueling process asynchronously");
                
                return await Task.Run(() => StartDefueling(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartDefuelingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StartDefuelingAsync", $"Error starting defueling asynchronously: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the defueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if defueling was stopped successfully, false otherwise</returns>
        public async Task<bool> StopDefuelingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:StopDefuelingAsync", "Stopping defueling process asynchronously");
                
                return await Task.Run(() => StopDefueling(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StopDefuelingAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StopDefuelingAsync", $"Error stopping defueling asynchronously: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Updates the fuel amount asynchronously
        /// </summary>
        /// <param name="fuelAmount">The new fuel amount in kg</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the fuel amount was updated successfully, false otherwise</returns>
        public async Task<bool> UpdateFuelAmountAsync(double fuelAmount, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:UpdateFuelAmountAsync", $"Updating fuel amount to {fuelAmount} kg asynchronously");
                
                return await Task.Run(() => UpdateFuelAmount(fuelAmount), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:UpdateFuelAmountAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:UpdateFuelAmountAsync", $"Error updating fuel amount asynchronously: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Synchronizes fuel quantities between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task SynchronizeFuelQuantitiesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:SynchronizeFuelQuantitiesAsync", "Synchronizing fuel quantities");
                
                // Update progress tracker
                _progressTracker.UpdateProgress(
                    _prosimFuelService.GetFuelCurrent(),
                    _prosimFuelService.GetFuelPlanned(),
                    _prosimFuelService.FuelUnits);
                
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:SynchronizeFuelQuantitiesAsync", 
                    $"Synchronized fuel state: Planned: {_prosimFuelService.GetFuelPlanned()} {_prosimFuelService.FuelUnits}, " +
                    $"Current: {_prosimFuelService.GetFuelCurrent()} {_prosimFuelService.FuelUnits}");
                
                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:SynchronizeFuelQuantitiesAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:SynchronizeFuelQuantitiesAsync", $"Error synchronizing fuel quantities: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Calculates the required fuel based on the flight plan
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the required fuel amount in kg</returns>
        public async Task<double> CalculateRequiredFuelAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:CalculateRequiredFuelAsync", "Calculating required fuel");
                
                // For now, we'll just return the planned fuel amount from the fuel service
                // In the future, this could be enhanced to calculate fuel based on flight plan data
                double requiredFuel = _prosimFuelService.GetFuelPlanned();
                
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:CalculateRequiredFuelAsync", 
                    $"Calculated required fuel: {requiredFuel} {_prosimFuelService.FuelUnits}");
                
                return await Task.FromResult(requiredFuel);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:CalculateRequiredFuelAsync", "Operation canceled");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:CalculateRequiredFuelAsync", $"Error calculating required fuel: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Manages fuel based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ManageFuelForStateAsync(FlightState state, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:ManageFuelForStateAsync", $"Managing fuel for state: {state}");
                
                // Skip if we've already processed this state (unless it's DEPARTURE or TURNAROUND which may need repeated processing)
                if (_processedStates.Contains(state) && state != FlightState.DEPARTURE && state != FlightState.TURNAROUND)
                {
                    _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:ManageFuelForStateAsync", 
                        $"State {state} already processed, skipping");
                    return;
                }
                
                switch (state)
                {
                    case FlightState.PREFLIGHT:
                        // In preflight, ensure fuel quantities are synchronized and set initial fuel
                        await SynchronizeFuelQuantitiesAsync(cancellationToken);
                        _prosimFuelService.SetInitialFuel();
                        _processedStates.Add(state); // Mark as processed
                        break;
                        
                    case FlightState.DEPARTURE:
                        // In departure, start refueling if not already in progress
                        if (_refuelingStateManager.State == RefuelingState.Idle)
                        {
                            await StartRefuelingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.TAXIOUT:
                        // In taxiout, stop refueling if in progress
                        if (_refuelingStateManager.State == RefuelingState.Refueling)
                        {
                            await StopRefuelingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.FLIGHT:
                        // In flight, ensure refueling is stopped
                        if (_refuelingStateManager.State == RefuelingState.Refueling)
                        {
                            await StopRefuelingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.TAXIIN:
                        // In taxiin, no fuel operations
                        break;
                        
                    case FlightState.ARRIVAL:
                        // In arrival, no fuel operations
                        break;
                        
                    case FlightState.TURNAROUND:
                        // In turnaround, calculate required fuel for next flight
                        double requiredFuel = await CalculateRequiredFuelAsync(cancellationToken);
                        await UpdateFuelAmountAsync(requiredFuel, cancellationToken);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:ManageFuelForStateAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:ManageFuelForStateAsync", 
                    $"Error managing fuel for state {state}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets the service orchestrator
        /// </summary>
        /// <param name="serviceOrchestrator">The GSX service orchestrator</param>
        public void SetServiceOrchestrator(IGSXServiceOrchestrator serviceOrchestrator)
        {
            _serviceOrchestrator = serviceOrchestrator ?? throw new ArgumentNullException(nameof(serviceOrchestrator));
            _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:SetServiceOrchestrator", "Service orchestrator set");
        }
        
        /// <summary>
        /// Sets the event aggregator for publishing events
        /// </summary>
        /// <param name="eventAggregator">The event aggregator</param>
        public void SetEventAggregator(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:SetEventAggregator", "Event aggregator set");
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
        /// Monitors the refueling progress asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task MonitorRefuelingProgressAsync(CancellationToken cancellationToken)
        {
            try
            {
                bool fuelLoadingComplete = false;
                
                while ((_refuelingStateManager.State == RefuelingState.Requested || 
                       _refuelingStateManager.State == RefuelingState.Refueling) && 
                       !cancellationToken.IsCancellationRequested)
                {
                    // Check fuel hose connection
                    _hoseMonitor.CheckConnection();
                    
                    // Only pump fuel when hose is connected and we're in Refueling state
                    if (_refuelingStateManager.State == RefuelingState.Refueling && _hoseMonitor.IsConnected)
                    {
                        // Update current fuel amount
                        double currentFuel = _prosimFuelService.GetFuelAmount();
                        double plannedFuel = _prosimFuelService.GetFuelPlanned();
                        
                        // Update progress tracker
                        _progressTracker.UpdateProgress(currentFuel, plannedFuel, _prosimFuelService.FuelUnits);
                        
                        // Perform refueling at the configured rate
                        bool isComplete = _prosimFuelService.Refuel();
                        if (isComplete)
                        {
                            _logger.Log(LogLevel.Information, "GSXFuelCoordinator:MonitorRefuelingProgressAsync", 
                                "Fuel loading complete - waiting for hose disconnection");
                            
                            _refuelingStateManager.TransitionTo(RefuelingState.Complete);
                            fuelLoadingComplete = true;
                            
                            // Don't break the loop yet - wait for the hose to be disconnected
                        }
                    }
                    
                    // If fuel loading is complete but hose is still connected, wait for disconnection
                    if (fuelLoadingComplete && !_hoseMonitor.IsConnected)
                    {
                        _logger.Log(LogLevel.Information, "GSXFuelCoordinator:MonitorRefuelingProgressAsync", 
                            "Fuel loading complete and hose disconnected - stopping fuel transfer");
                        _prosimFuelService.RefuelStop();
                        break;
                    }
                    
                    // Wait before checking again
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:MonitorRefuelingProgressAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:MonitorRefuelingProgressAsync", 
                    $"Error monitoring refueling progress: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles fuel state changes from ProSim
        /// </summary>
        private void OnProsimFuelStateChanged(object sender, FuelStateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:OnProsimFuelStateChanged", 
                    $"Fuel state changed: {e.OperationType}, Current: {e.CurrentAmount} {e.FuelUnits}, Planned: {e.PlannedAmount} {e.FuelUnits}");
                
                // Update progress tracker
                _progressTracker.UpdateProgress(e.CurrentAmount, e.PlannedAmount, e.FuelUnits);
                
                // Forward the event
                OnFuelStateChanged(e.OperationType, e.CurrentAmount, e.PlannedAmount);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:OnProsimFuelStateChanged", 
                    $"Error handling fuel state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles refueling state changes
        /// </summary>
        private void OnRefuelingStateChanged(object sender, RefuelingStateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:OnRefuelingStateChanged", 
                    $"Refueling state changed from {e.PreviousState} to {e.NewState}");
                
                // Handle state changes
                if (e.NewState == RefuelingState.Requested && e.PreviousState == RefuelingState.Idle)
                {
                    OnFuelStateChanged("RefuelingStarted", _prosimFuelService.GetFuelCurrent(), _prosimFuelService.GetFuelPlanned());
                }
                else if (e.NewState == RefuelingState.Idle && e.PreviousState == RefuelingState.Refueling)
                {
                    OnFuelStateChanged("RefuelingStopped", _prosimFuelService.GetFuelCurrent(), _prosimFuelService.GetFuelPlanned());
                }
                else if (e.NewState == RefuelingState.Complete)
                {
                    OnFuelStateChanged("RefuelingComplete", _prosimFuelService.GetFuelCurrent(), _prosimFuelService.GetFuelPlanned());
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:OnRefuelingStateChanged", 
                    $"Error handling refueling state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles progress changes
        /// </summary>
        private void OnProgressChanged(object sender, RefuelingProgressChangedEventArgs e)
        {
            try
            {
                // Use EventAggregator if available
                if (_eventAggregator != null)
                {
                    _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:OnProgressChanged", 
                        $"Publishing refueling progress event via EventAggregator: {e.ProgressPercentage}%");
                    _eventAggregator.Publish(e);
                }
                
                // Also raise the event directly for backward compatibility
                RefuelingProgressChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:OnProgressChanged", 
                    $"Error handling progress change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles fuel hose connection changes
        /// </summary>
        private void OnHoseConnectionChanged(object sender, bool isConnected)
        {
            try
            {
                _logger.Log(LogLevel.Information, "GSXFuelCoordinator:OnHoseConnectionChanged", 
                    $"Fuel hose {(isConnected ? "connected" : "disconnected")}");
                
                if (isConnected && _refuelingStateManager.State == RefuelingState.Requested)
                {
                    // Now that the fuel hose is connected, start the actual fuel transfer
                    _prosimFuelService.StartFuelTransfer();
                    _refuelingStateManager.TransitionTo(RefuelingState.Refueling);
                    
                    OnFuelStateChanged("FuelHoseConnected", _prosimFuelService.GetFuelCurrent(), _prosimFuelService.GetFuelPlanned());
                }
                else if (!isConnected && _refuelingStateManager.State == RefuelingState.Refueling)
                {
                    OnFuelStateChanged("FuelHoseDisconnected", _prosimFuelService.GetFuelCurrent(), _prosimFuelService.GetFuelPlanned());
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:OnHoseConnectionChanged", 
                    $"Error handling hose connection change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles state changes from the state manager
        /// </summary>
        private async void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:OnStateChanged", 
                    $"State changed from {e.PreviousState} to {e.NewState}");
                
                // Clear processed states when transitioning to a new state
                if (e.PreviousState != e.NewState)
                {
                    _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:OnStateChanged", 
                        "Clearing processed states due to state transition");
                    _processedStates.Clear();
                }
                
                // Manage fuel based on the new state
                await ManageFuelForStateAsync(e.NewState);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:OnStateChanged", 
                    $"Error handling state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Raises the FuelStateChanged event
        /// </summary>
        protected virtual void OnFuelStateChanged(string operationType, double currentAmount, double plannedAmount)
        {
            try
            {
                var args = new FuelStateChangedEventArgs(operationType, currentAmount, plannedAmount, _prosimFuelService.FuelUnits);
                
                // Use EventAggregator if available
                if (_eventAggregator != null)
                {
                    _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:OnFuelStateChanged", 
                        $"Publishing fuel state change event via EventAggregator: {operationType}");
                    _eventAggregator.Publish(args);
                }
                
                // Also raise the event directly for backward compatibility
                var handler = FuelStateChanged;
                if (handler != null)
                {
                    handler(this, args);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:OnFuelStateChanged", $"Error raising FuelStateChanged event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disposes resources used by the fuel coordinator
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources used by the fuel coordinator
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                // Unsubscribe from events
                if (_prosimFuelService != null)
                    _prosimFuelService.FuelStateChanged -= OnProsimFuelStateChanged;
                
                if (_stateManager != null)
                    _stateManager.StateChanged -= OnStateChanged;
                
                if (_refuelingStateManager != null)
                    _refuelingStateManager.StateChanged -= OnRefuelingStateChanged;
                
                if (_progressTracker != null)
                    _progressTracker.ProgressChanged -= OnProgressChanged;
                
                if (_hoseMonitor != null)
                    _hoseMonitor.ConnectionChanged -= OnHoseConnectionChanged;
            }
            
            _disposed = true;
        }
    }
}
