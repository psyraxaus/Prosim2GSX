# System Patterns: Prosim2GSX

## Architecture Overview

Prosim2GSX follows a modular architecture with clear separation of concerns. The system is built as a Windows desktop application that runs alongside MSFS and Prosim, acting as a bridge between these systems and GSX Pro.

```mermaid
graph TD
    P2G[Prosim2GSX] --- SC[SimConnect]
    P2G --- PI[Prosim Interface]
    P2G --- GC[GSX Controller]
    P2G --- UI[User Interface]
    P2G --- CFG[Configuration]
    P2G --- LOG[Logger]
    P2G --- IPC[IPC Manager]
    P2G --- MSC[MobiSimConnect]
    
    SC --- MSFS[Microsoft Flight Simulator]
    PI --- PROSIM[Prosim A320]
    GC --- GSX[GSX Pro]
    UI --- USER[User]
    CFG --- SETTINGS[Settings File]
    IPC --- APPS[External Applications]
    MSC --- WASM[MobiFlight WASM Module]
```

## Key Components

### Core Controllers

1. **ProsimController**
   - Interfaces with Prosim A320 through the Prosim SDK
   - Monitors aircraft state, fuel levels, passenger counts
   - Receives and processes events from Prosim
   - Sends commands to Prosim when needed

2. **GsxController**
   - Manages communication with GSX Pro
   - Initiates service calls based on aircraft state
   - Monitors GSX service status
   - Synchronizes GSX state with Prosim

3. **ServiceController**
   - Orchestrates the service flow between systems
   - Implements the business logic for when services should be called
   - Manages the state machine for ground operations
   - Handles timing and sequencing of operations

### Communication Interfaces

1. **MobiSimConnect**
   - Interfaces with the MobiFlight WASM module
   - Provides access to MSFS variables and events
   - Enables monitoring of aircraft state in MSFS

2. **ProsimInterface**
   - Wraps the Prosim SDK for easier integration
   - Provides event-based communication with Prosim
   - Abstracts Prosim-specific implementation details

3. **IPCManager**
   - Handles inter-process communication
   - Enables integration with external applications
   - Provides a communication channel for audio control

### Support Systems

1. **ConfigurationFile**
   - Manages persistent settings
   - Handles loading and saving of user preferences
   - Provides defaults for unconfigured options

2. **Logger**
   - Records application events and errors
   - Supports troubleshooting and debugging
   - Maintains history of operations

3. **FlightPlan**
   - Represents the current flight plan
   - Stores fuel, passenger, and cargo information
   - Used for synchronization between systems

## Design Patterns

### Event Aggregator Pattern
The system implements an event aggregator pattern to decouple components and improve UI responsiveness:

- **Core Components**:
  - `EventBase`: Abstract base class for all events
  - `IEventAggregator`: Interface defining publish/subscribe methods
  - `EventAggregator`: Singleton implementation with thread-safe operations
  - `SubscriptionToken`: Token-based system for managing subscriptions

- **Event Types**:
  - `ServiceStatusChangedEvent`: For ground service status changes
  - `ConnectionStatusChangedEvent`: For connection status changes
  - `FlightPhaseChangedEvent`: For flight phase transitions
  - `DataRefChangedEvent`: For Prosim dataref changes
  - `LvarChangedEvent`: For MSFS LVAR changes

- **Implementation**:
  - Publishers call `EventAggregator.Instance.Publish<TEvent>(event)` to broadcast events
  - Subscribers call `EventAggregator.Instance.Subscribe<TEvent>(handler)` to register handlers
  - Subscribers receive a token that can be used to unsubscribe later
  - Thread-safe implementation ensures reliable operation in a multi-threaded environment
  - UI components use `Dispatcher.Invoke` to update UI elements from event handlers
  - Proper cleanup is implemented to prevent memory leaks

