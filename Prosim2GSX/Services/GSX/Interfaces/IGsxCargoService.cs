using Prosim2GSX.Services.GSX.Events;
using System;

namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for managing cargo operations and doors
    /// </summary>
    public interface IGsxCargoService
    {
        /// <summary>
        /// Gets whether the forward cargo door is open
        /// </summary>
        bool IsForwardCargoDoorOpen { get; }

        /// <summary>
        /// Gets whether the aft cargo door is open
        /// </summary>
        bool IsAftCargoDoorOpen { get; }

        /// <summary>
        /// Gets whether the cargo loading is active
        /// </summary>
        bool IsCargoLoadingActive { get; }

        /// <summary>
        /// Gets whether the cargo unloading is active
        /// </summary>
        bool IsCargoUnloadingActive { get; }

        /// <summary>
        /// Gets the cargo loading percentage
        /// </summary>
        int CargoLoadingPercentage { get; }

        /// <summary>
        /// Gets the cargo unloading percentage
        /// </summary>
        int CargoUnloadingPercentage { get; }

        /// <summary>
        /// Opens the forward cargo door
        /// </summary>
        void OpenForwardCargoDoor();

        /// <summary>
        /// Closes the forward cargo door
        /// </summary>
        void CloseForwardCargoDoor();

        /// <summary>
        /// Opens the aft cargo door
        /// </summary>
        void OpenAftCargoDoor();

        /// <summary>
        /// Closes the aft cargo door
        /// </summary>
        void CloseAftCargoDoor();

        /// <summary>
        /// Opens both cargo doors
        /// </summary>
        void OpenCargoDoors();

        /// <summary>
        /// Closes both cargo doors
        /// </summary>
        void CloseCargoDoors();

        /// <summary>
        /// Toggle the forward cargo door state
        /// </summary>
        void ToggleForwardCargoDoor();

        /// <summary>
        /// Toggle the aft cargo door state
        /// </summary>
        void ToggleAftCargoDoor();

        /// <summary>
        /// Subscribes to cargo LVARs and service toggle LVARs
        /// </summary>
        void SubscribeToCargoEvents();

        /// <summary>
        /// Process cargo loading/unloading operations
        /// </summary>
        void ProcessCargoOperations();

        /// <summary>
        /// Event raised when cargo loading/unloading state changes
        /// </summary>
        event EventHandler<CargoOperationEventArgs> CargoOperationChanged;

        /// <summary>
        /// Event raised when cargo loading/unloading percentage changes
        /// </summary>
        event EventHandler<CargoPercentageEventArgs> CargoPercentageChanged;

        /// <summary>
        /// Event raised when cargo door state changes
        /// </summary>
        event EventHandler<CargoDoorEventArgs> CargoDoorStateChanged;
    }
}
