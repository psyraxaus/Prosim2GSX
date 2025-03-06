# Phase 3.4: GSXDoorManager and GSXEquipmentManager Implementation

## Overview

This document outlines the implementation plan for Phase 3.4 of the Prosim2GSX modularization strategy. In this phase, we'll extract door and equipment management functionality from the GsxController into dedicated services.

## Implementation Steps

### 1. Create DoorStateChangedEventArgs.cs

Create a new event args class in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for door state changes
    /// </summary>
    public class DoorStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the door type
        /// </summary>
        public DoorType DoorType { get; }
        
        /// <summary>
        /// Gets whether the door is open
        /// </summary>
        public bool IsOpen { get; }
        
        /// <summary>
        /// Gets the timestamp of the state change
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the DoorStateChangedEventArgs class
        /// </summary>
        /// <param name="doorType">The door type</param>
        /// <param name="isOpen">Whether the door is open</param>
        public DoorStateChangedEventArgs(DoorType doorType, bool isOpen)
        {
            DoorType = doorType;
            IsOpen = isOpen;
            Timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Door types
    /// </summary>
    public enum DoorType
    {
        ForwardRight,
        AftRight,
        ForwardCargo,
        AftCargo
    }
}
```

### 2. Create EquipmentStateChangedEventArgs.cs

Create a new event args class in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for equipment state changes
    /// </summary>
    public class EquipmentStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the equipment type
        /// </summary>
        public EquipmentType EquipmentType { get; }
        
        /// <summary>
        /// Gets whether the equipment is connected
        /// </summary>
        public bool IsConnected { get; }
        
        /// <summary>
        /// Gets the timestamp of the state change
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the EquipmentStateChangedEventArgs class
        /// </summary>
        /// <param name="equipmentType">The equipment type</param>
        /// <param name="isConnected">Whether the equipment is connected</param>
        public EquipmentStateChangedEventArgs(EquipmentType equipmentType, bool isConnected)
        {
            EquipmentType = equipmentType;
            IsConnected = isConnected;
            Timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Equipment types
    /// </summary>
    public enum EquipmentType
    {
        Jetway,
        Stairs,
        GPU,
        PCA,
        Chocks
    }
}
```

### 3. Create IGSXDoorManager.cs

