# Phase 3.1: GSXMenuService and GSXAudioService Implementation

## Overview

This document outlines the implementation plan for Phase 3.1 of the Prosim2GSX modularization strategy. In this phase, we'll extract menu interaction and audio control functionality from the GsxController into separate services.

## Implementation Steps

### 1. Create IGSXMenuService.cs

Create a new interface file in the Services folder:

```csharp
namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX menu interaction service
    /// </summary>
    public interface IGSXMenuService
    {
        /// <summary>
        /// Gets or sets whether an operator was selected
        /// </summary>
        bool OperatorWasSelected { get; set; }
        
        /// <summary>
        /// Opens the GSX menu
        /// </summary>
        void MenuOpen();
        
        /// <summary>
        /// Selects a menu item by index
        /// </summary>
        /// <param name="index">The index of the menu item to select (1-based)</param>
        /// <param name="waitForMenu">Whether to wait for the menu to be ready before selecting</param>
        void MenuItem(int index, bool waitForMenu = true);
        
        /// <summary>
        /// Waits for the GSX menu to be ready
        /// </summary>
        void MenuWaitReady();
        
        /// <summary>
        /// Checks if operator selection is active
        /// </summary>
        /// <returns>1 if operator selection is active, 0 if not, -1 if unknown</returns>
        int IsOperatorSelectionActive();
        
        /// <summary>
        /// Handles operator selection
        /// </summary>
        void OperatorSelection();
    }
}
```

### 2. Create GSXMenuService.cs

Create a new implementation file in the Services folder:

```csharp
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX menu interaction
    /// </summary>
    public class GSXMenuService : IGSXMenuService
    {
        private readonly string pathMenuFile = @"\MSFS\fsdreamteam-gsx-pro\html_ui\InGamePanels\FSDT_GSX_Panel\menu";
        private readonly string registryPath = @"HKEY_CURRENT_USER\SOFTWARE\FSDreamTeam";
        private readonly string registryValue = @"root";
        private string menuFile = "";
        private readonly MobiSimConnect simConnect;
        private readonly ServiceModel model;
        private bool operatorWasSelected = false;
        
        public bool OperatorWasSelected 
        { 
            get => operatorWasSelected; 
            set => operatorWasSelected = value; 
        }
        
        public GSXMenuService(ServiceModel model, MobiSimConnect simConnect)
        {
            this.model = model;
            this.simConnect = simConnect;
            
            string regPath = (string)Registry.GetValue(registryPath, registryValue, null) + pathMenuFile;
            if (Path.Exists(regPath))
                menuFile = regPath;
        }
        
        /// <summary>
        /// Opens the GSX menu
        /// </summary>
        public void MenuOpen()
        {
            simConnect.IsGsxMenuReady = false;
            Logger.Log(LogLevel.Debug, "GSXMenuService:MenuOpen", $"Opening GSX Menu");
            simConnect.WriteLvar("FSDT_GSX_MENU_OPEN", 1);
        }
        
        /// <summary>
        /// Selects a menu item by index
        /// </summary>
        public void MenuItem(int index, bool waitForMenu = true)
        {
            if (waitForMenu)
                MenuWaitReady();
            simConnect.IsGsxMenuReady = false;
            Logger.Log(LogLevel.Debug, "GSXMenuService:MenuItem", $"Selecting Menu Option {index} (L-Var Value {index - 1})");
            simConnect.WriteLvar("FSDT_GSX_MENU_CHOICE", index - 1);
        }
        
        /// <summary>
        /// Waits for the GSX menu to be ready
        /// </summary>
        public void MenuWaitReady()
        {
            int counter = 0;
            while (!simConnect.IsGsxMenuReady && counter < 1000) { Thread.Sleep(100); counter++; }
            Logger.Log(LogLevel.Debug, "GSXMenuService:MenuWaitReady", $"Wait ended after {counter * 100}ms");
        }
        
        /// <summary>
        /// Checks if operator selection is active
        /// </summary>
        public int IsOperatorSelectionActive()
        {
            int result = -1;

            if (!string.IsNullOrEmpty(menuFile))
            {
                string[] lines = File.ReadLines(menuFile).ToArray();
                if (lines.Length > 1)
                {
                    if (!string.IsNullOrEmpty(lines[0]) && (lines[0] == "Select handling operator" || lines[0] == "Select catering operator"))
                    {
                        Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"Match found for operator Selection: '{lines[0]}'");
                        result = 1;
                    }
                    else if (string.IsNullOrEmpty(lines[0]))
                    {
                        Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"Line is empty! Lines total: {lines.Length}");
                        result = -1;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"No Match found for operator Selection: '{lines[0]}'");
                        result = 0;
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"Menu Lines not above 1 ({lines.Length})");
                }
            }
            else
            {
                Logger.Log(LogLevel.Debug, "GSXMenuService:IsOperatorSelectionActive", $"Menu File was empty");
            }

            return result;
        }
        
        /// <summary>
        /// Handles operator selection
        /// </summary>
        public void OperatorSelection()
        {
            Thread.Sleep(2000);

            int result = IsOperatorSelectionActive();
            if (result == -1)
            {
                Logger.Log(LogLevel.Information, "GSXMenuService:OperatorSelection", $"Waiting {model.OperatorDelay}s for Operator Selection");
                Thread.Sleep((int)(model.OperatorDelay * 1000));
            }
            else if (result == 1)
            {
                Logger.Log(LogLevel.Information, "GSXMenuService:OperatorSelection", $"Operator Selection active, choosing Option 1");
                MenuItem(1);
                operatorWasSelected = true;
            }
            else
                Logger.Log(LogLevel.Information, "GSXMenuService:OperatorSelection", $"No Operator Selection needed");
        }
    }
}
```

