using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates passenger operations between GSX and ProSim
    /// </summary>
    public interface IGSXPassengerCoordinator : IDisposable
    {
        /// <summary>
        /// Event raised when passenger state changes
        /// </summary>
        event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
        
        /// <summary>
        /// Gets the planned number of passengers
        /// </summary>
        int PassengersPlanned { get; }
        
        /// <summary>
        /// Gets the current number of passengers
        /// </summary>
        int PassengersCurrent { get; }
        
        /// <summary>
        /// Gets a value indicating whether boarding is in progress
        /// </summary>
        bool IsBoardingInProgress { get; }
        
        /// <summary>
        /// Gets a value indicating whether deboarding is in progress
        /// </summary>
        bool IsDeboardingInProgress { get; }
        
        /// <summary>
        /// Initializes the passenger coordinator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Starts the boarding process
        /// </summary>
        /// <returns>True if boarding was started successfully, false otherwise</returns>
        bool StartBoarding();
        
        /// <summary>
        /// Stops the boarding process
        /// </summary>
        /// <returns>True if boarding was stopped successfully, false otherwise</returns>
        bool StopBoarding();
        
        /// <summary>
        /// Starts the deboarding process
        /// </summary>
        /// <returns>True if deboarding was started successfully, false otherwise</returns>
        bool StartDeboarding();
        
        /// <summary>
        /// Stops the deboarding process
        /// </summary>
        /// <returns>True if deboarding was stopped successfully, false otherwise</returns>
        bool StopDeboarding();
        
        /// <summary>
        /// Updates the passenger count
        /// </summary>
        /// <param name="passengerCount">The new passenger count</param>
        /// <returns>True if the passenger count was updated successfully, false otherwise</returns>
        bool UpdatePassengerCount(int passengerCount);
        
        /// <summary>
        /// Starts the boarding process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if boarding was started successfully, false otherwise</returns>
        Task<bool> StartBoardingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stops the boarding process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if boarding was stopped successfully, false otherwise</returns>
        Task<bool> StopBoardingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Starts the deboarding process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if deboarding was started successfully, false otherwise</returns>
        Task<bool> StartDeboardingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stops the deboarding process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if deboarding was stopped successfully, false otherwise</returns>
        Task<bool> StopDeboardingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates the passenger count asynchronously
        /// </summary>
        /// <param name="passengerCount">The new passenger count</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the passenger count was updated successfully, false otherwise</returns>
        Task<bool> UpdatePassengerCountAsync(int passengerCount, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synchronizes passenger states between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SynchronizePassengerStatesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Manages passengers based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task ManagePassengersForStateAsync(FlightState state, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        void RegisterForStateChanges(IGSXStateManager stateManager);
        
        /// <summary>
        /// Sets the service orchestrator
        /// </summary>
        /// <param name="serviceOrchestrator">The GSX service orchestrator</param>
        void SetServiceOrchestrator(IGSXServiceOrchestrator serviceOrchestrator);
    }
}
