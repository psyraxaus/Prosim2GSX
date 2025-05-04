using System.Threading.Tasks;
using Prosim2GSX.Services.Prosim.Events;
using Prosim2GSX.Services.Prosim.Models;

namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for managing Prosim loadsheet generation
    /// </summary>
    public interface ILoadsheetService
    {
        /// <summary>
        /// Subscribe to loadsheet data changes - should only be called after flight plan is loaded
        /// and loadsheet datarefs exist
        /// </summary>
        void SubscribeToLoadsheetChanges();

        /// <summary>
        /// Generate a loadsheet using Prosim's native functionality
        /// </summary>
        /// <param name="type">Type of loadsheet ("Preliminary" or "Final")</param>
        /// <param name="maxRetries">Maximum number of retries if generation fails</param>
        /// <param name="force">Force generation even if already generated</param>
        /// <returns>Task that completes with detailed result information</returns>
        Task<LoadsheetResult> GenerateLoadsheet(string type, int maxRetries = 3, bool force = false);

        /// <summary>
        /// Resend the current loadsheet to the MCDU
        /// </summary>
        Task<bool> ResendLoadsheet();

        /// <summary>
        /// Check if the Prosim EFB server is running and accessible
        /// </summary>
        /// <returns>True if the server is accessible, false otherwise</returns>
        Task<bool> CheckServerStatus();

        /// <summary>
        /// Reset all loadsheets
        /// </summary>
        Task<bool> ResetLoadsheets();
    }
}
