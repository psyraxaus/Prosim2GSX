# Active Context: Prosim2GSX

## Current Focus
The current focus has been on enhancing service architecture with improved state management across multiple services. We've implemented a comprehensive state tracking pattern, added thread synchronization with dedicated lock objects, created service-specific log categories for better filtering, and improved error handling and recovery mechanisms. These changes have significantly improved the reliability and maintainability of the application.

Prior to these architectural improvements, the focus was on simplifying the loadsheet generation process by removing redundant custom weight and balance calculations and fully relying on Prosim's native loadsheet functionality. This change removed unnecessary complexity and potential points of failure in the code. The redundant variables and methods related to custom weight and balance calculations were removed from GsxController.cs and ProsimController.cs, while keeping the enhanced error handling, server status checking, and retry logic in ProsimLoadsheetService.cs.

The loadsheet generation process now follows a more robust workflow:
1. First checking if the Prosim EFB server is available via a health check
2. Only proceeding with loadsheet generation if the server is confirmed to be running
3. Using proper HTTP request handling with timeout management
4. Implementing exponential backoff for retries
5. Detailed error reporting with HTTP status code interpretation
6. Proper thread synchronization to prevent multiple simultaneous generation attempts
7. Tracking of loadsheet states (NotStarted, Generating, Completed, Failed)

Prior to this simplification, the focus was on enhancing error handling for loadsheet generation and fixing issues with deboarding state handling. We significantly improved the error diagnostics and recovery capabilities for loadsheet generation by implementing server status checking, detailed HTTP status code interpretation, and better retry logic. We also fixed an issue where deboarding was being called prematurely during departure by improving the state handling logic.

Prior to these enhancements, the focus was on fixing an application crash that occurred when the default SimBrief ID was set to 0. This issue was causing the application to crash in KernelBase.dll after displaying a message box and switching to the Settings tab. The fix involved removing the redundant SimbriefIdRequiredEvent system and implementing a more robust first-time setup dialog that validates the SimBrief ID at application startup.

Prior to these CG calculation enhancements, the focus was on updating the user interface to a new Electronic Flight Bag (EFB) look, providing a more modern and intuitive interface for users. This UI redesign improves the visual appearance and usability of the application while maintaining all the existing functionality. The new EFB-style interface follows design patterns common in modern aviation applications, making the tool more familiar to pilots who use similar interfaces in their flight operations.

Before the UI update, the focus was on implementing an event aggregator system to improve the UI responsiveness and decouple components in the application. This implementation follows the publisher-subscriber pattern, allowing different parts of the application to communicate without direct dependencies. The event aggregator system enables real-time updates to the UI when monitored items change, such as service statuses, connection states, and flight phases.

Previous work also focused on implementing a new Prosim dataref subscription system and enhancing the cockpit door integration between Prosim and GSX. The dataref subscription system involved creating a callback-based monitoring system for Prosim datarefs and implementing synchronization between the cockpit door state in Prosim and the corresponding LVAR in GSX. The implementation allows the cockpit door to muffle cabin sounds when closed, enhancing the realism of the simulation. Additionally, the previous work on cargo door logic, catering service door operation, and refueling process enhancements has been thoroughly tested and verified.

## Recent Changes
- Refactored logging system to use standard .NET ILogger interfaces:
  - Resolved circular dependency between ILoggerFactory and UiLogListener
  - Added RegisterService<T>() method to ServiceLocator for manual service registration
  - Fixed ambiguous references between Microsoft.Extensions.Logging.ILogger and Serilog.ILogger
  - Eliminated duplicate log entries by simplifying logging configuration
  - Updated AudioService to handle null SimConnect references for improved startup reliability
  - Enhanced logging initialization to follow proper initialization sequence
  - This prepares the groundwork for extracting ProsimService to a standalone library

