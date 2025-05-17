using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.GSX.Enums;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of boarding service
    /// </summary>
    public class GsxBoardingService : IGsxBoardingService
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly IGsxSimConnectService _simConnectService;
        private readonly IGsxMenuService _menuService;
        private readonly IDoorControlService _doorControlService;
        private readonly ServiceModel _model;
        private readonly ILogger<GsxBoardingService> _logger;

        private bool _isBoarding = false;
        private bool _isBoardingComplete = false;
        private bool _isBoardingRequested = false;
        private bool _isDeboarding = false;
        private bool _isDeboardingComplete = false;
        private int _plannedPassengers = 0;

        /// <inheritdoc/>
        public bool IsBoardingActive => _isBoarding;

        /// <inheritdoc/>
        public bool IsBoardingComplete => _isBoardingComplete;

        /// <inheritdoc/>
        public bool IsBoardingRequested => _isBoardingRequested;

        /// <inheritdoc/>
        public bool IsDeboarding => _isDeboarding;

        /// <inheritdoc/>
        public bool IsDeboardingComplete => _isDeboardingComplete;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxBoardingService(
            ILogger<GsxBoardingService> logger,
            IProsimInterface prosimInterface,
            IGsxSimConnectService simConnectService,
            IGsxMenuService menuService,
            IDoorControlService doorControlService,
            ServiceModel model)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _doorControlService = doorControlService ?? throw new ArgumentNullException(nameof(doorControlService));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            // Subscribe to boarding/deboarding state changes
            _simConnectService.SubscribeToGsxLvar("FSDT_GSX_BOARDING_STATE", OnBoardingStateChanged);
            _simConnectService.SubscribeToGsxLvar("FSDT_GSX_DEBOARDING_STATE", OnDeboardingStateChanged);

            _logger.LogInformation("GSX Boarding Service initialized");
        }

        /// <summary>
        /// Handler for boarding state changes
        /// </summary>
        private void OnBoardingStateChanged(float newValue, float oldValue, string lvarName)
        {
            _logger.LogDebug("Boarding state changed from {OldValue} to {NewValue}", oldValue, newValue);

            if (newValue != oldValue)
            {
                var status = newValue == (int)GsxServiceState.Completed ? ServiceStatus.Completed :
                            newValue == (int)GsxServiceState.Active ? ServiceStatus.Active :
                            newValue == (int)GsxServiceState.Requested ? ServiceStatus.Requested :
                            ServiceStatus.Inactive;

                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Boarding", status));

                // If boarding is requested, mark as requested
                if (newValue == 4 && !_isBoardingRequested)
                {
                    _isBoarding = false;
                    _isBoardingRequested = true;
                    _isBoardingComplete = false;
                    _logger.LogInformation("Boarding requested");
                }

                // If boarding is completed, mark as complete
                if (newValue == 6 && !_isBoardingComplete)
                {
                    _isBoarding = false;
                    _isBoardingRequested = false;
                    _isBoardingComplete = true;
                    _logger.LogInformation("Boarding completed automatically");
                }

                // If boarding becomes active, mark as active
                if (newValue == 5 && !_isBoarding)
                {
                    _isBoarding = true;
                    _isBoardingRequested = false;
                    _isBoardingComplete = false;
                    _logger.LogInformation("Boarding started automatically");
                }
            }
        }

        /// <summary>
        /// Handler for deboarding state changes
        /// </summary>
        private void OnDeboardingStateChanged(float newValue, float oldValue, string lvarName)
        {
            _logger.LogDebug("Deboarding state changed from {OldValue} to {NewValue}", oldValue, newValue);

            if (newValue != oldValue)
            {
                var status = newValue == (int)GsxServiceState.Completed ? ServiceStatus.Completed :
                            newValue == (int)GsxServiceState.Active ? ServiceStatus.Active :
                            newValue == (int)GsxServiceState.Requested ? ServiceStatus.Requested :
                            ServiceStatus.Inactive;

                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Deboarding", status));

                // If deboarding is completed, mark as complete
                if (newValue == 6 && !_isDeboardingComplete)
                {
                    _isDeboarding = false;
                    _isDeboardingComplete = true;
                    _logger.LogInformation("Deboarding completed automatically");
                }

                // If deboarding becomes active, mark as active
                if (newValue == 5 && !_isDeboarding)
                {
                    _isDeboarding = true;
                    _isDeboardingComplete = false;
                    _logger.LogInformation("Deboarding started automatically");
                }
            }
        }

        /// <inheritdoc/>
        public void StartBoarding()
        {
            _isBoarding = true;
            _isBoardingComplete = false;

            _logger.LogInformation("Boarding service started");
        }

        /// <inheritdoc/>
        public void StopBoarding()
        {
            _isBoarding = false;
            _isBoardingComplete = true;

            // We no longer manipulate doors here

            _logger.LogInformation("Boarding service stopped");
        }

        /// <inheritdoc/>
        public void StartDeboarding()
        {
            _isDeboarding = true;
            _isDeboardingComplete = false;

            _logger.LogInformation("Deboarding service started");
        }

        /// <inheritdoc/>
        public void StopDeboarding()
        {
            _isDeboarding = false;
            _isDeboardingComplete = true;

            // We no longer manipulate doors here

            _logger.LogInformation("Deboarding service stopped");
        }

        /// <inheritdoc/>
        public bool ProcessBoarding(int paxCurrent, int cargoPercent)
        {
            if (!_isBoarding)
                return false;

            // Check if GSX considers boarding complete
            if (_simConnectService.GetBoardingState() == (int)GsxServiceState.Completed)
            {
                _logger.LogInformation("GSX reports boarding completed");

                return true;
            }

            // Check if cargo is fully loaded and passengers are boarded
            if (cargoPercent >= 99 && paxCurrent >= GetPlannedPassengers())
            {
                _logger.LogInformation("Boarding is complete: Cargo {CargoPercent}%, Passengers {CurrentPax}/{PlannedPax}",
                    cargoPercent, paxCurrent, GetPlannedPassengers());

                // Boarding is complete but doors should already be closed at this point
                return true;
            }

            _logger.LogDebug("Boarding progress: Cargo {CargoPercent}%, Passengers {CurrentPax}/{PlannedPax}",
                cargoPercent, paxCurrent, GetPlannedPassengers());

            return false;
        }

        /// <inheritdoc/>
        public bool ProcessDeboarding(int paxCurrent, int cargoPercent)
        {
            if (!_isDeboarding)
                return false;

            // Check if GSX considers deboarding complete
            if (_simConnectService.GetDeboardingState() == (int)GsxServiceState.Completed)
            {
                _logger.LogInformation("GSX reports deboarding completed");

                return true;
            }

            // Check if cargo is fully unloaded and all passengers have deboarded
            if (cargoPercent >= 99 && paxCurrent <= 0)
            {
                _logger.LogInformation("Deboarding is complete: Cargo {CargoPercent}%, Passengers {CurrentPax}/{PlannedPax}",
                    cargoPercent, paxCurrent, GetPlannedPassengers());

                // Deboarding is complete but doors should already be closed at this point
                return true;
            }

            _logger.LogDebug("Deboarding progress: Cargo {CargoPercent}%, Passengers {CurrentPax}/{PlannedPax}",
                cargoPercent, paxCurrent, GetPlannedPassengers());

            return false;
        }

        /// <inheritdoc/>
        public void SetPassengers(int numPax)
        {
            try
            {
                // Set the planned passenger count
                _plannedPassengers = numPax;

                // Update the GSX LVAR
                _simConnectService.WriteGsxLvar("FSDT_GSX_NUMPASSENGERS", numPax);

                _logger.LogInformation("Passenger count set to {PassengerCount}", numPax);

                // Handle crew settings if needed
                if (_model.DisableCrew)
                {
                    _simConnectService.WriteGsxLvar("FSDT_GSX_CREW_NOT_DEBOARDING", 1);
                    _simConnectService.WriteGsxLvar("FSDT_GSX_CREW_NOT_BOARDING", 1);
                    _simConnectService.WriteGsxLvar("FSDT_GSX_PILOTS_NOT_DEBOARDING", 1);
                    _simConnectService.WriteGsxLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1);
                    _simConnectService.WriteGsxLvar("FSDT_GSX_NUMCREW", 0);
                    _simConnectService.WriteGsxLvar("FSDT_GSX_NUMPILOTS", 0);
                    _simConnectService.WriteGsxLvar("FSDT_GSX_CREW_ON_BOARD", 1);

                    _logger.LogInformation("Crew boarding disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting passenger count");
            }
        }

        /// <inheritdoc/>
        public void RequestBoardingService()
        {
            try
            {
                // Open menu and select boarding
                _menuService.OpenMenu();
                _menuService.SelectMenuItem(4);

                // Handle operator selection if needed
                _menuService.HandleOperatorSelection();

                _logger.LogInformation("Boarding service requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting boarding service");
            }
        }

        /// <inheritdoc/>
        public void RequestDeboardingService()
        {
            try
            {
                // Open menu and select deboarding
                _menuService.OpenMenu();
                _menuService.SelectMenuItem(1);

                // Handle operator selection if needed
                _menuService.HandleOperatorSelection();

                _logger.LogInformation("Deboarding service requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting deboarding service");
            }
        }

        /// <inheritdoc/>
        public int GetCurrentPassengers()
        {
            try
            {
                // Get the current passenger count based on boarding/deboarding state
                if (_isBoarding)
                {
                    return _simConnectService.GetBoardedPassengerCount();
                }
                else if (_isDeboarding)
                {
                    return GetPlannedPassengers() - _simConnectService.GetDeboardedPassengerCount();
                }
                else
                {
                    // If neither boarding nor deboarding, return planned count
                    return GetPlannedPassengers();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current passengers");
                return 0;
            }
        }

        /// <inheritdoc/>
        public int GetPlannedPassengers()
        {
            try
            {
                // If we have a cached value, use it
                if (_plannedPassengers > 0)
                    return _plannedPassengers;

                // Otherwise, get from GSX
                int gsxPax = (int)_simConnectService.ReadGsxLvar("FSDT_GSX_NUMPASSENGERS");

                // If GSX has a value, use it
                if (gsxPax > 0)
                {
                    _plannedPassengers = gsxPax;
                    return gsxPax;
                }

                // Fall back to passenger service
                var passengerService = ServiceLocator.PassengerService;
                int plannedPax = passengerService?.GetPlannedPassengers() ?? 0;

                // Cache the value
                _plannedPassengers = plannedPax;

                _logger.LogDebug("Using planned passenger count: {PassengerCount}", _plannedPassengers);

                return plannedPax;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting planned passengers");
                return 0;
            }
        }
    }
}