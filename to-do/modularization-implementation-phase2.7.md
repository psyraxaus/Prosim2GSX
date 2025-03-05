# Phase 2.7: ProsimFuelService Implementation

## Overview

This document outlines the implementation of Phase 2.7 of the Prosim2GSX modularization strategy, which involves creating a dedicated service for fuel-related operations.

## Implementation Details

### 1. Created WeightConversionUtility

Created a utility class for weight conversions to avoid duplicating this logic:

```csharp
namespace Prosim2GSX.Utilities
{
    public static class WeightConversionUtility
    {
        public const float KgToLbsConversionFactor = 2.205f;
        
        public static double KgToLbs(double kg)
        {
            return kg * KgToLbsConversionFactor;
        }
        
        public static double LbsToKg(double lbs)
        {
            return lbs / KgToLbsConversionFactor;
        }
    }
}
```

### 2. Created IProsimFuelService Interface

Created an interface for the fuel service:

```csharp
public interface IProsimFuelService
{
    event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;
    double FuelPlanned { get; }
    double FuelCurrent { get; }
    string FuelUnits { get; }
    void SetInitialFuel();
    void RefuelStart();
    bool Refuel();
    void RefuelStop();
    double GetFuelAmount();
    float GetFuelRateKGS();
    void UpdateFromFlightPlan(double plannedFuel, bool forceCurrentUpdate = false);
    double GetFuelPlanned();
    double GetFuelCurrent();
}
```

### 3. Created ProsimFuelService Implementation

Implemented the fuel service:

```csharp
public class ProsimFuelService : IProsimFuelService
{
    private readonly IProsimService _prosimService;
    private readonly ServiceModel _model;
    private double _fuelCurrent;
    private double _fuelPlanned;
    private string _fuelUnits;
    
    // Properties and methods implementation
    // ...
}
```

The implementation includes:
- Tracking current and planned fuel amounts
- Converting between kg and lbs as needed
- Setting initial fuel based on configuration
- Managing the refueling process
- Raising events when fuel state changes

### 4. Updated ProsimController

Modified ProsimController to use the new fuel service:

1. Added a field for the fuel service
   ```csharp
   private readonly IProsimFuelService _fuelService;
   ```

2. Initialized the fuel service in the constructor
   ```csharp
   _fuelService = new ProsimFuelService(Interface.ProsimService, Model);
   ```

3. Subscribed to fuel state change events
   ```csharp
   _fuelService.FuelStateChanged += (sender, args) => {
       Logger.Log(LogLevel.Debug, "ProsimController:FuelStateChanged", 
           $"{args.OperationType}: Current: {args.CurrentAmount} {args.FuelUnits}, Planned: {args.PlannedAmount} {args.FuelUnits}");
   };
   ```

4. Updated the Update method to use the fuel service
   ```csharp
   // Update fuel data from flight plan
   _fuelService.UpdateFromFlightPlan(FlightPlan.Fuel, forceCurrent);
   ```

5. Replaced fuel-related methods with calls to the fuel service
   ```csharp
   public double GetFuelPlanned() => _fuelService.GetFuelPlanned();
   public double GetFuelCurrent() => _fuelService.GetFuelCurrent();
   public void SetInitialFuel() => _fuelService.SetInitialFuel();
   public void RefuelStart() => _fuelService.RefuelStart();
   public bool Refuel() => _fuelService.Refuel();
   public void RefuelStop() => _fuelService.RefuelStop();
   public double GetFuelAmount() => _fuelService.GetFuelAmount();
   ```

### 5. Updated ServiceModel

Removed the `GetFuelRateKGS` method from ServiceModel as it's now part of the ProsimFuelService.

## Benefits

1. **Improved Separation of Concerns**: Fuel-related functionality is now encapsulated in a dedicated service.
2. **Enhanced Testability**: The fuel service can be tested in isolation.
3. **Better Code Organization**: Related functionality is grouped together.
4. **Reduced Coupling**: ProsimController no longer directly manages fuel operations.
5. **Consistent Event Handling**: Fuel state changes are communicated through events.

## Testing

The implementation was tested to ensure:

1. Fuel data is correctly loaded from flight plans
2. Initial fuel is set correctly based on configuration
3. Refueling works correctly
4. Fuel amount is reported correctly
5. Events are raised correctly

## Conclusion

The implementation of ProsimFuelService successfully extracts fuel-related functionality from ProsimController and ServiceModel into a dedicated service. This improves code organization, maintainability, and testability.
