# Prosim2GSX Service Interfaces

This document describes the service interfaces used in the Prosim2GSX application. These interfaces define the contracts between different components of the system.

## Core Service Interfaces

### ISimConnectService

Interface for interacting with Microsoft Flight Simulator via SimConnect.

```csharp
public interface ISimConnectService
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<T> GetSimVarAsync<T>(string simVar);
    Task SetSimVarAsync<T>(string simVar, T value);
    Task<bool> TransmitClientEventAsync(uint eventId, uint data = 0);
    Task SubscribeToSystemEventAsync(string eventName);
    Task SubscribeToSimVarAsync(string simVar, SimVarType type, uint interval = 1000);
    event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
    event EventHandler<SimVarChangedEventArgs> SimVarChanged;
    event EventHandler<SimEventReceivedEventArgs> SimEventReceived;
}
```

**Purpose**: Provides abstraction over the SimConnect API for interacting with Microsoft Flight Simulator.

**Key Methods**:
- `ConnectAsync()`: Establishes connection to MSFS2020
- `GetSimVarAsync<T>()`: Retrieves simulator variables
- `SetSimVarAsync<T>()`: Sets simulator variables
- `SubscribeToSimVarAsync()`: Subscribes to simulator variable changes

**Events**:
- `ConnectionStateChanged`: Raised when connection state changes
- `SimVarChanged`: Raised when a subscribed simulator variable changes
- `SimEventReceived`: Raised when a simulator event is received

### IProsimService

Interface for interacting with ProsimA320.

```csharp
public interface IProsimService
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<T> GetVariableAsync<T>(string variable);
    Task SetVariableAsync<T>(string variable, T value);
    Task ExecuteCommandAsync(string command);
    Task SubscribeToVariableAsync(string variable, uint interval = 1000);
    event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
    event EventHandler<VariableChangedEventArgs> VariableChanged;
}
```

**Purpose**: Provides abstraction over the ProSim SDK for interacting with ProsimA320.

**Key Methods**:
- `ConnectAsync()`: Establishes connection to ProsimA320
- `GetVariableAsync<T>()`: Retrieves ProSim variables
- `SetVariableAsync<T>()`: Sets ProSim variables
- `ExecuteCommandAsync()`: Executes commands in ProSim
- `SubscribeToVariableAsync()`: Subscribes to ProSim variable changes

**Events**:
- `ConnectionStateChanged`: Raised when connection state changes
- `VariableChanged`: Raised when a subscribed variable changes

### IFlightPlanService

Interface for managing flight plans.

```csharp
public interface IFlightPlanService
{
    Task<FlightPlan> LoadFlightPlanAsync(string flightNumber);
    Task<bool> SaveFlightPlanAsync(FlightPlan flightPlan);
    Task<IEnumerable<string>> GetAvailableFlightPlansAsync();
    Task<FlightPlan> GetCurrentFlightPlanAsync();
    event EventHandler<FlightPlanLoadedEventArgs> FlightPlanLoaded;
}
```

**Purpose**: Provides methods for loading, saving, and managing flight plans.

**Key Methods**:
- `LoadFlightPlanAsync()`: Loads a flight plan by flight number
- `SaveFlightPlanAsync()`: Saves a flight plan
- `GetAvailableFlightPlansAsync()`: Gets a list of available flight plans
- `GetCurrentFlightPlanAsync()`: Gets the currently loaded flight plan

**Events**:
- `FlightPlanLoaded`: Raised when a flight plan is loaded

### IAcarsService

Interface for ACARS communication.

```csharp
public interface IAcarsService
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync(string network, string callsign, string password);
    Task DisconnectAsync();
    Task<bool> SendMessageAsync(string recipient, string message);
    Task<bool> SendLoadsheetAsync(string recipient, string loadsheet);
    event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
    event EventHandler<MessageReceivedEventArgs> MessageReceived;
}
```

**Purpose**: Provides methods for communicating with ACARS networks.

**Key Methods**:
- `ConnectAsync()`: Connects to an ACARS network
- `SendMessageAsync()`: Sends a message to a recipient
- `SendLoadsheetAsync()`: Sends a loadsheet to a recipient

**Events**:
- `ConnectionStateChanged`: Raised when connection state changes
- `MessageReceived`: Raised when a message is received

## ProSim Service Interfaces

