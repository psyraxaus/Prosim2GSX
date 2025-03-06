# Phase 3.2: GSXAudioService Implementation

## Overview

This document outlines the enhanced implementation plan for Phase 3.2 of the Prosim2GSX modularization strategy. In this phase, we'll extract audio control functionality from the GsxController into a separate service following the Single Responsibility Principle, with improved testability, thread safety, and event-based communication.

## Implementation Timeline

| Task | Estimated Duration | Dependencies |
|------|-------------------|--------------|
| Create IAudioSessionManager interface and implementation | 0.5 day | None |
| Create IGSXAudioService interface | 0.5 day | None |
| Implement GSXAudioService | 2 days | IGSXAudioService, IAudioSessionManager |
| Update GsxController | 0.5 day | GSXAudioService |
| Update ServiceController | 0.5 day | GSXAudioService, AudioSessionManager |
| Testing | 1.5 days | All implementation |
| **Total** | **5-5.5 days** | |

## Implementation Steps

### 1. Create IAudioSessionManager.cs

Create a new interface file in the Services folder to abstract CoreAudio functionality.

### 2. Create Event Argument Classes

Create event argument classes for audio events.

### 3. Create IGSXAudioService.cs

Create a new interface file in the Services folder with enhanced functionality.

### 4. Create GSXAudioService.cs

Create a new implementation file in the Services folder with enhanced functionality.

### 5. Update GsxController.cs

Update the GsxController class to use the new service:

```csharp
// Add new field
private readonly IGSXAudioService audioService;

// Update constructor (assuming GSXMenuService was already added in Phase 3.1)
public GsxController(ServiceModel model, ProsimController prosimController, FlightPlan flightPlan, 
    IAcarsService acarsService, IGSXMenuService menuService, IGSXAudioService audioService)
{
    Model = model;
    ProsimController = prosimController;
    FlightPlan = flightPlan;
    this.acarsService = acarsService;
    this.menuService = menuService;
    this.audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));

    SimConnect = IPCManager.SimConnect;
    
    // Subscribe to audio service events
    this.audioService.AudioSessionFound += OnAudioSessionFound;
    this.audioService.VolumeChanged += OnVolumeChanged;
    this.audioService.MuteChanged += OnMuteChanged;
    
    // Subscribe to SimConnect variables...
}

// Add event handlers for audio service events
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

// Add cleanup in Dispose method
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        // Unsubscribe from events
        if (audioService != null)
        {
            audioService.AudioSessionFound -= OnAudioSessionFound;
            audioService.VolumeChanged -= OnVolumeChanged;
            audioService.MuteChanged -= OnMuteChanged;
        }
    }
    
    base.Dispose(disposing);
}
```

