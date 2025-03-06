# Phase 3.3: GSXServiceCoordinator Implementation

## Overview

This document outlines the implementation plan for Phase 3.3 of the Prosim2GSX modularization strategy. In this phase, we'll extract service coordination functionality from the GsxController into a dedicated service.

## Implementation Steps

### 1. Create ServiceOperationEventArgs.cs

Create a new event args class in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for service operations
    /// </summary>
    public class ServiceOperationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the service operation type
        /// </summary>
        public ServiceOperationType OperationType { get; }
        
        /// <summary>
        /// Gets the operation status
        /// </summary>
        public ServiceOperationStatus Status { get; }
        
        /// <summary>
        /// Gets the timestamp of the operation
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Gets additional data related to the operation
        /// </summary>
        public object Data { get; }
        
        /// <summary>
        /// Initializes a new instance of the ServiceOperationEventArgs class
        /// </summary>
        /// <param name="operationType">The service operation type</param>
        /// <param name="status">The operation status</param>
        /// <param name="data">Additional data related to the operation</param>
        public ServiceOperationEventArgs(ServiceOperationType operationType, ServiceOperationStatus status, object data = null)
        {
            OperationType = operationType;
            Status = status;
            Timestamp = DateTime.Now;
            Data = data;
        }
    }
    
    /// <summary>
    /// Service operation types
    /// </summary>
    public enum ServiceOperationType
    {
        Boarding,
        Deboarding,
        Refueling,
        Catering,
        GroundEquipment,
        Jetway,
        Stairs,
        Pushback
    }
    
    /// <summary>
    /// Service operation status
    /// </summary>
    public enum ServiceOperationStatus
    {
        Started,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
}
```

### 2. Create IGSXServiceCoordinator.cs

Create a new interface file in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX service coordination
    /// </summary>
    public interface IGSXServiceCoordinator
    {
        /// <summary>
        /// Event raised when a service operation status changes
        /// </summary>
        event EventHandler<ServiceOperationEventArgs> ServiceOperationStatusChanged;
        
        /// <summary>
        /// Initializes the service coordinator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Runs loading services (boarding, refueling, catering)
        /// </summary>
        /// <param name="refuelState">The current refueling state</param>
        /// <param name="cateringState">The current catering state</param>
        /// <param name="boardingState">The current boarding state</param>
        /// <param name="boardingCargoPercent">The current boarding cargo percentage</param>
        /// <param name="boardingPassengerCount">The current boarding passenger count</param>
        void RunLoadingServices(int refuelState, int cateringState, int boardingState, int boardingCargoPercent, int boardingPassengerCount);
        
        /// <summary>
        /// Runs departure services (loadsheet, ground equipment removal, pushback)
        /// </summary>
        /// <param name="departureState">The current departure state</param>
        /// <param name="jetwayState">The current jetway state</param>
        /// <param name="jetwayOperateState">The current jetway operate state</param>
        /// <param name="gsxPinInserted">Whether the GSX bypass pin is inserted</param>
        /// <param name="isFrozen">Whether the aircraft is frozen</param>
        /// <param name="groundSpeed">The current ground speed</param>
        /// <param name="throttleLeftInput">The current left throttle input</param>
        /// <param name="throttleRightInput">The current right throttle input</param>
        void RunDepartureServices(int departureState, int jetwayState, int jetwayOperateState, bool gsxPinInserted, bool isFrozen, double groundSpeed, double throttleLeftInput, double throttleRightInput);
        
        /// <summary>
        /// Runs arrival services (jetway/stairs, ground equipment)
        /// </summary>
        /// <param name="deboardState">The current deboarding state</param>
        /// <param name="couatlStarted">Whether the Couatl engine is started</param>
        /// <param name="beaconOn">Whether the beacon is on</param>
        void RunArrivalServices(int deboardState, bool couatlStarted, bool beaconOn);
        
        /// <summary>
        /// Runs deboarding service
        /// </summary>
        /// <param name="deboardState">The current deboarding state</param>
        /// <param name="plannedPassengers">The planned passenger count</param>
        /// <param name="currentPassengers">The current passenger count</param>
        /// <param name="deboardingPassengerCount">The deboarding passenger count</param>
        /// <param name="deboardingCargoPercent">The deboarding cargo percentage</param>
        void RunDeboardingService(int deboardState, int plannedPassengers, int currentPassengers, int deboardingPassengerCount, int deboardingCargoPercent);
        
        /// <summary>
        /// Sets the passenger count in GSX
        /// </summary>
        /// <param name="numPax">The number of passengers</param>
        void SetPassengers(int numPax);
        
        /// <summary>
        /// Calls jetway and/or stairs
        /// </summary>
        void CallJetwayStairs();
        
        /// <summary>
        /// Resets the service coordinator state
        /// </summary>
        void Reset();
        
        /// <summary>
        /// Gets whether boarding is finished
        /// </summary>
        bool IsBoardingFinished { get; }
        
        /// <summary>
        /// Gets whether refueling is finished
        /// </summary>
        bool IsRefuelingFinished { get; }
        
        /// <summary>
        /// Gets whether catering is finished
        /// </summary>
        bool IsCateringFinished { get; }
        
        /// <summary>
        /// Gets whether the final loadsheet has been sent
        /// </summary>
        bool IsFinalLoadsheetSent { get; }
        
        /// <summary>
        /// Gets whether equipment has been removed
        /// </summary>
        bool IsEquipmentRemoved { get; }
        
        /// <summary>
        /// Gets whether pushback is finished
        /// </summary>
        bool IsPushbackFinished { get; }
    }
}
```

