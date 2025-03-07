using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates ground equipment operations between GSX and ProSim
    /// </summary>
    public class GSXEquipmentCoordinator : IGSXEquipmentCoordinator
    {
        private readonly IProsimEquipmentService _prosimEquipmentService;
        private readonly ILogger _logger;
        private IGSXStateManager _stateManager;
        private bool _disposed;
        
        // Equipment state tracking
        private bool _isGpuConnected;
        private bool _isPcaConnected;
        private bool _areChocksPlaced;
        private bool _isJetwayConnected;
        
        /// <summary>
        /// Event raised when equipment state changes
        /// </summary>
        public event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
        
        /// <summary>
        /// Gets a value indicating whether the GPU is connected
        /// </summary>
        public bool IsGpuConnected => _isGpuConnected;
        
        /// <summary>
        /// Gets a value indicating whether the PCA is connected
        /// </summary>
        public bool IsPcaConnected => _isPcaConnected;
        
        /// <summary>
        /// Gets a value indicating whether the chocks are placed
        /// </summary>
        public bool AreChocksPlaced => _areChocksPlaced;
        
        /// <summary>
        /// Gets a value indicating whether the jetway is connected
        /// </summary>
        public bool IsJetwayConnected => _isJetwayConnected;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GSXEquipmentCoordinator"/> class
        /// </summary>
        /// <param name="prosimEquipmentService">The ProSim equipment service</param>
        /// <param name="logger">The logger</param>
        public GSXEquipmentCoordinator(IProsimEquipmentService prosimEquipmentService, ILogger logger)
        {
            _prosimEquipmentService = prosimEquipmentService ?? throw new ArgumentNullException(nameof(prosimEquipmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Subscribe to equipment state change events
            _prosimEquipmentService.EquipmentStateChanged += OnProsimEquipmentStateChanged;
        }
        
        /// <summary>
        /// Initializes the equipment coordinator
        /// </summary>
        public void Initialize()
        {
            _logger.Log(LogLevel.Information, "GSXEquipmentCoordinator:Initialize", "Initializing equipment coordinator");
        }
        
        /// <summary>
        /// Connects the specified equipment
        /// </summary>
        /// <param name="equipmentType">The type of equipment to connect</param>
        /// <returns>True if the equipment was connected successfully, false otherwise</returns>
        public bool ConnectEquipment(EquipmentType equipmentType)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:ConnectEquipment", $"Connecting equipment: {equipmentType}");
                
                switch (equipmentType)
                {
                    case EquipmentType.GPU:
                        _prosimEquipmentService.SetServiceGPU(true);
                        UpdateEquipmentState(EquipmentType.GPU, true);
                        return true;
                        
                    case EquipmentType.PCA:
                        _prosimEquipmentService.SetServicePCA(true);
                        UpdateEquipmentState(EquipmentType.PCA, true);
                        return true;
                        
                    case EquipmentType.Chocks:
                        _prosimEquipmentService.SetServiceChocks(true);
                        UpdateEquipmentState(EquipmentType.Chocks, true);
                        return true;
                        
                    case EquipmentType.Jetway:
                        // Jetway is handled differently as it's not directly controlled by ProsimEquipmentService
                        UpdateEquipmentState(EquipmentType.Jetway, true);
                        return true;
                        
                    default:
                        _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:ConnectEquipment", $"Unknown equipment type: {equipmentType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:ConnectEquipment", $"Error connecting equipment {equipmentType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Disconnects the specified equipment
        /// </summary>
        /// <param name="equipmentType">The type of equipment to disconnect</param>
        /// <returns>True if the equipment was disconnected successfully, false otherwise</returns>
        public bool DisconnectEquipment(EquipmentType equipmentType)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:DisconnectEquipment", $"Disconnecting equipment: {equipmentType}");
                
                switch (equipmentType)
                {
                    case EquipmentType.GPU:
                        _prosimEquipmentService.SetServiceGPU(false);
                        UpdateEquipmentState(EquipmentType.GPU, false);
                        return true;
                        
                    case EquipmentType.PCA:
                        _prosimEquipmentService.SetServicePCA(false);
                        UpdateEquipmentState(EquipmentType.PCA, false);
                        return true;
                        
                    case EquipmentType.Chocks:
                        _prosimEquipmentService.SetServiceChocks(false);
                        UpdateEquipmentState(EquipmentType.Chocks, false);
                        return true;
                        
                    case EquipmentType.Jetway:
                        // Jetway is handled differently as it's not directly controlled by ProsimEquipmentService
                        UpdateEquipmentState(EquipmentType.Jetway, false);
                        return true;
                        
                    default:
                        _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:DisconnectEquipment", $"Unknown equipment type: {equipmentType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:DisconnectEquipment", $"Error disconnecting equipment {equipmentType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Connects the specified equipment asynchronously
        /// </summary>
        /// <param name="equipmentType">The type of equipment to connect</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the equipment was connected successfully, false otherwise</returns>
        public async Task<bool> ConnectEquipmentAsync(EquipmentType equipmentType, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:ConnectEquipmentAsync", $"Connecting equipment asynchronously: {equipmentType}");
                
                return await Task.Run(() => ConnectEquipment(equipmentType), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:ConnectEquipmentAsync", $"Operation canceled for equipment {equipmentType}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:ConnectEquipmentAsync", $"Error connecting equipment {equipmentType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Disconnects the specified equipment asynchronously
        /// </summary>
        /// <param name="equipmentType">The type of equipment to disconnect</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the equipment was disconnected successfully, false otherwise</returns>
        public async Task<bool> DisconnectEquipmentAsync(EquipmentType equipmentType, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:DisconnectEquipmentAsync", $"Disconnecting equipment asynchronously: {equipmentType}");
                
                return await Task.Run(() => DisconnectEquipment(equipmentType), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:DisconnectEquipmentAsync", $"Operation canceled for equipment {equipmentType}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:DisconnectEquipmentAsync", $"Error disconnecting equipment {equipmentType}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Synchronizes equipment states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task SynchronizeEquipmentStatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:SynchronizeEquipmentStatesAsync", "Synchronizing equipment states");
                
                // No direct way to query equipment states from ProSim, so we'll just update based on our tracked states
                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:SynchronizeEquipmentStatesAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:SynchronizeEquipmentStatesAsync", $"Error synchronizing equipment states: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Manages equipment based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ManageEquipmentForStateAsync(FlightState state, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:ManageEquipmentForStateAsync", $"Managing equipment for state: {state}");
                
                switch (state)
                {
                    case FlightState.PREFLIGHT:
                        // In preflight, connect all ground equipment
                        await ConnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.DEPARTURE:
                        // In departure, keep ground equipment connected
                        await ConnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.TAXIOUT:
                        // In taxiout, disconnect all ground equipment
                        await DisconnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.FLIGHT:
                        // In flight, ensure all ground equipment is disconnected
                        await DisconnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.TAXIIN:
                        // In taxiin, ensure all ground equipment is disconnected
                        await DisconnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await DisconnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.ARRIVAL:
                        // In arrival, connect all ground equipment
                        await ConnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                        
                    case FlightState.TURNAROUND:
                        // In turnaround, keep ground equipment connected
                        await ConnectEquipmentAsync(EquipmentType.GPU, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.PCA, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Chocks, cancellationToken);
                        await ConnectEquipmentAsync(EquipmentType.Jetway, cancellationToken);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "GSXEquipmentCoordinator:ManageEquipmentForStateAsync", "Operation canceled");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:ManageEquipmentForStateAsync", $"Error managing equipment for state {state}: {ex.Message}");
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
        /// Updates the equipment state and raises the EquipmentStateChanged event
        /// </summary>
        /// <param name="equipmentType">The type of equipment</param>
        /// <param name="isConnected">A value indicating whether the equipment is connected</param>
        private void UpdateEquipmentState(EquipmentType equipmentType, bool isConnected)
        {
            bool stateChanged = false;
            
            switch (equipmentType)
            {
                case EquipmentType.GPU:
                    stateChanged = _isGpuConnected != isConnected;
                    _isGpuConnected = isConnected;
                    break;
                    
                case EquipmentType.PCA:
                    stateChanged = _isPcaConnected != isConnected;
                    _isPcaConnected = isConnected;
                    break;
                    
                case EquipmentType.Chocks:
                    stateChanged = _areChocksPlaced != isConnected;
                    _areChocksPlaced = isConnected;
                    break;
                    
                case EquipmentType.Jetway:
                    stateChanged = _isJetwayConnected != isConnected;
                    _isJetwayConnected = isConnected;
                    break;
            }
            
            if (stateChanged)
            {
                var args = new EquipmentStateChangedEventArgs(equipmentType, isConnected);
                OnEquipmentStateChanged(args);
            }
        }
        
        /// <summary>
        /// Handles equipment state changes from ProSim
        /// </summary>
        private void OnProsimEquipmentStateChanged(object sender, EquipmentStateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:OnProsimEquipmentStateChanged", 
                    $"Equipment {e.EquipmentType} state changed to {(e.IsConnected ? "connected" : "disconnected")}");
                
                // Update our internal state
                UpdateEquipmentState(e.EquipmentType, e.IsConnected);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:OnProsimEquipmentStateChanged", 
                    $"Error handling equipment state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles state changes from the state manager
        /// </summary>
        private async void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "GSXEquipmentCoordinator:OnStateChanged", 
                    $"State changed from {e.PreviousState} to {e.NewState}");
                
                // Manage equipment based on the new state
                await ManageEquipmentForStateAsync(e.NewState);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "GSXEquipmentCoordinator:OnStateChanged", 
                    $"Error handling state change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Raises the EquipmentStateChanged event
        /// </summary>
        protected virtual void OnEquipmentStateChanged(EquipmentStateChangedEventArgs e)
        {
            EquipmentStateChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// Disposes resources used by the equipment coordinator
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources used by the equipment coordinator
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                // Unsubscribe from events
                _prosimEquipmentService.EquipmentStateChanged -= OnProsimEquipmentStateChanged;
                
                if (_stateManager != null)
                    _stateManager.StateChanged -= OnStateChanged;
            }
            
            _disposed = true;
        }
    }
}
