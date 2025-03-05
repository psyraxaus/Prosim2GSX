using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for managing cargo operations in ProSim
    /// </summary>
    public class ProsimCargoService : IProsimCargoService
    {
        private readonly IProsimService _prosimService;
        private int _cargoPlanned;
        private int _cargoCurrentPercentage;
        private const float _cargoDistMain = 4000.0f / 9440.0f;
        private const float _cargoDistBulk = 1440.0f / 9440.0f;
        
        /// <summary>
        /// Gets the planned cargo amount
        /// </summary>
        public int CargoPlanned => _cargoPlanned;
        
        /// <summary>
        /// Gets the current cargo amount as a percentage of planned
        /// </summary>
        public int CargoCurrentPercentage => _cargoCurrentPercentage;
        
        /// <summary>
        /// Event raised when cargo state changes
        /// </summary>
        public event EventHandler<CargoStateChangedEventArgs> CargoStateChanged;
        
        /// <summary>
        /// Creates a new instance of ProsimCargoService
        /// </summary>
        /// <param name="prosimService">The ProSim service to use for communication with ProSim</param>
        /// <exception cref="ArgumentNullException">Thrown if prosimService is null</exception>
        public ProsimCargoService(IProsimService prosimService)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _cargoPlanned = 0;
            _cargoCurrentPercentage = 0;
        }
        
        /// <summary>
        /// Updates cargo data from a flight plan
        /// </summary>
        /// <param name="cargoAmount">The cargo amount from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current cargo state to match planned</param>
        public void UpdateFromFlightPlan(int cargoAmount, bool forceCurrentUpdate = false)
        {
            try
            {
                _cargoPlanned = cargoAmount;
                
                // Log the updated cargo amount
                Logger.Log(LogLevel.Debug, "ProsimCargoService:UpdateFromFlightPlan", 
                    $"Updated cargo amount to {cargoAmount}");
                
                // If requested, also update current cargo state to match planned
                if (forceCurrentUpdate)
                {
                    _cargoCurrentPercentage = 100;
                    ChangeCargo(100);
                    OnCargoStateChanged("UpdatedFromFlightPlan", _cargoCurrentPercentage, _cargoPlanned);
                }
                else
                {
                    // Initialize cargo with zero loading
                    ChangeCargo(0);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimCargoService:UpdateFromFlightPlan", 
                    $"Error updating from flight plan: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Changes the cargo amount to the specified percentage of the planned amount
        /// </summary>
        /// <param name="percentage">The percentage of planned cargo to load (0-100)</param>
        public void ChangeCargo(int percentage)
        {
            try
            {
                // Validate percentage is within range
                if (percentage < 0 || percentage > 100)
                {
                    Logger.Log(LogLevel.Warning, "ProsimCargoService:ChangeCargo", 
                        $"Invalid percentage value: {percentage}. Must be between 0 and 100.");
                    percentage = Math.Clamp(percentage, 0, 100);
                }
                
                // If no change, return early
                if (percentage == _cargoCurrentPercentage)
                {
                    return;
                }
                
                // Calculate actual cargo amount based on percentage
                float cargo = (float)_cargoPlanned * (float)(percentage / 100.0f);
                
                // Distribute cargo between forward and aft compartments
                _prosimService.SetVariable("aircraft.cargo.forward.amount", (float)cargo * _cargoDistMain);
                _prosimService.SetVariable("aircraft.cargo.aft.amount", (float)cargo * _cargoDistMain);
                
                // Update current percentage
                _cargoCurrentPercentage = percentage;
                
                // Log the change
                Logger.Log(LogLevel.Debug, "ProsimCargoService:ChangeCargo", 
                    $"Cargo set to {percentage}% of {_cargoPlanned}: forward {cargo * _cargoDistMain} aft {cargo * _cargoDistMain}");
                
                // Raise event
                OnCargoStateChanged("ChangeCargo", _cargoCurrentPercentage, _cargoPlanned);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimCargoService:ChangeCargo", 
                    $"Error changing cargo: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the planned cargo amount
        /// </summary>
        /// <returns>The planned cargo amount</returns>
        public int GetCargoPlanned()
        {
            return _cargoPlanned;
        }
        
        /// <summary>
        /// Gets the current cargo amount as a percentage of planned
        /// </summary>
        /// <returns>The current cargo percentage (0-100)</returns>
        public int GetCargoCurrentPercentage()
        {
            return _cargoCurrentPercentage;
        }
        
        /// <summary>
        /// Raises the CargoStateChanged event
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentPercentage">The current cargo percentage</param>
        /// <param name="plannedAmount">The planned cargo amount</param>
        protected virtual void OnCargoStateChanged(string operationType, int currentPercentage, int plannedAmount)
        {
            CargoStateChanged?.Invoke(this, new CargoStateChangedEventArgs(operationType, currentPercentage, plannedAmount));
        }
    }
}
