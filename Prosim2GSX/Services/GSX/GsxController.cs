using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services.GSX.Enums;
using Prosim2GSX.Services.GSX.Implementation;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Text;
using System.Threading;

namespace Prosim2GSX.Services.GSX
{
    /// <summary>
    /// Controller for GSX integration using a service-oriented approach
    /// </summary>
    public class GsxController
    {
        private readonly ILogger<GsxController> _logger;
        private ILoggerFactory _loggerFactory;
        private readonly IGsxFlightStateService _flightStateService;
        private readonly IGsxMenuService _menuService;
        private readonly IGsxGroundServicesService _groundServicesService;
        private readonly IGsxCargoService _cargoService;
        private readonly IGsxCateringService _cateringService;
        private readonly IGsxBoardingService _boardingService;
        private readonly IGsxRefuelingService _refuelingService;
        private readonly IGsxSimConnectService _simConnectService;
        private readonly IProsimInterface _prosimInterface;
        private readonly IRefuelingService _prosimRefuelingService;
        private readonly IFlightPlanService _flightPlanService;
        private readonly IPassengerService _passengerService;
        private readonly ILoadsheetService _loadsheetService;
        private readonly IAudioService _audioService;
        private readonly ServiceModel _model;
        private readonly FlightPlan _flightPlan;
        private DataRefChangedHandler _cockpitDoorHandler;
        private AcarsClient _acarsClient;

        // State tracking variables
        private string _flightPlanID = "0";
        private bool _planePositioned = false;
        private bool _connectCalled = false;
        private bool _pcaCalled = false;
        private bool _initialFuelSet = false;
        private bool _initialFluidsSet = false;
        private bool _firstRun = true;
        private bool _equipmentRemoved = false;
        private bool _pcaRemoved = false;
        private int _delayCounter = 0;
        private int _loadsheetDelay = 0;
        private bool _opsCallsignSet = false;
        private string _opsCallsign = "";

        /// <summary>
        /// The interval between service loop iterations in milliseconds
        /// </summary>
        public int Interval { get; set; } = 1000;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxController(
            ILogger<GsxController> logger,
            ServiceModel model,
            FlightPlan flightPlan,
            IAudioService audioService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Store direct dependencies
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _flightPlan = flightPlan ?? throw new ArgumentNullException(nameof(flightPlan));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));

            // Get services from ServiceLocator
            _prosimInterface = ServiceLocator.ProsimInterface;
            _flightPlanService = ServiceLocator.FlightPlanService;
            _passengerService = ServiceLocator.PassengerService;
            _prosimRefuelingService = ServiceLocator.RefuelingService;
            _loadsheetService = ServiceLocator.LoadsheetService;

            // Get GSX services - add null checking here
            _flightStateService = ServiceLocator.GsxFlightStateService ??
                throw new InvalidOperationException("GsxFlightStateService is not available");
            _menuService = ServiceLocator.GsxMenuService ??
                throw new InvalidOperationException("GsxMenuService is not available");
            _groundServicesService = ServiceLocator.GsxGroundServicesService ??
                throw new InvalidOperationException("GsxGroundServicesService is not available");
            _boardingService = ServiceLocator.GsxBoardingService ??
                throw new InvalidOperationException("GsxBoardingService is not available");
            _refuelingService = ServiceLocator.GsxRefuelingService ??
                throw new InvalidOperationException("GsxRefuelingService is not available");
            _simConnectService = ServiceLocator.GsxSimConnectService ??
                throw new InvalidOperationException("GsxSimConnectService is not available");
            _cateringService = ServiceLocator.GsxCateringService ??
                throw new InvalidOperationException("GsxCateringService is not available");
            _cargoService = ServiceLocator.GsxCargoService ??
                throw new InvalidOperationException("GsxCargoService is not available");

            // Initialize and validate services
            ValidateServices();

            // Initialize service
            Initialize();

