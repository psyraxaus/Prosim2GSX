using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class RefuelingService : IRefuelingService
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly ServiceModel _model;

        private double _currentFuel = 0;
        private double _plannedFuel = 0;
        private double _targetFuel = 0;
        private string _fuelUnits = "KG";
        private bool _isRefuelingActive = false;
        private bool _isRefuelingComplete = false;

        private static readonly float _weightConversion = 2.205f;

        /// <inheritdoc/>
        public double CurrentFuel => _currentFuel;

        /// <inheritdoc/>
        public double PlannedFuel => _plannedFuel;

        /// <inheritdoc/>
        public string FuelUnits => _fuelUnits;

        /// <inheritdoc/>
        public bool IsRefuelingActive => _isRefuelingActive;

        /// <inheritdoc/>
        public bool IsRefuelingComplete => _isRefuelingComplete;

        public RefuelingService(IProsimInterface prosimInterface, ServiceModel model)
        {
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <inheritdoc/>
        public void UpdateFuelData(FlightPlan flightPlan)
        {
            try
            {
                // Get current fuel amount
                _currentFuel = GetFuelAmount();

                // Get fuel units
                _fuelUnits = Convert.ToString(_prosimInterface.GetProsimVariable("system.config.Units.Weight"));

                // Get planned fuel
                if (_model.FlightPlanType == "MCDU" && flightPlan != null)
                {
                    _plannedFuel = flightPlan.Fuel;
                }
                else
                {
                    var fuelTarget = _prosimInterface.GetProsimVariable("aircraft.refuel.fuelTarget");
                    _plannedFuel = fuelTarget != null ? Convert.ToDouble(fuelTarget) : 0;
                }

                // Convert if needed
                if (_fuelUnits == "LBS")
                    _plannedFuel /= _weightConversion;

                LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"Updated fuel data - Current: {_currentFuel}kg, Planned: {_plannedFuel}kg, Units: {_fuelUnits}", LogCategory.Refueling);
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Exception during UpdateFuelData: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public double GetFuelAmount()
        {
            try
            {
                double fuelAmount = Convert.ToDouble(_prosimInterface.GetProsimVariable("aircraft.fuel.total.amount.kg"));
                _currentFuel = fuelAmount; // Update internal state
                return fuelAmount;
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error getting fuel amount: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public void SetInitialFuel()
        {
            try
            {
                if (_model.SetZeroFuel)
                {
                    LogService.Log(LogLevel.Information, nameof(RefuelingService),
                        "Start at Zero Fuel amount - Resetting to 0kg (0lbs)");
                    _prosimInterface.SetProsimVariable("aircraft.fuel.total.amount.kg", 0.0D);
                    _currentFuel = 0D;
                }
                else if (_model.SetSaveFuel)
                {
                    LogService.Log(LogLevel.Information, nameof(RefuelingService),
                        $"Using saved fuel value - Resetting to {_model.SavedFuelAmount}");
                    _prosimInterface.SetProsimVariable("aircraft.fuel.total.amount.kg", _model.SavedFuelAmount);
                    _currentFuel = _model.SavedFuelAmount;
                }
                else if (_currentFuel > _plannedFuel && _plannedFuel > 0)
                {
                    LogService.Log(LogLevel.Information, nameof(RefuelingService),
                        "Current Fuel higher than planned - Resetting to 1500kg (3307lbs)");
                    _prosimInterface.SetProsimVariable("aircraft.fuel.total.amount.kg", 1500.0D);
                    _currentFuel = 1500D;
                }

                // Set target fuel
                if (_plannedFuel > 0)
                {
                    SetFuelTarget(_plannedFuel);
                }
                else
                {
                    // Use default if no planned fuel
                    SetFuelTarget(10000.0);
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error setting initial fuel: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void SetFuelTarget(double amount)
        {
            try
            {
                _targetFuel = amount;

                // Round up to nearest 100
                _targetFuel = Math.Ceiling(_targetFuel / 100.0) * 100.0;

                // Set the datarefs
                _prosimInterface.SetProsimVariable("aircraft.refuel.fuelTarget", _targetFuel);
                _prosimInterface.SetProsimVariable("efb.plannedfuel", _targetFuel);

                LogService.Log(LogLevel.Information, nameof(RefuelingService),
                    $"Fuel target set to {_targetFuel:F1}kg");
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error setting fuel target: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void SetInitialFluids()
        {
            try
            {
                _prosimInterface.SetProsimVariable("aircraft.hydraulics.blue.quantity", _model.HydaulicsBlueAmount);
                _prosimInterface.SetProsimVariable("aircraft.hydraulics.green.quantity", _model.HydaulicsGreenAmount);
                _prosimInterface.SetProsimVariable("aircraft.hydraulics.yellow.quantity", _model.HydaulicsYellowAmount);

                LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                    "Initial hydraulic fluids set successfully", LogCategory.Refueling);
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error setting initial fluids: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public (double, double, double) GetHydraulicFluidValues()
        {
            try
            {
                double blue = Convert.ToDouble(_prosimInterface.GetProsimVariable("aircraft.hydraulics.blue.quantity"));
                double green = Convert.ToDouble(_prosimInterface.GetProsimVariable("aircraft.hydraulics.green.quantity"));
                double yellow = Convert.ToDouble(_prosimInterface.GetProsimVariable("aircraft.hydraulics.yellow.quantity"));

                // Update model values
                _model.HydaulicsBlueAmount = blue;
                _model.HydaulicsGreenAmount = green;
                _model.HydaulicsYellowAmount = yellow;

                return (blue, green, yellow);
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error getting hydraulic fluid values: {ex.Message}");
                return (0, 0, 0);
            }
        }

        /// <inheritdoc/>
        public void StartRefueling()
        {
            try
            {
                // Get the current fuel amount
                _currentFuel = GetFuelAmount();

                // Get the target fuel amount if not set yet
                if (_targetFuel <= 0)
                {
                    if (_plannedFuel > 0)
                    {
                        _targetFuel = _plannedFuel;
                    }
                    else
                    {
                        // Default target if nothing else is available
                        _targetFuel = 10000.0;
                    }
                }

                // Initialize with power off - will be turned on when hose connects
                _prosimInterface.SetProsimVariable("aircraft.refuel.fuelTarget", _targetFuel);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);

                // Reset state
                _isRefuelingActive = false;
                _isRefuelingComplete = false;

                LogService.Log(LogLevel.Information, nameof(RefuelingService),
                    $"Refueling initialized. Current: {_currentFuel:F1}kg, Target: {_targetFuel:F1}kg");
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error starting refueling: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void StopRefueling()
        {
            try
            {
                // Turn off power
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);

                // Update state
                _isRefuelingActive = false;
                _isRefuelingComplete = true;

                // Log the completion
                LogService.Log(LogLevel.Information, nameof(RefuelingService),
                    $"Refueling completed. Final: {_currentFuel:F1}kg");

                // Publish event
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Completed));
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error stopping refueling: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void PauseRefueling()
        {
            try
            {
                // Turn off power
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);

                _isRefuelingActive = false;

                LogService.Log(LogLevel.Information, nameof(RefuelingService),
                    $"Refueling paused at {_currentFuel:F1}kg");

                // Publish event
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Waiting));
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error pausing refueling: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void ResumeRefueling()
        {
            try
            {
                // Turn on power with appropriate rate
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", true);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingRate", _model.GetFuelRateKGS());

                _isRefuelingActive = true;

                LogService.Log(LogLevel.Information, nameof(RefuelingService),
                    $"Refueling resumed. Current: {_currentFuel:F1}kg, Target: {_targetFuel:F1}kg");

                // Publish event
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Active));
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error resuming refueling: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public bool ProcessRefueling()
        {
            // If refueling is not active or already complete, just return the current state
            if (_isRefuelingComplete)
                return true;

            if (!_isRefuelingActive)
                return false;

            try
            {
                // Get the current fuel amount
                _currentFuel = GetFuelAmount();

                // Check if target has been reached
                if (Math.Abs(_currentFuel - _targetFuel) < 10.0)
                {
                    StopRefueling();
                    return true;
                }

                // Use the configured fuel rate from the model
                // _model.GetFuelRateKGS() returns kg/s, so we need to account for processing interval
                // Assuming this method is called approximately once per second
                double step = _model.GetFuelRateKGS();

                // Don't exceed target
                if (_currentFuel + step > _targetFuel)
                {
                    step = _targetFuel - _currentFuel;
                }

                // Add the fuel
                double newFuel = _currentFuel + step;
                _prosimInterface.SetProsimVariable("aircraft.fuel.total.amount.kg", newFuel);
                _currentFuel = newFuel;

                LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"Fuel incremented by {step:F1}kg: {_currentFuel - step:F1} -> {newFuel:F1}kg (Rate: {_model.GetFuelRateKGS():F1}kg/s)", LogCategory.Refueling);

                return false;
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error processing refueling: {ex.Message}");
                return false;
            }
        }
    }
}