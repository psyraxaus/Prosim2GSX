# Active Context: Prosim2GSX

## Current Work Focus

The current focus for Prosim2GSX is migrating the application from .NET 7.0 to .NET 8.0. This includes updating dependencies, addressing potential breaking changes, and ensuring compatibility with the latest .NET framework.

### Primary Objectives

1. **.NET 8.0 Migration**
   - Update target framework to .NET 8.0
   - Update NuGet packages to compatible versions
   - Address potential breaking changes
   - Ensure compatibility with external dependencies

2. **Code Improvements**
   - Enhance error handling and resilience
   - Improve security in XML processing
   - Optimize performance with .NET 8.0 features
   - Update logging and configuration handling

3. **Testing and Validation**
   - Verify build success with .NET 8.0
   - Test all major application workflows
   - Validate integration with external systems
   - Benchmark performance against .NET 7.0 baseline

## Recent Changes

### Modularization Implementation (March 2025)

1. **Core Services Extraction**
   - Completed Phase 1.1: SimConnectService implementation
     - Created ISimConnectService interface and implementation
     - Updated MobiSimConnect to use SimConnectService
     - Improved error handling and logging
   
   - Completed Phase 1.2: ProsimService implementation
     - Created IProsimService interface and implementation
     - Updated ProsimInterface to use ProsimService
     - Added event-based connection state notification
     - Improved error handling and centralized ProSim SDK interaction
     - Documentation available in to-do/modularization-implementation-phase1.2.md

2. **Shared and ProSim Services Extraction**
   - Completed Phase 2.1: AcarsService implementation
     - Created IAcarsService interface and implementation
     - Moved ACARS-related methods from GsxController
     - Updated GsxController to use AcarsService
     - Documentation available in to-do/modularization-implementation-phase2.1.md
   
   - Completed Phase 2.2: FlightPlanService implementation
     - Created IFlightPlanService interface and implementation
     - Moved flight plan loading and parsing logic from FlightPlan class
     - Updated FlightPlan class to use FlightPlanService
     - Implemented secure XML processing with proper settings
     - Added event-based notification for new flight plans
     - Documentation available in to-do/modularization-implementation-phase2.2.md
   
   - Completed Phase 2.3: ProsimDoorService implementation
     - Created IProsimDoorService interface and implementation
     - Moved door-related methods from ProsimController
     - Updated ProsimController to use ProsimDoorService
     - Added event-based notification for door state changes
     - Documentation available in to-do/modularization-implementation-phase2.3.md
   
   - Completed Phase 2.4: ProsimEquipmentService implementation
     - Created IProsimEquipmentService interface and implementation
     - Moved equipment-related methods from ProsimController
     - Updated ProsimController to use ProsimEquipmentService
     - Added event-based notification for equipment state changes
     - Documentation available in to-do/modularization-implementation-phase2.4.md
   
   - Completed Phase 2.5: ProsimPassengerService implementation
     - Created IProsimPassengerService interface and implementation
     - Moved passenger-related methods from ProsimController
     - Updated ProsimController to use ProsimPassengerService
     - Added event-based notification for passenger state changes
     - Added callback mechanism for cargo operations (transitional until ProsimCargoService)
     - Documentation available in to-do/modularization-implementation-phase2.5.md
   
   - Completed Phase 2.6: ProsimCargoService implementation
     - Created IProsimCargoService interface and implementation
     - Moved cargo-related methods from ProsimController
     - Updated ProsimController to use ProsimCargoService
     - Added event-based notification for cargo state changes
     - Documentation available in to-do/modularization-implementation-phase2.6.md
   
   - Completed Phase 2.7: ProsimFuelService implementation
     - Created IProsimFuelService interface and implementation
     - Created WeightConversionUtility for kg/lbs conversions
     - Moved fuel-related methods from ProsimController
     - Moved GetFuelRateKGS method from ServiceModel
     - Updated ProsimController to use ProsimFuelService
     - Added event-based notification for fuel state changes
     - Documentation available in to-do/modularization-implementation-phase2.7.md
   
   - Completed Phase 2.8: ProsimFlightDataService implementation
     - Created IProsimFlightDataService interface and implementation
     - Moved flight data-related methods from ProsimController
     - Updated ProsimController to use ProsimFlightDataService
     - Added event-based notification for flight data changes
     - Implemented secure XML processing in GetFMSFlightNumber method
     - Documentation available in to-do/modularization-implementation-phase2.8.md
   
   - Completed Phase 2.9: ProsimFluidService implementation
     - Created IProsimFluidService interface and implementation
     - Moved hydraulic fluid-related methods from ProsimController
     - Updated ProsimController to use ProsimFluidService
     - Added event-based notification for fluid state changes
     - Documentation available in to-do/modularization-implementation-phase2.9.md
   
   - Completed Phase 2.10: Shared Service Interfaces implementation
     - Created IPassengerService interface for passenger management
     - Created ICargoService interface for cargo management
     - Created IFuelService interface for fuel management
     - Designed interfaces to be platform-agnostic for future GSX integration
     - Documentation available in to-do/modularization-implementation-phase2.10.md

