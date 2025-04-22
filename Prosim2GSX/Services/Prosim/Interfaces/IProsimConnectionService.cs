using System;

namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for managing ProSim connection
    /// </summary>
    public interface IProsimConnectionService
    {
        /// <summary>
        /// Whether ProSim is currently connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Initialize the ProSim connection service
        /// </summary>
        /// <returns>True if initialization was successful</returns>
        bool Initialize();

        /// <summary>
        /// Connect to ProSim SDK
        /// </summary>
        /// <returns>True if connection successful</returns>
        bool Connect();

        /// <summary>
        /// Disconnect from ProSim SDK
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Wait for ProSim to become available
        /// </summary>
        /// <param name="cancellationRequested">Callback to check if operation should be cancelled</param>
        /// <returns>True if ProSim became available</returns>
        bool WaitForAvailability(Func<bool> cancellationRequested = null);
    }
}