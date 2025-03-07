﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using CoreAudio;
using Microsoft.Win32;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using System;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Prosim2GSX
{
    public class GsxController
    {
        private readonly string pathMenuFile = @"\MSFS\fsdreamteam-gsx-pro\html_ui\InGamePanels\FSDT_GSX_Panel\menu";
        private readonly string registryPath = @"HKEY_CURRENT_USER\SOFTWARE\FSDreamTeam";
        private readonly string registryValue = @"root";
        private readonly string gsxProcess = "Couatl64_MSFS";
        private string menuFile = "";

        /// <summary>
        /// Gets the current flight state
        /// </summary>
        public FlightState CurrentFlightState => stateManager.CurrentState;

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
        private bool firstRun = true;
        private string flightPlanID = "0";
        private bool initialFuelSet = false;
        private bool initialFluidsSet = false;
        private string lastVhf1App;
        private double macZfw = 0.0d;
        private bool operatorWasSelected = false;
        private string opsCallsign = "";
        private bool opsCallsignSet = false;
        private int paxPlanned = 0;
        private bool pcaCalled = false;
        private bool pcaRemoved = false;
        private bool planePositioned = false;
        private bool prelimLoadsheet = false;
        private bool finalLoadsheetSend = false;
        private bool prelimFlightData = false;
        private bool pushFinished = false;
        private bool pushNwsDisco = false;
        private bool pushRunning = false;
        private bool refuelFinished = false;
        private bool refueling = false;
        private bool refuelPaused = false;
        private bool refuelRequested = false;


        private readonly IAcarsService acarsService;
        private readonly IGSXMenuService menuService;
        private readonly IGSXAudioService audioService;
        private readonly IGSXStateManager stateManager;
        private readonly IGSXLoadsheetManager loadsheetManager;
        private readonly IGSXDoorManager doorManager;
        private readonly MobiSimConnect SimConnect;
        private readonly ProsimController ProsimController;
        private readonly ServiceModel Model;
        private readonly FlightPlan FlightPlan;

        public int Interval { get; set; } = 1000;

        public GsxController(ServiceModel model, ProsimController prosimController, FlightPlan flightPlan, IAcarsService acarsService, IGSXMenuService menuService, IGSXAudioService audioService, IGSXStateManager stateManager, IGSXLoadsheetManager loadsheetManager, IGSXDoorManager doorManager)
        {
            Model = model;
            ProsimController = prosimController;
            FlightPlan = flightPlan;
            this.acarsService = acarsService;
            this.menuService = menuService;
            this.audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.loadsheetManager = loadsheetManager ?? throw new ArgumentNullException(nameof(loadsheetManager));
            this.doorManager = doorManager ?? throw new ArgumentNullException(nameof(doorManager));

            // Subscribe to audio service events
            this.audioService.AudioSessionFound += OnAudioSessionFound;
            this.audioService.VolumeChanged += OnVolumeChanged;
            this.audioService.MuteChanged += OnMuteChanged;
            
            // Subscribe to state manager events
            this.stateManager.StateChanged += OnStateChanged;
            
            // Subscribe to loadsheet manager events
            this.loadsheetManager.LoadsheetGenerated += OnLoadsheetGenerated;
            
            // Subscribe to door manager events
            this.doorManager.DoorStateChanged += OnDoorStateChanged;
            
            // Initialize state manager and door manager
            this.stateManager.Initialize();
            this.doorManager.Initialize();

            SimConnect = IPCManager.SimConnect;
            SimConnect.SubscribeSimVar("SIM ON GROUND", "Bool");
            SimConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_CATERING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_REFUELING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_BOARDING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_DEPARTURE_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_DEICING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS");
            SimConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL");
            SimConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
            SimConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO");
            SimConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO");
            SimConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
            SimConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");
            SimConnect.SubscribeLvar("FSDT_GSX_FUELHOSE_CONNECTED");
            SimConnect.SubscribeLvar("FSDT_VAR_EnginesStopped");
            SimConnect.SubscribeLvar("FSDT_GSX_COUATL_STARTED");
            SimConnect.SubscribeLvar("FSDT_GSX_JETWAY");
            SimConnect.SubscribeLvar("FSDT_GSX_OPERATEJETWAYS_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_STAIRS");
            SimConnect.SubscribeLvar("FSDT_GSX_OPERATESTAIRS_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_BYPASS_PIN");
            SimConnect.SubscribeLvar("FSDT_VAR_Frozen");
            SimConnect.SubscribeLvar("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE");
            SimConnect.SubscribeLvar("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE");
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

            if (!string.IsNullOrEmpty(Model.Vhf1VolumeApp))
                lastVhf1App = Model.Vhf1VolumeApp;

            string regPath = (string)Registry.GetValue(registryPath, registryValue, null) + pathMenuFile;
            if (Path.Exists(regPath))
                menuFile = regPath;

            if (Model.TestArrival)
                ProsimController.Update(true);
                
            Logger.Log(LogLevel.Information, "GsxController:Constructor", "GSX Controller initialized");
        }

        public void ResetAudio()
        {
            audioService.ResetAudio();
        }

        public void ControlAudio()
        {
            audioService.ControlAudio();
        }
        
        // Event handlers for audio service events
        private void OnAudioSessionFound(object sender, AudioSessionEventArgs e)
        {
            Logger.Log(LogLevel.Information, "GsxController:OnAudioSessionFound", 
                $"Audio session found for {e.ProcessName}");
        }

        private void OnVolumeChanged(object sender, AudioVolumeChangedEventArgs e)
        {
            Logger.Log(LogLevel.Debug, "GsxController:OnVolumeChanged", 
                $"Volume changed for {e.ProcessName}: {e.Volume}");
        }

        private void OnMuteChanged(object sender, AudioMuteChangedEventArgs e)
        {
            Logger.Log(LogLevel.Debug, "GsxController:OnMuteChanged", 
                $"Mute state changed for {e.ProcessName}: {e.Muted}");
        }
        
        // Event handler for door manager events
        private void OnDoorStateChanged(object sender, DoorStateChangedEventArgs e)
        {
            // Update local state variables based on door state changes
            switch (e.DoorType)
            {
                case DoorType.ForwardRight:
                    forwardRightDoorOpened = e.IsOpen;
                    Logger.Log(LogLevel.Information, "GsxController:OnDoorStateChanged", 
                        $"Forward right door is now {(e.IsOpen ? "open" : "closed")}");
                    break;
                case DoorType.AftRight:
                    aftRightDoorOpened = e.IsOpen;
                    Logger.Log(LogLevel.Information, "GsxController:OnDoorStateChanged", 
                        $"Aft right door is now {(e.IsOpen ? "open" : "closed")}");
                    break;
                case DoorType.ForwardCargo:
                    forwardCargoDoorOpened = e.IsOpen;
                    Logger.Log(LogLevel.Information, "GsxController:OnDoorStateChanged", 
                        $"Forward cargo door is now {(e.IsOpen ? "open" : "closed")}");
                    break;
                case DoorType.AftCargo:
                    aftCargoDoorOpened = e.IsOpen;
                    Logger.Log(LogLevel.Information, "GsxController:OnDoorStateChanged", 
                        $"Aft cargo door is now {(e.IsOpen ? "open" : "closed")}");
                    break;
            }
        }
        
        // Event handler for loadsheet manager events
        private void OnLoadsheetGenerated(object sender, LoadsheetGeneratedEventArgs e)
        {
            // Update local state variables based on loadsheet type
            if (e.LoadsheetType.Equals("prelim", StringComparison.OrdinalIgnoreCase))
            {
                prelimLoadsheet = true;
                Logger.Log(LogLevel.Information, "GsxController:OnLoadsheetGenerated", 
                    $"Preliminary loadsheet for flight {e.FlightNumber} generated {(e.Success ? "successfully" : "with errors")} at {e.Timestamp:HH:mm:ss}");
            }
            else if (e.LoadsheetType.Equals("final", StringComparison.OrdinalIgnoreCase))
            {
                finalLoadsheetSend = true;
                Logger.Log(LogLevel.Information, "GsxController:OnLoadsheetGenerated", 
                    $"Final loadsheet for flight {e.FlightNumber} generated {(e.Success ? "successfully" : "with errors")} at {e.Timestamp:HH:mm:ss}");
            }
        }
        
        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            Logger.Log(LogLevel.Information, "GsxController:OnStateChanged", 
                $"Flight state changed from {e.PreviousState} to {e.NewState}");
                
            // Perform state-specific actions
            switch (e.NewState)
            {
                case FlightState.PREFLIGHT:
                    // Initialize for preflight
                    Interval = 1000;
                    break;
                case FlightState.DEPARTURE:
                    // Initialize for departure
                    Interval = 1000;
                    Logger.Log(LogLevel.Information, "GsxController:OnStateChanged", 
                        $"State Change: Preparation -> DEPARTURE (Waiting for Refueling and Boarding)");
                    break;
                case FlightState.TAXIOUT:
                    // Initialize for taxiout
                    Interval = 60000;
                    Logger.Log(LogLevel.Information, "GsxController:OnStateChanged", 
                        $"State Change: DEPARTURE -> Taxi-Out");
                    break;
                case FlightState.FLIGHT:
                    // Initialize for flight
                    Interval = 180000;
                    Logger.Log(LogLevel.Information, "GsxController:OnStateChanged", 
                        $"State Change: Taxi-Out -> Flight");
                    break;
                case FlightState.TAXIIN:
                    // Initialize for taxiin
                    Interval = 2500;
                    Logger.Log(LogLevel.Information, "GsxController:OnStateChanged", 
                        $"State Change: Flight -> Taxi-In (Waiting for Engines stopped and Beacon off)");
                    break;
                case FlightState.ARRIVAL:
                    // Initialize for arrival
                    Interval = 1000;
                    Logger.Log(LogLevel.Information, "GsxController:OnStateChanged", 
                        $"State Change: Taxi-In -> Arrival (Waiting for Deboarding)");
                    break;
                case FlightState.TURNAROUND:
                    // Initialize for turnaround
                    Interval = 10000;
                    Logger.Log(LogLevel.Information, "GsxController:OnStateChanged", 
                        $"State Change: Arrival -> Turn-Around (Waiting for new Flightplan)");
                    break;
            }
        }
        
        // Flag to track if the controller is fully initialized
        private bool _isInitialized = false;

        public void RunServices()
        {
            // Check if SimConnect is available
            if (SimConnect == null)
            {
                Logger.Log(LogLevel.Warning, "GsxController:RunServices", "SimConnect not available");
                return;
            }
            
            // Check if FlightPlan is available when needed
            if (FlightPlan == null)
            {
                Logger.Log(LogLevel.Warning, "GsxController:RunServices", "FlightPlan not available");
                return;
            }
            
            // Mark as initialized on first successful run
            if (!_isInitialized)
            {
                _isInitialized = true;
                Logger.Log(LogLevel.Information, "GsxController:RunServices", "GSX Controller initialized and ready");
            }
            
            bool simOnGround = SimConnect.ReadSimVar("SIM ON GROUND", "Bool") != 0.0f;
            ProsimController.Update(false);

            if (operatorWasSelected)
            {
                MenuOpen();
                operatorWasSelected = false;
            }

            //PREPARATION (On-Ground and Engines not running)
            if (stateManager.IsPreflight() && simOnGround && !ProsimController.enginesRunning && ProsimController.Interface.ReadDataRef("system.switches.S_OH_ELEC_BAT1") == 1)
            {
                if (Model.UseAcars && !opsCallsignSet)
                {
                    
                    try
                    {
                        opsCallsign = acarsService.FlightCallsignToOpsCallsign(ProsimController.flightNumber);
                        acarsService.Initialize(ProsimController.flightNumber);
                        opsCallsignSet = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "GsxController:RunServices", $"Unable to set opsCallSign - Error: {ex.Message}");
                    }

                }
                if (Model.TestArrival)
                {
                    stateManager.TransitionToFlight();
                    ProsimController.Update(true);
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Test Arrival - Plane is in 'Flight'");
                    return;
                }
                Interval = 1000;

                if (SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Couatl Engine not running");

                    if (Model.GsxVolumeControl)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Resetting GSX Audio (Engine not running)");
                        audioService.ResetAudio();
                    }

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
                    stateManager.TransitionToDeparture();
                    flightPlanID = ProsimController.flightPlanID;
                    SetPassengers(ProsimController.GetPaxPlanned());
                    if (!prelimFlightData)
                    {
                        macZfw = ProsimController.GetZfwCG();
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"MACZFW: {macZfw} %");

                        prelimFlightData = true;
                    }
                }

            }
            //Special Case: loaded in Flight or with Engines Running
            if (stateManager.IsPreflight() && (!simOnGround || ProsimController.enginesRunning))
            {
                ProsimController.Update(true);
                flightPlanID = ProsimController.flightPlanID;

                stateManager.TransitionToFlight();
                
                if (simOnGround && ProsimController.enginesRunning)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Starting on Runway - Removing Ground Equipment");
                    ProsimController.SetServiceChocks(false);
                    ProsimController.SetServiceGPU(false);
                }

                return;
            }

            //DEPARTURE - Get sim Zulu Time and send Prelim Loadsheet
            if (stateManager.IsDeparture() && !loadsheetManager.IsPreliminaryLoadsheetSent())
            {
                var simTime = SimConnect.ReadEnvVar("ZULU TIME", "Seconds");
                TimeSpan time = TimeSpan.FromSeconds(simTime);
                Logger.Log(LogLevel.Debug, "GsxController:RunServices", $"ZULU time - {simTime}");

                string flightNumber = ProsimController.GetFMSFlightNumber();

                if (Model.UseAcars && !string.IsNullOrEmpty(flightNumber))
                {
                    try
                    {
                        // Use the loadsheet manager to generate and send the preliminary loadsheet
                        // We don't need to await this as it's a fire-and-forget operation
                        // The loadsheet manager will handle the async operation and raise an event when complete
                        _ = loadsheetManager.GeneratePreliminaryLoadsheetAsync(flightNumber);
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Preliminary loadsheet generation initiated");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "GsxController:RunServices", $"Error initiating preliminary loadsheet generation: {ex.Message}");
                    }
                }
                else if (!Model.UseAcars)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"ACARS is disabled, skipping preliminary loadsheet");
                }
                else if (string.IsNullOrEmpty(flightNumber))
                {
                    Logger.Log(LogLevel.Warning, "GsxController:RunServices", $"Flight number is empty, cannot send preliminary loadsheet");
                }
            }

            //DEPARTURE - Boarding & Refueling
            int refuelState = (int)SimConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
            int cateringState = (int)SimConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
            if (stateManager.IsDeparture() && (!refuelFinished || !boardFinished))
            {
                RunLoadingServices(refuelState, cateringState);

                return;
            }

            //DEPARTURE - Loadsheet & Ground-Equipment
            if (stateManager.IsDeparture() && refuelFinished && boardFinished)
            {
                RunDEPARTUREServices();

                return;
            }

            // Handle flight state transitions
            if (stateManager.IsTaxiout() || stateManager.IsFlight())
            {
                //TAXIOUT -> FLIGHT
                if ((stateManager.IsTaxiout() || stateManager.IsDeparture()) && !simOnGround)
                {
                    if (stateManager.IsDeparture()) //in flight restart
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"In-Flight restart detected");
                        ProsimController.Update(true);
                        flightPlanID = ProsimController.flightPlanID;
                    }
                    stateManager.TransitionToFlight();
                    return;
                }

                //FLIGHT -> TAXIIN
                if (stateManager.IsFlight() && simOnGround)
                {
                    stateManager.TransitionToTaxiin();
                    
                    if (Model.TestArrival)
                        flightPlanID = ProsimController.flightPlanID;
                    pcaCalled = false;
                    connectCalled = false;

                    return;
                }
            }

            //TAXIIN -> ARRIVAL - Ground Equipment
            int deboard_state = (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");
            if (stateManager.IsTaxiin() && SimConnect.ReadLvar("FSDT_VAR_EnginesStopped") == 1 && SimConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && SimConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 0)
            {
                RunArrivalServices(deboard_state);

                return;
            }

            //ARRIVAL - Deboarding
            if (stateManager.IsArrival() && deboard_state >= 4)
            {
                RunDeboardingService(deboard_state);
            }

            //Pre-Flight - Turn-Around
            if (stateManager.IsTurnaround())
            {
                if (ProsimController.IsFlightplanLoaded() && ProsimController.flightPlanID != flightPlanID)
                {
                    flightPlanID = ProsimController.flightPlanID;
                    if (Model.UseAcars)
                    {
                        acarsService.Callsign = acarsService.FlightCallsignToOpsCallsign(ProsimController.flightNumber);
                    }
                    
                    // Reset state variables
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
                    
                    // Transition to DEPARTURE state
                    stateManager.TransitionToDeparture();
                }
            }
        }

        private void RunLoadingServices(int refuelState, int cateringState)
        {
            Interval = 1000;
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

            // Handle doors for catering
            if (Model.SetOpenCateringDoor)
            {
                // Check service toggles and delegate to door manager
                bool service1Toggle = SimConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE") == 1;
                bool service2Toggle = SimConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE") == 1;
                
                if (service1Toggle || service2Toggle)
                {
                    doorManager.HandleServiceToggle(
                        service1Toggle ? 1 : 2, 
                        service1Toggle ? !doorManager.IsForwardRightDoorOpen : !doorManager.IsAftRightDoorOpen);
                }
            }

            if (!cateringFinished && cateringState == 6)
            {
                cateringFinished = true;
                Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Catering finished");
                
                // Close forward right door when catering is finished (if not already closed)
                if (Model.SetOpenCateringDoor && doorManager.IsForwardRightDoorOpen)
                {
                    doorManager.CloseDoor(DoorType.ForwardRight);
                }
                
                // Open cargo doors after catering is finished if enabled
                if (Model.SetOpenCargoDoors)
                {
                    doorManager.OpenDoor(DoorType.ForwardCargo);
                    doorManager.OpenDoor(DoorType.AftCargo);
                    Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Opened cargo doors for loading");
                }
            }

            if (Model.AutoBoarding)
            {
                if (!boardingRequested && refuelFinished && ((Model.CallCatering && cateringFinished) || !Model.CallCatering))
                {
                    if (delayCounter == 0)
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Waiting 90s before calling Boarding");

                    if (delayCounter < 90)
                        delayCounter++;
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
            }

            if (!refueling && !refuelFinished && refuelState == 5)
            {
                refueling = true;
                refuelPaused = true;
                Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Fuel Service active");
                ProsimController.RefuelStart();
            }
            else if (refueling)
            {

                if (SimConnect.ReadLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1)
                {
                    if (refuelPaused)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Fuel Hose connected - refueling");

                        refuelPaused = false;
                    }

                    if (ProsimController.Refuel())
                    {
                        refueling = false;
                        refuelFinished = true;
                        refuelPaused = false;
                        ProsimController.RefuelStop();
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Refuel completed");
                    }
                }
                else
                {
                    if (!refuelPaused && !refuelFinished)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Fuel Hose disconnected - waiting for next Truck");
                        refuelPaused = true;
                    }
                }
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
                    if (doorManager.IsForwardCargoDoorOpen)
                    {
                        doorManager.CloseDoor(DoorType.ForwardCargo);
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Closed forward cargo door after loading");
                    }
                    
                    if (doorManager.IsAftCargoDoorOpen)
                    {
                        doorManager.CloseDoor(DoorType.AftCargo);
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
                    if (doorManager.IsForwardCargoDoorOpen)
                    {
                        doorManager.CloseDoor(DoorType.ForwardCargo);
                        Logger.Log(LogLevel.Information, "GsxController:RunLoadingServices", $"Closed forward cargo door after boarding");
                    }
                    
                    if (doorManager.IsAftCargoDoorOpen)
                    {
                        doorManager.CloseDoor(DoorType.AftCargo);
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
            if (!loadsheetManager.IsFinalLoadsheetSent())
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
                    Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Transmitting Final Loadsheet ...");
                    ProsimController.TriggerFinal();
                    
                    if (Model.UseAcars)
                    {
                        string flightNumber = ProsimController.GetFMSFlightNumber();
                        if (!string.IsNullOrEmpty(flightNumber))
                        {
                            try
                            {
                                // Use the loadsheet manager to generate and send the final loadsheet
                                // We don't need to await this as it's a fire-and-forget operation
                                // The loadsheet manager will handle the async operation and raise an event when complete
                                _ = loadsheetManager.GenerateFinalLoadsheetAsync(flightNumber);
                                Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Final loadsheet generation initiated");
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogLevel.Error, "GsxController:RunDEPARTUREServices", $"Error initiating final loadsheet generation: {ex.Message}");
                            }
                        }
                        else
                        {
                            Logger.Log(LogLevel.Warning, "GsxController:RunDEPARTUREServices", $"Flight number is empty, cannot send final loadsheet");
                            // Mark as sent anyway to avoid getting stuck
                            finalLoadsheetSend = true;
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"ACARS is disabled, skipping final loadsheet");
                        // Mark as sent anyway to avoid getting stuck
                        finalLoadsheetSend = true;
                    }
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
                    stateManager.TransitionToTaxiout();
                    delay = 0;
                    delayCounter = 0;
            }
        }

        private void RunArrivalServices(int deboard_state)
        {
            if (SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
            {
                Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Couatl Engine not running");
                if (Model.GsxVolumeControl)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Resetting GSX Audio (Engine not running)");
                    audioService.ResetAudio();
                }
                return;
            }

            if (Model.AutoConnect && !connectCalled)
            {
                CallJetwayStairs();
                connectCalled = true;
                return;
            }

            if (SimConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1)
                return;

            if (Model.ConnectPCA && !pcaCalled && (!Model.PcaOnlyJetways || (Model.PcaOnlyJetways && SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
            {
                Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Connecting PCA");
                ProsimController.SetServicePCA(true);
                pcaCalled = true;
            }

            Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Setting GPU and Chocks");
            ProsimController.SetServiceChocks(true);
            ProsimController.SetServiceGPU(true);
            SetPassengers(ProsimController.GetPaxPlanned());

            stateManager.TransitionToArrival();

            if (Model.AutoDeboarding && deboard_state < 4)
            {
                Logger.Log(LogLevel.Information, "GsxController:RunArrivalServices", $"Calling Deboarding Service");
                SetPassengers(ProsimController.GetPaxPlanned());
                MenuOpen();
                MenuItem(1);
                if (!Model.AutoConnect)
                    OperatorSelection();
            }

            if (Model.SetSaveFuel)
            {
                double arrivalFuel = ProsimController.GetFuelAmount();
                Model.SavedFuelAmount = arrivalFuel;
            }

            if (Model.SetSaveHydraulicFluids)
            {
                var hydraulicFluids = ProsimController.GetHydraulicFluidValues();
                Model.HydaulicsBlueAmount = hydraulicFluids.Item1;
                Model.HydaulicsGreenAmount = hydraulicFluids.Item2;
                Model.HydaulicsYellowAmount = hydraulicFluids.Item3;
            }
        }

        private void RunDeboardingService(int deboard_state)
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
                if (ProsimController.Deboarding(paxCurrent, (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT")) || deboard_state == 6 || deboard_state == 1)
                {
                    deboarding = false;
                    Logger.Log(LogLevel.Information, "GsxController:RunDeboardingService", $"Deboarding finished (GSX State {deboard_state})");
                    ProsimController.DeboardingStop();
                    stateManager.TransitionToTurnaround();
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
            menuService.OperatorSelection();
            operatorWasSelected = menuService.OperatorWasSelected;
        }

        private int IsOperatorSelectionActive()
        {
            return menuService.IsOperatorSelectionActive();
        }

        private void MenuOpen()
        {
            menuService.MenuOpen();
        }

        private void MenuItem(int index, bool waitForMenu = true)
        {
            menuService.MenuItem(index, waitForMenu);
        }

        private void MenuWaitReady()
        {
            menuService.MenuWaitReady();
        }

        
        public void Dispose()
        {
            // Unsubscribe from events
            if (audioService != null)
            {
                audioService.AudioSessionFound -= OnAudioSessionFound;
                audioService.VolumeChanged -= OnVolumeChanged;
                audioService.MuteChanged -= OnMuteChanged;
            }
            
            if (stateManager != null)
            {
                stateManager.StateChanged -= OnStateChanged;
            }
            
            if (loadsheetManager != null)
            {
                loadsheetManager.LoadsheetGenerated -= OnLoadsheetGenerated;
            }
            
            if (doorManager != null)
            {
                doorManager.DoorStateChanged -= OnDoorStateChanged;
            }
            
            Logger.Log(LogLevel.Information, "GsxController:Dispose", "GSX Controller disposed");
        }
    }
}
