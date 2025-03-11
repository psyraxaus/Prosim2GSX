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
        
        /// <summary>
        /// Updates planned fuel data from a flight plan without changing the current fuel amount
        /// </summary>
        /// <param name="plannedFuel">The planned fuel amount from the flight plan</param>
        public void UpdatePlannedFuel(double plannedFuel)
        {
            try
            {
                double previousPlanned = _fuelPlanned;
                _fuelPlanned = plannedFuel;
                
                // Update fuel units from ProSim
                _fuelUnits = _prosimService.ReadDataRef("system.config.Units.Weight");
                
                // Convert if necessary
                if (_fuelUnits == "LBS")
                    _fuelPlanned = WeightConversionUtility.LbsToKg(_fuelPlanned);
                
                // Only log if the value has changed significantly (more than 0.1 kg)
                if (Math.Abs(_fuelPlanned - previousPlanned) > 0.1)
                {
                    Logger.Log(LogLevel.Debug, "ProsimFuelService:UpdatePlannedFuel", 
                        $"Updated planned fuel amount to {_fuelPlanned} kg");
                }
                
                // Read current fuel amount (but don't change it)
                _fuelCurrent = _prosimService.ReadDataRef("aircraft.fuel.total.amount.kg");
                
                OnFuelStateChanged("UpdatedPlannedFuel", _fuelCurrent, _fuelPlanned);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:UpdatePlannedFuel", 
                    $"Error updating planned fuel: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Sets the current fuel amount to the specified value
        /// </summary>
        /// <param name="fuelAmount">The fuel amount to set</param>
        public void SetCurrentFuel(double fuelAmount)
        {
            try
            {
                _fuelCurrent = fuelAmount;
                _prosimService.SetVariable("aircraft.fuel.total.amount.kg", _fuelCurrent);
                
                Logger.Log(LogLevel.Debug, "ProsimFuelService:SetCurrentFuel", 
                    $"Set current fuel amount to {_fuelCurrent} kg");
                
                OnFuelStateChanged("SetCurrentFuel", _fuelCurrent, _fuelPlanned);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:SetCurrentFuel", 
                    $"Error setting current fuel: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Updates fuel data from a flight plan
        /// </summary>
        /// <param name="plannedFuel">The planned fuel amount from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current fuel state to match planned</param>
        public void UpdateFromFlightPlan(double plannedFuel, bool forceCurrentUpdate = false)
        {
            try
            {
                // Update the planned fuel amount
                UpdatePlannedFuel(plannedFuel);
                
                // If requested, also update current fuel state to match planned
                if (forceCurrentUpdate)
                {
                    SetCurrentFuel(_fuelPlanned);
                }
                
                OnFuelStateChanged("UpdatedFromFlightPlan", _fuelCurrent, _fuelPlanned);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:UpdateFromFlightPlan", 
                    $"Error updating from flight plan: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Sets the initial fuel based on configuration settings
        /// </summary>
        public void SetInitialFuel()
        {
            try
            {
                bool useZeroFuel = _model.SetZeroFuel;
                
                if (useZeroFuel)
                {
                    Logger.Log(LogLevel.Information, "ProsimFuelService:SetInitialFuel", 
                        $"Start at Zero Fuel amount - Resetting to 0kg (0lbs)");
                    _prosimService.SetVariable("aircraft.fuel.total.amount.kg", 0.0D);
                    _fuelCurrent = 0D;
                }
                else if (_model.SetSaveFuel)
                {
                    Logger.Log(LogLevel.Information, "ProsimFuelService:SetInitialFuel", 
                        $"Using saved fuel value - Resetting to {_model.SavedFuelAmount}");
                    _prosimService.SetVariable("aircraft.fuel.total.amount.kg", _model.SavedFuelAmount);
                    _fuelCurrent = _model.SavedFuelAmount;
                }
                else if (_fuelCurrent > _fuelPlanned)
                {
                    Logger.Log(LogLevel.Information, "ProsimFuelService:SetInitialFuel", 
                        $"Current Fuel higher than planned - Resetting to 1500kg (3307lbs)");
                    _prosimService.SetVariable("aircraft.fuel.total.amount.kg", 1500.0D);
                    _fuelCurrent = 1500D;
                }
                
                OnFuelStateChanged("SetInitialFuel", _fuelCurrent, _fuelPlanned);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:SetInitialFuel", 
                    $"Error setting initial fuel: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Prepares the refueling process by setting up the target fuel amount
        /// </summary>
        public void PrepareRefueling()
        {
            try
            {
                _prosimService.SetVariable("aircraft.refuel.refuelingRate", 0.0D);
                
                // Round up planned fuel to the nearest 100
                double roundedFuelPlanned = Math.Ceiling(_fuelPlanned / 100.0) * 100.0;
                Logger.Log(LogLevel.Debug, "ProsimFuelService:PrepareRefueling", 
                    $"Rounding fuel from {_fuelPlanned} to {roundedFuelPlanned}");
                
                if (_fuelUnits == "KG")
                    _prosimService.SetVariable("aircraft.refuel.fuelTarget", roundedFuelPlanned);
                else
                    _prosimService.SetVariable("aircraft.refuel.fuelTarget", WeightConversionUtility.KgToLbs(roundedFuelPlanned));
                
                // Update the fuelPlanned value to the rounded value
                _fuelPlanned = roundedFuelPlanned;
                
                OnFuelStateChanged("PrepareRefueling", _fuelCurrent, _fuelPlanned);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:PrepareRefueling", 
                    $"Error preparing refueling: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Starts the fuel transfer by setting refuelingPower to true
        /// </summary>
        public void StartFuelTransfer()
        {
            try
            {
                // Get current fuel amount before starting transfer
                _fuelCurrent = _prosimService.ReadDataRef("aircraft.fuel.total.amount.kg");
                
                Logger.Log(LogLevel.Information, "ProsimFuelService:StartFuelTransfer", 
                    $"Starting fuel transfer with refuelingPower=true. Current: {_fuelCurrent} kg, Target: {_fuelPlanned} kg");
                
                // Set refueling power to true to start the actual fuel transfer
                _prosimService.SetVariable("aircraft.refuel.refuelingPower", true);
                
                // Verify the refueling power was set
                bool refuelingPower = Convert.ToBoolean(_prosimService.ReadDataRef("aircraft.refuel.refuelingPower"));
                Logger.Log(LogLevel.Debug, "ProsimFuelService:StartFuelTransfer", 
                    $"Refueling power state after setting: {refuelingPower}");
                
                OnFuelStateChanged("StartFuelTransfer", _fuelCurrent, _fuelPlanned);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:StartFuelTransfer", 
                    $"Error starting fuel transfer: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Starts the refueling process (only prepares for refueling, doesn't start fuel transfer)
        /// </summary>
        public void RefuelStart()
        {
            try
            {
                // Only prepare for refueling, don't start fuel transfer yet
                // The fuel transfer will be started by StartFuelTransfer() when the hose is connected
                PrepareRefueling();
                
                Logger.Log(LogLevel.Information, "ProsimFuelService:RefuelStart", 
                    $"Prepared for refueling. Target: {_fuelPlanned} {_fuelUnits}, Current: {_fuelCurrent} {_fuelUnits}");
                
                OnFuelStateChanged("RefuelStart", _fuelCurrent, _fuelPlanned);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:RefuelStart", 
                    $"Error starting refueling: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Continues the refueling process
        /// </summary>
        /// <returns>True if refueling is complete, false otherwise</returns>
        public bool Refuel()
        {
            try
            {
                // Check if refueling power is active
                bool refuelingPower = Convert.ToBoolean(_prosimService.ReadDataRef("aircraft.refuel.refuelingPower"));
                
                if (!refuelingPower)
                {
                    Logger.Log(LogLevel.Warning, "ProsimFuelService:Refuel", 
                        "Refueling power is not active, cannot transfer fuel");
                    return false;
                }
                
                // Get current fuel amount
                _fuelCurrent = _prosimService.ReadDataRef("aircraft.fuel.total.amount.kg");
                
                // Calculate fuel step based on rate
                float step = GetFuelRateKGS();
                
                // Calculate new fuel amount
                double newFuelAmount;
                if (_fuelCurrent + step < _fuelPlanned)
                    newFuelAmount = _fuelCurrent + step;
                else
                    newFuelAmount = _fuelPlanned;
                
                // Only update if there's a meaningful change
                if (Math.Abs(newFuelAmount - _fuelCurrent) > 0.1)
                {
                    _fuelCurrent = newFuelAmount;
                    _prosimService.SetVariable("aircraft.fuel.total.amount.kg", _fuelCurrent);
                    
                    Logger.Log(LogLevel.Debug, "ProsimFuelService:Refuel", 
                        $"Transferred fuel: Current: {_fuelCurrent} kg, Target: {_fuelPlanned} kg");
                    
                    OnFuelStateChanged("Refuel", _fuelCurrent, _fuelPlanned);
                }
                
                // Check if refueling is complete
                bool isComplete = Math.Abs(_fuelCurrent - _fuelPlanned) < 1.0; // Allow for small floating point differences
                
                if (isComplete)
                {
                    Logger.Log(LogLevel.Information, "ProsimFuelService:Refuel", 
                        $"Refueling complete. Final amount: {_fuelCurrent} kg");
                }
                
                return isComplete;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:Refuel", 
                    $"Error during refueling: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Stops the refueling process
        /// </summary>
        public void RefuelStop()
        {
            try
            {
                // Get current fuel amount
                _fuelCurrent = _prosimService.ReadDataRef("aircraft.fuel.total.amount.kg");
                
                // Calculate completion percentage
                double completionPercentage = 0;
                if (_fuelPlanned > 0)
                {
                    completionPercentage = (_fuelCurrent / _fuelPlanned) * 100;
                    completionPercentage = Math.Min(100, completionPercentage);
                }
                
                Logger.Log(LogLevel.Information, "ProsimFuelService:RefuelStop", 
                    $"Stopping refueling. Current: {_fuelCurrent} kg, Target: {_fuelPlanned} kg, Completion: {completionPercentage:F1}%");
                
                // Check if refueling is complete (within 1% of target)
                bool isComplete = Math.Abs(_fuelCurrent - _fuelPlanned) < (_fuelPlanned * 0.01);
                
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
                _prosimService.SetVariable("aircraft.refuel.refuelingPower", false);
                
                // Verify the refueling power was set to false
                bool refuelingPower = Convert.ToBoolean(_prosimService.ReadDataRef("aircraft.refuel.refuelingPower"));
                Logger.Log(LogLevel.Debug, "ProsimFuelService:RefuelStop", 
                    $"Refueling power state after stopping: {refuelingPower}");
                
                OnFuelStateChanged("RefuelStop", _fuelCurrent, _fuelPlanned);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:RefuelStop", 
                    $"Error stopping refueling: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        /// <returns>The current fuel amount in kg</returns>
        public double GetFuelAmount()
        {
            try
            {
                _fuelCurrent = _prosimService.ReadDataRef("aircraft.fuel.total.amount.kg");
                return _fuelCurrent;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:GetFuelAmount", 
                    $"Error getting fuel amount: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the fuel rate in kg/s
        /// </summary>
        /// <returns>The fuel rate in kg/s</returns>
        public float GetFuelRateKGS()
        {
            try
            {
                if (_model.RefuelUnit == "KGS")
                    return _model.RefuelRate;
                else
                    return (float)WeightConversionUtility.LbsToKg(_model.RefuelRate);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFuelService:GetFuelRateKGS", 
                    $"Error getting fuel rate: {ex.Message}");
                throw;
            }
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
        
        /// <summary>
        /// Raises the FuelStateChanged event
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentAmount">The current fuel amount</param>
        /// <param name="plannedAmount">The planned fuel amount</param>
        protected virtual void OnFuelStateChanged(string operationType, double currentAmount, double plannedAmount)
        {
            FuelStateChanged?.Invoke(this, new FuelStateChangedEventArgs(operationType, currentAmount, plannedAmount, _fuelUnits));
        }
    }
}