- Implemented comprehensive thread-safe UI updates:
  - Added `ExecuteOnUIThread` helper method to ViewModelBase for marshaling operations to the UI thread
  - Updated EventAggregator to ensure events are published on the UI thread
  - Fixed double dispatching in ConnectionStatusViewModel
  - Updated LogMessagesViewModel to use thread-safe update pattern
  - Ensured all ViewModels that handle events from background threads use thread-safe patterns
  - Improved thread safety for event handlers in ViewModels with background operations
  - Fixed thread safety issues in AudioSettingsViewModel for async operations
  - This resolves UI crashes that could occur during "check of the chocks" stage when UI updates happened on background threads

- Implemented comprehensive thread-safe loadsheet generation:
  - Added proper synchronization with dedicated lock objects to prevent race conditions
  - Implemented state tracking flags for better process control
  - Added safeguards against concurrent operations with proper lock usage
  - Ensured consistent state with `finally` blocks
  - Renamed flags to accurately reflect their purpose
  - Added properties to expose status via interfaces
  - Focused services purely on their core responsibilities
  - Improved logging with specific categories for better filtering

- Enhanced refueling system architecture:
  - Implemented clear state machine for refueling process (requested, active, paused, completed)
  - Added proper state tracking with boolean flags and public properties
  - Improved fuel hose connection/disconnection handling
  - Created dedicated callback for fuel hose state changes
  - Implemented pause/resume functionality based on fuel hose state
  - Enhanced logging with refueling-specific log categories
  - Separated responsibilities between GSX and Prosim refueling services
  - Added explicit refueling state verification

- Optimized GSX menu service for reliability:
  - Improved menu waiting mechanism with proper timeout
  - Added small delay after menu selections for better synchronization
  - Enhanced error handling for menu file operations
  - Improved logging with menu-specific log category
  - Added proper handling for operator selection scenarios
  - Enhanced error detection and recovery for menu operations
  - Implemented robust menu state verification

- Fixed VoiceMeeter channel control issue:
  - Fixed namespace conflict with LogLevel enum in ServiceModel.cs by using fully qualified name (Prosim2GSX.LogLevel)
  - Updated VoicemeeterRemote64.dll to a newer version for better compatibility across different systems
  - Enhanced error handling for VoiceMeeter API initialization
  - Improved logging for VoiceMeeter operations to aid in debugging
  - Implemented a more flexible audio channel configuration system that supports both strips and buses in VoiceMeeter
  - Added UI for selecting VoiceMeeter strips/buses with dynamic loading of available options
  - Added VoiceMeeter diagnostics functionality to help troubleshoot audio issues

- Simplified loadsheet generation by removing redundant custom weight and balance calculations:
  - Removed redundant variables from GsxController.cs (finalMacTow, finalMacZfw, prelimMacTow, prelimMacZfw, finalTow, finalZfw, prelimTow, prelimZfw, macZfw)
  - Removed custom weight and balance calculation methods from ProsimController.cs
  - Kept the enhanced error handling, server status checking, and retry logic in ProsimLoadsheetService.cs
  - Fully relying on Prosim's native loadsheet functionality for more reliable operation
  - Simplified the codebase by removing unnecessary complexity
  - Reduced potential points of failure in the loadsheet generation process

- Enhanced VoiceMeeter integration for audio control:
  - Fixed issues with VHF2, VHF3, CAB, and PA channels not controlling VoiceMeeter
  - Modified the IsXControllable() methods to work with VoiceMeeter even with empty process names
  - Fixed an issue where the code was trying to set read-only _REC datarefs
  - Improved the UI for selecting VoiceMeeter strips and buses
  - Added proper error handling for VoiceMeeter operations
  - Enhanced the synchronization between Prosim datarefs and VoiceMeeter parameters

- Enhanced the center of gravity (CG) calculations for more accurate loadsheet data:
  - Implemented a dedicated A320WeightAndBalance calculator class
  - Used proper A320 reference values for MAC calculations
  - Improved the comparison between preliminary and final loadsheet values
  - Added tolerance-based detection of significant changes (0.5% for MAC values)
  - Enhanced the loadsheet formatting with proper marking of changes
  - Implemented sophisticated weight distribution across cabin zones, cargo compartments, and fuel tanks
  - Integrated the CG calculations with Prosim and GSX for accurate data synchronization
  - Improved the preliminary and final loadsheet generation process

