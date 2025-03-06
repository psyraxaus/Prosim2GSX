# Phase 3.2: GSXAudioService Implementation

## Overview

This document outlines the implementation plan for Phase 3.2 of the Prosim2GSX modularization strategy. In this phase, we'll extract audio control functionality from the GsxController into a separate service following the Single Responsibility Principle.

## Implementation Timeline

| Task | Estimated Duration | Dependencies |
|------|-------------------|--------------|
| Create IGSXAudioService interface | 0.5 day | None |
| Implement GSXAudioService | 1.5 days | IGSXAudioService |
| Update GsxController | 0.5 day | GSXAudioService |
| Update ServiceController | 0.5 day | GSXAudioService |
| Testing | 1 day | All implementation |
| **Total** | **3-4 days** | |

## Implementation Steps

### 1. Create IGSXAudioService.cs

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

### 2. Create GSXAudioService.cs

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

### 3. Update GsxController.cs

Update the GsxController class to use the new service:

```csharp
// Add new field
private readonly IGSXAudioService audioService;

// Update constructor (assuming GSXMenuService was already added in Phase 3.1)
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
```

### 4. Update ServiceController.cs

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
    
    // Step 7: Create GsxController
    var gsxController = new GsxController(Model, ProsimController, FlightPlan, acarsService, menuService, audioService);
    
    // Store the GsxController in IPCManager
    IPCManager.GsxController = gsxController;
    
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Services initialized successfully");
}
```

### 5. Add Unit Tests

Create unit tests for GSXAudioService in the Tests folder:

```csharp
[TestClass]
public class GSXAudioServiceTests
{
    [TestMethod]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        var model = new ServiceModel();
        var simConnectMock = new Mock<MobiSimConnect>();
        
        // Act
        var service = new GSXAudioService(model, simConnectMock.Object);
        
        // Assert
        Assert.IsNotNull(service);
    }
    
    [TestMethod]
    public void ResetAudio_ResetsAudioSessions()
    {
        // Arrange
        var model = new ServiceModel { GsxVolumeControl = true };
        var simConnectMock = new Mock<MobiSimConnect>();
        var service = new GSXAudioService(model, simConnectMock.Object);
        
        // Note: This test would need to mock the CoreAudio dependencies
        // which is beyond the scope of this implementation plan
        
        // Act
        service.ResetAudio();
        
        // Assert
        // Verify audio sessions are reset
    }
    
    // Additional tests for other methods...
}
```

### 6. Test the Implementation

Test the implementation to ensure it works correctly.

## Benefits

1. **Improved Separation of Concerns**
   - Audio control is now handled by a dedicated service
   - The service has a single responsibility
   - GsxController is simplified and more focused
   - Clear boundaries between different functionalities

2. **Enhanced Testability**
   - The service can be tested in isolation
   - Dependencies are explicit and can be mocked
   - Unit tests can be written for the service
   - Easier to simulate different scenarios

3. **Better Maintainability**
   - Changes to audio control can be made without affecting other parts of the system
   - Code is more organized and easier to understand
   - New features can be added to the service without modifying GsxController
   - Reduced complexity in GsxController

4. **Improved Error Handling**
   - More focused error handling in the service
   - Better isolation of failures
   - Clearer logging and diagnostics
   - Easier to recover from specific failures

## Implementation Considerations

### Dependencies
- **CoreAudio**: The GSXAudioService will depend on the CoreAudio library for audio control
- **SimConnect**: The service will need access to SimConnect for reading/writing L-vars
- **Process Monitoring**: The service will need to monitor process state

### Error Handling
- Implement robust error handling in the service
- Use try-catch blocks for operations that might fail
- Log exceptions with appropriate context
- Provide fallback behavior when possible

### Testing Strategy
- Create unit tests for the service
- Test normal operation paths
- Test error handling paths
- Test edge cases (e.g., missing audio sessions, process exits)
- Mock CoreAudio dependencies for testing

### Refactoring Opportunities
- Consider further breaking down the GSXAudioService into smaller components:
  - GSXAudioSessionManager for managing audio sessions
  - GSXAudioVolumeController for controlling volume
  - GSXProcessMonitor for monitoring process state

## Next Steps

After implementing Phase 3.2, we'll proceed with Phase 3.3 to extract state management functionality into a dedicated GSXStateManager service. This will further reduce the complexity of the GsxController and improve the overall architecture of the application.