- **Example**:
  ```csharp
  // Publishing an event
  EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Jetway", ServiceStatus.Active));
  
  // Subscribing to events
  _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<ServiceStatusChangedEvent>(OnServiceStatusChanged));
  
  // Handling events
  private void OnServiceStatusChanged(ServiceStatusChangedEvent evt)
  {
      Dispatcher.Invoke(() => {
          // Update UI based on service status
          switch (evt.ServiceName)
          {
              case "Jetway":
                  JetwayStatusIndicator.Fill = GetBrushForStatus(evt.Status);
                  break;
              // Other cases...
          }
      });
  }
  
  // Unsubscribing from events
  foreach (var token in _subscriptionTokens)
  {
      EventAggregator.Instance.Unsubscribe<EventBase>(token);
  }
  ```

### MVVM (Model-View-ViewModel)
The UI components follow the MVVM pattern, separating the user interface (View) from the business logic (ViewModel) and data (Model).

- **Models**: ServiceModel and other data structures
- **ViewModels**: NotifyIconViewModel and others
- **Views**: MainWindow.xaml and other UI components

### Dynamic Theming System
The application implements a dynamic theming system that allows customization of the UI based on airline themes:

- **Core Components**:
  - `Theme`: Class representing a theme with name, description, and colors
  - `ThemeColors`: Class containing color properties for various UI elements
  - `FlightPhaseColors`: Class containing colors for different flight phases
  - `ThemeManager`: Singleton class that handles loading, applying, and switching themes
  - `ThemeResources.xaml`: ResourceDictionary containing default theme resources

- **Theme Storage**:
  - Themes are stored as JSON files in the Themes directory
  - Each theme file contains color definitions for all UI elements
  - JSON format allows for easy creation and modification of themes
  - Theme files are loaded dynamically at runtime
  - Theme files are automatically copied to the output directory during build
  - The project file includes a wildcard pattern to ensure all theme JSON files are included

- **Color Conversion**:
  - Hex color strings in JSON (e.g., "#1E90FF") are converted to System.Windows.Media.Color objects
  - Conversion is handled by helper methods in the Theme classes
  - This approach allows for human-readable color definitions in theme files

- **Theme Application**:
  - ThemeManager loads all available themes from the Themes directory
  - The current theme is stored in application settings
  - When a theme is applied, all UI resources are updated with the theme's colors
  - The UI automatically updates to reflect the new theme

- **User Interface**:
  - Theme selection is available in the Settings tab
  - Users can switch between themes at runtime
  - A refresh button allows reloading themes without restarting the application
  - The theme directory path is displayed for users who want to create custom themes

- **Default Themes**:
  - Light: Default light theme with blue accents
  - Dark: Dark theme with blue accents on dark backgrounds
  - Airline Themes: Qantas (red), Delta (blue), Lufthansa (yellow), Finnair (blue)
  - Custom themes can be added by creating new JSON files in the Themes directory

- **Implementation**:
  ```csharp
  // Loading themes
  private void LoadThemesFromDirectory(string directory)
  {
      _themes.Clear();
      
      foreach (string file in Directory.GetFiles(directory, "*.json"))
      {
          try
          {
              string json = File.ReadAllText(file);
              Theme theme = JsonSerializer.Deserialize<Theme>(json, new JsonSerializerOptions 
              { 
                  PropertyNameCaseInsensitive = true
              });
              
              if (theme != null && !string.IsNullOrEmpty(theme.Name))
              {
                  _themes[theme.Name] = theme;
              }
          }
          catch (Exception ex)
          {
              Logger.Log(LogLevel.Warning, "ThemeManager", $"Failed to load theme file {file}: {ex.Message}");
          }
      }
  }
  
  // Applying a theme
  public void ApplyTheme(string themeName)
  {
      if (_themes.ContainsKey(themeName))
      {
          _currentTheme = _themes[themeName];
          _serviceModel.SetSetting("currentTheme", themeName);
          ApplyThemeToResources();
      }
  }
  
  // Updating UI resources
  private void ApplyThemeToResources()
  {
      var resources = Application.Current.Resources;
      
      resources["PrimaryColor"] = new SolidColorBrush(_currentTheme.Colors.GetPrimaryColor());
      resources["SecondaryColor"] = new SolidColorBrush(_currentTheme.Colors.GetSecondaryColor());
      // ... other color resources
  }
  ```

