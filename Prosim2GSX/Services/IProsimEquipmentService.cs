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

        /// <summary>
        /// Gets a value indicating whether the jetway is connected
        /// </summary>
        /// <returns>True if the jetway is connected, false otherwise</returns>
        bool IsJetwayConnected();

        /// <summary>
        /// Gets a value indicating whether the stairs are connected
        /// </summary>
        /// <returns>True if the stairs are connected, false otherwise</returns>
        bool IsStairsConnected();

        /// <summary>
        /// Gets a value indicating whether the GPU is connected
        /// </summary>
        /// <returns>True if the GPU is connected, false otherwise</returns>
        bool IsGpuConnected();

        /// <summary>
        /// Gets a value indicating whether the PCA is connected
        /// </summary>
        /// <returns>True if the PCA is connected, false otherwise</returns>
        bool IsPcaConnected();

        /// <summary>
        /// Gets a value indicating whether the chocks are placed
        /// </summary>
        /// <returns>True if the chocks are placed, false otherwise</returns>
        bool AreChocksPlaced();

        /// <summary>
        /// Toggles the jetway connection state
        /// </summary>
        void ToggleJetway();

        /// <summary>
        /// Toggles the stairs connection state
        /// </summary>
        void ToggleStairs();

        /// <summary>
        /// Toggles the GPU connection state
        /// </summary>
        void ToggleGpu();

        /// <summary>
        /// Toggles the PCA connection state
        /// </summary>
        void TogglePca();

        /// <summary>
        /// Toggles the chocks placement state
        /// </summary>
        void ToggleChocks();
    }
}
