using Prosim2GSX.Models;
using System;
using System.Threading;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for coordinating GSX services
    /// </summary>
    public class GSXServiceCoordinator : IGSXServiceCoordinator
    {
        private readonly ServiceModel model;
        private readonly MobiSimConnect simConnect;
        private readonly ProsimController prosimController;
        private readonly IGSXMenuService menuService;
        private readonly IGSXLoadsheetManager loadsheetManager;
        private readonly IGSXDoorManager doorManager;
        private IGSXCargoCoordinator cargoCoordinator;
        private IGSXFuelCoordinator fuelCoordinator;
        private readonly IAcarsService acarsService;
        
        // Service state variables
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
        private bool initialFuelSet = false;
        private bool initialFluidsSet = false;
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
        
        /// <summary>
        /// Event raised when a service status changes
        /// </summary>
        public event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;
        
        /// <summary>
        /// Initializes a new instance of the GSXServiceCoordinator class
        /// </summary>
        /// <param name="model">The service model</param>
        /// <param name="simConnect">The SimConnect instance</param>
        /// <param name="prosimController">The ProsimController instance</param>
        /// <param name="menuService">The GSX menu service</param>
        /// <param name="loadsheetManager">The GSX loadsheet manager</param>
        /// <param name="doorManager">The GSX door manager</param>
        /// <param name="cargoCoordinator">The GSX cargo coordinator</param>
        /// <param name="acarsService">The ACARS service</param>
        public GSXServiceCoordinator(
            ServiceModel model,
            MobiSimConnect simConnect,
            ProsimController prosimController,
            IGSXMenuService menuService,
            IGSXLoadsheetManager loadsheetManager,
            IGSXDoorManager doorManager,
            IGSXCargoCoordinator cargoCoordinator,
            IAcarsService acarsService)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.simConnect = simConnect ?? throw new ArgumentNullException(nameof(simConnect));
            this.prosimController = prosimController ?? throw new ArgumentNullException(nameof(prosimController));
            this.menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            this.loadsheetManager = loadsheetManager ?? throw new ArgumentNullException(nameof(loadsheetManager));
            this.doorManager = doorManager ?? throw new ArgumentNullException(nameof(doorManager));
            this.cargoCoordinator = cargoCoordinator; // Can be null initially, will be set later
            this.acarsService = acarsService ?? throw new ArgumentNullException(nameof(acarsService));
            
            // Subscribe to loadsheet manager events
            this.loadsheetManager.LoadsheetGenerated += OnLoadsheetGenerated;
            
            // Subscribe to door manager events
            this.doorManager.DoorStateChanged += OnDoorStateChanged;
        }
        
        /// <summary>
        /// Initializes the service coordinator
        /// </summary>
        public void Initialize()
        {
            ResetServiceStatus();
            Logger.Log(LogLevel.Information, "GSXServiceCoordinator:Initialize", "Service coordinator initialized");
        }
        
        /// <summary>
        /// Resets the service status
        /// </summary>
        public void ResetServiceStatus()
        {
            aftCargoDoorOpened = false;
            aftRightDoorOpened = false;
            forwardRightDoorOpened = false;
            boarding = false;
            forwardCargoDoorOpened = false;
            boardFinished = false;
            boardingRequested = false;
            cateringFinished = false;
            cateringRequested = false;
            connectCalled = false;
            deboarding = false;
            delay = 0;
            delayCounter = 0;
            equipmentRemoved = false;
            firstRun = true;
            initialFuelSet = false;
            initialFluidsSet = false;
            operatorWasSelected = false;
            opsCallsign = "";
            opsCallsignSet = false;
            paxPlanned = 0;
            pcaCalled = false;
            pcaRemoved = false;
            planePositioned = false;
            prelimLoadsheet = false;
            finalLoadsheetSend = false;
            prelimFlightData = false;
            pushFinished = false;
            pushNwsDisco = false;
            pushRunning = false;
            refuelFinished = false;
            refueling = false;
            refuelPaused = false;
            refuelRequested = false;
            
            Logger.Log(LogLevel.Information, "GSXServiceCoordinator:ResetServiceStatus", "Service status reset");
        }
        
        /// <summary>
        /// Runs loading services (refueling, catering, boarding)
        /// </summary>
        /// <param name="refuelState">The current refuel state</param>
        /// <param name="cateringState">The current catering state</param>
        public void RunLoadingServices(int refuelState, int cateringState)
        {
            if (model.AutoRefuel)
            {
                if (!initialFuelSet)
                {
                    prosimController.SetInitialFuel();
                    initialFuelSet = true;
                    OnServiceStatusChanged("Fuel", "Initial fuel set", false);
                }

                if (model.SetSaveHydraulicFluids && !initialFluidsSet)
                {
                    prosimController.SetInitialFluids();
                    initialFluidsSet = true;
                    OnServiceStatusChanged("Hydraulics", "Initial fluids set", false);
                }

                if (!refuelRequested && refuelState != 6)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Calling Refuel Service");
                    menuService.MenuOpen();
                    menuService.MenuItem(3);
                    refuelRequested = true;
                    OnServiceStatusChanged("Refuel", "Requested", false);
                    return;
                }

                if (model.CallCatering && !cateringRequested && cateringState != 6)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Calling Catering Service");
                    menuService.MenuOpen();
                    menuService.MenuItem(2);
                    OperatorSelection();
                    cateringRequested = true;
                    OnServiceStatusChanged("Catering", "Requested", false);
                    return;
                }
            }

            // Passenger door handling for catering has been moved to GSXServiceOrchestrator.CheckAllDoorToggles()
            // to ensure consistent handling with cargo doors

            if (!cateringFinished && cateringState == 6)
            {
                cateringFinished = true;
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Catering finished");
                OnServiceStatusChanged("Catering", "Completed", true);
                
                // Close forward right door when catering is finished (if not already closed)
                if (model.SetOpenCateringDoor && doorManager.IsForwardRightDoorOpen)
                {
                    doorManager.CloseDoor(DoorType.ForwardRight);
                }
                
                // Cargo doors will now be opened by the door manager in response to GSX Pro ground crew requests
                // The automatic door opening code has been removed
            }

            if (model.AutoBoarding)
            {
                if (!boardingRequested && refuelFinished && ((model.CallCatering && cateringFinished) || !model.CallCatering))
                {
                    // Add enhanced logging
                    if (delayCounter == 0)
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", 
                            $"Services complete - Waiting 90s before proceeding with cargo and boarding");
                    }

                    if (delayCounter < 90)
                    {
                        delayCounter++;
                    }
                    else
                    {
                        // Start cargo loading based on configuration
                        if (model.SetOpenCargoDoors)
                        {
                            if (model.CargoLoadingBeforeBoarding)
                            {
                                // Start cargo loading before boarding
                                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", 
                                    $"Starting cargo loading before boarding");
                                
                                bool cargoStarted = cargoCoordinator?.TryStartLoading(true) ?? false;
                                
                                if (cargoStarted)
                                {
                                    OnServiceStatusChanged("Cargo", "Loading started before boarding", false);
                                }
                                else
                                {
                                    Logger.Log(LogLevel.Warning, "GSXServiceCoordinator:RunLoadingServices", 
                                        $"Failed to start cargo loading before boarding");
                                }
                                
                                // Add a small delay to allow cargo loading to initialize
                                Thread.Sleep(1000);
                            }
                            else
                            {
                                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", 
                                    $"Cargo loading will start during boarding process");
                            }
                        }

                        // Then proceed with boarding
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Calling Boarding Service");
                        SetPassengers(prosimController.GetPaxPlanned());
                        menuService.MenuOpen();
                        menuService.MenuItem(4);
                        delayCounter = 0;
                        boardingRequested = true;
                        OnServiceStatusChanged("Boarding", "Requested", false);
                    }
                    return;
                }
            }

            if (!refueling && !refuelFinished && refuelState == 5)
            {
                refueling = true;
                refuelPaused = true;
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Fuel Service active");
                OnServiceStatusChanged("Refuel", "Active", false);
                prosimController.RefuelStart();
            }
            else if (refueling)
            {
                if (simConnect.ReadLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1)
                {
                    if (refuelPaused)
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Fuel Hose connected - refueling");
                        OnServiceStatusChanged("Refuel", "Hose connected", false);
                        refuelPaused = false;
                    }

                    if (prosimController.Refuel())
                    {
                        refueling = false;
                        refuelFinished = true;
                        refuelPaused = false;
                        prosimController.RefuelStop();
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Refuel completed");
                        OnServiceStatusChanged("Refuel", "Completed", true);
                    }
                }
                else
                {
                    if (!refuelPaused && !refuelFinished)
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Fuel Hose disconnected - waiting for next Truck");
                        OnServiceStatusChanged("Refuel", "Hose disconnected", false);
                        refuelPaused = true;
                    }
                }
            }

            if (!boarding && !boardFinished && simConnect.ReadLvar("FSDT_GSX_BOARDING_STATE") >= 4)
            {
                boarding = true;
                prosimController.BoardingStart();
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Boarding Service active");
                OnServiceStatusChanged("Boarding", "Active", false);
                
                // Start cargo loading during boarding if configured that way
                if (model.SetOpenCargoDoors && !model.CargoLoadingBeforeBoarding)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", 
                        $"Starting cargo loading during boarding process");
                    
                    bool cargoStarted = cargoCoordinator?.TryStartLoading(true) ?? false;
                    
                    if (cargoStarted)
                    {
                        OnServiceStatusChanged("Cargo", "Loading started during boarding", false);
                    }
                    else
                    {
                        Logger.Log(LogLevel.Warning, "GSXServiceCoordinator:RunLoadingServices", 
                            $"Failed to start cargo loading during boarding");
                    }
                }
            }
            else if (boarding)
            {
                // Check cargo loading percentage
                int cargoPercent = (int)simConnect.ReadLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
                
                // Close cargo doors when cargo loading reaches 100%
                if (cargoPercent == 100)
                {
                    if (doorManager.IsForwardCargoDoorOpen)
                    {
                        doorManager.CloseDoor(DoorType.ForwardCargo);
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed forward cargo door after loading");
                    }
                    
                    if (doorManager.IsAftCargoDoorOpen)
                    {
                        doorManager.CloseDoor(DoorType.AftCargo);
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed aft cargo door after loading");
                    }
                }
                
                // Check if boarding and cargo loading are complete
                if (prosimController.Boarding((int)simConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL"), cargoPercent) || simConnect.ReadLvar("FSDT_GSX_BOARDING_STATE") == 6)
                {
                    boarding = false;
                    boardFinished = true;
                    prosimController.BoardingStop();
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Boarding completed");
                    OnServiceStatusChanged("Boarding", "Completed", true);
                    
                    // Ensure cargo doors are closed when boarding is complete
                    if (doorManager.IsForwardCargoDoorOpen)
                    {
                        doorManager.CloseDoor(DoorType.ForwardCargo);
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed forward cargo door after boarding");
                    }
                    
                    if (doorManager.IsAftCargoDoorOpen)
                    {
                        doorManager.CloseDoor(DoorType.AftCargo);
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed aft cargo door after boarding");
                    }
                }
            }
        }
        
        /// <summary>
        /// Runs departure services (loadsheet, equipment removal, pushback)
        /// </summary>
        /// <param name="departureState">The current departure state</param>
        public void RunDepartureServices(int departureState)
        {
            if (model.ConnectPCA && !pcaRemoved)
            {
                // Check for APU started with APU bleed on, beacon on, and external power changed from on to Avail
                bool apuStarted = prosimController.GetStatusFunction("system.indicators.I_OH_ELEC_APU_START_U") != 0;
                bool apuBleedOn = prosimController.GetStatusFunction("system.switches.S_OH_PNEUMATIC_APU_BLEED") != 0;
                bool beaconOn = prosimController.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") != 0;
                bool extPowerAvail = prosimController.GetStatusFunction("system.indicators.I_OH_ELEC_EXT_PWR_L") == 0;
                
                if (apuStarted && apuBleedOn && beaconOn && extPowerAvail)
                {
                    prosimController.SetServicePCA(false);
                    pcaRemoved = true;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"APU Started with Bleed on, Beacon on, and External Power Avail - removing PCA");
                    OnServiceStatusChanged("PCA", "Removed", true);
                }
            }

            //LOADSHEET
            if (!IsFinalLoadsheetSent())
            {
                if (delay == 0)
                {
                    delay = new Random().Next(90, 150);
                    delayCounter = 0;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Final Loadsheet in {delay}s");
                }

                if (delayCounter < delay)
                {
                    delayCounter++;
                    return;
                }
                else
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Transmitting Final Loadsheet ...");
                    prosimController.TriggerFinal();
                    
                    if (model.UseAcars)
                    {
                        string flightNumber = prosimController.GetFMSFlightNumber();
                        if (!string.IsNullOrEmpty(flightNumber))
                        {
                            try
                            {
                                // Use the loadsheet manager to generate and send the final loadsheet
                                // We don't need to await this as it's a fire-and-forget operation
                                // The loadsheet manager will handle the async operation and raise an event when complete
                                _ = loadsheetManager.GenerateFinalLoadsheetAsync(flightNumber);
                                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Final loadsheet generation initiated");
                                OnServiceStatusChanged("Loadsheet", "Final generation initiated", false);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogLevel.Error, "GSXServiceCoordinator:RunDepartureServices", $"Error initiating final loadsheet generation: {ex.Message}");
                                // Mark as sent anyway to avoid getting stuck
                                finalLoadsheetSend = true;
                                OnServiceStatusChanged("Loadsheet", "Final generation failed", true);
                            }
                        }
                        else
                        {
                            Logger.Log(LogLevel.Warning, "GSXServiceCoordinator:RunDepartureServices", $"Flight number is empty, cannot send final loadsheet");
                            // Mark as sent anyway to avoid getting stuck
                            finalLoadsheetSend = true;
                            OnServiceStatusChanged("Loadsheet", "Final generation skipped (no flight number)", true);
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"ACARS is disabled, skipping final loadsheet");
                        // Mark as sent anyway to avoid getting stuck
                        finalLoadsheetSend = true;
                        OnServiceStatusChanged("Loadsheet", "Final generation skipped (ACARS disabled)", true);
                    }
                }
            }
            //EQUIPMENT
            else if (!equipmentRemoved)
            {
                //equipmentRemoved = simConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && simConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1 && simConnect.ReadLvar("I_OH_ELEC_EXT_PWR_L") == 0;
                if (prosimController.GetStatusFunction("system.switches.S_MIP_PARKING_BRAKE") == 1 && 
                    prosimController.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") == 1 && 
                    prosimController.GetStatusFunction("system.indicators.I_OH_ELEC_EXT_PWR_L") == 0) 
                { 
                    equipmentRemoved = true;
                };
                
                if (equipmentRemoved)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Preparing for Pushback - removing Equipment");
                    OnServiceStatusChanged("Equipment", "Removing for pushback", false);
                    
                    if (departureState < 4 && simConnect.ReadLvar("FSDT_GSX_JETWAY") != 2 && 
                        simConnect.ReadLvar("FSDT_GSX_JETWAY") == 5 && 
                        simConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE") < 3)
                    {
                        menuService.MenuOpen();
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Removing Jetway");
                        menuService.MenuItem(6);
                        OnServiceStatusChanged("Jetway", "Removing", false);
                    }
                    
                    prosimController.SetServiceChocks(false);
                    prosimController.SetServicePCA(false);
                    prosimController.SetServiceGPU(false);
                    OnServiceStatusChanged("Equipment", "Removed", true);
                }
            }
            //PUSHBACK
            else if (!pushFinished)
            {
                if (!model.SynchBypass)
                {
                    pushFinished = true;
                    OnServiceStatusChanged("Pushback", "Skipped (SynchBypass disabled)", true);
                    return;
                }

                double gs = simConnect.ReadSimVar("GPS GROUND SPEED", "Meters per second") * 0.00002966071308045356;
                if (!pushRunning && gs > 1.5 && (simConnect.ReadLvar("A_FC_THROTTLE_LEFT_INPUT") > 2.05 || simConnect.ReadLvar("A_FC_THROTTLE_RIGHT_INPUT") > 2.05))
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Push-Back was skipped");
                    OnServiceStatusChanged("Pushback", "Skipped (aircraft moving)", true);
                    pushFinished = true;
                    pushRunning = false;
                    return;
                }

                if (!pushRunning && departureState >= 4)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Push-Back Service is active");
                    OnServiceStatusChanged("Pushback", "Active", false);
                    pushRunning = true;
                }

                if (pushRunning)
                {
                    bool gsxPinInserted = simConnect.ReadLvar("FSDT_GSX_BYPASS_PIN") != 0;
                    if (gsxPinInserted && !pushNwsDisco)
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"By Pass Pin inserted");
                        OnServiceStatusChanged("Pushback", "Bypass pin inserted", false);
                        simConnect.WriteLvar("FSDT_VAR_Frozen", 1);
                        pushNwsDisco = true;
                    }
                    else if (gsxPinInserted && pushNwsDisco)
                    {
                        bool isFrozen = simConnect.ReadLvar("FSDT_VAR_Frozen") == 1;

                        if (!isFrozen)
                        {
                            Logger.Log(LogLevel.Debug, "GSXServiceCoordinator:RunDepartureServices", $"Re-Freezing Plane");
                            simConnect.WriteLvar("FSDT_VAR_Frozen", 1);
                        }
                    }

                    if (!gsxPinInserted && pushNwsDisco)
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"By Pass Pin removed");
                        OnServiceStatusChanged("Pushback", "Bypass pin removed", false);
                        simConnect.WriteLvar("FSDT_VAR_Frozen", 0);
                        pushNwsDisco = false;
                        pushRunning = false;
                        pushFinished = true;
                        OnServiceStatusChanged("Pushback", "Completed", true);
                    }
                }
            }
        }
        
        /// <summary>
        /// Runs arrival services (jetway/stairs, PCA, GPU, chocks)
        /// </summary>
        /// <param name="deboardState">The current deboard state</param>
        public void RunArrivalServices(int deboardState)
        {
            if (simConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Couatl Engine not running");
                return;
            }

            if (model.AutoConnect && !connectCalled)
            {
                CallJetwayStairs();
                connectCalled = true;
                return;
            }

            if (simConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1)
                return;

            if (model.ConnectPCA && !pcaCalled && (!model.PcaOnlyJetways || (model.PcaOnlyJetways && simConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Connecting PCA");
                prosimController.SetServicePCA(true);
                pcaCalled = true;
                OnServiceStatusChanged("PCA", "Connected", true);
            }

            Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Setting GPU and Chocks");
            prosimController.SetServiceChocks(true);
            prosimController.SetServiceGPU(true);
            OnServiceStatusChanged("Equipment", "GPU and chocks set", true);
            
            SetPassengers(prosimController.GetPaxPlanned());

            if (model.AutoDeboarding && deboardState < 4)
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Calling Deboarding Service");
                SetPassengers(prosimController.GetPaxPlanned());
                menuService.MenuOpen();
                menuService.MenuItem(1);
                OnServiceStatusChanged("Deboarding", "Requested", false);
                
                if (!model.AutoConnect)
                    OperatorSelection();
            }

            if (model.SetSaveFuel)
            {
                double arrivalFuel = prosimController.GetFuelAmount();
                model.SavedFuelAmount = arrivalFuel;
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Saved fuel amount: {arrivalFuel}");
                OnServiceStatusChanged("Fuel", "Saved arrival amount", true);
            }

            if (model.SetSaveHydraulicFluids)
            {
                var hydraulicFluids = prosimController.GetHydraulicFluidValues();
                model.HydaulicsBlueAmount = hydraulicFluids.Item1;
                model.HydaulicsGreenAmount = hydraulicFluids.Item2;
                model.HydaulicsYellowAmount = hydraulicFluids.Item3;
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Saved hydraulic fluid values");
                OnServiceStatusChanged("Hydraulics", "Saved arrival values", true);
            }
        }
        
        /// <summary>
        /// Runs deboarding service
        /// </summary>
        /// <param name="deboardState">The current deboard state</param>
        public void RunDeboardingService(int deboardState)
        {
            if (!deboarding)
            {
                deboarding = true;
                prosimController.DeboardingStart();
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDeboardingService", $"Deboarding Service active");
                OnServiceStatusChanged("Deboarding", "Active", false);
                return;
            }
            else if (deboarding)
            {
                if (simConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS") != paxPlanned)
                {
                    Logger.Log(LogLevel.Warning, "GSXServiceCoordinator:RunDeboardingService", $"Passenger changed during Boarding! Trying to reset Number ...");
                    simConnect.WriteLvar("FSDT_GSX_NUMPASSENGERS", paxPlanned);
                }

                int paxCurrent = (int)simConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS") - (int)simConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
                int cargoPercent = (int)simConnect.ReadLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");
                
                if (prosimController.Deboarding(paxCurrent, cargoPercent) || deboardState == 6 || deboardState == 1)
                {
                    deboarding = false;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDeboardingService", $"Deboarding finished (GSX State {deboardState})");
                    prosimController.DeboardingStop();
                    OnServiceStatusChanged("Deboarding", "Completed", true);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Sets the number of passengers
        /// </summary>
        /// <param name="numPax">The number of passengers</param>
        public void SetPassengers(int numPax)
        {
            simConnect.WriteLvar("FSDT_GSX_NUMPASSENGERS", numPax);
            paxPlanned = numPax;
            Logger.Log(LogLevel.Information, "GSXServiceCoordinator:SetPassengers", $"Passenger Count set to {numPax}");
            
            if (model.DisableCrew)
            {
                simConnect.WriteLvar("FSDT_GSX_CREW_NOT_DEBOARDING", 1);
                simConnect.WriteLvar("FSDT_GSX_CREW_NOT_BOARDING", 1);
                simConnect.WriteLvar("FSDT_GSX_PILOTS_NOT_DEBOARDING", 1);
                simConnect.WriteLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1);
                simConnect.WriteLvar("FSDT_GSX_NUMCREW", 0);
                simConnect.WriteLvar("FSDT_GSX_NUMPILOTS", 0);
                simConnect.WriteLvar("FSDT_GSX_CREW_ON_BOARD", 1);
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:SetPassengers", $"Crew Boarding disabled");
            }
            
            OnServiceStatusChanged("Passengers", $"Count set to {numPax}", true);
        }
        
        /// <summary>
        /// Calls jetway and/or stairs
        /// </summary>
        public void CallJetwayStairs()
        {
            menuService.MenuOpen();

            if (simConnect.ReadLvar("FSDT_GSX_JETWAY") != 2 && simConnect.ReadLvar("FSDT_GSX_JETWAY") != 5 && simConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE") < 3)
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:CallJetwayStairs", $"Calling Jetway");
                menuService.MenuItem(6);
                OperatorSelection();
                OnServiceStatusChanged("Jetway", "Requested", false);

                // Only call stairs if JetwayOnly is false
                if (!model.JetwayOnly && simConnect.ReadLvar("FSDT_GSX_STAIRS") != 2 && simConnect.ReadLvar("FSDT_GSX_STAIRS") != 5 && simConnect.ReadLvar("FSDT_GSX_OPERATESTAIRS_STATE") < 3)
                {
                    Thread.Sleep(1500);
                    menuService.MenuOpen();
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:CallJetwayStairs", $"Calling Stairs");
                    menuService.MenuItem(7);
                    OnServiceStatusChanged("Stairs", "Requested", false);
                }
                else if (model.JetwayOnly)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:CallJetwayStairs", $"Jetway Only mode - skipping stairs");
                }
            }
            else if (!model.JetwayOnly && simConnect.ReadLvar("FSDT_GSX_STAIRS") != 5 && simConnect.ReadLvar("FSDT_GSX_OPERATESTAIRS_STATE") < 3)
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:CallJetwayStairs", $"Calling Stairs");
                menuService.MenuItem(7);
                OperatorSelection();
                OnServiceStatusChanged("Stairs", "Requested", false);
            }
            else if (model.JetwayOnly)
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:CallJetwayStairs", $"Jetway Only mode - skipping stairs");
            }
        }
        
        /// <summary>
        /// Checks if refueling is complete
        /// </summary>
        /// <returns>True if refueling is complete, false otherwise</returns>
        public bool IsRefuelingComplete()
        {
            return refuelFinished;
        }
        
        /// <summary>
        /// Checks if boarding is complete
        /// </summary>
        /// <returns>True if boarding is complete, false otherwise</returns>
        public bool IsBoardingComplete()
        {
            return boardFinished;
        }
        
        /// <summary>
        /// Checks if catering is complete
        /// </summary>
        /// <returns>True if catering is complete, false otherwise</returns>
        public bool IsCateringComplete()
        {
            return cateringFinished;
        }
        
        /// <summary>
        /// Checks if the loadsheet has been sent
        /// </summary>
        /// <returns>True if the loadsheet has been sent, false otherwise</returns>
        public bool IsFinalLoadsheetSent()
        {
            return finalLoadsheetSend;
        }
        
        /// <summary>
        /// Checks if the preliminary loadsheet has been sent
        /// </summary>
        /// <returns>True if the preliminary loadsheet has been sent, false otherwise</returns>
        public bool IsPreliminaryLoadsheetSent()
        {
            return prelimLoadsheet;
        }
        
        /// <summary>
        /// Checks if equipment has been removed
        /// </summary>
        /// <returns>True if equipment has been removed, false otherwise</returns>
        public bool IsEquipmentRemoved()
        {
            return equipmentRemoved;
        }
        
        /// <summary>
        /// Checks if pushback is complete
        /// </summary>
        /// <returns>True if pushback is complete, false otherwise</returns>
        public bool IsPushbackComplete()
        {
            return pushFinished;
        }
        
        /// <summary>
        /// Checks if deboarding is complete
        /// </summary>
        /// <returns>True if deboarding is complete, false otherwise</returns>
        public bool IsDeboardingComplete()
        {
            return !deboarding && simConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE") == 6;
        }
        
        /// <summary>
        /// Handles operator selection for GSX services
        /// </summary>
        private void OperatorSelection()
        {
            menuService.OperatorSelection();
            operatorWasSelected = menuService.OperatorWasSelected;
        }
        
        /// <summary>
        /// Event handler for loadsheet manager events
        /// </summary>
        private void OnLoadsheetGenerated(object sender, LoadsheetGeneratedEventArgs e)
        {
            // Update local state variables based on loadsheet type
            if (e.LoadsheetType.Equals("prelim", StringComparison.OrdinalIgnoreCase))
            {
                prelimLoadsheet = true;
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:OnLoadsheetGenerated", 
                    $"Preliminary loadsheet for flight {e.FlightNumber} generated {(e.Success ? "successfully" : "with errors")} at {e.Timestamp:HH:mm:ss}");
                OnServiceStatusChanged("Loadsheet", "Preliminary generated", true);
            }
            else if (e.LoadsheetType.Equals("final", StringComparison.OrdinalIgnoreCase))
            {
                finalLoadsheetSend = true;
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:OnLoadsheetGenerated", 
                    $"Final loadsheet for flight {e.FlightNumber} generated {(e.Success ? "successfully" : "with errors")} at {e.Timestamp:HH:mm:ss}");
                OnServiceStatusChanged("Loadsheet", "Final generated", true);
            }
        }
        
        /// <summary>
        /// Event handler for door manager events
        /// </summary>
        private void OnDoorStateChanged(object sender, DoorStateChangedEventArgs e)
        {
            // Update local state variables based on door state changes
            switch (e.DoorType)
            {
                case DoorType.ForwardRight:
                    forwardRightDoorOpened = e.IsOpen;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:OnDoorStateChanged", 
                        $"Forward right door is now {(e.IsOpen ? "open" : "closed")}");
                    OnServiceStatusChanged("Door", $"Forward right door {(e.IsOpen ? "opened" : "closed")}", true);
                    break;
                case DoorType.AftRight:
                    aftRightDoorOpened = e.IsOpen;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:OnDoorStateChanged", 
                        $"Aft right door is now {(e.IsOpen ? "open" : "closed")}");
                    OnServiceStatusChanged("Door", $"Aft right door {(e.IsOpen ? "opened" : "closed")}", true);
                    break;
                case DoorType.ForwardCargo:
                    forwardCargoDoorOpened = e.IsOpen;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:OnDoorStateChanged", 
                        $"Forward cargo door is now {(e.IsOpen ? "open" : "closed")}");
                    OnServiceStatusChanged("Door", $"Forward cargo door {(e.IsOpen ? "opened" : "closed")}", true);
                    break;
                case DoorType.AftCargo:
                    aftCargoDoorOpened = e.IsOpen;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:OnDoorStateChanged", 
                        $"Aft cargo door is now {(e.IsOpen ? "open" : "closed")}");
                    OnServiceStatusChanged("Door", $"Aft cargo door {(e.IsOpen ? "opened" : "closed")}", true);
                    break;
            }
        }
        
        /// <summary>
        /// Raises the ServiceStatusChanged event
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="status">The current status of the service</param>
        /// <param name="isCompleted">Whether the service is completed</param>
        protected virtual void OnServiceStatusChanged(string serviceType, string status, bool isCompleted)
        {
            ServiceStatusChanged?.Invoke(this, new ServiceStatusChangedEventArgs(serviceType, status, isCompleted));
        }
        
        /// <summary>
        /// Gets the door manager instance
        /// </summary>
        /// <returns>The door manager instance</returns>
        public IGSXDoorManager GetDoorManager()
        {
            return doorManager;
        }
        
        /// <summary>
        /// Sets the cargo coordinator after construction
        /// </summary>
        /// <param name="cargoCoordinator">The cargo coordinator to set</param>
        public void SetCargoCoordinator(IGSXCargoCoordinator cargoCoordinator)
        {
            this.cargoCoordinator = cargoCoordinator ?? throw new ArgumentNullException(nameof(cargoCoordinator));
            Logger.Log(LogLevel.Information, "GSXServiceCoordinator:SetCargoCoordinator", "Cargo coordinator set");
        }
        
        /// <summary>
        /// Sets the fuel coordinator after construction
        /// </summary>
        /// <param name="fuelCoordinator">The fuel coordinator to set</param>
        public void SetFuelCoordinator(IGSXFuelCoordinator fuelCoordinator)
        {
            this.fuelCoordinator = fuelCoordinator ?? throw new ArgumentNullException(nameof(fuelCoordinator));
            Logger.Log(LogLevel.Information, "GSXServiceCoordinator:SetFuelCoordinator", "Fuel coordinator set");
        }
        
        /// <summary>
        /// Gets the fuel coordinator instance
        /// </summary>
        /// <returns>The fuel coordinator instance</returns>
        public IGSXFuelCoordinator GetFuelCoordinator()
        {
            return fuelCoordinator;
        }
    }
}