### First-Time Setup Pattern
The application implements a first-time setup pattern to ensure critical configuration is completed before the main application starts:

- **Core Components**:
  - `FirstTimeSetupDialog`: A dedicated dialog for first-time configuration
  - `App.xaml.cs`: Startup logic that checks for default configuration values
  - `ServiceModel`: Model that stores and validates configuration

- **Implementation**:
  - During application startup, the system checks if the SimBrief ID is set to the default value (0)
  - If the default value is detected, the first-time setup dialog is displayed
  - The dialog provides a user-friendly interface for entering and validating the SimBrief ID
  - Real-time validation ensures the ID is not empty, not "0", and is a valid numeric value
  - The dialog provides clear feedback on validation status
  - The user must enter a valid ID to proceed; canceling the dialog exits the application
  - Once a valid ID is provided, it's saved to the configuration and the application continues normal initialization

- **Benefits**:
  - Ensures critical configuration is completed before the application attempts to use it
  - Prevents crashes that could occur when using default or invalid configuration values
  - Provides a better user experience than error messages after startup
  - Centralizes validation logic in a dedicated component
  - Replaces the previous event-based approach that could lead to crashes

- **Example**:
  ```csharp
  // In App.xaml.cs
  if (Model.SimBriefID == "0")
  {
      // Show the first-time setup dialog
      var setupDialog = new FirstTimeSetupDialog(Model);
      bool? result = setupDialog.ShowDialog();
      
      // If the user cancels, exit the application
      if (result != true)
      {
          Logger.Log(LogLevel.Information, "App:OnStartup", 
              "User cancelled first-time setup. Exiting application.");
          Current.Shutdown();
          return;
      }
      
      // At this point, the user has entered a valid SimBrief ID
      Logger.Log(LogLevel.Information, "App:OnStartup", 
          $"User entered SimBrief ID: {Model.SimBriefID}");
  }
  
  // In FirstTimeSetupDialog.xaml.cs
  private void ValidateSimBriefID()
  {
      string id = txtSimbriefID.Text.Trim();
      
      // Check if the ID is empty
      if (string.IsNullOrWhiteSpace(id))
      {
          txtValidationMessage.Text = "Please enter a SimBrief ID.";
          btnContinue.IsEnabled = false;
          _idValidated = false;
          return;
      }
      
      // Check if the ID is "0"
      if (id == "0")
      {
          txtValidationMessage.Text = "The SimBrief ID cannot be 0. Please enter a valid ID.";
          btnContinue.IsEnabled = false;
          _idValidated = false;
          return;
      }
      
      // Check if the ID is a valid number
      if (!int.TryParse(id, out _))
      {
          txtValidationMessage.Text = "The SimBrief ID must be a numeric value.";
          btnContinue.IsEnabled = false;
          _idValidated = false;
          return;
      }
      
      // If we get here, the ID is valid
      txtValidationMessage.Text = "SimBrief ID validated successfully!";
      txtValidationMessage.Foreground = System.Windows.Media.Brushes.Green;
      btnContinue.IsEnabled = true;
      _idValidated = true;
      
      // Save the ID to the model
      _model.SetSetting("pilotID", id);
      _model.SimBriefID = id;
  }
  ```

### EFB-Style UI Design Pattern
The application implements an Electronic Flight Bag (EFB) style user interface design pattern, which is common in modern aviation applications:

- **Header Bar**: A prominent header bar with application title and navigation controls (color based on theme)
- **Tabbed Interface**: Content organized into logical tabs (FLIGHT STATUS and SETTINGS)
- **Status Indicators**: Visual indicators using color-coded circles to show connection and service states
- **Flight Phase Visualization**: A progress bar showing the current flight phase with clear visual feedback
- **Categorized Settings**: Settings organized into logical categories with clear headers
- **Modern Styling**: Consistent use of rounded corners, proper spacing, and theme-based color scheme
- **Responsive Layout**: UI elements that adapt to different states and provide clear visual feedback
- **Navigation Icons**: Simplified navigation using icon-based buttons in the header
- **Date Display**: Current date displayed in the header for situational awareness
- **Consistent Visual Language**: Uniform styling of UI elements (buttons, checkboxes, text fields, etc.)
- **Dynamic Theming**: UI colors change based on the selected theme, allowing airline-specific branding

This design pattern enhances usability by:
- Providing clear visual hierarchy and organization
- Using familiar aviation-style interface elements
- Offering immediate visual feedback on system status
- Maintaining consistency across all UI components
- Improving readability and reducing visual clutter
- Allowing personalization through theme selection

### Observer Pattern
The system uses events and event handlers extensively to communicate state changes between components:

- Controllers subscribe to events from external systems
- UI components observe changes in ViewModels
- Services react to state changes in the aircraft
- LVAR changes trigger registered callbacks through the MobiSimConnect callback system
- Prosim dataref changes trigger registered callbacks through the ProsimController dataref subscription system
- The event aggregator system extends this pattern with a centralized publish/subscribe mechanism

### Dataref Subscription Pattern
The system implements a comprehensive subscription pattern for Prosim dataref changes:

- Components register handlers for specific Prosim dataref changes via ProsimController
- A dedicated monitoring system periodically checks for changes in subscribed datarefs
- When a dataref value changes, all registered handlers are invoked with old and new values
- Thread-safe implementation ensures reliable operation in a multi-threaded environment
- Proper lifecycle management prevents memory leaks and resource exhaustion
- Multiple handlers can be registered for the same dataref, enabling flexible event handling
- Error handling is built into the monitoring system to prevent cascading failures
- Example of dataref subscription for cockpit door state:
  ```csharp
  // Register a handler for cockpit door state changes
  ProsimController.SubscribeToDataRef("system.switches.S_PED_COCKPIT_DOOR", cockpitDoorHandler);
  
  // Handler implementation
  private void OnCockpitDoorStateChanged(string dataRef, dynamic oldValue, dynamic newValue)
  {
      if (dataRef == "system.switches.S_PED_COCKPIT_DOOR")
      {
          // Determine door state based on switch position
          bool doorOpen = (int)newValue == 1;
          
          // Update GSX LVAR to match door state
          SimConnect.WriteLvar("FSDT_GSX_COCKPIT_DOOR_OPEN", doorOpen ? 1 : 0);
      }
  }
  ```

### Callback Pattern
The system implements a callback pattern for LVAR value changes:

- Components register callbacks for specific LVAR changes via MobiSimConnect
- When an LVAR value changes, registered callbacks are invoked with old and new values
- Callbacks are used to implement reactive behavior to simulator state changes
- Specific callbacks handle critical state changes like fuel hose connection/disconnection
- The refueling process uses callbacks to pause/resume based on fuel hose state
- Catering service state changes are monitored via dedicated callbacks:
  ```csharp
  private void OnCateringStateChanged(float newValue, float oldValue, string lvarName)
  {
      cateringState = newValue;
      Logger.Log(LogLevel.Debug, "GSXController", $"Catering state changed to {newValue}");
      
      if (newValue == 6 && !cateringFinished)
      {
          cateringFinished = true;
          Logger.Log(LogLevel.Information, "GSXController", $"Catering service completed");
      }
  }
  ```
- Service toggle changes trigger door operations via callbacks:
  ```csharp
  private void OnServiceToggleChanged(float newValue, float oldValue, string lvarName)
  {
      if (serviceToggles.ContainsKey(lvarName) && oldValue == SERVICE_TOGGLE_OFF && newValue == SERVICE_TOGGLE_ON)
      {
          serviceToggles[lvarName]();
      }
  }
  ```
- Error handling is built into the callback execution to prevent crashes

### State Machine
The service flow follows a state machine pattern:

