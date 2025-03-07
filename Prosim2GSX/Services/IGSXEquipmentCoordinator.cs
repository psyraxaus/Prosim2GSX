using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates ground equipment operations between GSX and ProSim
    /// </summary>
    public interface IGSXEquipmentCoordinator : IDisposable
    {
        /// <summary>
        /// Event raised when equipment state changes
        /// </summary>
        event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
        
        /// <summary>
        /// Gets a value indicating whether the GPU is connected
        /// </summary>
        bool IsGpuConnected { get; }
        
        /// <summary>
        /// Gets a value indicating whether the PCA is connected
        /// </summary>
        bool IsPcaConnected { get; }
        
        /// <summary>
        /// Gets a value indicating whether the chocks are placed
        /// </summary>
        bool AreChocksPlaced { get; }
        
        /// <summary>
        /// Gets a value indicating whether the jetway is connected
        /// </summary>
        bool IsJetwayConnected { get; }
        
        /// <summary>
        /// Initializes the equipment coordinator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Connects the specified equipment
        /// </summary>
        /// <param name="equipmentType">The type of equipment to connect</param>
        /// <returns>True if the equipment was connected successfully, false otherwise</returns>
        bool ConnectEquipment(EquipmentType equipmentType);
        
        /// <summary>
        /// Disconnects the specified equipment
        /// </summary>
        /// <param name="equipmentType">The type of equipment to disconnect</param>
        /// <returns>True if the equipment was disconnected successfully, false otherwise</returns>
        bool DisconnectEquipment(EquipmentType equipmentType);
        
        /// <summary>
        /// Connects the specified equipment asynchronously
        /// </summary>
        /// <param name="equipmentType">The type of equipment to connect</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the equipment was connected successfully, false otherwise</returns>
        Task<bool> ConnectEquipmentAsync(EquipmentType equipmentType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Disconnects the specified equipment asynchronously
        /// </summary>
        /// <param name="equipmentType">The type of equipment to disconnect</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the equipment was disconnected successfully, false otherwise</returns>
        Task<bool> DisconnectEquipmentAsync(EquipmentType equipmentType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synchronizes equipment states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SynchronizeEquipmentStatesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Manages equipment based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task ManageEquipmentForStateAsync(FlightState state, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        void RegisterForStateChanges(IGSXStateManager stateManager);
    }
}
