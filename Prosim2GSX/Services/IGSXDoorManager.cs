using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Defines the types of doors available on the aircraft
    /// </summary>
    public enum DoorType
    {
        ForwardLeft,
        ForwardRight,
        AftLeft,
        AftRight,
        ForwardCargo,
        AftCargo
    }

    /// <summary>
    /// Event arguments for door state changes
    /// </summary>
    public class DoorStateChangedEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the type of door that changed state
        /// </summary>
        public DoorType DoorType { get; }

        /// <summary>
        /// Gets a value indicating whether the door is open
        /// </summary>
        public bool IsOpen { get; }

        /// <summary>
        /// Gets the name of the door that changed state
        /// </summary>
        public string DoorName { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="DoorStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="doorType">The type of door that changed state</param>
        /// <param name="isOpen">A value indicating whether the door is open</param>
        public DoorStateChangedEventArgs(DoorType doorType, bool isOpen)
        {
            DoorType = doorType;
            IsOpen = isOpen;
            DoorName = doorType.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoorStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="doorName">The name of the door that changed state</param>
        /// <param name="isOpen">A value indicating whether the door is open</param>
        public DoorStateChangedEventArgs(string doorName, bool isOpen)
        {
            DoorName = doorName;
            IsOpen = isOpen;
            
            // Map string identifiers to enum values
            DoorType = doorName switch
            {
                "ForwardLeftDoor" => DoorType.ForwardLeft,
                "ForwardRightDoor" => DoorType.ForwardRight,
                "AftLeftDoor" => DoorType.AftLeft,
                "AftRightDoor" => DoorType.AftRight,
                "ForwardCargoDoor" => DoorType.ForwardCargo,
                "AftCargoDoor" => DoorType.AftCargo,
                _ => throw new ArgumentException($"Unknown door name: {doorName}", nameof(doorName))
            };
            
        }
    }

    /// <summary>
    /// Interface for managing aircraft doors in GSX
    /// </summary>
    public interface IGSXDoorManager
    {
        /// <summary>
        /// Gets a value indicating whether the forward left door is open
        /// </summary>
        bool IsForwardLeftDoorOpen { get; }
        
        /// <summary>
        /// Gets a value indicating whether the forward right door is open
        /// </summary>
        bool IsForwardRightDoorOpen { get; }

        /// <summary>
        /// Gets a value indicating whether the aft left door is open
        /// </summary>
        bool IsAftLeftDoorOpen { get; }
        
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
        /// Gets a value indicating whether the forward right service is active
        /// </summary>
        bool IsForwardRightServiceActive { get; }

        /// <summary>
        /// Gets a value indicating whether the aft right service is active
        /// </summary>
        bool IsAftRightServiceActive { get; }

        /// <summary>
        /// Gets a value indicating whether the forward cargo service is active
        /// </summary>
        bool IsForwardCargoServiceActive { get; }

        /// <summary>
        /// Gets a value indicating whether the aft cargo service is active
        /// </summary>
        bool IsAftCargoServiceActive { get; }

        /// <summary>
        /// Initializes the door manager
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
        /// Handles a service toggle from GSX
        /// </summary>
        /// <param name="serviceNumber">The service number (1 or 2)</param>
        /// <param name="isActive">True if the service is active, false otherwise</param>
        void HandleServiceToggle(int serviceNumber, bool isActive);
        
        /// <summary>
        /// Handles a cargo door toggle from GSX
        /// </summary>
        /// <param name="cargoNumber">The cargo door number (1 for forward, 2 for aft)</param>
        /// <param name="isActive">True if the toggle is active, false otherwise</param>
        void HandleCargoDoorToggle(int cargoNumber, bool isActive);

        /// <summary>
        /// Handles cargo loading percentage updates
        /// </summary>
        /// <param name="cargoLoadingPercent">The cargo loading percentage (0-100)</param>
        void HandleCargoLoading(int cargoLoadingPercent);

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
        /// Handles a service toggle from GSX asynchronously
        /// </summary>
        /// <param name="serviceNumber">The service number (1 or 2)</param>
        /// <param name="isActive">True if the service is active, false otherwise</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task HandleServiceToggleAsync(int serviceNumber, bool isActive, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Handles a cargo door toggle from GSX asynchronously
        /// </summary>
        /// <param name="cargoNumber">The cargo door number (1 for forward, 2 for aft)</param>
        /// <param name="isActive">True if the toggle is active, false otherwise</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task HandleCargoDoorToggleAsync(int cargoNumber, bool isActive, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles cargo loading percentage updates asynchronously
        /// </summary>
        /// <param name="cargoLoadingPercent">The cargo loading percentage (0-100)</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task HandleCargoLoadingAsync(int cargoLoadingPercent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Occurs when a door state changes
        /// </summary>
        event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
    }
}