### IProsimDoorService

Interface for managing aircraft doors in ProSim.

```csharp
public interface IProsimDoorService
{
    Task<bool> OpenDoorAsync(DoorType door);
    Task<bool> CloseDoorAsync(DoorType door);
    Task<bool> IsDoorOpenAsync(DoorType door);
    Task<Dictionary<DoorType, bool>> GetAllDoorStatesAsync();
    event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
}
```

**Purpose**: Provides methods for controlling aircraft doors in ProSim.

**Key Methods**:
- `OpenDoorAsync()`: Opens a specific door
- `CloseDoorAsync()`: Closes a specific door
- `IsDoorOpenAsync()`: Checks if a door is open
- `GetAllDoorStatesAsync()`: Gets the state of all doors

**Events**:
- `DoorStateChanged`: Raised when a door state changes

### IProsimEquipmentService

Interface for managing ground equipment in ProSim.

```csharp
public interface IProsimEquipmentService
{
    Task<bool> ConnectGpuAsync();
    Task<bool> DisconnectGpuAsync();
    Task<bool> IsGpuConnectedAsync();
    Task<bool> ConnectPcaAsync();
    Task<bool> DisconnectPcaAsync();
    Task<bool> IsPcaConnectedAsync();
    Task<bool> PlaceChocksAsync();
    Task<bool> RemoveChocksAsync();
    Task<bool> AreChocksPlacedAsync();
    Task<Dictionary<EquipmentType, bool>> GetAllEquipmentStatesAsync();
    event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
}
```

**Purpose**: Provides methods for controlling ground equipment in ProSim.

**Key Methods**:
- `ConnectGpuAsync()`: Connects the GPU
- `DisconnectGpuAsync()`: Disconnects the GPU
- `ConnectPcaAsync()`: Connects the PCA
- `DisconnectPcaAsync()`: Disconnects the PCA
- `PlaceChocksAsync()`: Places wheel chocks
- `RemoveChocksAsync()`: Removes wheel chocks
- `GetAllEquipmentStatesAsync()`: Gets the state of all equipment

**Events**:
- `EquipmentStateChanged`: Raised when equipment state changes

### IProsimPassengerService

Interface for managing passengers in ProSim.

```csharp
public interface IProsimPassengerService
{
    Task<int> GetPassengerCountAsync();
    Task<int> GetMaxPassengerCountAsync();
    Task<bool> SetPassengerCountAsync(int count);
    Task<bool> StartBoardingAsync(int passengerCount);
    Task<bool> StartDeboardingAsync();
    Task<bool> IsBoardingCompleteAsync();
    Task<bool> IsDeboardingCompleteAsync();
    Task<PassengerState> GetPassengerStateAsync();
    event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
}
```

**Purpose**: Provides methods for managing passengers in ProSim.

**Key Methods**:
- `GetPassengerCountAsync()`: Gets the current passenger count
- `SetPassengerCountAsync()`: Sets the passenger count
- `StartBoardingAsync()`: Starts the boarding process
- `StartDeboardingAsync()`: Starts the deboarding process
- `IsBoardingCompleteAsync()`: Checks if boarding is complete
- `IsDeboardingCompleteAsync()`: Checks if deboarding is complete

**Events**:
- `PassengerStateChanged`: Raised when passenger state changes

### IProsimCargoService

Interface for managing cargo in ProSim.

```csharp
public interface IProsimCargoService
{
    Task<double> GetCargoWeightAsync();
    Task<double> GetMaxCargoWeightAsync();
    Task<bool> SetCargoWeightAsync(double weightKg);
    Task<bool> StartLoadingAsync(double weightKg);
    Task<bool> StartUnloadingAsync();
    Task<bool> IsLoadingCompleteAsync();
    Task<bool> IsUnloadingCompleteAsync();
    Task<CargoState> GetCargoStateAsync();
    event EventHandler<CargoStateChangedEventArgs> CargoStateChanged;
}
```

**Purpose**: Provides methods for managing cargo in ProSim.

**Key Methods**:
- `GetCargoWeightAsync()`: Gets the current cargo weight
- `SetCargoWeightAsync()`: Sets the cargo weight
- `StartLoadingAsync()`: Starts the cargo loading process
- `StartUnloadingAsync()`: Starts the cargo unloading process
- `IsLoadingCompleteAsync()`: Checks if loading is complete
- `IsUnloadingCompleteAsync()`: Checks if unloading is complete