Create a new interface file in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX door management
    /// </summary>
    public interface IGSXDoorManager
    {
        /// <summary>
        /// Event raised when a door state changes
        /// </summary>
        event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
        
        /// <summary>
        /// Gets whether the forward right door is open
        /// </summary>
        bool IsForwardRightDoorOpen { get; }
        
        /// <summary>
        /// Gets whether the aft right door is open
        /// </summary>
        bool IsAftRightDoorOpen { get; }
        
        /// <summary>
        /// Gets whether the forward cargo door is open
        /// </summary>
        bool IsForwardCargoDoorOpen { get; }
        
        /// <summary>
        /// Gets whether the aft cargo door is open
        /// </summary>
        bool IsAftCargoDoorOpen { get; }
        
        /// <summary>
        /// Initializes the door manager
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Sets the forward right door state
        /// </summary>
        /// <param name="isOpen">Whether the door should be open</param>
        void SetForwardRightDoor(bool isOpen);
        
        /// <summary>
        /// Sets the aft right door state
        /// </summary>
        /// <param name="isOpen">Whether the door should be open</param>
        void SetAftRightDoor(bool isOpen);
        
        /// <summary>
        /// Sets the forward cargo door state
        /// </summary>
        /// <param name="isOpen">Whether the door should be open</param>
        void SetForwardCargoDoor(bool isOpen);
        
        /// <summary>
        /// Sets the aft cargo door state
        /// </summary>
        /// <param name="isOpen">Whether the door should be open</param>
        void SetAftCargoDoor(bool isOpen);
        
        /// <summary>
        /// Handles door toggle requests from GSX
        /// </summary>
        /// <param name="service1Toggle">Whether service 1 toggle is active</param>
        /// <param name="service2Toggle">Whether service 2 toggle is active</param>
        /// <param name="openCateringDoor">Whether catering doors should be opened</param>
        /// <param name="openCargoDoors">Whether cargo doors should be opened</param>
        void HandleDoorToggleRequests(bool service1Toggle, bool service2Toggle, bool openCateringDoor, bool openCargoDoors);
        
        /// <summary>
        /// Closes all doors
        /// </summary>
        void CloseAllDoors();
    }
}
```

### 4. Create GSXDoorManager.cs

Create a new implementation file in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX door management
    /// </summary>
    public class GSXDoorManager : IGSXDoorManager
    {
        private readonly ProsimController prosimController;
        
        private bool forwardRightDoorOpen = false;
        private bool aftRightDoorOpen = false;
        private bool forwardCargoDoorOpen = false;
        private bool aftCargoDoorOpen = false;
        
        /// <summary>
        /// Event raised when a door state changes
        /// </summary>
        public event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
        
        /// <summary>
        /// Gets whether the forward right door is open
        /// </summary>
        public bool IsForwardRightDoorOpen => forwardRightDoorOpen;
        
        /// <summary>
        /// Gets whether the aft right door is open
        /// </summary>
        public bool IsAftRightDoorOpen => aftRightDoorOpen;
        
        /// <summary>
        /// Gets whether the forward cargo door is open
        /// </summary>
        public bool IsForwardCargoDoorOpen => forwardCargoDoorOpen;
        
        /// <summary>
        /// Gets whether the aft cargo door is open
        /// </summary>
        public bool IsAftCargoDoorOpen => aftCargoDoorOpen;
        
        /// <summary>
        /// Initializes a new instance of the GSXDoorManager class
        /// </summary>
        public GSXDoorManager(ProsimController prosimController)
        {
            this.prosimController = prosimController;
        }
        
        /// <summary>
        /// Initializes the door manager
        /// </summary>
        public void Initialize()
        {
            forwardRightDoorOpen = false;
            aftRightDoorOpen = false;
            forwardCargoDoorOpen = false;
            aftCargoDoorOpen = false;
            
            Logger.Log(LogLevel.Information, "GSXDoorManager:Initialize", "Door manager initialized");
        }
        
        /// <summary>
        /// Sets the forward right door state
        /// </summary>
        public void SetForwardRightDoor(bool isOpen)
        {
            if (forwardRightDoorOpen != isOpen)
            {
                prosimController.SetForwardRightDoor(isOpen);
                forwardRightDoorOpen = isOpen;
                Logger.Log(LogLevel.Information, "GSXDoorManager:SetForwardRightDoor", $"{(isOpen ? "Opened" : "Closed")} forward right door");
                OnDoorStateChanged(DoorType.ForwardRight, isOpen);
            }
        }
        
        /// <summary>
        /// Sets the aft right door state
        /// </summary>
        public void SetAftRightDoor(bool isOpen)
        {
            if (aftRightDoorOpen != isOpen)
            {
                prosimController.SetAftRightDoor(isOpen);
                aftRightDoorOpen = isOpen;
                Logger.Log(LogLevel.Information, "GSXDoorManager:SetAftRightDoor", $"{(isOpen ? "Opened" : "Closed")} aft right door");
                OnDoorStateChanged(DoorType.AftRight, isOpen);
            }
        }
        
        /// <summary>
        /// Sets the forward cargo door state
        /// </summary>
        public void SetForwardCargoDoor(bool isOpen)
        {
            if (forwardCargoDoorOpen != isOpen)
            {
                prosimController.SetForwardCargoDoor(isOpen);
                forwardCargoDoorOpen = isOpen;
                Logger.Log(LogLevel.Information, "GSXDoorManager:SetForwardCargoDoor", $"{(isOpen ? "Opened" : "Closed")} forward cargo door");
                OnDoorStateChanged(DoorType.ForwardCargo, isOpen);
            }
        }
        
        /// <summary>
        /// Sets the aft cargo door state
        /// </summary>
        public void SetAftCargoDoor(bool isOpen)
        {
            if (aftCargoDoorOpen != isOpen)
            {
                prosimController.SetAftCargoDoor(isOpen);
                aftCargoDoorOpen = isOpen;
                Logger.Log(LogLevel.Information, "GSXDoorManager:SetAftCargoDoor", $"{(isOpen ? "Opened" : "Closed")} aft cargo door");
                OnDoorStateChanged(DoorType.AftCargo, isOpen);
            }
        }
        
        /// <summary>
        /// Handles door toggle requests from GSX
        /// </summary>
        public void HandleDoorToggleRequests(bool service1Toggle, bool service2Toggle, bool openCateringDoor, bool openCargoDoors)
        {
            if (openCateringDoor)
            {
                // Check if catering service is waiting for forward door to be opened
                if (service1Toggle && !forwardRightDoorOpen)
                {
                    SetForwardRightDoor(true);
                }
                
                // Check if catering service is waiting for forward door to be closed
                if (service1Toggle && forwardRightDoorOpen)
                {
                    SetForwardRightDoor(false);
                }

                // Check if catering service is waiting for aft door to be opened
                if (service2Toggle && !aftRightDoorOpen)
                {
                    SetAftRightDoor(true);
                }

                // Check if catering service is waiting for aft door to be closed
                if (service2Toggle && aftRightDoorOpen)
                {
                    SetAftRightDoor(false);
                }
            }
            
            // Open cargo doors if enabled and catering is finished
            if (openCargoDoors && !forwardCargoDoorOpen && !aftCargoDoorOpen)
            {
                SetForwardCargoDoor(true);
                SetAftCargoDoor(true);
            }
        }
        
        /// <summary>
        /// Closes all doors
        /// </summary>
        public void CloseAllDoors()
        {
            SetForwardRightDoor(false);
            SetAftRightDoor(false);
            SetForwardCargoDoor(false);
            SetAftCargoDoor(false);
            
            Logger.Log(LogLevel.Information, "GSXDoorManager:CloseAllDoors", "All doors closed");
        }
        
        /// <summary>
        /// Raises the DoorStateChanged event
        /// </summary>
        protected virtual void OnDoorStateChanged(DoorType doorType, bool isOpen)
        {
            DoorStateChanged?.Invoke(this, new DoorStateChangedEventArgs(doorType, isOpen));
        }
    }
}
```