3. **Architecture Improvements**
   - Enhanced separation of concerns
   - Improved testability through interface-based design
   - Centralized error handling and logging
   - Implemented event-based communication
   - Added secure XML processing
   - Continued modularization of the codebase

### .NET 8.0 Migration (March 2025)

1. **Framework Update**
   - Updated target framework from .NET 7.0 to .NET 8.0
   - Updated version number to 0.4.0
   - Updated copyright year to 2025
   - Updated application description to indicate .NET 8.0 compatibility

2. **Dependency Updates**
   - Updated NuGet packages to .NET 8.0 compatible versions:
     - CefSharp.OffScreen.NETCore: 112.3.0 → 120.1.110
     - CommunityToolkit.Mvvm: 8.2.0 → 8.2.2
     - CoreAudio: 1.27.0 → 1.37.0
     - H.NotifyIcon.Wpf: 2.0.108 → 2.0.124
     - Serilog: 2.12.0 → 3.1.1
     - chromiumembeddedframework.runtime packages: 112.3.0 → 120.1.110

3. **Code Improvements**
   - Enhanced XML handling in ConfigurationFile.cs
   - Improved culture and formatting in RealInvariantFormat.cs
   - Updated CefSharp initialization in App.xaml.cs
   - Enhanced error handling and logging throughout the application
   - Improved application startup and configuration

### Development Environment Updates (March 2025)

1. **Shell Preferences**
   - Added PowerShell as the preferred shell for all terminal commands
   - Updated .clinerules to include Development Environment section
   - Standardized on PowerShell conventions for command execution
   - Commands will be prefixed with "powershell -Command" when executing
   - All terminal commands will use Windows/PowerShell syntax (not Linux/Mac)
   - File paths will use Windows conventions (backslashes or properly escaped)

## Current State Assessment

Based on the initial code review, Prosim2GSX appears to be a functional application with the following key components implemented:

1. **Core Integration**
   - Connection to ProsimA320 via ProSim SDK
   - Connection to MSFS2020 via SimConnect
   - Basic flight state management

2. **Service Automation**
   - Boarding and deboarding synchronization
   - Refueling synchronization
   - Catering service calls
   - Ground equipment management

3. **User Interface**
   - System tray application
   - Configuration UI with settings
   - Status indicators for connections

4. **Additional Features**
   - Audio control for GSX and other applications
   - ACARS integration for loadsheets
   - Flight plan synchronization

## Next Steps

The following steps are recommended for continued development and improvement of Prosim2GSX:

### Short-term Tasks

1. **Complete Memory Bank Documentation**
   - Finalize all memory bank files
   - Ensure documentation accurately reflects the current state
   - Identify any gaps in documentation

2. **Code Review**
   - Perform a detailed review of the codebase
   - Identify potential bugs or issues
   - Document code quality and maintainability

3. **Testing**
   - Test the application with ProsimA320 and MSFS2020
   - Verify all features work as expected
   - Document any issues found

### Medium-term Tasks

1. **Bug Fixes**
   - Address any issues identified during testing
   - Improve error handling and resilience
   - Fix edge cases in state management

2. **Feature Enhancements**
   - Improve synchronization accuracy
   - Enhance user feedback during operations
   - Add more configuration options

3. **Performance Optimization**
   - Review resource usage
   - Optimize polling intervals
   - Reduce unnecessary operations

### Long-term Goals

1. **Expanded Integration**
   - Support for additional aircraft types
   - Integration with other ground service add-ons
   - Enhanced ACARS functionality

2. **User Experience Improvements**
   - More detailed status information
   - Visual feedback for service operations
   - Improved configuration UI

3. **Robustness Enhancements**
   - Better handling of connection failures
   - Automatic recovery from errors
   - Comprehensive logging and diagnostics

## Active Decisions

### Decision Points

1. **Architecture Approach**
   - Continue with the current modular architecture
   - Maintain clear separation of concerns
   - Preserve the state machine pattern for flight phases

2. **Integration Strategy**
   - Focus on stability of existing integrations
   - Ensure reliable operation with current versions
   - Maintain compatibility with future updates

3. **Feature Prioritization**
   - Prioritize stability and reliability over new features
   - Focus on improving existing functionality
   - Address user-reported issues first

### Open Questions

1. **Version Compatibility**
   - How will the application handle different versions of ProsimA320?
   - What is the compatibility strategy for MSFS2020 updates?
   - How to manage GSX Pro version changes?

2. **Error Handling**
   - What is the strategy for handling connection failures?
   - How should the application recover from unexpected states?
   - What level of logging is appropriate for troubleshooting?

3. **User Experience**
   - Is the current UI sufficient for user needs?
   - Are there additional configuration options needed?
   - How can the application provide better feedback during operations?

## Current Challenges

1. **Integration Stability**
   - Maintaining reliable connections to external systems
   - Handling unexpected behavior from ProsimA320 or GSX
   - Managing state synchronization across systems

2. **Configuration Complexity**
   - Balancing flexibility with usability
   - Providing appropriate defaults
   - Explaining complex options to users

3. **Testing Limitations**
   - Difficulty in automated testing due to external dependencies
   - Need for manual testing with actual flight simulator
   - Variability in user environments and configurations