**Events**:
- `CargoStateChanged`: Raised when cargo state changes

### IProsimFuelService

Interface for managing fuel in ProSim.

```csharp
public interface IProsimFuelService
{
    Task<double> GetCenterTankFuelAsync();
    Task<double> GetLeftTankFuelAsync();
    Task<double> GetRightTankFuelAsync();
    Task<double> GetTotalFuelAsync();
    Task<double> GetMaxFuelAsync();
    Task<bool> SetFuelAsync(double centerTankKg, double leftTankKg, double rightTankKg);
    Task<bool> StartRefuelingAsync(double targetFuelKg);
    Task<bool> StopRefuelingAsync();
    Task<bool> IsRefuelingCompleteAsync();
    Task<FuelState> GetFuelStateAsync();
    event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;
}
```

**Purpose**: Provides methods for managing fuel in ProSim.

**Key Methods**:
- `GetCenterTankFuelAsync()`: Gets the center tank fuel quantity
- `GetLeftTankFuelAsync()`: Gets the left tank fuel quantity
- `GetRightTankFuelAsync()`: Gets the right tank fuel quantity
- `GetTotalFuelAsync()`: Gets the total fuel quantity
- `SetFuelAsync()`: Sets the fuel quantities
- `StartRefuelingAsync()`: Starts the refueling process
- `StopRefuelingAsync()`: Stops the refueling process
- `IsRefuelingCompleteAsync()`: Checks if refueling is complete

**Events**:
- `FuelStateChanged`: Raised when fuel state changes

### IProsimFlightDataService

Interface for accessing flight data in ProSim.

```csharp
public interface IProsimFlightDataService
{
    Task<string> GetFlightNumberAsync();
    Task<string> GetDepartureAirportAsync();
    Task<string> GetArrivalAirportAsync();
    Task<double> GetAltitudeAsync();
    Task<double> GetSpeedAsync();
    Task<bool> IsAirborneAsync();
    Task<bool> AreEnginesRunningAsync();
    Task<bool> IsParkingBrakeSetAsync();
    Task<FlightData> GetFlightDataAsync();
    event EventHandler<FlightDataChangedEventArgs> FlightDataChanged;
}
```

**Purpose**: Provides methods for accessing flight data in ProSim.

**Key Methods**:
- `GetFlightNumberAsync()`: Gets the flight number
- `GetDepartureAirportAsync()`: Gets the departure airport
- `GetArrivalAirportAsync()`: Gets the arrival airport
- `GetAltitudeAsync()`: Gets the current altitude
- `GetSpeedAsync()`: Gets the current speed
- `IsAirborneAsync()`: Checks if the aircraft is airborne
- `AreEnginesRunningAsync()`: Checks if engines are running
- `IsParkingBrakeSetAsync()`: Checks if parking brake is set

**Events**:
- `FlightDataChanged`: Raised when flight data changes

### IProsimFluidService

Interface for managing hydraulic fluids in ProSim.

```csharp
public interface IProsimFluidService
{
    Task<double> GetHydraulicFluidLevelAsync(HydraulicSystem system);
    Task<bool> SetHydraulicFluidLevelAsync(HydraulicSystem system, double level);
    Task<Dictionary<HydraulicSystem, double>> GetAllHydraulicFluidLevelsAsync();
    event EventHandler<FluidStateChangedEventArgs> FluidStateChanged;
}
```

**Purpose**: Provides methods for managing hydraulic fluids in ProSim.

**Key Methods**:
- `GetHydraulicFluidLevelAsync()`: Gets the hydraulic fluid level for a system
- `SetHydraulicFluidLevelAsync()`: Sets the hydraulic fluid level for a system
- `GetAllHydraulicFluidLevelsAsync()`: Gets the fluid levels for all hydraulic systems

**Events**:
- `FluidStateChanged`: Raised when fluid state changes

## GSX Service Interfaces

### IGSXMenuService

Interface for interacting with GSX menus.