### 3. Create GSXServiceCoordinator.cs

Create a new implementation file in the Services folder:

```csharp
using System;
using System.Threading;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX service coordination
    /// </summary>
    public class GSXServiceCoordinator : IGSXServiceCoordinator
    {
        private readonly ServiceModel model;
        private readonly MobiSimConnect simConnect;
        private readonly ProsimController prosimController;
        private readonly IAcarsService acarsService;
        private readonly IGSXMenuService menuService;
        
        // Service state flags
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
        private bool initialFuelSet = false;
        private bool initialFluidsSet = false;
        private double macZfw = 0.0d;
        private string opsCallsign = "";
        private bool opsCallsignSet = false;
        private int paxPlanned = 0;
        private bool pcaCalled = false;
        private bool pcaRemoved = false;
        private bool planePositioned = false;
        private bool prelimFlightData = false;
        private double prelimFuel = 0d;
        private bool prelimLoadsheet = false;
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
        
        /// <summary>
        /// Event raised when a service operation status changes
        /// </summary>
        public event EventHandler<ServiceOperationEventArgs> ServiceOperationStatusChanged;
        
        /// <summary>
        /// Gets whether boarding is finished
        /// </summary>
        public bool IsBoardingFinished => boardFinished;
        
        /// <summary>
        /// Gets whether refueling is finished
        /// </summary>
        public bool IsRefuelingFinished => refuelFinished;
        
        /// <summary>
        /// Gets whether catering is finished
        /// </summary>
        public bool IsCateringFinished => cateringFinished;
        
        /// <summary>
        /// Gets whether the final loadsheet has been sent
        /// </summary>
        public bool IsFinalLoadsheetSent => finalLoadsheetSend;
        
        /// <summary>
        /// Gets whether equipment has been removed
        /// </summary>
        public bool IsEquipmentRemoved => equipmentRemoved;
        
        /// <summary>
        /// Gets whether pushback is finished
        /// </summary>
        public bool IsPushbackFinished => pushFinished;
        
        /// <summary>
        /// Initializes a new instance of the GSXServiceCoordinator class
        /// </summary>
        public GSXServiceCoordinator(ServiceModel model, MobiSimConnect simConnect, ProsimController prosimController, IAcarsService acarsService, IGSXMenuService menuService)
        {
            this.model = model;
            this.simConnect = simConnect;
            this.prosimController = prosimController;
            this.acarsService = acarsService;
            this.menuService = menuService;
        }
        
        /// <summary>
        /// Initializes the service coordinator
        /// </summary>
        public void Initialize()
        {
            Reset();
            Logger.Log(LogLevel.Information, "GSXServiceCoordinator:Initialize", "Service coordinator initialized");
        }
        
        /// <summary>
        /// Resets the service coordinator state
        /// </summary>
        public void Reset()
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
            finalFuel = 0d;
            finalLoadsheetSend = false;
            finalMacTow = 00.0d;
            finalMacZfw = 00.0d;
            finalPax = 0;
            finalTow = 00.0d;
            finalZfw = 00.0d;
            firstRun = true;
            initialFuelSet = false;
            initialFluidsSet = false;
            macZfw = 0.0d;
            opsCallsign = "";
            opsCallsignSet = false;
            paxPlanned = 0;
            pcaCalled = false;
            pcaRemoved = false;
            planePositioned = false;
            prelimFlightData = false;
            prelimFuel = 0d;
            prelimLoadsheet = false;
            prelimMacTow = 00.0d;
            prelimMacZfw = 00.0d;
            prelimPax = 0;
            prelimTow = 00.0d;
            prelimZfw = 00.0d;
            pushFinished = false;
            pushNwsDisco = false;
            pushRunning = false;
            refuelFinished = false;
            refueling = false;
            refuelPaused = false;
            refuelRequested = false;
            
            Logger.Log(LogLevel.Information, "GSXServiceCoordinator:Reset", "Service coordinator state reset");
        }
        
        /// <summary>
        /// Runs loading services (boarding, refueling, catering)
        /// </summary>
        public void RunLoadingServices(int refuelState, int cateringState, int boardingState, int boardingCargoPercent, int boardingPassengerCount)
        {
            if (model.AutoRefuel)
            {
                if (!initialFuelSet)
                {
                    prosimController.SetInitialFuel();
                    initialFuelSet = true;
                }

                if (model.SetSaveHydraulicFluids && !initialFluidsSet)
                {
                    prosimController.SetInitialFluids();
                    initialFluidsSet = true;
                }

                if (!refuelRequested && refuelState != 6)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Calling Refuel Service");
                    menuService.MenuOpen();
                    menuService.MenuItem(3);
                    refuelRequested = true;
                    OnServiceOperationStatusChanged(ServiceOperationType.Refueling, ServiceOperationStatus.Started);
                    return;
                }

                if (model.CallCatering && !cateringRequested && cateringState != 6)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Calling Catering Service");
                    menuService.MenuOpen();
                    menuService.MenuItem(2);
                    menuService.OperatorSelection();
                    cateringRequested = true;
                    OnServiceOperationStatusChanged(ServiceOperationType.Catering, ServiceOperationStatus.Started);
                    return;
                }
            }

            // Handle doors for catering
            if (model.SetOpenCateringDoor)
            {
                // Check if catering service is waiting for forward door to be opened
                if (simConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE") == 1 && !forwardRightDoorOpened)
                {
                    prosimController.SetForwardRightDoor(true);
                    forwardRightDoorOpened = true;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Opened forward right door for catering (service 1 toggle)");
                }
                
                // Check if catering service is waiting for aft door to be closed
                if (simConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE") == 1 && forwardRightDoorOpened)
                {
                    prosimController.SetForwardRightDoor(false);
                    forwardRightDoorOpened = false;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed forward right door to complete catering service (service 1 toggle)");
                }

                // Check if catering service is waiting for door to be opened
                if (simConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE") == 1 && !aftRightDoorOpened)
                {
                    prosimController.SetAftRightDoor(true);
                    aftRightDoorOpened = true;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Opened aft right door for catering (service 2 toggle)");
                }

                // Check if catering service is waiting for door to be closed
                if (simConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE") == 1 && aftRightDoorOpened)
                {
                    prosimController.SetAftRightDoor(false);
                    aftRightDoorOpened = false;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed aft right door to complete catering service (service 2 toggle)");
                }
            }

            if (!cateringFinished && cateringState == 6)
            {
                cateringFinished = true;
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Catering finished");
                OnServiceOperationStatusChanged(ServiceOperationType.Catering, ServiceOperationStatus.Completed);
                
                // Close forward right door when catering is finished (if not already closed)
                if (model.SetOpenCateringDoor && forwardRightDoorOpened)
                {
                    prosimController.SetForwardRightDoor(false);
                    forwardRightDoorOpened = false;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed forward right door after catering");
                }
                
                // Open cargo doors after catering is finished if enabled
                if (model.SetOpenCargoDoors)
                {
                    prosimController.SetForwardCargoDoor(true);
                    prosimController.SetAftCargoDoor(true);
                    forwardCargoDoorOpened = true;
                    aftCargoDoorOpened = true;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Opened cargo doors for loading");
                }
            }

            if (model.AutoBoarding)
            {
                if (!boardingRequested && refuelFinished && ((model.CallCatering && cateringFinished) || !model.CallCatering))
                {
                    if (delayCounter == 0)
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Waiting 90s before calling Boarding");

                    if (delayCounter < 90)
                        delayCounter++;
                    else
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Calling Boarding Service");
                        SetPassengers(prosimController.GetPaxPlanned());
                        menuService.MenuOpen();
                        menuService.MenuItem(4);
                        delayCounter = 0;
                        boardingRequested = true;
                        OnServiceOperationStatusChanged(ServiceOperationType.Boarding, ServiceOperationStatus.Started);
                    }
                    return;
                }
            }

            if (!refueling && !refuelFinished && refuelState == 5)
            {
                refueling = true;
                refuelPaused = true;
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Fuel Service active");
                prosimController.RefuelStart();
                OnServiceOperationStatusChanged(ServiceOperationType.Refueling, ServiceOperationStatus.InProgress);
            }
            else if (refueling)
            {
                if (simConnect.ReadLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1)
                {
                    if (refuelPaused)
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Fuel Hose connected - refueling");
                        refuelPaused = false;
                    }

                    if (prosimController.Refuel())
                    {
                        refueling = false;
                        refuelFinished = true;
                        refuelPaused = false;
                        prosimController.RefuelStop();
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Refuel completed");
                        OnServiceOperationStatusChanged(ServiceOperationType.Refueling, ServiceOperationStatus.Completed);
                    }
                }
                else
                {
                    if (!refuelPaused && !refuelFinished)
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Fuel Hose disconnected - waiting for next Truck");
                        refuelPaused = true;
                    }
                }
            }

            if (!boarding && !boardFinished && boardingState >= 4)
            {
                boarding = true;
                prosimController.BoardingStart();
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Boarding Service active");
                OnServiceOperationStatusChanged(ServiceOperationType.Boarding, ServiceOperationStatus.InProgress);
            }
            else if (boarding)
            {
                // Check cargo loading percentage
                
                // Close cargo doors when cargo loading reaches 100%
                if (boardingCargoPercent == 100)
                {
                    if (forwardCargoDoorOpened)
                    {
                        prosimController.SetForwardCargoDoor(false);
                        forwardCargoDoorOpened = false;
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed forward cargo door after loading");
                    }
                    
                    if (aftCargoDoorOpened)
                    {
                        prosimController.SetAftCargoDoor(false);
                        aftCargoDoorOpened = false;
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed aft cargo door after loading");
                    }
                }
                
                // Check if boarding and cargo loading are complete
                if (prosimController.Boarding(boardingPassengerCount, boardingCargoPercent) || boardingState == 6)
                {
                    boarding = false;
                    boardFinished = true;
                    prosimController.BoardingStop();
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Boarding completed");
                    OnServiceOperationStatusChanged(ServiceOperationType.Boarding, ServiceOperationStatus.Completed);
                    
                    // Ensure cargo doors are closed when boarding is complete
                    if (forwardCargoDoorOpened)
                    {
                        prosimController.SetForwardCargoDoor(false);
                        forwardCargoDoorOpened = false;
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed forward cargo door after boarding");
                    }
                    
                    if (aftCargoDoorOpened)
                    {
                        prosimController.SetAftCargoDoor(false);
                        aftCargoDoorOpened = false;
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunLoadingServices", $"Closed aft cargo door after boarding");
                    }
                }
            }
        }
        
        /// <summary>
        /// Runs departure services (loadsheet, ground equipment removal, pushback)
        /// </summary>
        public void RunDepartureServices(int departureState, int jetwayState, int jetwayOperateState, bool gsxPinInserted, bool isFrozen, double groundSpeed, double throttleLeftInput, double throttleRightInput)
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
                }
            }

            //LOADSHEET
            if (!finalLoadsheetSend)
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
                    finalLoadsheetSend = true;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Final Loadsheet sent to ACARS");
                    if (model.UseAcars)
                    {
                        var finalLoadedData = prosimController.GetLoadedData("final");
                        var prelimData = (prelimZfw, prelimTow, prelimPax, prelimMacZfw, prelimMacTow, prelimFuel);
                        System.Threading.Tasks.Task task = acarsService.SendFinalLoadsheetAsync(prosimController.GetFMSFlightNumber(), finalLoadedData, prelimData);
                    }
                }
            }
            //EQUIPMENT
            else if (!equipmentRemoved)
            {
                //equipmentRemoved = simConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && simConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1 && simConnect.ReadLvar("I_OH_ELEC_EXT_PWR_L") == 0;
                if (prosimController.GetStatusFunction("system.switches.S_MIP_PARKING_BRAKE") == 1 && prosimController.GetStatusFunction("system.switches.S_OH_EXT_LT_BEACON") == 1 && prosimController.GetStatusFunction("system.indicators.I_OH_ELEC_EXT_PWR_L") == 0) { equipmentRemoved = true;};
                if (equipmentRemoved)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Preparing for Pushback - removing Equipment");
                    if (departureState < 4 && jetwayState != 2 && jetwayState == 5 && jetwayOperateState < 3)
                    {
                        menuService.MenuOpen();
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Removing Jetway");
                        menuService.MenuItem(6);
                    }
                    prosimController.SetServiceChocks(false);
                    prosimController.SetServicePCA(false);
                    prosimController.SetServiceGPU(false);
                    OnServiceOperationStatusChanged(ServiceOperationType.GroundEquipment, ServiceOperationStatus.Completed);
                }
            }
            //PUSHBACK
            else if (!pushFinished)
            {
                if (!model.SynchBypass)
                {
                    pushFinished = true;
                    return;
                }

                if (!pushRunning && groundSpeed > 1.5 && (throttleLeftInput > 2.05 || throttleRightInput > 2.05))
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Push-Back was skipped");
                    pushFinished = true;
                    pushRunning = false;
                    OnServiceOperationStatusChanged(ServiceOperationType.Pushback, ServiceOperationStatus.Completed);
                    return;
                }

                if (!pushRunning && departureState >= 4)
                {
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"Push-Back Service is active");
                    pushRunning = true;
                    OnServiceOperationStatusChanged(ServiceOperationType.Pushback, ServiceOperationStatus.Started);
                }

                if (pushRunning)
                {
                    if (gsxPinInserted && !pushNwsDisco)
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"By Pass Pin inserted");
                        simConnect.WriteLvar("FSDT_VAR_Frozen", 1);
                        pushNwsDisco = true;
                    }
                    else if (gsxPinInserted && pushNwsDisco)
                    {
                        if (!isFrozen)
                        {
                            Logger.Log(LogLevel.Debug, "GSXServiceCoordinator:RunDepartureServices", $"Re-Freezing Plane");
                            simConnect.WriteLvar("FSDT_VAR_Frozen", 1);
                        }
                    }

                    if (!gsxPinInserted && pushNwsDisco)
                    {
                        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDepartureServices", $"By Pass Pin removed");
                        simConnect.WriteLvar("FSDT_VAR_Frozen", 0);
                        pushNwsDisco = false;
                        pushRunning = false;
                        pushFinished = true;
                        OnServiceOperationStatusChanged(ServiceOperationType.Pushback, ServiceOperationStatus.Completed);
                    }
                }
            }
        }
        
        /// <summary>
        /// Runs arrival services (jetway/stairs, ground equipment)
        /// </summary>
        public void RunArrivalServices(int deboardState, bool couatlStarted, bool beaconOn)
        {
            if (!couatlStarted)
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Couatl Engine not running");
                return;
            }

            if (beaconOn)
                return;

            if (model.AutoConnect && !connectCalled)
            {
                CallJetwayStairs();
                connectCalled = true;
                return;
            }

            if (model.ConnectPCA && !pcaCalled && (!model.PcaOnlyJetways || (model.PcaOnlyJetways && simConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Connecting PCA");
                prosimController.SetServicePCA(true);
                pcaCalled = true;
            }

            Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Setting GPU and Chocks");
            prosimController.SetServiceChocks(true);
            prosimController.SetServiceGPU(true);
            SetPassengers(prosimController.GetPaxPlanned());

            OnServiceOperationStatusChanged(ServiceOperationType.GroundEquipment, ServiceOperationStatus.Completed);

            if (model.AutoDeboarding && deboardState < 4)
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunArrivalServices", $"Calling Deboarding Service");
                SetPassengers(prosimController.GetPaxPlanned());
                menuService.MenuOpen();
                menuService.MenuItem(1);
                if (!model.AutoConnect)
                    menuService.OperatorSelection();
                
                OnServiceOperationStatusChanged(ServiceOperationType.Deboarding, ServiceOperationStatus.Started);
            }

            if (model.SetSaveFuel)
            {
                double arrivalFuel = prosimController.GetFuelAmount();
                model.SavedFuelAmount = arrivalFuel;
            }

            if (model.SetSaveHydraulicFluids)
            {
                var hydraulicFluids = prosimController.GetHydraulicFluidValues();
                model.HydaulicsBlueAmount = hydraulicFluids.Item1;
                model.HydaulicsGreenAmount = hydraulicFluids.Item2;
                model.HydaulicsYellowAmount = hydraulicFluids.Item3;
            }
        }
        
        /// <summary>
        /// Runs deboarding service
        /// </summary>
        public void RunDeboardingService(int deboardState, int plannedPassengers, int currentPassengers, int deboardingPassengerCount, int deboardingCargoPercent)
        {
            if (!deboarding)
            {
                deboarding = true;
                prosimController.DeboardingStart();
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDeboardingService", $"Deboarding Service active");
                OnServiceOperationStatusChanged(ServiceOperationType.Deboarding, ServiceOperationStatus.InProgress);
                return;
            }
            else if (deboarding)
            {
                if (simConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS") != plannedPassengers)
                {
                    Logger.Log(LogLevel.Warning, "GSXServiceCoordinator:RunDeboardingService", $"Passenger changed during Boarding! Trying to reset Number ...");
                    simConnect.WriteLvar("FSDT_GSX_NUMPASSENGERS", plannedPassengers);
                }

                int paxCurrent = currentPassengers - deboardingPassengerCount;
                if (prosimController.Deboarding(paxCurrent, deboardingCargoPercent) || deboardState == 6 || deboardState == 1)
                {
                    deboarding = false;
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:RunDeboardingService", $"Deboarding finished (GSX State {deboardState})");
                    prosimController.DeboardingStop();
                    OnServiceOperationStatusChanged(ServiceOperationType.Deboarding, ServiceOperationStatus.Completed);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Sets the passenger count in GSX
        /// </summary>
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
                menuService.OperatorSelection();
                OnServiceOperationStatusChanged(ServiceOperationType.Jetway, ServiceOperationStatus.Started);

                // Only call stairs if JetwayOnly is false
                if (!model.JetwayOnly && simConnect.ReadLvar("FSDT_GSX_STAIRS") != 2 && simConnect.ReadLvar("FSDT_GSX_STAIRS") != 5 && simConnect.ReadLvar("FSDT_GSX_OPERATESTAIRS_STATE") < 3)
                {
                    Thread.Sleep(1500);
                    menuService.MenuOpen();
                    Logger.Log(LogLevel.Information, "GSXServiceCoordinator:CallJetwayStairs", $"Calling Stairs");
                    menuService.MenuItem(7);
                    OnServiceOperationStatusChanged(ServiceOperationType.Stairs, ServiceOperationStatus.Started);
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
                menuService.OperatorSelection();
                OnServiceOperationStatusChanged(ServiceOperationType.Stairs, ServiceOperationStatus.Started);
            }
            else if (model.JetwayOnly)
            {
                Logger.Log(LogLevel.Information, "GSXServiceCoordinator:CallJetwayStairs", $"Jetway Only mode - skipping stairs");
            }
        }
        
        /// <summary>
        /// Raises the ServiceOperationStatusChanged event
        /// </summary>
        protected virtual void OnServiceOperationStatusChanged(ServiceOperationType operationType, ServiceOperationStatus status, object data = null)
        {
            ServiceOperationStatusChanged?.Invoke(this, new ServiceOperationEventArgs(operationType, status, data));
        }
    }
}
```

