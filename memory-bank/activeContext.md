# Active Context: Prosim2GSX

## Current Focus
The current focus has been on enhancing the center of gravity (CG) calculations for more accurate loadsheet data, as well as implementing a new Prosim dataref subscription system and enhancing the cockpit door integration between Prosim and GSX. The CG calculation improvements involve sophisticated methods to accurately determine the Zero Fuel Weight Center of Gravity (MACZFW) and Take Off Weight Center of Gravity (MACTOW) by temporarily manipulating fuel states and reading values directly from Prosim. The dataref subscription system involved creating a callback-based monitoring system for Prosim datarefs and implementing synchronization between the cockpit door state in Prosim and the corresponding LVAR in GSX. The implementation allows the cockpit door to muffle cabin sounds when closed, enhancing the realism of the simulation. Additionally, the previous work on cargo door logic, catering service door operation, and refueling process enhancements has been thoroughly tested and verified.

## Recent Changes
- Implemented a comprehensive dataref subscription system in ProsimController for monitoring Prosim dataref changes
- Added a callback-based pattern for handling Prosim dataref value changes
- Implemented cockpit door state synchronization between Prosim and GSX
- Added functionality to update the GSX FSDT_GSX_COCKPIT_DOOR_OPEN LVAR based on cockpit door state
- Implemented sound muffling effect when the cockpit door is closed
- Added robust error handling in the dataref monitoring system
- Completed successful testing of the new dataref subscription system with the cockpit door switch
- Completed thorough testing of the enhanced refueling process, confirming proper functionality
- Completed verification of fuel synchronization between GSX and Prosim
- Enhanced the cargo door logic with automatic operation based on loading status and catering state
- Completed thorough testing of cargo door integration with GSX, confirming proper opening and closing behavior
- Completed thorough testing of catering service door operation, verifying correct functionality
- Implemented dedicated door operation methods in GSXController (OperateFrontDoor, OperateAftDoor, OperateFrontCargoDoor, OperateAftCargoDoor)
- Added constants for different service states (GSX_WAITING_STATE, GSX_FINISHED_STATE, GSX_COMPLETED_STATE)
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
- Implementing a comprehensive dataref subscription system for Prosim to improve integration capabilities
- Creating a thread-safe monitoring system with proper lifecycle management
- Supporting multiple handlers per dataref to enable flexible event handling
- Implementing cockpit door state synchronization to enhance realism with sound muffling
- Implementing a callback pattern for LVAR changes to improve responsiveness
- Using a dictionary-based approach for service toggle handling to improve code organization
- Enhancing door operation logic based on service states (waiting, finished, completed)
- Implementing automatic door operations for catering and cargo services
- Implementing fuel hose state management to improve refueling realism
- Implementing sophisticated center of gravity calculation methods with temporary fuel state manipulation
- Using proper A320 fuel loading patterns for accurate MACTOW calculations
- Implementing safeguards to restore original fuel states after CG calculations
- Ensuring accurate CG data for both preliminary and final loadsheets
- Using constants for service states to improve code readability and maintainability
- Previously: Choosing to update to .NET 8 for improved performance and extended support

## Current Challenges
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
1. Identify additional Prosim datarefs that could benefit from the subscription system
2. Explore extending the dataref subscription pattern to other simulation variables
3. Optimize the monitoring interval for different types of datarefs
4. Consider implementing priority levels for different dataref monitors
5. Evaluate the accuracy of center of gravity calculations with additional aircraft loading scenarios
6. Optimize performance of the callback system
7. Document the new dataref subscription system and cockpit door integration for future development
8. Explore potential improvements to error handling for edge cases
9. Consider adding more configuration options for door operation behavior
10. Evaluate performance impact of the dataref monitoring system under heavy load

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
