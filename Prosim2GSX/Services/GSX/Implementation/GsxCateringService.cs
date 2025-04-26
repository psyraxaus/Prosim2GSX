using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.GSX.Enums;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of GSX catering service
    /// </summary>
    public class GsxCateringService : IGsxCateringService
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly IGsxSimConnectService _simConnectService;
        private readonly IGsxMenuService _menuService;
        private readonly IDoorControlService _doorControlService;
        private readonly ServiceModel _model;

        private bool _cateringRequested = false;
        private bool _cateringComplete = false;
        private int _cateringState = 0;

        // Dictionary to map service toggle LVAR names to door operations
        private readonly Dictionary<string, Action> _serviceToggles = new Dictionary<string, Action>();

        /// <inheritdoc/>
        public bool IsCateringRequested => _cateringRequested;

        /// <inheritdoc/>
        public bool IsCateringActive => _cateringState == (int)GsxServiceState.Active;

        /// <inheritdoc/>
        public bool IsCateringComplete => _cateringComplete || _cateringState == (int)GsxServiceState.Completed;

        /// <inheritdoc/>
        public int CateringState => _cateringState;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxCateringService(
            IProsimInterface prosimInterface,
            IGsxSimConnectService simConnectService,
            IGsxMenuService menuService,
            IDoorControlService doorControlService,
            ServiceModel model)
        {
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _doorControlService = doorControlService ?? throw new ArgumentNullException(nameof(doorControlService));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            // Subscribe to catering state changes
            _simConnectService.SubscribeToGsxLvar("FSDT_GSX_CATERING_STATE", OnCateringStateChanged);

            // Initialize service toggles
            InitializeServiceToggles();
        }

        /// <summary>
        /// Initialize the service toggle mappings
        /// </summary>
        private void InitializeServiceToggles()
        {
            _serviceToggles.Add("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE", ToggleFrontDoor);
            _serviceToggles.Add("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE", ToggleAftDoor);
        }

        /// <inheritdoc/>
        public void SubscribeToServiceToggles()
        {
            // Subscribe to all service toggle LVARs
            foreach (var toggleLvar in _serviceToggles.Keys)
            {
                _simConnectService.SubscribeToGsxLvar(toggleLvar, OnServiceToggleChanged);
            }
        }

        /// <summary>
        /// Handler for catering state changes
        /// </summary>
        private void OnCateringStateChanged(float newValue, float oldValue, string lvarName)
        {
            _cateringState = (int)newValue;

            LogService.Log(LogLevel.Debug, nameof(GsxCateringService), $"Catering state changed to {newValue}", LogCategory.Catering);

            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == (int)GsxServiceState.Completed ? ServiceStatus.Completed :
                                      newValue == (int)GsxServiceState.Active ? ServiceStatus.Active :
                                      newValue == (int)GsxServiceState.Requested ? ServiceStatus.Requested :
                                      ServiceStatus.Inactive;

                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Catering", status));
            }

            // Set cateringComplete when catering reaches completed state
            if (newValue == (int)GsxServiceState.Completed && !_cateringComplete)
            {
                _cateringComplete = true;
                LogService.Log(LogLevel.Information, nameof(GsxCateringService), "Catering service completed");
            }
        }

        /// <summary>
        /// Handler for service toggle changes
        /// </summary>
        private void OnServiceToggleChanged(float newValue, float oldValue, string lvarName)
        {
            LogService.Log(LogLevel.Debug, nameof(GsxCateringService),
                $"Service toggle {lvarName} changed from {oldValue} to {newValue}", LogCategory.Catering);

            // Check if this is one of our monitored service toggles
            if (_serviceToggles.ContainsKey(lvarName))
            {
                // Check if door toggle changed from 0 to 1
                if (oldValue == 0 && newValue == 1)
                {
                    // Trigger the appropriate door operation based on the current catering state
                    _serviceToggles[lvarName]();
                }
            }
        }

        /// <inheritdoc/>
        public void RequestCateringService()
        {
            if (_cateringRequested || _cateringComplete)
                return;

            LogService.Log(LogLevel.Information, nameof(GsxCateringService), "Calling Catering Service");

            _menuService.OpenMenu();
            _menuService.SelectMenuItem(2);
            _menuService.HandleOperatorSelection((int)_model.OperatorDelay);

            _cateringRequested = true;
        }

        /// <inheritdoc/>
        public bool ProcessCatering()
        {
            // Nothing to actively process for catering, just return if complete
            return _cateringComplete;
        }

        /// <inheritdoc/>
        public void ToggleFrontDoor()
        {
            // Operate front door based on catering state
            LogService.Log(LogLevel.Debug, nameof(GsxCateringService), "Command to operate Front Door", LogCategory.Catering);

            if (_model.SetOpenCateringDoor)
            {
                if (_cateringState == (int)GsxServiceState.Requested ||
                    (_cateringState == (int)GsxServiceState.Active && !_prosimInterface.GetProsimVariable("doors.entry.right.fwd")))
                {
                    _doorControlService.SetForwardRightDoor(true);
                    LogService.Log(LogLevel.Information, nameof(GsxCateringService), "Opened forward right door for catering");
                }
                else if (_cateringState == (int)GsxServiceState.Active && _prosimInterface.GetProsimVariable("doors.entry.right.fwd"))
                {
                    _doorControlService.SetForwardRightDoor(false);
                    LogService.Log(LogLevel.Information, nameof(GsxCateringService), "Closed forward right door");
                }
            }
        }

        /// <inheritdoc/>
        public void ToggleAftDoor()
        {
            // Operate aft door based on catering state
            LogService.Log(LogLevel.Debug, nameof(GsxCateringService), "Command to operate Aft Door", LogCategory.Catering);

            if (_model.SetOpenCateringDoor)
            {
                if (_cateringState == (int)GsxServiceState.Requested ||
                    (_cateringState == (int)GsxServiceState.Active && !_prosimInterface.GetProsimVariable("doors.entry.right.aft")))
                {
                    _doorControlService.SetAftRightDoor(true);
                    LogService.Log(LogLevel.Information, nameof(GsxCateringService), "Opened aft right door for catering");
                }
                else if (_cateringState == (int)GsxServiceState.Active && _prosimInterface.GetProsimVariable("doors.entry.right.aft"))
                {
                    _doorControlService.SetAftRightDoor(false);
                    LogService.Log(LogLevel.Information, nameof(GsxCateringService), "Closed aft right door");
                }
            }
        }
    }
}