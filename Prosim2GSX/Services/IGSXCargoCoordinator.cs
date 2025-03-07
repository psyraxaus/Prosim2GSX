using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates cargo operations between GSX and ProSim
    /// </summary>
    public interface IGSXCargoCoordinator : IDisposable
    {
        /// <summary>
        /// Event raised when cargo state changes
        /// </summary>
        event EventHandler<CargoStateChangedEventArgs> CargoStateChanged;
        
        /// <summary>
        /// Gets the planned cargo amount
        /// </summary>
        int CargoPlanned { get; }
        
        /// <summary>
        /// Gets the current cargo percentage
        /// </summary>
        int CargoCurrentPercentage { get; }
        
        /// <summary>
        /// Gets a value indicating whether loading is in progress
        /// </summary>
        bool IsLoadingInProgress { get; }
        
        /// <summary>
        /// Gets a value indicating whether unloading is in progress
        /// </summary>
        bool IsUnloadingInProgress { get; }
        
        /// <summary>
        /// Initializes the cargo coordinator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Starts the cargo loading process
        /// </summary>
        /// <returns>True if loading was started successfully, false otherwise</returns>
        bool StartLoading();
        
        /// <summary>
        /// Stops the cargo loading process
        /// </summary>
        /// <returns>True if loading was stopped successfully, false otherwise</returns>
        bool StopLoading();
        
        /// <summary>
        /// Starts the cargo unloading process
        /// </summary>
        /// <returns>True if unloading was started successfully, false otherwise</returns>
        bool StartUnloading();
        
        /// <summary>
        /// Stops the cargo unloading process
        /// </summary>
        /// <returns>True if unloading was stopped successfully, false otherwise</returns>
        bool StopUnloading();
        
        /// <summary>
        /// Updates the cargo amount
        /// </summary>
        /// <param name="cargoAmount">The new cargo amount</param>
        /// <returns>True if the cargo amount was updated successfully, false otherwise</returns>
        bool UpdateCargoAmount(int cargoAmount);
        
        /// <summary>
        /// Changes the cargo percentage
        /// </summary>
        /// <param name="percentage">The new cargo percentage (0-100)</param>
        /// <returns>True if the cargo percentage was changed successfully, false otherwise</returns>
        bool ChangeCargoPercentage(int percentage);
        
        /// <summary>
        /// Starts the cargo loading process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if loading was started successfully, false otherwise</returns>
        Task<bool> StartLoadingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stops the cargo loading process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if loading was stopped successfully, false otherwise</returns>
        Task<bool> StopLoadingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Starts the cargo unloading process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if unloading was started successfully, false otherwise</returns>
        Task<bool> StartUnloadingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stops the cargo unloading process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if unloading was stopped successfully, false otherwise</returns>
        Task<bool> StopUnloadingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates the cargo amount asynchronously
        /// </summary>
        /// <param name="cargoAmount">The new cargo amount</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the cargo amount was updated successfully, false otherwise</returns>
        Task<bool> UpdateCargoAmountAsync(int cargoAmount, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Changes the cargo percentage asynchronously
        /// </summary>
        /// <param name="percentage">The new cargo percentage (0-100)</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the cargo percentage was changed successfully, false otherwise</returns>
        Task<bool> ChangeCargoPercentageAsync(int percentage, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synchronizes cargo states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SynchronizeCargoStatesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Manages cargo based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task ManageCargoForStateAsync(FlightState state, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Coordinates with doors for cargo operations
        /// </summary>
        /// <param name="forLoading">True for loading operations, false for unloading or closing</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task CoordinateWithDoorsAsync(bool forLoading, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        void RegisterForStateChanges(IGSXStateManager stateManager);
        
        /// <summary>
        /// Registers a door coordinator for cargo door operations
        /// </summary>
        /// <param name="doorCoordinator">The door coordinator to register</param>
        void RegisterDoorCoordinator(IGSXDoorCoordinator doorCoordinator);
    }
}
