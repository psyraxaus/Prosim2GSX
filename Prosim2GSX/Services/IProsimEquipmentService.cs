using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for managing ProSim equipment services such as PCA, chocks, and GPU
    /// </summary>
    public interface IProsimEquipmentService
    {
        /// <summary>
        /// Event raised when equipment state changes
        /// </summary>
        event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;

        /// <summary>
        /// Sets the state of the Preconditioned Air (PCA) service
        /// </summary>
        /// <param name="enable">True to enable PCA, false to disable</param>
        void SetServicePCA(bool enable);

        /// <summary>
        /// Sets the state of the wheel chocks
        /// </summary>
        /// <param name="enable">True to place chocks, false to remove</param>
        void SetServiceChocks(bool enable);

        /// <summary>
        /// Sets the state of the Ground Power Unit (GPU)
        /// </summary>
        /// <param name="enable">True to connect GPU, false to disconnect</param>
        void SetServiceGPU(bool enable);
    }
}
