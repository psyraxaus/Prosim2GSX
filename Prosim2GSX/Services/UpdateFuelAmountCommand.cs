using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Command to update the fuel amount
    /// </summary>
    public class UpdateFuelAmountCommand : IRefuelingCommand
    {
        private readonly IProsimFuelService _fuelService;
        private readonly RefuelingStateManager _stateManager;
        private readonly ILogger _logger;
        private readonly double _fuelAmount;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateFuelAmountCommand"/> class
        /// </summary>
        /// <param name="fuelService">The ProSim fuel service</param>
        /// <param name="stateManager">The refueling state manager</param>
        /// <param name="logger">The logger</param>
        /// <param name="fuelAmount">The fuel amount to set</param>
        public UpdateFuelAmountCommand(
            IProsimFuelService fuelService,
            RefuelingStateManager stateManager,
            ILogger logger,
            double fuelAmount)
        {
            _fuelService = fuelService ?? throw new ArgumentNullException(nameof(fuelService));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            if (fuelAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(fuelAmount), "Fuel amount cannot be negative");
                
            _fuelAmount = fuelAmount;
        }
        
        /// <summary>
        /// Executes the command asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the command was executed successfully, false otherwise</returns>
        public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log(LogLevel.Debug, "UpdateFuelAmountCommand:ExecuteAsync", $"Updating fuel amount to {_fuelAmount} kg");
                
                // Check if cancellation is requested
                cancellationToken.ThrowIfCancellationRequested();
                
                // Always update the planned fuel amount
                _fuelService.UpdatePlannedFuel(_fuelAmount);
                
                // Only set the current fuel amount if we're not in a refueling state
                bool isRefueling = _stateManager.State == RefuelingState.Requested || 
                                  _stateManager.State == RefuelingState.Refueling;
                
                if (!isRefueling)
                {
                    _logger.Log(LogLevel.Debug, "UpdateFuelAmountCommand:ExecuteAsync", 
                        "Not in refueling state, setting current fuel to match planned");
                    _fuelService.SetCurrentFuel(_fuelAmount);
                }
                else
                {
                    _logger.Log(LogLevel.Debug, "UpdateFuelAmountCommand:ExecuteAsync", 
                        "In refueling state, not updating current fuel amount");
                }
                
                return await Task.FromResult(true);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "UpdateFuelAmountCommand:ExecuteAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "UpdateFuelAmountCommand:ExecuteAsync", $"Error updating fuel amount: {ex.Message}");
                return false;
            }
        }
    }
}
