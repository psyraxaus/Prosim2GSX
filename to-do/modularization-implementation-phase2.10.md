# Prosim2GSX Modularization Implementation - Phase 2.10: Create Shared Service Interfaces

## Overview

This document outlines the implementation of Phase 2.10 of the Prosim2GSX modularization strategy, which focuses on creating shared service interfaces for passenger, cargo, and fuel management. These interfaces provide a common abstraction layer that can be implemented by both ProSim-specific and future GSX-specific services.

## Implementation Details

### 1. IPassengerService Interface

Created a new interface file `IPassengerService.cs` in the `Services` folder with the following key features:

- **Properties**:
  - `PassengersPlanned`: Gets the planned number of passengers
  - `PassengersCurrent`: Gets the current number of passengers

- **Methods**:
  - `UpdateFromFlightPlan`: Updates passenger data from a flight plan
  - `BoardingStart`: Starts the boarding process
  - `Boarding`: Processes boarding
  - `BoardingStop`: Stops the boarding process
  - `DeboardingStart`: Starts the deboarding process
  - `Deboarding`: Processes deboarding
  - `DeboardingStop`: Stops the deboarding process

- **Events**:
  - `PassengerStateChanged`: Raised when passenger state changes

- **Event Arguments**:
  - `PassengerStateChangedEventArgs`: Contains information about passenger state changes

### 2. ICargoService Interface

Created a new interface file `ICargoService.cs` in the `Services` folder with the following key features:

- **Properties**:
  - `CargoPlanned`: Gets the planned cargo amount
  - `CargoCurrentPercentage`: Gets the current cargo amount as a percentage of planned

- **Methods**:
  - `UpdateFromFlightPlan`: Updates cargo data from a flight plan
  - `LoadingStart`: Starts the cargo loading process
  - `Loading`: Processes cargo loading
  - `LoadingStop`: Stops the cargo loading process
  - `UnloadingStart`: Starts the cargo unloading process
  - `Unloading`: Processes cargo unloading
  - `UnloadingStop`: Stops the cargo unloading process
  - `ChangeCargo`: Changes the cargo amount to the specified percentage of the planned amount

- **Events**:
  - `CargoStateChanged`: Raised when cargo state changes

- **Event Arguments**:
  - `CargoStateChangedEventArgs`: Contains information about cargo state changes

### 3. IFuelService Interface

Created a new interface file `IFuelService.cs` in the `Services` folder with the following key features:

- **Properties**:
  - `FuelPlanned`: Gets the planned fuel amount in kg
  - `FuelCurrent`: Gets the current fuel amount in kg
  - `FuelUnits`: Gets the fuel units (KG or LBS)

- **Methods**:
  - `UpdateFromFlightPlan`: Updates fuel data from a flight plan
  - `RefuelStart`: Starts the refueling process
  - `Refuel`: Continues the refueling process
  - `RefuelStop`: Stops the refueling process
  - `GetFuelRate`: Gets the fuel rate in kg/s

- **Events**:
  - `FuelStateChanged`: Raised when fuel state changes

- **Event Arguments**:
  - `FuelStateChangedEventArgs`: Contains information about fuel state changes

## Design Considerations

### 1. Interface Design Principles

- **Consistency**: All interfaces follow a similar pattern with properties, methods, and events
- **Simplicity**: Interfaces include only essential operations needed by both ProSim and GSX
- **Abstraction**: Implementation details are hidden behind clear, well-defined interfaces
- **Event-Based Communication**: All interfaces use events to notify subscribers of state changes

### 2. Compatibility with Existing Services

The interfaces were designed to be compatible with the existing ProSim-specific services:

- `IPassengerService` aligns with `IProsimPassengerService`
- `ICargoService` aligns with `IProsimCargoService`
- `IFuelService` aligns with `IProsimFuelService`

This ensures that the existing services can be updated to implement the new interfaces with minimal changes.

### 3. Future GSX Integration

The interfaces were designed with future GSX integration in mind:

- Operations are platform-agnostic
- No ProSim-specific assumptions
- Clear separation between interface and implementation
- Consistent naming conventions

## Benefits

### 1. Improved Abstraction

- Clear separation between interface and implementation
- Platform-agnostic service definitions
- Consistent API across different platforms

### 2. Enhanced Extensibility

- Easier to add support for additional platforms
- Simplified integration of new features
- More flexible architecture

### 3. Better Testability

- Interfaces can be mocked for testing
- Implementation details are hidden behind abstractions
- Consistent testing approach across platforms

### 4. Reduced Coupling

- Controllers can depend on abstractions, not concrete implementations
- Services can be developed and tested independently
- Changes to one platform don't affect others

## Next Steps

### 1. Update Existing ProSim Services

The next phase should focus on updating the existing ProSim services to implement the new shared interfaces:

- Modify `ProsimPassengerService` to implement `IPassengerService`
- Modify `ProsimCargoService` to implement `ICargoService`
- Modify `ProsimFuelService` to implement `IFuelService`

### 2. Prepare for GSX Services

Once the ProSim services have been updated, the focus can shift to creating GSX-specific implementations:

- Create `GSXPassengerService` implementing `IPassengerService`
- Create `GSXCargoService` implementing `ICargoService`
- Create `GSXFuelService` implementing `IFuelService`

### 3. Update Controllers

Finally, the controllers should be updated to use the shared interfaces:

- Modify controllers to use `IPassengerService`, `ICargoService`, and `IFuelService`
- Implement dependency injection to select the appropriate implementation
- Ensure proper coordination between services

## Conclusion

The implementation of shared service interfaces in Phase 2.10 represents a significant step in the modularization of the Prosim2GSX application. By creating these interfaces, we establish a common abstraction layer that will facilitate better integration between ProSim and GSX, improve code organization, and enhance maintainability and testability.

This approach aligns with the overall modularization strategy and sets the stage for the upcoming phases focused on GSX service extraction and state management refinement.
