using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using Prosim2GSX.Models;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class RefuelingService : IRefuelingService
    {
        private readonly IProsimInterface _prosimService;
        private readonly ServiceModel _model;

        public double CurrentFuel { get; private set; } = 0;
        public double PlannedFuel { get; private set; } = 0;
        public double TargetFuel { get; private set; } = 0;
        public string FuelUnits { get; private set; } = "KG";

        private static readonly float _weightConversion = 2.205f;

        public RefuelingService(IProsimInterface prosimService, ServiceModel model)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void UpdateFuelData(FlightPlan flightPlan)
        {
            try
            {
                CurrentFuel = _prosimService.GetProsimVariable("aircraft.fuel.total.amount.kg");
                FuelUnits = _prosimService.GetProsimVariable("system.config.Units.Weight");

                if (_model.FlightPlanType == "MCDU")
                {
                    if (flightPlan != null)
                    {
                        PlannedFuel = flightPlan.Fuel;
                    }
                }
                else
                {
                    PlannedFuel = _prosimService.GetProsimVariable("aircraft.refuel.fuelTarget");
                }

                // Convert if needed
                if (FuelUnits == "LBS")
                    PlannedFuel /= _weightConversion;

                Logger.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"Updated fuel data - Current: {CurrentFuel}kg, Planned: {PlannedFuel}kg, Units: {FuelUnits}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Exception during UpdateFuelData: {ex.Message}");
            }
        }

        public double GetFuelAmount()
        {
            try
            {
                double fuelAmount = _prosimService.GetProsimVariable("aircraft.fuel.total.amount.kg");
                return fuelAmount;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error getting fuel amount: {ex.Message}");
                return 0;
            }
        }

        public void SetInitialFuel()
        {
            try
            {
                if (_model.SetZeroFuel)
                {
                    Logger.Log(LogLevel.Information, nameof(RefuelingService),
                        "Start at Zero Fuel amount - Resetting to 0kg (0lbs)");
                    _prosimService.SetProsimVariable("aircraft.fuel.total.amount.kg", 0.0D);
                    CurrentFuel = 0D;
                }
                else if (_model.SetSaveFuel)
                {
                    Logger.Log(LogLevel.Information, nameof(RefuelingService),
                        $"Using saved fuel value - Resetting to {_model.SavedFuelAmount}");
                    _prosimService.SetProsimVariable("aircraft.fuel.total.amount.kg", _model.SavedFuelAmount);
                    CurrentFuel = _model.SavedFuelAmount;
                }
                else if (CurrentFuel > PlannedFuel)
                {
                    Logger.Log(LogLevel.Information, nameof(RefuelingService),
                        "Current Fuel higher than planned - Resetting to 1500kg (3307lbs)");
                    _prosimService.SetProsimVariable("aircraft.fuel.total.amount.kg", 1500.0D);
                    CurrentFuel = 1500D;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error setting initial fuel: {ex.Message}");
            }
        }

        public void SetInitialFluids()
        {
            try
            {
                _prosimService.SetProsimVariable("aircraft.hydraulics.blue.quantity", _model.HydaulicsBlueAmount);
                _prosimService.SetProsimVariable("aircraft.hydraulics.green.quantity", _model.HydaulicsGreenAmount);
                _prosimService.SetProsimVariable("aircraft.hydraulics.yellow.quantity", _model.HydaulicsYellowAmount);

                Logger.Log(LogLevel.Debug, nameof(RefuelingService),
                    "Initial hydraulic fluids set successfully");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error setting initial fluids: {ex.Message}");
            }
        }

        public (double, double, double) GetHydraulicFluidValues()
        {
            try
            {
                _model.HydaulicsBlueAmount = _prosimService.GetProsimVariable("aircraft.hydraulics.blue.quantity");
                _model.HydaulicsGreenAmount = _prosimService.GetProsimVariable("aircraft.hydraulics.green.quantity");
                _model.HydaulicsYellowAmount = _prosimService.GetProsimVariable("aircraft.hydraulics.yellow.quantity");

                return (_model.HydaulicsBlueAmount, _model.HydaulicsGreenAmount, _model.HydaulicsYellowAmount);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error getting hydraulic fluid values: {ex.Message}");
                return (0, 0, 0);
            }
        }

        public void StartRefueling()
        {
            try
            {
                // Initialize refueling with power off initially
                _prosimService.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);
                _prosimService.SetProsimVariable("aircraft.refuel.refuelingPower", false);

                // Round up planned fuel to the nearest 100
                TargetFuel = Math.Ceiling(PlannedFuel / 100.0) * 100.0;
                Logger.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"Rounding fuel from {PlannedFuel} to {TargetFuel}");

                if (FuelUnits == "KG")
                    _prosimService.SetProsimVariable("aircraft.refuel.fuelTarget", TargetFuel);
                else
                    _prosimService.SetProsimVariable("aircraft.refuel.fuelTarget", TargetFuel * _weightConversion);

                Logger.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"Fuel target set to {TargetFuel} kg. Current fuel: {CurrentFuel} kg");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error starting refueling: {ex.Message}");
            }
        }

        public bool ProcessRefueling()
        {
            try
            {
                float step = _model.GetFuelRateKGS();

                Logger.Log(LogLevel.Debug, nameof(RefuelingService),
                    $"Refueling step: Current={CurrentFuel}, Target={TargetFuel}, Step={step}");

                if (CurrentFuel + step < TargetFuel)
                {
                    CurrentFuel += step;
                    Logger.Log(LogLevel.Debug, nameof(RefuelingService),
                        $"Refueling in progress: {CurrentFuel}/{TargetFuel} kg");
                }
                else
                {
                    CurrentFuel = TargetFuel;
                    Logger.Log(LogLevel.Information, nameof(RefuelingService),
                        $"Refueling complete: {CurrentFuel}/{TargetFuel} kg");
                }

                _prosimService.SetProsimVariable("aircraft.fuel.total.amount.kg", CurrentFuel);

                return Math.Abs(CurrentFuel - TargetFuel) < 1.0;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error during refueling process: {ex.Message}");
                return false;
            }
        }

        public void StopRefueling()
        {
            try
            {
                Logger.Log(LogLevel.Information, nameof(RefuelingService), "Refueling stopped");
                _prosimService.SetProsimVariable("aircraft.refuel.refuelingPower", false);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error stopping refueling: {ex.Message}");
            }
        }

        public void PauseRefueling()
        {
            try
            {
                Logger.Log(LogLevel.Information, nameof(RefuelingService), "Refueling paused - hose disconnected");
                // Turn off power when hose disconnected
                _prosimService.SetProsimVariable("aircraft.refuel.refuelingPower", false);
                _prosimService.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error pausing refueling: {ex.Message}");
            }
        }

        public void ResumeRefueling()
        {
            try
            {
                Logger.Log(LogLevel.Information, nameof(RefuelingService), "Refueling resumed - hose connected");
                // Turn on power when hose connected
                _prosimService.SetProsimVariable("aircraft.refuel.refuelingPower", true);
                _prosimService.SetProsimVariable("aircraft.refuel.refuelingRate", _model.GetFuelRateKGS());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(RefuelingService),
                    $"Error resuming refueling: {ex.Message}");
            }
        }
    }
}