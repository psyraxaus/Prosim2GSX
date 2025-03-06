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

3. **Newtonsoft.Json**
   - JSON parsing and serialization
   - Used for handling flight plan data and configuration

4. **CommunityToolkit.Mvvm**
   - MVVM implementation for WPF
   - Provides source generators for boilerplate code
   - Simplifies property change notification

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

### Development Tools

1. **Visual Studio**
   - Primary IDE for development
   - C# and XAML editing
   - Debugging and profiling

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

## Technical Constraints

### Platform Limitations

1. **Windows Only**
   - Application is Windows-specific due to dependencies
   - Requires Windows 10 or higher
   - No support for macOS or Linux

2. **Single-Instance**
   - Only one instance of the application can run at a time
   - Uses system tray for UI to minimize footprint

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

2. **Responsiveness**
   - UI must remain responsive during background operations
   - Service operations run on background threads
   - Throttling of update frequency during less critical phases

3. **Thread Safety**
   - Services may be called from different threads
   - Critical sections protected with locks
   - Async methods with cancellation support
   - Thread-safe event raising

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
