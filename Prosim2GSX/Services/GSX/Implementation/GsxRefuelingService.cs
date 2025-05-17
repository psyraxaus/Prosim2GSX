using System;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Logging;
using Prosim2GSX.Services.Prosim.Implementation;
using Prosim2GSX.Services.Prosim.Interfaces;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of GSX refueling service
    /// </summary>
    public class GsxRefuelingService : IGsxRefuelingService
    {
        private readonly ILogger<GsxRefuelingService> _logger;
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
            ILogger<GsxRefuelingService> logger,
            IProsimInterface prosimInterface,
            IGsxSimConnectService simConnectService,
            IGsxMenuService menuService,
            IRefuelingService refuelingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _prosimRefuelingService = refuelingService ?? throw new ArgumentNullException(nameof(refuelingService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));

            _simConnectService.SubscribeToGsxLvar("FSDT_GSX_FUELHOSE_CONNECTED", OnFuelHoseStateChanged);

            _logger.LogDebug("GsxRefuelingService initialized");
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
        public bool IsFuelHoseConnected => _fuelHoseConnected;

        /// <inheritdoc/>
        public void SetInitialFuel()
        {
            _logger.LogDebug("Setting initial fuel"); // Replace LogService.Log 
            _prosimRefuelingService.SetInitialFuel();
            _initialFuelSet = true;
        }

        /// <inheritdoc/>
        public void SetHydraulicFluidLevels()
        {
            _logger.LogInformation("Setting initial hydraulic fluid levels"); // Replace LogService.Log
            _prosimRefuelingService.SetInitialFluids();
            _hydraulicFluidsSet = true;
        }

        /// <inheritdoc/>
        public void RequestRefueling()
        {
            _logger.LogInformation("Requesting refueling service"); // Replace LogService.Log

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

            // Replace LogService.Log with category scope pattern
            using (_logger.BeginScope(new { Category = LogCategories.Refueling }))
            {
                _logger.LogInformation("Fuel Service active");
            }

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
                _logger.LogInformation("Refuel completed"); // Replace LogService.Log
            }
        }

        /// <inheritdoc/>
        public void StopRefueling()
        {
            _logger.LogInformation("GSX reports refueling completed (state 6)"); // Replace LogService.Log
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
            // Replace LogService.Log with category scope pattern
            using (_logger.BeginScope(new { Category = LogCategories.Refueling }))
            {
                _logger.LogDebug("Fuel hose state changed from {OldValue} to {NewValue}", oldValue, newValue);
            }

            _fuelHoseConnected = newValue == 1;

            if (_refuelingActive)
            {
                if (_fuelHoseConnected)
                {
                    // Fuel hose was just connected
                    using (_logger.BeginScope(new { Category = LogCategories.Refueling }))
                    {
                        _logger.LogInformation("Fuel hose connected");
                    }

                    _refuelingPaused = false;
                    _prosimRefuelingService.ResumeRefueling();
                }
                else
                {
                    // Fuel hose was just disconnected
                    using (_logger.BeginScope(new { Category = LogCategories.Refueling }))
                    {
                        _logger.LogInformation("Fuel hose disconnected");
                    }

                    _refuelingPaused = true;
                    _prosimRefuelingService.PauseRefueling();
                }
            }
        }
    }
}
