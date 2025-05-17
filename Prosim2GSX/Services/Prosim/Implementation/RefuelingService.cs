using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Prosim.Enums;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Threading;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class RefuelingService : IRefuelingService
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly ServiceModel _model;
        private readonly ILogger<RefuelingService> _logger;

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

        public RefuelingService(
            ILogger<RefuelingService> logger,
            IProsimInterface prosimInterface,
            ServiceModel model)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                _logger.LogDebug("Updated fuel data - Current: {CurrentFuel}kg, Planned: {PlannedFuel}kg, State: {State}, Units: {Units}",
                    _currentFuel, _plannedFuel, _refuelingState, _fuelUnits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during UpdateFuelData");
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
                _logger.LogError(ex, "Error getting fuel amount");
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
                    _logger.LogInformation("Start at Zero Fuel amount - Resetting to 0kg (0lbs)");
                    _prosimInterface.SetProsimVariable("aircraft.fuel.total.amount.kg", 0.0D);
                    _currentFuel = 0D;
                }
                else if (_model.SetSaveFuel)
                {
                    _logger.LogInformation("Using saved fuel value - Resetting to {SavedFuel}", _model.SavedFuelAmount);
                    _prosimInterface.SetProsimVariable("aircraft.fuel.total.amount.kg", _model.SavedFuelAmount);
                    _currentFuel = _model.SavedFuelAmount;
                }
                else if (_currentFuel > _plannedFuel && _plannedFuel > 0)
                {
                    _logger.LogInformation("Current Fuel higher than planned - Resetting to 1500kg (3307lbs)");
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
                _logger.LogError(ex, "Error setting initial fuel");
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

                _logger.LogInformation("Fuel target set to {TargetFuel:F1}kg", _targetFuel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting fuel target");
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

                _logger.LogDebug("Initial hydraulic fluids set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting initial fluids");
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
                _logger.LogError(ex, "Error getting hydraulic fluid values");
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
                _logger.LogDebug("Rounding fuel from {PlannedFuel} to {TargetFuel}", _plannedFuel, _targetFuel);

                if (_fuelUnits == "KG")
                    _prosimInterface.SetProsimVariable("aircraft.refuel.fuelTarget", _targetFuel);
                else
                    _prosimInterface.SetProsimVariable("aircraft.refuel.fuelTarget", _targetFuel * _weightConversion);

                _logger.LogDebug("Fuel target set to {TargetFuel} kg. Current fuel: {CurrentFuel} kg", _targetFuel, _currentFuel);

                _logger.LogInformation("Refueling initialized. Current: {CurrentFuel:F1}kg, Target: {TargetFuel:F1}kg", _currentFuel, _targetFuel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting refueling");
            }
        }

        /// <inheritdoc/>
        public void StopRefueling()
        {
            try
            {
                _logger.LogInformation("RefuelStop Requested");
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingActive", false);

                // Log the completion
                _logger.LogInformation("Refueling completed. Final: {FinalFuel:F1}kg, State: {State}", _currentFuel, _refuelingState);

                // Publish event
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Completed));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping refueling");
            }
        }

        /// <inheritdoc/>
        public void PauseRefueling()
        {
            try
            {
                // Log previous state for debugging
                _logger.LogDebug("PauseRefueling called. Previous state: {State}", _refuelingState);

                // Only pause if active
                if (_refuelingState != RefuelingState.Active)
                {
                    _logger.LogDebug("PauseRefueling called but refueling is not active (state: {State})", _refuelingState);
                    return;
                }

                // Turn off power
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingActive", false);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);

                // Update state
                _refuelingState = RefuelingState.Paused;

                _logger.LogInformation("Refueling paused at {CurrentFuel:F1}kg, State: {State}", _currentFuel, _refuelingState);

                // Publish event
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Waiting));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing refueling");
            }
        }

        /// <inheritdoc/>
        public void ResumeRefueling()
        {
            try
            {
                _logger.LogInformation("Refueling resumed - hose connected");
                // Turn on power when hose connected
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingPower", true);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingActive", true);
                _prosimInterface.SetProsimVariable("aircraft.refuel.refuelingRate", _model.GetFuelRateKGS());

                // Publish event
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Active));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming refueling");
            }
        }

        /// <inheritdoc/>
        public bool ProcessRefueling()
        {
            // Log entry state for debugging
            _logger.LogDebug("ProcessRefueling called - State: {State}, Current: {CurrentFuel:F1}kg, Target: {TargetFuel:F1}kg",
                _refuelingState, _currentFuel, _targetFuel);

            try
            {
                float step = _model.GetFuelRateKGS();

                _logger.LogDebug("Refueling step: Current={CurrentFuel}, Target={TargetFuel}, Step={Step}",
                    _currentFuel, _targetFuel, step);

                if (_currentFuel + step < _targetFuel)
                {
                    _currentFuel += step;
                    _logger.LogDebug("Refueling in progress: {CurrentFuel}/{TargetFuel} kg", _currentFuel, _targetFuel);
                }
                else
                {
                    _currentFuel = _targetFuel;
                    _logger.LogInformation("Refueling complete: {CurrentFuel}/{TargetFuel} kg", _currentFuel, _targetFuel);
                }

                _prosimInterface.SetProsimVariable("aircraft.fuel.total.amount.kg", _currentFuel);

                return Math.Abs(_currentFuel - _targetFuel) < 1.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refueling");
                return false;
            }
        }
    }
}
