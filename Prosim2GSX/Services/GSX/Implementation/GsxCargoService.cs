using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.GSX.Events;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
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
            IProsimInterface prosimInterface,
            IGsxSimConnectService simConnectService,
            IDoorControlService doorControlService,
            ServiceModel model)
        {
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _doorControlService = doorControlService ?? throw new ArgumentNullException(nameof(doorControlService));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            // Initialize service toggles
            InitializeServiceToggles();

            // Initialize door states
            UpdateDoorStates();
        }

        /// <summary>
        /// Initialize service toggle mappings
        /// </summary>
        private void InitializeServiceToggles()
        {
            _serviceToggles.Add("FSDT_GSX_AIRCRAFT_CARGO_1_TOGGLE", ToggleForwardCargoDoor);
            _serviceToggles.Add("FSDT_GSX_AIRCRAFT_CARGO_2_TOGGLE", ToggleAftCargoDoor);
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

                LogService.Log(LogLevel.Debug, nameof(GsxCargoService),
                    $"Door states updated - Forward: {forwardDoorState}, Aft: {aftDoorState}", LogCategory.Cargo);
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(GsxCargoService),
                    $"Error updating door states: {ex.Message}");
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

                LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                    "Successfully subscribed to cargo events");
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(GsxCargoService),
                    $"Error subscribing to cargo events: {ex.Message}");
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handler for cargo loading state changes
        /// </summary>
        private void OnCargoLoadingChanged(float newValue, float oldValue, string lvarName)
        {
            LogService.Log(LogLevel.Debug, nameof(GsxCargoService),
                $"Cargo loading changed from {oldValue} to {newValue}", LogCategory.Cargo);

            bool isActive = newValue == 1;
            _cargoLoadingActive = isActive;

            // Notify subscribers
            CargoOperationChanged?.Invoke(this, new CargoOperationEventArgs(true, isActive));

            // Open cargo doors when loading starts
            if (isActive && _model.SetOpenCargoDoors)
            {
                LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                    "Cargo loading started - opening cargo doors");
                OpenCargoDoors();
            }

            // Close cargo doors when loading completes
            if (!isActive && oldValue == 1 && _model.SetOpenCargoDoors)
            {
                LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                    "Cargo loading completed - closing cargo doors");
                CloseCargoDoors();
            }
        }

        /// <summary>
        /// Handler for cargo unloading state changes
        /// </summary>
        private void OnCargoUnloadingChanged(float newValue, float oldValue, string lvarName)
        {
            LogService.Log(LogLevel.Debug, nameof(GsxCargoService),
                $"Cargo unloading changed from {oldValue} to {newValue}", LogCategory.Cargo);

            bool isActive = newValue == 1;
            _cargoUnloadingActive = isActive;

            // Notify subscribers
            CargoOperationChanged?.Invoke(this, new CargoOperationEventArgs(false, isActive));

            // Open cargo doors when unloading starts
            if (isActive && _model.SetOpenCargoDoors)
            {
                LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                    "Cargo unloading started - opening cargo doors");
                OpenCargoDoors();
            }

            // Close cargo doors when unloading completes
            if (!isActive && oldValue == 1 && _model.SetOpenCargoDoors)
            {
                LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                    "Cargo unloading completed - closing cargo doors");
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

            LogService.Log(LogLevel.Debug, nameof(GsxCargoService),
                $"Cargo loading percentage changed from {oldValue}% to {percentage}%", LogCategory.Cargo);

            // Notify subscribers
            CargoPercentageChanged?.Invoke(this, new CargoPercentageEventArgs(true, percentage));

            // Close cargo doors when percentage reaches 100%
            if (percentage >= 100 && oldValue < 100 && _model.SetOpenCargoDoors && _cargoLoadingActive)
            {
                LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                    "Cargo loading reached 100% - closing cargo doors");
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

            LogService.Log(LogLevel.Debug, nameof(GsxCargoService),
                $"Cargo unloading percentage changed from {oldValue}% to {percentage}%", LogCategory.Cargo);

            // Notify subscribers
            CargoPercentageChanged?.Invoke(this, new CargoPercentageEventArgs(false, percentage));

            // Close cargo doors when percentage reaches 100%
            if (percentage >= 100 && oldValue < 100 && _model.SetOpenCargoDoors && _cargoUnloadingActive)
            {
                LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                    "Cargo unloading reached 100% - closing cargo doors");
                CloseCargoDoors();
            }
        }

        /// <summary>
        /// Handler for service toggle changes
        /// </summary>
        private void OnServiceToggleChanged(float newValue, float oldValue, string lvarName)
        {
            LogService.Log(LogLevel.Debug, nameof(GsxCargoService),
                $"Service toggle {lvarName} changed from {oldValue} to {newValue}", LogCategory.Cargo);

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
                    LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                        "Opened forward cargo door");

                    // Notify subscribers
                    CargoDoorStateChanged?.Invoke(this, new CargoDoorEventArgs(true, true));
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(GsxCargoService),
                    $"Error opening forward cargo door: {ex.Message}");
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
                    LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                        "Closed forward cargo door");

                    // Notify subscribers
                    CargoDoorStateChanged?.Invoke(this, new CargoDoorEventArgs(true, false));
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(GsxCargoService),
                    $"Error closing forward cargo door: {ex.Message}");
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
                    LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                        "Opened aft cargo door");

                    // Notify subscribers
                    CargoDoorStateChanged?.Invoke(this, new CargoDoorEventArgs(false, true));
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(GsxCargoService),
                    $"Error opening aft cargo door: {ex.Message}");
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
                    LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                        "Closed aft cargo door");

                    // Notify subscribers
                    CargoDoorStateChanged?.Invoke(this, new CargoDoorEventArgs(false, false));
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(GsxCargoService),
                    $"Error closing aft cargo door: {ex.Message}");
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
                    LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                        "Toggled forward cargo door: closed");
                }
                else
                {
                    OpenForwardCargoDoor();
                    LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                        "Toggled forward cargo door: opened");
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(GsxCargoService),
                    $"Error toggling forward cargo door: {ex.Message}");
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
                    LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                        "Toggled aft cargo door: closed");
                }
                else
                {
                    OpenAftCargoDoor();
                    LogService.Log(LogLevel.Information, nameof(GsxCargoService),
                        "Toggled aft cargo door: opened");
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(GsxCargoService),
                    $"Error toggling aft cargo door: {ex.Message}");
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
                LogService.Log(LogLevel.Error, nameof(GsxCargoService),
                    $"Error processing cargo operations: {ex.Message}");
            }
        }
    }
}