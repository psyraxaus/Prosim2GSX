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
        private readonly IGSXServiceOrchestrator _serviceOrchestrator;
        private readonly ILogger _logger;
        private readonly MobiSimConnect _simConnect;
        private IGSXStateManager _stateManager;
        private bool _disposed;
        
        // Fuel state tracking
        private RefuelingState _refuelingState;
        private double _fuelPlanned;
        private double _fuelCurrent;
        private string _fuelUnits;
        private int _refuelingProgressPercentage;
        private readonly object _stateLock = new object();
        
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
        public RefuelingState RefuelingState => _refuelingState;
        
        /// <summary>
        /// Gets the planned fuel amount in kg
        /// </summary>
        public double FuelPlanned => _fuelPlanned;
        
        /// <summary>
        /// Gets the current fuel amount in kg
        /// </summary>
        public double FuelCurrent => _fuelCurrent;
        
        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        public string FuelUnits => _fuelUnits;
        
        /// <summary>
        /// Gets the refueling progress percentage (0-100)
        /// </summary>
        public int RefuelingProgressPercentage => _refuelingProgressPercentage;
        
        /// <summary>
        /// Gets the fuel rate in kg/s
        /// </summary>
        public float FuelRateKGS => _prosimFuelService.GetFuelRateKGS();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelCoordinator"/> class
        /// </summary>
        /// <param name="prosimFuelService">The ProSim fuel service</param>
        /// <param name="serviceOrchestrator">The GSX service orchestrator</param>
        /// <param name="simConnect">The SimConnect instance</param>
        /// <param name="logger">The logger</param>
        public GSXFuelCoordinator(
            IProsimFuelService prosimFuelService,
            IGSXServiceOrchestrator serviceOrchestrator,
            MobiSimConnect simConnect,
            ILogger logger)
        {
            _prosimFuelService = prosimFuelService ?? throw new ArgumentNullException(nameof(prosimFuelService));
            _serviceOrchestrator = serviceOrchestrator ?? throw new ArgumentNullException(nameof(serviceOrchestrator));
            _simConnect = simConnect ?? throw new ArgumentNullException(nameof(simConnect));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _refuelingState = RefuelingState.Idle;
            _fuelPlanned = 0;
            _fuelCurrent = 0;
            _fuelUnits = "KG";
            _refuelingProgressPercentage = 0;
            
            // Subscribe to fuel state change events
            _prosimFuelService.FuelStateChanged += OnProsimFuelStateChanged;
        }
        
        /// <summary>
        /// Initializes the fuel coordinator
        /// </summary>
        public void Initialize()
        {
            _logger.Log(LogLevel.Information, "GSXFuelCoordinator:Initialize", "Initializing fuel coordinator");
            
            // Initialize fuel amounts
            _fuelPlanned = _prosimFuelService.GetFuelPlanned();
            _fuelCurrent = _prosimFuelService.GetFuelCurrent();
            _fuelUnits = _prosimFuelService.FuelUnits;
            
            _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:Initialize", 
                $"Initial fuel state: Planned: {_fuelPlanned} {_fuelUnits}, Current: {_fuelCurrent} {_fuelUnits}");
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
                
                lock (_stateLock)
                {
                    if (_refuelingState == RefuelingState.Refueling)
                    {
                        _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartRefueling", "Refueling already in progress");
                        return false;
                    }
                    
                    if (_refuelingState == RefuelingState.Defueling)
                    {
                        _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartRefueling", "Cannot start refueling while defueling is in progress");
                        return false;
                    }
                    
                    _refuelingState = RefuelingState.Requested;
                }
                
                // Prepare refueling in ProSim (but don't start fuel transfer yet)
                // This only sets up the target fuel amount but doesn't start pumping fuel
                _prosimFuelService.PrepareRefueling();
                
                // Update fuel state
                _fuelPlanned = _prosimFuelService.GetFuelPlanned();
                _fuelCurrent = _prosimFuelService.GetFuelCurrent();
                _fuelUnits = _prosimFuelService.FuelUnits;
                
                _logger.Log(LogLevel.Information, "GSXFuelCoordinator:StartRefueling", 
                    $"Prepared for refueling. Target: {_fuelPlanned} {_fuelUnits}, Current: {_fuelCurrent} {_fuelUnits}");
                
                // Raise event
                OnFuelStateChanged("RefuelingStarted", _fuelCurrent, _fuelPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StartRefueling", $"Error starting refueling: {ex.Message}");
                
                lock (_stateLock)
                {
                    _refuelingState = RefuelingState.Error;
                }
                
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
                
                lock (_stateLock)
                {
                    if (_refuelingState != RefuelingState.Refueling)
                    {
                        _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StopRefueling", "No refueling in progress");
                        return false;
                    }
                    
                    _refuelingState = RefuelingState.Idle;
                }
                
                // Stop refueling in ProSim
                _prosimFuelService.RefuelStop();
                
                // Update fuel state
                _fuelPlanned = _prosimFuelService.GetFuelPlanned();
                _fuelCurrent = _prosimFuelService.GetFuelCurrent();
                
                // Raise event
                OnFuelStateChanged("RefuelingStopped", _fuelCurrent, _fuelPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StopRefueling", $"Error stopping refueling: {ex.Message}");
                
                lock (_stateLock)
                {
                    _refuelingState = RefuelingState.Error;
                }
                
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
                
                lock (_stateLock)
                {
                    if (_refuelingState == RefuelingState.Defueling)
                    {
                        _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartDefueling", "Defueling already in progress");
                        return false;
                    }
                    
                    if (_refuelingState == RefuelingState.Refueling)
                    {
                        _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartDefueling", "Cannot start defueling while refueling is in progress");
                        return false;
                    }
                    
                    _refuelingState = RefuelingState.Defueling;
                }
                
                // Currently, ProSim doesn't have a direct defueling method
                // We'll implement this by setting a lower target fuel amount
                // and using the refueling mechanism
                
                // Update fuel state
                _fuelPlanned = 0; // Target zero fuel for defueling
                _fuelCurrent = _prosimFuelService.GetFuelCurrent();
                
                // Raise event
                OnFuelStateChanged("DefuelingStarted", _fuelCurrent, _fuelPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StartDefueling", $"Error starting defueling: {ex.Message}");
                
                lock (_stateLock)
                {
                    _refuelingState = RefuelingState.Error;
                }
                
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
                
                lock (_stateLock)
                {
                    if (_refuelingState != RefuelingState.Defueling)
                    {
                        _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StopDefueling", "No defueling in progress");
                        return false;
                    }
                    
                    _refuelingState = RefuelingState.Idle;
                }
                
                // Update fuel state
                _fuelCurrent = _prosimFuelService.GetFuelCurrent();
                
                // Raise event
                OnFuelStateChanged("DefuelingStopped", _fuelCurrent, _fuelPlanned);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StopDefueling", $"Error stopping defueling: {ex.Message}");
                
                lock (_stateLock)
                {
                    _refuelingState = RefuelingState.Error;
                }
                
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
                
                // Always update the planned fuel amount
                _prosimFuelService.UpdatePlannedFuel(fuelAmount);
                
                // Only set the current fuel amount if we're not in a refueling state
                bool isRefueling = _refuelingState == RefuelingState.Requested || 
                                   _refuelingState == RefuelingState.Refueling;
                
                if (!isRefueling)
                {
                    _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:UpdateFuelAmount", 
                        "Not in refueling state, setting current fuel to match planned");
                    _prosimFuelService.SetCurrentFuel(fuelAmount);
                }
                else
                {
                    _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:UpdateFuelAmount", 
                        "In refueling state, not updating current fuel amount");
                }
                
                // Update fuel state
                _fuelPlanned = _prosimFuelService.GetFuelPlanned();
                _fuelCurrent = _prosimFuelService.GetFuelCurrent();
                
                // Raise event
                OnFuelStateChanged("FuelAmountUpdated", _fuelCurrent, _fuelPlanned);
                
                return true;
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
                
                // Use the synchronous method for the actual operation
                bool result = StartRefueling();
                
                if (result)
                {
                    // Monitor refueling progress asynchronously
                    await Task.Run(async () => {
                        bool fuelHoseConnected = false;
                        bool fuelTransferStarted = false;
                        bool fuelLoadingComplete = false;
                        
                        while ((_refuelingState == RefuelingState.Requested || _refuelingState == RefuelingState.Refueling) && 
                               !cancellationToken.IsCancellationRequested)
                        {
                            // Check if fuel hose is connected
                            bool isHoseConnected = _simConnect.ReadLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1;
                            
                            if (isHoseConnected && !fuelHoseConnected)
                            {
                                _logger.Log(LogLevel.Information, "GSXFuelCoordinator:StartRefuelingAsync", 
                                    "Fuel hose connected - starting fuel transfer");
                                
                                // Now that the fuel hose is connected, start the actual fuel transfer
                                _prosimFuelService.StartFuelTransfer();
                                
                                lock (_stateLock)
                                {
                                    _refuelingState = RefuelingState.Refueling;
                                }
                                
                                OnFuelStateChanged("FuelHoseConnected", _fuelCurrent, _fuelPlanned);
                                fuelHoseConnected = true;
                                fuelTransferStarted = true;
                            }
                            else if (!isHoseConnected && fuelHoseConnected)
                            {
                                _logger.Log(LogLevel.Information, "GSXFuelCoordinator:StartRefuelingAsync", 
                                    "Fuel hose disconnected");
                                
                                // Only stop the fuel transfer if the fuel loading is complete
                                if (fuelLoadingComplete)
                                {
                                    _logger.Log(LogLevel.Information, "GSXFuelCoordinator:StartRefuelingAsync", 
                                        "Fuel loading complete and hose disconnected - stopping fuel transfer");
                                    _prosimFuelService.RefuelStop();
                                }
                                else
                                {
                                    _logger.Log(LogLevel.Information, "GSXFuelCoordinator:StartRefuelingAsync", 
                                        "Fuel loading not complete - keeping refueling power active");
                                }
                                
                                fuelHoseConnected = false;
                                fuelTransferStarted = false;
                                OnFuelStateChanged("FuelHoseDisconnected", _fuelCurrent, _fuelPlanned);
                            }
                            
                            // Only pump fuel when hose is connected and we're in Refueling state
                            // and fuel transfer has been started
                            if (_refuelingState == RefuelingState.Refueling && fuelHoseConnected && fuelTransferStarted)
                            {
                                // Update current fuel amount
                                _fuelCurrent = _prosimFuelService.GetFuelAmount();
                                
                                // Calculate progress percentage
                                if (_fuelPlanned > 0)
                                {
                                    _refuelingProgressPercentage = (int)((_fuelCurrent / _fuelPlanned) * 100);
                                    _refuelingProgressPercentage = Math.Min(100, _refuelingProgressPercentage);
                                }
                                
                                // Raise progress event
                                OnRefuelingProgressChanged(_refuelingProgressPercentage, _fuelCurrent, _fuelPlanned);
                                
                                // Perform refueling at the configured rate
                                bool isComplete = _prosimFuelService.Refuel();
                                if (isComplete)
                                {
                                    _logger.Log(LogLevel.Information, "GSXFuelCoordinator:StartRefuelingAsync", 
                                        "Fuel loading complete - waiting for hose disconnection");
                                    
                                    lock (_stateLock)
                                    {
                                        _refuelingState = RefuelingState.Complete;
                                        _refuelingProgressPercentage = 100;
                                    }
                                    
                                    fuelLoadingComplete = true;
                                    OnFuelStateChanged("RefuelingComplete", _fuelCurrent, _fuelPlanned);
                                    
                                    // Don't break the loop yet - wait for the hose to be disconnected
                                    // before stopping the refueling power
                                }
                            }
                            
                            // If fuel loading is complete but hose is still connected, wait for disconnection
                            if (fuelLoadingComplete && !isHoseConnected)
                            {
                                _logger.Log(LogLevel.Information, "GSXFuelCoordinator:StartRefuelingAsync", 
                                    "Fuel loading complete and hose disconnected - stopping fuel transfer");
                                _prosimFuelService.RefuelStop();
                                break;
                            }
                            
                            // Wait before checking again
                            await Task.Delay(1000, cancellationToken);
                        }
                    }, cancellationToken);
                }
                
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXFuelCoordinator:StartRefuelingAsync", "Operation canceled");
                
                // Stop refueling if it was started
                if (_refuelingState == RefuelingState.Refueling)
                {
                    StopRefueling();
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:StartRefuelingAsync", $"Error starting refueling asynchronously: {ex.Message}");
                
                lock (_stateLock)
                {
                    _refuelingState = RefuelingState.Error;
                }
                
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
                
                // Update fuel state
                _fuelPlanned = _prosimFuelService.GetFuelPlanned();
                _fuelCurrent = _prosimFuelService.GetFuelCurrent();
                _fuelUnits = _prosimFuelService.FuelUnits;
                
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:SynchronizeFuelQuantitiesAsync", 
                    $"Synchronized fuel state: Planned: {_fuelPlanned} {_fuelUnits}, Current: {_fuelCurrent} {_fuelUnits}");
                
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
                    $"Calculated required fuel: {requiredFuel} {_fuelUnits}");
                
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
                        if (_refuelingState == RefuelingState.Idle)
                        {
                            await StartRefuelingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.TAXIOUT:
                        // In taxiout, stop refueling if in progress
                        if (_refuelingState == RefuelingState.Refueling)
                        {
                            await StopRefuelingAsync(cancellationToken);
                        }
                        break;
                        
                    case FlightState.FLIGHT:
                        // In flight, ensure refueling is stopped
                        if (_refuelingState == RefuelingState.Refueling)
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
        /// Handles fuel state changes from ProSim
        /// </summary>
        private void OnProsimFuelStateChanged(object sender, FuelStateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXFuelCoordinator:OnProsimFuelStateChanged", 
                    $"Fuel state changed: {e.OperationType}, Current: {e.CurrentAmount} {e.FuelUnits}, Planned: {e.PlannedAmount} {e.FuelUnits}");
                
                // Update our internal state
                _fuelPlanned = e.PlannedAmount;
                _fuelCurrent = e.CurrentAmount;
                _fuelUnits = e.FuelUnits;
                
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
                var handler = FuelStateChanged;
                if (handler != null)
                {
                    var args = new FuelStateChangedEventArgs(operationType, currentAmount, plannedAmount, _fuelUnits);
                    handler(this, args);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:OnFuelStateChanged", $"Error raising FuelStateChanged event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Raises the RefuelingProgressChanged event
        /// </summary>
        protected virtual void OnRefuelingProgressChanged(int progressPercentage, double currentAmount, double targetAmount)
        {
            try
            {
                var handler = RefuelingProgressChanged;
                if (handler != null)
                {
                    var args = new RefuelingProgressChangedEventArgs(progressPercentage, currentAmount, targetAmount, _fuelUnits);
                    handler(this, args);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXFuelCoordinator:OnRefuelingProgressChanged", $"Error raising RefuelingProgressChanged event: {ex.Message}");
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
            }
            
            _disposed = true;
        }
    }
}