```csharp
public interface IGSXMenuService
{
    Task<bool> SelectMenuItemAsync(string menuItem);
    Task<bool> SelectOperatorAsync(string operator);
    Task<bool> ConfirmSelectionAsync();
    Task<bool> CancelSelectionAsync();
    Task<bool> IsMenuOpenAsync();
    Task<IEnumerable<string>> GetAvailableMenuItemsAsync();
    Task<IEnumerable<string>> GetAvailableOperatorsAsync();
}
```

**Purpose**: Provides methods for interacting with GSX menus.

**Key Methods**:
- `SelectMenuItemAsync()`: Selects a menu item
- `SelectOperatorAsync()`: Selects an operator
- `ConfirmSelectionAsync()`: Confirms the current selection
- `CancelSelectionAsync()`: Cancels the current selection
- `IsMenuOpenAsync()`: Checks if a menu is open
- `GetAvailableMenuItemsAsync()`: Gets available menu items
- `GetAvailableOperatorsAsync()`: Gets available operators

### IGSXAudioService

Interface for controlling GSX audio.

```csharp
public interface IGSXAudioService
{
    Task<bool> SetVolumeAsync(float volume);
    Task<float> GetVolumeAsync();
    Task<bool> MuteAsync();
    Task<bool> UnmuteAsync();
    Task<bool> IsMutedAsync();
    Task<bool> ResetAudioAsync();
    ValueTask<bool> SetVolumeFromKnobAsync(float knobPosition, CancellationToken cancellationToken = default);
    event EventHandler<AudioEventArgs> AudioStateChanged;
}
```

**Purpose**: Provides methods for controlling GSX audio.

**Key Methods**:
- `SetVolumeAsync()`: Sets the volume level
- `GetVolumeAsync()`: Gets the current volume level
- `MuteAsync()`: Mutes the audio
- `UnmuteAsync()`: Unmutes the audio
- `IsMutedAsync()`: Checks if audio is muted
- `ResetAudioAsync()`: Resets audio settings
- `SetVolumeFromKnobAsync()`: Sets volume based on cockpit knob position

**Events**:
- `AudioStateChanged`: Raised when audio state changes

### IGSXStateManager

Interface for managing flight state.

```csharp
public interface IGSXStateManager
{
    FlightState CurrentState { get; }
    Task<bool> InitializeAsync();
    bool TryTransitionTo(FlightState newState);
    bool IsValidTransition(FlightState currentState, FlightState newState);
    bool IsInState(FlightState state);
    bool IsInAnyState(params FlightState[] states);
    Task<FlightState> PredictNextStateAsync(AircraftParameters parameters);
    void StartStateTimeout(TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<bool> SaveStateAsync();
    Task<bool> RestoreStateAsync();
    IReadOnlyList<StateTransitionRecord> GetStateHistory();
    event EventHandler<StateChangedEventArgs<FlightState>> StateChanged;
    event EventHandler<StateTimeoutEventArgs> StateTimeout;
    event EventHandler<StateRestoredEventArgs> StateRestored;
    event EventHandler<PredictedStateChangedEventArgs> PredictedStateChanged;
}
```

**Purpose**: Provides methods for managing flight state.

**Key Methods**:
- `TryTransitionTo()`: Attempts to transition to a new state
- `IsValidTransition()`: Checks if a transition is valid
- `IsInState()`: Checks if the current state matches a specific state
- `IsInAnyState()`: Checks if the current state matches any of the specified states
- `PredictNextStateAsync()`: Predicts the next state based on aircraft parameters
- `StartStateTimeout()`: Starts a timeout for the current state
- `SaveStateAsync()`: Saves the current state
- `RestoreStateAsync()`: Restores a saved state
- `GetStateHistory()`: Gets the history of state transitions

**Events**:
- `StateChanged`: Raised when state changes
- `StateTimeout`: Raised when a state timeout occurs
- `StateRestored`: Raised when state is restored
- `PredictedStateChanged`: Raised when predicted state changes

### IGSXServiceCoordinator

Interface for coordinating GSX services.

```csharp
public interface IGSXServiceCoordinator
{
    Task<bool> RunBoardingServiceAsync();
    Task<bool> RunDeboardingServiceAsync();
    Task<bool> RunCateringServiceAsync();
    Task<bool> RunCleaningServiceAsync();
    Task<bool> RunRefuelingServiceAsync();
    Task<bool> RunPushbackServiceAsync();
    Task<bool> CancelServiceAsync(ServiceType serviceType);
    Task<bool> IsServiceRunningAsync(ServiceType serviceType);
    Task<bool> IsServiceCompleteAsync(ServiceType serviceType);
    Task<ServiceStatus> GetServiceStatusAsync(ServiceType serviceType);
    Task<Dictionary<ServiceType, ServiceStatus>> GetAllServiceStatusesAsync();
    event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;
}
```

