using Microsoft.Extensions.Logging;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class CargoService : ICargoService
    {
        private readonly IProsimInterface _prosimService;
        private readonly ServiceModel _model;
        private readonly ILogger<CargoService> _logger;

        private const float _cargoDistMain = 4000.0f / 9440.0f;
        private const float _cargoDistBulk = 1440.0f / 9440.0f;
        private int _cargoLast;
        private bool _isManuallyReset = false;

        public int PlannedCargo { get; private set; }

        public CargoService(
            ILogger<CargoService> logger,
            IProsimInterface prosimService,
            ServiceModel model)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            _logger.LogDebug("CargoService initialized");
        }

        /// <inheritdoc/>
        public void UpdateCargoData(FlightPlan flightPlan)
        {
            try
            {
                if (_isManuallyReset)
                {
                    _logger.LogDebug("Skipping cargo update - manually reset to empty");
                    return; // Skip update if manually reset
                }

                if (_model.FlightPlanType == "MCDU")
                {
                    if (flightPlan != null)
                    {
                        PlannedCargo = flightPlan.CargoTotal;

                        _prosimService.SetProsimVariable("aircraft.cargo.aft.amount", Convert.ToDouble(flightPlan.CargoTotal / 2));
                        _prosimService.SetProsimVariable("aircraft.cargo.forward.amount", Convert.ToDouble(flightPlan.CargoTotal / 2));
                        _prosimService.SetProsimVariable("efb.plannedCargoKg", flightPlan.CargoTotal);

                        string ForwardCargoStr = Convert.ToString(_prosimService.GetProsimVariable("aircraft.cargo.forward.amount"));
                        string AftCargoStr = Convert.ToString(_prosimService.GetProsimVariable("aircraft.cargo.aft.amount"));
                        _logger.LogDebug("Temp Cargo set: forward {ForwardCargo} aft {AftCargo}",
                            ForwardCargoStr,
                            AftCargoStr);
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
                _logger.LogError(ex, "Exception during UpdateCargoData");
            }
        }

        /// <inheritdoc/>
        public void UpdateCargoLoading(int cargoPercentage)
        {
            if (cargoPercentage == _cargoLast)
                return;

            _cargoLast = cargoPercentage;
            float cargo = (float)PlannedCargo * (float)(cargoPercentage / 100.0f);

            _prosimService.SetProsimVariable("aircraft.cargo.forward.amount", (float)cargo * _cargoDistMain);
            _prosimService.SetProsimVariable("aircraft.cargo.aft.amount", (float)cargo * _cargoDistMain);

            _logger.LogDebug("Cargo updated: {Percentage}% of {PlannedCargo}kg", cargoPercentage, PlannedCargo);
        }

        /// <inheritdoc/>
        public void ResetCargoToEmpty()
        {
            _isManuallyReset = true; // Set flag
            _prosimService.SetProsimVariable("aircraft.cargo.forward.amount", 0.0);
            _prosimService.SetProsimVariable("aircraft.cargo.aft.amount", 0.0);
        }
    }
}