- Implemented a dynamic airline theming system:
  - Created a Theme class structure to represent theme data
  - Implemented a ThemeManager class to handle loading and applying themes
  - Added ThemeResources.xaml with default theme resources
  - Created JSON theme files for Light, Dark, Qantas, Delta, Lufthansa, and Finnair
  - Fixed color parsing issues in theme files by implementing a hex-to-color conversion system
  - Added a theme selection UI in the Settings tab
  - Implemented theme refresh functionality
  - Made the UI fully themeable with dynamic resources
  - Updated the project file to copy theme files to the output directory

- Fixed an issue with MSFS connection status not showing correctly in the UI:
  - Added code to re-publish connection status events in ServiceController.ServiceLoop() method
  - This ensures that UI indicators are properly updated when the service loop starts
  - Fixed a timing issue where events were published before the UI had subscribed to them

- Updated the UI to a new Electronic Flight Bag (EFB) look:
  - Redesigned the main window with a modern blue header bar
  - Added a date display in the header
  - Implemented navigation icons in the header (settings and help)
  - Reorganized the content into a tabbed interface with "FLIGHT STATUS" and "SETTINGS" tabs
  - Created modern styles for all UI elements (buttons, labels, checkboxes, etc.)
  - Implemented a visual flight phase progress bar
  - Redesigned the connection status indicators with colored circles
  - Improved the ground services status display with clear visual indicators
  - Enhanced the log messages area with better formatting
  - Reorganized settings into logical categories with clear headers
  - Improved the overall spacing and alignment for better readability
  - Added rounded corners and modern styling to all UI elements
  - Updated the window title to "Prosim2GSX EFB"
- Updated the MainWindow.xaml.cs code to support the new UI:
  - Added handlers for the new navigation buttons
  - Implemented the flight phase progress bar highlighting
  - Updated the event handlers to work with the new UI elements
  - Added proper cleanup of event subscriptions in the window closing handler
- Previous changes:
  - Removed redundant functions from MainWindow.xaml.cs that are now replaced by the event aggregator system
  - Implemented an event aggregator system using the publisher-subscriber pattern
  - Created a base event class (EventBase) for all events in the system
  - Implemented an event aggregator interface (IEventAggregator) with publish/subscribe methods
  - Created a subscription token system for managing event subscriptions
  - Implemented a singleton event aggregator with thread-safe operations
  - Created specific event types for different aspects of the application
  - Modified GsxController to publish events when status changes occur
  - Added a new UpdateGroundServicesStatus method to regularly check and publish status changes
  - Updated MainWindow to subscribe to events and update UI elements in response
  - Implemented thread-safe event handling with Dispatcher.Invoke for UI updates
  - Added proper event subscription cleanup in the MainWindow's closing handler
  - Decoupled the UI update logic from the service controllers
  - Implemented a comprehensive dataref subscription system in ProsimController
  - Added a callback-based pattern for handling Prosim dataref value changes
  - Implemented cockpit door state synchronization between Prosim and GSX
  - Added functionality to update the GSX FSDT_GSX_COCKPIT_DOOR_OPEN LVAR based on cockpit door state
  - Implemented sound muffling effect when the cockpit door is closed
  - Added robust error handling in the dataref monitoring system
  - Enhanced the cargo door logic with automatic operation based on loading status and catering state
  - Implemented dedicated door operation methods in GSXController
  - Added constants for different service states
  - Enhanced the refueling process with fuel hose state management via callbacks
  - Implemented pause/resume functionality for refueling based on fuel hose connection state
  - Implemented sophisticated center of gravity calculation methods for accurate MACZFW and MACTOW values
  - Added temporary fuel tank manipulation to get precise CG readings from Prosim
  - Implemented proper fuel distribution logic for MACTOW calculations based on A320 fuel loading patterns
  - Added safeguards to restore original fuel states after CG calculations
  - Added better fuel target calculation with rounding to nearest 100
  - Implemented a callback-based system for LVAR value changes in MobiSimConnect.cs
  - Added a dictionary-based approach to map service toggle LVAR names to door operations
  - Enhanced cargo loading integration with automatic door operation
  - Improved catering state management with dedicated callbacks
  - Added better error handling for LVAR callbacks
  - Added automatic cargo door closing when cargo loading reaches 100%
  - Previously: Migrated the application from .NET 7 to .NET 8
  - Previously: Updated NuGet packages to their latest versions compatible with .NET 8
  - Previously: Updated the application version from 0.3.0 to 0.4.0

