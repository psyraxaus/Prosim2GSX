# Phase 3.6: Refine GsxController Implementation

## Overview

This document outlines the implementation plan for Phase 3.6 of the Prosim2GSX modularization strategy. In this phase, we'll refine the GsxController to be a thin facade that delegates to the appropriate services.

## Implementation Steps

### 1. Analyze GsxController Dependencies

Before refactoring the GsxController, let's analyze its dependencies:

1. **ServiceModel** - Configuration model
2. **ProsimController** - Interface to ProSim
3. **FlightPlan** - Flight plan data
4. **IAcarsService** - ACARS communication
5. **IGSXMenuService** - GSX menu interaction
6. **IGSXAudioService** - Audio control
7. **IGSXStateManager** - State management
8. **IGSXServiceCoordinator** - Service coordination
9. **IGSXDoorManager** - Door management
10. **IGSXEquipmentManager** - Equipment management
11. **IGSXLoadsheetManager** - Loadsheet management
12. **MobiSimConnect** - SimConnect interface

### 2. Refactor GsxController.cs

Refactor the GsxController class to be a thin facade:

```csharp
using System;
using System.Threading;

namespace Prosim2GSX
{
    /// <summary>
    /// Controller for GSX integration
    /// </summary>
    public class GsxController : IDisposable
    {
        // Dependencies
        private readonly ServiceModel model;
        private readonly ProsimController prosimController;
        private readonly FlightPlan flightPlan;
        private readonly IAcarsService acarsService;
        private readonly IGSXMenuService menuService;
        private readonly IGSXAudioService audioService;
        private readonly IGSXStateManager stateManager;
        private readonly IGSXServiceCoordinator serviceCoordinator;
        private readonly IGSXDoorManager doorManager;
        private readonly IGSXEquipmentManager equipmentManager;
        private readonly IGSXLoadsheetManager loadsheetManager;
        private readonly MobiSimConnect simConnect;
        
        // State
        private bool isInitialized = false;
        private int interval = 1000;
        
        /// <summary>
        /// Gets the current interval for service execution
        /// </summary>
        public int Interval
        {
            get => interval;
            private set => interval = value;
        }
        
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        public FlightState CurrentFlightState => stateManager.CurrentState;
        
        /// <summary>
        /// Gets the service model
        /// </summary>
        public ServiceModel Model { get; }
        
        /// <summary>
        /// Gets the ProSim controller
        /// </summary>
        public ProsimController ProsimController { get; }
        
        /// <summary>
        /// Gets the flight plan
        /// </summary>
        public FlightPlan FlightPlan { get; }
        
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
            IGSXServiceCoordinator serviceCoordinator,
            IGSXDoorManager doorManager,
            IGSXEquipmentManager equipmentManager,
            IGSXLoadsheetManager loadsheetManager)
        {
            this.model = model;
            this.prosimController = prosimController;
            this.flightPlan = flightPlan;
            this.acarsService = acarsService;
            this.menuService = menuService;
            this.audioService = audioService;
            this.stateManager = stateManager;
            this.serviceCoordinator = serviceCoordinator;
            this.doorManager = doorManager;
            this.equipmentManager = equipmentManager;
            this.loadsheetManager = loadsheetManager;
            
            Model = model;
            ProsimController = prosimController;
            FlightPlan = flightPlan;
            
            simConnect = IPCManager.SimConnect;
            
            // Initialize services
            InitializeServices();
            
            // Subscribe to events
            SubscribeToEvents();
            
            if (model.TestArrival)
                prosimController.Update(true);
        }
        
        /// <summary>
        /// Initializes the services
        /// </summary>
        private void InitializeServices()
        {
            stateManager.Initialize();
            serviceCoordinator.Initialize();
            doorManager.Initialize();
            equipmentManager.Initialize();
            loadsheetManager.Initialize();
            
            Logger.Log(LogLevel.Information, "GsxController:InitializeServices", "Services initialized");
        }
        
        /// <summary>
        /// Subscribes to service events
        /// </summary>
        private void SubscribeToEvents()
        {
            stateManager.StateChanged += OnStateChanged;
            serviceCoordinator.ServiceOperationStatusChanged += OnServiceOperationStatusChanged;
            doorManager.DoorStateChanged += OnDoorStateChanged;
            equipmentManager.EquipmentStateChanged += OnEquipmentStateChanged;
            loadsheetManager.LoadsheetGenerated += OnLoadsheetGenerated;
            
            Logger.Log(LogLevel.Information, "GsxController:SubscribeToEvents", "Subscribed to service events");
        }
        
        /// <summary>
        /// Handles state changes
        /// </summary>
        private void OnStateChanged(object sender, FlightStateChangedEventArgs e)
        {
            // Update interval based on state
            switch (e.NewState)
            {
                case FlightState.FLIGHT:
                    Interval = 180000; // Longer interval during flight
                    break;
                case FlightState.TAXIIN:
                    Interval = 2500; // Shorter interval during taxi
                    break;
                default:
                    Interval = 1000; // Default interval
                    break;
            }
            
            Logger.Log(LogLevel.Information, "GsxController:OnStateChanged", $"State changed from {e.PreviousState} to {e.NewState}");
        }
        
        /// <summary>
        /// Handles service operation status changes
        /// </summary>
        private void OnServiceOperationStatusChanged(object sender, ServiceOperationEventArgs e)
        {
            Logger.Log(LogLevel.Information, "GsxController:OnServiceOperationStatusChanged", $"Service operation {e.OperationType} status changed to {e.Status}");
        }
        
        /// <summary>
        /// Handles door state changes
        /// </summary>
        private void OnDoorStateChanged(object sender, DoorStateChangedEventArgs e)
        {
            Logger.Log(LogLevel.Information, "GsxController:OnDoorStateChanged", $"Door {e.DoorType} {(e.IsOpen ? "opened" : "closed")}");
        }
        
        /// <summary>
        /// Handles equipment state changes
        /// </summary>
        private void OnEquipmentStateChanged(object sender, EquipmentStateChangedEventArgs e)
        {
            Logger.Log(LogLevel.Information, "GsxController:OnEquipmentStateChanged", $"Equipment {e.EquipmentType} {(e.IsConnected ? "connected" : "disconnected")}");
        }
        
        /// <summary>
        /// Handles loadsheet generation
        /// </summary>
        private void OnLoadsheetGenerated(object sender, LoadsheetGeneratedEventArgs e)
        {
            Logger.Log(LogLevel.Information, "GsxController:OnLoadsheetGenerated", $"{e.Type} loadsheet generated");
        }
        
        /// <summary>
        /// Runs GSX services
        /// </summary>
        public void RunServices()
        {
            // Check if SimConnect is available
            if (simConnect == null)
            {
                Logger.Log(LogLevel.Warning, "GsxController:RunServices", "SimConnect not available");
                return;
            }
            
            // Check if FlightPlan is available when needed
            if (flightPlan == null)
            {
                Logger.Log(LogLevel.Warning, "GsxController:RunServices", "FlightPlan not available");
                return;
            }
            
            // Mark as initialized on first successful run
            if (!isInitialized)
            {
                isInitialized = true;
                Logger.Log(LogLevel.Information, "GsxController:RunServices", "GSX Controller initialized and ready");
            }
            
            // Update ProSim controller
            prosimController.Update(false);
            
            // Get current state
            bool simOnGround = simConnect.ReadSimVar("SIM ON GROUND", "Bool") != 0.0f;
            bool batteryOn = prosimController.Interface.ReadDataRef("system.switches.S_OH_ELEC_BAT1") == 1;
            bool flightPlanLoaded = prosimController.IsFlightplanLoaded();
            
            // Handle door toggle requests
            bool service1Toggle = simConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE") == 1;
            bool service2Toggle = simConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE") == 1;
            doorManager.HandleDoorToggleRequests(service1Toggle, service2Toggle, model.SetOpenCateringDoor, model.SetOpenCargoDoors);
            
            // Update state
            stateManager.UpdateState(simOnGround, prosimController.enginesRunning, batteryOn, flightPlanLoaded, prosimController.flightPlanID, model.TestArrival);
            
            // Handle audio control
            audioService.ControlAudio();
            
            // Handle state-specific operations
            HandleStateOperations(simOnGround, batteryOn, flightPlanLoaded);
        }
        
        /// <summary>
        /// Handles state-specific operations
        /// </summary>
        private void HandleStateOperations(bool simOnGround, bool batteryOn, bool flightPlanLoaded)
        {
            // Handle PREFLIGHT state
            if (stateManager.IsPreflight() && simOnGround && !prosimController.enginesRunning && batteryOn)
            {
                HandlePreflightOperations();
                return;
            }
            
            // Handle DEPARTURE state
            if (stateManager.IsDeparture())
            {
                HandleDepartureOperations();
                return;
            }
            
            // Handle TAXIOUT -> FLIGHT transition
            if (stateManager.IsTaxiout() && !simOnGround)
            {
                stateManager.TransitionToFlight();
                return;
            }
            
            // Handle FLIGHT -> TAXIIN transition
            if (stateManager.IsFlight() && simOnGround)
            {
                stateManager.TransitionToTaxiin();
                return;
            }
            
            // Handle TAXIIN -> ARRIVAL transition
            int deboardState = (int)simConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");
            if (stateManager.IsTaxiin() && simConnect.ReadLvar("FSDT_VAR_EnginesStopped") == 1 && simConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && simConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 0)
            {
                HandleArrivalOperations(deboardState);
                return;
            }
            
            // Handle ARRIVAL - Deboarding
            if (stateManager.IsArrival() && deboardState >= 4)
            {
                HandleDeboardingOperations(deboardState);
            }
        }
        
        /// <summary>
        /// Handles preflight operations
        /// </summary>
        private void HandlePreflightOperations()
        {
            // Initialize ACARS
            if (model.UseAcars && !acarsService.IsInitialized)
            {
                try
                {
                    string opsCallsign = acarsService.FlightCallsignToOpsCallsign(prosimController.flightNumber);
                    acarsService.Initialize(prosimController.flightNumber);
                    Logger.Log(LogLevel.Information, "GsxController:HandlePreflightOperations", $"ACARS initialized with callsign {opsCallsign}");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "GsxController:HandlePreflightOperations", $"Unable to initialize ACARS - Error: {ex.Message}");
                }
            }
            
            // Check if Couatl engine is running
            if (simConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
            {
                Logger.Log(LogLevel.Information, "GsxController:HandlePreflightOperations", $"Couatl Engine not running");
                return;
            }
            
            // Handle plane repositioning
            if (model.RepositionPlane && !serviceCoordinator.IsPlaneRepositioned)
            {
                Logger.Log(LogLevel.Information, "GsxController:HandlePreflightOperations", $"Waiting {model.RepositionDelay}s before Repositioning ...");
                equipmentManager.SetChocks(true);
                Thread.Sleep((int)(model.RepositionDelay * 1000.0f));
                Logger.Log(LogLevel.Information, "GsxController:HandlePreflightOperations", $"Repositioning Plane");
                menuService.MenuOpen();
                Thread.Sleep(100);
                menuService.MenuItem(10);
                Thread.Sleep(250);
                menuService.MenuItem(1);
                serviceCoordinator.SetPlaneRepositioned(true);
                Thread.Sleep(1500);
                return;
            }
            
            // Handle jetway/stairs connection
            if (model.AutoConnect && !serviceCoordinator.IsJetwayStairsConnected)
            {
                int jetwayState = (int)simConnect.ReadLvar("FSDT_GSX_JETWAY");
                int jetwayOperateState = (int)simConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE");
                int stairsState = (int)simConnect.ReadLvar("FSDT_GSX_STAIRS");
                int stairsOperateState = (int)simConnect.ReadLvar("FSDT_GSX_OPERATESTAIRS_STATE");
                
                equipmentManager.CallJetwayStairs(menuService, jetwayState, jetwayOperateState, stairsState, stairsOperateState, model.JetwayOnly);
                serviceCoordinator.SetJetwayStairsConnected(true);
                return;
            }
            
            // Handle PCA connection
            if (model.ConnectPCA && !serviceCoordinator.IsPcaConnected && (!model.PcaOnlyJetways || (model.PcaOnlyJetways && simConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
            {
                Logger.Log(LogLevel.Information, "GsxController:HandlePreflightOperations", $"Connecting PCA");
                equipmentManager.SetPca(true);
                serviceCoordinator.SetPcaConnected(true);
                return;
            }
            
            // Handle first run
            if (!serviceCoordinator.IsFirstRunCompleted)
            {
                Logger.Log(LogLevel.Information, "GsxController:HandlePreflightOperations", $"Setting GPU and Chocks");
                equipmentManager.SetChocks(true);
                equipmentManager.SetGpu(true);
                Logger.Log(LogLevel.Information, "GsxController:HandlePreflightOperations", $"State: Preparation (Waiting for Flightplan import)");
                serviceCoordinator.SetFirstRunCompleted(true);
            }
            
            // Handle flight plan loading
            if (flightPlanLoaded)
            {
                stateManager.TransitionToDeparture(prosimController.flightPlanID);
                serviceCoordinator.SetPassengers(prosimController.GetPaxPlanned());
                
                if (!serviceCoordinator.IsPrelimFlightDataRecorded)
                {
                    double macZfw = prosimController.GetZfwCG();
                    Logger.Log(LogLevel.Information, "GsxController:HandlePreflightOperations", $"MACZFW: {macZfw} %");
                    serviceCoordinator.SetPrelimFlightDataRecorded(true);
                }
            }
        }
        
        /// <summary>
        /// Handles departure operations
        /// </summary>
        private void HandleDepartureOperations()
        {
            // Send preliminary loadsheet
            if (!loadsheetManager.IsPreliminaryLoadsheetSent)
            {
                var simTime = simConnect.ReadEnvVar("ZULU TIME", "Seconds");
                TimeSpan time = TimeSpan.FromSeconds(simTime);
                Logger.Log(LogLevel.Debug, "GsxController:HandleDepartureOperations", $"ZULU time - {simTime}");

                string flightNumber = prosimController.GetFMSFlightNumber();

                if (model.UseAcars && !string.IsNullOrEmpty(flightNumber))
                {
                    var prelimLoadedData = prosimController.GetLoadedData("prelim");
                    loadsheetManager.SendPreliminaryLoadsheet(flightNumber, prelimLoadedData);
                }
            }

            // Handle loading services
            int refuelState = (int)simConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
            int cateringState = (int)simConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
            int boardingState = (int)simConnect.ReadLvar("FSDT_GSX_BOARDING_STATE");
            int boardingCargoPercent = (int)simConnect.ReadLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
            int boardingPassengerCount = (int)simConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL");
            
            if (!serviceCoordinator.IsRefuelingFinished || !serviceCoordinator.IsBoardingFinished)
            {
                serviceCoordinator.RunLoadingServices(refuelState, cateringState, boardingState, boardingCargoPercent, boardingPassengerCount);
                return;
            }

            // Handle departure services
            if (serviceCoordinator.IsRefuelingFinished && serviceCoordinator.IsBoardingFinished)
            {
                int departureState = (int)simConnect.ReadLvar("FSDT_GSX_DEPARTURE_STATE");
                int jetwayState = (int)simConnect.ReadLvar("FSDT_GSX_JETWAY");
                int jetwayOperateState = (int)simConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE");
                bool gsxPinInserted = simConnect.ReadLvar("FSDT_GSX_BYPASS_PIN") != 0;
                bool isFrozen = simConnect.ReadLvar("FSDT_VAR_Frozen") == 1;
                double groundSpeed = simConnect.ReadSimVar("GPS GROUND SPEED", "Meters per second") * 0.00002966071308045356;
                double throttleLeftInput = simConnect.ReadLvar("A_FC_THROTTLE_LEFT_INPUT");
                double throttleRightInput = simConnect.ReadLvar("A_FC_THROTTLE_RIGHT_INPUT");
                
                serviceCoordinator.RunDepartureServices(departureState, jetwayState, jetwayOperateState, gsxPinInserted, isFrozen, groundSpeed, throttleLeftInput, throttleRightInput);
                
                // Check if departure services are complete and transition to TAXIOUT state
                if (serviceCoordinator.IsPushbackFinished)
                {
                    stateManager.TransitionToTaxiout();
                }
                
                return;
            }
        }
        
        /// <summary>
        /// Handles arrival operations
        /// </summary>
        private void HandleArrivalOperations(int deboardState)
        {
            bool couatlStarted = simConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") == 1;
            bool beaconOn = simConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1;
            
            serviceCoordinator.RunArrivalServices(deboardState, couatlStarted, beaconOn);
            
            // Transition to ARRIVAL state
            stateManager.TransitionToArrival();
        }
        
        /// <summary>
        /// Handles deboarding operations
        /// </summary>
        private void HandleDeboardingOperations(int deboardState)
        {
            int plannedPassengers = serviceCoordinator.PlannedPassengers;
            int currentPassengers = (int)simConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS");
            int deboardingPassengerCount = (int)simConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
            int deboardingCargoPercent = (int)simConnect.ReadLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");
            
            serviceCoordinator.RunDeboardingService(deboardState, plannedPassengers, currentPassengers, deboardingPassengerCount, deboardingCargoPercent);
            
            // Check if deboarding is complete and transition to TURNAROUND state
            if (!serviceCoordinator.IsDeboardingActive && (deboardState == 6 || deboardState == 1))
            {
                stateManager.TransitionToTurnaround();
            }
        }
        
        /// <summary>
        /// Resets audio settings to default
        /// </summary>
        public void ResetAudio()
        {
            audioService.ResetAudio();
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
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
            
            if (doorManager != null)
            {
                doorManager.DoorStateChanged -= OnDoorStateChanged;
            }
            
            if (equipmentManager != null)
            {
                equipmentManager.EquipmentStateChanged -= OnEquipmentStateChanged;
            }
            
            if (loadsheetManager != null)
            {
                loadsheetManager.LoadsheetGenerated -= OnLoadsheetGenerated;
            }
            
            Logger.Log(LogLevel.Information, "GsxController:Dispose", "GSX Controller disposed");
        }
    }
}
```