### 4. Update GsxController.cs

Update the GsxController class to use the new service:

```csharp
// Add new field
private readonly IGSXServiceCoordinator serviceCoordinator;

// Update constructor
public GsxController(ServiceModel model, ProsimController prosimController, FlightPlan flightPlan, IAcarsService acarsService, IGSXMenuService menuService, IGSXAudioService audioService, IGSXStateManager stateManager, IGSXServiceCoordinator serviceCoordinator)
{
    Model = model;
    ProsimController = prosimController;
    FlightPlan = flightPlan;
    this.acarsService = acarsService;
    this.menuService = menuService;
    this.audioService = audioService;
    this.stateManager = stateManager;
    this.serviceCoordinator = serviceCoordinator;

    SimConnect = IPCManager.SimConnect;
    // Subscribe to SimConnect variables...
    
    // Initialize services
    stateManager.Initialize();
    serviceCoordinator.Initialize();
    
    // Subscribe to events
    stateManager.StateChanged += OnStateChanged;
    serviceCoordinator.ServiceOperationStatusChanged += OnServiceOperationStatusChanged;
    
    if (Model.TestArrival)
        ProsimController.Update(true);
}

// Add event handler for service operation status changes
private void OnServiceOperationStatusChanged(object sender, ServiceOperationEventArgs e)
{
    // Handle service operation status changes
    Logger.Log(LogLevel.Information, "GsxController:OnServiceOperationStatusChanged", $"Service operation {e.OperationType} status changed to {e.Status}");
}

// Replace RunLoadingServices method with call to service
private void RunLoadingServices(int refuelState, int cateringState)
{
    int boardingState = (int)SimConnect.ReadLvar("FSDT_GSX_BOARDING_STATE");
    int boardingCargoPercent = (int)SimConnect.ReadLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
    int boardingPassengerCount = (int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL");
    
    serviceCoordinator.RunLoadingServices(refuelState, cateringState, boardingState, boardingCargoPercent, boardingPassengerCount);
}

// Replace RunDEPARTUREServices method with call to service
private void RunDEPARTUREServices()
{
    int departureState = (int)SimConnect.ReadLvar("FSDT_GSX_DEPARTURE_STATE");
    int jetwayState = (int)SimConnect.ReadLvar("FSDT_GSX_JETWAY");
    int jetwayOperateState = (int)SimConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE");
    bool gsxPinInserted = SimConnect.ReadLvar("FSDT_GSX_BYPASS_PIN") != 0;
    bool isFrozen = SimConnect.ReadLvar("FSDT_VAR_Frozen") == 1;
    double groundSpeed = SimConnect.ReadSimVar("GPS GROUND SPEED", "Meters per second") * 0.00002966071308045356;
    double throttleLeftInput = SimConnect.ReadLvar("A_FC_THROTTLE_LEFT_INPUT");
    double throttleRightInput = SimConnect.ReadLvar("A_FC_THROTTLE_RIGHT_INPUT");
    
    serviceCoordinator.RunDepartureServices(departureState, jetwayState, jetwayOperateState, gsxPinInserted, isFrozen, groundSpeed, throttleLeftInput, throttleRightInput);
    
    // Check if departure services are complete and transition to TAXIOUT state
    if (serviceCoordinator.IsPushbackFinished)
    {
        stateManager.TransitionToTaxiout();
    }
}

// Replace RunArrivalServices method with call to service
private void RunArrivalServices(int deboardState)
{
    bool couatlStarted = SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") == 1;
    bool beaconOn = SimConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1;
    
    serviceCoordinator.RunArrivalServices(deboardState, couatlStarted, beaconOn);
    
    // Transition to ARRIVAL state
    stateManager.TransitionToArrival();
}

// Replace RunDeboardingService method with call to service
private void RunDeboardingService(int deboardState)
{
    int plannedPassengers = paxPlanned;
    int currentPassengers = (int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS");
    int deboardingPassengerCount = (int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
    int deboardingCargoPercent = (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");
    
    serviceCoordinator.RunDeboardingService(deboardState, plannedPassengers, currentPassengers, deboardingPassengerCount, deboardingCargoPercent);
    
    // Check if deboarding is complete and transition to TURNAROUND state
    if (!deboarding && (deboardState == 6 || deboardState == 1))
    {
        stateManager.TransitionToTurnaround();
    }
}

// Replace SetPassengers method with call to service
private void SetPassengers(int numPax)
{
    serviceCoordinator.SetPassengers(numPax);
}

// Replace CallJetwayStairs method with call to service
private void CallJetwayStairs()
{
    serviceCoordinator.CallJetwayStairs();
}

// Update RunServices method to use serviceCoordinator properties
public void RunServices()
{
    // ... existing code ...
    
    // Handle DEPARTURE state
    if (stateManager.IsDeparture())
    {
        // ... existing code ...
        
        // Boarding & Refueling
        int refuelState = (int)SimConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
        int cateringState = (int)SimConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
        if (!serviceCoordinator.IsRefuelingFinished || !serviceCoordinator.IsBoardingFinished)
        {
            RunLoadingServices(refuelState, cateringState);
            return;
        }

        // Loadsheet & Ground-Equipment
        if (serviceCoordinator.IsRefuelingFinished && serviceCoordinator.IsBoardingFinished)
        {
            RunDEPARTUREServices();
            return;
        }
    }
    
    // ... existing code ...
}

// Clean up resources
public void Dispose()
{
    // Unsubscribe from events
    if (stateManager != null)
    {
        stateManager.StateChanged -= OnStateChanged;
    }
    
    if (serviceCoordinator != null)
    {
        serviceCoordinator.ServiceOperationStatusChanged -= OnServiceOperationStatusChanged;
    }
    
    // ... other cleanup code ...
}
```

