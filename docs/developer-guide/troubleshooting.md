# Prosim2GSX Troubleshooting Guide

This guide provides solutions for common issues that may arise when developing or using Prosim2GSX.

## Table of Contents

1. [Connection Issues](#connection-issues)
2. [Service Coordination Problems](#service-coordination-problems)
3. [State Transition Failures](#state-transition-failures)
4. [Performance Problems](#performance-problems)
5. [Debugging Techniques](#debugging-techniques)

## Connection Issues

### SimConnect Connection Failures

**Symptoms**:
- Application fails to connect to MSFS2020
- "SimConnect connection failed" error message
- Services dependent on SimConnect don't work

**Causes**:
- MSFS2020 is not running
- SimConnect.dll is missing or incorrect version
- SimConnect configuration is incorrect
- Another application is using SimConnect

**Solutions**:

1. **Verify MSFS2020 is Running**
   ```csharp
   // Check if MSFS2020 is running
   private bool IsMsfsRunning()
   {
       return Process.GetProcessesByName("FlightSimulator").Length > 0;
   }
   ```

2. **Check SimConnect.dll**
   - Ensure SimConnect.dll is in the application directory
   - Verify it's the correct version for your MSFS2020 installation
   - Try copying SimConnect.dll from the MSFS2020 SDK

3. **Retry Connection with Backoff**
   ```csharp
   // Retry SimConnect connection with exponential backoff
   private async Task<bool> ConnectWithRetryAsync()
   {
       int retryCount = 0;
       int maxRetries = 5;
       TimeSpan delay = TimeSpan.FromSeconds(1);
       
       while (retryCount < maxRetries)
       {
           try
           {
               _logger.LogInformation($"Attempting to connect to SimConnect (attempt {retryCount + 1}/{maxRetries})");
               
               if (await _simConnectService.ConnectAsync())
               {
                   _logger.LogInformation("Successfully connected to SimConnect");
                   return true;
               }
               
               _logger.LogWarning($"Failed to connect to SimConnect, retrying in {delay.TotalSeconds} seconds");
               
               await Task.Delay(delay);
               
               retryCount++;
               delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, retryCount)));
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error connecting to SimConnect");
               
               await Task.Delay(delay);
               
               retryCount++;
               delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, retryCount)));
           }
       }
       
       _logger.LogError("Failed to connect to SimConnect after multiple attempts");
       return false;
   }
   ```

4. **Check for Other Applications**
   - Close other applications that might be using SimConnect
   - Restart MSFS2020 if necessary

### ProSim Connection Failures

**Symptoms**:
- Application fails to connect to ProsimA320
- "ProSim connection failed" error message
- Services dependent on ProSim don't work

**Causes**:
- ProsimA320 is not running
- ProSimSDK.dll is missing or incorrect version
- Network configuration is incorrect (if running on separate machine)
- ProSim license issues

**Solutions**:

1. **Verify ProsimA320 is Running**
   ```csharp
   // Check if ProsimA320 is running
   private bool IsProsimRunning()
   {
       return Process.GetProcessesByName("ProsimA320").Length > 0;
   }
   ```

2. **Check ProSimSDK.dll**
   - Ensure ProSimSDK.dll is in the application directory
   - Verify it's the correct version for your ProsimA320 installation

3. **Retry Connection with Backoff**
   ```csharp
   // Retry ProSim connection with exponential backoff
   private async Task<bool> ConnectWithRetryAsync()
   {
       int retryCount = 0;
       int maxRetries = 5;
       TimeSpan delay = TimeSpan.FromSeconds(1);
       
       while (retryCount < maxRetries)
       {
           try
           {
               _logger.LogInformation($"Attempting to connect to ProSim (attempt {retryCount + 1}/{maxRetries})");
               
               if (await _prosimService.ConnectAsync())
               {
                   _logger.LogInformation("Successfully connected to ProSim");
                   return true;
               }
               
               _logger.LogWarning($"Failed to connect to ProSim, retrying in {delay.TotalSeconds} seconds");
               
               await Task.Delay(delay);
               
               retryCount++;
               delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, retryCount)));
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error connecting to ProSim");
               
               await Task.Delay(delay);
               
               retryCount++;
               delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, retryCount)));
           }
       }
       
       _logger.LogError("Failed to connect to ProSim after multiple attempts");
       return false;
   }
   ```

4. **Check Network Configuration**
   - If ProsimA320 is running on a separate machine, check network connectivity
   - Verify firewall settings
   - Try pinging the ProsimA320 machine

## Service Coordination Problems

### Services Not Starting

**Symptoms**:
- Services don't start when expected
- "Service X failed to start" error message
- No visible activity for expected services

**Causes**:
- Dependencies not initialized
- State machine in wrong state
- Service coordinator not properly configured
- Circular dependencies not resolved

**Solutions**:

1. **Check Dependencies**
   ```csharp
   // Check if all dependencies are initialized
   private bool AreDependenciesInitialized()
   {
       if (_stateManager == null)
       {
           _logger.LogError("StateManager is null");
           return false;
       }
       
       if (_doorCoordinator == null)
       {
           _logger.LogError("DoorCoordinator is null");
           return false;
       }
       
       if (_equipmentCoordinator == null)
       {
           _logger.LogError("EquipmentCoordinator is null");
           return false;
       }
       
       // Check other dependencies...
       
       return true;
   }
   ```

2. **Verify State Machine State**
   ```csharp
   // Verify state machine is in the expected state
   private bool IsInExpectedState(ServiceType serviceType)
   {
       switch (serviceType)
       {
           case ServiceType.Boarding:
               return _stateManager.IsInState(FlightState.DEPARTURE);
           case ServiceType.Deboarding:
               return _stateManager.IsInState(FlightState.ARRIVAL);
           case ServiceType.Catering:
               return _stateManager.IsInState(FlightState.DEPARTURE) || _stateManager.IsInState(FlightState.TURNAROUND);
           case ServiceType.Refueling:
               return _stateManager.IsInState(FlightState.DEPARTURE) || _stateManager.IsInState(FlightState.TURNAROUND);
           case ServiceType.Pushback:
               return _stateManager.IsInState(FlightState.TAXIOUT);
           default:
               return false;
       }
   }
   ```

3. **Resolve Circular Dependencies**
   ```csharp
   // Example of resolving circular dependencies
   public class ServiceController
   {
       private IGSXServiceOrchestrator _serviceOrchestrator;
       private IGSXCargoCoordinator _cargoCoordinator;
       
       public void Initialize()
       {
           // Create cargo coordinator with null orchestrator
           _cargoCoordinator = new GSXCargoCoordinator(_prosimCargoService, null, _logger);
           
           // Create service orchestrator with cargo coordinator
           _serviceOrchestrator = new GSXServiceOrchestrator(_stateManager, _cargoCoordinator, /* other dependencies */, _logger);
           
           // Set service orchestrator on cargo coordinator
           _cargoCoordinator.SetServiceOrchestrator(_serviceOrchestrator);
       }
   }
   ```

4. **Check Service Configuration**
   ```csharp
   // Check service configuration
   private bool IsServiceConfigured(ServiceType serviceType)
   {
       switch (serviceType)
       {
           case ServiceType.Boarding:
               return _serviceModel.BoardingEnabled;
           case ServiceType.Deboarding:
               return _serviceModel.DeboardingEnabled;
           case ServiceType.Catering:
               return _serviceModel.CateringEnabled;
           case ServiceType.Refueling:
               return _serviceModel.RefuelingEnabled;
           case ServiceType.Pushback:
               return _serviceModel.PushbackEnabled;
           default:
               return false;
       }
   }
   ```

### Door Management Issues

**Symptoms**:
- Doors don't open or close when expected
- Services start but doors remain closed
- Doors open but services don't start
- Doors open and close repeatedly

**Causes**:
- Door toggle state tracking issues
- Conflicting door management systems
- Missing door state verification
- Rapid door state changes

**Solutions**:

1. **Implement Toggle State Tracking**
   ```csharp
   // Track previous toggle states
   private Dictionary<string, bool> _previousToggleStates = new Dictionary<string, bool>();
   
   // Check if toggle state has changed
   private bool HasToggleChanged(string toggleName, bool currentState)
   {
       if (!_previousToggleStates.TryGetValue(toggleName, out var previousState))
       {
           _previousToggleStates[toggleName] = currentState;
           return currentState; // Consider it changed if it's the first time we're seeing it
       }
       
       if (previousState != currentState)
       {
           _previousToggleStates[toggleName] = currentState;
           return true;
       }
       
       return false;
   }
   ```

2. **Verify Door State Before Changes**
   ```csharp
   // Verify door state before making changes
   private async Task<bool> VerifyAndOpenDoorAsync(DoorType door)
   {
       try
       {
           var isOpen = await _doorService.IsDoorOpenAsync(door);
           
           if (isOpen)
           {
               _logger.LogInformation($"Door {door} is already open, no action needed");
               return true;
           }
           
           _logger.LogInformation($"Opening door {door}");
           return await _doorService.OpenDoorAsync(door);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, $"Failed to verify and open door {door}");
           return false;
       }
   }
   ```

3. **Implement Circuit Breaker for Door Operations**
   ```csharp
   // Implement circuit breaker for door operations
   private class DoorCircuitBreaker
   {
       private readonly Dictionary<DoorType, List<DateTime>> _doorOperations = new Dictionary<DoorType, List<DateTime>>();
       private readonly TimeSpan _timeWindow = TimeSpan.FromSeconds(5);
       private readonly int _maxOperations = 5;
       private readonly ILogger _logger;
       
       public DoorCircuitBreaker(ILogger logger)
       {
           _logger = logger;
       }
       
       public bool AllowOperation(DoorType door)
       {
           if (!_doorOperations.TryGetValue(door, out var operations))
           {
               operations = new List<DateTime>();
               _doorOperations[door] = operations;
           }
           
           // Remove operations outside the time window
           operations.RemoveAll(time => DateTime.UtcNow - time > _timeWindow);
           
           // Check if we've exceeded the maximum number of operations
           if (operations.Count >= _maxOperations)
           {
               _logger.LogWarning($"Circuit breaker triggered for door {door}: too many operations in a short time");
               return false;
           }
           
           // Record the operation
           operations.Add(DateTime.UtcNow);
           return true;
       }
   }
   ```

4. **Respect Service Toggles**
   ```csharp
   // Respect service toggles when managing doors
   private async Task<bool> ManageDoorsForStateAsync(FlightState state)
   {
       try
       {
           switch (state)
           {
               case FlightState.DEPARTURE:
                   // Only close doors if no services are active
                   if (!await IsAnyServiceActiveAsync())
                   {
                       await CloseAllDoorsAsync();
                   }
                   break;
               
               // Other states...
           }
           
           return true;
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, $"Failed to manage doors for state {state}");
           return false;
       }
   }
   
   private async Task<bool> IsAnyServiceActiveAsync()
   {
       var serviceStatuses = await _serviceOrchestrator.GetAllServiceStatusesAsync();
       return serviceStatuses.Values.Any(status => status == ServiceStatus.Running);
   }
   ```

## State Transition Failures

### Invalid State Transitions

**Symptoms**:
- State transitions fail unexpectedly
- "Invalid state transition" error message
- Application gets stuck in a particular state

**Causes**:
- Transition not defined as valid
- Transition conditions not met
- State machine in inconsistent state
- Concurrent state transitions

**Solutions**:

1. **Check Valid Transitions**
   ```csharp
   // Log all valid transitions for the current state
   private void LogValidTransitions(FlightState currentState)
   {
       _logger.LogInformation($"Valid transitions from {currentState}:");
       
       foreach (FlightState state in Enum.GetValues(typeof(FlightState)))
       {
           if (IsValidTransition(currentState, state))
           {
               _logger.LogInformation($"  - {state}");
           }
       }
   }
   ```

2. **Verify Transition Conditions**
   ```csharp
   // Verify conditions for a state transition
   private async Task<bool> AreTransitionConditionsMetAsync(FlightState currentState, FlightState newState)
   {
       switch (currentState)
       {
           case FlightState.PREFLIGHT:
               if (newState == FlightState.DEPARTURE)
               {
                   return await _flightDataService.GetFlightNumberAsync() != null;
               }
               break;
           
           case FlightState.DEPARTURE:
               if (newState == FlightState.TAXIOUT)
               {
                   var equipmentStates = await _equipmentCoordinator.GetAllEquipmentStatesAsync();
                   return !equipmentStates.Values.Any(connected => connected);
               }
               break;
           
           // Other transitions...
       }
       
       return false;
   }
   ```

3. **Reset State Machine**
   ```csharp
   // Reset state machine to a known state
   private async Task<bool> ResetStateAsync()
   {
       _logger.LogWarning("Resetting state machine to PREFLIGHT");
       
       // Force state to PREFLIGHT
       _currentState = FlightState.PREFLIGHT;
       
       // Clear state history
       _stateHistory.Clear();
       
       // Record the reset
       RecordStateTransition(FlightState.PREFLIGHT, FlightState.PREFLIGHT);
       
       // Execute entry actions for PREFLIGHT
       ExecuteEntryActions(FlightState.PREFLIGHT);
       
       // Raise state changed event
       OnStateChanged(new StateChangedEventArgs<FlightState>(FlightState.PREFLIGHT, FlightState.PREFLIGHT));
       
       return true;
   }
   ```

4. **Use Locking for Concurrent Transitions**
   ```csharp
   // Use locking for concurrent transitions
   private readonly object _stateLock = new object();
   
   public bool TryTransitionTo(FlightState newState)
   {
       lock (_stateLock)
       {
           if (!IsValidTransition(CurrentState, newState))
           {
               _logger.LogWarning($"Invalid state transition from {CurrentState} to {newState}");
               return false;
           }
           
           var previousState = CurrentState;
           
           // Execute exit actions for the current state
           ExecuteExitActions(previousState);
           
           // Update the state
           CurrentState = newState;
           
           // Record the transition
           RecordStateTransition(previousState, newState);
           
           // Execute entry actions for the new state
           ExecuteEntryActions(newState);
           
           // Raise the state changed event
           OnStateChanged(new StateChangedEventArgs<FlightState>(previousState, newState));
           
           _logger.LogInformation($"State transitioned from {previousState} to {newState}");
           return true;
       }
   }
   ```

### State Prediction Failures

**Symptoms**:
- State predictions are incorrect
- Automatic state transitions don't occur
- State timeouts don't work as expected

**Causes**:
- Incorrect prediction logic
- Missing or incorrect aircraft parameters
- Timeout cancellation issues
- Event handling issues

**Solutions**:

1. **Improve Prediction Logic**
   ```csharp
   // Improve state prediction logic
   public async Task<FlightState> PredictNextStateAsync(AircraftParameters parameters)
   {
       _logger.LogInformation($"Predicting next state from {CurrentState} with parameters: {parameters}");
       
       switch (CurrentState)
       {
           case FlightState.PREFLIGHT:
               if (parameters.HasFlightPlan)
               {
                   _logger.LogInformation("Predicting transition to DEPARTURE (has flight plan)");
                   return FlightState.DEPARTURE;
               }
               break;
           
           case FlightState.DEPARTURE:
               if (!parameters.HasGroundEquipment)
               {
                   _logger.LogInformation("Predicting transition to TAXIOUT (no ground equipment)");
                   return FlightState.TAXIOUT;
               }
               break;
           
           // Other states...
       }
       
       _logger.LogInformation($"No state transition predicted, staying in {CurrentState}");
       return CurrentState;
   }
   ```

2. **Enhance Aircraft Parameters**
   ```csharp
   // Enhance aircraft parameters collection
   public async Task<AircraftParameters> GetCurrentParametersAsync()
   {
       try
       {
           var parameters = new AircraftParameters
           {
               HasFlightPlan = await _flightDataService.GetFlightNumberAsync() != null,
               IsAirborne = await _flightDataService.IsAirborneAsync(),
               EnginesRunning = await _flightDataService.AreEnginesRunningAsync(),
               ParkingBrakeSet = await _flightDataService.IsParkingBrakeSetAsync(),
               Altitude = await _flightDataService.GetAltitudeAsync(),
               Speed = await _flightDataService.GetSpeedAsync()
           };
           
           // Get equipment states
           var equipmentStates = await _equipmentCoordinator.GetAllEquipmentStatesAsync();
           parameters.HasGroundEquipment = equipmentStates.Values.Any(connected => connected);
           
           // Get passenger count
           parameters.PassengersOnBoard = await _passengerCoordinator.GetPassengerCountAsync();
           
           return parameters;
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Failed to get current aircraft parameters");
           return new AircraftParameters();
       }
   }
   ```

3. **Improve Timeout Handling**
   ```csharp
   // Improve timeout handling
   public void StartStateTimeout(TimeSpan timeout, CancellationToken cancellationToken = default)
   {
       _logger.LogInformation($"Starting state timeout for {CurrentState} with timeout {timeout}");
       
       // Cancel any existing timeout
       _stateTimeoutCts?.Cancel();
       _stateTimeoutCts?.Dispose();
       
       // Create new cancellation token source
       _stateTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
       
       // Start timeout task
       Task.Run(async () =>
       {
           try
           {
               await Task.Delay(timeout, _stateTimeoutCts.Token);
               
               _logger.LogInformation($"State timeout triggered for {CurrentState}");
               
               // Raise timeout event
               OnStateTimeout(new StateTimeoutEventArgs(CurrentState, timeout));
               
               // Attempt to transition to the next logical state
               var parameters = await _aircraftParametersProvider.GetCurrentParametersAsync();
               var predictedState = await PredictNextStateAsync(parameters);
               
               if (predictedState != CurrentState)
               {
                   _logger.LogInformation($"Attempting to transition from {CurrentState} to {predictedState} due to timeout");
                   TryTransitionTo(predictedState);
               }
           }
           catch (OperationCanceledException)
           {
               _logger.LogInformation($"State timeout cancelled for {CurrentState}");
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, $"Error in state timeout for {CurrentState}");
           }
       }, _stateTimeoutCts.Token);
   }
   ```

4. **Enhance Event Handling**
   ```csharp
   // Enhance event handling for state changes
   private void SubscribeToEvents()
   {
       // Subscribe to flight data changes
       _flightDataService.FlightDataChanged += OnFlightDataChanged;
       
       // Subscribe to equipment state changes
       _equipmentCoordinator.EquipmentStateChanged += OnEquipmentStateChanged;
       
       // Subscribe to passenger state changes
       _passengerCoordinator.PassengerStateChanged += OnPassengerStateChanged;
   }
   
   private async void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
   {
       try
       {
           _logger.LogInformation($"Flight data changed: {e}");
           
           // Get current parameters
           var parameters = await _aircraftParametersProvider.GetCurrentParametersAsync();
           
           // Predict next state
           var predictedState = await PredictNextStateAsync(parameters);
           
           // Attempt to transition if prediction is different from current state
           if (predictedState != CurrentState)
           {
               _logger.LogInformation($"Attempting to transition from {CurrentState} to {predictedState} due to flight data change");
               TryTransitionTo(predictedState);
           }
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error handling flight data change");
       }
   }
   ```

## Performance Problems

### High CPU Usage

**Symptoms**:
- Application uses excessive CPU
- System becomes unresponsive
- MSFS2020 or ProsimA320 performance degrades

**Causes**:
- Tight polling loops
- Inefficient event handling
- Excessive logging
- Memory leaks

**Solutions**:

1. **Optimize Polling Intervals**
   ```csharp
   // Optimize polling intervals based on flight state
   private TimeSpan GetPollingInterval(FlightState state)
   {
       switch (state)
       {
           case FlightState.FLIGHT:
               // Less frequent polling during flight
               return TimeSpan.FromSeconds(5);
           
           case FlightState.TAXIOUT:
           case FlightState.TAXIIN:
               // Moderate polling during taxi
               return TimeSpan.FromSeconds(2);
           
           case FlightState.DEPARTURE:
           case FlightState.ARRIVAL:
           case FlightState.TURNAROUND:
               // More frequent polling during ground operations
               return TimeSpan.FromSeconds(1);
           
           default:
               // Default polling interval
               return TimeSpan.FromSeconds(1);
       }
   }
   
   // Use adaptive polling
   private async Task StartPollingAsync(CancellationToken cancellationToken)
   {
       while (!cancellationToken.IsCancellationRequested)
       {
           try
           {
               // Get current state
               var currentState = _stateManager.CurrentState;
               
               // Get polling interval for current state
               var pollingInterval = GetPollingInterval(currentState);
               
               // Poll for updates
               await PollForUpdatesAsync();
               
               // Wait for next poll
               await Task.Delay(pollingInterval, cancellationToken);
           }
           catch (OperationCanceledException)
           {
               // Cancellation requested
               break;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error in polling loop");
               
               // Wait before retrying
               await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
           }
       }
   }
   ```

2. **Use Efficient Event Handling**
   ```csharp
   // Use weak event pattern to prevent memory leaks
   public class WeakEventManager<TEventArgs> where TEventArgs : EventArgs
   {
       private readonly Dictionary<string, List<WeakReference>> _eventHandlers = new Dictionary<string, List<WeakReference>>();
       private readonly object _lock = new object();
       
       public void AddHandler(string eventName, EventHandler<TEventArgs> handler)
       {
           lock (_lock)
           {
               if (!_eventHandlers.TryGetValue(eventName, out var handlers))
               {
                   handlers = new List<WeakReference>();
                   _eventHandlers[eventName] = handlers;
               }
               
               handlers.Add(new WeakReference(handler));
           }
       }
       
       public void RemoveHandler(string eventName, EventHandler<TEventArgs> handler)
       {
           lock (_lock)
           {
               if (_eventHandlers.TryGetValue(eventName, out var handlers))
               {
                   for (int i = handlers.Count - 1; i >= 0; i--)
                   {
                       var weakReference = handlers[i];
                       
                       if (!weakReference.IsAlive || weakReference.Target.Equals(handler))
                       {
                           handlers.RemoveAt(i);
                       }
                   }
                   
                   if (handlers.Count == 0)
                   {
                       _eventHandlers.Remove(eventName);
                   }
               }
           }
       }
       
       public void RaiseEvent(object sender, string eventName, TEventArgs args)
       {
           List<EventHandler<TEventArgs>> handlersToInvoke = new List<EventHandler<TEventArgs>>();
           
           lock (_lock)
           {
               if (_eventHandlers.TryGetValue(eventName, out var handlers))
               {
                   for (int i = handlers.Count - 1; i >= 0; i--)
                   {
                       var weakReference = handlers[i];
                       
                       if (weakReference.IsAlive)
                       {
                           handlersToInvoke.Add((EventHandler<TEventArgs>)weakReference.Target);
                       }
                       else
                       {
                           handlers.RemoveAt(i);
                       }
                   }
                   
                   if (handlers.Count == 0)
                   {
                       _eventHandlers.Remove(eventName);
                   }
               }
           }
           
           foreach (var handler in handlersToInvoke)
           {
               try
               {
                   handler(sender, args);
               }
               catch (Exception ex)
               {
                   // Log exception but continue with other handlers
                   Console.WriteLine($"Error in event handler: {ex}");
               }
           }
       }
   }
   ```

3. **Optimize Logging**
   ```csharp
   // Use conditional logging
   private void LogDebug(string message, [CallerMemberName] string memberName = "")
   {
       if (_serviceModel.VerboseLogging)
       {
           _logger.LogDebug($"[{memberName}] {message}");
       }
   }
   
   // Use structured logging
   private void LogStructured(LogLevel level, string message, params object[] args)
   {
       if (args.Length > 0)
       {
           _logger.Log(level, message, args);
       }
       else
       {
           _logger.Log(level, message);
       }
   }
   ```

4. **Use Memory Pooling**
   ```csharp
   // Use object pooling for frequently created objects
   public class ObjectPool<T> where T : class
   {
       private readonly ConcurrentBag<T> _objects;
       private readonly Func<T> _objectGenerator;
       
       public ObjectPool(Func<T> objectGenerator)
       {
           _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
           _objects = new ConcurrentBag<T>();
       }
       
       public T Get()
       {
           if (_objects.TryTake(out T item))
           {
               return item;
           }
           
           return _objectGenerator();
       }
       
       public void Return(T item)
       {
           _objects.Add(item);
       }
   }
   
   // Example usage
   private readonly ObjectPool<StringBuilder> _stringBuilderPool = new ObjectPool<StringBuilder>(() => new StringBuilder());
   
   private string FormatMessage(string format, params object[] args)
   {
       var sb = _stringBuilderPool.Get();
       try
       {
           sb.Clear();
           sb.AppendFormat(format, args);
           return sb.ToString();
       }
       finally
       {
           _stringBuilderPool.Return(sb);
       }
   }
   ```

### Memory Leaks

**Symptoms**:
- Application memory usage grows over time
- Performance degrades after extended use
- Application crashes with out of memory exceptions

**Causes**:
- Unsubscribed event handlers
- Unclosed resources
- Circular references
- Large object allocations

**Solutions**:

1. **Properly Unsubscribe from Events**
   ```csharp
   // Properly unsubscribe from events
   private void SubscribeToEvents()
   {
       _stateManager.StateChanged += OnStateChanged;
       _serviceOrchestrator.ServiceStatusChanged += OnServiceStatusChanged;
       _fuelCoordinator.RefuelingProgressChanged += OnRefuelingProgressChanged;
   }
   
   private void UnsubscribeFromEvents()
   {
       _stateManager.StateChanged -= OnStateChanged;
       _serviceOrchestrator.ServiceStatusChanged -= OnServiceStatusChanged;
       _fuelCoordinator.RefuelingProgressChanged -= OnRefuelingProgressChanged;
   }
   
   public void Dispose()
   {
       UnsubscribeFromEvents();
       
       // Dispose other resources...
   }
   ```

2. **Use Using Statements for Disposable Resources**
   ```csharp
   // Use using statements for disposable resources
   private async Task<string> ReadFileAsync(string path)
   {
       using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
       using (var reader = new StreamReader(fileStream))
       {
           return await reader.ReadToEndAsync();
       }
   }
   ```

3. **Use Weak References for Event Handlers**
   ```csharp
   // Use weak references for event handlers
   public class WeakEventHandler<TEventArgs> where TEventArgs : EventArgs
