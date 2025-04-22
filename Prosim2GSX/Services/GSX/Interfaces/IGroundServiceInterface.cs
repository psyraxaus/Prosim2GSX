using Prosim2GSX.Services.GSX.Models;

namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Interface for ground service operations
    /// </summary>
    public interface IGroundServiceInterface
    {
        /// <summary>
        /// Set chocks status
        /// </summary>
        /// <param name="enable">True to enable chocks, false to disable</param>
        void SetChocks(bool enable);

        /// <summary>
        /// Set ground power unit status
        /// </summary>
        /// <param name="enable">True to enable GPU, false to disable</param>
        void SetGPU(bool enable);

        /// <summary>
        /// Set preconditioned air status
        /// </summary>
        /// <param name="enable">True to enable PCA, false to disable</param>
        void SetPCA(bool enable);

        /// <summary>
        /// Get the current status of ground services
        /// </summary>
        /// <returns>Current ground service status</returns>
        GroundServiceStatus GetStatus();
    }
}