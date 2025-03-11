using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Factory for creating refueling commands
    /// </summary>
    public class RefuelingCommandFactory
    {
        private readonly IProsimFuelService _fuelService;
        private readonly RefuelingStateManager _stateManager;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RefuelingCommandFactory"/> class
        /// </summary>
        /// <param name="fuelService">The ProSim fuel service</param>
        /// <param name="stateManager">The refueling state manager</param>
        /// <param name="logger">The logger</param>
        public RefuelingCommandFactory(
            IProsimFuelService fuelService,
            RefuelingStateManager stateManager,
            ILogger logger)
        {
            _fuelService = fuelService ?? throw new ArgumentNullException(nameof(fuelService));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Creates a command to start refueling
        /// </summary>
        /// <returns>A start refueling command</returns>
        public IRefuelingCommand CreateStartRefuelingCommand()
        {
            return new StartRefuelingCommand(_fuelService, _stateManager, _logger);
        }
        
        /// <summary>
        /// Creates a command to stop refueling
        /// </summary>
        /// <returns>A stop refueling command</returns>
        public IRefuelingCommand CreateStopRefuelingCommand()
        {
            return new StopRefuelingCommand(_fuelService, _stateManager, _logger);
        }
        
        /// <summary>
        /// Creates a command to update the fuel amount
        /// </summary>
        /// <param name="fuelAmount">The fuel amount to set</param>
        /// <returns>An update fuel amount command</returns>
        public IRefuelingCommand CreateUpdateFuelAmountCommand(double fuelAmount)
        {
            return new UpdateFuelAmountCommand(_fuelService, _stateManager, _logger, fuelAmount);
        }
    }
}