**Purpose**: Provides methods for coordinating GSX services.

**Key Methods**:
- `RunBoardingServiceAsync()`: Runs the boarding service
- `RunDeboardingServiceAsync()`: Runs the deboarding service
- `RunCateringServiceAsync()`: Runs the catering service
- `RunCleaningServiceAsync()`: Runs the cleaning service
- `RunRefuelingServiceAsync()`: Runs the refueling service
- `RunPushbackServiceAsync()`: Runs the pushback service
- `CancelServiceAsync()`: Cancels a service
- `IsServiceRunningAsync()`: Checks if a service is running
- `IsServiceCompleteAsync()`: Checks if a service is complete
- `GetServiceStatusAsync()`: Gets the status of a service
- `GetAllServiceStatusesAsync()`: Gets the status of all services

**Events**:
- `ServiceStatusChanged`: Raised when service status changes

### IGSXDoorManager

Interface for managing aircraft doors in GSX.

```csharp
public interface IGSXDoorManager
{
    Task<bool> OpenDoorAsync(DoorType door);
    Task<bool> CloseDoorAsync(DoorType door);
    Task<bool> IsDoorOpenAsync(DoorType door);
    Task<Dictionary<DoorType, bool>> GetAllDoorStatesAsync();
    Task<bool> IsDoorToggleActiveAsync(DoorType door);
    Task<Dictionary<DoorType, bool>> GetAllDoorTogglesAsync();
    event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
}
```

**Purpose**: Provides methods for managing aircraft doors in GSX.

**Key Methods**:
- `OpenDoorAsync()`: Opens a specific door
- `CloseDoorAsync()`: Closes a specific door
- `IsDoorOpenAsync()`: Checks if a door is open
- `GetAllDoorStatesAsync()`: Gets the state of all doors
- `IsDoorToggleActiveAsync()`: Checks if a door toggle is active
- `GetAllDoorTogglesAsync()`: Gets the state of all door toggles

**Events**:
- `DoorStateChanged`: Raised when door state changes

### IGSXLoadsheetManager

Interface for managing loadsheets in GSX.

```csharp
public interface IGSXLoadsheetManager
{
    Task<Loadsheet> GenerateLoadsheetAsync(FlightPlan flightPlan);
    Task<bool> SendLoadsheetAsync(Loadsheet loadsheet);
    Task<bool> SendLoadsheetViaAcarsAsync(Loadsheet loadsheet, string recipient);
    Task<Loadsheet> GetCurrentLoadsheetAsync();
    event EventHandler<LoadsheetGeneratedEventArgs> LoadsheetGenerated;
}
```

**Purpose**: Provides methods for managing loadsheets in GSX.

**Key Methods**:
- `GenerateLoadsheetAsync()`: Generates a loadsheet from a flight plan
- `SendLoadsheetAsync()`: Sends a loadsheet to GSX
- `SendLoadsheetViaAcarsAsync()`: Sends a loadsheet via ACARS
- `GetCurrentLoadsheetAsync()`: Gets the current loadsheet

**Events**:
- `LoadsheetGenerated`: Raised when a loadsheet is generated

## GSX Coordinator Interfaces

### IGSXControllerFacade

Interface for the GSX controller facade.

```csharp
public interface IGSXControllerFacade
{
    Task<bool> InitializeAsync();
    Task<bool> StartBoardingAsync();
    Task<bool> StartDeboardingAsync();
    Task<bool> StartCateringAsync();
    Task<bool> StartCleaningAsync();
    Task<bool> StartRefuelingAsync();
    Task<bool> StartPushbackAsync();
    Task<bool> CancelServiceAsync(ServiceType serviceType);
    Task<FlightState> GetCurrentStateAsync();
    Task<bool> TransitionToStateAsync(FlightState newState);
    Task<ServiceStatus> GetServiceStatusAsync(ServiceType serviceType);
    event EventHandler<StateChangedEventArgs<FlightState>> StateChanged;
    event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;
    event EventHandler<RefuelingProgressChangedEventArgs> RefuelingProgressChanged;
}
```

