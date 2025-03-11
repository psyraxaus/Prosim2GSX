using System;
using Prosim2GSX.Models;
using Prosim2GSX.Utilities;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for managing fuel operations in ProSim
    /// </summary>
    public class ProsimFuelService : IProsimFuelService
    {
        // Constants
        private const string FUEL_AMOUNT_DATAREF = "aircraft.fuel.total.amount.kg";
        private const string FUEL_UNITS_DATAREF = "system.config.Units.Weight";
        private const string REFUELING_POWER_DATAREF = "aircraft.refuel.refuelingPower";
        private const string REFUELING_RATE_DATAREF = "aircraft.refuel.refuelingRate";
        private const string FUEL_TARGET_DATAREF = "aircraft.refuel.fuelTarget";
        private const double SIGNIFICANT_CHANGE_THRESHOLD = 0.1;
        private const double REFUELING_COMPLETE_THRESHOLD = 1.0;
        
        // Private fields
        private readonly IProsimService _prosimService;
        private readonly ServiceModel _model;
        private double _fuelCurrent;
        private double _fuelPlanned;
        private string _fuelUnits;
        
        /// <summary>
        /// Gets the planned fuel amount in kg
        /// </summary>
        public double FuelPlanned => _fuelPlanned;
        
        /// <summary>
        /// Gets the current fuel amount in kg
        /// </summary>
        public double FuelCurrent => _fuelCurrent;
        
        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        public string FuelUnits => _fuelUnits;
        
        /// <summary>
        /// Event raised when fuel state changes
        /// </summary>
        public event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;
        
        /// <summary>
        /// Creates a new instance of ProsimFuelService
        /// </summary>
        /// <param name="prosimService">The ProSim service to use for communication with ProSim</param>
        /// <param name="model">The service model containing configuration settings</param>
        /// <exception cref="ArgumentNullException">Thrown if prosimService or model is null</exception>
        public ProsimFuelService(IProsimService prosimService, ServiceModel model)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _fuelCurrent = 0;
            _fuelPlanned = 0;
            _fuelUnits = "KG";
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Executes an action with standardized error handling
        /// </summary>
        private T ExecuteWithErrorHandling<T>(string methodName, Func<T> action, string errorMessage = null)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                string logMessage = errorMessage ?? $"Error in {methodName}";
                Logger.Log(LogLevel.Error, $"ProsimFuelService:{methodName}", $"{logMessage}: {ex.Message}");
                throw; // Preserve original exception for proper error propagation
            }
        }
        
        /// <summary>
        /// Executes an action with standardized error handling (void version)
        /// </summary>
        private void ExecuteWithErrorHandling(string methodName, Action action, string errorMessage = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                string logMessage = errorMessage ?? $"Error in {methodName}";
                Logger.Log(LogLevel.Error, $"ProsimFuelService:{methodName}", $"{logMessage}: {ex.Message}");
                throw; // Preserve original exception for proper error propagation
            }
        }
        
        /// <summary>
        /// Reads a value from ProSim with error handling
        /// </summary>
        private T ReadProSimValue<T>(string dataRef, string methodName)
        {
            try
            {
                var value = _prosimService.ReadDataRef(dataRef);
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"ProsimFuelService:{methodName}", 
                    $"Error reading ProSim variable {dataRef}: {ex.Message}");
                throw; // Rethrow to maintain error propagation
            }
        }
        
        /// <summary>
        /// Sets a value in ProSim with error handling
        /// </summary>
        private void SetProSimValue<T>(string dataRef, T value, string methodName)
        {
            try
            {
                _prosimService.SetVariable(dataRef, value);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"ProsimFuelService:{methodName}", 
                    $"Error setting ProSim variable {dataRef} to {value}: {ex.Message}");
                throw; // Rethrow to maintain error propagation
            }
        }
        
        /// <summary>
        /// Updates fuel state and raises the appropriate event
        /// </summary>
        private void UpdateFuelState(
            string operationType, 
            double? newCurrentFuel = null, 
            double? newPlannedFuel = null,
            bool logStateChange = true)
        {
            bool stateChanged = false;
            double previousCurrent = _fuelCurrent;
            double previousPlanned = _fuelPlanned;
            
            // Update current fuel if provided
            if (newCurrentFuel.HasValue)
            {
                _fuelCurrent = newCurrentFuel.Value;
                stateChanged = stateChanged || Math.Abs(_fuelCurrent - previousCurrent) > SIGNIFICANT_CHANGE_THRESHOLD;
            }
            
            // Update planned fuel if provided
            if (newPlannedFuel.HasValue)
            {
                _fuelPlanned = newPlannedFuel.Value;
                stateChanged = stateChanged || Math.Abs(_fuelPlanned - previousPlanned) > SIGNIFICANT_CHANGE_THRESHOLD;
            }
            
            // Log significant changes
            if (logStateChange && stateChanged)
            {
                Logger.Log(LogLevel.Debug, $"ProsimFuelService:{operationType}", 
                    $"Fuel state updated - Current: {_fuelCurrent} kg, Planned: {_fuelPlanned} kg");
            }
            
            // Always raise the event to maintain compatibility with existing code
            OnFuelStateChanged(operationType, _fuelCurrent, _fuelPlanned);
        }
        
        /// <summary>
        /// Reads the current fuel amount from ProSim
        /// </summary>
        private double ReadCurrentFuel(string methodName)
        {
            return ReadProSimValue<double>(FUEL_AMOUNT_DATAREF, methodName);
        }
        
        /// <summary>
        /// Updates the fuel units from ProSim
        /// </summary>
        private void UpdateFuelUnits(string methodName)
        {
            _fuelUnits = ReadProSimValue<string>(FUEL_UNITS_DATAREF, methodName);
        }
        
        /// <summary>
        /// Raises the FuelStateChanged event
        /// </summary>
        protected virtual void OnFuelStateChanged(string operationType, double currentAmount, double plannedAmount)
        {
            FuelStateChanged?.Invoke(this, new FuelStateChangedEventArgs(operationType, currentAmount, plannedAmount, _fuelUnits));
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Updates planned fuel data from a flight plan without changing the current fuel amount
        /// </summary>
        /// <param name="plannedFuel">The planned fuel amount from the flight plan</param>
        public void UpdatePlannedFuel(double plannedFuel)
        {
            ExecuteWithErrorHandling("UpdatePlannedFuel", () =>
            {
                // Update fuel units from ProSim
                UpdateFuelUnits("UpdatePlannedFuel");
                
                // Convert if necessary
                double convertedFuel = plannedFuel;
                if (_fuelUnits == "LBS")
                    convertedFuel = WeightConversionUtility.LbsToKg(convertedFuel);
                
                // Read current fuel amount (but don't change it)
                double currentFuel = ReadCurrentFuel("UpdatePlannedFuel");
                
                // Update state and raise event
                UpdateFuelState(
                    "UpdatedPlannedFuel", 
                    newCurrentFuel: currentFuel, 
                    newPlannedFuel: convertedFuel);
            });
        }
        
        /// <summary>
        /// Sets the current fuel amount to the specified value
        /// </summary>
        /// <param name="fuelAmount">The fuel amount to set</param>
        public void SetCurrentFuel(double fuelAmount)
        {
            ExecuteWithErrorHandling("SetCurrentFuel", () =>
            {
                // Set the fuel amount in ProSim
                SetProSimValue(FUEL_AMOUNT_DATAREF, fuelAmount, "SetCurrentFuel");
                
                // Update state and raise event
                UpdateFuelState("SetCurrentFuel", newCurrentFuel: fuelAmount);
            });
        }
        
        /// <summary>
        /// Updates fuel data from a flight plan
        /// </summary>
        /// <param name="plannedFuel">The planned fuel amount from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current fuel state to match planned</param>
        public void UpdateFromFlightPlan(double plannedFuel, bool forceCurrentUpdate = false)
        {
            ExecuteWithErrorHandling("UpdateFromFlightPlan", () =>
            {
                // Update the planned fuel amount
                UpdatePlannedFuel(plannedFuel);
                
                // If requested, also update current fuel state to match planned
                if (forceCurrentUpdate)
                {
                    SetCurrentFuel(_fuelPlanned);
                }
                
                // Raise event for the overall operation
                UpdateFuelState("UpdatedFromFlightPlan", logStateChange: false);
            });
        }
        
        /// <summary>
        /// Sets the initial fuel based on configuration settings
        /// </summary>
        public void SetInitialFuel()
        {
            ExecuteWithErrorHandling("SetInitialFuel", () =>
            {
                double fuelToSet = _fuelCurrent;
                string logMessage = null;
                
                if (_model.SetZeroFuel)
                {
                    fuelToSet = 0.0;
                    logMessage = $"Start at Zero Fuel amount - Resetting to 0kg (0lbs)";
                }
                else if (_model.SetSaveFuel)
                {
                    fuelToSet = _model.SavedFuelAmount;
                    logMessage = $"Using saved fuel value - Resetting to {fuelToSet}";
                }
                else if (_fuelCurrent > _fuelPlanned)
                {
                    fuelToSet = 1500.0;
                    logMessage = $"Current Fuel higher than planned - Resetting to 1500kg (3307lbs)";
                }
                
                if (logMessage != null)
                {
                    Logger.Log(LogLevel.Information, "ProsimFuelService:SetInitialFuel", logMessage);
                    SetProSimValue(FUEL_AMOUNT_DATAREF, fuelToSet, "SetInitialFuel");
                    UpdateFuelState("SetInitialFuel", newCurrentFuel: fuelToSet);
                }
                else
                {
                    // No changes needed, just raise the event
                    UpdateFuelState("SetInitialFuel", logStateChange: false);
                }
            });
        }
        
        /// <summary>
        /// Prepares the refueling process by setting up the target fuel amount
        /// </summary>
        public void PrepareRefueling()
        {
            ExecuteWithErrorHandling("PrepareRefueling", () =>
            {
                // Set refueling rate to 0
                SetProSimValue(REFUELING_RATE_DATAREF, 0.0, "PrepareRefueling");
                
                // Round up planned fuel to the nearest 100
                double roundedFuelPlanned = Math.Ceiling(_fuelPlanned / 100.0) * 100.0;
                Logger.Log(LogLevel.Debug, "ProsimFuelService:PrepareRefueling", 
                    $"Rounding fuel from {_fuelPlanned} to {roundedFuelPlanned}");
                
                // Set the fuel target in the appropriate units
                if (_fuelUnits == "KG")
                    SetProSimValue(FUEL_TARGET_DATAREF, roundedFuelPlanned, "PrepareRefueling");
                else
                    SetProSimValue(FUEL_TARGET_DATAREF, WeightConversionUtility.KgToLbs(roundedFuelPlanned), "PrepareRefueling");
                
                // Update the fuelPlanned value to the rounded value
                UpdateFuelState("PrepareRefueling", newPlannedFuel: roundedFuelPlanned);
            });
        }
        
        /// <summary>
        /// Starts the fuel transfer by setting refuelingPower to true
        /// </summary>
        public void StartFuelTransfer()
        {
            ExecuteWithErrorHandling("StartFuelTransfer", () =>
            {
                // Get current fuel amount before starting transfer
                double currentFuel = ReadCurrentFuel("StartFuelTransfer");
                
                Logger.Log(LogLevel.Information, "ProsimFuelService:StartFuelTransfer", 
                    $"Starting fuel transfer with refuelingPower=true. Current: {currentFuel} kg, Target: {_fuelPlanned} kg");
                
                // Set refueling power to true to start the actual fuel transfer
                SetProSimValue(REFUELING_POWER_DATAREF, true, "StartFuelTransfer");
                
                // Verify the refueling power was set
                bool refuelingPower = ReadProSimValue<bool>(REFUELING_POWER_DATAREF, "StartFuelTransfer");
                Logger.Log(LogLevel.Debug, "ProsimFuelService:StartFuelTransfer", 
                    $"Refueling power state after setting: {refuelingPower}");
                
                // Update state and raise event
                UpdateFuelState("StartFuelTransfer", newCurrentFuel: currentFuel);
            });
        }
        
        /// <summary>
        /// Starts the refueling process (only prepares for refueling, doesn't start fuel transfer)
        /// </summary>
        public void RefuelStart()
        {
            ExecuteWithErrorHandling("RefuelStart", () =>
            {
                // Only prepare for refueling, don't start fuel transfer yet
                // The fuel transfer will be started by StartFuelTransfer() when the hose is connected
                PrepareRefueling();
                
                Logger.Log(LogLevel.Information, "ProsimFuelService:RefuelStart", 
                    $"Prepared for refueling. Target: {_fuelPlanned} {_fuelUnits}, Current: {_fuelCurrent} {_fuelUnits}");
                
                // Raise event for the overall operation
                UpdateFuelState("RefuelStart", logStateChange: false);
            });
        }
        
        /// <summary>
        /// Continues the refueling process
        /// </summary>
        /// <returns>True if refueling is complete, false otherwise</returns>
        public bool Refuel()
        {
            return ExecuteWithErrorHandling("Refuel", () =>
            {
                // Check if refueling power is active
                bool refuelingPower = ReadProSimValue<bool>(REFUELING_POWER_DATAREF, "Refuel");
                
                if (!refuelingPower)
                {
                    Logger.Log(LogLevel.Warning, "ProsimFuelService:Refuel", 
                        "Refueling power is not active, cannot transfer fuel");
                    return false;
                }
                
                // Get current fuel amount
                double currentFuel = ReadCurrentFuel("Refuel");
                
                // Calculate fuel step based on rate
                float step = GetFuelRateKGS();
                
                // Calculate new fuel amount
                double newFuelAmount;
                if (currentFuel + step < _fuelPlanned)
                    newFuelAmount = currentFuel + step;
                else
                    newFuelAmount = _fuelPlanned;
                
                // Only update if there's a meaningful change
                if (Math.Abs(newFuelAmount - currentFuel) > SIGNIFICANT_CHANGE_THRESHOLD)
                {
                    SetProSimValue(FUEL_AMOUNT_DATAREF, newFuelAmount, "Refuel");
                    
                    Logger.Log(LogLevel.Debug, "ProsimFuelService:Refuel", 
                        $"Transferred fuel: Current: {newFuelAmount} kg, Target: {_fuelPlanned} kg");
                    
                    // Update state and raise event
                    UpdateFuelState("Refuel", newCurrentFuel: newFuelAmount);
                }
                else
                {
                    // Still update our internal state even if we didn't update ProSim
                    _fuelCurrent = currentFuel;
                }
                
                // Check if refueling is complete
                bool isComplete = Math.Abs(_fuelCurrent - _fuelPlanned) < REFUELING_COMPLETE_THRESHOLD;
                
                if (isComplete)
                {
                    Logger.Log(LogLevel.Information, "ProsimFuelService:Refuel", 
                        $"Refueling complete. Final amount: {_fuelCurrent} kg");
                }
                
                return isComplete;
            });
        }
        
        /// <summary>
        /// Stops the refueling process
        /// </summary>
        public void RefuelStop()
        {
            ExecuteWithErrorHandling("RefuelStop", () =>
            {
                // Get current fuel amount
                double currentFuel = ReadCurrentFuel("RefuelStop");
                
                // Calculate completion percentage
                double completionPercentage = 0;
                if (_fuelPlanned > 0)
                {
                    completionPercentage = (currentFuel / _fuelPlanned) * 100;
                    completionPercentage = Math.Min(100, completionPercentage);
                }
                
                Logger.Log(LogLevel.Information, "ProsimFuelService:RefuelStop", 
                    $"Stopping refueling. Current: {currentFuel} kg, Target: {_fuelPlanned} kg, Completion: {completionPercentage:F1}%");
                
                // Check if refueling is complete (within 1% of target)
                bool isComplete = Math.Abs(currentFuel - _fuelPlanned) < (_fuelPlanned * 0.01);
                
                if (isComplete)
                {
                    Logger.Log(LogLevel.Information, "ProsimFuelService:RefuelStop", 
                        "Refueling is complete - setting refueling power to false");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "ProsimFuelService:RefuelStop", 
                        "Refueling is not complete but stopping anyway - setting refueling power to false");
                }
                
                // Set refueling power to false to stop the fuel transfer
                SetProSimValue(REFUELING_POWER_DATAREF, false, "RefuelStop");
                
                // Verify the refueling power was set to false
                bool refuelingPower = ReadProSimValue<bool>(REFUELING_POWER_DATAREF, "RefuelStop");
                Logger.Log(LogLevel.Debug, "ProsimFuelService:RefuelStop", 
                    $"Refueling power state after stopping: {refuelingPower}");
                
                // Update state and raise event
                UpdateFuelState("RefuelStop", newCurrentFuel: currentFuel);
            });
        }
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        /// <returns>The current fuel amount in kg</returns>
        public double GetFuelAmount()
        {
            return ExecuteWithErrorHandling("GetFuelAmount", () =>
            {
                double currentFuel = ReadCurrentFuel("GetFuelAmount");
                _fuelCurrent = currentFuel; // Update internal state
                return currentFuel;
            });
        }
        
        /// <summary>
        /// Gets the fuel rate in kg/s
        /// </summary>
        /// <returns>The fuel rate in kg/s</returns>
        public float GetFuelRateKGS()
        {
            return ExecuteWithErrorHandling("GetFuelRateKGS", () =>
            {
                if (_model.RefuelUnit == "KGS")
                    return _model.RefuelRate;
                else
                    return (float)WeightConversionUtility.LbsToKg(_model.RefuelRate);
            });
        }
        
        /// <summary>
        /// Gets the planned fuel amount
        /// </summary>
        /// <returns>The planned fuel amount in kg</returns>
        public double GetFuelPlanned()
        {
            return _fuelPlanned;
        }
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        /// <returns>The current fuel amount in kg</returns>
        public double GetFuelCurrent()
        {
            return _fuelCurrent;
        }
        
        #endregion
    }
}
