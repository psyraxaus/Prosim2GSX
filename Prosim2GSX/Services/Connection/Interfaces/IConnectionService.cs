using System;
using System.Threading.Tasks;
using Prosim2GSX.Services.Connection.Events;

namespace Prosim2GSX.Services.Connection.Interfaces
{
    /// <summary>
    /// Service for managing connections to external systems
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>
        /// Establishes connection to the Flight Simulator process
        /// </summary>
        /// <returns>True if connection was established, false otherwise</returns>
        Task<bool> ConnectToFlightSimulatorAsync();

        /// <summary>
        /// Establishes connection to SimConnect API
        /// </summary>
        /// <returns>True if connection was established, false otherwise</returns>
        Task<bool> ConnectToSimConnectAsync();

        /// <summary>
        /// Establishes connection to Prosim737 application
        /// </summary>
        /// <returns>True if connection was established, false otherwise</returns>
        Task<bool> ConnectToProsimAsync();

        /// <summary>
        /// Waits for the simulator session to be ready
        /// </summary>
        /// <returns>True if session is ready, false otherwise</returns>
        Task<bool> WaitForSessionReadyAsync();

        /// <summary>
        /// Gets whether Flight Simulator is connected
        /// </summary>
        bool IsFlightSimulatorConnected { get; }

        /// <summary>
        /// Gets whether SimConnect is connected
        /// </summary>
        bool IsSimConnectConnected { get; }

        /// <summary>
        /// Gets whether Prosim is connected
        /// </summary>
        bool IsProsimConnected { get; }

        /// <summary>
        /// Gets whether the simulator session is ready
        /// </summary>
        bool IsSessionReady { get; }

        /// <summary>
        /// Disconnects from all external systems
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Event raised when connection status changes
        /// </summary>
        event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
    }
}