**Purpose**: Provides a simplified interface to the GSX subsystem.

**Key Methods**:
- `InitializeAsync()`: Initializes the GSX controller
- `StartBoardingAsync()`: Starts the boarding process
- `StartDeboardingAsync()`: Starts the deboarding process
- `StartCateringAsync()`: Starts the catering service
- `StartCleaningAsync()`: Starts the cleaning service
- `StartRefuelingAsync()`: Starts the refueling process
- `StartPushbackAsync()`: Starts the pushback process
- `CancelServiceAsync()`: Cancels a service
- `GetCurrentStateAsync()`: Gets the current flight state
- `TransitionToStateAsync()`: Transitions to a new flight state
- `GetServiceStatusAsync()`: Gets the status of a service

**Events**:
- `StateChanged`: Raised when flight state changes
- `ServiceStatusChanged`: Raised when service status changes
- `RefuelingProgressChanged`: Raised when refueling progress changes

### IGSXServiceOrchestrator

Interface for orchestrating GSX services.

```csharp
public interface IGSXServiceOrchestrator
{
    Task<bool> InitializeAsync();
    Task<bool> StartBoardingServiceAsync();
    Task<bool> StartDeboardingServiceAsync();
    Task<bool> StartCateringServiceAsync();
    Task<bool> StartCleaningServiceAsync();
    Task<bool> StartRefuelingServiceAsync();
    Task<bool> StartPushbackServiceAsync();
    Task<bool> CancelServiceAsync(ServiceType serviceType);
    Task<bool> IsServiceRunningAsync(ServiceType serviceType);
    Task<bool> IsServiceCompleteAsync(ServiceType serviceType);
    Task<ServiceStatus> GetServiceStatusAsync(ServiceType serviceType);
    Task<Dictionary<ServiceType, ServiceStatus>> GetAllServiceStatusesAsync();
    Task<IEnumerable<ServiceType>> PredictServicesForStateAsync(FlightState state);
    Task<bool> RegisterPreServiceCallbackAsync(ServiceType serviceType, Func<Task<bool>> callback);
    Task<bool> RegisterPostServiceCallbackAsync(ServiceType serviceType, Func<Task<bool>> callback);
    void NotifyBoardingStarted();
    void NotifyDeboardingStarted();
    void NotifyCateringStarted();
    void NotifyCleaningStarted();
    void NotifyRefuelingStarted();
    void NotifyPushbackStarted();
    event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;
    event EventHandler<ServicePredictionEventArgs> ServicePredictionChanged;
}
```

**Purpose**: Provides methods for orchestrating GSX services.

**Key Methods**:
- `StartBoardingServiceAsync()`: Starts the boarding service
- `StartDeboardingServiceAsync()`: Starts the deboarding service
- `StartCateringServiceAsync()`: Starts the catering service
- `StartCleaningServiceAsync()`: Starts the cleaning service
- `StartRefuelingServiceAsync()`: Starts the refueling service
- `StartPushbackServiceAsync()`: Starts the pushback service
- `CancelServiceAsync()`: Cancels a service
- `IsServiceRunningAsync()`: Checks if a service is running
- `IsServiceCompleteAsync()`: Checks if a service is complete
- `GetServiceStatusAsync()`: Gets the status of a service
- `GetAllServiceStatusesAsync()`: Gets the status of all services
- `PredictServicesForStateAsync()`: Predicts services for a flight state
- `RegisterPreServiceCallbackAsync()`: Registers a callback to be executed before a service
- `RegisterPostServiceCallbackAsync()`: Registers a callback to be executed after a service

**Events**:
- `ServiceStatusChanged`: Raised when service status changes
- `ServicePredictionChanged`: Raised when service prediction changes

### IGSXDoorCoordinator

Interface for coordinating aircraft doors.

```csharp
public interface IGSXDoorCoordinator
{
    Task<bool> InitializeAsync();
    Task<bool> OpenDoorAsync(DoorType door);
    Task<bool> CloseDoorAsync(DoorType door);
    Task<bool> IsDoorOpenAsync(DoorType door);
    Task<Dictionary<DoorType, bool>> GetAllDoorStatesAsync();
    Task<bool> ManageDoorsForStateAsync(FlightState state);
    Task<bool> OpenDoorsForServiceAsync(ServiceType serviceType);
    Task<bool> CloseDoorsForServiceAsync(ServiceType serviceType);
    Task<bool> IsDoorRequiredForServiceAsync(DoorType door, ServiceType serviceType);
    Task<Dictionary<ServiceType, IEnumerable<DoorType>>> GetServiceDoorRequirementsAsync();
    event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
}
```

