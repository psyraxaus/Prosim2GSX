# Prosim2GSX Modularization Implementation - Phase 2.1 AcarsService

## Overview

This document details the implementation of Phase 2.1 of the Prosim2GSX modularization strategy, which focused on extracting ACARS-related functionality into a dedicated service.

## Implementation Details

### 1. Created Service Interface

Created `IAcarsService.cs` in the Services folder with the following interface:

```csharp
public interface IAcarsService
{
    string Callsign { get; set; }
    string FlightCallsignToOpsCallsign(string flightNumber);
    void Initialize(string flightNumber);
    Task SendMessageToAcars(string flightNumber, string messageType, string message);
    Task SendPreliminaryLoadsheetAsync(string flightNumber, Tuple<string, string, string, string, string, string, string, string, double, double, double, double, double, double, int, int, double, double, int, int, int, double> prelimLoadedData);
    Task SendFinalLoadsheetAsync(string flightNumber, Tuple<string, string, string, string, string, string, string, string, double, double, double, double, double, double, int, int, double, double, int, int, int, double> finalLoadedData, (double prelimZfw, double prelimTow, int prelimPax, double prelimMacZfw, double prelimMacTow, double prelimFuel) prelimData);
}
```

### 2. Implemented Service Class

Created `AcarsService.cs` in the Services folder that:

- Implements the `IAcarsService` interface
- Encapsulates all ACARS-related functionality
- Handles loadsheet formatting and differences detection
- Manages communication with the ACARS system
- Contains helper methods for generating random names and license numbers

Key methods implemented:

- `FlightCallsignToOpsCallsign`: Converts flight callsigns to operations callsigns
- `SendMessageToAcars`: Sends messages to the ACARS system
- `SendPreliminaryLoadsheetAsync`: Formats and sends preliminary loadsheets
- `SendFinalLoadsheetAsync`: Formats and sends final loadsheets with difference detection
- Helper methods for formatting loadsheets and generating random data

### 3. Updated GsxController

Modified `GsxController.cs` to:

- Accept an `IAcarsService` through dependency injection in the constructor
- Remove the direct dependency on `AcarsClient`
- Use the service for all ACARS-related operations
- Remove duplicated code that's now in the service

Key changes:

- Added `IAcarsService` field
- Updated constructor to accept and store the service
- Replaced direct ACARS operations with service calls
- Removed methods that were moved to the service

### 4. Updated ServiceController

Modified `ServiceController.cs` to:

- Create an instance of `AcarsService`
- Pass it to the `GsxController` constructor
- Added the necessary using directive for the Services namespace

## Benefits Achieved

1. **Improved Separation of Concerns**
   - ACARS functionality is now isolated in a dedicated service
   - GsxController is no longer responsible for ACARS-specific logic

2. **Enhanced Testability**
   - The service can be mocked for testing GsxController
   - ACARS functionality can be tested independently

3. **Better Maintainability**
   - Changes to ACARS functionality only require updates to the service
   - The interface provides a clear contract for interactions

4. **Reduced Code Duplication**
   - Common ACARS-related code is centralized in the service
   - Formatting and helper methods are reused across different operations

## Next Steps

The successful implementation of Phase 2.1 provides a foundation for the remaining modularization phases. The next phases should follow a similar pattern of:

1. Defining clear interfaces
2. Implementing services with focused responsibilities
3. Updating existing code to use the new services
4. Testing to ensure functionality is maintained

The next phase to implement is Phase 2.2 FlightPlanService, which will extract flight plan loading and parsing logic from the FlightPlan class.
