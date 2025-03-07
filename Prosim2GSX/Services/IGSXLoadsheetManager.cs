using System;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX loadsheet management
    /// </summary>
    public interface IGSXLoadsheetManager
    {
        /// <summary>
        /// Event raised when a loadsheet is generated
        /// </summary>
        event EventHandler<LoadsheetGeneratedEventArgs> LoadsheetGenerated;
        
        /// <summary>
        /// Initializes the loadsheet manager
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Generates and sends a preliminary loadsheet
        /// </summary>
        /// <param name="flightNumber">The flight number</param>
        /// <returns>True if the loadsheet was generated and sent successfully</returns>
        Task<bool> GeneratePreliminaryLoadsheetAsync(string flightNumber);
        
        /// <summary>
        /// Generates and sends a final loadsheet
        /// </summary>
        /// <param name="flightNumber">The flight number</param>
        /// <returns>True if the loadsheet was generated and sent successfully</returns>
        Task<bool> GenerateFinalLoadsheetAsync(string flightNumber);
        
        /// <summary>
        /// Checks if a preliminary loadsheet has been sent
        /// </summary>
        /// <returns>True if a preliminary loadsheet has been sent</returns>
        bool IsPreliminaryLoadsheetSent();
        
        /// <summary>
        /// Checks if a final loadsheet has been sent
        /// </summary>
        /// <returns>True if a final loadsheet has been sent</returns>
        bool IsFinalLoadsheetSent();
        
        /// <summary>
        /// Resets the loadsheet manager state
        /// </summary>
        void Reset();
    }
}