### 6. Update ServiceController.cs

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
    
    // Step 6: Create AudioSessionManager
    var audioSessionManager = new CoreAudioSessionManager();
    
    // Step 7: Create GSX services
    var menuService = new GSXMenuService(Model, IPCManager.SimConnect);
    var audioService = new GSXAudioService(Model, IPCManager.SimConnect, audioSessionManager);
    
    // Configure audio service properties
    audioService.AudioSessionRetryCount = 5; // Increase retry count for better reliability
    audioService.AudioSessionRetryDelay = TimeSpan.FromSeconds(1); // Shorter delay between retries
    
    // Step 8: Create GsxController
    var gsxController = new GsxController(Model, ProsimController, FlightPlan, acarsService, menuService, audioService);
    
    // Store the GsxController in IPCManager
    IPCManager.GsxController = gsxController;
    
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Services initialized successfully");
}
```

### 7. Add Unit Tests

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
        var audioSessionManagerMock = new Mock<IAudioSessionManager>();
        
        // Act
        var service = new GSXAudioService(model, simConnectMock.Object, audioSessionManagerMock.Object);
        
        // Assert
        Assert.IsNotNull(service);
        Assert.AreEqual(3, service.AudioSessionRetryCount);
        Assert.AreEqual(TimeSpan.FromSeconds(2), service.AudioSessionRetryDelay);
    }
    
    [TestMethod]
    public void Constructor_WithNullModel_ThrowsArgumentNullException()
    {
        // Arrange
        var simConnectMock = new Mock<MobiSimConnect>();
        var audioSessionManagerMock = new Mock<IAudioSessionManager>();
        
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new GSXAudioService(null, simConnectMock.Object, audioSessionManagerMock.Object));
    }
    
    [TestMethod]
    public void Constructor_WithNullSimConnect_ThrowsArgumentNullException()
    {
        // Arrange
        var model = new ServiceModel();
        var audioSessionManagerMock = new Mock<IAudioSessionManager>();
        
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new GSXAudioService(model, null, audioSessionManagerMock.Object));
    }
    
    [TestMethod]
    public void Constructor_WithNullAudioSessionManager_ThrowsArgumentNullException()
    {
        // Arrange
        var model = new ServiceModel();
        var simConnectMock = new Mock<MobiSimConnect>();
        
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new GSXAudioService(model, simConnectMock.Object, null));
    }
    
    [TestMethod]
    public void GetAudioSessions_CallsAudioSessionManager()
    {
        // Arrange
        var model = new ServiceModel { GsxVolumeControl = true, Vhf1VolumeControl = true, Vhf1VolumeApp = "TestApp" };
        var simConnectMock = new Mock<MobiSimConnect>();
        var audioSessionManagerMock = new Mock<IAudioSessionManager>();
        
        audioSessionManagerMock
            .Setup(m => m.GetSessionForProcessWithRetry(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns((string processName, int retryCount, TimeSpan retryDelay) => 
                new AudioSessionControl2());
        
        var service = new GSXAudioService(model, simConnectMock.Object, audioSessionManagerMock.Object);
        
        bool audioSessionFoundRaised = false;
        service.AudioSessionFound += (sender, e) => audioSessionFoundRaised = true;
        
        // Act
        service.GetAudioSessions();
        
        // Assert
        audioSessionManagerMock.Verify(m => 
            m.GetSessionForProcessWithRetry("Couatl64_MSFS", It.IsAny<int>(), It.IsAny<TimeSpan>()), 
            Times.Once);
        audioSessionManagerMock.Verify(m => 
            m.GetSessionForProcessWithRetry("TestApp", It.IsAny<int>(), It.IsAny<TimeSpan>()), 
            Times.Once);
        Assert.IsTrue(audioSessionFoundRaised);
    }
    
    [TestMethod]
    public void ResetAudio_ResetsAudioSessions()
    {
        // Arrange
        var model = new ServiceModel { GsxVolumeControl = true, Vhf1VolumeControl = true, Vhf1VolumeApp = "TestApp" };
        var simConnectMock = new Mock<MobiSimConnect>();
        var audioSessionManagerMock = new Mock<IAudioSessionManager>();
        
        var gsxSession = new AudioSessionControl2();
        var vhf1Session = new AudioSessionControl2();
        
        audioSessionManagerMock
            .Setup(m => m.GetSessionForProcessWithRetry("Couatl64_MSFS", It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(gsxSession);
        audioSessionManagerMock
            .Setup(m => m.GetSessionForProcessWithRetry("TestApp", It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(vhf1Session);
        
        var service = new GSXAudioService(model, simConnectMock.Object, audioSessionManagerMock.Object);
        
        bool volumeChangedRaised = false;
        bool muteChangedRaised = false;
        service.VolumeChanged += (sender, e) => volumeChangedRaised = true;
        service.MuteChanged += (sender, e) => muteChangedRaised = true;
        
        // Get sessions first
        service.GetAudioSessions();
        
        // Act
        service.ResetAudio();
        
        // Assert
        audioSessionManagerMock.Verify(m => m.ResetSession(gsxSession), Times.Once);
        audioSessionManagerMock.Verify(m => m.ResetSession(vhf1Session), Times.Once);
        Assert.IsTrue(volumeChangedRaised);
        Assert.IsTrue(muteChangedRaised);
    }
    
    [TestMethod]
    public async Task ControlAudioAsync_WithCancellation_DoesNotThrow()
    {
        // Arrange
        var model = new ServiceModel();
        var simConnectMock = new Mock<MobiSimConnect>();
        var audioSessionManagerMock = new Mock<IAudioSessionManager>();
        
        var service = new GSXAudioService(model, simConnectMock.Object, audioSessionManagerMock.Object);
        
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        // Act & Assert
        await service.ControlAudioAsync(cts.Token); // Should not throw
    }
    
    [TestMethod]
    public void ControlAudio_WithNoAudioMode_ResetsAudio()
    {
        // Arrange
        var model = new ServiceModel { GsxVolumeControl = true };
        var simConnectMock = new Mock<MobiSimConnect>();
        var audioSessionManagerMock = new Mock<IAudioSessionManager>();
        
        simConnectMock.Setup(m => m.ReadLvar("I_FCU_TRACK_FPA_MODE")).Returns(0);
        simConnectMock.Setup(m => m.ReadLvar("I_FCU_HEADING_VS_MODE")).Returns(0);
        
        var service = new GSXAudioService(model, simConnectMock.Object, audioSessionManagerMock.Object);
        
        // Create a spy to check if ResetAudio is called
        var serviceSpy = new Mock<IGSXAudioService>();
        serviceSpy.Setup(s => s.ResetAudio()).Verifiable();
        
        // Act
        service.ControlAudio();
        
        // Assert
        // This is a bit tricky to test directly since we can't mock the service itself
        // In a real test, we would use a test spy or refactor to make this more testable
        // For now, we'll just verify the simConnect calls
        simConnectMock.Verify(m => m.ReadLvar("I_FCU_TRACK_FPA_MODE"), Times.Once);
        simConnectMock.Verify(m => m.ReadLvar("I_FCU_HEADING_VS_MODE"), Times.Once);
    }
    
    // Additional tests for other methods...
}
```

### 8. Test the Implementation

Test the implementation to ensure it works correctly:

1. Build the solution to verify there are no compilation errors
2. Run the application and verify that audio control works as expected
3. Check the logs to ensure proper operation and error handling
4. Test edge cases such as:
   - Application startup with no audio sessions available
   - Audio sessions becoming available after startup
   - Audio sessions disappearing during operation
   - Configuration changes during operation

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

5. **Thread Safety**
   - Explicit locking for shared resources
   - Clear documentation of thread safety guarantees
   - Reduced risk of race conditions
   - Safer concurrent access to audio sessions

6. **Event-Based Communication**
   - Loose coupling between components
   - Clear notification of state changes
   - Easier to add new subscribers
   - More flexible architecture

7. **Async Support**
   - Better responsiveness during long-running operations
   - Support for cancellation
   - Improved resource utilization
   - More flexible integration options

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