### 3. Update IGSXServiceCoordinator.cs

Update the IGSXServiceCoordinator interface to include additional properties and methods:

```csharp
/// <summary>
/// Interface for GSX service coordination
/// </summary>
public interface IGSXServiceCoordinator
{
    // ... existing members ...
    
    /// <summary>
    /// Gets whether the plane has been repositioned
    /// </summary>
    bool IsPlaneRepositioned { get; }
    
    /// <summary>
    /// Gets whether jetway/stairs have been connected
    /// </summary>
    bool IsJetwayStairsConnected { get; }
    
    /// <summary>
    /// Gets whether PCA has been connected
    /// </summary>
    bool IsPcaConnected { get; }
    
    /// <summary>
    /// Gets whether the first run has been completed
    /// </summary>
    bool IsFirstRunCompleted { get; }
    
    /// <summary>
    /// Gets whether preliminary flight data has been recorded
    /// </summary>
    bool IsPrelimFlightDataRecorded { get; }
    
    /// <summary>
    /// Gets whether deboarding is active
    /// </summary>
    bool IsDeboardingActive { get; }
    
    /// <summary>
    /// Gets the planned passenger count
    /// </summary>
    int PlannedPassengers { get; }
    
    /// <summary>
    /// Sets whether the plane has been repositioned
    /// </summary>
    void SetPlaneRepositioned(bool value);
    
    /// <summary>
    /// Sets whether jetway/stairs have been connected
    /// </summary>
    void SetJetwayStairsConnected(bool value);
    
    /// <summary>
    /// Sets whether PCA has been connected
    /// </summary>
    void SetPcaConnected(bool value);
    
    /// <summary>
    /// Sets whether the first run has been completed
    /// </summary>
    void SetFirstRunCompleted(bool value);
    
    /// <summary>
    /// Sets whether preliminary flight data has been recorded
    /// </summary>
    void SetPrelimFlightDataRecorded(bool value);
}
```

