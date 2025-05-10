using Prosim2GSX.Services.Prosim.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Interface for direct interaction with ProSim SDK
    /// </summary>
    public interface IProsimInterface
    {
        /// <summary>
        /// Connect to the ProSim SDK
        /// </summary>
        void ConnectProsimSDK();

        /// <summary>
        /// Check if ProSim is ready
        /// </summary>
        /// <returns>True if ProSim is connected and ready</returns>
        bool IsProsimReady();

        /// <summary>
        /// Get a ProSim variable
        /// </summary>
        /// <param name="dataRef">The dataref to get</param>
        /// <returns>The value of the variable</returns>
        dynamic GetProsimVariable(string dataRef);

        /// <summary>
        /// Set a ProSim variable
        /// </summary>
        /// <param name="dataRef">The dataref to set</param>
        /// <param name="value">The value to set</param>
        void SetProsimVariable(string dataRef, object value);

        /// <summary>
        /// Get the Prosim backend URL
        /// </summary>
        /// <returns>The URL for the Prosim backend</returns>
        string GetBackendUrl();

        /// <summary>
        /// Make a POST request to the Prosim backend
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="jsonContent">The JSON content</param>
        /// <returns>True if successful</returns>
        Task<bool> PostAsync(string url, string jsonContent);

        /// <summary>
        /// Make a POST request with detailed result
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="jsonContent">The JSON content</param>
        /// <returns>The operation result</returns>
        Task<HttpOperationResult> PostAsyncWithDetails(string url, string jsonContent);

        /// <summary>
        /// Make a DELETE request to the Prosim backend
        /// </summary>
        /// <param name="url">The URL</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteAsync(string url);

        /// <summary>
        /// Make a DELETE request with detailed result
        /// </summary>
        /// <param name="url">The URL</param>
        /// <returns>The operation result</returns>
        Task<HttpOperationResult> DeleteAsyncWithDetails(string url);

        // Add this method to the interface
        /// <summary>
        /// Gets the status of a function as an integer
        /// </summary>
        /// <param name="dataRef">The dataref to check</param>
        /// <returns>Status as integer (1 for true/on, 0 for false/off)</returns>
        int GetStatusFunction(string dataRef);
    }
}
