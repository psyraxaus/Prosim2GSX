using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Command to stop the refueling process
    /// </summary>
    public class StopRefuelingCommand : IRefuelingCommand
    {
        private readonly IProsimFuelService _fuelService;
        private readonly RefuelingStateManager _stateManager;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="StopRefuelingCommand"/> class
        /// </summary>
        /// <param name="fuelService">The ProSim fuel service</param>
        /// <param name="stateManager">The refueling state manager</param>
        /// <param name="logger">The logger</param>
        public StopRefuelingCommand(
            IProsimFuelService fuelService,
            RefuelingStateManager stateManager,
            ILogger logger)
        {
            _fuelService = fuelService ?? throw new ArgumentNullException(nameof(fuelService));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.Log(LogLevel.Debug, "StopRefuelingCommand:ExecuteAsync", "Stopping refueling process");
                
                // Check if cancellation is requested
                cancellationToken.ThrowIfCancellationRequested();
                
                if (_stateManager.State != RefuelingState.Refueling && 
                    _stateManager.State != RefuelingState.Requested &&
                    _stateManager.State != RefuelingState.Complete)
                {
                    _logger.Log(LogLevel.Warning, "StopRefuelingCommand:ExecuteAsync", "No refueling in progress");
                    return false;
                }
                
                // Transition to Idle state
                if (!_stateManager.TransitionTo(RefuelingState.Idle))
                {
                    _logger.Log(LogLevel.Warning, "StopRefuelingCommand:ExecuteAsync", "Invalid state transition");
                    return false;
                }
                
                // Stop refueling in ProSim
                _fuelService.RefuelStop();
                
                return await Task.FromResult(true);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Warning, "StopRefuelingCommand:ExecuteAsync", "Operation canceled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "StopRefuelingCommand:ExecuteAsync", $"Error stopping refueling: {ex.Message}");
                _stateManager.TransitionTo(RefuelingState.Error);
                return false;
            }
        }
    }
}