## Active Decisions
- Using standard .NET ILogger interfaces throughout the application for better maintainability
- Allowing AudioService to initialize with null SimConnect to improve application startup reliability
- Simplifying logging configuration to eliminate duplicate log entries
- Providing manual service registration in ServiceLocator to resolve circular dependencies
- Using ExecuteOnUIThread pattern for all ViewModels that receive updates from background threads
- Ensuring EventAggregator dispatches events to UI thread for thread-safe handling
- Centralizing thread marshaling logic in the ViewModelBase class for consistency
- Ensuring async operations that update UI elements properly marshal back to the UI thread
- Using comprehensive state tracking with boolean flags and public properties
- Implementing service-specific log categories for better troubleshooting
- Ensuring thread safety with proper synchronization primitives
- Maintaining state consistency with finally blocks and clear state transitions
- Using callback patterns for event-driven state changes
- Implementing explicit service status properties for better integration
- Focusing services on their core responsibilities with clear separation of concerns
- Fully relying on Prosim's native loadsheet functionality instead of custom calculations
- Removing redundant variables and methods to simplify the codebase
- Keeping the enhanced error handling, server status checking, and retry logic for loadsheet generation
- Maintaining the improved deboarding state handling to prevent premature deboarding during departure
- Implementing a state-machine approach for flight phase management with clear transitions
- Using an event-driven architecture for UI updates to improve responsiveness
- Separating service interfaces from implementations to improve testability and maintainability
- Using a dictionary-based approach for service toggle handling to improve code organization
- Implementing thread-safe event handling with proper subscription management
- Using VoiceMeeter API for advanced audio control beyond Windows Core Audio capabilities
- Supporting both Core Audio and VoiceMeeter APIs for flexibility
- Making channels controllable with VoiceMeeter even when process names are empty
- Only reading _REC datarefs, not attempting to set them
- Implementing an event aggregator system to decouple components and improve UI responsiveness
- Using a publisher-subscriber pattern for event-based communication
- Creating a thread-safe singleton implementation of the event aggregator
- Using a token-based subscription system for managing event subscriptions
- Implementing specific event types for different aspects of the application
- Using Dispatcher.Invoke for thread-safe UI updates from event handlers
- Implementing proper event subscription cleanup to prevent memory leaks
- Implementing a comprehensive dataref subscription system for Prosim to improve integration capabilities
- Creating a thread-safe monitoring system with proper lifecycle management
- Supporting multiple handlers per dataref to enable flexible event handling
- Implementing cockpit door state synchronization to enhance realism with sound muffling
- Implementing a callback pattern for LVAR changes to improve responsiveness
- Using a dictionary-based approach for service toggle handling to improve code organization
- Enhancing door operation logic based on service states (waiting, finished, completed)
- Implementing automatic door operations for catering and cargo services
- Implementing fuel hose state management to improve refueling realism
- Using constants for service states to improve code readability and maintainability
- Previously: Choosing to update to .NET 8 for improved performance and extended support

