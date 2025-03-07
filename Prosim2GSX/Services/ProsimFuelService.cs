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
        /// Updates fuel data from a flight plan
        /// </summary>
        /// <param name="plannedFuel">The planned fuel amount from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current fuel state to match planned</param>
        public void UpdateFromFlightPlan(double plannedFuel, bool forceCurrentUpdate = false)
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
                    Logger.Log(LogLevel.Debug, "ProsimFuelService:UpdateFromFlightPlan", 
                        $"Updated planned fuel amount to {_fuelPlanned} kg");
                }
                
                // If requested, also update current fuel state to match planned
                if (forceCurrentUpdate)
                {
                    _fuelCurrent = _fuelPlanned;
                    _prosimService.SetVariable("aircraft.fuel.total.amount.kg", _fuelCurrent);
                    OnFuelStateChanged("UpdatedFromFlightPlan", _fuelCurrent, _fuelPlanned);
                }
                
                // Read current fuel amount
                _fuelCurrent = _prosimService.ReadDataRef("aircraft.fuel.total.amount.kg");
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
        /// Starts the refueling process
        /// </summary>
        public void RefuelStart()
        {
            try
            {
                _prosimService.SetVariable("aircraft.refuel.refuelingRate", 0.0D);
                _prosimService.SetVariable("aircraft.refuel.refuelingPower", true);
                
                // Round up planned fuel to the nearest 100
                double roundedFuelPlanned = Math.Ceiling(_fuelPlanned / 100.0) * 100.0;
                Logger.Log(LogLevel.Debug, "ProsimFuelService:RefuelStart", 
                    $"Rounding fuel from {_fuelPlanned} to {roundedFuelPlanned}");
                
                if (_fuelUnits == "KG")
                    _prosimService.SetVariable("aircraft.refuel.fuelTarget", roundedFuelPlanned);
                else
                    _prosimService.SetVariable("aircraft.refuel.fuelTarget", WeightConversionUtility.KgToLbs(roundedFuelPlanned));
                
                // Update the fuelPlanned value to the rounded value
                _fuelPlanned = roundedFuelPlanned;
                
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
                float step = GetFuelRateKGS();
                
                if (_fuelCurrent + step < _fuelPlanned)
                    _fuelCurrent += step;
                else
                    _fuelCurrent = _fuelPlanned;
                
                _prosimService.SetVariable("aircraft.fuel.total.amount.kg", _fuelCurrent);
                
                bool isComplete = Math.Abs(_fuelCurrent - _fuelPlanned) < 1.0; // Allow for small floating point differences
                
                OnFuelStateChanged("Refuel", _fuelCurrent, _fuelPlanned);
                
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
                Logger.Log(LogLevel.Information, "ProsimFuelService:RefuelStop", $"RefuelStop Requested");
                
                _prosimService.SetVariable("aircraft.refuel.refuelingPower", false);
                
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
