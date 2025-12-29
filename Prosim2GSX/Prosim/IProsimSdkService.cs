using ProsimInterface;
using System;
using System.Threading.Tasks;

namespace Prosim2GSX.Prosim
{
    /// <summary>
    /// Interface for ProSim SDK service providing centralized SDK management
    /// </summary>
    public interface IProsimSdkService
    {
        /// <summary>
        /// Indicates whether the SDK is successfully connected to ProSim
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Indicates whether the SDK service has been initialized
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// The ProSim aircraft interface instance managed by this service
        /// </summary>
        ProsimAircraftInterface AircraftInterface { get; }

        /// <summary>
        /// Event raised when connection state changes
        /// </summary>
        event Action<bool> OnConnectionChanged;

        /// <summary>
        /// Initialize the SDK service and create the aircraft interface
        /// </summary>
        /// <returns>True if initialization successful</returns>
        Task<bool> Initialize();

        /// <summary>
        /// Set the aircraft interface instance (called by GsxController)
        /// </summary>
        void SetAircraftInterface(ProsimAircraftInterface aircraftInterface);

        /// <summary>
        /// Verify that the SDK can connect to ProSim
        /// </summary>
        /// <returns>True if connection verified</returns>
        Task<bool> VerifyConnection();

        /// <summary>
        /// Attempt to reconnect to ProSim
        /// </summary>
        Task Reconnect();

        /// <summary>
        /// Start the SDK service monitoring
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the SDK service
        /// </summary>
        Task Stop();
    }
}