### 5. Create IGSXEquipmentManager.cs

Create a new interface file in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX equipment management
    /// </summary>
    public interface IGSXEquipmentManager
    {
        /// <summary>
        /// Event raised when equipment state changes
        /// </summary>
        event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
        
        /// <summary>
        /// Gets whether the jetway is connected
        /// </summary>
        bool IsJetwayConnected { get; }
        
        /// <summary>
        /// Gets whether stairs are connected
        /// </summary>
        bool IsStairsConnected { get; }
        
        /// <summary>
        /// Gets whether the GPU is connected
        /// </summary>
        bool IsGpuConnected { get; }
        
        /// <summary>
        /// Gets whether the PCA is connected
        /// </summary>
        bool IsPcaConnected { get; }
        
        /// <summary>
        /// Gets whether chocks are set
        /// </summary>
        bool AreChocksSet { get; }
        
        /// <summary>
        /// Initializes the equipment manager
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Sets the GPU state
        /// </summary>
        /// <param name="isConnected">Whether the GPU should be connected</param>
        void SetGpu(bool isConnected);
        
        /// <summary>
        /// Sets the PCA state
        /// </summary>
        /// <param name="isConnected">Whether the PCA should be connected</param>
        void SetPca(bool isConnected);
        
        /// <summary>
        /// Sets the chocks state
        /// </summary>
        /// <param name="isSet">Whether the chocks should be set</param>
        void SetChocks(bool isSet);
        
        /// <summary>
        /// Calls jetway and/or stairs
        /// </summary>
        /// <param name="menuService">The GSX menu service</param>
        /// <param name="jetwayState">The current jetway state</param>
        /// <param name="jetwayOperateState">The current jetway operate state</param>
        /// <param name="stairsState">The current stairs state</param>
        /// <param name="stairsOperateState">The current stairs operate state</param>
        /// <param name="jetwayOnly">Whether only jetway should be called</param>
        void CallJetwayStairs(IGSXMenuService menuService, int jetwayState, int jetwayOperateState, int stairsState, int stairsOperateState, bool jetwayOnly);
        
        /// <summary>
        /// Removes all ground equipment
        /// </summary>
        void RemoveAllEquipment();
    }
}
```

### 6. Create GSXEquipmentManager.cs

Create a new implementation file in the Services folder:

```csharp
using System;
using System.Threading;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX equipment management
    /// </summary>
    public class GSXEquipmentManager : IGSXEquipmentManager
    {
        private readonly ProsimController prosimController;
        private readonly MobiSimConnect simConnect;
        
        private bool jetwayConnected = false;
        private bool stairsConnected = false;
        private bool gpuConnected = false;
        private bool pcaConnected = false;
        private bool chocksSet = false;
        
        /// <summary>
        /// Event raised when equipment state changes
        /// </summary>
        public event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
        
        /// <summary>
        /// Gets whether the jetway is connected
        /// </summary>
        public bool IsJetwayConnected => jetwayConnected;
        
        /// <summary>
        /// Gets whether stairs are connected
        /// </summary>
        public bool IsStairsConnected => stairsConnected;
        
        /// <summary>
        /// Gets whether the GPU is connected
        /// </summary>
        public bool IsGpuConnected => gpuConnected;
        
        /// <summary>
        /// Gets whether the PCA is connected
        /// </summary>
        public bool IsPcaConnected => pcaConnected;
        
        /// <summary>
        /// Gets whether chocks are set
        /// </summary>
        public bool AreChocksSet => chocksSet;
        
        /// <summary>
        /// Initializes a new instance of the GSXEquipmentManager class
        /// </summary>
        public GSXEquipmentManager(ProsimController prosimController, MobiSimConnect simConnect)
        {
            this.prosimController = prosimController;
            this.simConnect = simConnect;
        }
        
        /// <summary>
        /// Initializes the equipment manager
        /// </summary>
        public void Initialize()
        {
            jetwayConnected = false;
            stairsConnected = false;
            gpuConnected = false;
            pcaConnected = false;
            chocksSet = false;
            
            Logger.Log(LogLevel.Information, "GSXEquipmentManager:Initialize", "Equipment manager initialized");
        }
        
        /// <summary>
        /// Sets the GPU state
        /// </summary>
        public void SetGpu(bool isConnected)
        {
            if (gpuConnected != isConnected)
            {
                prosimController.SetServiceGPU(isConnected);
                gpuConnected = isConnected;
                Logger.Log(LogLevel.Information, "GSXEquipmentManager:SetGpu", $"{(isConnected ? "Connected" : "Disconnected")} GPU");
                OnEquipmentStateChanged(EquipmentType.GPU, isConnected);
            }
        }
        
        /// <summary>
        /// Sets the PCA state
        /// </summary>
        public void SetPca(bool isConnected)
        {
            if (pcaConnected != isConnected)
            {
                prosimController.SetServicePCA(isConnected);
                pcaConnected = isConnected;
                Logger.Log(LogLevel.Information, "GSXEquipmentManager:SetPca", $"{(isConnected ? "Connected" : "Disconnected")} PCA");
                OnEquipmentStateChanged(EquipmentType.PCA, isConnected);
            }
        }
        
        /// <summary>
        /// Sets the chocks state
        /// </summary>
        public void SetChocks(bool isSet)
        {
            if (chocksSet != isSet)
            {
                prosimController.SetServiceChocks(isSet);
                chocksSet = isSet;
                Logger.Log(LogLevel.Information, "GSXEquipmentManager:SetChocks", $"{(isSet ? "Set" : "Removed")} chocks");
                OnEquipmentStateChanged(EquipmentType.Chocks, isSet);
            }
        }
        
        /// <summary>
        /// Calls jetway and/or stairs
        /// </summary>
        public void CallJetwayStairs(IGSXMenuService menuService, int jetwayState, int jetwayOperateState, int stairsState, int stairsOperateState, bool jetwayOnly)
        {
            menuService.MenuOpen();

            if (jetwayState != 2 && jetwayState != 5 && jetwayOperateState < 3)
            {
                Logger.Log(LogLevel.Information, "GSXEquipmentManager:CallJetwayStairs", $"Calling Jetway");
                menuService.MenuItem(6);
                menuService.OperatorSelection();
                jetwayConnected = true;
                OnEquipmentStateChanged(EquipmentType.Jetway, true);

                // Only call stairs if JetwayOnly is false
                if (!jetwayOnly && stairsState != 2 && stairsState != 5 && stairsOperateState < 3)
                {
                    Thread.Sleep(1500);
                    menuService.MenuOpen();
                    Logger.Log(LogLevel.Information, "GSXEquipmentManager:CallJetwayStairs", $"Calling Stairs");
                    menuService.MenuItem(7);
                    stairsConnected = true;
                    OnEquipmentStateChanged(EquipmentType.Stairs, true);
                }
                else if (jetwayOnly)
                {
                    Logger.Log(LogLevel.Information, "GSXEquipmentManager:CallJetwayStairs", $"Jetway Only mode - skipping stairs");
                }
            }
            else if (!jetwayOnly && stairsState != 5 && stairsOperateState < 3)
            {
                Logger.Log(LogLevel.Information, "GSXEquipmentManager:CallJetwayStairs", $"Calling Stairs");
                menuService.MenuItem(7);
                menuService.OperatorSelection();
                stairsConnected = true;
                OnEquipmentStateChanged(EquipmentType.Stairs, true);
            }
            else if (jetwayOnly)
            {
                Logger.Log(LogLevel.Information, "GSXEquipmentManager:CallJetwayStairs", $"Jetway Only mode - skipping stairs");
            }
        }
        
        /// <summary>
        /// Removes all ground equipment
        /// </summary>
        public void RemoveAllEquipment()
        {
            SetGpu(false);
            SetPca(false);
            SetChocks(false);
            
            // Remove jetway if connected
            if (jetwayConnected)
            {
                // Check if jetway is connected and can be removed
                if (simConnect.ReadLvar("FSDT_GSX_JETWAY") != 2 && simConnect.ReadLvar("FSDT_GSX_JETWAY") == 5 && simConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE") < 3)
                {
                    Logger.Log(LogLevel.Information, "GSXEquipmentManager:RemoveAllEquipment", $"Removing Jetway");
                    // Note: This requires a menu service, which should be passed as a parameter or injected
                    // For now, we'll just log that the jetway should be removed
                    jetwayConnected = false;
                    OnEquipmentStateChanged(EquipmentType.Jetway, false);
                }
            }
            
            Logger.Log(LogLevel.Information, "GSXEquipmentManager:RemoveAllEquipment", "All ground equipment removed");
        }
        
        /// <summary>
        /// Raises the EquipmentStateChanged event
        /// </summary>
        protected virtual void OnEquipmentStateChanged(EquipmentType equipmentType, bool isConnected)
        {
            EquipmentStateChanged?.Invoke(this, new EquipmentStateChangedEventArgs(equipmentType, isConnected));
        }
    }
}
```

### 7. Update GsxController.cs

Update the GsxController class to use the new services:

```csharp
// Add new fields
private readonly IGSXDoorManager doorManager;
private readonly IGSXEquipmentManager equipmentManager;

