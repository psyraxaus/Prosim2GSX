using System;
using System.Windows.Controls;
using Prosim2GSX.Events;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.Services.Prosim.Implementation;
using Prosim2GSX.Services.Prosim.Interfaces;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of GSX refueling service
    /// </summary>
    public class GsxRefuelingService : IGsxRefuelingService
    {
        private readonly IRefuelingService _prosimRefuelingService;
        private readonly IProsimInterface _prosimInterface;
        private readonly IGsxMenuService _menuService;
        private readonly IGsxSimConnectService _simConnectService;

        private bool _refuelingCompleted = false;
        private bool _initialFuelSet = false;
        private bool _hydraulicFluidsSet = false;
        private bool _refuelingRequested = false;
        private bool _refuelingActive = false;
        private bool _refuelingPaused = false;
        private bool _fuelHoseConnected = false;


        /// <summary>
        /// Constructor
        /// </summary>
        public GsxRefuelingService(
            IProsimInterface prosimInterface,
            IGsxSimConnectService simConnectService,
            IGsxMenuService menuService,
            IRefuelingService refuelingService
            
            )
        {
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _prosimRefuelingService = refuelingService ?? throw new ArgumentNullException(nameof(refuelingService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));

            _simConnectService.SubscribeToGsxLvar("FSDT_GSX_FUELHOSE_CONNECTED", OnFuelHoseStateChanged);
        }

        /// <inheritdoc/>
        public bool IsRefuelingComplete => _refuelingCompleted;

        /// <inheritdoc/>
        public bool IsInitialFuelSet => _initialFuelSet;

        /// <inheritdoc/>
        public bool IsHydraulicFluidsSet => _hydraulicFluidsSet;

        /// <inheritdoc/>
        public bool IsRefuelingRequested => _refuelingRequested;

        /// <inheritdoc/>
        public bool IsRefuelingActive => _refuelingActive;

        /// <inheritdoc/>
        public bool IsRefuelingPaused => _refuelingPaused;

        /// <inheritdoc/>
        public void SetInitialFuel()
        {
            LogService.Log(LogLevel.Debug, nameof(GsxRefuelingService), "Setting initial fuel");
            _prosimRefuelingService.SetInitialFuel();
            _initialFuelSet = true;
        }

        /// <inheritdoc/>
        public void SetHydraulicFluidLevels()
        {
            LogService.Log(LogLevel.Information, nameof(GsxRefuelingService), "Setting initial hydraulic fluid levels");
            _prosimRefuelingService.SetInitialFluids();
            _hydraulicFluidsSet = true;
        }

        /// <inheritdoc/>
        public void RequestRefueling()
        {
            LogService.Log(LogLevel.Information, nameof(GsxRefuelingService), "Requesting refueling service");

            _menuService.OpenMenu();
            _menuService.SelectMenuItem(3);
            _refuelingRequested = true;

            // Publish event
            EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Requested));
        }

        /// <inheritdoc/>
        public void SetRefuelingActive()
        {
            _refuelingActive = true;
            _refuelingPaused = true; // Start in paused state until hose is connected
            LogService.Log(LogLevel.Information, nameof(GsxRefuelingService), $"Fuel Service active", LogCategory.Refueling);
            _prosimRefuelingService.StartRefueling();
        }

        /// <inheritdoc/>
        public void ProcessRefueling()
        {
            if (_prosimRefuelingService.ProcessRefueling())
            {
                _refuelingActive = false;
                _refuelingCompleted = true;
                _refuelingPaused = false;
                _prosimRefuelingService.StopRefueling();
                LogService.Log(LogLevel.Information, nameof(GsxRefuelingService), $"Refuel completed");
            }
        }

        /// <inheritdoc/>
        public void StopRefueling()
        {
            LogService.Log(LogLevel.Information, nameof(GsxRefuelingService), $"GSX reports refueling completed (state 6)");
            _refuelingActive = false;
            _refuelingCompleted = true;
            _refuelingPaused = false;
            _prosimRefuelingService.StopRefueling();
        }

        /// <summary>
        /// Handler for fuel hose state changes
        /// </summary>
        private void OnFuelHoseStateChanged(float newValue, float oldValue, string lvarName)
        {
            LogService.Log(LogLevel.Debug, nameof(GsxController),
                $"Fuel hose state changed from {oldValue} to {newValue}", LogCategory.Refueling);
            _fuelHoseConnected = newValue == 1;

            if (_refuelingActive)
            {
                if (_fuelHoseConnected)
                {
                    // Fuel hose was just connected
                    LogService.Log(LogLevel.Information, nameof(GsxRefuelingService),
                        $"Fuel hose connected", LogCategory.Refueling);
                    _refuelingPaused = false;
                    _prosimRefuelingService.ResumeRefueling();
                }
                else
                {
                    // Fuel hose was just disconnected
                    LogService.Log(LogLevel.Information, nameof(GsxRefuelingService),
                        $"Fuel hose disconnected", LogCategory.Refueling);
                    _refuelingPaused = true;
                    _prosimRefuelingService.PauseRefueling();
                }
            }
        }
    }
}