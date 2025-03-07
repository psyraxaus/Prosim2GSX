# Technical Context: Prosim2GSX

## Technologies Used

### Programming Languages and Frameworks

1. **C#**
   - Primary programming language
   - .NET 8.0 for Windows desktop application
   - Language version: C# 11.0+

2. **WPF (Windows Presentation Foundation)**
   - UI framework for Windows desktop applications
   - XAML for UI definition
   - Data binding for UI updates
   - Custom controls for EFB UI components
   - Multi-window support for secondary monitor use

3. **Newtonsoft.Json**
   - JSON parsing and serialization
   - Used for handling flight plan data and configuration
   - Will be used for EFB theme configuration

4. **CommunityToolkit.Mvvm**
   - MVVM implementation for WPF
   - Provides source generators for boilerplate code
   - Simplifies property change notification
   - Used for EFB UI view models

5. **SVG.NET (Planned)**
   - SVG rendering for aircraft diagrams
   - Vector graphics for scalable UI elements
   - Animation support for state changes

6. **WPF Animation Framework**
   - Page transition animations
   - State change visualizations
   - Progress indicators and loading animations

7. **WPF Theming System**
   - Dynamic resource dictionaries
   - Runtime theme switching
   - Airline-specific theme customization

### External SDKs and APIs

1. **SimConnect**
   - Microsoft Flight Simulator SDK
   - Provides API for interacting with MSFS2020
   - Allows reading/writing simulator variables
   - Enables subscription to simulator events

2. **ProSim SDK**
   - Official SDK for ProsimA320
   - Provides API for interacting with ProsimA320
   - Allows reading/writing ProSim variables
   - Enables monitoring of ProSim events

3. **CoreAudio**
   - Windows Core Audio API
   - Used for controlling audio levels of external applications
   - Enables volume control and mute functionality

4. **System.Speech (Planned)**
   - Text-to-speech capabilities for EFB UI
   - Voice notifications for critical events
   - Configurable voice settings

### Testing Frameworks

1. **MSTest**
   - Primary testing framework
   - Used for unit and integration testing
   - Provides test runners and assertions

2. **Moq**
   - Mocking framework for .NET
   - Used for creating mock objects in unit tests
   - Enables testing components in isolation

3. **FluentAssertions (Optional)**
   - Provides more readable assertions
   - Enhances test readability and maintainability
   - Simplifies complex assertions

4. **WPF UI Testing (Planned)**
   - UI automation testing for EFB interface
   - Visual verification of UI components
   - Interaction testing for EFB controls

### Development Tools

1. **Visual Studio**
   - Primary IDE for development
   - C# and XAML editing
   - Debugging and profiling
   - WPF Designer for UI development

2. **Git**
   - Version control system
   - Used for source code management

3. **PowerShell**
   - Preferred shell for terminal commands
   - Used for build scripts and automation
   - Commands prefixed with "powershell -Command" when executing
   - All commands use Windows/PowerShell syntax (not Linux/Mac)
   - File paths use Windows conventions with backslashes (C:\path\to\file)
   - PowerShell cmdlets preferred over legacy cmd.exe commands

4. **Blend for Visual Studio (Planned)**
   - Advanced XAML design
   - Animation creation and editing
   - Visual state management
   - Resource dictionary editing

5. **SVG Editing Tools (Planned)**
   - Creation and editing of aircraft diagrams
   - Service vehicle visualizations
   - Ground equipment icons
   - Airline logos and branding assets

## Development Environment

### Required Software

1. **Visual Studio 2019 or later**
   - With .NET desktop development workload
   - C# and WPF components

2. **Microsoft Flight Simulator 2020**
   - Required for testing GSX integration
   - SimConnect SDK must be installed

3. **ProsimA320**
   - Required for testing ProSim integration
   - Version 3.0 or higher

4. **GSX Pro for MSFS2020**
   - Required for testing GSX integration

### Build Process

1. **Compilation**
   - Standard C# compilation process
   - Outputs executable and DLLs

2. **Deployment**
   - Simple xcopy deployment
   - No installation required
   - Configuration file stored alongside executable

### Testing Environment

1. **Local Testing**
   - Requires running instances of MSFS2020 and ProsimA320
   - GSX Pro must be installed and configured in MSFS2020
   - Test flight plans must be available in ProsimA320

2. **Debug Features**
   - Logging to file for troubleshooting
   - Configurable log levels
   - Test mode for simulating arrival without flight

3. **Unit Testing**
   - MSTest for test framework
   - Moq for mocking dependencies
   - Tests organized by service/component
   - Focus on testing service interfaces

4. **Integration Testing**
   - Tests interactions between services
   - Verifies end-to-end workflows
   - Uses real or mocked external dependencies

5. **UI Testing (Planned)**
   - Visual verification of EFB UI components
   - Interaction testing for EFB controls
   - Theme switching and customization testing
   - Multi-window and secondary monitor testing

## Technical Constraints

### Platform Limitations

1. **Windows Only**
   - Application is Windows-specific due to dependencies
   - Requires Windows 10 or higher
   - No support for macOS or Linux

2. **Single-Instance**
   - Only one instance of the application can run at a time
   - Uses system tray for UI to minimize footprint
   - EFB UI will support multiple windows but single application instance

3. **External Process Dependencies**
   - Relies on MSFS2020 and ProsimA320 running
   - Must handle cases where either application is not available

### Integration Limitations

1. **SimConnect Constraints**
   - Limited to variables exposed by MSFS2020
   - Subject to SimConnect API limitations
   - Must handle connection failures gracefully

2. **ProSim SDK Constraints**
   - Limited to functionality exposed by ProSim SDK
   - Must handle connection failures gracefully
   - Version-specific compatibility issues

