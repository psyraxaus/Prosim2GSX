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
