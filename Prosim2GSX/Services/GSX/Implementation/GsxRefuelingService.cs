using Prosim2GSX.Models;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of GSX refueling service
    /// </summary>
    public class GsxRefuelingService : IGsxRefuelingService
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly IGsxSimConnectService _simConnectService;
        private readonly IGsxMenuService _menuService;
        private readonly IFlightPlanService _flightPlanService;
        private readonly IRefuelingService _refuelingService;
        private readonly ServiceModel _model;

        private bool _refuelingRequested = false;
        private bool _initialFuelSet = false;

        /// <inheritdoc/>
        public bool IsRefuelingRequested => _refuelingRequested;

        /// <inheritdoc/>
        public bool IsFuelHoseConnected => _simConnectService.IsFuelHoseConnected();

        /// <inheritdoc/>
        public bool IsRefuelingComplete => _refuelingService.IsRefuelingComplete;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxRefuelingService(
            IProsimInterface prosimInterface,
            IGsxSimConnectService simConnectService,
            IGsxMenuService menuService,
            IFlightPlanService flightPlanService,
            IRefuelingService refuelingService,
            ServiceModel model)
        {
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _flightPlanService = flightPlanService ?? throw new ArgumentNullException(nameof(flightPlanService));
            _refuelingService = refuelingService ?? throw new ArgumentNullException(nameof(refuelingService));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            // Subscribe to GSX LVARs
            _simConnectService.SubscribeToGsxLvar("FSDT_GSX_REFUELING_STATE", OnRefuelingStateChanged);
            _simConnectService.SubscribeToGsxLvar("FSDT_GSX_FUELHOSE_CONNECTED", OnFuelHoseStateChanged);
        }

        /// <inheritdoc/>
        public void RequestRefuelingService()
        {
            Logger.Log(LogLevel.Information, nameof(GsxRefuelingService), "Requesting refueling service");

            // Initialize refueling in the Prosim service
            _refuelingService.StartRefueling();

            // Open GSX menu and select refueling
            _menuService.OpenMenu();
            _menuService.SelectMenuItem(3);
            _menuService.HandleOperatorSelection((int)_model.OperatorDelay);

            _refuelingRequested = true;
        }

        /// <inheritdoc/>
        public bool ProcessRefueling()
        {
            // Check if not requested yet
            if (!_refuelingRequested)
                return false;

            // Check if already completed
            if (_refuelingService.IsRefuelingComplete)
                return true;

            // Check fuel hose status and manage fuel transfer accordingly
            bool fuelHoseConnected = IsFuelHoseConnected;

            if (fuelHoseConnected && !_refuelingService.IsRefuelingActive)
            {
                // Start fuel transfer if hose connected but not active
                _refuelingService.ResumeRefueling();

                Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                    "Fuel hose connected but transfer not active - starting transfer");
            }
            else if (!fuelHoseConnected && _refuelingService.IsRefuelingActive)
            {
                // Pause fuel transfer if hose disconnected but active
                _refuelingService.PauseRefueling();

                Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                    "Fuel hose disconnected but transfer active - pausing transfer");
            }

            // Process fuel transfer if active
            if (_refuelingService.IsRefuelingActive)
            {
                return _refuelingService.ProcessRefueling();
            }

            return false;
        }

        /// <inheritdoc/>
        public void SetInitialFuel()
        {
            if (_initialFuelSet)
                return;

            _refuelingService.SetInitialFuel();
            _initialFuelSet = true;
        }

        /// <inheritdoc/>
        public double GetCurrentFuelAmount()
        {
            return _refuelingService.GetFuelAmount();
        }

        /// <summary>
        /// Handler for fuel hose state changes
        /// </summary>
        private void OnFuelHoseStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                $"Fuel hose state changed from {oldValue} to {newValue}");

            bool connected = newValue == 1;

            if (connected && !_refuelingService.IsRefuelingActive)
            {
                Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    "Fuel hose connected - starting fuel transfer");
                _refuelingService.ResumeRefueling();
            }
            else if (!connected && _refuelingService.IsRefuelingActive)
            {
                Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    "Fuel hose disconnected - pausing fuel transfer");
                _refuelingService.PauseRefueling();
            }
        }

        /// <summary>
        /// Handler for refueling state changes
        /// </summary>
        private void OnRefuelingStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                $"GSX refueling state changed from {oldValue} to {newValue}");

            // State 4 = Requested, 5 = Active, 6 = Completed
            if (newValue >= 4 && !_refuelingRequested)
            {
                _refuelingRequested = true;
                // Start the refueling in Prosim service
                _refuelingService.StartRefueling();
            }

            if (newValue == 6 && !_refuelingService.IsRefuelingComplete)
            {
                // GSX considers refueling complete
                _refuelingService.StopRefueling();
            }
        }
    }
}