## Current Challenges
- Ensuring proper integration with Prosim's native loadsheet functionality
- Handling potential errors or edge cases in the loadsheet generation process
- Maintaining compatibility with future updates to Prosim's loadsheet system
- Ensuring proper error handling and recovery for loadsheet generation failures
- Managing the lifecycle of event subscriptions to prevent memory leaks
- Balancing event publishing frequency with performance considerations
- Coordinating the timing of door operations with GSX service states
- Handling edge cases in the refueling process (disconnection, reconnection)
- Ensuring accurate synchronization between Prosim and GSX Pro during all flight phases
- Managing the complexity of the state machine transitions, especially during abnormal scenarios
- Ensuring proper cleanup of resources when components are disposed
- Ensuring the event aggregator system handles all edge cases properly
- Managing the lifecycle of event subscriptions to prevent memory leaks
- Balancing event publishing frequency with performance considerations
- Ensuring thread safety in the event aggregator system
- Handling potential exceptions in event handlers without affecting the main application
- Ensuring proper cleanup of event subscriptions when components are disposed
- Coordinating the timing of UI updates with event publishing
- Ensuring the dataref subscription system handles all edge cases properly
- Managing the lifecycle of dataref monitors to prevent memory leaks
- Balancing monitoring frequency with performance considerations
- Ensuring thread safety in the dataref monitoring system
- Handling potential exceptions in dataref callbacks without affecting the main application
- Ensuring the callback system handles all edge cases properly
- Managing the lifecycle of callbacks to prevent memory leaks
- Coordinating the timing of door operations with GSX service states
- Ensuring accurate fuel synchronization between GSX and Prosim
- Handling edge cases in the refueling process (disconnection, reconnection)
- Ensuring proper door operation timing based on catering and cargo loading states
- Testing the automatic door operations with various service scenarios

## Next Steps
1. Test the enhanced service architecture with various flight scenarios
2. Monitor for any issues with the improved state management system
3. Consider adding more detailed logging for service state transitions
4. Explore potential improvements to error handling for edge cases
5. Update documentation to reflect the improved service architecture
6. Consider implementing automated testing for core components
7. Explore extending automation to cover push-back, de-ice, and gate selection services
8. Implement performance metrics to monitor service response times
9. Enhance the event filtering system to reduce unnecessary UI updates
10. Optimize the monitoring interval for different types of datarefs based on criticality
6. Extend the event aggregator system to cover more aspects of the application
7. Implement additional event types for other state changes in the system
8. Optimize event publishing frequency for different types of events
9. Consider implementing event filtering to reduce unnecessary UI updates
10. Evaluate the performance impact of the event aggregator system under heavy load
11. Identify additional Prosim datarefs that could benefit from the subscription system
12. Explore extending the dataref subscription pattern to other simulation variables
13. Optimize the monitoring interval for different types of datarefs
14. Consider implementing priority levels for different dataref monitors
15. Optimize performance of the callback system
16. Document the new CG calculation system and weight and balance calculator for future development
17. Explore potential improvements to error handling for edge cases
18. Consider adding more configuration options for door operation behavior
19. Evaluate performance impact of the dataref monitoring system under heavy load

## Open Questions
- What are the most common issues users encounter?
- Are there specific areas of the integration that need improvement?
- What are the current development priorities?
- Are there planned features not yet implemented?
- How well does the system handle edge cases and error conditions?

## Current State Assessment
The project appears to be a functional integration tool that successfully bridges Prosim A320 and GSX Pro. The architecture seems well-structured with clear separation of concerns and appropriate use of design patterns. The documentation provides a good overview of the system's purpose and functionality.

## Development Environment
The development environment now requires .NET 8 SDK for building the application. The project structure follows standard .NET conventions with appropriate organization of components.

## User Feedback
No specific user feedback has been documented yet. This section will be updated as feedback is received and analyzed.

## Integration Status
The integration between Prosim A320 and GSX Pro appears to be working as described in the README. The system handles various ground service operations and synchronizes state between the two systems.

## Documentation Status
Initial documentation has been created in the memory bank. This will need to be refined and expanded as more information becomes available and as the project evolves.