### 4. Update GSXServiceCoordinator.cs

Update the GSXServiceCoordinator class to implement the new properties and methods:

```csharp
/// <summary>
/// Service for GSX service coordination
/// </summary>
public class GSXServiceCoordinator : IGSXServiceCoordinator
{
    // ... existing members ...
    
    private bool planeRepositioned = false;
    private bool jetwayStairsConnected = false;
    private bool pcaConnected = false;
    private bool firstRunCompleted = false;
    private bool prelimFlightDataRecorded = false;
    
    /// <summary>
    /// Gets whether the plane has been repositioned
    /// </summary>
    public bool IsPlaneRepositioned => planeRepositioned;
    
    /// <summary>
    /// Gets whether jetway/stairs have been connected
    /// </summary>
    public bool IsJetwayStairsConnected => jetwayStairsConnected;
    
    /// <summary>
    /// Gets whether PCA has been connected
    /// </summary>
    public bool IsPcaConnected => pcaConnected;
    
    /// <summary>
    /// Gets whether the first run has been completed
    /// </summary>
    public bool IsFirstRunCompleted => firstRunCompleted;
    
    /// <summary>
    /// Gets whether preliminary flight data has been recorded
    /// </summary>
    public bool IsPrelimFlightDataRecorded => prelimFlightDataRecorded;
    
    /// <summary>
    /// Gets whether deboarding is active
    /// </summary>
    public bool IsDeboardingActive => deboarding;
    
    /// <summary>
    /// Gets the planned passenger count
    /// </summary>
    public int PlannedPassengers => paxPlanned;
    
    // ... existing methods ...
    
    /// <summary>
    /// Sets whether the plane has been repositioned
    /// </summary>
    public void SetPlaneRepositioned(bool value)
    {
        planeRepositioned = value;
    }
    
    /// <summary>
    /// Sets whether jetway/stairs have been connected
    /// </summary>
    public void SetJetwayStairsConnected(bool value)
    {
        jetwayStairsConnected = value;
    }
    
    /// <summary>
    /// Sets whether PCA has been connected
    /// </summary>
    public void SetPcaConnected(bool value)
    {
        pcaConnected = value;
    }
    
    /// <summary>
    /// Sets whether the first run has been completed
    /// </summary>
    public void SetFirstRunCompleted(bool value)
    {
        firstRunCompleted = value;
    }
    
    /// <summary>
    /// Sets whether preliminary flight data has been recorded
    /// </summary>
    public void SetPrelimFlightDataRecorded(bool value)
    {
        prelimFlightDataRecorded = value;
    }
    
    /// <summary>
    /// Resets the service coordinator state
    /// </summary>
    public override void Reset()
    {
        // ... existing reset code ...
        
        planeRepositioned = false;
        jetwayStairsConnected = false;
        pcaConnected = false;
        firstRunCompleted = false;
        prelimFlightDataRecorded = false;
        
        Logger.Log(LogLevel.Information, "GSXServiceCoordinator:Reset", "Service coordinator state reset");
    }
}
```

