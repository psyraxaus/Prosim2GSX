using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class CargoService : ICargoService
    {
        private readonly IProsimInterface _prosimService;
        private readonly ServiceModel _model;

        private const float _cargoDistMain = 4000.0f / 9440.0f;
        private const float _cargoDistBulk = 1440.0f / 9440.0f;
        private int _cargoLast;

        public int PlannedCargo { get; private set; }

        public CargoService(IProsimInterface prosimService, ServiceModel model)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void UpdateCargoData(FlightPlan flightPlan)
        {
            try
            {
                if (_model.FlightPlanType == "MCDU")
                {
                    if (flightPlan != null)
                    {
                        PlannedCargo = flightPlan.CargoTotal;

                        _prosimService.SetProsimVariable("aircraft.cargo.aft.amount", Convert.ToDouble(flightPlan.CargoTotal / 2));
                        _prosimService.SetProsimVariable("aircraft.cargo.forward.amount", Convert.ToDouble(flightPlan.CargoTotal / 2));

                        LogService.Log(LogLevel.Debug, nameof(CargoService),
                            $"Temp Cargo set: forward {_prosimService.GetProsimVariable("aircraft.cargo.forward.amount")} " +
                            $"aft {_prosimService.GetProsimVariable("aircraft.cargo.aft.amount")}", LogCategory.Cargo);
                    }
                }
/*
                else
                {
                    string str = (string)_prosimService.GetProsimVariable("efb.loading");
                    if (!string.IsNullOrWhiteSpace(str))
                        int.TryParse(str[1..], out PlannedCargo);
                }
*/
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(CargoService),
                    $"Exception during UpdateCargoData: {ex.Message}");
            }
        }

        public void UpdateCargoLoading(int cargoPercentage)
        {
            if (cargoPercentage == _cargoLast)
                return;

            _cargoLast = cargoPercentage;
            float cargo = (float)PlannedCargo * (float)(cargoPercentage / 100.0f);

            _prosimService.SetProsimVariable("aircraft.cargo.forward.amount", (float)cargo * _cargoDistMain);
            _prosimService.SetProsimVariable("aircraft.cargo.aft.amount", (float)cargo * _cargoDistMain);

            LogService.Log(LogLevel.Debug, nameof(CargoService),
                $"Cargo updated: {cargoPercentage}% of {PlannedCargo}kg", LogCategory.Cargo);
        }
    }
}