### 3. Create IGSXAudioService.cs

Create a new interface file in the Services folder:

```csharp
namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX audio control service
    /// </summary>
    public interface IGSXAudioService
    {
        /// <summary>
        /// Gets audio sessions for GSX and VHF1
        /// </summary>
        void GetAudioSessions();
        
        /// <summary>
        /// Resets audio settings to default
        /// </summary>
        void ResetAudio();
        
        /// <summary>
        /// Controls audio based on cockpit controls
        /// </summary>
        void ControlAudio();
    }
}
```

### 4. Create GSXAudioService.cs

Create a new implementation file in the Services folder:

```csharp
using CoreAudio;
using System;
using System.Diagnostics;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX audio control
    /// </summary>
    public class GSXAudioService : IGSXAudioService
    {
        private readonly string gsxProcess = "Couatl64_MSFS";
        private AudioSessionControl2 gsxAudioSession = null;
        private float gsxAudioVolume = -1;
        private int gsxAudioMute = -1;
        private AudioSessionControl2 vhf1AudioSession = null;
        private float vhf1AudioVolume = -1;
        private int vhf1AudioMute = -1;
        private string lastVhf1App;
        
        private readonly ServiceModel model;
        private readonly MobiSimConnect simConnect;
        
        public GSXAudioService(ServiceModel model, MobiSimConnect simConnect)
        {
            this.model = model;
            this.simConnect = simConnect;
            
            if (!string.IsNullOrEmpty(model.Vhf1VolumeApp))
                lastVhf1App = model.Vhf1VolumeApp;
        }
        
        /// <summary>
        /// Gets audio sessions for GSX and VHF1
        /// </summary>
        public void GetAudioSessions()
        {
            if (model.GsxVolumeControl && gsxAudioSession == null)
            {
                MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
                var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                foreach (var device in devices)
                {
                    foreach (var session in device.AudioSessionManager2.Sessions)
                    {
                        Process p = Process.GetProcessById((int)session.ProcessID);
                        if (p.ProcessName == gsxProcess)
                        {
                            gsxAudioSession = session;
                            Logger.Log(LogLevel.Information, "GSXAudioService:GetAudioSessions", $"Found Audio Session for GSX");
                            break;
                        }
                    }

                    if (gsxAudioSession != null)
                        break;
                }
            }

            if (model.IsVhf1Controllable() && vhf1AudioSession == null)
            {
                MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
                var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                foreach (var device in devices)
                {
                    foreach (var session in device.AudioSessionManager2.Sessions)
                    {
                        Process p = Process.GetProcessById((int)session.ProcessID);
                        if (p.ProcessName == model.Vhf1VolumeApp)
                        {
                            vhf1AudioSession = session;
                            Logger.Log(LogLevel.Information, "GSXAudioService:GetAudioSessions", $"Found Audio Session for {model.Vhf1VolumeApp}");
                            break;
                        }
                    }

                    if (vhf1AudioSession != null)
                        break;
                }
            }
        }
        
        /// <summary>
        /// Resets audio settings to default
        /// </summary>
        public void ResetAudio()
        {
            if (gsxAudioSession != null && (gsxAudioSession.SimpleAudioVolume.MasterVolume != 1.0f || gsxAudioSession.SimpleAudioVolume.Mute))
            {
                gsxAudioSession.SimpleAudioVolume.MasterVolume = 1.0f;
                gsxAudioSession.SimpleAudioVolume.Mute = false;
                Logger.Log(LogLevel.Information, "GSXAudioService:ResetAudio", $"Audio resetted for GSX");
            }

            if (vhf1AudioSession != null && (vhf1AudioSession.SimpleAudioVolume.MasterVolume != 1.0f || vhf1AudioSession.SimpleAudioVolume.Mute))
            {
                vhf1AudioSession.SimpleAudioVolume.MasterVolume = 1.0f;
                vhf1AudioSession.SimpleAudioVolume.Mute = false;
                Logger.Log(LogLevel.Information, "GSXAudioService:ResetAudio", $"Audio resetted for {model.Vhf1VolumeApp}");
            }
        }
        
        /// <summary>
        /// Controls audio based on cockpit controls
        /// </summary>
        public void ControlAudio()
        {
            try
            {
                if (simConnect.ReadLvar("I_FCU_TRACK_FPA_MODE") == 0 && simConnect.ReadLvar("I_FCU_HEADING_VS_MODE") == 0)
                {
                    if (model.GsxVolumeControl || model.IsVhf1Controllable())
                        ResetAudio();
                    return;
                }

                // GSX Audio Control
                ControlGsxAudio();
                
                // VHF1 Audio Control
                ControlVhf1Audio();
                
                // App Change Handling
                HandleAppChange();
                
                // Process Exit Handling
                HandleProcessExits();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Debug, "GSXAudioService:ControlAudio", $"Exception {ex.GetType()} during Audio Control: {ex.Message}");
            }
        }
        
        private void ControlGsxAudio()
        {
            if (model.GsxVolumeControl && gsxAudioSession != null)
            {
                float volume = simConnect.ReadLvar("A_ASP_INT_VOLUME");
                int muted = (int)simConnect.ReadLvar("I_ASP_INT_REC");
                if (volume >= 0 && volume != gsxAudioVolume)
                {
                    gsxAudioSession.SimpleAudioVolume.MasterVolume = volume;
                    gsxAudioVolume = volume;
                }

                if (muted >= 0 && muted != gsxAudioMute)
                {
                    gsxAudioSession.SimpleAudioVolume.Mute = muted == 0;
                    gsxAudioMute = muted;
                }
            }
            else if (model.GsxVolumeControl && gsxAudioSession == null)
            {
                GetAudioSessions();
                gsxAudioVolume = -1;
                gsxAudioMute = -1;
            }
            else if (!model.GsxVolumeControl && gsxAudioSession != null)
            {
                gsxAudioSession.SimpleAudioVolume.MasterVolume = 1.0f;
                gsxAudioSession.SimpleAudioVolume.Mute = false;
                gsxAudioSession = null;
                gsxAudioVolume = -1;
                gsxAudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", $"Disabled Audio Session for GSX (Setting disabled)");
            }
        }
        
        private void ControlVhf1Audio()
        {
            if (model.IsVhf1Controllable() && vhf1AudioSession != null)
            {
                float volume = simConnect.ReadLvar("A_ASP_VHF_1_VOLUME");
                int muted = (int)simConnect.ReadLvar("I_ASP_VHF_1_REC");
                if (volume >= 0 && volume != vhf1AudioVolume)
                {
                    vhf1AudioSession.SimpleAudioVolume.MasterVolume = volume;
                    vhf1AudioVolume = volume;
                }

                if (model.Vhf1LatchMute && muted >= 0 && muted != vhf1AudioMute)
                {
                    vhf1AudioSession.SimpleAudioVolume.Mute = muted == 0;
                    vhf1AudioMute = muted;
                }
                else if (!model.Vhf1LatchMute && vhf1AudioSession.SimpleAudioVolume.Mute)
                {
                    Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", $"Unmuting {lastVhf1App} (App muted and Mute-Option disabled)");
                    vhf1AudioSession.SimpleAudioVolume.Mute = false;
                    vhf1AudioMute = -1;
                }
            }
            else if (model.IsVhf1Controllable() && vhf1AudioSession == null)
            {
                GetAudioSessions();
                vhf1AudioVolume = -1;
                vhf1AudioMute = -1;
            }
            else if (!model.Vhf1VolumeControl && !string.IsNullOrEmpty(lastVhf1App) && vhf1AudioSession != null)
            {
                vhf1AudioSession.SimpleAudioVolume.MasterVolume = 1.0f;
                vhf1AudioSession.SimpleAudioVolume.Mute = false;
                vhf1AudioSession = null;
                vhf1AudioVolume = -1;
                vhf1AudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", $"Disabled Audio Session for {lastVhf1App} (Setting disabled)");
            }
        }
        
        private void HandleAppChange()
        {
            if (lastVhf1App != model.Vhf1VolumeApp)
            {
                if (vhf1AudioSession != null)
                {
                    vhf1AudioSession.SimpleAudioVolume.MasterVolume = 1.0f;
                    vhf1AudioSession.SimpleAudioVolume.Mute = false;
                    vhf1AudioSession = null;
                    vhf1AudioVolume = -1;
                    vhf1AudioMute = -1;
                    Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", $"Disabled Audio Session for {lastVhf1App} (App changed)");
                }
                GetAudioSessions();
            }
            lastVhf1App = model.Vhf1VolumeApp;
        }
        
        private void HandleProcessExits()
        {
            // GSX exited
            if (model.GsxVolumeControl && gsxAudioSession != null && !IPCManager.IsProcessRunning(gsxProcess))
            {
                gsxAudioSession = null;
                gsxAudioVolume = -1;
                gsxAudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", $"Disabled Audio Session for GSX (App not running)");
            }

            // COUATL
            if (model.GsxVolumeControl && gsxAudioSession != null && simConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
            {
                gsxAudioSession.SimpleAudioVolume.MasterVolume = 1.0f;
                gsxAudioSession.SimpleAudioVolume.Mute = false;
                gsxAudioSession = null;
                gsxAudioVolume = -1;
                gsxAudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", $"Disabled Audio Session for GSX (Couatl Engine not started)");
            }

            // VHF1 exited
            if (model.IsVhf1Controllable() && vhf1AudioSession != null && !IPCManager.IsProcessRunning(model.Vhf1VolumeApp))
            {
                vhf1AudioSession = null;
                vhf1AudioVolume = -1;
                vhf1AudioMute = -1;
                Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudio", $"Disabled Audio Session for {model.Vhf1VolumeApp} (App not running)");
            }
        }
    }
}
```