### 5. Update IAcarsService.cs

Update the IAcarsService interface to include an IsInitialized property:

```csharp
/// <summary>
/// Interface for ACARS service
/// </summary>
public interface IAcarsService
{
    // ... existing members ...
    
    /// <summary>
    /// Gets whether the ACARS service is initialized
    /// </summary>
    bool IsInitialized { get; }
}
```

### 6. Update AcarsService.cs

Update the AcarsService class to implement the IsInitialized property:

```csharp
/// <summary>
/// Service for ACARS communication
/// </summary>
public class AcarsService : IAcarsService
{
    // ... existing members ...
    
    private bool isInitialized = false;
    
    /// <summary>
    /// Gets whether the ACARS service is initialized
    /// </summary>
    public bool IsInitialized => isInitialized;
    
    // ... existing methods ...
    
    /// <summary>
    /// Initializes the ACARS service
    /// </summary>
    public void Initialize(string flightNumber)
    {
        // ... existing initialization code ...
        
        isInitialized = true;
        
        Logger.Log(LogLevel.Information, "AcarsService:Initialize", $"ACARS service initialized for flight {flightNumber}");
    }
}
```

### 7. Update ServiceController.cs

No changes needed to ServiceController.cs, as it already initializes all the required services and passes them to the GsxController.