// Update constructor
public GsxController(ServiceModel model, ProsimController prosimController, FlightPlan flightPlan, IAcarsService acarsService, IGSXMenuService menuService, IGSXAudioService audioService, IGSXStateManager stateManager, IGSXServiceCoordinator serviceCoordinator, IGSXDoorManager doorManager, IGSXEquipmentManager equipmentManager)
{
    Model = model;
    ProsimController = prosimController;
    FlightPlan = flightPlan;
    this.acarsService = acarsService;
    this.menuService = menuService;
    this.audioService = audioService;
    this.stateManager = stateManager;
    this.serviceCoordinator = serviceCoordinator;
    this.doorManager = doorManager;
    this.equipmentManager = equipmentManager;

    SimConnect = IPCManager.SimConnect;
    // Subscribe to SimConnect variables...
    
    // Initialize services
    stateManager.Initialize();
    serviceCoordinator.Initialize();
    doorManager.Initialize();
    equipmentManager.Initialize();
    
    // Subscribe to events
    stateManager.StateChanged += OnStateChanged;
    serviceCoordinator.ServiceOperationStatusChanged += OnServiceOperationStatusChanged;
    doorManager.DoorStateChanged += OnDoorStateChanged;
    equipmentManager.EquipmentStateChanged += OnEquipmentStateChanged;
    
    if (Model.TestArrival)
        ProsimController.Update(true);
}

