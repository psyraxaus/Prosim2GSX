using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Threading;

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

        private bool _isRefueling = false;
        private bool _isRefuelingComplete = false;
        private bool _isRefuelingPaused = false;
        private double _targetFuelAmount = 0;
        private double _initialFuelAmount = 0;
        private DateTime _refuelingStartTime;
        private bool _initialFuelSet = false;

        /// <inheritdoc/>
        public bool IsRefueling => _isRefueling;

        /// <inheritdoc/>
        public bool IsRefuelingComplete => _isRefuelingComplete;

        /// <inheritdoc/>
        public bool IsRefuelingPaused => _isRefuelingPaused;

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

            // Subscribe to refueling state changes
            _simConnectService.SubscribeToGsxLvar("FSDT_GSX_REFUELING_STATE", OnRefuelingStateChanged);
            _simConnectService.SubscribeToGsxLvar("FSDT_GSX_FUELHOSE_CONNECTED", OnFuelHoseStateChanged);
        }

        /// <summary>
        /// Handler for refueling state changes
        /// </summary>
        private void OnRefuelingStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                $"Refueling state changed from {oldValue} to {newValue}");

            if (newValue != oldValue)
            {
                var status = newValue == 6 ? ServiceStatus.Completed :
                            newValue == 5 ? ServiceStatus.Active :
                            newValue == 4 ? ServiceStatus.Requested :
                            ServiceStatus.Inactive;

                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", status));

                // If refueling is completed automatically by GSX
                if (newValue == 6 && !_isRefuelingComplete)
                {
                    _isRefueling = false;
                    _isRefuelingComplete = true;
                    _isRefuelingPaused = false;

                    Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                        "Refueling completed automatically by GSX");
                }

                // If refueling becomes active
                if (newValue == 5 && !_isRefueling)
                {
                    Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                        "Refueling started automatically by GSX");
                }
            }
        }

        /// <summary>
        /// Handler for fuel hose state changes
        /// </summary>
        private void OnFuelHoseStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                $"Fuel hose state changed from {oldValue} to {newValue}");

            if (_isRefueling)
            {
                if (newValue == 1 && oldValue == 0)
                {
                    // Fuel hose was just connected
                    Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                        $"Fuel hose connected - starting fuel transfer");
                    _isRefuelingPaused = false;

                    // Start the actual fuel transfer process
                    ResumeRefueling();
                }
                else if (newValue == 0 && oldValue == 1)
                {
                    // Fuel hose was just disconnected
                    Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                        $"Fuel hose disconnected - pausing fuel transfer");
                    _isRefuelingPaused = true;
                    PauseRefueling();
                }
            }

            // Call the handler for external use
            HandleFuelHoseStateChange(newValue == 1);
        }

        /// <inheritdoc/>
        public void StartRefueling()
        {
            if (_isRefueling)
                return;

            try
            {
                // Get the current fuel amount
                _initialFuelAmount = GetCurrentFuelAmount();

                // Get the target fuel amount
                _targetFuelAmount = GetTargetFuelAmount();

                // Set the start time
                _refuelingStartTime = DateTime.Now;

                // Delegate to the RefuelingService to initialize the refueling process
                _refuelingService.StartRefueling();

                // Set flags
                _isRefueling = true;
                _isRefuelingComplete = false;
                _isRefuelingPaused = true; // Start paused until fuel hose is connected

                Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    $"Refueling started. Initial: {_initialFuelAmount:F1}, Target: {_targetFuelAmount:F1}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error starting refueling: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void StopRefueling()
        {
            if (!_isRefueling)
                return;

            try
            {
                // Delegate to the RefuelingService to stop the refueling process
                _refuelingService.StopRefueling();

                // Set flags
                _isRefueling = false;
                _isRefuelingComplete = true;
                _isRefuelingPaused = false;

                Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    $"Refueling stopped. Final: {GetCurrentFuelAmount():F1}, Target: {_targetFuelAmount:F1}");

                // Update the UI
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Completed));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error stopping refueling: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void PauseRefueling()
        {
            if (!_isRefueling || _isRefuelingPaused)
                return;

            try
            {
                // Set flags
                _isRefuelingPaused = true;

                // Delegate to the RefuelingService to pause the refueling process
                _refuelingService.PauseRefueling();

                Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    $"Refueling paused. Current: {GetCurrentFuelAmount():F1}, Target: {_targetFuelAmount:F1}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error pausing refueling: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void ResumeRefueling()
        {
            if (!_isRefueling || !_isRefuelingPaused)
                return;

            try
            {
                // Set flags
                _isRefuelingPaused = false;

                // Delegate to the RefuelingService to resume the refueling process
                _refuelingService.ResumeRefueling();

                Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    $"Refueling resumed. Current: {GetCurrentFuelAmount():F1}, Target: {_targetFuelAmount:F1}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error resuming refueling: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public bool ProcessRefueling()
        {
            if (!_isRefueling || _isRefuelingComplete || _isRefuelingPaused)
                return false;

            try
            {
                // Get the current fuel amount
                double currentFuel = GetCurrentFuelAmount();

                // Check if refueling is complete
                if (Math.Abs(currentFuel - _targetFuelAmount) < 10.0)
                {
                    StopRefueling();
                    return true;
                }

                // Check if the GSX refueling state indicates completed
                if (_simConnectService.GetRefuelingState() == 6)
                {
                    StopRefueling();
                    return true;
                }

                // Only process refueling if hose is connected
                bool fuelHoseConnected = _simConnectService.IsFuelHoseConnected();
                if (fuelHoseConnected)
                {
                    // Delegate to the Prosim RefuelingService to handle the actual fuel transfer
                    bool isComplete = _refuelingService.ProcessRefueling();

                    // If Prosim's refueling service says we're done, stop our service too
                    if (isComplete)
                    {
                        StopRefueling();
                        return true;
                    }

                    double progress = (currentFuel - _initialFuelAmount) / (_targetFuelAmount - _initialFuelAmount) * 100.0;
                    progress = Math.Min(progress, 100.0);
                    progress = Math.Max(progress, 0.0);

                    Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                        $"Refueling progress: {progress:F1}%. Current: {currentFuel:F1}, Target: {_targetFuelAmount:F1}");
                }
                else
                {
                    Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                        $"Waiting for fuel hose connection. Current: {currentFuel:F1}, Target: {_targetFuelAmount:F1}");
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error processing refueling: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public void HandleFuelHoseStateChange(bool connected)
        {
            try
            {
                if (_isRefueling)
                {
                    if (connected && _isRefuelingPaused)
                    {
                        Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                            "Fuel hose connected - resuming fuel transfer");
                        ResumeRefueling();
                    }
                    else if (!connected && !_isRefuelingPaused)
                    {
                        Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                            "Fuel hose disconnected - pausing fuel transfer");
                        PauseRefueling();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error handling fuel hose state change: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void RequestRefuelingService()
        {
            try
            {
                // Open menu and select refueling
                _menuService.OpenMenu();
                _menuService.SelectMenuItem(3);

                // Handle operator selection if needed
                _menuService.HandleOperatorSelection((int)_model.OperatorDelay);

                Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    "Refueling service requested");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error requesting refueling service: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void SetInitialFuel()
        {
            if (_initialFuelSet)
                return;

            try
            {
                // Get the target fuel amount from the flight plan
                double targetFuel = GetTargetFuelAmount();

                // Store the initial amount
                _prosimInterface.SetProsimVariable("aircraft.refuel.fuelTarget", targetFuel);

                _initialFuelSet = true;

                Logger.Log(LogLevel.Information, nameof(GsxRefuelingService),
                    $"Initial fuel target set to {targetFuel:F1}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error setting initial fuel: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the current fuel amount
        /// </summary>
        public double GetCurrentFuelAmount()
        {
            try
            {
                // Get the current fuel amount from Prosim
                double fuel = Convert.ToDouble(_prosimInterface.GetProsimVariable("aircraft.fuel.total"));
                return fuel;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error getting current fuel amount: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get the target fuel amount
        /// </summary>
        private double GetTargetFuelAmount()
        {
            try
            {
                // First try to get the planned fuel from the dedicated refueling service
                if (_refuelingService != null)
                {
                    double plannedFuel = _refuelingService.PlannedFuel;
                    if (plannedFuel > 0)
                    {
                        Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                            $"Using planned fuel from RefuelingService: {plannedFuel}");
                        return plannedFuel;
                    }
                }

                // If that fails, try other methods...
                var targetRef = _prosimInterface.GetProsimVariable("aircraft.refuel.fuelTarget");
                if (targetRef != null)
                {
                    double targetFuel = Convert.ToDouble(targetRef);
                    if (targetFuel > 0)
                    {
                        Logger.Log(LogLevel.Debug, nameof(GsxRefuelingService),
                            $"Using fuel target from Prosim dataref: {targetFuel}");
                        return targetFuel;
                    }
                }

                // Fallback to a default value
                Logger.Log(LogLevel.Warning, nameof(GsxRefuelingService),
                    "Could not determine fuel target, using default of 10000");
                return 10000; // Default fallback
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error getting target fuel amount: {ex.Message}");
                return 10000; // Default fallback on error
            }
        }

        /// <summary>
        /// Increment the fuel amount
        /// </summary>
        private void IncrementFuelAmount()
        {
            try
            {
                // Get the current fuel amount
                double currentFuel = GetCurrentFuelAmount();

                // Calculate how much fuel to add
                double fuelToAdd = Math.Min(50.0, _targetFuelAmount - currentFuel);

                // Ensure we don't go over the target
                if (fuelToAdd <= 0)
                    return;

                // Add the fuel
                _prosimInterface.SetProsimVariable("aircraft.fuel.total", currentFuel + fuelToAdd);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxRefuelingService),
                    $"Error incrementing fuel amount: {ex.Message}");
            }
        }
    }
}