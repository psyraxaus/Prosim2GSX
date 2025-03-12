using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for managing ProSim equipment such as PCA, chocks, and GPU
    /// </summary>
    public class ProsimEquipmentService : IProsimEquipmentService
    {
        private readonly IProsimService _prosimService;

        /// <summary>
        /// Event raised when equipment state changes
        /// </summary>
        public event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;

        /// <summary>
        /// Creates a new instance of ProsimEquipmentService
        /// </summary>
        /// <param name="prosimService">The ProSim service to use for communication with ProSim</param>
        /// <exception cref="ArgumentNullException">Thrown if prosimService is null</exception>
        public ProsimEquipmentService(IProsimService prosimService)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
        }

        /// <summary>
        /// Sets the state of the Preconditioned Air (PCA) service
        /// </summary>
        /// <param name="enable">True to enable PCA, false to disable</param>
        public void SetServicePCA(bool enable)
        {
            _prosimService.SetVariable("groundservice.preconditionedAir", enable);
            OnEquipmentStateChanged("PCA", enable);
        }

        /// <summary>
        /// Sets the state of the wheel chocks
        /// </summary>
        /// <param name="enable">True to place chocks, false to remove</param>
        public void SetServiceChocks(bool enable)
        {
            _prosimService.SetVariable("efb.chocks", enable);
            OnEquipmentStateChanged("Chocks", enable);
        }

        /// <summary>
        /// Sets the state of the Ground Power Unit (GPU)
        /// </summary>
        /// <param name="enable">True to connect GPU, false to disconnect</param>
        public void SetServiceGPU(bool enable)
        {
            _prosimService.SetVariable("groundservice.groundpower", enable);
            OnEquipmentStateChanged("GPU", enable);
        }

        /// <summary>
        /// Gets a value indicating whether the jetway is connected
        /// </summary>
        /// <returns>True if the jetway is connected, false otherwise</returns>
        public bool IsJetwayConnected()
        {
            // Implementation would depend on how ProSim exposes this information
            // For now, return a placeholder value
            return (bool)_prosimService.ReadDataRef("groundservice.jetway");
        }

        /// <summary>
        /// Gets a value indicating whether the stairs are connected
        /// </summary>
        /// <returns>True if the stairs are connected, false otherwise</returns>
        public bool IsStairsConnected()
        {
            // Implementation would depend on how ProSim exposes this information
            // For now, return a placeholder value
            return (bool)_prosimService.ReadDataRef("groundservice.stairs");
        }

        /// <summary>
        /// Gets a value indicating whether the GPU is connected
        /// </summary>
        /// <returns>True if the GPU is connected, false otherwise</returns>
        public bool IsGpuConnected()
        {
            return (bool)_prosimService.ReadDataRef("groundservice.groundpower");
        }

        /// <summary>
        /// Gets a value indicating whether the PCA is connected
        /// </summary>
        /// <returns>True if the PCA is connected, false otherwise</returns>
        public bool IsPcaConnected()
        {
            return (bool)_prosimService.ReadDataRef("groundservice.preconditionedAir");
        }

        /// <summary>
        /// Gets a value indicating whether the chocks are placed
        /// </summary>
        /// <returns>True if the chocks are placed, false otherwise</returns>
        public bool AreChocksPlaced()
        {
            return (bool)_prosimService.ReadDataRef("efb.chocks");
        }

        /// <summary>
        /// Toggles the jetway connection state
        /// </summary>
        public void ToggleJetway()
        {
            bool currentState = IsJetwayConnected();
            _prosimService.SetVariable("groundservice.jetway", !currentState);
            OnEquipmentStateChanged("Jetway", !currentState);
        }

        /// <summary>
        /// Toggles the stairs connection state
        /// </summary>
        public void ToggleStairs()
        {
            bool currentState = IsStairsConnected();
            _prosimService.SetVariable("groundservice.stairs", !currentState);
            OnEquipmentStateChanged("Stairs", !currentState);
        }

        /// <summary>
        /// Toggles the GPU connection state
        /// </summary>
        public void ToggleGpu()
        {
            bool currentState = IsGpuConnected();
            SetServiceGPU(!currentState);
        }

        /// <summary>
        /// Toggles the PCA connection state
        /// </summary>
        public void TogglePca()
        {
            bool currentState = IsPcaConnected();
            SetServicePCA(!currentState);
        }

        /// <summary>
        /// Toggles the chocks placement state
        /// </summary>
        public void ToggleChocks()
        {
            bool currentState = AreChocksPlaced();
            SetServiceChocks(!currentState);
        }

        /// <summary>
        /// Raises the EquipmentStateChanged event
        /// </summary>
        /// <param name="equipmentName">Name of the equipment</param>
        /// <param name="isEnabled">Whether the equipment is enabled</param>
        protected virtual void OnEquipmentStateChanged(string equipmentName, bool isEnabled)
        {
            EquipmentStateChanged?.Invoke(this, new EquipmentStateChangedEventArgs(equipmentName, isEnabled));
        }
    }
}
