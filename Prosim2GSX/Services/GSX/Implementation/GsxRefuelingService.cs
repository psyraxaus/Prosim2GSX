using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.GSX.Enums;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
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
        private bool _refuelingCompleted = false;
        private bool _initialFuelSet = false;
        private int _refuelingState = 0;

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
            LogService.Log(LogLevel.Information, nameof(GsxRefuelingService), "Requesting refueling service");

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

                LogService.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                    "Fuel hose connected but transfer not active - starting transfer", LogCategory.Refueling);
            }
            else if (!fuelHoseConnected && _refuelingService.IsRefuelingActive)
            {
                // Pause fuel transfer if hose disconnected but active
                _refuelingService.PauseRefueling();

                LogService.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                    "Fuel hose disconnected but transfer active - pausing transfer", LogCategory.Refueling);
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
            LogService.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                $"Fuel hose state changed from {oldValue} to {newValue}", LogCategory.Refueling);

            bool connected = newValue == 1;

            if (connected && !_refuelingService.IsRefuelingActive)
            {
                LogService.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    "Fuel hose connected - starting fuel transfer");
                _refuelingService.ResumeRefueling();
            }
            else if (!connected && _refuelingService.IsRefuelingActive)
            {
                LogService.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    "Fuel hose disconnected - pausing fuel transfer");
                _refuelingService.PauseRefueling();
            }
        }

        /// <summary>
        /// Handler for refueling state changes
        /// </summary>
        private void OnRefuelingStateChanged(float newValue, float oldValue, string lvarName)
        {
            _refuelingState = (int)newValue;

            LogService.Log(LogLevel.Debug, nameof(GsxRefuelingService), $"Refueling state changed to {newValue}", LogCategory.Refueling);

            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == (int)GsxServiceState.Completed ? ServiceStatus.Completed :
                                      newValue == (int)GsxServiceState.Active ? ServiceStatus.Active :
                                      newValue == (int)GsxServiceState.Requested ? ServiceStatus.Requested :
                                      ServiceStatus.Inactive;

                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", status));
            }

            // Set refuelignComplete when refueling reaches completed state
            if (newValue == (int)GsxServiceState.Completed && !_refuelingCompleted)
            {
                _refuelingCompleted = true;
                LogService.Log(LogLevel.Information, nameof(GsxRefuelingService), "Refueling service completed");
            }
        }
    }
}