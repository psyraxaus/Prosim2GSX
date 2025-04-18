# Active Context: Prosim2GSX

## Current Focus
The current focus has been on simplifying the loadsheet generation process by removing redundant custom weight and balance calculations and fully relying on Prosim's native loadsheet functionality. This change removes unnecessary complexity and potential points of failure in the code. The redundant variables and methods related to custom weight and balance calculations have been removed from GsxController.cs and ProsimController.cs, while keeping the enhanced error handling, server status checking, and retry logic in ProsimLoadsheetService.cs.

Prior to this simplification, the focus was on enhancing error handling for loadsheet generation and fixing issues with deboarding state handling. We significantly improved the error diagnostics and recovery capabilities for loadsheet generation by implementing server status checking, detailed HTTP status code interpretation, and better retry logic. We also fixed an issue where deboarding was being called prematurely during departure by improving the state handling logic.

Prior to these enhancements, the focus was on fixing an application crash that occurred when the default SimBrief ID was set to 0. This issue was causing the application to crash in KernelBase.dll after displaying a message box and switching to the Settings tab. The fix involved removing the redundant SimbriefIdRequiredEvent system and implementing a more robust first-time setup dialog that validates the SimBrief ID at application startup.

Prior to these CG calculation enhancements, the focus was on updating the user interface to a new Electronic Flight Bag (EFB) look, providing a more modern and intuitive interface for users. This UI redesign improves the visual appearance and usability of the application while maintaining all the existing functionality. The new EFB-style interface follows design patterns common in modern aviation applications, making the tool more familiar to pilots who use similar interfaces in their flight operations.

Before the UI update, the focus was on implementing an event aggregator system to improve the UI responsiveness and decouple components in the application. This implementation follows the publisher-subscriber pattern, allowing different parts of the application to communicate without direct dependencies. The event aggregator system enables real-time updates to the UI when monitored items change, such as service statuses, connection states, and flight phases.

Previous work also focused on implementing a new Prosim dataref subscription system and enhancing the cockpit door integration between Prosim and GSX. The dataref subscription system involved creating a callback-based monitoring system for Prosim datarefs and implementing synchronization between the cockpit door state in Prosim and the corresponding LVAR in GSX. The implementation allows the cockpit door to muffle cabin sounds when closed, enhancing the realism of the simulation. Additionally, the previous work on cargo door logic, catering service door operation, and refueling process enhancements has been thoroughly tested and verified.

## Recent Changes
- Fixed VoiceMeeter channel control issue:
  - Fixed namespace conflict with LogLevel enum in ServiceModel.cs by using fully qualified name (Prosim2GSX.LogLevel)
  - Updated VoicemeeterRemote64.dll to a newer version for better compatibility across different systems
  - Enhanced error handling for VoiceMeeter API initialization
  - Improved logging for VoiceMeeter operations to aid in debugging

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
- Fully relying on Prosim's native loadsheet functionality instead of custom calculations
- Removing redundant variables and methods to simplify the codebase
- Keeping the enhanced error handling, server status checking, and retry logic for loadsheet generation
- Maintaining the improved deboarding state handling to prevent premature deboarding during departure
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
1. Test the simplified loadsheet generation process with various flight scenarios
2. Monitor for any issues with Prosim's native loadsheet functionality
3. Consider adding more detailed logging for loadsheet generation to aid in troubleshooting
4. Explore potential improvements to error handling for edge cases
5. Update documentation to reflect the simplified approach to loadsheet generation
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