**Purpose**: Provides methods for coordinating aircraft doors.

**Key Methods**:
- `OpenDoorAsync()`: Opens a specific door
- `CloseDoorAsync()`: Closes a specific door
- `IsDoorOpenAsync()`: Checks if a door is open
- `GetAllDoorStatesAsync()`: Gets the state of all doors
- `ManageDoorsForStateAsync()`: Manages doors for a flight state
- `OpenDoorsForServiceAsync()`: Opens doors required for a service
- `CloseDoorsForServiceAsync()`: Closes doors after a service
- `IsDoorRequiredForServiceAsync()`: Checks if a door is required for a service
- `GetServiceDoorRequirementsAsync()`: Gets door requirements for all services

**Events**:
- `DoorStateChanged`: Raised when door state changes

### IGSXEquipmentCoordinator

Interface for coordinating ground equipment.

```csharp
public interface IGSXEquipmentCoordinator
{
    Task<bool> InitializeAsync();
    Task<bool> ConnectGpuAsync();
    Task<bool> DisconnectGpuAsync();
    Task<bool> IsGpuConnectedAsync();
    Task<bool> ConnectPcaAsync();
    Task<bool> DisconnectPcaAsync();
    Task<bool> IsPcaConnectedAsync();
    Task<bool> PlaceChocksAsync();
    Task<bool> RemoveChocksAsync();
    Task<bool> AreChocksPlacedAsync();
    Task<Dictionary<EquipmentType, bool>> GetAllEquipmentStatesAsync();
    Task<bool> ManageEquipmentForStateAsync(FlightState state);
    Task<bool> ConnectEquipmentForServiceAsync(ServiceType serviceType);
    Task<bool> DisconnectEquipmentForServiceAsync(ServiceType serviceType);
    Task<bool> IsEquipmentRequiredForServiceAsync(EquipmentType equipment, ServiceType serviceType);
    Task<Dictionary<ServiceType, IEnumerable<EquipmentType>>> GetServiceEquipmentRequirementsAsync();
    event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
}
```

**Purpose**: Provides methods for coordinating ground equipment.

**Key Methods**:
- `ConnectGpuAsync()`: Connects the GPU
- `DisconnectGpuAsync()`: Disconnects the GPU
- `ConnectPcaAsync()`: Connects the PCA
- `DisconnectPcaAsync()`: Disconnects the PCA
- `PlaceChocksAsync()`: Places wheel chocks
- `RemoveChocksAsync()`: Removes wheel chocks
- `GetAllEquipmentStatesAsync()`: Gets the state of all equipment
- `ManageEquipmentForStateAsync()`: Manages equipment for a flight state
- `ConnectEquipmentForServiceAsync()`: Connects equipment required for a service
- `DisconnectEquipmentForServiceAsync()`: Disconnects equipment after a service
- `IsEquipmentRequiredForServiceAsync()`: Checks if equipment is required for a service
- `GetServiceEquipmentRequirementsAsync()`: Gets equipment requirements for all services

**Events**:
- `EquipmentStateChanged`: Raised when equipment state changes

### IGSXPassengerCoordinator

Interface for coordinating passenger operations.

```csharp
public interface IGSXPassengerCoordinator
{
    Task<bool> InitializeAsync();
    Task<int> GetPassengerCountAsync();
    Task<int> GetMaxPassengerCountAsync();
    Task<bool> SetPassengerCountAsync(int count);
    Task<bool> StartBoardingAsync();
    Task<bool> StartDeboardingAsync();
    Task<bool> IsBoardingCompleteAsync();
    Task<bool> IsDeboardingCompleteAsync();
    Task<PassengerState> GetPassengerStateAsync();
    Task<double> GetBoardingProgressAsync();
    Task<double> GetDeboardingProgressAsync();
    Task<bool> UpdatePassengerManifestAsync(FlightPlan flightPlan);
    event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
    event EventHandler
