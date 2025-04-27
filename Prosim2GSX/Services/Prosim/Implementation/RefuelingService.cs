using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.Prosim.Enums;
using System;
using System.Threading;

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
        private RefuelingState _refuelingState = RefuelingState.Inactive;
        private DateTime _lastFuelUpdate = DateTime.MinValue;
        private double _lastFuelValue = 0;
        private int _unchangedFuelCounter = 0;

        private static readonly float _weightConversion = 2.205f;

        /// <inheritdoc/>
        public double CurrentFuel => _currentFuel;

        /// <inheritdoc/>
        public double PlannedFuel => _plannedFuel;

        /// <inheritdoc/>
        public string FuelUnits => _fuelUnits;

        /// <inheritdoc/>
        public RefuelingState State => _refuelingState;

        /// <inheritdoc/>
        public bool IsRefuelingActive => _refuelingState == RefuelingState.Active;

        /// <inheritdoc/>
        public bool IsRefuelingComplete => _refuelingState == RefuelingState.Completed;

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
                _lastFuelValue = _currentFuel;

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
                    $"Updated fuel data - Current: {_currentFuel}kg, Planned: {_plannedFuel}kg, State: {_refuelingState}, Units: {_fuelUnits}", LogCategory.Refueling);
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

                // Reset state tracking
                _refuelingState = RefuelingState.Inactive;
                _lastFuelUpdate = DateTime.MinValue;
                _lastFuelValue = _currentFuel;
                _unchangedFuelCounter = 0;
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
                // Initialize with power off - will be turned on when hose connects
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingActive", false);

                // Round up planned fuel to the nearest 100
                _targetFuel = Math.Ceiling(_plannedFuel / 100.0) * 100.0;
                LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"Rounding fuel from {_plannedFuel} to {_targetFuel}", LogCategory.Refueling);

                if (_fuelUnits == "KG")
                    _prosimInterface.SetProsimVariable("aircraft.refuel.fuelTarget", _targetFuel);
                else
                    _prosimInterface.SetProsimVariable("aircraft.refuel.fuelTarget", _targetFuel * _weightConversion);

                LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"Fuel target set to {_targetFuel} kg. Current fuel: {_currentFuel} kg", LogCategory.Refueling);

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
                LogService.Log(LogLevel.Information, nameof(RefuelingService), $"RefuelStop Requested");
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingActive", false);

                // Log the completion
                LogService.Log(LogLevel.Information, nameof(RefuelingService),
                    $"Refueling completed. Final: {_currentFuel:F1}kg, State: {_refuelingState}");

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
                // Log previous state for debugging
                LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"PauseRefueling called. Previous state: {_refuelingState}", LogCategory.Refueling);

                // Only pause if active
                if (_refuelingState != RefuelingState.Active)
                {
                    LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                        $"PauseRefueling called but refueling is not active (state: {_refuelingState})",
                        LogCategory.Refueling);
                    return;
                }

                // Turn off power
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingActive", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);

                // Update state
                _refuelingState = RefuelingState.Paused;

                LogService.Log(LogLevel.Information, nameof(RefuelingService),
                    $"Refueling paused at {_currentFuel:F1}kg, State: {_refuelingState}");

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
                LogService.Log(LogLevel.Information, nameof(RefuelingService), $"Refueling resumed - hose connected");
                // Turn on power when hose connected
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", true);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingActive", true);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingRate", _model.GetFuelRateKGS());
                
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
            // Log entry state for debugging
            LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                $"ProcessRefueling called - State: {_refuelingState}, " +
                $"Current: {_currentFuel:F1}kg, Target: {_targetFuel:F1}kg",
                LogCategory.Refueling);

            try
            {
                float step = _model.GetFuelRateKGS();

                LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"Refueling step: Current={_currentFuel}, Target={_targetFuel}, Step={step}");

                if (_currentFuel + step < _targetFuel)
                {
                    _currentFuel += step;
                    LogService.Log(LogLevel.Debug, nameof(RefuelingService),
                        $"Refueling in progress: {_currentFuel}/{_targetFuel} kg");
                }
                else
                {
                    _currentFuel = _targetFuel;
                    LogService.Log(LogLevel.Information, nameof(RefuelingService),
                        $"Refueling complete: {_currentFuel}/{_targetFuel} kg");
                }

                _prosimInterface.SetProsimVariable("aircraft.fuel.total.amount.kg", _currentFuel);

                return Math.Abs(_currentFuel - _targetFuel) < 1.0;
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