### 5. Update GsxController.cs

Update the GsxController class to use the new services:

```csharp
// Add new fields
private readonly IGSXMenuService menuService;
private readonly IGSXAudioService audioService;

// Update constructor
public GsxController(ServiceModel model, ProsimController prosimController, FlightPlan flightPlan, IAcarsService acarsService, IGSXMenuService menuService, IGSXAudioService audioService)
{
    Model = model;
    ProsimController = prosimController;
    FlightPlan = flightPlan;
    this.acarsService = acarsService;
    this.menuService = menuService;
    this.audioService = audioService;

    SimConnect = IPCManager.SimConnect;
    // Subscribe to SimConnect variables...
}

// Replace GetAudioSessions method with call to service
private void GetAudioSessions()
{
    audioService.GetAudioSessions();
}

// Replace ResetAudio method with call to service
public void ResetAudio()
{
    audioService.ResetAudio();
}

// Replace ControlAudio method with call to service
public void ControlAudio()
{
    audioService.ControlAudio();
}

// Replace MenuOpen method with call to service
private void MenuOpen()
{
    menuService.MenuOpen();
}

// Replace MenuItem method with call to service
private void MenuItem(int index, bool waitForMenu = true)
{
    menuService.MenuItem(index, waitForMenu);
}

// Replace MenuWaitReady method with call to service
private void MenuWaitReady()
{
    menuService.MenuWaitReady();
}

// Replace IsOperatorSelectionActive method with call to service
private int IsOperatorSelectionActive()
{
    return menuService.IsOperatorSelectionActive();
}

// Replace OperatorSelection method with call to service
private void OperatorSelection()
{
    menuService.OperatorSelection();
}

// Update RunServices method to use operatorWasSelected from menuService
public void RunServices()
{
    // ...
    if (menuService.OperatorWasSelected)
    {
        MenuOpen();
        menuService.OperatorWasSelected = false;
    }
    // ...
}
```

### 6. Update ServiceController.cs

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
    
    // Step 7: Create GsxController
    var gsxController = new GsxController(Model, ProsimController, FlightPlan, acarsService, menuService, audioService);
    
    // Store the GsxController in IPCManager
    IPCManager.GsxController = gsxController;
    
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Services initialized successfully");
}
```

### 7. Add Unit Tests

Create unit tests for the new services in the Tests folder.

### 8. Test the Implementation

Test the implementation to ensure it works correctly.

## Benefits

1. **Improved Separation of Concerns**
   - Menu interaction and audio control are now handled by dedicated services
   - Each service has a single responsibility
   - GsxController is simplified and more focused

2. **Enhanced Testability**
   - Services can be tested in isolation
   - Dependencies are explicit and can be mocked
   - Unit tests can be written for each service

3. **Better Maintainability**
   - Changes to menu interaction or audio control can be made without affecting other parts of the system
   - Code is more organized and easier to understand
   - New features can be added to specific services without modifying GsxController

## Next Steps

After implementing Phase 3.1, we'll proceed with Phase 3.2 to extract state management functionality into a dedicated GSXStateManager service.