// Add event handlers for door and equipment state changes
private void OnDoorStateChanged(object sender, DoorStateChangedEventArgs e)
{
    // Handle door state changes
    Logger.Log(LogLevel.Information, "GsxController:OnDoorStateChanged", $"Door {e.DoorType} {(e.IsOpen ? "opened" : "closed")}");
}

private void OnEquipmentStateChanged(object sender, EquipmentStateChangedEventArgs e)
{
    // Handle equipment state changes
    Logger.Log(LogLevel.Information, "GsxController:OnEquipmentStateChanged", $"Equipment {e.EquipmentType} {(e.IsConnected ? "connected" : "disconnected")}");
}

// Update RunServices method to use doorManager and equipmentManager
public void RunServices()
{
    // ... existing code ...
    
    // Handle door toggle requests
    bool service1Toggle = SimConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE") == 1;
    bool service2Toggle = SimConnect.ReadLvar("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE") == 1;
    doorManager.HandleDoorToggleRequests(service1Toggle, service2Toggle, Model.SetOpenCateringDoor, Model.SetOpenCargoDoors);
    
    // Handle PREFLIGHT state
    if (stateManager.IsPreflight() && simOnGround && !ProsimController.enginesRunning && batteryOn)
    {
        // ... existing code ...
        
        if (Model.RepositionPlane && !planePositioned)
        {
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Waiting {Model.RepositionDelay}s before Repositioning ...");
            equipmentManager.SetChocks(true);
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
            int jetwayState = (int)SimConnect.ReadLvar("FSDT_GSX_JETWAY");
            int jetwayOperateState = (int)SimConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE");
            int stairsState = (int)SimConnect.ReadLvar("FSDT_GSX_STAIRS");
            int stairsOperateState = (int)SimConnect.ReadLvar("FSDT_GSX_OPERATESTAIRS_STATE");
            
            equipmentManager.CallJetwayStairs(menuService, jetwayState, jetwayOperateState, stairsState, stairsOperateState, Model.JetwayOnly);
            connectCalled = true;
            return;
        }

        if (Model.ConnectPCA && !pcaCalled && (!Model.PcaOnlyJetways || (Model.PcaOnlyJetways && SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
        {
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Connecting PCA");
            equipmentManager.SetPca(true);
            pcaCalled = true;
            return;
        }

        if (firstRun)
        {
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Setting GPU and Chocks");
            equipmentManager.SetChocks(true);
            equipmentManager.SetGpu(true);
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State: Preparation (Waiting for Flightplan import)");
            firstRun = false;
        }
        
        // ... existing code ...
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
    
    if (doorManager != null)
    {
        doorManager.DoorStateChanged -= OnDoorStateChanged;
    }
    
    if (equipmentManager != null)
    {
        equipmentManager.EquipmentStateChanged -= OnEquipmentStateChanged;
    }
    
    // ... other cleanup code ...
}
```

### 8. Update ServiceController.cs

Update the ServiceController class to initialize the new services:

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
    var doorManager = new GSXDoorManager(ProsimController);
    var equipmentManager = new GSXEquipmentManager(ProsimController, IPCManager.SimConnect);
    
    // Step 7: Create GsxController
    var gsxController = new GsxController(Model, ProsimController, FlightPlan, acarsService, menuService, audioService, stateManager, serviceCoordinator, doorManager, equipmentManager);
    
    // Store the GsxController in IPCManager
    IPCManager.GsxController = gsxController;
    
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Services initialized successfully");
}
```

### 9. Add Unit Tests

Create unit tests for the new services in the Tests folder:

```csharp
[TestClass]
public class GSXDoorManagerTests
{
    [TestMethod]
    public void Initialize_ResetsDoorStates()
    {
        // Arrange
        var prosimControllerMock = new Mock<ProsimController>(new ServiceModel());
        var doorManager = new GSXDoorManager(prosimControllerMock.Object);
        
        // Act
        doorManager.Initialize();
        
        // Assert
        Assert.IsFalse(doorManager.IsForwardRightDoorOpen);
        Assert.IsFalse(doorManager.IsAftRightDoorOpen);
        Assert.IsFalse(doorManager.IsForwardCargoDoorOpen);
        Assert.IsFalse(doorManager.IsAftCargoDoorOpen);
    }
    
    [TestMethod]
    public void SetForwardRightDoor_RaisesEvent()
    {
        // Arrange
        var prosimControllerMock = new Mock<ProsimController>(new ServiceModel());
        var doorManager = new GSXDoorManager(prosimControllerMock.Object);
        
        bool eventRaised = false;
        DoorStateChangedEventArgs eventArgs = null;
        doorManager.DoorStateChanged += (sender, e) => 
        {
            eventRaised = true;
            eventArgs = e;
        };
        
        // Act
        doorManager.SetForwardRightDoor(true);
        
        // Assert
        Assert.IsTrue(eventRaised);
        Assert.AreEqual(DoorType.ForwardRight, eventArgs.DoorType);
        Assert.IsTrue(eventArgs.IsOpen);
        Assert.IsTrue(doorManager.IsForwardRightDoorOpen);
        prosimControllerMock.Verify(p => p.SetForwardRightDoor(true), Times.Once);
    }
    
    [TestMethod]
    public void HandleDoorToggleRequests_OpensCateringDoors()
    {
        // Arrange
        var prosimControllerMock = new Mock<ProsimController>(new ServiceModel());
        var doorManager = new GSXDoorManager(prosimControllerMock.Object);
        
        // Act
        doorManager.HandleDoorToggleRequests(true, false, true, false);
        
        // Assert
        Assert.IsTrue(doorManager.IsForwardRightDoorOpen);
        Assert.IsFalse(doorManager.IsAftRightDoorOpen);
        prosimControllerMock.Verify(p => p.SetForwardRightDoor(true), Times.Once);
        prosimControllerMock.Verify(p => p.SetAftRightDoor(It.IsAny<bool>()), Times.Never);
    }
    
    [TestMethod]
    public void CloseAllDoors_ClosesAllDoors()
    {
        // Arrange
        var prosimControllerMock = new Mock<ProsimController>(new ServiceModel());
        var doorManager = new GSXDoorManager(prosimControllerMock.Object);
        
        // Open all doors first
        doorManager.SetForwardRightDoor(true);
        doorManager.SetAftRightDoor(true);
        doorManager.SetForwardCargoDoor(true);
        doorManager.SetAftCargoDoor(true);
        
        // Act
        doorManager.CloseAllDoors();
        
        // Assert
        Assert.IsFalse(doorManager.IsForwardRightDoorOpen);
        Assert.IsFalse(doorManager.IsAftRightDoorOpen);
        Assert.IsFalse(doorManager.IsForwardCargoDoorOpen);
        Assert.IsFalse(doorManager.IsAftCargoDoorOpen);
        prosimControllerMock.Verify(p => p.SetForwardRightDoor(false), Times.Once);
        prosimControllerMock.Verify(p => p.SetAftRightDoor(false), Times.Once);
        prosimControllerMock.Verify(p => p.SetForwardCargoDoor(false), Times.Once);
        prosimControllerMock.Verify(p => p.SetAftCargoDoor(false), Times.Once);
    }
}

[TestClass]
public class GSXEquipmentManagerTests
{
    [TestMethod]
    public void Initialize_ResetsEquipmentStates()
    {
        // Arrange
        var prosimControllerMock = new Mock<ProsimController>(new ServiceModel());
        var simConnectMock = new Mock<MobiSimConnect>();
        var equipmentManager = new GSXEquipmentManager(prosimControllerMock.Object, simConnectMock.Object);
        
        // Act
        equipmentManager.Initialize();
        
        // Assert
        Assert.IsFalse(equipmentManager.IsJetwayConnected);
        Assert.IsFalse(equipmentManager.IsStairsConnected);
        Assert.IsFalse(equipmentManager.IsGpuConnected);
        Assert.IsFalse(equipmentManager.IsPcaConnected);
        Assert.IsFalse(equipmentManager.AreChocksSet);
    }
    
    [TestMethod]
    public void SetGpu_RaisesEvent()
    {
        // Arrange
        var prosimControllerMock = new Mock<ProsimController>(new ServiceModel());
        var simConnectMock = new Mock<MobiSimConnect>();
        var equipmentManager = new GSXEquipmentManager(prosimControllerMock.Object, simConnectMock.Object);
        
        bool eventRaised = false;
        EquipmentStateChangedEventArgs eventArgs = null;
        equipmentManager.EquipmentStateChanged += (sender, e) => 
        {
            eventRaised = true;
            eventArgs = e;
        };
        
        // Act
        equipmentManager.SetGpu(true);
        
        // Assert
        Assert.IsTrue(eventRaised);
        Assert.AreEqual(EquipmentType.GPU, eventArgs.EquipmentType);
        Assert.IsTrue(eventArgs.IsConnected);
        Assert.IsTrue(equipmentManager.IsGpuConnected);
        prosimControllerMock.Verify(p => p.SetServiceGPU(true), Times.Once);
    }
    
    [TestMethod]
    public void CallJetwayStairs_CallsJetwayAndStairs()
    {
        // Arrange
        var prosimControllerMock = new Mock<ProsimController>(new ServiceModel());
        var simConnectMock = new Mock<MobiSimConnect>();
        var menuServiceMock = new Mock<IGSXMenuService>();
        var equipmentManager = new GSXEquipmentManager(prosimControllerMock.Object, simConnectMock.Object);
        
        bool eventRaised = false;
        EquipmentStateChangedEventArgs eventArgs = null;
        equipmentManager.EquipmentStateChanged += (sender, e) => 
        {
            eventRaised = true;
            eventArgs = e;
        };
        
        // Act
        equipmentManager.CallJetwayStairs(menuServiceMock.Object, 0, 0, 0, 0, false);
        
        // Assert
        Assert.IsTrue(eventRaised);
        Assert.AreEqual(EquipmentType.Jetway, eventArgs.EquipmentType);
        Assert.IsTrue(eventArgs.IsConnected);
        Assert.IsTrue(equipmentManager.IsJetwayConnected);
        menuServiceMock.Verify(m => m.MenuOpen(), Times.AtLeastOnce);
        menuServiceMock.Verify(m => m.MenuItem(6), Times.Once);
        menuServiceMock.Verify(m => m.OperatorSelection(), Times.AtLeastOnce);
    }
    
    [TestMethod]
    public void RemoveAllEquipment_RemovesAllEquipment()
    {
        // Arrange
        var prosimControllerMock = new Mock<ProsimController>(new ServiceModel());
        var simConnectMock = new Mock<MobiSimConnect>();
        var equipmentManager = new GSXEquipmentManager(prosimControllerMock.Object, simConnectMock.Object);
        
        // Connect all equipment first
        equipmentManager.SetGpu(true);
        equipmentManager.SetPca(true);
        equipmentManager.SetChocks(true);
        
        // Act
        equipmentManager.RemoveAllEquipment();
        
        // Assert
        Assert.IsFalse(equipmentManager.IsGpuConnected);
        Assert.IsFalse(equipmentManager.IsPcaConnected);
        Assert.IsFalse(equipmentManager.AreChocksSet);
        prosimControllerMock.Verify(p => p.SetServiceGPU(false), Times.Once);
        prosimControllerMock.Verify(p => p.SetServicePCA(false), Times.Once);
        prosimControllerMock.Verify(p => p.SetServiceChocks(false), Times.Once);
    }
}
```

### 10. Test the Implementation

Test the implementation to ensure it works correctly.

## Benefits

1. **Improved Separation of Concerns**
   - Door and equipment management are now handled by dedicated services
   - Each service has a single responsibility
   - GsxController is simplified and more focused

2. **Enhanced Testability**
   - Door and equipment operations can be tested in isolation
   - Dependencies are explicit and can be mocked
   - Unit tests can be written for each service

3. **Better Maintainability**
   - Changes to door and equipment management can be made without affecting other parts of the system
   - Code is more organized and easier to understand
   - New door or equipment operations can be added without modifying GsxController

4. **Event-Based Communication**
   - Components can subscribe to door and equipment state changes
   - Reduces tight coupling between components
   - Makes the system more extensible

## Next Steps

After implementing Phase 3.4, we'll proceed with Phase 3.5 to extract loadsheet management functionality into a dedicated GSXLoadsheetManager service.