### 8. Add Unit Tests

Create unit tests for the refactored GsxController in the Tests folder:

```csharp
[TestClass]
public class GsxControllerTests
{
    [TestMethod]
    public void Constructor_InitializesServices()
    {
        // Arrange
        var model = new ServiceModel();
        var prosimControllerMock = new Mock<ProsimController>(model);
        var flightPlanMock = new Mock<FlightPlan>(model, new FlightPlanService(model));
        var acarsServiceMock = new Mock<IAcarsService>();
        var menuServiceMock = new Mock<IGSXMenuService>();
        var audioServiceMock = new Mock<IGSXAudioService>();
        var stateManagerMock = new Mock<IGSXStateManager>();
        var serviceCoordinatorMock = new Mock<IGSXServiceCoordinator>();
        var doorManagerMock = new Mock<IGSXDoorManager>();
        var equipmentManagerMock = new Mock<IGSXEquipmentManager>();
        var loadsheetManagerMock = new Mock<IGSXLoadsheetManager>();
        
        // Act
        var gsxController = new GsxController(
            model,
            prosimControllerMock.Object,
            flightPlanMock.Object,
            acarsServiceMock.Object,
            menuServiceMock.Object,
            audioServiceMock.Object,
            stateManagerMock.Object,
            serviceCoordinatorMock.Object,
            doorManagerMock.Object,
            equipmentManagerMock.Object,
            loadsheetManagerMock.Object);
        
        // Assert
        Assert.IsNotNull(gsxController);
        stateManagerMock.Verify(s => s.Initialize(), Times.Once);
        serviceCoordinatorMock.Verify(s => s.Initialize(), Times.Once);
        doorManagerMock.Verify(d => d.Initialize(), Times.Once);
        equipmentManagerMock.Verify(e => e.Initialize(), Times.Once);
        loadsheetManagerMock.Verify(l => l.Initialize(), Times.Once);
    }
    
    [TestMethod]
    public void Constructor_SubscribesToEvents()
    {
        // Arrange
        var model = new ServiceModel();
        var prosimControllerMock = new Mock<ProsimController>(model);
        var flightPlanMock = new Mock<FlightPlan>(model, new FlightPlanService(model));
        var acarsServiceMock = new Mock<IAcarsService>();
        var menuServiceMock = new Mock<IGSXMenuService>();
        var audioServiceMock = new Mock<IGSXAudioService>();
        var stateManagerMock = new Mock<IGSXStateManager>();
        var serviceCoordinatorMock = new Mock<IGSXServiceCoordinator>();
        var doorManagerMock = new Mock<IGSXDoorManager>();
        var equipmentManagerMock = new Mock<IGSXEquipmentManager>();
        var loadsheetManagerMock = new Mock<IGSXLoadsheetManager>();
        
        // Act
        var gsxController = new GsxController(
            model,
            prosimControllerMock.Object,
            flightPlanMock.Object,
            acarsServiceMock.Object,
            menuServiceMock.Object,
            audioServiceMock.Object,
            stateManagerMock.Object,
            serviceCoordinatorMock.Object,
            doorManagerMock.Object,
            equipmentManagerMock.Object,
            loadsheetManagerMock.Object);
        
        // Assert
        Assert.IsNotNull(gsxController);
        
        // Verify that event handlers were attached
        // Note: We can't directly verify event subscriptions, but we can check if the controller responds to events
        
        // Trigger state changed event
        var stateChangedArgs = new FlightStateChangedEventArgs(FlightState.PREFLIGHT, FlightState.DEPARTURE);
        stateManagerMock.Raise(s => s.StateChanged += null, stateChangedArgs);
        
        // Trigger service operation status changed event
        var serviceOperationArgs = new ServiceOperationEventArgs(ServiceOperationType.Boarding, ServiceOperationStatus.Started);
        serviceCoordinatorMock.Raise(s => s.ServiceOperationStatusChanged += null, serviceOperationArgs);
        
        // Trigger door state changed event
        var doorStateArgs = new DoorStateChangedEventArgs(DoorType.ForwardRight, true);
        doorManagerMock.Raise(d => d.DoorStateChanged += null, doorStateArgs);
        
        // Trigger equipment state changed event
        var equipmentStateArgs = new EquipmentStateChangedEventArgs(EquipmentType.GPU, true);
        equipmentManagerMock.Raise(e => e.EquipmentStateChanged += null, equipmentStateArgs);
        
        // Trigger loadsheet generated event
        var loadsheetArgs = new LoadsheetGeneratedEventArgs(LoadsheetType.Preliminary, "Loadsheet content");
        loadsheetManagerMock.Raise(l => l.LoadsheetGenerated += null, loadsheetArgs);
    }
    
    [TestMethod]
    public void RunServices_UpdatesState()
    {
        // Arrange
        var model = new ServiceModel();
        var prosimControllerMock = new Mock<ProsimController>(model);
        var flightPlanMock = new Mock<FlightPlan>(model, new FlightPlanService(model));
        var acarsServiceMock = new Mock<IAcarsService>();
        var menuServiceMock = new Mock<IGSXMenuService>();
        var audioServiceMock = new Mock<IGSXAudioService>();
        var stateManagerMock = new Mock<IGSXStateManager>();
        var serviceCoordinatorMock = new Mock<IGSXServiceCoordinator>();
        var doorManagerMock = new Mock<IGSXDoorManager>();
        var equipmentManagerMock = new Mock<IGSXEquipmentManager>();
        var loadsheetManagerMock = new Mock<IGSXLoadsheetManager>();
        
        // Setup SimConnect mock
        var simConnectMock = new Mock<MobiSimConnect>();
        simConnectMock.Setup(s => s.ReadSimVar("SIM ON GROUND", "Bool")).Returns(1.0f);
        
        // Setup ProsimController mock
        prosimControllerMock.Setup(p => p.Interface.ReadDataRef("system.switches.S_OH_ELEC_BAT1")).Returns(1);
        prosimControllerMock.Setup(p => p.IsFlightplanLoaded()).Returns(true);
        
        // Setup state manager mock
        stateManagerMock.Setup(s => s.IsPreflight()).Returns(true);
        
        // Replace IPCManager.SimConnect with our mock
        var originalSimConnect = IPCManager.SimConnect;
        IPCManager.SimConnect = simConnectMock.Object;
        
        try
        {
            // Act
            var gsxController = new GsxController(
                model,
                prosimControllerMock.Object,
                flightPlanMock.Object,
                acarsServiceMock.Object,
                menuServiceMock.Object,
                audioServiceMock.Object,
                stateManagerMock.Object,
                serviceCoordinatorMock.Object,
                doorManagerMock.Object,
                equipmentManagerMock.Object,
                loadsheetManagerMock.Object);
            
            gsxController.RunServices();
            
            // Assert
            prosimControllerMock.Verify(p => p.Update(false), Times.Once);
            stateManagerMock.Verify(s => s.UpdateState(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            audioServiceMock.Verify(a => a.ControlAudio(), Times.Once);
        }
        finally
        {
            // Restore original SimConnect
            IPCManager.SimConnect = originalSimConnect;
        }
    }
    
    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var model = new ServiceModel();
        var prosimControllerMock = new Mock<ProsimController>(model);
        var flightPlanMock = new Mock<FlightPlan>(model, new FlightPlanService(model));
        var acarsServiceMock = new Mock<IAcarsService>();
        var menuServiceMock = new Mock<IGSXMenuService>();
        var audioServiceMock = new Mock<IGSXAudioService>();
        var stateManagerMock = new Mock<IGSXStateManager>();
        var serviceCoordinatorMock = new Mock<IGSXServiceCoordinator>();
        var doorManagerMock = new Mock<IGSXDoorManager>();
        var equipmentManagerMock = new Mock<IGSXEquipmentManager>();
        var loadsheetManagerMock = new Mock<IGSXLoadsheetManager>();
        
        // Act
        var gsxController = new GsxController(
            model,
            prosimControllerMock.Object,
            flightPlanMock.Object,
            acarsServiceMock.Object,
            menuServiceMock.Object,
            audioServiceMock.Object,
            stateManagerMock.Object,
            serviceCoordinatorMock.Object,
            doorManagerMock.Object,
            equipmentManagerMock.Object,
            loadsheetManagerMock.Object);
        
        // Dispose the controller
        gsxController.Dispose();
        
        // Assert
        // Note: We can't directly verify event unsubscriptions, but we can check if the controller responds to events after disposal
        
        // Trigger state changed event
        var stateChangedArgs = new FlightStateChangedEventArgs(FlightState.PREFLIGHT, FlightState.DEPARTURE);
        stateManagerMock.Raise(s => s.StateChanged += null, stateChangedArgs);
        
        // Trigger service operation status changed event
        var serviceOperationArgs = new ServiceOperationEventArgs(ServiceOperationType.Boarding, ServiceOperationStatus.Started);
        serviceCoordinatorMock.Raise(s => s.ServiceOperationStatusChanged += null, serviceOperationArgs);
        
        // Trigger door state changed event
        var doorStateArgs = new DoorStateChangedEventArgs(DoorType.ForwardRight, true);
        doorManagerMock.Raise(d => d.DoorStateChanged += null, doorStateArgs);
        
        // Trigger equipment state changed event
        var equipmentStateArgs = new EquipmentStateChangedEventArgs(EquipmentType.GPU, true);
        equipmentManagerMock.Raise(e => e.EquipmentStateChanged += null, equipmentStateArgs);
        
        // Trigger loadsheet generated event
        var loadsheetArgs = new LoadsheetGeneratedEventArgs(LoadsheetType.Preliminary, "Loadsheet content");
        loadsheetManagerMock.Raise(l => l.LoadsheetGenerated += null, loadsheetArgs);
    }
}
```

### 9. Test the Implementation

Test the implementation to ensure it works correctly.

## Benefits

1. **Improved Separation of Concerns**
   - GsxController is now a thin facade that delegates to specialized services
   - Each service has a single responsibility
   - Code is more organized and easier to understand

2. **Enhanced Testability**
   - GsxController can be tested in isolation
   - Dependencies are explicit and can be mocked
   - Unit tests can be written for each component

3. **Better Maintainability**
   - Changes to one service don't affect other services
   - New features can be added without modifying existing code
   - Code is more modular and easier to maintain

4. **Event-Based Communication**
   - Components communicate through events
   - Reduces tight coupling between components
   - Makes the system more extensible

5. **Clearer Responsibility Boundaries**
   - Each service has a clear responsibility
   - GsxController orchestrates the services
   - Services don't need to know about each other

## Next Steps

After implementing Phase 3.6, we'll proceed with Phase 4 to implement comprehensive unit tests for all components of the system.