            // If test arrival is enabled, update services for arrival scenario
            if (_model.TestArrival)
            {
                ServiceLocator.UpdateAllServices(true, _flightPlan);
            }

            _logger.LogInformation("GSX controller initialized");
        }

        /// <summary>
        /// Validate that all required services are available
        /// </summary>
        private void ValidateServices()
        {
            if (_prosimInterface == null)
                throw new InvalidOperationException("ProsimInterface service is not available");

            if (_flightPlanService == null)
                throw new InvalidOperationException("FlightPlanService service is not available");

            if (_simConnectService == null)
                throw new InvalidOperationException("SimConnect is not available");

            // Note: The GSX-specific services are optional and will be created on demand if needed
        }

        /// <summary>
        /// Initialize the controller
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Create and register cockpit door handler
                _cockpitDoorHandler = new DataRefChangedHandler(OnCockpitDoorStateChanged);

                // Get the data ref monitoring service
                var dataRefService = ServiceLocator.DataRefService;

                // Log current status of monitoring service
                _logger.LogInformation("DataRef monitoring active: {IsMonitoringActive}", dataRefService.IsMonitoringActive);

                // Force start monitoring if needed
                if (!dataRefService.IsMonitoringActive)
                {
                    _logger.LogWarning("DataRef monitoring not active, starting it manually");
                    dataRefService.StartMonitoring();
                }

                // Subscribe to cockpit door state changes
                _logger.LogInformation("Subscribing to cockpit door state changes");
                dataRefService.SubscribeToDataRef("system.switches.S_PED_COCKPIT_DOOR", _cockpitDoorHandler);

                // Subscibe to LVARs for changes
                SubscribeToLVARsForMonitoring();

                // Log successful initialization
                _logger.LogInformation("Successfully subscribed to all required datarefs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initialization");
            }
        }

        /// <summary>
        /// Register data ref change handlers
        /// </summary>
        private void SubscribeToLVARsForMonitoring()
        {
            try
            {
                // Subscribe to essential SimConnect variables
                if (_simConnectService != null)
                {
                    // These are now handled by IGsxSimConnectService
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering data ref handlers");
            }
        }

        /// <summary>
        /// Main service loop entry point
        /// </summary>
        public void RunServices()
        {
            try
            {
                // Update all services with the latest data
                ServiceLocator.UpdateAllServices(false, _flightPlan);

                // First, handle the state-specific processing
                bool simOnGround = _simConnectService.IsSimOnGround();

                // Special case: In PREFLIGHT state, check if loaded in flight or with engines running
                if (_flightStateService.CurrentFlightState == FlightState.PREFLIGHT)
                {
                    bool enginesRunning = _flightPlanService.EnginesRunning;
                    if (!simOnGround || enginesRunning)
                    {
                        HandleInFlightStart();
                        return;
                    }
                }

                // Handle state transitions for flight
                if (_flightStateService.CurrentFlightState <= FlightState.FLIGHT)
                {
                    // TAXIOUT -> FLIGHT
                    if (_flightStateService.CurrentFlightState <= FlightState.TAXIOUT && !simOnGround)
                    {
                        // In-flight restart
                        if (_flightStateService.CurrentFlightState <= FlightState.DEPARTURE)
                        {
                            _logger.LogInformation("In-Flight restart detected");
                            ServiceLocator.UpdateAllServices(true, _flightPlan);
                            _flightPlanID = _flightPlanService.FlightPlanID;
                        }

                        _flightStateService.TransitionToState(FlightState.FLIGHT);
                        _logger.LogInformation("State Change: Taxi-Out -> Flight");
                        Interval = 180000; // Longer interval during flight
                        return;
                    }

                    // FLIGHT -> TAXIIN
                    else if (_flightStateService.CurrentFlightState == FlightState.FLIGHT && simOnGround)
                    {
                        _flightStateService.TransitionToState(FlightState.TAXIIN);
                        _logger.LogInformation("State Change: Flight -> Taxi-In (Waiting for Engines stopped and Beacon off)");

                        Interval = 2500;

                        if (_model.TestArrival)
                            _flightPlanID = _flightPlanService.FlightPlanID;

                        // Reset for arrival
                        _pcaCalled = false;
                        _connectCalled = false;
                        return;
                    }
                }

                // Handle the current flight state in more detail
                var currentState = _flightStateService.CurrentFlightState;

                switch (currentState)
                {
                    case FlightState.PREFLIGHT:
                        HandlePreflightState();
                        break;

                    case FlightState.DEPARTURE:
                        HandleDepartureState();
                        break;

                    case FlightState.TAXIOUT:
                        // Handled by transitions above
                        break;

                    case FlightState.FLIGHT:
                        // Handled by transitions above
                        break;

                    case FlightState.TAXIIN:
                        HandleTaxiinState();
                        break;

                    case FlightState.ARRIVAL:
                        HandleArrivalState();
                        break;

                    case FlightState.TURNAROUND:
                        HandleTurnaroundState();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RunServices");
            }
        }

        /// <summary>
        /// Handle in-flight start scenario
        /// </summary>
        private void HandleInFlightStart()
        {
            ServiceLocator.UpdateAllServices(true, _flightPlan);
            _flightPlanID = _flightPlanService.FlightPlanID;

            _flightStateService.TransitionToState(FlightState.FLIGHT);
            Interval = 180000;
            _logger.LogInformation("Current State is Flight.");

            bool simOnGround = _simConnectService.IsSimOnGround();
            bool enginesRunning = _flightPlanService.EnginesRunning;

            if (simOnGround && enginesRunning)
            {
                _logger.LogInformation("Starting on Runway - Removing Ground Equipment");
                _groundServicesService.DisconnectAllGroundServices();
            }
        }

        /// <summary>
        /// Handle the PREFLIGHT state
        /// </summary>
        private void HandlePreflightState()
        {
            bool batteryOn = _prosimInterface.GetProsimVariable("system.switches.S_OH_ELEC_BAT1") == 1;

            if (!batteryOn)
                return;

            // Setup ACARS if enabled
            if (_model.UseAcars && !_opsCallsignSet)
            {
                try
                {
                    _opsCallsign = FlightCallsignToOpsCallsign(_flightPlanService.FlightNumber);
                    _acarsClient = new AcarsClient(_loggerFactory.CreateLogger<AcarsClient>(), _opsCallsign, _model.AcarsSecret, _model.AcarsNetworkUrl);
                    _opsCallsignSet = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to set opsCallSign");
                }
            }

            // Handle test arrival mode
            if (_model.TestArrival)
            {
                _flightStateService.TransitionToState(FlightState.FLIGHT);
                ServiceLocator.UpdateAllServices(true, _flightPlan);
                _logger.LogInformation("Test Arrival - Plane is in 'Flight'");
                return;
            }

            // Set interval
            Interval = 1000;

            // Check if Couatl engine is running
            if (!_simConnectService.IsCouatlRunning())
            {
                _logger.LogInformation("Couatl Engine not running");
                return;
            }

            // Handle repositioning
            if (_model.RepositionPlane && !_planePositioned)
            {
                RepositionPlane();
                return;
            }
            else if (!_model.RepositionPlane && !_planePositioned)
            {
                _planePositioned = true;
                _logger.LogInformation("Repositioning was skipped (disabled in Settings)");
            }

            // Auto-connect jetway/stairs if enabled
            if (_model.AutoConnect && !_connectCalled)
            {
                _groundServicesService.CallJetwayStairs(_model.JetwayOnly);
                _connectCalled = true;
                return;
            }

            // Connect PCA if enabled
            if (_model.ConnectPCA && !_pcaCalled &&
                (!_model.PcaOnlyJetways || (_model.PcaOnlyJetways && _simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY") != 2)))
            {
                _logger.LogInformation("Connecting PCA");
                _groundServicesService.ConnectPca();
                _pcaCalled = true;
                return;
            }

            // First run initialization
            if (_firstRun)
            {
                _logger.LogInformation("Setting GPU and Chocks");
                _groundServicesService.SetChocks(true);
                _groundServicesService.ConnectGpu();
                _logger.LogInformation("State: Preparation (Waiting for Flightplan import)");
                _firstRun = false;
            }

            // Check if flight plan is loaded
            if (_flightPlanService.IsFlightplanLoaded())
            {
                _flightStateService.TransitionToState(FlightState.DEPARTURE);
                _flightPlanID = _flightPlanService.FlightPlanID;

                // Add null check for passenger service
                if (_passengerService != null)
                {
                    _boardingService.SetPassengers(_passengerService.GetPlannedPassengers());
                }
                else
                {
                    // Fallback to default value
                    _logger.LogWarning("PassengerService is null. Using default passenger count.");
                    _boardingService.SetPassengers(0);
                }

                _logger.LogInformation("State Change: Preparation -> DEPARTURE (Waiting for Refueling and Boarding)");
            }
        }

        /// <summary>
        /// Handle the DEPARTURE state
        /// </summary>
        private async void HandleDepartureState()
        {
            // Check refueling, boarding, and cargo states
            bool refuelingComplete = _refuelingService.IsRefuelingComplete;
            bool initialFuelSet = _refuelingService.IsInitialFuelSet;
            bool initialFluidsSet = _refuelingService.IsHydraulicFluidsSet;
            bool refuelingRequested = _refuelingService.IsRefuelingRequested;
            bool refuelingActive = _refuelingService.IsRefuelingActive;
            bool refuelingPaused = _refuelingService.IsRefuelingPaused;
            bool fuelHoseConnected = _refuelingService.IsFuelHoseConnected;
            bool boardingComplete = _boardingService.IsBoardingComplete;
            bool boardingRequested = _boardingService.IsBoardingRequested;
            bool cateringComplete = _cateringService.IsCateringComplete;
            bool cateringRequested = _cateringService.IsCateringRequested;
            bool fuelTargetSet = _prosimInterface.GetStatusFunction("aircraft.refuel.fuelTarget") >= 1 && _prosimInterface.GetStatusFunction("efb.plannedfuel") >= 1;
            bool cargoTargeteSet = _prosimInterface.GetStatusFunction("efb.plannedCargoKg") >= 1;
            bool passengerTargetSet = _prosimInterface.GetProsimVariable("efb.passengers.booked") == _prosimInterface.GetProsimVariable("aircraft.passengers.seatOccupation");
            bool preliminaryLoadsheetRequested = _loadsheetService.PreliminaryLoadsheetRequested;
            bool finalLoadsheetRequested = _loadsheetService.FinalLoadsheetRequested;

            // First, check if preliminary loadsheet needs to be generated
            if (!preliminaryLoadsheetRequested && fuelHoseConnected)
            {
                // Log current conditions for debugging
                _logger.LogDebug(
                    "Loadsheet conditions: Fuel Target Set={FuelTargetSet}, Cargo Target Set: {CargoTargetSet}, " +
                    "Hose Connected: {HoseConnected}, Refueling Active: {RefuelingActive}, Refueling Complete: {RefuelingComplete}",
                    fuelTargetSet, cargoTargeteSet, fuelHoseConnected, refuelingActive, refuelingComplete);

                // Only generate loadsheet when all conditions are met
                if (fuelTargetSet && cargoTargeteSet && refuelingActive && !refuelingComplete)
                {
                    _logger.LogInformation("Generating preliminary loadsheet - all conditions met");

                    // Update passenger statistics before generating loadsheet
                    _passengerService.UpdatePassengerStatistics();

                    // Now generate the loadsheet
                    await _loadsheetService.GenerateLoadsheet("Preliminary");
                    // Update local state:
                    preliminaryLoadsheetRequested = true;
                    return;
                }
            }

            if (!refuelingComplete || !boardingComplete || !cateringComplete)
            {
                // Handle refueling service
                if (_model.AutoRefuel)
                {
                    if (!initialFuelSet)
                        _refuelingService.SetInitialFuel();

                    if (_model.SetSaveHydraulicFluids && !initialFluidsSet)
                        _refuelingService.SetHydraulicFluidLevels();

                    // Request refueling if needed
                    _logger.LogDebug("_refuelingRequested: {RefuelingRequested}, GetRefuelingState: {RefuelingState}",
                        refuelingRequested, _simConnectService.GetRefuelingState());

                    if (!refuelingRequested && _simConnectService.GetRefuelingState() != 6)
                    {
                        _logger.LogInformation("Calling Refuel Service");
                        _refuelingService.RequestRefueling();
                        return;
                    }

                    if (_model.CallCatering && !cateringRequested && _simConnectService.GetCateringState() != 6)
                    {
                        _logger.LogInformation("Requesting catering service");
                        _cateringService.RequestCateringService();
                        return;
                    }

                    // If refueling not active and GSX state is active, set active
                    _logger.LogDebug("_refuelingActive: {RefuelingActive}, _refuelingFinished: {RefuelingComplete}, GetRefuelingState: {RefuelingState}",
                        refuelingActive, refuelingComplete, _simConnectService.GetRefuelingState());

                    if (!refuelingActive && _simConnectService.GetRefuelingState() == 5)
                        _refuelingService.SetRefuelingActive();

                    // Process refueling if active
                    else if (refuelingActive)
                    {
                        _logger.LogDebug("_refuelingActive: {RefuelingActive}, _refuelingPaused: {RefuelingPaused}",
                            refuelingActive, refuelingPaused);

                        // Only perform active refueling when not paused
                        if (!refuelingPaused && _simConnectService.ReadGsxLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1)
                            _refuelingService.ProcessRefueling();


                        // Add state transition check for GSX refueling state
                        if ((int)_simConnectService.ReadGsxLvar("FSDT_GSX_REFUELING_STATE") == 6) // Check if GSX considers refueling completed
                        {
                            if (!refuelingComplete)
                                _refuelingService.StopRefueling();
                        }
                    }

                    if (refuelingComplete && !boardingRequested)
                    {
                        _logger.LogDebug(
                            "Refueling finished. AutoBoarding: {AutoBoarding}, CateringFinished: {CateringFinished}, " +
                            "CallCatering: {CallCatering}, DelayCounter: {DelayCounter}",
                            _model.AutoBoarding, cateringComplete, _model.CallCatering, _delayCounter);
                    }
                }

                // Handle boarding service
                if (_model.AutoBoarding && !_boardingService.IsBoardingActive && !boardingComplete)
                {
                    // Wait for refueling and catering to complete before boarding
                    if (refuelingComplete)
                    {
                        if (_delayCounter == 0)
                            _logger.LogInformation("Waiting 90s before calling Boarding");

                        if (_delayCounter < 90)
                        {
                            _delayCounter++;
                            _logger.LogDebug("Boarding delay counter: {DelayCounter}/90", _delayCounter);
                        }
                        else
                        {
                            _boardingService.SetPassengers(_passengerService.GetPlannedPassengers());
                            _boardingService.RequestBoardingService();
                            _delayCounter = 0;
                        }
                        return;
                    }
                }

                // Process boarding if active
                if (_boardingService.IsBoardingActive)
                {
                    int paxCurrent = _boardingService.GetCurrentPassengers();
                    int cargoPercent = _simConnectService.GetCargoBoardingPercentage();

                    if (_boardingService.ProcessBoarding(paxCurrent, cargoPercent))
                    {
                        _boardingService.StopBoarding();
                    }
                }

                return;
            }

            // If both boarding and refueling are complete, handle final loadsheet and departure
            if (refuelingComplete && boardingComplete)
            {
                // Handle final loadsheet generation
                if (!finalLoadsheetRequested)
                {
                    if (_loadsheetDelay == 0)
                    {
                        _loadsheetDelay = new Random().Next(90, 150);
                        _delayCounter = 0;
                        _logger.LogInformation("Final Loadsheet in {Delay}s", _loadsheetDelay);
                    }

                    if (_delayCounter < _loadsheetDelay)
                    {
                        _delayCounter++;
                        return;
                    }
                    else
                    {
                        _loadsheetService.GenerateLoadsheet("Final");
                        return;
                    }
                }

                // Handle equipment removal if loadsheet is sent
                if (!_equipmentRemoved)
                {
                    bool parkingBrakeSet = _prosimInterface.GetStatusFunction("system.switches.S_MIP_PARKING_BRAKE") == 1;
                    bool beaconOn = _prosimInterface.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") == 1;
                    bool extPowerAvail = _prosimInterface.GetStatusFunction("system.indicators.I_OH_ELEC_EXT_PWR_L") == 0;

                    if (parkingBrakeSet && beaconOn && extPowerAvail)
                    {
                        _equipmentRemoved = true;

                        _logger.LogInformation("Preparing for Pushback - removing Equipment");

                        // Remove jetway/stairs if needed
                        int jetwayState = (int)_simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY");
                        int jetwayOpState = (int)_simConnectService.ReadGsxLvar("FSDT_GSX_OPERATEJETWAYS_STATE");
                        int departState = _simConnectService.GetDepartureState();

                        if (departState < 4 && jetwayState != 2 && jetwayState == 5 && jetwayOpState < 3)
                        {
                            _groundServicesService.RemoveJetwayStairs();
                        }

                        // Disconnect all ground services
                        _groundServicesService.DisconnectAllGroundServices();
                    }
                }

                // Transition to TAXIOUT when equipment is removed
                if (_equipmentRemoved)
                {
                    _flightStateService.TransitionToState(FlightState.TAXIOUT);
                    EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("GPU", ServiceStatus.Inactive));
                    EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("PCA", ServiceStatus.Inactive));
                    EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Chocks", ServiceStatus.Inactive));
                    EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Inactive));

                    _logger.LogInformation("State Change: DEPARTURE -> Taxi-Out");

                    // Reset counters
                    _loadsheetDelay = 0;
                    _delayCounter = 0;

                    // Increase interval for taxiout
                    Interval = 60000;
                }
            }
        }

        /// <summary>
        /// Handle the TAXIIN state
        /// </summary>
        private void HandleTaxiinState()
        {
            // Check arrival conditions
            double engine1 = _prosimInterface.GetProsimVariable("aircraft.engine1.raw");
            double engine2 = _prosimInterface.GetProsimVariable("aircraft.engine2.raw");
            bool enginesAreOff = engine1 < 18.0D && engine2 < 18.0D;
            bool parkingBrakeSet = _prosimInterface.GetStatusFunction("system.switches.S_MIP_PARKING_BRAKE") == 1;
            bool beaconIsOff = _prosimInterface.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") == 0;
            bool groundSpeedNearZero = _simConnectService.ReadGsxLvar("GPS GROUND SPEED") < 0.5;

            // Log the current state of each condition for debugging
            _logger.LogDebug(
                "TAXIIN conditions: Engines Off={EnginesOff}, Parking Brake={ParkingBrake}, " +
                "Beacon Off={BeaconOff}, Low Speed={LowSpeed}",
                enginesAreOff, parkingBrakeSet, beaconIsOff, groundSpeedNearZero);

            // Check for critical arrival conditions
            if (enginesAreOff && parkingBrakeSet)
            {
                // Additional check for low ground speed to ensure aircraft has fully stopped
                if (groundSpeedNearZero)
                {
                    _logger.LogInformation("Aircraft appears to be at final parking position, initiating arrival services");

                    // Transition to ARRIVAL state
                    _flightStateService.TransitionToState(FlightState.ARRIVAL);

                    // Set up ground services for arrival
                    SetupArrivalServices();
                }
                else
                {
                    _logger.LogDebug("Waiting for aircraft to come to a complete stop");
                }
            }
        }

        /// <summary>
        /// Handle the ARRIVAL state
        /// </summary>
        private void HandleArrivalState()
        {
            // Check if deboarding is active
            int deboardState = _simConnectService.GetDeboardingState();

            if (deboardState >= 4)
            {
                // Start deboarding if not already started
                if (!_boardingService.IsDeboarding)
                {
                    _boardingService.StartDeboarding();
                }

                // Process deboarding
                int paxCurrent = _boardingService.GetCurrentPassengers();
                int cargoPercent = _simConnectService.GetCargoDeboardingPercentage();

                if (_boardingService.ProcessDeboarding(paxCurrent, cargoPercent) || deboardState == 6)
                {
                    // Deboarding complete, transition to TURNAROUND
                    _boardingService.StopDeboarding();
                    _flightStateService.TransitionToState(FlightState.TURNAROUND);
                    _logger.LogInformation("State Change: Arrival -> Turn-Around (Waiting for new Flightplan)");
                    Interval = 10000;
                }
            }
        }

        /// <summary>
        /// Handle the TURNAROUND state
        /// </summary>
        private void HandleTurnaroundState()
        {
            // Check if a new flight plan is loaded
            if (_flightPlanService.IsFlightplanLoaded() && _flightPlanService.FlightPlanID != _flightPlanID)
            {
                _flightPlanID = _flightPlanService.FlightPlanID;

                // Update ACARS client if needed
                if (_model.UseAcars)
                {
                    _acarsClient.SetCallsign(FlightCallsignToOpsCallsign(_flightPlanService.FlightNumber));
                }

                // Reset loadsheet state
                _loadsheetService.ResetLoadsheets();

                // Reset refueling state for new flight
                _initialFuelSet = false;

                // Reset all state tracking variables
                _flightStateService.TransitionToState(FlightState.DEPARTURE);
                _planePositioned = true;
                _connectCalled = true;
                _pcaCalled = true;
                _initialFuelSet = false;
                _initialFluidsSet = false;
                _equipmentRemoved = false;
                _pcaRemoved = false;
                _delayCounter = 0;
                _loadsheetDelay = 0;

                _logger.LogInformation("State Change: Turn-Around -> DEPARTURE (Waiting for Refueling and Boarding)");
            }
        }

        /// <summary>
        /// Setup arrival services
        /// </summary>
        private void SetupArrivalServices()
        {
            // First, set ground services
            _logger.LogInformation("Setting GPU and Chocks");
            _groundServicesService.SetChocks(true);
            _groundServicesService.ConnectGpu();

            // Load passenger data for deboarding
            _boardingService.SetPassengers(_passengerService.GetPlannedPassengers());

            // Handle beacon state check separately - if beacon is still on, don't connect PCA or call stairs/jetway
            if (_prosimInterface.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") == 1)
            {
                _logger.LogInformation("Waiting for beacon to be turned off before connecting PCA or calling boarding equipment");
                return;
            }

            // Connect boarding equipment if not already called
            if (_model.AutoConnect && !_connectCalled)
            {
                _groundServicesService.CallJetwayStairs(_model.JetwayOnly);
                _connectCalled = true;
                // Return to allow time for the jetway/stairs to connect before proceeding
                return;
            }

            // Connect PCA after jetway/stairs if needed
            if (_model.ConnectPCA && !_pcaCalled &&
                (!_model.PcaOnlyJetways || (_model.PcaOnlyJetways && _simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY") != 2)))
            {
                _logger.LogInformation("Connecting PCA");
                _groundServicesService.ConnectPca();
                _pcaCalled = true;
                // Return to allow PCA to connect
                return;
            }

            // Now that all services are set up, initiate deboarding if needed
            if (_model.AutoDeboarding && _simConnectService.GetDeboardingState() < 4)
            {
                _logger.LogInformation("Calling Deboarding Service");
                _boardingService.RequestDeboardingService();
            }

            // Save fuel and hydraulic state if enabled
            if (_model.SetSaveFuel)
            {
                try
                {
                    // Get fuel amount directly from Prosim interface instead
                    double arrivalFuel = Convert.ToDouble(_prosimInterface.GetProsimVariable("aircraft.fuel.total"));
                    _model.SavedFuelAmount = arrivalFuel;
                    _logger.LogInformation("Saved arrival fuel amount: {ArrivalFuel}", arrivalFuel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving fuel amount");
                }
            }
        }

        /// <summary>
        /// Reposition the aircraft
        /// </summary>
        private void RepositionPlane()
        {
            _logger.LogInformation("Waiting {Delay}s before Repositioning...", _model.RepositionDelay);

            _groundServicesService.SetChocks(true);

            // Sleep for the configured delay
            Thread.Sleep((int)(_model.RepositionDelay * 1000.0f));

            _logger.LogInformation("Repositioning Plane");

            // Open the menu
            _menuService.OpenMenu();
            Thread.Sleep(100);

            // Select repositioning option (typically 10)
            _menuService.SelectMenuItem(10);
            Thread.Sleep(250);

            // Select first repositioning option (typically 1)
            _menuService.SelectMenuItem(1);

            // Mark as positioned
            _planePositioned = true;

            // Wait for the repositioning to complete
            Thread.Sleep(1500);
        }

        /// <summary>
        /// Convert flight callsign to operations callsign
        /// </summary>
        private string FlightCallsignToOpsCallsign(string flightNumber)
        {
            _logger.LogDebug("Flight Number obtained from flight plan: {FlightNumber}", flightNumber);

            var count = 0;

            // Count leading letters
            foreach (char c in flightNumber)
            {
                if (!char.IsLetter(c))
                {
                    break;
                }

                ++count;
            }

            // Remove the airline code
            StringBuilder sb = new StringBuilder(flightNumber, 8);
            sb.Remove(0, count);

            // Limit to last 4 digits if longer
            if (sb.Length >= 5)
            {
                sb.Remove(0, (sb.Length - 4));
            }

            // Add OPS prefix
            sb.Insert(0, "OPS");

            _logger.LogDebug("Changed OPS callsign: {Callsign}", sb.ToString());

            return sb.ToString();
        }

        /// <summary>
        /// Handler for cockpit door state changes
        /// </summary>
        /// <param name="dataRef">The dataref name</param>
        /// <param name="oldValue">The old value</param>
        /// <param name="newValue">The new value</param>
        private void OnCockpitDoorStateChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            try
            {
                // Convert value to int to make sure we're dealing with the same type
                int newDoorState = Convert.ToInt32(newValue);
                int oldDoorState = Convert.ToInt32(oldValue);

                // Only process if value actually changed
                if (newDoorState != oldDoorState)
                {
                    // Determine door state: 0=Normal/Closed, 1=Unlock/Open, 2=Lock/Closed
                    bool doorOpen = newDoorState == 1;

                    // Update GSX LVAR to match door state (0=closed, 1=open)
                    _simConnectService.WriteGsxLvar("FSDT_GSX_COCKPIT_DOOR_OPEN", doorOpen ? 1 : 0);

                    _logger.LogInformation("Cockpit door state changed to {State} ({Status})",
                        newDoorState, (doorOpen ? "opened" : "closed"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cockpit door state change");
            }
        }
    }
}
