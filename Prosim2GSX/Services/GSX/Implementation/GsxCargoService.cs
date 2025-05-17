using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.GSX.Events;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of cargo service
    /// </summary>
    public class GsxCargoService : IGsxCargoService
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly IGsxSimConnectService _simConnectService;
        private readonly IDoorControlService _doorControlService;
        private readonly ServiceModel _model;
        private readonly ILogger<GsxCargoService> _logger;

        private bool _frontCargoDoorOpen = false;
        private bool _aftCargoDoorOpen = false;
        private bool _cargoLoadingActive = false;
        private bool _cargoUnloadingActive = false;
        private int _cargoLoadingPercentage = 0;
        private int _cargoUnloadingPercentage = 0;

        // Dictionary to map service toggle LVAR names to door operations
        private readonly Dictionary<string, Action> _serviceToggles = new Dictionary<string, Action>();

        /// <inheritdoc/>
        public bool IsForwardCargoDoorOpen => _frontCargoDoorOpen;

        /// <inheritdoc/>
        public bool IsAftCargoDoorOpen => _aftCargoDoorOpen;

        /// <inheritdoc/>
        public bool IsCargoLoadingActive => _cargoLoadingActive;

        /// <inheritdoc/>
        public bool IsCargoUnloadingActive => _cargoUnloadingActive;

        /// <inheritdoc/>
        public int CargoLoadingPercentage => _cargoLoadingPercentage;

        /// <inheritdoc/>
        public int CargoUnloadingPercentage => _cargoUnloadingPercentage;

        /// <inheritdoc/>
        public event EventHandler<CargoOperationEventArgs> CargoOperationChanged;

        /// <inheritdoc/>
        public event EventHandler<CargoPercentageEventArgs> CargoPercentageChanged;

        /// <inheritdoc/>
        public event EventHandler<CargoDoorEventArgs> CargoDoorStateChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxCargoService(
            ILogger<GsxCargoService> logger,
            IProsimInterface prosimInterface,
            IGsxSimConnectService simConnectService,
            IDoorControlService doorControlService,
            ServiceModel model)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _doorControlService = doorControlService ?? throw new ArgumentNullException(nameof(doorControlService));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            // Initialize service toggles
            InitializeServiceToggles();

            // Initialize door states
            UpdateDoorStates();

            _logger.LogInformation("GSX Cargo Service initialized");
        }

        /// <summary>
        /// Initialize service toggle mappings
        /// </summary>
        private void InitializeServiceToggles()
        {
            _serviceToggles.Add("FSDT_GSX_AIRCRAFT_CARGO_1_TOGGLE", ToggleForwardCargoDoor);
            _serviceToggles.Add("FSDT_GSX_AIRCRAFT_CARGO_2_TOGGLE", ToggleAftCargoDoor);

            _logger.LogDebug("Service toggles initialized");
        }

        /// <summary>
        /// Update door states from actual door positions
        /// </summary>
        private void UpdateDoorStates()
        {
            try
            {
                // Get actual door states from Prosim
                string forwardDoorState = _doorControlService.GetForwardCargoDoor();
                string aftDoorState = _doorControlService.GetAftCargoDoor();

                // Update internal state
                _frontCargoDoorOpen = forwardDoorState == "open";
                _aftCargoDoorOpen = aftDoorState == "open";

                _logger.LogDebug("Door states updated - Forward: {ForwardDoorState}, Aft: {AftDoorState}",
                    forwardDoorState, aftDoorState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating door states");
            }
        }

        /// <inheritdoc/>
        public void SubscribeToCargoEvents()
        {
            try
            {
                // Subscribe to cargo operation LVARs
                _simConnectService.SubscribeToGsxLvar("FSDT_GSX_BOARDING_CARGO", OnCargoLoadingChanged);
                _simConnectService.SubscribeToGsxLvar("FSDT_GSX_DEBOARDING_CARGO", OnCargoUnloadingChanged);
                _simConnectService.SubscribeToGsxLvar("FSDT_GSX_BOARDING_CARGO_PERCENT", OnCargoLoadingPercentChanged);
                _simConnectService.SubscribeToGsxLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT", OnCargoUnloadingPercentChanged);

                // Subscribe to service toggle LVARs
                foreach (var toggleLvar in _serviceToggles.Keys)
                {
                    _simConnectService.SubscribeToGsxLvar(toggleLvar, OnServiceToggleChanged);
                }

                _logger.LogInformation("Successfully subscribed to cargo events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to cargo events");
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handler for cargo loading state changes
        /// </summary>
        private void OnCargoLoadingChanged(float newValue, float oldValue, string lvarName)
        {
            _logger.LogDebug("Cargo loading changed from {OldValue} to {NewValue}", oldValue, newValue);

            bool isActive = newValue == 1;
            _cargoLoadingActive = isActive;

            // Notify subscribers
            CargoOperationChanged?.Invoke(this, new CargoOperationEventArgs(true, isActive));

            // Open cargo doors when loading starts
            if (isActive && _model.SetOpenCargoDoors)
            {
                _logger.LogInformation("Cargo loading started - opening cargo doors");
                OpenCargoDoors();
            }

            // Close cargo doors when loading completes
            if (!isActive && oldValue == 1 && _model.SetOpenCargoDoors)
            {
                _logger.LogInformation("Cargo loading completed - closing cargo doors");
                CloseCargoDoors();
            }
        }

        /// <summary>
        /// Handler for cargo unloading state changes
        /// </summary>
        private void OnCargoUnloadingChanged(float newValue, float oldValue, string lvarName)
        {
            _logger.LogDebug("Cargo unloading changed from {OldValue} to {NewValue}", oldValue, newValue);

            bool isActive = newValue == 1;
            _cargoUnloadingActive = isActive;

            // Notify subscribers
            CargoOperationChanged?.Invoke(this, new CargoOperationEventArgs(false, isActive));

            // Open cargo doors when unloading starts
            if (isActive && _model.SetOpenCargoDoors)
            {
                _logger.LogInformation("Cargo unloading started - opening cargo doors");
                OpenCargoDoors();
            }

            // Close cargo doors when unloading completes
            if (!isActive && oldValue == 1 && _model.SetOpenCargoDoors)
            {
                _logger.LogInformation("Cargo unloading completed - closing cargo doors");
                CloseCargoDoors();
            }
        }

        /// <summary>
        /// Handler for cargo loading percentage changes
        /// </summary>
        private void OnCargoLoadingPercentChanged(float newValue, float oldValue, string lvarName)
        {
            int percentage = (int)newValue;
            _cargoLoadingPercentage = percentage;

            _logger.LogDebug("Cargo loading percentage changed from {OldPercentage}% to {NewPercentage}%",
                (int)oldValue, percentage);

            // Notify subscribers
            CargoPercentageChanged?.Invoke(this, new CargoPercentageEventArgs(true, percentage));

            // Close cargo doors when percentage reaches 100%
            if (percentage >= 100 && oldValue < 100 && _model.SetOpenCargoDoors && _cargoLoadingActive)
            {
                _logger.LogInformation("Cargo loading reached 100% - closing cargo doors");
                CloseCargoDoors();
            }
        }

        /// <summary>
        /// Handler for cargo unloading percentage changes
        /// </summary>
        private void OnCargoUnloadingPercentChanged(float newValue, float oldValue, string lvarName)
        {
            int percentage = (int)newValue;
            _cargoUnloadingPercentage = percentage;

            _logger.LogDebug("Cargo unloading percentage changed from {OldPercentage}% to {NewPercentage}%",
                (int)oldValue, percentage);

            // Notify subscribers
            CargoPercentageChanged?.Invoke(this, new CargoPercentageEventArgs(false, percentage));

            // Close cargo doors when percentage reaches 100%
            if (percentage >= 100 && oldValue < 100 && _model.SetOpenCargoDoors && _cargoUnloadingActive)
            {
                _logger.LogInformation("Cargo unloading reached 100% - closing cargo doors");
                CloseCargoDoors();
            }
        }

        /// <summary>
        /// Handler for service toggle changes
        /// </summary>
        private void OnServiceToggleChanged(float newValue, float oldValue, string lvarName)
        {
            _logger.LogDebug("Service toggle {LvarName} changed from {OldValue} to {NewValue}",
                lvarName, oldValue, newValue);

            // Check if this is one of our monitored service toggles
            if (_serviceToggles.ContainsKey(lvarName))
            {
                // Check if toggle changed from 0 to 1
                if (oldValue == 0 && newValue == 1)
                {
                    // Trigger the appropriate door operation
                    _serviceToggles[lvarName]();
                }
            }
        }

        #endregion

        #region Door Operations

        /// <inheritdoc/>
        public void OpenForwardCargoDoor()
        {
            try
            {
                if (_model.SetOpenCargoDoors && !_frontCargoDoorOpen)
                {
                    _doorControlService.SetForwardCargoDoor(true);
                    _frontCargoDoorOpen = true;
                    _logger.LogInformation("Opened forward cargo door");

                    // Notify subscribers
                    CargoDoorStateChanged?.Invoke(this, new CargoDoorEventArgs(true, true));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening forward cargo door");
            }
        }

        /// <inheritdoc/>
        public void CloseForwardCargoDoor()
        {
            try
            {
                if (_frontCargoDoorOpen)
                {
                    _doorControlService.SetForwardCargoDoor(false);
                    _frontCargoDoorOpen = false;
                    _logger.LogInformation("Closed forward cargo door");

                    // Notify subscribers
                    CargoDoorStateChanged?.Invoke(this, new CargoDoorEventArgs(true, false));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing forward cargo door");
            }
        }

        /// <inheritdoc/>
        public void OpenAftCargoDoor()
        {
            try
            {
                if (_model.SetOpenCargoDoors && !_aftCargoDoorOpen)
                {
                    _doorControlService.SetAftCargoDoor(true);
                    _aftCargoDoorOpen = true;
                    _logger.LogInformation("Opened aft cargo door");

                    // Notify subscribers
                    CargoDoorStateChanged?.Invoke(this, new CargoDoorEventArgs(false, true));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening aft cargo door");
            }
        }

        /// <inheritdoc/>
        public void CloseAftCargoDoor()
        {
            try
            {
                if (_aftCargoDoorOpen)
                {
                    _doorControlService.SetAftCargoDoor(false);
                    _aftCargoDoorOpen = false;
                    _logger.LogInformation("Closed aft cargo door");

                    // Notify subscribers
                    CargoDoorStateChanged?.Invoke(this, new CargoDoorEventArgs(false, false));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing aft cargo door");
            }
        }

        /// <inheritdoc/>
        public void OpenCargoDoors()
        {
            OpenForwardCargoDoor();
            OpenAftCargoDoor();
        }

        /// <inheritdoc/>
        public void CloseCargoDoors()
        {
            CloseForwardCargoDoor();
            CloseAftCargoDoor();
        }

        /// <inheritdoc/>
        public void ToggleForwardCargoDoor()
        {
            try
            {
                if (_frontCargoDoorOpen)
                {
                    CloseForwardCargoDoor();
                    _logger.LogInformation("Toggled forward cargo door: closed");
                }
                else
                {
                    OpenForwardCargoDoor();
                    _logger.LogInformation("Toggled forward cargo door: opened");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling forward cargo door");
            }
        }

        /// <inheritdoc/>
        public void ToggleAftCargoDoor()
        {
            try
            {
                if (_aftCargoDoorOpen)
                {
                    CloseAftCargoDoor();
                    _logger.LogInformation("Toggled aft cargo door: closed");
                }
                else
                {
                    OpenAftCargoDoor();
                    _logger.LogInformation("Toggled aft cargo door: opened");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling aft cargo door");
            }
        }

        #endregion

        /// <inheritdoc/>
        public void ProcessCargoOperations()
        {
            try
            {
                // This method is called regularly by the GsxController to process cargo operations
                // We don't need to do much here since most of the work is event-driven,
                // but we can use this to update door states and other regular processing

                // Update door states from actual Prosim door positions
                UpdateDoorStates();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cargo operations");
            }
        }
    }
}