- Each flight phase has defined states (pre-flight, boarding, departure, etc.)
- Transitions between states are triggered by specific events
- Actions are performed when entering or exiting states
- The refueling process implements a mini-state machine with states for active, paused, and completed
- State transitions are triggered by both GSX events and fuel hose connection status

### Dependency Injection
Components are designed with loose coupling in mind:

- Controllers accept interfaces rather than concrete implementations
- Services can be replaced or mocked for testing
- Configuration is injected rather than hardcoded

### Singleton
Some components are implemented as singletons to ensure a single instance:

- Configuration manager
- Logger
- Communication interfaces

### Dictionary-Based Action Mapping
The system uses dictionary-based action mapping for service toggles:

- Service toggle LVAR names are mapped to specific door operation actions
- This approach centralizes the mapping logic and improves maintainability
- Actions are triggered based on LVAR state changes
- The pattern allows for easy addition of new service toggle mappings
- Similar mapping approach is used for other state-based actions like refueling control
- Catering service door operations are implemented using this pattern:
  ```csharp
  // Dictionary to map service toggle LVAR names to door operations
  private readonly Dictionary<string, Action> serviceToggles = new Dictionary<string, Action>();
  
  // Initialization in constructor
  serviceToggles.Add("FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE", () => OperateFrontDoor());
  serviceToggles.Add("FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE", () => OperateAftDoor());
  serviceToggles.Add("FSDT_GSX_AIRCRAFT_CARGO_1_TOGGLE", () => OperateFrontCargoDoor());
  serviceToggles.Add("FSDT_GSX_AIRCRAFT_CARGO_2_TOGGLE", () => OperateAftCargoDoor());
  ```

## Component Relationships

### UI Update Flow with Event Aggregator
1. GsxController monitors service states, connection statuses, and flight phases
2. When a state change is detected, GsxController publishes an appropriate event through the EventAggregator
3. MainWindow, which has subscribed to these events, receives the event notification
4. Event handlers in MainWindow update the UI elements using Dispatcher.Invoke for thread safety
5. This decoupled approach allows the UI to be updated without direct dependencies on the controllers
6. Example flow for service status updates:
   - GsxController detects a change in jetway status
   - GsxController publishes a ServiceStatusChangedEvent
   - MainWindow's OnServiceStatusChanged handler is invoked
   - The handler updates the JetwayStatusIndicator with the appropriate color

### Cockpit Door State Flow
1. ProsimController monitors the cockpit door switch state via dataref subscription
2. When the cockpit door switch changes, the OnCockpitDoorStateChanged handler is invoked
3. The handler determines the door state based on the switch position (0=Normal/Closed, 1=Unlock/Open, 2=Lock/Closed)
4. The GSX LVAR (FSDT_GSX_COCKPIT_DOOR_OPEN) is updated to match the door state (0=closed, 1=open)
5. GSX uses this LVAR to control cabin sound muffling when the cockpit door is closed
6. The cockpit door indicator in Prosim is also updated to reflect the current state
7. Additionally, a DataRefChangedEvent is published through the EventAggregator

### Initialization Flow
1. Application starts and initializes core components
2. Connections are established with MSFS, Prosim, and GSX
3. Configuration is loaded
4. UI is initialized
5. Event handlers are registered
6. System begins monitoring for state changes

### Service Orchestration
1. ServiceController monitors aircraft state through ProsimController and MobiSimConnect
2. When conditions are met for a service (e.g., flight plan loaded), ServiceController triggers the appropriate action
3. GsxController executes the service call to GSX
4. System monitors for service completion
5. When service completes, state is synchronized between systems

### Refueling Process Flow
1. GSXController initiates refueling by calling the GSX refueling service
2. ProsimController initializes refueling with target fuel calculation (rounded to nearest 100)
3. Fuel hose connection state is monitored via LVAR callbacks
4. When hose is connected, refueling is active; when disconnected, refueling is paused
5. Refueling continues until target fuel level is reached or GSX reports completion
6. Center of gravity calculations are performed for accurate loadsheet data using the GetZfwCG() and GetTowCG() methods

