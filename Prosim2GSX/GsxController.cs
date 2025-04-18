﻿﻿﻿﻿﻿﻿﻿﻿﻿using Microsoft.FlightSimulator.SimConnect;
using Microsoft.Win32;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services.WeightAndBalance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX
{
    public enum FlightState
    {
        PREFLIGHT = 0,
        DEPARTURE,
        TAXIOUT,
        FLIGHT,
        TAXIIN,
        ARRIVAL,
        TURNAROUND
    }

    public class GsxController
    {
        private readonly string pathMenuFile = @"\MSFS\fsdreamteam-gsx-pro\html_ui\InGamePanels\FSDT_GSX_Panel\menu";
        private readonly string registryPath = @"HKEY_CURRENT_USER\SOFTWARE\FSDreamTeam";
        private readonly string registryValue = @"root";
        private readonly string gsxProcess = "Couatl64_MSFS";
        private string menuFile = "";

        private readonly IAudioService _audioService;

        private readonly Services.WeightAndBalance.ProsimLoadsheetService _loadsheetService;

        private DataRefChangedHandler cockpitDoorHandler;
        private DataRefChangedHandler onGPUStateChanged;
        private DataRefChangedHandler onPCAStateChanged;
        private DataRefChangedHandler onChocksStateChanged;

        private FlightState _state = FlightState.PREFLIGHT;
        private FlightState _previousState = FlightState.PREFLIGHT;
        
        /// <summary>
        /// Gets or sets the current flight state
        /// </summary>
        public FlightState CurrentFlightState
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    FlightState oldState = _state;
                    _state = value;
                    // Publish the event when the state changes
                    EventAggregator.Instance.Publish(new FlightPhaseChangedEvent(oldState, _state));
                    Logger.Log(LogLevel.Information, "GsxController", $"State Change: {oldState} -> {_state}");
                }
            }
        }

        private bool aftCargoDoorOpened = false;
        private bool aftRightDoorOpened = false;
        private bool forwardRightDoorOpened = false;
        private bool boarding = false;
        private bool forwardCargoDoorOpened = false;
        private bool boardFinished = false;
        private bool boardingRequested = false;
        private bool cateringFinished = false;
        private bool cateringRequested = false;
        private bool connectCalled = false;
        private bool deboarding = false;
        private int delay = 0;
        private int delayCounter = 0;
        private bool equipmentRemoved = false;
        private double finalFuel = 0d;
        private bool finalLoadsheetSend = false;
        private double finalMacTow = 00.0d;
        private double finalMacZfw = 00.0d;
        private int finalPax = 0;
        private double finalTow = 00.0d;
        private double finalZfw = 00.0d;
        private bool firstRun = true;
        private string flightPlanID = "0";
        private bool initialFuelSet = false;
        private bool initialFluidsSet = false;
        private double macZfw = 0.0d;
        private bool operatorWasSelected = false;
        private string opsCallsign = "";
        private bool opsCallsignSet = false;
        private int paxPlanned = 0;
        private bool pcaCalled = false;
        private bool pcaRemoved = false;
        private bool planePositioned = false;
        private double prelimFuel = 0d;
        private bool prelimLoadsheet = false;
        
        // Loadsheet generation state tracking
        private enum LoadsheetState { NotStarted, Waiting, Generating, Completed, Failed }
        private LoadsheetState _prelimLoadsheetState = LoadsheetState.NotStarted;
        private DateTime _nextLoadsheetAttemptTime = DateTime.MinValue;
        private Task<LoadsheetResult> _currentLoadsheetTask = null;
        private string _loadsheetFlightPlanId = null;
        private double prelimMacTow = 00.0d;
        private double prelimMacZfw = 00.0d;
        private int prelimPax = 0;
        private double prelimTow = 00.0d;
        private double prelimZfw = 00.0d;
        private bool pushFinished = false;
        private bool pushNwsDisco = false;
        private bool pushRunning = false;
        private bool refuelFinished = false;
        private bool refueling = false;
        private bool refuelPaused = false;
        private bool refuelRequested = false;

        private AcarsClient AcarsClient;
        private MobiSimConnect SimConnect;
        private ProsimController ProsimController;
        private ServiceModel Model;
        private FlightPlan FlightPlan;

        // State tracking variables
        private float cateringState = 0;

        // Define constants for different service states
        private const float GSX_SERVICE_AVAILABLE = 1;
        private const float GSX_SERVICE_UNAVAILABLE = 2;
        private const float GSX_SERVICE_BYPASSED = 3;
        private const float GSX_SERVICE_REQUESTED = 4;
        private const float GSX_SERVICE_ACTIVE = 5;
        private const float GSX_SERVICE_COMPLETED = 6;

        private const float SERVICE_TOGGLE_ON = 1;
        private const float SERVICE_TOGGLE_OFF = 0;

        private bool cockpitDoorStateChanged = false;
        private int lastCockpitDoorState = -1;

        private int currentDeboardState = 0;

        // Dictionary to map service toggle LVAR names to door operations
        private readonly Dictionary<string, Action> serviceToggles = new Dictionary<string, Action>();

        public int Interval { get; set; } = 1000;

        public GsxController(ServiceModel model, ProsimController prosimController, FlightPlan flightPlan, IAudioService audioService)
        {
            Model = model;
            ProsimController = prosimController;
            FlightPlan = flightPlan;
            _audioService = audioService;

            cockpitDoorHandler = new DataRefChangedHandler(OnCockpitDoorStateChanged);
            onGPUStateChanged = new DataRefChangedHandler(OnGPUStateChanged);
            onPCAStateChanged = new DataRefChangedHandler(OnPCAStateChanged);
            onChocksStateChanged = new DataRefChangedHandler(OnChocksStateChanged);


            SimConnect = IPCManager.SimConnect;

            // Initialize loadsheet service
            _loadsheetService = new ProsimLoadsheetService(prosimController);
            _loadsheetService.SubscribeToLoadsheetChanges();

            SimConnect.SubscribeSimVar("SIM ON GROUND", "Bool");
            SimConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_STATE", OnDeboardingStateChanged);
            SimConnect.SubscribeLvar("FSDT_GSX_CATERING_STATE", OnCateringStateChanged);
            SimConnect.SubscribeLvar("FSDT_GSX_COCKPIT_DOOR_OPEN");
            SimConnect.SubscribeLvar("FSDT_GSX_REFUELING_STATE", OnRefuelingStateChanged);
            SimConnect.SubscribeLvar("FSDT_GSX_BOARDING_STATE", OnBoardingStateChanged);
            SimConnect.SubscribeLvar("FSDT_GSX_DEPARTURE_STATE", OnDepartureStateChanged);
            SimConnect.SubscribeLvar("FSDT_GSX_DEICING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS");
            SimConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL");
            SimConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
            SimConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO", OnCargoLoadingChanged);
            SimConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO", OnCargoLoadingChanged);
            SimConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
            SimConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");
            SimConnect.SubscribeLvar("FSDT_GSX_FUELHOSE_CONNECTED", OnFuelHoseStateChanged);
            SimConnect.SubscribeLvar("FSDT_VAR_EnginesStopped");
            SimConnect.SubscribeLvar("FSDT_GSX_COUATL_STARTED");
            SimConnect.SubscribeLvar("FSDT_GSX_JETWAY", OnJetwayStateChanged);
            SimConnect.SubscribeLvar("FSDT_GSX_OPERATEJETWAYS_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_STAIRS", OnStairsStateChanged);
            SimConnect.SubscribeLvar("FSDT_GSX_OPERATESTAIRS_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_BYPASS_PIN");
            SimConnect.SubscribeLvar("FSDT_VAR_Frozen");
            SimConnect.SubscribeLvar("S_MIP_PARKING_BRAKE");
            SimConnect.SubscribeLvar("S_OH_EXT_LT_BEACON");
            SimConnect.SubscribeLvar("I_OH_ELEC_EXT_PWR_L");
            SimConnect.SubscribeLvar("I_OH_ELEC_APU_START_U");
            SimConnect.SubscribeLvar("S_OH_PNEUMATIC_APU_BLEED");
            SimConnect.SubscribeLvar("I_FCU_TRACK_FPA_MODE");
            SimConnect.SubscribeLvar("I_FCU_HEADING_VS_MODE");
            SimConnect.SubscribeLvar("I_ASP_INT_REC");
            SimConnect.SubscribeLvar("A_ASP_INT_VOLUME");
            SimConnect.SubscribeLvar("I_ASP_VHF_1_REC");
            SimConnect.SubscribeLvar("A_ASP_VHF_1_VOLUME");
            SimConnect.SubscribeLvar("A_FC_THROTTLE_LEFT_INPUT");
            SimConnect.SubscribeLvar("A_FC_THROTTLE_RIGHT_INPUT");
            SimConnect.SubscribeSimVar("GPS GROUND SPEED", "Meters per second");
            SimConnect.SubscribeEnvVar("ZULU TIME", "Seconds");

            ProsimController.SubscribeToDataRef("system.switches.S_PED_COCKPIT_DOOR", cockpitDoorHandler);
            ProsimController.SubscribeToDataRef("groundservice.groundpower", onGPUStateChanged);
            ProsimController.SubscribeToDataRef("groundservice.preconditionedAir", onPCAStateChanged);
            ProsimController.SubscribeToDataRef("efb.chocks", onChocksStateChanged);
            


            // Initialize the service toggle mapping
            serviceToggles.Add("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE", () => OperateFrontDoor());
            serviceToggles.Add("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE", () => OperateAftDoor());
            serviceToggles.Add("FSDT_GSX_AIRCRAFT_CARGO_1_TOGGLE", () => OperateFrontCargoDoor());
            serviceToggles.Add("FSDT_GSX_AIRCRAFT_CARGO_2_TOGGLE", () => OperateAftCargoDoor());


            // Subscribe to all service toggle LVARs
            foreach (var toggleLvar in serviceToggles.Keys)
            {
                SimConnect.SubscribeLvar(toggleLvar, OnServiceToggleChanged);
            }

            string regPath = (string)Registry.GetValue(registryPath, registryValue, null) + pathMenuFile;
            if (Path.Exists(regPath))
                menuFile = regPath;

            if (Model.TestArrival)
                ProsimController.Update(true);
        }

        private string FlightCallsignToOpsCallsign(string flightNumber)
        {
            Logger.Log(LogLevel.Debug, "GsxController:FlightCallsignToOpsCallsign", $"Flight Number obtained from flight plan: {flightNumber}");

            var count = 0;

            foreach (char c in flightNumber)
            {
                if (!char.IsLetter(c))
                {
                    break;
                }

                ++count;
            }

            StringBuilder sb = new StringBuilder(flightNumber, 8);
            sb.Remove(0, count);

            if (sb.Length >= 5)
            {
                sb.Remove(0, (sb.Length - 4));
            }

            sb.Insert(0, "OPS");
            Logger.Log(LogLevel.Debug, "GsxController:FlightCallsignToOpsCallsign", $"Changed OPS callsign: {sb.ToString()}");

            return sb.ToString();
        }

        public void RunServices()
        {
            bool simOnGround = SimConnect.ReadSimVar("SIM ON GROUND", "Bool") != 0.0f;
            ProsimController.Update(false);

            if (operatorWasSelected)
            {
                MenuOpen();
                operatorWasSelected = false;
            }

            //PREPARATION (On-Ground and Engines not running)
            if (_state == FlightState.PREFLIGHT && simOnGround && !ProsimController.enginesRunning && ProsimController.Interface.GetProsimVariable("system.switches.S_OH_ELEC_BAT1") == 1)
            {
                if (Model.UseAcars && !opsCallsignSet)
                {
                    
                    try
                    {
                        opsCallsign = FlightCallsignToOpsCallsign(ProsimController.flightNumber);
                        this.AcarsClient = new AcarsClient(opsCallsign, Model.AcarsSecret, Model.AcarsNetworkUrl);
                        opsCallsignSet = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "GsxController:RunServices", $"Unable to set opsCallSign - Error: {ex.Message}");
                    }

                }
            if (Model.TestArrival)
                {
                    CurrentFlightState = FlightState.FLIGHT;
                    ProsimController.Update(true);
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Test Arrival - Plane is in 'Flight'");
                    return;
                }
                Interval = 1000;

                if (SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Couatl Engine not running");

                    return;
                }

                if (Model.RepositionPlane && !planePositioned)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Waiting {Model.RepositionDelay}s before Repositioning ...");
                    ProsimController.SetServiceChocks(true);
                    Thread.Sleep((int)(Model.RepositionDelay * 1000.0f));
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Repositioning Plane");
                    MenuOpen();
                    Thread.Sleep(100);
                    MenuItem(10);
                    Thread.Sleep(250);
                    MenuItem(1);
                    planePositioned = true;
                    Thread.Sleep(1500);
                    return;
                }
                else if (!Model.RepositionPlane && !planePositioned)
                {
                    planePositioned = true;
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Repositioning was skipped (disabled in Settings)");
                }

                if (Model.AutoConnect && !connectCalled)
                {
                    CallJetwayStairs();
                    connectCalled = true;
                    return;
                }

                if (Model.ConnectPCA && !pcaCalled && (!Model.PcaOnlyJetways || (Model.PcaOnlyJetways && SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Connecting PCA");
                    ProsimController.SetServicePCA(true);
                    pcaCalled = true;
                    return;
                }

                if (firstRun)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Setting GPU and Chocks");
                    ProsimController.SetServiceChocks(true);
                    ProsimController.SetServiceGPU(true);
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State: Preparation (Waiting for Flightplan import)");
                    firstRun = false;
                }

                if (ProsimController.IsFlightplanLoaded())
                {
                    CurrentFlightState = FlightState.DEPARTURE;
                    flightPlanID = ProsimController.flightPlanID;
                    SetPassengers(ProsimController.GetPaxPlanned());

                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Preparation -> DEPARTURE (Waiting for Refueling and Boarding)");
                }

            }
            //Special Case: loaded in Flight or with Engines Running
            if (_state == FlightState.PREFLIGHT && (!simOnGround || ProsimController.enginesRunning))
            {
                ProsimController.Update(true);
                flightPlanID = ProsimController.flightPlanID;

                CurrentFlightState = FlightState.FLIGHT;
                Interval = 180000;
                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Current State is Flight.");

                if (simOnGround && ProsimController.enginesRunning)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Starting on Runway - Removing Ground Equipment");
                    ProsimController.SetServiceChocks(false);
                    ProsimController.SetServiceGPU(false);
                }

                return;
            }

            //DEPARTURE - Generate Preliminary Loadsheet
            if (_state == FlightState.DEPARTURE && !prelimLoadsheet)
            {
                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Generating preliminary loadsheet using Prosim native functionality");
                
                // Generate preliminary loadsheet using Prosim's native functionality with enhanced error handling
                _loadsheetService.GenerateLoadsheet("Preliminary").ContinueWith(task => {
                    if (task.IsFaulted)
                    {
                        // Handle task failure (exception in the task itself)
                        Logger.Log(LogLevel.Error, "GsxController:RunServices", 
                            $"Task exception while generating preliminary loadsheet: {task.Exception?.InnerException?.Message ?? "Unknown error"}");
                        return;
                    }
                    
                    var result = task.Result;
                    if (result.Success)
                    {
                        prelimLoadsheet = true;
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", "Preliminary loadsheet generated successfully");
                        
                        // If ACARS is enabled, we can still send the loadsheet data via ACARS
                        if (Model.UseAcars)
                        {
                            try
                            {
                                string flightNumber = ProsimController.GetFMSFlightNumber();
                                if (!string.IsNullOrEmpty(flightNumber))
                                {
                                    // Get the loadsheet data from Prosim
                                    var loadsheetData = ProsimController.GetLoadsheetData("Preliminary");
                                    if (loadsheetData != null)
                                    {
                                        // Parse the loadsheet data and send it via ACARS
                                        string prelimLoadsheetString = loadsheetData.ToString();
                                        System.Threading.Tasks.Task acarsTask = AcarsClient.SendMessageToAcars(
                                            flightNumber, "telex", prelimLoadsheetString);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogLevel.Error, "GsxController:RunServices", $"Error sending loadsheet to ACARS: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        // Log detailed error information
                        if (result.StatusCode.HasValue)
                        {
                            Logger.Log(LogLevel.Error, "GsxController:RunServices", 
                                $"Failed to generate preliminary loadsheet: HTTP {(int)result.StatusCode} - {result.StatusCode}");
                            
                            // Log specific guidance based on status code
                            switch (result.StatusCode)
                            {
                                case HttpStatusCode.NotFound:
                                    Logger.Log(LogLevel.Error, "GsxController:RunServices", 
                                        "The loadsheet endpoint was not found. Verify that Prosim's EFB server is running on port 5000.");
                                    break;
                                case HttpStatusCode.BadRequest:
                                    Logger.Log(LogLevel.Error, "GsxController:RunServices", 
                                        "Bad request to loadsheet endpoint. Verify that the flight plan is loaded correctly.");
                                    break;
                                case HttpStatusCode.Unauthorized:
                                case HttpStatusCode.Forbidden:
                                    Logger.Log(LogLevel.Error, "GsxController:RunServices", 
                                        "Authorization error accessing loadsheet endpoint. Check Prosim configuration.");
                                    break;
                                case HttpStatusCode.InternalServerError:
                                    Logger.Log(LogLevel.Error, "GsxController:RunServices", 
                                        "Prosim server error generating loadsheet. Check Prosim logs for details.");
                                    break;
                                case HttpStatusCode.ServiceUnavailable:
                                    Logger.Log(LogLevel.Error, "GsxController:RunServices", 
                                        "Prosim service unavailable. Verify that Prosim is running and not overloaded.");
                                    break;
                            }
                        }
                        else
                        {
                            // Log the error message if no status code is available
                            Logger.Log(LogLevel.Error, "GsxController:RunServices", 
                                $"Failed to generate preliminary loadsheet: {result.ErrorMessage}");
                        }
                        
                        // Log response content if available
                        if (!string.IsNullOrEmpty(result.ResponseContent))
                        {
                            Logger.Log(LogLevel.Error, "GsxController:RunServices", 
                                $"Response content: {result.ResponseContent}");
                        }
                    }
                });
            }

            //DEPARTURE - Boarding & Refueling
            int refuelState = (int)SimConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
            int cateringState = (int)SimConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
            if (_state == FlightState.DEPARTURE && (!refuelFinished || !boardFinished))
            {
                RunLoadingServices(refuelState, cateringState);

                return;
            }

            //DEPARTURE - Loadsheet & Ground-Equipment
            if (_state == FlightState.DEPARTURE && refuelFinished && boardFinished)
            {
                RunDEPARTUREServices();

                return;
            }

            if (_state <= FlightState.FLIGHT)
            {
                //TAXIOUT -> FLIGHT
                if (_state <= FlightState.TAXIOUT && !simOnGround)
                {
                    if (_state <= FlightState.DEPARTURE) //in flight restart
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"In-Flight restart detected");
                        ProsimController.Update(true);
                        flightPlanID = ProsimController.flightPlanID;
                    }
                    CurrentFlightState = FlightState.FLIGHT;
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Taxi-Out -> Flight");
                    Interval = 180000;

                    return;
                }

                //FLIGHT -> TAXIIN
                if (_state == FlightState.FLIGHT && simOnGround)
                {
                    CurrentFlightState = FlightState.TAXIIN;
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Flight -> Taxi-In (Waiting for Engines stopped and Beacon off)");

                    Interval = 2500;
                    if (Model.TestArrival)
                        flightPlanID = ProsimController.flightPlanID;
                    pcaCalled = false;
                    connectCalled = false;

                    return;
                }
            }

            //TAXIIN -> ARRIVAL - Ground Equipment
            if (_state == FlightState.TAXIIN)
            {
                double engine1 = ProsimController.Interface.GetProsimVariable("aircraft.engine1.raw");
                double engine2 = ProsimController.Interface.GetProsimVariable("aircraft.engine2.raw");
                bool enginesAreOff = engine1 < 18.0D && engine2 < 18.0D;
                bool parkingBrakeSet = ProsimController.GetStatusFunction("system.switches.S_MIP_PARKING_BRAKE") == 1;
                bool beaconIsOff = ProsimController.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") == 0;
                bool groundSpeedNearZero = SimConnect.ReadSimVar("GPS GROUND SPEED", "Meters per second") < 0.5;

                // Log the current state of each condition for debugging
                Logger.Log(LogLevel.Debug, "GsxController:RunServices",
                    $"TAXIIN conditions: Engines Off={enginesAreOff}, Parking Brake={parkingBrakeSet}, " +
                    $"Beacon Off={beaconIsOff}, Low Speed={groundSpeedNearZero}");

                // Check for critical arrival conditions
                if (enginesAreOff && parkingBrakeSet)
                {
                    // Primary conditions for arrival are met

                    // If beacon is still on, notify but proceed anyway with limited services
                    if (!beaconIsOff)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunServices",
                            $"Proceeding to ARRIVAL with beacon still on - some services will be delayed");
                    }

                    // Additional check for low ground speed to ensure aircraft has fully stopped
                    if (groundSpeedNearZero)
                    {
                        // Add a small delay to ensure aircraft has settled
                        Logger.Log(LogLevel.Information, "GsxController:RunServices",
                            $"Aircraft appears to be at final parking position, initiating arrival services");

                        // Get current deboarding state before transition
                        int currentDeboardState = (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");

                        // Run arrival services with the current deboarding state
                        RunArrivalServices();
                        return;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, "GsxController:RunServices",
                            $"Waiting for aircraft to come to a complete stop");
                    }
                }
            }

            //ARRIVAL - Deboarding
            if (_state == FlightState.ARRIVAL && currentDeboardState >= 4)
            {
                RunDeboardingService();
            }

            //Pre-Flight - Turn-Around
            if (_state == FlightState.TURNAROUND)
            {
                if (ProsimController.IsFlightplanLoaded() && ProsimController.flightPlanID != flightPlanID)
                {
                    flightPlanID = ProsimController.flightPlanID;
                    if (Model.UseAcars)
                    {
                        AcarsClient.SetCallsign(FlightCallsignToOpsCallsign(ProsimController.flightNumber));
                    }
                    
                    // Reset loadsheet flags in the loadsheet service to ensure we generate new loadsheets for the new flight
                    _loadsheetService.ResetLoadsheetFlags();
                    
                    CurrentFlightState = FlightState.DEPARTURE;
                    planePositioned = true;
                    connectCalled = true;
                    pcaCalled = true;
                    initialFuelSet = false;
                    refueling = false;
                    refuelPaused = false;
                    refuelFinished = false;
                    cateringFinished = false;
                    refuelRequested = false;
                    cateringRequested = false;
                    boarding = false;
                    boardingRequested = false;
                    boardFinished = false;
                    finalLoadsheetSend = false;
                    equipmentRemoved = false;
                    pushRunning = false;
                    pushFinished = false;
                    pushNwsDisco = false;
                    pcaRemoved = false;
                    deboarding = false;
                    delayCounter = 0;
                    paxPlanned = 0;
                    delay = 0;
                    prelimLoadsheet = false; // Reset the preliminary loadsheet flag

                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Turn-Around -> DEPARTURE (Waiting for Refueling and Boarding)");
                }
            }
        }

        private void RunLoadingServices(int refuelState, int cateringState)
        {
            Interval = 1000;

            if (Model.CallCatering && !cateringFinished && cateringState == 6)
            {
                cateringFinished = true;
                Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Catering service completed (state 6)");
            }

            if (Model.AutoRefuel)
            {
                if (!initialFuelSet)
                {
                    ProsimController.SetInitialFuel();
                    initialFuelSet = true;
                }

                if (Model.SetSaveHydraulicFluids && !initialFluidsSet)
                {
                    ProsimController.SetInitialFluids();
                    initialFluidsSet = true;
                }

                if (!refuelRequested && refuelState != 6)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Calling Refuel Service");
                    MenuOpen();
                    MenuItem(3);
                    refuelRequested = true;
                    return;
                }

                if (Model.CallCatering && !cateringRequested && cateringState != 6)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Calling Catering Service");
                    MenuOpen();
                    MenuItem(2);
                    OperatorSelection();
                    cateringRequested = true;
                    return;
                }
            }

            if (Model.AutoBoarding)
            {
                if (!boardingRequested && refuelFinished && ((Model.CallCatering && cateringFinished) || !Model.CallCatering))
                {
                    if (delayCounter == 0)
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Waiting 90s before calling Boarding");

                    if (delayCounter < 90)
                    {
                        delayCounter++;
                        Logger.Log(LogLevel.Debug, "GsxController:RunLoadingServices", $"Boarding delay counter: {delayCounter}/90");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Calling Boarding Service");
                        SetPassengers(ProsimController.GetPaxPlanned());
                        MenuOpen();
                        MenuItem(4);
                        delayCounter = 0;
                        boardingRequested = true;
                    }
                    return;
                }
                else if (!boardingRequested)
                {
                    Logger.Log(LogLevel.Debug, "GsxController:RunLoadingServices",
                        $"Not ready for boarding yet. Refuel finished: {refuelFinished}, Catering finished: {cateringFinished}, Call catering: {Model.CallCatering}");
                }
            }

            if (!refueling && !refuelFinished && refuelState == 5)
            {
                refueling = true;
                refuelPaused = true; // Start in paused state until hose is connected
                Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Fuel Service active");
                ProsimController.RefuelStart();

                // Check initial state of the fuel hose
                if (SimConnect.ReadLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices",
                        $"Fuel hose already connected - starting fuel transfer");
                    refuelPaused = false;
                    ProsimController.RefuelResume();
                }
            }
            else if (refueling)
            {
                // Only perform active refueling when not paused
                if (!refuelPaused && SimConnect.ReadLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1)
                {
                    if (ProsimController.Refuel())
                    {
                        refueling = false;
                        refuelFinished = true;
                        refuelPaused = false;
                        ProsimController.RefuelStop();
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Refuel completed");
                    }
                }

                // Add state transition check for GSX refueling state
                int currentRefuelState = (int)SimConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
                if (currentRefuelState == 6) // Check if GSX considers refueling completed
                {
                    if (!refuelFinished)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"GSX reports refueling completed (state 6)");
                        refueling = false;
                        refuelFinished = true;
                        refuelPaused = false;
                        ProsimController.RefuelStop();
                    }
                }
            }

            if (refuelFinished && !boardingRequested)
            {
                Logger.Log(LogLevel.Debug, "GsxController:RunLoadingServices",
                    $"Refueling finished. AutoBoarding: {Model.AutoBoarding}, CateringFinished: {cateringFinished}, CallCatering: {Model.CallCatering}, DelayCounter: {delayCounter}");
            }

            if (!boarding && !boardFinished && SimConnect.ReadLvar("FSDT_GSX_BOARDING_STATE") >= 4)
            {
                boarding = true;
                ProsimController.BoardingStart();
                Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Boarding Service active");
            }
            else if (boarding)
            {
                // Check cargo loading percentage
                int cargoPercent = (int)SimConnect.ReadLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
                
                // Close cargo doors when cargo loading reaches 100%
                if (cargoPercent == 100)
                {
                    if (forwardCargoDoorOpened)
                    {
                        ProsimController.SetForwardCargoDoor(false);
                        forwardCargoDoorOpened = false;
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Closed forward cargo door after loading");
                    }
                    
                    if (aftCargoDoorOpened)
                    {
                        ProsimController.SetAftCargoDoor(false);
                        aftCargoDoorOpened = false;
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Closed aft cargo door after loading");
                    }
                }
                
                // Check if boarding and cargo loading are complete
                if (ProsimController.Boarding((int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL"), cargoPercent) || SimConnect.ReadLvar("FSDT_GSX_BOARDING_STATE") == 6)
                {
                    boarding = false;
                    boardFinished = true;
                    ProsimController.BoardingStop();
                    Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Boarding completed");
                    
                    // Ensure cargo doors are closed when boarding is complete
                    if (forwardCargoDoorOpened)
                    {
                        ProsimController.SetForwardCargoDoor(false);
                        forwardCargoDoorOpened = false;
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Closed forward cargo door after boarding");
                    }
                    
                    if (aftCargoDoorOpened)
                    {
                        ProsimController.SetAftCargoDoor(false);
                        aftCargoDoorOpened = false;
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Closed aft cargo door after boarding");
                    }
                }
            }
        }

        private void RunDEPARTUREServices()
        {
            if (Model.ConnectPCA && !pcaRemoved)
            {
                // Check for APU started with APU bleed on, beacon on, and external power changed from on to Avail
                bool apuStarted = ProsimController.GetStatusFunction("system.indicators.I_OH_ELEC_APU_START_U") != 0;
                bool apuBleedOn = ProsimController.GetStatusFunction("system.switches.S_OH_PNEUMATIC_APU_BLEED") != 0;
                bool beaconOn = ProsimController.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") != 0;
                bool extPowerAvail = ProsimController.GetStatusFunction("system.indicators.I_OH_ELEC_EXT_PWR_L") == 0;
                if (apuStarted && apuBleedOn && beaconOn && extPowerAvail)
                {
                    ProsimController.SetServicePCA(false);
                    pcaRemoved = true;
                    Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"APU Started with Bleed on, Beacon on, and External Power Avail - removing PCA");
                }
            }

            //LOADSHEET
            int departState = (int)SimConnect.ReadLvar("FSDT_GSX_DEPARTURE_STATE");
            if (!finalLoadsheetSend)
            {
                if (delay == 0)
                {
                    delay = new Random().Next(90, 150);
                    delayCounter = 0;
                    Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Final Loadsheet in {delay}s");

                }

                if (delayCounter < delay)
                {
                    delayCounter++;
                    return;
                }
                else
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Generating final loadsheet using Prosim native functionality");
                    
                    // Generate final loadsheet using Prosim's native functionality with enhanced error handling
                    _loadsheetService.GenerateLoadsheet("Final").ContinueWith(task => {
                        if (task.IsFaulted)
                        {
                            // Handle task failure (exception in the task itself)
                            Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", 
                                $"Task exception while generating final loadsheet: {task.Exception?.InnerException?.Message ?? "Unknown error"}");
                            return;
                        }
                        
                        var result = task.Result;
                        if (result.Success)
                        {
                            finalLoadsheetSend = true;
                            Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", "Final loadsheet generated and sent to MCDU successfully");
                            
                            // If ACARS is enabled, we can still send the loadsheet data via ACARS
                            if (Model.UseAcars)
                            {
                                try
                                {
                                    // Get the loadsheet data from Prosim
                                    var loadsheetData = ProsimController.GetLoadsheetData("Final");
                                    if (loadsheetData != null)
                                    {
                                        // Parse the loadsheet data and send it via ACARS
                                        string finalLoadsheetString = loadsheetData.ToString();
                                        System.Threading.Tasks.Task acarsTask = AcarsClient.SendMessageToAcars(
                                            ProsimController.GetFMSFlightNumber(), "telex", finalLoadsheetString);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", $"Error sending loadsheet to ACARS: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            // Log detailed error information
                            if (result.StatusCode.HasValue)
                            {
                                Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", 
                                    $"Failed to generate final loadsheet: HTTP {(int)result.StatusCode} - {result.StatusCode}");
                                
                                // Log specific guidance based on status code
                                switch (result.StatusCode)
                                {
                                    case HttpStatusCode.NotFound:
                                        Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", 
                                            "The loadsheet endpoint was not found. Verify that Prosim's EFB server is running on port 5000.");
                                        break;
                                    case HttpStatusCode.BadRequest:
                                        Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", 
                                            "Bad request to loadsheet endpoint. Verify that the flight plan is loaded correctly.");
                                        break;
                                    case HttpStatusCode.Unauthorized:
                                    case HttpStatusCode.Forbidden:
                                        Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", 
                                            "Authorization error accessing loadsheet endpoint. Check Prosim configuration.");
                                        break;
                                    case HttpStatusCode.InternalServerError:
                                        Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", 
                                            "Prosim server error generating loadsheet. Check Prosim logs for details.");
                                        break;
                                    case HttpStatusCode.ServiceUnavailable:
                                        Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", 
                                            "Prosim service unavailable. Verify that Prosim is running and not overloaded.");
                                        break;
                                }
                            }
                            else
                            {
                                // Log the error message if no status code is available
                                Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", 
                                    $"Failed to generate final loadsheet: {result.ErrorMessage}");
                            }
                            
                            // Log response content if available
                            if (!string.IsNullOrEmpty(result.ResponseContent))
                            {
                                Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", 
                                    $"Response content: {result.ResponseContent}");
                            }
                        }
                    });
                }
            }
            //EQUIPMENT
            else if (!equipmentRemoved)
            {
                //equipmentRemoved = SimConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && SimConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1 && SimConnect.ReadLvar("I_OH_ELEC_EXT_PWR_L") == 0;
                if (ProsimController.GetStatusFunction("system.switches.S_MIP_PARKING_BRAKE") == 1 && ProsimController.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") == 1 && ProsimController.GetStatusFunction("system.indicators.I_OH_ELEC_EXT_PWR_L") == 0) { equipmentRemoved = true;};
                if (equipmentRemoved)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Preparing for Pushback - removing Equipment");
                    if (departState < 4 && SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2 && SimConnect.ReadLvar("FSDT_GSX_JETWAY") == 5 && SimConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE") < 3)
                    {
                        MenuOpen();
                        Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Removing Jetway");
                        MenuItem(6);
                    }
                    ProsimController.SetServiceChocks(false);
                    ProsimController.SetServicePCA(false);
                    ProsimController.SetServiceGPU(false);
                }
            }
            //PUSHBACK
            else if (!pushFinished)
            {
                if (!Model.SynchBypass)
                {
                    pushFinished = true;
                    return;
                }

                double gs = SimConnect.ReadSimVar("GPS GROUND SPEED", "Meters per second") * 0.00002966071308045356;
                if (!pushRunning && gs > 1.5 && (SimConnect.ReadLvar("A_FC_THROTTLE_LEFT_INPUT") > 2.05 || SimConnect.ReadLvar("A_FC_THROTTLE_RIGHT_INPUT") > 2.05))
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Push-Back was skipped");
                    pushFinished = true;
                    pushRunning = false;
                    return;
                }

                if (!pushRunning && departState >= 4)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Push-Back Service is active");
                    pushRunning = true;
                    Interval = 100;
                }

                if (pushRunning)
                {
                    bool gsxPinInserted = SimConnect.ReadLvar("FSDT_GSX_BYPASS_PIN") != 0;
                    if (gsxPinInserted && !pushNwsDisco)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"By Pass Pin inserted");
                        SimConnect.WriteLvar("FSDT_VAR_Frozen", 1);
                        pushNwsDisco = true;
                    }
                    else if (gsxPinInserted && pushNwsDisco)
                    {
                        bool isFrozen = SimConnect.ReadLvar("FSDT_VAR_Frozen") == 1;

                        if (!isFrozen)
                        {
                            Logger.Log(LogLevel.Debug, "GsxController:RunDEPARTUREServices", $"Re-Freezing Plane");
                            SimConnect.WriteLvar("FSDT_VAR_Frozen", 1);
                        }
                    }

                    if (!gsxPinInserted && pushNwsDisco)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"By Pass Pin removed");
                        SimConnect.WriteLvar("FSDT_VAR_Frozen", 0);
                        pushNwsDisco = false;
                        pushRunning = false;
                        pushFinished = true;
                        Interval = 1000;
                    }
                }
            }
            else //DEPARTURE -> TAXIOUT
            {
                CurrentFlightState = FlightState.TAXIOUT;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("GPU", ServiceStatus.Inactive));
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("PCA", ServiceStatus.Inactive));
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Chocks", ServiceStatus.Inactive));
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Inactive));
                refuelFinished = false; // Reset the flag when transitioning to TAXIOUT
                Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"State Change: DEPARTURE -> Taxi-Out");
                delay = 0;
                delayCounter = 0;
                Interval = 60000;
            }
        }

        private void RunArrivalServices()
        {
            // Check if we're already in ARRIVAL state to avoid redundant operations
            if (_state == FlightState.ARRIVAL)
            {
                return;
            }

            // First, set the state to ARRIVAL - this should be done before any other operations
            // to ensure all systems respond to the correct state
            CurrentFlightState = FlightState.ARRIVAL;
            Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"State Change: Taxi-In -> Arrival (Setting up ground services)");

            // Check if Couatl Engine is running before proceeding with GSX operations
            if (SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
            {
                Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Couatl Engine not running - will retry");
                return;
            }

            // Set ground services first - these are critical
            Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Setting GPU and Chocks");
            ProsimController.SetServiceChocks(true);
            ProsimController.SetServiceGPU(true);

            // Load passenger data for deboarding
            SetPassengers(ProsimController.GetPaxPlanned());

            // Handle beacon state check separately - if beacon is still on, don't connect PCA or call stairs/jetway
            if (ProsimController.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") == 1)
            {
                Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Waiting for beacon to be turned off before connecting PCA or calling boarding equipment");
                return;
            }

            // Connect boarding equipment if not already called
            if (Model.AutoConnect && !connectCalled)
            {
                CallJetwayStairs();
                connectCalled = true;
                // Return to allow time for the jetway/stairs to connect before proceeding
                return;
            }

            // Connect PCA after jetway/stairs if needed
            if (Model.ConnectPCA && !pcaCalled && (!Model.PcaOnlyJetways || (Model.PcaOnlyJetways && SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
            {
                Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Connecting PCA");
                ProsimController.SetServicePCA(true);
                pcaCalled = true;
                // Return to allow PCA to connect
                return;
            }

            // Now that all services are set up, initiate deboarding if needed
            if (Model.AutoDeboarding && currentDeboardState < 4)
            {
                Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Calling Deboarding Service");
                MenuOpen();
                MenuItem(1);
                if (!Model.AutoConnect)
                    OperatorSelection();
            }

            // Save fuel and hydraulic state if enabled
            if (Model.SetSaveFuel)
            {
                double arrivalFuel = ProsimController.GetFuelAmount();
                Model.SavedFuelAmount = arrivalFuel;
                Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Saved arrival fuel amount: {arrivalFuel}");
            }

            if (Model.SetSaveHydraulicFluids)
            {
                var hydraulicFluids = ProsimController.GetHydraulicFluidValues();
                Model.HydaulicsBlueAmount = hydraulicFluids.Item1;
                Model.HydaulicsGreenAmount = hydraulicFluids.Item2;
                Model.HydaulicsYellowAmount = hydraulicFluids.Item3;
                Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Saved hydraulic fluid levels");
            }
        }

        private void RunDeboardingService()
        {
            if (!deboarding)
            {
                deboarding = true;
                ProsimController.DeboardingStart();
                Interval = 1000;
                Logger.Log(LogLevel.Information, "GsxController:RunDeboardingService", $"Deboarding Service active");
                return;
            }
            else if (deboarding)
            {
                if (SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS") != paxPlanned)
                {
                    Logger.Log(LogLevel.Warning, "GsxController:RunDeboardingService", $"Passenger changed during Boarding! Trying to reset Number ...");
                    SimConnect.WriteLvar("FSDT_GSX_NUMPASSENGERS", paxPlanned);
                }

                int paxCurrent = (int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS") - (int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");

                // Use the class-level variable instead of the parameter
                if (ProsimController.Deboarding(paxCurrent, (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT")) ||
                    currentDeboardState == 6 || currentDeboardState == 1)
                {
                    deboarding = false;
                    Logger.Log(LogLevel.Information, "GsxController:RunDeboardingService", $"Deboarding finished (GSX State {currentDeboardState})");
                    ProsimController.DeboardingStop();
                    Logger.Log(LogLevel.Information, "GsxController:RunDeboardingService", $"State Change: Arrival -> Turn-Around (Waiting for new Flightplan)");
                    CurrentFlightState = FlightState.TURNAROUND;
                    Interval = 10000;
                    return;
                }
            }
        }

        private void SetPassengers(int numPax)
        {
            SimConnect.WriteLvar("FSDT_GSX_NUMPASSENGERS", numPax);
            paxPlanned = numPax;
            Logger.Log(LogLevel.Information, "GsxController:SetPassengers", $"Passenger Count set to {numPax}");
            if (Model.DisableCrew)
            {
                SimConnect.WriteLvar("FSDT_GSX_CREW_NOT_DEBOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_CREW_NOT_BOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_PILOTS_NOT_DEBOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_NUMCREW", 0);
                SimConnect.WriteLvar("FSDT_GSX_NUMPILOTS", 0);
                SimConnect.WriteLvar("FSDT_GSX_CREW_ON_BOARD", 1);
                Logger.Log(LogLevel.Information, "GsxController:SetPassengers", $"Crew Boarding disabled");
            }
        }

        private void CallJetwayStairs()
        {
            MenuOpen();

            if (SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2 && SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 5 && SimConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE") < 3)
            {
                Logger.Log(LogLevel.Information, "GsxController:CallJetwayStairs", $"Calling Jetway");
                MenuItem(6);
                OperatorSelection();

                // Only call stairs if JetwayOnly is false
                if (!Model.JetwayOnly && SimConnect.ReadLvar("FSDT_GSX_STAIRS") != 2 && SimConnect.ReadLvar("FSDT_GSX_STAIRS") != 5 && SimConnect.ReadLvar("FSDT_GSX_OPERATESTAIRS_STATE") < 3)
                {
                    Thread.Sleep(1500);
                    MenuOpen();
                    Logger.Log(LogLevel.Information, "GsxController:CallJetwayStairs", $"Calling Stairs");
                    MenuItem(7);
                }
                else if (Model.JetwayOnly)
                {
                    Logger.Log(LogLevel.Information, "GsxController:CallJetwayStairs", $"Jetway Only mode - skipping stairs");
                }
            }
            else if (!Model.JetwayOnly && SimConnect.ReadLvar("FSDT_GSX_STAIRS") != 5 && SimConnect.ReadLvar("FSDT_GSX_OPERATESTAIRS_STATE") < 3)
            {
                Logger.Log(LogLevel.Information, "GsxController:CallJetwayStairs", $"Calling Stairs");
                MenuItem(7);
                OperatorSelection();
            }
            else if (Model.JetwayOnly)
            {
                Logger.Log(LogLevel.Information, "GsxController:CallJetwayStairs", $"Jetway Only mode - skipping stairs");
            }
        }

        private void OperatorSelection()
        {
            Thread.Sleep(2000);

            int result = IsOperatorSelectionActive();
            if (result == -1)
            {
                Logger.Log(LogLevel.Information, "GsxController:OperatorSelection", $"Waiting {Model.OperatorDelay}s for Operator Selection");
                Thread.Sleep((int)(Model.OperatorDelay * 1000));
            }
            else if (result == 1)
            {
                Logger.Log(LogLevel.Information, "GsxController:OperatorSelection", $"Operator Selection active, choosing Option 1");
                MenuItem(1);
                operatorWasSelected = true;
            }
            else
                Logger.Log(LogLevel.Information, "GsxController:OperatorSelection", $"No Operator Selection needed");
        }

        private int IsOperatorSelectionActive()
        {
            int result = -1;

            if (!string.IsNullOrEmpty(menuFile))
            {
                string[] lines = File.ReadLines(menuFile).ToArray();
                if (lines.Length > 1)
                {
                    if (!string.IsNullOrEmpty(lines[0]) && (lines[0] == "Select handling operator" || lines[0] == "Select catering operator"))
                    {
                        Logger.Log(LogLevel.Debug, "GsxController:IsOperatorSelectionActive", $"Match found for operator Selection: '{lines[0]}'");
                        result = 1;
                    }
                    else if (string.IsNullOrEmpty(lines[0]))
                    {
                        Logger.Log(LogLevel.Debug, "GsxController:IsOperatorSelectionActive", $"Line is empty! Lines total: {lines.Length}");
                        result = -1;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, "GsxController:IsOperatorSelectionActive", $"No Match found for operator Selection: '{lines[0]}'");
                        result = 0;
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "GsxController:IsOperatorSelectionActive", $"Menu Lines not above 1 ({lines.Length})");
                }
            }
            else
            {
                Logger.Log(LogLevel.Debug, "GsxController:IsOperatorSelectionActive", $"Menu File was empty");
            }

            return result;
        }

        private void MenuOpen()
        {
            SimConnect.IsGsxMenuReady = false;
            Logger.Log(LogLevel.Debug, "GsxController:MenuOpen", $"Opening GSX Menu");
            SimConnect.WriteLvar("FSDT_GSX_MENU_OPEN", 1);
        }

        private void MenuItem(int index, bool waitForMenu = true)
        {
            if (waitForMenu)
                MenuWaitReady();
            SimConnect.IsGsxMenuReady = false;
            Logger.Log(LogLevel.Debug, "GsxController:MenuItem", $"Selecting Menu Option {index} (L-Var Value {index - 1})");
            SimConnect.WriteLvar("FSDT_GSX_MENU_CHOICE", index - 1);
        }

        private void MenuWaitReady()
        {
            int counter = 0;
            while (!SimConnect.IsGsxMenuReady && counter < 1000) { Thread.Sleep(100); counter++; }
            Logger.Log(LogLevel.Debug, "GsxController:MenuWaitReady", $"Wait ended after {counter * 100}ms");
        }
       
        /// <summary>
        /// Compares preliminary and final loadsheet values and marks changes with "//"
        /// </summary>

        private (string, string, string, string, string, string, bool) GetLoadSheetDifferences(double prezfw, double preTow, int prePax, double preMacZfw, double preMacTow, double prefuel, double finalZfw, double finalTow, int finalPax, double finalMacZfw, double finalMacTow, double finalfuel)
        {
            // Tolerance values for detecting changes
            const double WeightTolerance = 1000.0; // 1000 kg tolerance for weight values
            const double MacTolerance = 0.5;     // 0.5% tolerance for MAC values - more sensitive to detect GSX randomization effects
            
            string zfwChanged = "";
            string towChanged = "";
            string paxChanged = "";
            string macZfwChanged = "";
            string macTowChanged = "";
            string fuelChanged = "";

            // Check if values have changed beyond tolerance
            bool hasZfwChanged = Math.Abs(prezfw - finalZfw) > WeightTolerance;
            bool hasTowChanged = Math.Abs(preTow - finalTow) > WeightTolerance;
            bool hasPaxChanged = prePax != finalPax;
            bool hasMacZfwChanged = Math.Abs(preMacZfw - finalMacZfw) > MacTolerance;
            bool hasMacTowChanged = Math.Abs(preMacTow - finalMacTow) > MacTolerance;
            bool hasFuelChanged = Math.Abs(prefuel - finalfuel) > WeightTolerance;

            // Mark changes with "//"
            if (hasZfwChanged)
            {
                zfwChanged = "//";
                Logger.Log(LogLevel.Debug, "GsxController:GetLoadSheetDifferences", $"ZFW changed: {prezfw} -> {finalZfw}");
            }

            if (hasTowChanged)
            {
                towChanged = "//";
                Logger.Log(LogLevel.Debug, "GsxController:GetLoadSheetDifferences", $"TOW changed: {preTow} -> {finalTow}");
            }

            if (hasPaxChanged)
            {
                paxChanged = "//";
                Logger.Log(LogLevel.Debug, "GsxController:GetLoadSheetDifferences", $"PAX changed: {prePax} -> {finalPax}");
            }

            if (hasMacZfwChanged)
            {
                macZfwChanged = "//";
                Logger.Log(LogLevel.Debug, "GsxController:GetLoadSheetDifferences", $"MACZFW changed: {preMacZfw:F2}% -> {finalMacZfw:F2}%");
            }

            if (hasMacTowChanged)
            {
                macTowChanged = "//";
                Logger.Log(LogLevel.Debug, "GsxController:GetLoadSheetDifferences", $"MACTOW changed: {preMacTow:F2}% -> {finalMacTow:F2}%");
            }

            if (hasFuelChanged)
            {
                fuelChanged = "//";
                Logger.Log(LogLevel.Debug, "GsxController:GetLoadSheetDifferences", $"Fuel changed: {prefuel} -> {finalfuel}");
            }

            // Determine if any values have changed
            bool hasChanged = hasZfwChanged || hasTowChanged || hasPaxChanged || hasMacZfwChanged || hasMacTowChanged || hasFuelChanged;

            return (zfwChanged, towChanged, paxChanged, macZfwChanged, macTowChanged, fuelChanged, hasChanged);
        }

        /// <summary>
        /// Handler for catering state changes
        /// </summary>
        private void OnCateringStateChanged(float newValue, float oldValue, string lvarName)
        {
            cateringState = newValue;
            Logger.Log(LogLevel.Debug, "GSXController", $"Catering state changed to {newValue}");
            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == 6 ? ServiceStatus.Completed :
                                       newValue == 5 ? ServiceStatus.Active :
                                       newValue == 4 ? ServiceStatus.Requested :
                                       ServiceStatus.Inactive;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Catering", status));
            }
            // Set cateringFinished when catering reaches completed state (typically state 6)
            if (newValue == 6 && !cateringFinished)
            {
                cateringFinished = true;
                Logger.Log(LogLevel.Information, "GSXController", $"Catering service completed");
            }
        }

        /// <summary>
        /// Handler for refueling state changes
        /// </summary>
        private void OnRefuelingStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, "GSXController", $"Refueling state changed to {newValue}");
            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == 6 ? ServiceStatus.Completed :
                                       newValue == 5 ? ServiceStatus.Active :
                                       newValue == 4 ? ServiceStatus.Requested :
                                       ServiceStatus.Inactive;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", status));
            }
            // Fallback if the refueling GSX service LVAR doesnt get set to completed 
            if (refuelFinished)
            {
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Refuel", ServiceStatus.Completed));
            }
        }

        /// <summary>
        /// Handler for boarding state changes
        /// </summary>
        private void OnBoardingStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, "GSXController", $"Boarding state changed to {newValue}");
            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == 6 ? ServiceStatus.Completed :
                                       newValue == 5 ? ServiceStatus.Active :
                                       newValue == 4 ? ServiceStatus.Requested :
                                       ServiceStatus.Inactive;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Boarding", status));
            }
        }

        /// <summary>
        /// Handler for deboarding state changes
        /// </summary>
        private void OnDeboardingStateChanged(float newValue, float oldValue, string lvarName)
        {
            currentDeboardState = (int)newValue;

            Logger.Log(LogLevel.Debug, "GSXController", $"Deboarding state changed to {newValue}");
            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == 6 ? ServiceStatus.Completed :
                                       newValue == 5 ? ServiceStatus.Active :
                                       newValue == 4 ? ServiceStatus.Requested :
                                       ServiceStatus.Inactive;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Deboarding", status));
            }
        }

        /// <summary>
        /// Handler for departure state changes
        /// </summary>
        private void OnDepartureStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, "GSXController", $"Departure state changed to {newValue}");

            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == 6 ? ServiceStatus.Completed :
                                       newValue == 5 ? ServiceStatus.Active :
                                       newValue == 4 ? ServiceStatus.Requested :
                                       ServiceStatus.Inactive;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Pushback", status));
            }
        }

        /// <summary>
        /// Handler for departure state changes
        /// </summary>
        private void OnJetwayStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, "GSXController", $"Jetway state changed to {newValue}");

            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == 5 ? ServiceStatus.Completed :
                                       newValue == 1 ? ServiceStatus.Disconnected :
                                       ServiceStatus.Inactive;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Jetway", status));
            }
        }

        /// <summary>
        /// Handler for Stairs state changes
        /// </summary>
        private void OnStairsStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, "GSXController", $"Stairs state changed to {newValue}");

            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == 5 ? ServiceStatus.Completed :
                                       newValue == 1 ? ServiceStatus.Disconnected :
                                       ServiceStatus.Inactive;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Stairs", status));
            }
        }

        /// <summary>
        /// Handler for fuel hose state changes
        /// </summary>
        private void OnFuelHoseStateChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, "GsxController:OnFuelHoseStateChanged",
                $"Fuel hose state changed from {oldValue} to {newValue}");

            if (refueling)
            {
                if (newValue == 1 && oldValue == 0)
                {
                    // Fuel hose was just connected
                    Logger.Log(LogLevel.Information, "GsxController:OnFuelHoseStateChanged",
                        $"Fuel hose connected - starting fuel transfer");
                    refuelPaused = false;
                    ProsimController.RefuelResume();
                }
                else if (newValue == 0 && oldValue == 1)
                {
                    // Fuel hose was just disconnected
                    Logger.Log(LogLevel.Information, "GsxController:OnFuelHoseStateChanged",
                        $"Fuel hose disconnected - pausing fuel transfer");
                    refuelPaused = true;
                    ProsimController.RefuelPause();
                }
            }
        }

        /// <summary>
        /// Handler for service toggle changes
        /// </summary>
        private void OnServiceToggleChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, "GSXController", $"Service toggle {lvarName} changed from {oldValue} to {newValue}");

            // Check if this is one of our monitored service toggles
            if (serviceToggles.ContainsKey(lvarName))
            {
                // Check if door toggle changed from 0 to 1
                if (oldValue == SERVICE_TOGGLE_OFF && newValue == SERVICE_TOGGLE_ON)
                {
                    // Trigger the appropriate door operation based on the current catering state
                    serviceToggles[lvarName]();
                }
            }
        }

        /// <summary>
        /// Handler for cargo loading percentage changes
        /// </summary>
        private void OnCargoLoadingChanged(float newValue, float oldValue, string lvarName)
        {
            Logger.Log(LogLevel.Debug, "GSXController", $"Cargo loading changed from {oldValue}% to {newValue}%");

            if (newValue == 1)
            {
                // Cargo service is running
                Logger.Log(LogLevel.Information, "GSXController", $"Cargo loading in progress");
            } 
            else if (newValue == 0)
            {
                // Cargo service completed
                Logger.Log(LogLevel.Information, "GSXController", $"Cargo loading complete, automatically closing cargo doors");

                // Close forward cargo door if it's open
                if (ProsimController.GetForwardCargoDoor() == "open")
                {
                    ProsimController.SetForwardCargoDoor(false);
                    forwardCargoDoorOpened = false;
                    Logger.Log(LogLevel.Information, "GSXController", $"Automatically closed forward cargo door after loading completion");
                }

                // Close aft cargo door if it's open
                if (ProsimController.GetAftCargoDoor() == "open")
                {
                    ProsimController.SetAftCargoDoor(false);
                    aftCargoDoorOpened = false;
                    Logger.Log(LogLevel.Information, "GSXController", $"Automatically closed aft cargo door after loading completion");
                }
            }
        }

        private void OperateFrontDoor()
        {
            // Operate front door based on catering state
            Logger.Log(LogLevel.Debug, "GsxController:OperateFrontDoor", $"Command to operate Front Door");
            if (Model.SetOpenCateringDoor)
            {
                if (cateringState == GSX_SERVICE_REQUESTED || (cateringState == GSX_SERVICE_ACTIVE && ProsimController.GetForwardRightDoor() == "closed"))
                {
                    ProsimController.SetForwardRightDoor(true);
                }
                else if (cateringState == GSX_SERVICE_ACTIVE && ProsimController.GetForwardRightDoor() == "open")
                {
                    ProsimController.SetForwardRightDoor(false);
                }
            }

        }

        private void OperateAftDoor()
        {
            // Operate aft door based on catering state
            Logger.Log(LogLevel.Debug, "GsxController:OperateAftDoor", $"Command to operate Aft Door");
            if (Model.SetOpenCateringDoor)
            {
                if (cateringState == GSX_SERVICE_REQUESTED || (cateringState == GSX_SERVICE_ACTIVE && ProsimController.GetAftRightDoor() == "closed"))
                {
                    ProsimController.SetAftRightDoor(true);
                }
                else if (cateringState == GSX_SERVICE_ACTIVE && ProsimController.GetAftRightDoor() == "open")
                {
                    ProsimController.SetAftRightDoor(false);
                }
            }

        }

        private void OperateFrontCargoDoor()
        {
            // Operate front door based on catering state
            Logger.Log(LogLevel.Debug, "GsxController:OperateFrontCargoDoor", $"Command to operate Front Cargo Door");
            if (Model.SetOpenCargoDoors)
            {
                if (cateringState == GSX_SERVICE_COMPLETED)
                {
                    ProsimController.SetForwardCargoDoor(true);
                }
            }
        }

        private void OperateAftCargoDoor()
        {
            // Operate aft door based on catering state
            Logger.Log(LogLevel.Debug, "GsxController:OperateAftCargoDoor", $"Command to operate Aft Cargo Door");
            if (Model.SetOpenCargoDoors)
            {
                if (cateringState == GSX_SERVICE_COMPLETED)
                {
                    ProsimController.SetAftCargoDoor(true);
                }
            }

        }

        private void OnCockpitDoorStateChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (dataRef == "system.switches.S_PED_COCKPIT_DOOR")
            {
                Logger.Log(LogLevel.Debug, "GsxController:OnCockpitDoorStateChanged",
                    $"Cockpit door switch changed from {oldValue} to {newValue}");

                // Determine door state based on switch position
                // 0=Normal (Door Closed), 1=Unlock (Door Open), 2=Lock (Door Closed)
                bool doorOpen = (int)newValue == 1; // Only open when in "Unlock" position

                // Update the GSX Pro LVAR to match the door state (0=closed, 1=open)
                int gsxDoorState = doorOpen ? 1 : 0;
                SimConnect.WriteLvar("FSDT_GSX_COCKPIT_DOOR_OPEN", gsxDoorState);

                // Update the cockpit door indicator (using byte type)
                // Value 1 when door is unlocked/open, 0 when door is closed/locked
                int indicatorState = doorOpen ? (int)1 : (int)0;
                ProsimController.Interface.SetProsimVariable("system.switches.S_DOORS_COCKPIT", indicatorState);

                Logger.Log(LogLevel.Debug, "GsxController:OnCockpitDoorStateChanged",
                    $"Door is {(doorOpen ? "open" : "closed")} - Set GSX LVAR to {gsxDoorState}, indicator to {indicatorState}");
            }
        }

        private void OnGPUStateChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (newValue != oldValue)
            {
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("GPU", newValue ? ServiceStatus.Completed : ServiceStatus.Disconnected));
            }
        }

        private void OnPCAStateChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (newValue != oldValue)
            {
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("PCA", newValue ? ServiceStatus.Completed : ServiceStatus.Disconnected));
            }
        }

        private void OnChocksStateChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (newValue != oldValue)
            {
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Chocks", newValue ? ServiceStatus.Completed : ServiceStatus.Disconnected));
            }
        }
    }
}
