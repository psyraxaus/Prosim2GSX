# Technical Context: Prosim2GSX

## Technology Stack

### Core Technologies
- **C# / .NET 8**: Primary development language and framework
- **WPF (Windows Presentation Foundation)**: UI framework for Windows desktop applications
- **XAML**: Markup language for defining WPF user interfaces
- **WPF Styles and Templates**: Used for creating the modern EFB-style UI
- **WPF Resources**: Used for defining reusable styles and templates
- **Task-based Asynchronous Pattern (TAP)**: Used for asynchronous operations
- **Dispatcher Pattern**: Used for thread-safe UI updates
- **Publisher-Subscriber Pattern**: Implemented via the EventAggregator
- **Dependency Injection**: Used for service locator pattern
- **JSON Serialization**: Used for theme configuration and data exchange
- **HTTP Client**: Used for communication with Prosim's EFB server

### External Dependencies
- **SimConnect SDK**: Microsoft Flight Simulator's API for external applications
- **Prosim SDK**: API for interfacing with Prosim A320 (via ProSimSDK.dll)
- **MobiFlight WASM Module**: Provides additional MSFS variable access
- **VoiceMeeter API**: Integration with VoiceMeeter for advanced audio control (via VoicemeeterRemote64.dll)
- **Windows.Gaming.Input**: Used for joystick detection in PTT functionality
- **System.Windows.Forms**: Used for keyboard input detection in PTT functionality
- **Newtonsoft.Json**: Used for JSON serialization/deserialization
- **System.Net.Http**: Used for HTTP communication with Prosim's EFB server
- **System.Threading.Tasks**: Used for asynchronous operations
- **System.Windows.Media**: Used for UI rendering and theming

### Development Tools
- **Visual Studio**: Primary IDE for development
- **.NET 8 SDK**: Required for building the application
- **Git**: Version control system
- **MSBuild**: Build system for .NET projects
- **NuGet**: Package manager for .NET dependencies
- **Visual Studio Debugger**: Used for debugging and troubleshooting
- **WPF Designer**: Used for UI design and XAML editing

## System Requirements

### Runtime Requirements
- **Operating System**: Windows 10/11
- **.NET 8 x64 Runtime**: Both .NET Runtime and .NET Desktop Runtime
- **Microsoft Flight Simulator**: Current version
- **Prosim A320**: Current version
- **GSX Pro**: Current version
- **MobiFlight WASM Module**: Installed in MSFS Community folder
- **VoiceMeeter** (optional): For advanced audio control
- **Prosim EFB Server**: Running on port 5000 for loadsheet generation
- **Internet Connection** (optional): For ACARS functionality

### Hardware Requirements
- Standard hardware capable of running MSFS and Prosim
- No specific additional hardware requirements

## Technical Constraints

### Platform Limitations
- Windows-only application due to dependencies
- Requires x64 architecture (not compatible with arm64)
- Cannot be run in a sandboxed environment
- Requires administrator privileges for some audio control features
- Dependent on Prosim EFB server for loadsheet generation
- Requires specific MSFS community modules (MobiFlight WASM)

### Integration Constraints
- Dependent on SimConnect API behavior and limitations
- Bound by Prosim SDK capabilities
- Limited by GSX Pro's automation capabilities
- Requires specific configuration in both Prosim and GSX
- GSX Pro lacks a formal API, requiring indirect interaction through LVARs
- Prosim EFB server must be running for loadsheet generation
- VoiceMeeter integration requires specific version compatibility
- Timing-sensitive operations may be affected by system performance

### Performance Considerations
- Minimal CPU and memory footprint to avoid impacting simulation performance
- Non-blocking operations to prevent UI freezing
- Efficient event handling to manage high-frequency updates
- Optimized CG calculations to minimize performance impact during loadsheet generation
- Asynchronous HTTP operations to prevent blocking during loadsheet generation
- Thread-safe event handling to prevent race conditions
- Proper resource cleanup to prevent memory leaks
- Optimized callback registration to minimize overhead
- Efficient state tracking to reduce unnecessary updates

## Development Environment Setup

### Required Software
- Visual Studio 2022 or newer
- .NET 8 SDK
- Git for version control
- SimConnect SDK (included in MSFS SDK)
- Prosim SDK (included in project as reference)
- VoiceMeeter (for testing audio integration)
- MSFS with MobiFlight WASM module installed
- Prosim A320 (for testing integration)
- GSX Pro (for testing integration)

