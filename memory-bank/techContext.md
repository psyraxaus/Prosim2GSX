# Technical Context: Prosim2GSX

## Technology Stack

### Core Technologies
- **C# / .NET 8**: Primary development language and framework
- **WPF (Windows Presentation Foundation)**: UI framework for Windows desktop applications
- **XAML**: Markup language for defining WPF user interfaces
- **WPF Styles and Templates**: Used for creating the modern EFB-style UI
- **WPF Resources**: Used for defining reusable styles and templates

### External Dependencies
- **SimConnect SDK**: Microsoft Flight Simulator's API for external applications
- **Prosim SDK**: API for interfacing with Prosim A320 (via ProSimSDK.dll)
- **MobiFlight WASM Module**: Provides additional MSFS variable access

### Development Tools
- **Visual Studio**: Primary IDE for development
- **.NET 8 SDK**: Required for building the application
- **Git**: Version control system

## System Requirements

### Runtime Requirements
- **Operating System**: Windows 10/11
- **.NET 8 x64 Runtime**: Both .NET Runtime and .NET Desktop Runtime
- **Microsoft Flight Simulator**: Current version
- **Prosim A320**: Current version
- **GSX Pro**: Current version
- **MobiFlight WASM Module**: Installed in MSFS Community folder

### Hardware Requirements
- Standard hardware capable of running MSFS and Prosim
- No specific additional hardware requirements

## Technical Constraints

### Platform Limitations
- Windows-only application due to dependencies
- Requires x64 architecture (not compatible with arm64)
- Cannot be run in a sandboxed environment

### Integration Constraints
- Dependent on SimConnect API behavior and limitations
- Bound by Prosim SDK capabilities
- Limited by GSX Pro's automation capabilities
- Requires specific configuration in both Prosim and GSX

### Performance Considerations
- Minimal CPU and memory footprint to avoid impacting simulation performance
- Non-blocking operations to prevent UI freezing
- Efficient event handling to manage high-frequency updates
- Optimized CG calculations to minimize performance impact during loadsheet generation

## Development Environment Setup

### Required Software
- Visual Studio 2022 or newer
- .NET 8 SDK
- Git for version control

### Project Structure
- **Prosim2GSX.sln**: Main solution file
- **Prosim2GSX/**: Main project directory
  - **Models/**: Data models
  - **Services/**: Business logic services
    - **WeightAndBalance/**: Weight and balance calculation services
      - **ProsimLoadsheetService.cs**: Service for interacting with Prosim's native loadsheet functionality
  - **UI/**: User interface components
  - **Behaviours/**: Custom WPF behaviors
  - **lib/**: External libraries (e.g., ProSimSDK.dll)

### Build Configuration
- **Debug**: Development build with additional logging
- **Release**: Optimized build for distribution

### Deployment
- Standalone executable with dependencies
- No installer required - extract and run
- Configuration stored in Prosim2GSX.dll.config

## External Interfaces

### SimConnect
- Connects to MSFS via SimConnect.dll
- Monitors aircraft state variables
- Receives events from the simulator
- Limited by SimConnect's update frequency

### Prosim SDK
- Interfaces with Prosim via ProSimSDK.dll
- Event-based communication
- Provides access to cockpit state and controls
- Allows manipulation of Prosim variables
- Provides aircraft weight and balance data
- Enables reading and manipulation of fuel quantities for CG calculations

### MobiFlight WASM
- Extends SimConnect capabilities
- Provides access to additional MSFS variables
- Higher update frequency for critical variables
- Requires the WASM module to be installed in MSFS

### GSX Pro
- No direct API - interaction through simulated inputs
- Service calls triggered through MSFS events
- Status monitoring through variable observation
- Limited automation capabilities

## Technical Debt and Challenges

### Known Limitations
- GSX Pro lacks a formal API, requiring indirect interaction
- Timing-sensitive operations may be affected by system performance
- Compatibility dependent on external software versions
- HTTP communication with Prosim's EFB server requires proper error handling and status checking
- State transitions between flight phases need careful management to prevent incorrect behavior
- Loadsheet generation depends on Prosim's native functionality being available and properly configured
- Server status checking is essential before attempting loadsheet generation to prevent failures

### Future Technical Considerations
- Adaptation to MSFS and GSX Pro updates
- Expansion of automation capabilities
- Improved error handling and recovery
- Keeping up with .NET updates and new features
- Enhancing integration with Prosim's native loadsheet functionality
- Optimizing loadsheet generation process and error handling

## Development Practices

### Coding Standards
- C# coding conventions
- MVVM pattern for UI components
- Clear separation of concerns
- Defensive programming for external interfaces
- Interface-based design for extensibility (e.g., IWeightAndBalanceCalculator)

### Testing Approach
- Manual testing with MSFS, Prosim, and GSX Pro
- Unit tests for core business logic
- Integration tests for key workflows
- User acceptance testing for complete scenarios
- Validation of CG calculations against known reference values

### Documentation
- Code documentation via XML comments
- User documentation in README
- Configuration options documented in tooltips
- Logging for troubleshooting
- Detailed comments for complex algorithms like CG calculations
