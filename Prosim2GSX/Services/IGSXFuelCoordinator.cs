using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Coordinates fuel operations between GSX and ProSim
    /// </summary>
    public interface IGSXFuelCoordinator : IDisposable
    {
        /// <summary>
        /// Event raised when fuel state changes
        /// </summary>
        event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;
        
        /// <summary>
        /// Event raised when refueling progress changes
        /// </summary>
        event EventHandler<RefuelingProgressChangedEventArgs> RefuelingProgressChanged;
        
        /// <summary>
        /// Gets the current refueling state
        /// </summary>
        RefuelingState RefuelingState { get; }
        
        /// <summary>
        /// Gets the planned fuel amount in kg
        /// </summary>
        double FuelPlanned { get; }
        
        /// <summary>
        /// Gets the current fuel amount in kg
        /// </summary>
        double FuelCurrent { get; }
        
        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        string FuelUnits { get; }
        
        /// <summary>
        /// Gets the refueling progress percentage (0-100)
        /// </summary>
        int RefuelingProgressPercentage { get; }
        
        /// <summary>
        /// Gets the fuel rate in kg/s
        /// </summary>
        float FuelRateKGS { get; }
        
        /// <summary>
        /// Initializes the fuel coordinator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Starts the refueling process
        /// </summary>
        /// <returns>True if refueling was started successfully, false otherwise</returns>
        bool StartRefueling();
        
        /// <summary>
        /// Stops the refueling process
        /// </summary>
        /// <returns>True if refueling was stopped successfully, false otherwise</returns>
        bool StopRefueling();
        
        /// <summary>
        /// Starts the defueling process
        /// </summary>
        /// <returns>True if defueling was started successfully, false otherwise</returns>
        bool StartDefueling();
        
        /// <summary>
        /// Stops the defueling process
        /// </summary>
        /// <returns>True if defueling was stopped successfully, false otherwise</returns>
        bool StopDefueling();
        
        /// <summary>
        /// Updates the fuel amount
        /// </summary>
        /// <param name="fuelAmount">The new fuel amount in kg</param>
        /// <returns>True if the fuel amount was updated successfully, false otherwise</returns>
        bool UpdateFuelAmount(double fuelAmount);
        
        /// <summary>
        /// Starts the refueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if refueling was started successfully, false otherwise</returns>
        Task<bool> StartRefuelingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stops the refueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if refueling was stopped successfully, false otherwise</returns>
        Task<bool> StopRefuelingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Starts the defueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if defueling was started successfully, false otherwise</returns>
        Task<bool> StartDefuelingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stops the defueling process asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if defueling was stopped successfully, false otherwise</returns>
        Task<bool> StopDefuelingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates the fuel amount asynchronously
        /// </summary>
        /// <param name="fuelAmount">The new fuel amount in kg</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the fuel amount was updated successfully, false otherwise</returns>
        Task<bool> UpdateFuelAmountAsync(double fuelAmount, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synchronizes fuel quantities between GSX and ProSim
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SynchronizeFuelQuantitiesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Calculates the required fuel based on the flight plan
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the required fuel amount in kg</returns>
        Task<double> CalculateRequiredFuelAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Manages fuel based on the current flight state
        /// </summary>
        /// <param name="state">The current flight state</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task ManageFuelForStateAsync(FlightState state, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sets the service orchestrator
        /// </summary>
        /// <param name="serviceOrchestrator">The GSX service orchestrator</param>
        void SetServiceOrchestrator(IGSXServiceOrchestrator serviceOrchestrator);
        
        /// <summary>
        /// Registers for state change notifications
        /// </summary>
        /// <param name="stateManager">The state manager to register with</param>
        void RegisterForStateChanges(IGSXStateManager stateManager);
        
        /// <summary>
        /// Sets the event aggregator for publishing events
        /// </summary>
        /// <param name="eventAggregator">The event aggregator</param>
        void SetEventAggregator(IEventAggregator eventAggregator);
    }
}
