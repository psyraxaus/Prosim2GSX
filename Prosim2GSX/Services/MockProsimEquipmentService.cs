using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Mock implementation of IProsimEquipmentService for use when the real service is not available
    /// </summary>
    public class MockProsimEquipmentService : IProsimEquipmentService
    {
        private readonly ILogger _logger;
        private bool _jetwayConnected = false;
        private bool _stairsConnected = false;
        private bool _gpuConnected = false;
        private bool _pcaConnected = false;
        private bool _chocksPlaced = false;

        /// <summary>
        /// Event that fires when equipment state changes
        /// </summary>
        public event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockProsimEquipmentService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public MockProsimEquipmentService(ILogger logger)
        {
            _logger = logger;
            _logger?.Log(LogLevel.Warning, "MockProsimEquipmentService:Constructor", 
                "Using mock equipment service. Equipment operations will not affect the actual aircraft.");
        }

        /// <summary>
        /// Gets a value indicating whether the jetway is connected
        /// </summary>
        /// <returns>True if the jetway is connected, false otherwise</returns>
        public bool IsJetwayConnected()
        {
            return _jetwayConnected;
        }

        /// <summary>
        /// Gets a value indicating whether the stairs are connected
        /// </summary>
        /// <returns>True if the stairs are connected, false otherwise</returns>
        public bool IsStairsConnected()
        {
            return _stairsConnected;
        }

        /// <summary>
        /// Gets a value indicating whether the GPU is connected
        /// </summary>
        /// <returns>True if the GPU is connected, false otherwise</returns>
        public bool IsGpuConnected()
        {
            return _gpuConnected;
        }

        /// <summary>
        /// Gets a value indicating whether the PCA is connected
        /// </summary>
        /// <returns>True if the PCA is connected, false otherwise</returns>
        public bool IsPcaConnected()
        {
            return _pcaConnected;
        }

        /// <summary>
        /// Gets a value indicating whether the chocks are placed
        /// </summary>
        /// <returns>True if the chocks are placed, false otherwise</returns>
        public bool AreChocksPlaced()
        {
            return _chocksPlaced;
        }

        /// <summary>
        /// Sets the state of the jetway
        /// </summary>
        /// <param name="connected">True to connect the jetway, false to disconnect it</param>
        public void SetJetway(bool connected)
        {
            if (_jetwayConnected != connected)
            {
                _jetwayConnected = connected;
                _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:SetJetway", 
                    $"Jetway set to {(connected ? "connected" : "disconnected")} (mock)");
                OnEquipmentStateChanged(EquipmentType.Jetway, connected);
            }
        }

        /// <summary>
        /// Sets the state of the stairs
        /// </summary>
        /// <param name="connected">True to connect the stairs, false to disconnect them</param>
        public void SetStairs(bool connected)
        {
            if (_stairsConnected != connected)
            {
                _stairsConnected = connected;
                _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:SetStairs", 
                    $"Stairs set to {(connected ? "connected" : "disconnected")} (mock)");
                OnEquipmentStateChanged(EquipmentType.Stairs, connected);
            }
        }

        /// <summary>
        /// Sets the state of the GPU
        /// </summary>
        /// <param name="connected">True to connect the GPU, false to disconnect it</param>
        public void SetGpu(bool connected)
        {
            if (_gpuConnected != connected)
            {
                _gpuConnected = connected;
                _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:SetGpu", 
                    $"GPU set to {(connected ? "connected" : "disconnected")} (mock)");
                OnEquipmentStateChanged(EquipmentType.GPU, connected);
            }
        }

        /// <summary>
        /// Sets the state of the Preconditioned Air (PCA) service
        /// </summary>
        /// <param name="enable">True to enable PCA, false to disable</param>
        public void SetServicePCA(bool enable)
        {
            if (_pcaConnected != enable)
            {
                _pcaConnected = enable;
                _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:SetServicePCA", 
                    $"PCA set to {(enable ? "connected" : "disconnected")} (mock)");
                OnEquipmentStateChanged(EquipmentType.PCA, enable);
            }
        }

        /// <summary>
        /// Sets the state of the wheel chocks
        /// </summary>
        /// <param name="enable">True to place chocks, false to remove</param>
        public void SetServiceChocks(bool enable)
        {
            if (_chocksPlaced != enable)
            {
                _chocksPlaced = enable;
                _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:SetServiceChocks", 
                    $"Chocks set to {(enable ? "placed" : "removed")} (mock)");
                OnEquipmentStateChanged(EquipmentType.Chocks, enable);
            }
        }

        /// <summary>
        /// Sets the state of the Ground Power Unit (GPU)
        /// </summary>
        /// <param name="enable">True to connect GPU, false to disconnect</param>
        public void SetServiceGPU(bool enable)
        {
            if (_gpuConnected != enable)
            {
                _gpuConnected = enable;
                _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:SetServiceGPU", 
                    $"GPU set to {(enable ? "connected" : "disconnected")} (mock)");
                OnEquipmentStateChanged(EquipmentType.GPU, enable);
            }
        }

        /// <summary>
        /// Toggles the jetway connection state
        /// </summary>
        public void ToggleJetway()
        {
            _jetwayConnected = !_jetwayConnected;
            _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:ToggleJetway", 
                $"Jetway toggled to {(_jetwayConnected ? "connected" : "disconnected")} (mock)");
            OnEquipmentStateChanged(EquipmentType.Jetway, _jetwayConnected);
        }

        /// <summary>
        /// Toggles the stairs connection state
        /// </summary>
        public void ToggleStairs()
        {
            _stairsConnected = !_stairsConnected;
            _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:ToggleStairs", 
                $"Stairs toggled to {(_stairsConnected ? "connected" : "disconnected")} (mock)");
            OnEquipmentStateChanged(EquipmentType.Stairs, _stairsConnected);
        }

        /// <summary>
        /// Toggles the GPU connection state
        /// </summary>
        public void ToggleGpu()
        {
            _gpuConnected = !_gpuConnected;
            _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:ToggleGpu", 
                $"GPU toggled to {(_gpuConnected ? "connected" : "disconnected")} (mock)");
            OnEquipmentStateChanged(EquipmentType.GPU, _gpuConnected);
        }

        /// <summary>
        /// Toggles the PCA connection state
        /// </summary>
        public void TogglePca()
        {
            _pcaConnected = !_pcaConnected;
            _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:TogglePca", 
                $"PCA toggled to {(_pcaConnected ? "connected" : "disconnected")} (mock)");
            OnEquipmentStateChanged(EquipmentType.PCA, _pcaConnected);
        }

        /// <summary>
        /// Toggles the chocks placement state
        /// </summary>
        public void ToggleChocks()
        {
            _chocksPlaced = !_chocksPlaced;
            _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:ToggleChocks", 
                $"Chocks toggled to {(_chocksPlaced ? "placed" : "removed")} (mock)");
            OnEquipmentStateChanged(EquipmentType.Chocks, _chocksPlaced);
        }

        /// <summary>
        /// Initializes all equipment states to a known state (disconnected/removed)
        /// </summary>
        public void InitializeEquipmentStates()
        {
            _jetwayConnected = false;
            _stairsConnected = false;
            _gpuConnected = false;
            _pcaConnected = false;
            _chocksPlaced = false;
            _logger?.Log(LogLevel.Debug, "MockProsimEquipmentService:InitializeEquipmentStates", 
                "All equipment initialized to disconnected/removed state (mock)");
        }

        /// <summary>
        /// Raises the EquipmentStateChanged event
        /// </summary>
        /// <param name="equipmentType">The type of equipment that changed state</param>
        /// <param name="isConnected">The new state of the equipment</param>
        protected virtual void OnEquipmentStateChanged(EquipmentType equipmentType, bool isConnected)
        {
            EquipmentStateChanged?.Invoke(this, new EquipmentStateChangedEventArgs(equipmentType, isConnected));
        }
    }
}
