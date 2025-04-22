using System.Threading.Tasks;
using Prosim2GSX.Services.GSX.Models;

namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for generating loadsheets
    /// </summary>
    public interface IGsxLoadsheetService
    {
        /// <summary>
        /// Current state of the preliminary loadsheet generation
        /// </summary>
        LoadsheetState PreliminaryLoadsheetState { get; }

        /// <summary>
        /// Current state of the final loadsheet generation
        /// </summary>
        LoadsheetState FinalLoadsheetState { get; }

        /// <summary>
        /// Generate a preliminary loadsheet
        /// </summary>
        /// <returns>Result of the loadsheet generation</returns>
        Task<LoadsheetResult> GeneratePreliminaryLoadsheet();

        /// <summary>
        /// Generate a final loadsheet
        /// </summary>
        /// <returns>Result of the loadsheet generation</returns>
        Task<LoadsheetResult> GenerateFinalLoadsheet();

        /// <summary>
        /// Check if a loadsheet is available
        /// </summary>
        /// <param name="type">Type of loadsheet ("Preliminary" or "Final")</param>
        /// <returns>True if available</returns>
        bool IsLoadsheetAvailable(string type);

        /// <summary>
        /// Reset loadsheet generation states
        /// </summary>
        void ResetLoadsheetStates();

        /// <summary>
        /// Check if the loadsheet server is available
        /// </summary>
        /// <returns>True if available</returns>
        Task<bool> CheckServerStatus();

        /// <summary>
        /// Subscribe to loadsheet changes
        /// </summary>
        void SubscribeToLoadsheetChanges();

        /// <summary>
        /// Get loadsheet data
        /// </summary>
        /// <param name="type">Type of loadsheet ("Preliminary" or "Final")</param>
        /// <returns>Loadsheet data</returns>
        dynamic GetLoadsheetData(string type);
    }
}