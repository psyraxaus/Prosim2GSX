namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for managing GSX ground services
    /// </summary>
    public interface IGsxGroundServicesService
    {
        /// <summary>
        /// Whether the GPU is connected
        /// </summary>
        bool IsGpuConnected { get; }

        /// <summary>
        /// Whether PCA is connected
        /// </summary>
        bool IsPcaConnected { get; }

        /// <summary>
        /// Whether chocks are set
        /// </summary>
        bool AreChocksSet { get; }

        /// <summary>
        /// Whether the ground equipment is fully connected
        /// </summary>
        bool IsGroundEquipmentConnected { get; }

        /// <summary>
        /// Connect the GPU
        /// </summary>
        void ConnectGpu();

        /// <summary>
        /// Disconnect the GPU
        /// </summary>
        void DisconnectGpu();

        /// <summary>
        /// Connect PCA
        /// </summary>
        void ConnectPca();

        /// <summary>
        /// Disconnect PCA
        /// </summary>
        void DisconnectPca();

        /// <summary>
        /// Set chocks
        /// </summary>
        /// <param name="enable">True to set chocks, false to remove</param>
        void SetChocks(bool enable);

        /// <summary>
        /// Call jetway and/or stairs
        /// </summary>
        /// <param name="jetwayOnly">True to call only jetway, false to call stairs too</param>
        void CallJetwayStairs(bool jetwayOnly = false);

        /// <summary>
        /// Remove jetway and/or stairs
        /// </summary>
        void RemoveJetwayStairs();

        /// <summary>
        /// Connect all ground services
        /// </summary>
        /// <param name="jetwayOnly">True to call only jetway, false to call stairs too</param>
        void ConnectAllGroundServices(bool jetwayOnly = false);

        /// <summary>
        /// Disconnect all ground services
        /// </summary>
        void DisconnectAllGroundServices();
    }
}