### Loadsheet Generation Flow
1. **Server Status Checking:**
   - Before attempting to generate a loadsheet, the system checks if the Prosim EFB server is running and accessible
   - A simple GET request is sent to the server's health endpoint
   - If the server is not available, the operation is aborted with a clear error message
   - This prevents unnecessary attempts to generate loadsheets when the server is not available

2. **Loadsheet Generation:**
   - The system uses Prosim's native loadsheet functionality to generate loadsheets
   - A POST request is sent to the Prosim EFB server's loadsheet generation endpoint
   - The request includes the type of loadsheet to generate (Preliminary or Final)
   - The system tracks whether a loadsheet has already been generated for the current flight to avoid duplicate generation
   - Flags are reset when a new flight plan is loaded

3. **Error Handling:**
   - Detailed HTTP status code interpretation with specific troubleshooting steps for different error types
   - Retry logic with exponential backoff for transient failures
   - Comprehensive logging of request/response details for troubleshooting
   - Timeout handling to detect connection issues quickly
   - Specific error messages for common failure scenarios (server not available, network issues, etc.)

4. **Loadsheet Data Usage:**
   - Loadsheet data is received via dataref subscription callbacks
   - The system subscribes to the preliminary and final loadsheet datarefs
   - When a loadsheet is received, an event is raised to notify interested components
   - The loadsheet data can be sent to ACARS if enabled

### Catering Service Door Flow
1. GSXController monitors catering service state via LVAR callbacks
2. When catering service enters waiting state (state 4), passenger doors can be opened
3. Service toggle LVARs trigger door operation callbacks when changed from 0 to 1
4. Door operations are executed based on the current catering state:
   - During waiting state: Doors are opened to allow catering service
   - During finished state: Doors are closed if they were open
   - During completed state: Cargo doors can be opened for loading
5. Cargo doors are automatically closed when cargo loading reaches 100%
6. ProsimController executes the actual door operations in Prosim A320

### Data Flow
1. Flight plan data flows from Prosim to Prosim2GSX
2. Service requests flow from Prosim2GSX to GSX
3. Service status flows from GSX to Prosim2GSX
4. Synchronized state flows from Prosim2GSX to Prosim
5. Configuration flows bidirectionally between UI and ConfigurationFile
6. LVAR changes flow from MSFS to registered callbacks via MobiSimConnect
7. Door operation commands flow from GSXController to ProsimController
8. CG calculation data flows from ProsimController to GsxController for loadsheet generation
9. Loadsheet data flows from GsxController to ACARS system (when enabled)
10. Events flow from controllers to UI components via the EventAggregator

## Error Handling

- Enhanced HTTP error handling with detailed status code interpretation and troubleshooting steps
- Server status checking before attempting critical operations
- Graceful degradation when components are unavailable
- Sophisticated retry mechanisms with exponential backoff for transient failures
- Detailed logging of errors with context for effective troubleshooting
- User notifications for critical issues
- Recovery procedures for common failure scenarios
- Exception handling in LVAR callbacks to prevent cascading failures
- Value change validation to prevent unnecessary callback executions
- Exception handling in event handlers to prevent event propagation failures
- Thread-safe event publishing and subscription management
- Timeout handling for network operations
- Detailed request/response logging for API interactions
- Context-aware state handling to prevent incorrect state transitions
- Specific error handling for loadsheet generation failures:
  - HTTP 404 (Not Found): Guidance for checking if Prosim's EFB server is running on port 5000
  - HTTP 400 (Bad Request): Suggestions for verifying flight plan, fuel, passenger, and cargo data
  - HTTP 401/403 (Unauthorized/Forbidden): Tips for checking Prosim API access configuration
  - HTTP 500 (Internal Server Error): Steps to check Prosim logs and restart the EFB server
  - HTTP 503 (Service Unavailable): Guidance for checking if Prosim is running or overloaded
  - HTTP 408 (Request Timeout): Suggestions for checking network connectivity and server responsiveness
