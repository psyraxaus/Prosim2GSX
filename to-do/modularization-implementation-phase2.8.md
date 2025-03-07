# Modularization Implementation - Phase 2.8: ProsimFlightDataService

## Overview

This document outlines the implementation of Phase 2.8 of the Prosim2GSX modularization strategy, which focuses on extracting flight data-related functionality from the ProsimController into a dedicated ProsimFlightDataService.

## Implementation Details

### 1. Created Interface Definition

Created `IProsimFlightDataService.cs` interface file with the following methods:

```csharp
public interface IProsimFlightDataService
{
    event EventHandler<FlightDataChangedEventArgs> FlightDataChanged;
    
    (string Time, string Flight, string TailNumber, string DayOfFlight, 
     string DateOfFlight, string Origin, string Destination, 
     double EstZfw, double MaxZfw, double EstTow, double MaxTow, 
     double EstLaw, double MaxLaw, int PaxInfants, int PaxAdults, 
     double MacZfw, double MacTow, int PaxZoneA, int PaxZoneB, 
     int PaxZoneC, double FuelInTanks) GetLoadedData(string loadsheetType);
    
    string GetFMSFlightNumber();
    
    double GetZfwCG();
    
    double GetTowCG();
}
```

Also defined a `FlightDataChangedEventArgs` class for event handling:

```csharp
public class FlightDataChangedEventArgs : EventArgs
{
    public string DataType { get; }
    public object PreviousValue { get; }
    public object CurrentValue { get; }
    
    public FlightDataChangedEventArgs(string dataType, object currentValue, object previousValue = null)
    {
        DataType = dataType;
        CurrentValue = currentValue;
        PreviousValue = previousValue;
    }
}
```

### 2. Created Implementation Class

Created `ProsimFlightDataService.cs` implementation file with the following features:

- Dependency injection for `IProsimService` and `FlightPlan`
- Implementation of all interface methods
- Event-based notification for flight data changes
- Secure XML processing in the `GetFMSFlightNumber` method
- Comprehensive error handling and logging
- Detailed documentation with XML comments

Key implementation details:

1. **GetLoadedData Method**
   - Retrieves comprehensive flight data including weights, passenger counts, and CG values
   - Handles both preliminary and final loadsheet types
   - Raises events for significant data changes

2. **GetFMSFlightNumber Method**
   - Extracts flight number from the FMS XML data
   - Uses secure XML processing with `DtdProcessing.Prohibit` and null `XmlResolver`
   - Includes proper error handling and logging

3. **GetZfwCG Method**
   - Calculates the Zero Fuel Weight Center of Gravity
   - Temporarily sets fuel tanks to zero to get accurate ZFW CG
   - Restores original fuel values after calculation
   - Includes special case handling for specific ZFW values

4. **GetTowCG Method**
   - Calculates the Take Off Weight Center of Gravity
   - Intelligently determines when recalculation is needed
   - Handles various fuel distribution scenarios
   - Properly restores original fuel values after calculation

### 3. Updated ProsimController

Modified `ProsimController.cs` to use the new service:

1. Added a field for the flight data service:
   ```csharp
   private IProsimFlightDataService _flightDataService;
   ```

2. Initialized the service in the `IsProsimConnectionAvailable` method:
   ```csharp
   _flightDataService = new ProsimFlightDataService(Interface.ProsimService, FlightPlan);
   
   _flightDataService.FlightDataChanged += (sender, args) => {
       Logger.Log(LogLevel.Debug, "ProsimController:FlightDataChanged", 
           $"{args.DataType} changed to {args.CurrentValue}");
   };
   ```

3. Updated the following methods to delegate to the service:
   - `GetLoadedData`
   - `GetFMSFlightNumber`
   - `GetZfwCG`
   - `GetTowCG`

## Benefits

1. **Improved Separation of Concerns**
   - Flight data calculations are now isolated in a dedicated service
   - ProsimController becomes more focused on coordination rather than implementation details

2. **Enhanced Testability**
   - Flight data service can be tested independently
   - Mock implementations can be used for testing dependent components

3. **Better Error Handling**
   - Centralized error handling for flight data operations
   - Improved logging and diagnostics

4. **Event-Based Communication**
   - Flight data changes can be communicated via events
   - Components can subscribe to specific flight data changes

5. **Secure XML Processing**
   - Implementation includes secure XML processing practices
   - Prevents XML external entity (XXE) attacks

## Next Steps

1. **Unit Testing**
   - Create unit tests for ProsimFlightDataService
   - Test all methods with various inputs
   - Test event handling

2. **Integration Testing**
   - Test interaction with ProsimController
   - Verify correct behavior in different flight scenarios

3. **Continue Modularization**
   - Proceed with Phase 2.9: ProsimFluidService
   - Continue extracting domain-specific services from ProsimController