### 5. Update ServiceController.cs

Update the ServiceController class to initialize the new service:

```csharp
protected void InitializeServices()
{
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Initializing services...");
    
    // Step 1: Create FlightPlanService
    var flightPlanService = new FlightPlanService(Model);
    
    // Step 2: Create FlightPlan
    FlightPlan = new FlightPlan(Model, flightPlanService);
    
    // Step 3: Load flight plan
    if (!FlightPlan.Load())
    {
        Logger.Log(LogLevel.Warning, "ServiceController:InitializeServices", "Could not load flight plan, will retry in service loop");
    }
    
    // Step 4: Initialize FlightPlan in ProsimController
    ProsimController.InitializeFlightPlan(FlightPlan);
    
    // Step 5: Create AcarsService
    var acarsService = new AcarsService(Model.AcarsSecret, Model.AcarsNetworkUrl);
    
    // Step 6: Create GSX services
    var menuService = new GSXMenuService(Model, IPCManager.SimConnect);
    var audioService = new GSXAudioService(Model, IPCManager.SimConnect);
    var stateManager = new GSXStateManager();
    var serviceCoordinator = new GSXServiceCoordinator(Model, IPCManager.SimConnect, ProsimController, acarsService, menuService);
    
    // Step 7: Create GsxController
    var gsxController = new GsxController(Model, ProsimController, FlightPlan, acarsService, menuService, audioService, stateManager, serviceCoordinator);
    
    // Store the GsxController in IPCManager
    IPCManager.GsxController = gsxController;
    
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Services initialized successfully");
}
```

