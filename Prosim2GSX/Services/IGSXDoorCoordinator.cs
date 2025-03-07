using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates door operations between GSX and ProSim
    /// </summary>
    public interface IGSXDoorCoordinator : IDisposable
    {
        /// <summary>
        /// Event raised when a door state changes
        /// </summary>
        event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
        
        /// <summary>
        /// Gets a value indicating whether the forward right door is open
        /// </summary>
        bool IsForwardRightDoorOpen { get; }
        
        /// <summary>
        /// Gets a value indicating whether the aft right door is open
        /// </summary>
        bool IsAftRightDoorOpen { get; }
        
        /// <summary>
        /// Gets a value indicating whether the forward cargo door is open
        /// </summary>
        bool IsForwardCargoDoorOpen { get; }
        
        /// <summary>
        /// Gets a value indicating whether the aft cargo door is open
        /// </summary>
        bool IsAftCargoDoorOpen { get; }
        
        /// <summary>
        /// Initializes the door coordinator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Opens a door
        /// </summary>
        /// <param name="doorType">The type of door to open</param>
        /// <returns>True if the door was opened successfully, false otherwise</returns>
        bool OpenDoor(DoorType doorType);
        
        /// <summary>
        /// Closes a door
        /// </summary>
        /// <param name="doorType">The type of door to close</param>
        /// <returns>True if the door was closed successfully, false otherwise</returns>
        bool CloseDoor(DoorType doorType);
        
        /// <summary>
        /// Opens a door asynchronously
        /// </summary>
        /// <param name="doorType">The type of door to open</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the door was opened successfully, false otherwise</returns>
        Task<bool> OpenDoorAsync(DoorType doorType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Closes a door asynchronously
        /// </summary>
        /// <param name="doorType">The type of door to close</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the door was closed successfully, false otherwise</returns>
        Task<bool> CloseDoorAsync(DoorType doorType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synchronizes door states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SynchronizeDoorStatesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Manages doors based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task ManageDoorsForStateAsync(FlightState state, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        void RegisterForStateChanges(IGSXStateManager stateManager);
    }
}