### Project Structure
- **Prosim2GSX.sln**: Main solution file
- **Prosim2GSX/**: Main project directory
  - **Models/**: Data models
    - **ServiceModel.cs**: Core model for application settings and state
    - **GroundServiceStatus.cs**: Model for ground service status
    - **GsxServiceState.cs**: Model for GSX service state
    - **LoadsheetResult.cs**: Model for loadsheet generation results
    - **LoadsheetState.cs**: Enum for loadsheet generation states
    - **JoystickConfig.cs**: Model for joystick button configuration
    - **PttChannelConfig.cs**: Model for PTT channel configuration
  - **Services/**: Business logic services
    - **Audio/**: Audio control services
      - **AudioService.cs**: Service for audio control
      - **VoiceMeeterApi.cs**: Wrapper for VoiceMeeter API
    - **Connection/**: Connection services
      - **ApplicationConnectionService.cs**: Service for application connections
    - **GSX/**: GSX integration services
      - **GsxController.cs**: Main controller for GSX integration
      - **GsxHelpers.cs**: Helper methods for GSX integration
      - **Implementation/**: Implementation of GSX services
      - **Interfaces/**: Interfaces for GSX services
    - **Prosim/**: Prosim integration services
      - **ProsimServiceProvider.cs**: Provider for Prosim services
      - **Implementation/**: Implementation of Prosim services
      - **Interfaces/**: Interfaces for Prosim services
    - **PTT/**: Push-to-Talk services
      - **Implementations/**: Implementation of PTT services
        - **PttService.cs**: Main service for PTT functionality
      - **Interfaces/**: Interfaces for PTT services
        - **IPttService.cs**: Interface for PTT service
      - **Models/**: Models for PTT configuration
        - **JoystickConfig.cs**: Configuration for joystick buttons
        - **PttChannelConfig.cs**: Configuration for PTT channels
      - **Enums/**: Enumerations for PTT functionality
        - **AcpChannelType.cs**: Enum for ACP channel types
    - **WeightAndBalance/**: Weight and balance calculation services
      - **ProsimLoadsheetService.cs**: Service for interacting with Prosim's native loadsheet functionality
  - **Events/**: Event system components
    - **EventAggregator.cs**: Implementation of the event aggregator pattern
    - **EventBase.cs**: Base class for all events
    - **IEventAggregator.cs**: Interface for the event aggregator
    - **SubscriptionToken.cs**: Token for event subscriptions
    - **Various event classes**: Specific event types
  - **Themes/**: Theme system components
    - **Theme.cs**: Class representing a theme
    - **ThemeManager.cs**: Manager for loading and applying themes
    - **JSON theme files**: Theme definitions
  - **Behaviours/**: Custom WPF behaviors
    - **RealInvariantFormat.cs**: Behavior for formatting real numbers
  - **lib/**: External libraries (e.g., ProSimSDK.dll)
  - **Documentation/**: Project documentation
    - **ProsimLoadsheetIntegration.md**: Documentation for loadsheet integration

### Build Configuration
- **Debug**: Development build with additional logging
- **Release**: Optimized build for distribution
- **x64**: Target platform (required for SimConnect and Prosim SDK)
- **AnyCPU**: Not supported due to native dependencies

### Deployment
- Standalone executable with dependencies
- No installer required - extract and run
- Configuration stored in Prosim2GSX.dll.config
- Theme files stored in Themes directory
- External DLLs included in the application directory
- Requires .NET 8 Desktop Runtime to be installed
- Requires SimConnect.dll to be in the application directory or system path

## External Interfaces

### SimConnect
- Connects to MSFS via SimConnect.dll
- Monitors aircraft state variables
- Receives events from the simulator
- Limited by SimConnect's update frequency
- Implemented through the GsxSimConnectService class
- Provides access to LVAR values through ReadGsxLvar and WriteGsxLvar methods
- Handles connection and disconnection events
- Manages callback registration for LVAR changes
- Provides methods for checking simulator state (IsSimRunning, IsSimOnGround)

### Prosim SDK
- Interfaces with Prosim via ProSimSDK.dll
- Event-based communication
- Provides access to cockpit state and controls
- Allows manipulation of Prosim variables
- Provides aircraft weight and balance data
- Enables reading and manipulation of fuel quantities for CG calculations
- Implemented through the ProsimInterface class
- Provides methods for getting and setting Prosim variables
- Handles dataref subscription and notification
- Provides access to Prosim's EFB server for loadsheet generation
- Manages connection and disconnection events

### MobiFlight WASM
- Extends SimConnect capabilities
- Provides access to additional MSFS variables
- Higher update frequency for critical variables
- Requires the WASM module to be installed in MSFS
- Implemented through the MobiSimConnect class
- Provides methods for reading and writing LVAR values
- Handles callback registration for LVAR changes
- Manages connection and disconnection events

### GSX Pro
- No direct API - interaction through simulated inputs
- Service calls triggered through MSFS events
- Status monitoring through variable observation
- Limited automation capabilities
- Implemented through various GSX service classes
- GsxController orchestrates the interaction with GSX Pro
- GsxMenuService handles menu navigation for service calls
- GsxBoardingService manages boarding and deboarding
- GsxGroundServicesService handles ground equipment
- GsxRefuelingService manages the refueling process
- GsxLoadsheetService handles loadsheet generation

### Prosim EFB Server
- HTTP-based communication for loadsheet generation
- Runs on port 5000 by default
- Provides endpoints for generating preliminary and final loadsheets
- Requires proper error handling and status checking
- Implemented through the ProsimLoadsheetService class
- Uses async/await pattern for non-blocking operations
- Implements retry logic with exponential backoff
- Provides detailed error information for troubleshooting
- Includes server status checking before attempting operations

### VoiceMeeter API
- Interfaces with VoiceMeeter via VoicemeeterRemote64.dll
- Provides control over audio routing and levels
- Requires VoiceMeeter to be installed and running
- Implemented through the VoiceMeeterApi class
- Provides methods for controlling strips and buses
- Handles initialization and cleanup of the API
- Manages error handling and diagnostics
- Provides methods for getting available strips and buses

### Push-to-Talk System
- Interfaces with Prosim's ACP channel system
- Uses Windows.Gaming.Input for joystick support
- Integrates with System.Windows.Forms for keyboard detection
- Implements a sophisticated input detection system
- Provides channel-specific key mapping
- Supports application targeting for keypresses
- Implements thread-safe state management
- Offers real-time status display with visual feedback
- Integrates with the application's theming system
- Monitors "system.switches.S_ASP_SEND_CHANNEL" dataref for ACP channel integration
- Provides expandable channel sections in the UI for better organization
- Implements safeguards to prevent PTT activation on disabled channels
- Uses the EventAggregator for state change notifications
- Provides visual feedback for active/disabled channel states
- Implements modern button styles matching application theme

## Technical Debt and Challenges

### Known Limitations
- GSX Pro lacks a formal API, requiring indirect interaction
- Timing-sensitive operations may be affected by system performance
- Compatibility dependent on external software versions
- HTTP communication with Prosim's EFB server requires proper error handling and status checking
- State transitions between flight phases need careful management to prevent incorrect behavior
- Loadsheet generation depends on Prosim's native functionality being available and properly configured
- Server status checking is essential before attempting loadsheet generation to prevent failures
- VoiceMeeter integration requires careful DLL version management to ensure compatibility across different systems
- Namespace conflicts between similar types (like LogLevel) need to be handled with fully qualified names
- External DLL dependencies require proper error handling and diagnostics for troubleshooting
- Event subscription lifecycle management requires careful attention to prevent memory leaks
- Thread safety considerations for event handling and callback execution
- Proper cleanup of resources when components are disposed
- Balancing event publishing frequency with performance considerations
- Joystick button detection may vary across different hardware
- Keyboard input detection may be affected by other applications capturing keystrokes
- PTT functionality requires careful state management to prevent stuck PTT states
- ACP channel integration depends on Prosim's dataref implementation

### Future Technical Considerations
- Adaptation to MSFS and GSX Pro updates
- Expansion of automation capabilities
- Improved error handling and recovery
- Keeping up with .NET updates and new features
- Enhancing integration with Prosim's native loadsheet functionality
- Optimizing loadsheet generation process and error handling
- Implementing automated testing for core components
- Extending automation to cover push-back, de-ice, and gate selection services
- Implementing performance metrics to monitor service response times
- Enhancing the event filtering system to reduce unnecessary UI updates
- Optimizing the monitoring interval for different types of datarefs based on criticality
- Implementing a more sophisticated logging system with filtering and rotation
- Enhancing the theme system to support more customization options
- Improving the first-time setup experience with more guidance
- Extending PTT functionality to support more complex key combinations
- Improving joystick button detection for a wider range of hardware
- Enhancing keyboard input detection to handle more edge cases
- Adding more detailed logging for PTT state transitions
- Implementing more sophisticated error handling for PTT functionality
- Optimizing PTT performance for minimal latency

## Development Practices

### Coding Standards
- C# coding conventions
- MVVM pattern for UI components
- Clear separation of concerns
- Defensive programming for external interfaces
- Interface-based design for extensibility (e.g., IWeightAndBalanceCalculator)
- Consistent error handling and logging
- Proper use of async/await for asynchronous operations
- Thread-safe implementation of shared resources
- Proper resource cleanup with using statements and disposal
- Consistent naming conventions for variables, methods, and classes
- XML documentation for public APIs
- Proper exception handling with specific exception types
- Null checking and validation for method parameters

### Testing Approach
- Manual testing with MSFS, Prosim, and GSX Pro
- Unit tests for core business logic
- Integration tests for key workflows
- User acceptance testing for complete scenarios
- Validation of CG calculations against known reference values
- Testing with different theme configurations
- Testing with various audio setups (with and without VoiceMeeter)
- Testing with different flight scenarios (departure, arrival, turnaround)
- Testing with different aircraft configurations
- Testing with different GSX Pro settings
- Testing with different Prosim A320 versions
- Testing with different MSFS versions
- Testing PTT functionality with various joystick hardware
- Testing keyboard input detection with different key combinations
- Testing PTT integration with Prosim's ACP channel system
- Testing PTT UI with different theme configurations

### Documentation
- Code documentation via XML comments
- User documentation in README
- Configuration options documented in tooltips
- Logging for troubleshooting
- Detailed comments for complex algorithms like CG calculations
- Memory bank documentation for project context and progress
- Inline comments for complex logic
- Method-level documentation for public APIs
- Class-level documentation for component responsibilities
- Interface documentation for contract definitions
- Event documentation for publisher-subscriber relationships
- Error code documentation for troubleshooting
- Configuration documentation for setup and customization
- PTT functionality documentation in README
- Joystick button mapping documentation
- Keyboard shortcut documentation
- ACP channel integration documentation