### 6. Add Unit Tests

Create unit tests for the new service in the Tests folder:

```csharp
[TestClass]
public class GSXServiceCoordinatorTests
{
    [TestMethod]
    public void Initialize_ResetsState()
    {
        // Arrange
        var model = new ServiceModel();
        var simConnectMock = new Mock<MobiSimConnect>();
        var prosimControllerMock = new Mock<ProsimController>(model);
        var acarsServiceMock = new Mock<IAcarsService>();
        var menuServiceMock = new Mock<IGSXMenuService>();
        
        var serviceCoordinator = new GSXServiceCoordinator(model, simConnectMock.Object, prosimControllerMock.Object, acarsServiceMock.Object, menuServiceMock.Object);
        
        // Act
        serviceCoordinator.Initialize();
        
        // Assert
        Assert.IsFalse(serviceCoordinator.IsBoardingFinished);
        Assert.IsFalse(serviceCoordinator.IsRefuelingFinished);
        Assert.IsFalse(serviceCoordinator.IsCateringFinished);
        Assert.IsFalse(serviceCoordinator.IsFinalLoadsheetSent);
        Assert.IsFalse(serviceCoordinator.IsEquipmentRemoved);
        Assert.IsFalse(serviceCoordinator.IsPushbackFinished);
    }
    
    [TestMethod]
    public void RunLoadingServices_AutoRefuel_CallsRefuelService()
    {
        // Arrange
        var model = new ServiceModel { AutoRefuel = true };
        var simConnectMock = new Mock<MobiSimConnect>();
        var prosimControllerMock = new Mock<ProsimController>(model);
        var acarsServiceMock = new Mock<IAcarsService>();
        var menuServiceMock = new Mock<IGSXMenuService>();
        
        simConnectMock.Setup(s => s.ReadLvar("FSDT_GSX_REFUELING_STATE")).Returns(0);
        
        var serviceCoordinator = new GSXServiceCoordinator(model, simConnectMock.Object, prosimControllerMock.Object, acarsServiceMock.Object, menuServiceMock.Object);
        
        bool eventRaised = false;
        ServiceOperationEventArgs eventArgs = null;
        serviceCoordinator.ServiceOperationStatusChanged += (sender, e) => 
        {
            eventRaised = true;
            eventArgs = e;
        };
        
        // Act
        serviceCoordinator.RunLoadingServices(0, 0, 0, 0, 0);
        
        // Assert
        menuServiceMock.Verify(m => m.MenuOpen(), Times.Once);
        menuServiceMock.Verify(m => m.MenuItem(3), Times.Once);
        Assert.IsTrue(eventRaised);
        Assert.AreEqual(ServiceOperationType.Refueling, eventArgs.OperationType);
        Assert.AreEqual(ServiceOperationStatus.Started, eventArgs.Status);
    }
    
    // Add more tests for other methods
}
```

### 7. Test the Implementation

Test the implementation to ensure it works correctly.

## Benefits

1. **Improved Separation of Concerns**
   - Service coordination is now handled by a dedicated service
   - Service operations are centralized and consistent
   - GsxController is simplified and more focused

2. **Enhanced Testability**
   - Service operations can be tested in isolation
   - Dependencies are explicit and can be mocked
   - Unit tests can be written for each service operation

3. **Better Maintainability**
   - Changes to service coordination can be made without affecting other parts of the system
   - Code is more organized and easier to understand
   - New service operations can be added without modifying GsxController

4. **Event-Based Communication**
   - Components can subscribe to service operation status changes
   - Reduces tight coupling between components
   - Makes the system more extensible

## Next Steps

After implementing Phase 3.3, we'll proceed with Phase 3.4 to extract door and equipment management functionality into dedicated GSXDoorManager and GSXEquipmentManager services.
