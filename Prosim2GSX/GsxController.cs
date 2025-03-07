﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using CoreAudio;
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
    /// <summary>
    /// Controller for GSX integration with ProsimA320
    /// </summary>
    public class GsxController : IDisposable
    {
        // Constants
        private readonly string pathMenuFile = @"\MSFS\fsdreamteam-gsx-pro\html_ui\InGamePanels\FSDT_GSX_Panel\menu";
        private readonly string registryPath = @"HKEY_CURRENT_USER\SOFTWARE\FSDreamTeam";
        private readonly string registryValue = @"root";
        private readonly string gsxProcess = "Couatl64_MSFS";
        private string menuFile = "";

        // Minimal state variables (only those needed for coordination)
        private string flightPlanID = "0";
        private string lastVhf1App;
        private bool _isInitialized = false;
        
        // State variables that will be moved to services in future phases
        private bool aftCargoDoorOpened = false;
        private bool aftRightDoorOpened = false;
        private bool forwardRightDoorOpened = false;
        private bool forwardCargoDoorOpened = false;
        private bool boarding = false;
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
        
        // Dependencies (injected through constructor)
        private readonly IGSXAudioService audioService;
        private readonly IGSXStateManager stateManager;
        private readonly IGSXServiceCoordinator serviceCoordinator;
        private readonly IGSXLoadsheetManager loadsheetManager;
        private readonly IGSXDoorManager doorManager;
        private readonly IGSXMenuService menuService;
        private readonly IAcarsService acarsService;
        private readonly MobiSimConnect SimConnect;
        private readonly ProsimController ProsimController;
        private readonly ServiceModel Model;
        private readonly FlightPlan FlightPlan;

        /// <summary>
        /// Gets the current flight state
        /// </summary>
        public FlightState CurrentFlightState => stateManager.CurrentState;

        /// <summary>
        /// Gets or sets the interval between service runs in milliseconds
        /// </summary>
        public int Interval { get; set; } = 1000;

        /// <summary>
        /// Initializes a new instance of the GsxController class
        /// </summary>
        public GsxController(
            ServiceModel model, 
            ProsimController prosimController, 
            FlightPlan flightPlan, 
            IAcarsService acarsService, 
            IGSXMenuService menuService, 
            IGSXAudioService audioService, 
            IGSXStateManager stateManager, 
            IGSXLoadsheetManager loadsheetManager, 
            IGSXDoorManager doorManager, 
            IGSXServiceCoordinator serviceCoordinator)
        {
            try
            {
                // Store dependencies
                Model = model ?? throw new ArgumentNullException(nameof(model));
                ProsimController = prosimController ?? throw new ArgumentNullException(nameof(prosimController));
                FlightPlan = flightPlan ?? throw new ArgumentNullException(nameof(flightPlan));
                this.acarsService = acarsService ?? throw new ArgumentNullException(nameof(acarsService));
                this.menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
                this.audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
                this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
                this.serviceCoordinator = serviceCoordinator ?? throw new ArgumentNullException(nameof(serviceCoordinator));
                this.loadsheetManager = loadsheetManager ?? throw new ArgumentNullException(nameof(loadsheetManager));
                this.doorManager = doorManager ?? throw new ArgumentNullException(nameof(doorManager));

                // Subscribe to events
                SubscribeToEvents();

                // Initialize SimConnect
                SimConnect = IPCManager.SimConnect;
                SubscribeToSimConnectVariables();

                // Initialize services
                InitializeServices();

                // Store VHF1 app if configured
                if (!string.IsNullOrEmpty(Model.Vhf1VolumeApp))
                    lastVhf1App = Model.Vhf1VolumeApp;

                // Get GSX menu file path
                string regPath = (string)Registry.GetValue(registryPath, registryValue, null) + pathMenuFile;
                if (Path.Exists(regPath))
                    menuFile = regPath;

                // Handle test arrival mode
                if (Model.TestArrival)
                    ProsimController.Update(true);
                    
                Logger.Log(LogLevel.Information, "GsxController:Constructor", "GSX Controller initialized");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "GsxController:Constructor", $"Error initializing GSX Controller: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Resets audio settings to default
        /// </summary>
        public void ResetAudio()
        {
            try
            {
                audioService.ResetAudio();
                Logger.Log(LogLevel.Information, "GsxController:ResetAudio", "Audio reset successfully");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GsxController:ResetAudio", $"Error resetting audio: {ex.Message}");
            }
        }

        /// <summary>
        /// Controls audio based on cockpit controls
        /// </summary>
        public void ControlAudio()
        {
            try
            {
                audioService.ControlAudio();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GsxController:ControlAudio", $"Error controlling audio: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Subscribes to events from all services
        /// </summary>
        private void SubscribeToEvents()
        {
            try
            {
                // Subscribe to audio service events
                audioService.AudioSessionFound += OnAudioSessionFound;
                audioService.VolumeChanged += OnVolumeChanged;
                audioService.MuteChanged += OnMuteChanged;
                
                // Subscribe to state manager events
                stateManager.StateChanged += OnStateChanged;
                
                // Subscribe to service coordinator events
                serviceCoordinator.ServiceStatusChanged += OnServiceStatusChanged;
                
                // Subscribe to loadsheet manager events
                loadsheetManager.LoadsheetGenerated += OnLoadsheetGenerated;
                
                // Subscribe to door manager events
                doorManager.DoorStateChanged += OnDoorStateChanged;
                
                Logger.Log(LogLevel.Debug, "GsxController:SubscribeToEvents", "Subscribed to all service events");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GsxController:SubscribeToEvents", $"Error subscribing to events: {ex.Message}");
            }
        }

        /// <summary>
        /// Unsubscribes from all service events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            try
            {
                // Unsubscribe from audio service events
                if (audioService != null)
                {
                    audioService.AudioSessionFound -= OnAudioSessionFound;
                    audioService.VolumeChanged -= OnVolumeChanged;
                    audioService.MuteChanged -= OnMuteChanged;
                }
                
                // Unsubscribe from state manager events
                if (stateManager != null)
                {
                    stateManager.StateChanged -= OnStateChanged;
                }
                
                // Unsubscribe from service coordinator events
                if (serviceCoordinator != null)
                {
                    serviceCoordinator.ServiceStatusChanged -= OnServiceStatusChanged;
                }
                
                // Unsubscribe from loadsheet manager events
                if (loadsheetManager != null)
                {
                    loadsheetManager.LoadsheetGenerated -= OnLoadsheetGenerated;
                }
                
                // Unsubscribe from door manager events
                if (doorManager != null)
                {
                    doorManager.DoorStateChanged -= OnDoorStateChanged;
                }
                
                Logger.Log(LogLevel.Debug, "GsxController:UnsubscribeFromEvents", "Unsubscribed from all service events");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GsxController:UnsubscribeFromEvents", $"Error unsubscribing from events: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribes to SimConnect variables
        /// </summary>
        private void SubscribeToSimConnectVariables()
        {
            try
            {
                // Subscribe to SimConnect variables
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
                
                Logger.Log(LogLevel.Debug, "GsxController:SubscribeToSimConnectVariables", "Subscribed to SimConnect variables");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GsxController:SubscribeToSimConnectVariables", $"Error subscribing to SimConnect variables: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes all services
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                // Initialize state manager and service coordinator
                stateManager.Initialize();
                serviceCoordinator.Initialize();
                
                Logger.Log(LogLevel.Debug, "GsxController:InitializeServices", "Services initialized");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GsxController:InitializeServices", $"Error initializing services: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if all required services are available
        /// </summary>
        private bool AreServicesAvailable()
        {
            // Check if SimConnect is available
            if (SimConnect == null)
            {
                Logger.Log(LogLevel.Warning, "GsxController:AreServicesAvailable", "SimConnect not available");
                return false;
            }
            
            // Check if FlightPlan is available when needed
            if (FlightPlan == null)
            {
                Logger.Log(LogLevel.Warning, "GsxController:AreServicesAvailable", "FlightPlan not available");
                return false;
            }
            
            return true;
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
            Logger.Log(LogLevel.Information, "GsxController:OnDoorStateChanged", 
                $"Door state changed: {e.DoorType} - {(e.IsOpen ? "Opened" : "Closed")}");
        }
        
        // Event handler for service coordinator events
        private void OnServiceStatusChanged(object sender, ServiceStatusChangedEventArgs e)
        {
            Logger.Log(LogLevel.Information, "GsxController:OnServiceStatusChanged", 
                $"Service status changed: {e.ServiceType} - {e.Status} - {(e.IsCompleted ? "Completed" : "In Progress")}");
            
            // Update local state variables based on service status
            switch (e.ServiceType)
            {
                case "Refuel":
                    if (e.IsCompleted && e.Status.Contains("Completed"))
                        refuelFinished = true;
                    break;
                case "Boarding":
                    if (e.IsCompleted && e.Status.Contains("Completed"))
                        boardFinished = true;
                    break;
                case "Catering":
                    if (e.IsCompleted && e.Status.Contains("Completed"))
                        cateringFinished = true;
                    break;
                case "Loadsheet":
                    if (e.Status.Contains("Preliminary"))
                        prelimLoadsheet = true;
                    else if (e.Status.Contains("Final"))
                        finalLoadsheetSend = true;
                    break;
                case "Equipment":
                    if (e.IsCompleted && e.Status.Contains("Removed"))
                        equipmentRemoved = true;
                    break;
                case "Pushback":
                    if (e.IsCompleted && (e.Status.Contains("Completed") || e.Status.Contains("Skipped")))
                        pushFinished = true;
                    break;
                case "Door":
                    if (e.Status.Contains("Forward right door"))
                        forwardRightDoorOpened = e.Status.Contains("opened");
                    else if (e.Status.Contains("Aft right door"))
                        aftRightDoorOpened = e.Status.Contains("opened");
                    else if (e.Status.Contains("Forward cargo door"))
                        forwardCargoDoorOpened = e.Status.Contains("opened");
                    else if (e.Status.Contains("Aft cargo door"))
                        aftCargoDoorOpened = e.Status.Contains("opened");
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
            if (stateManager.IsDeparture() && (!serviceCoordinator.IsRefuelingComplete() || !serviceCoordinator.IsBoardingComplete()))
            {
                serviceCoordinator.RunLoadingServices(refuelState, cateringState);
                return;
            }

            //DEPARTURE - Loadsheet & Ground-Equipment
            int departureState = (int)SimConnect.ReadLvar("FSDT_GSX_DEPARTURE_STATE");
            if (stateManager.IsDeparture() && serviceCoordinator.IsRefuelingComplete() && serviceCoordinator.IsBoardingComplete())
            {
                serviceCoordinator.RunDepartureServices(departureState);
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
                serviceCoordinator.RunArrivalServices(deboard_state);
                return;
            }

            //ARRIVAL - Deboarding
            if (stateManager.IsArrival() && deboard_state >= 4)
            {
                serviceCoordinator.RunDeboardingService(deboard_state);
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

        // Note: The following methods have been removed as part of the refactoring to make GsxController a thin facade:
        // - RunLoadingServices
        // - RunDEPARTUREServices
        // - RunArrivalServices
        // - RunDeboardingService
        // 
        // These methods contained business logic that has been moved to the GSXServiceCoordinator.
        // The GsxController now delegates to the service coordinator in the RunServices method.

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

        
        /// <summary>
        /// Disposes the controller and unsubscribes from all events
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Unsubscribe from all events
                UnsubscribeFromEvents();
                
                Logger.Log(LogLevel.Information, "GsxController:Dispose", "GSX Controller disposed");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GsxController:Dispose", $"Error disposing controller: {ex.Message}");
            }
        }
    }
}