3. **GSX Constraints**
   - No direct API for GSX Pro
   - Relies on LVars and menu interaction
   - Limited control over GSX behavior

### Performance Considerations

1. **Resource Usage**
   - Minimal CPU and memory footprint
   - Runs alongside resource-intensive simulation software
   - Polling intervals adjusted based on flight phase
   - EFB UI will use resource loading optimization and caching

2. **Responsiveness**
   - UI must remain responsive during background operations
   - Service operations run on background threads
   - Throttling of update frequency during less critical phases
   - EFB UI will use throttling mechanisms for performance optimization

3. **Thread Safety**
   - Services may be called from different threads
   - Critical sections protected with locks
   - Async methods with cancellation support
   - Thread-safe event raising

4. **Graphics Performance (Planned)**
   - Optimized rendering for aircraft visualization
   - Efficient animation system
   - Hardware acceleration for UI components
   - Caching for theme assets and resources

## Dependencies

### Runtime Dependencies

1. **Microsoft .NET 8.0 Runtime**
   - Required for application execution
   - Must be installed on the target system
   - Provides improved performance and security over previous versions

2. **SimConnect.dll**
   - Microsoft Flight Simulator client library
   - Included with application
   - Version must match MSFS2020 installation
   - Accessed through SimConnectService abstraction

3. **ProSimSDK.dll**
   - ProsimA320 SDK library
   - Included with application
   - Version compatibility with ProsimA320 installation
   - Accessed through ProsimService abstraction

4. **CoreAudio Libraries**
   - Windows component for audio control
   - Typically pre-installed with Windows

### External System Dependencies

1. **Microsoft Flight Simulator 2020**
   - Must be installed and running
   - GSX Pro must be installed and configured
   - SimConnect must be properly configured

2. **ProsimA320**
   - Must be installed and running
   - Version 3.0 or higher
   - Network connectivity if running on separate machine

3. **ACARS Network (Optional)**
   - If ACARS integration is enabled
   - Requires network connectivity
   - Supported networks: Hoppie, SayIntentions

### EFB UI Assets (Planned)

1. **Aircraft Diagram SVG/XAML**
   - Scalable A320 aircraft diagram
   - Interactive elements for doors and service points
   - Animation states for different configurations

2. **Airline Branding Assets**
   - Logos for major airlines
   - Color schemes and typography
   - Background images and textures

3. **EFB Control Images and Icons**
   - Button and control graphics
   - Status indicators and alerts
   - Navigation icons and symbols

4. **Font Resources**
   - Airline-specific typography
   - Readable fonts for EFB displays
   - Icon fonts for common symbols

## Configuration Management

1. **User Settings**
   - Stored in XML configuration file
   - Loaded at startup
   - Updated when settings change
   - Persisted between sessions

2. **Flight Data**
   - Loaded from ProsimA320
   - Synchronized with GSX
   - Not persisted between sessions

3. **State Data**
   - Current flight state maintained in memory
   - Reset on application restart
   - Some state (fuel, hydraulic fluids) can be saved between sessions

4. **EFB Themes (Planned)**
   - Stored in JSON format
   - Airline-specific themes
   - User customization options
   - Dynamic loading at runtime

5. **EFB Window State (Planned)**
   - Window position and size
   - Detached state for secondary monitor
   - Page navigation history
   - Persisted between sessions

## Security Considerations

1. **No Network Authentication**
   - Local application with minimal network usage
   - ACARS integration requires API key/secret
   - No user authentication required

2. **No Sensitive Data**
   - Handles simulation data only
   - No personal or sensitive information

3. **Local File Access**
   - Reads/writes local configuration files
   - No access to system files or protected areas

4. **Theme Validation (Planned)**
   - JSON schema validation for theme files
   - Sanitization of external theme content
   - Error handling for invalid themes

## Architecture Patterns

1. **Service-Oriented Architecture**
   - Functionality divided into specialized services
   - Services communicate through well-defined interfaces
   - Promotes separation of concerns and testability

2. **Dependency Injection**
   - Services receive dependencies through constructors
   - Promotes loose coupling and testability
   - Enables mock implementations for testing

3. **Event-Based Communication**
   - Services raise events for state changes
   - Other services subscribe to relevant events
   - Reduces direct dependencies between components

4. **Interface-Based Design**
   - All services implement interfaces
   - Enables mock implementations for testing
   - Facilitates future platform extensions

5. **Thread Safety Patterns**
   - Lock objects for critical sections
   - Async methods with cancellation support
   - Thread-safe event raising
   - Immutable data structures where appropriate

6. **MVVM Pattern (Enhanced for EFB UI)**
   - Clear separation of View, ViewModel, and Model
   - Data binding for UI updates
   - Commands for user interactions
   - State management through view models

7. **Theme Provider Pattern (Planned)**
   - Centralized theme management
   - Dynamic resource dictionary loading
   - Theme switching at runtime
   - Consistent styling across components

## Error Handling Strategy

1. **Service-Specific Exceptions**
   - Custom exceptions for specific error scenarios
   - Detailed error information for troubleshooting
   - Consistent exception handling patterns

2. **Retry Mechanisms**
   - Automatic retry for transient failures
   - Configurable retry policies
   - Exponential backoff for external services

3. **Graceful Degradation**
   - Services can operate in degraded mode
   - Fallback behavior when dependencies fail
   - Clear error reporting to the user

4. **Comprehensive Logging**
   - Structured logging with Serilog
   - Appropriate log levels based on context
   - Detailed context information in log messages

5. **UI Error Handling (Planned)**
   - User-friendly error messages
   - Visual indicators for error states
   - Recovery options where possible
   - Detailed logging for troubleshooting
