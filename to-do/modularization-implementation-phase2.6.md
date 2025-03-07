# Modularization Implementation: Phase 2.6 - ProsimCargoService

## Overview

This document details the implementation of Phase 2.6 of the Prosim2GSX modularization strategy, which focuses on extracting cargo-related functionality from the ProsimController into a dedicated ProsimCargoService.

## Implementation Details

### 1. Created Interface Definition

Created `IProsimCargoService.cs` with the following key components:

- **Properties**:
  - `CargoPlanned`: Gets the planned cargo amount
  - `CargoCurrentPercentage`: Gets the current cargo amount as a percentage of planned

- **Methods**:
  - `UpdateFromFlightPlan`: Updates cargo data from a flight plan
  - `ChangeCargo`: Changes the cargo amount to the specified percentage of the planned amount
  - `GetCargoPlanned`: Gets the planned cargo amount
  - `GetCargoCurrentPercentage`: Gets the current cargo percentage

- **Events**:
  - `CargoStateChanged`: Raised when cargo state changes

### 2. Created Service Implementation

Implemented `ProsimCargoService.cs` with the following key features:

- **Private Fields**:
  - `_cargoPlanned`: Stores the total planned cargo amount
  - `_cargoCurrentPercentage`: Tracks the current cargo percentage
  - `_cargoDistMain` and `_cargoDistBulk`: Constants for cargo distribution ratios

- **Key Methods**:
  - `UpdateFromFlightPlan`: Sets the planned cargo amount and optionally initializes the current cargo
  - `ChangeCargo`: Updates the cargo amount in ProSim based on a percentage of the planned amount
  - Event notification for cargo state changes

### 3. Updated ProsimController

Modified `ProsimController.cs` to use the new ProsimCargoService:

- Added a private field for the cargo service
- Initialized the cargo service in the constructor
- Updated the `Update` method to use the cargo service
- Removed the `ChangeCargo` method and updated calls to it
- Updated the boarding and deboarding methods to use the cargo service directly
- Removed the cargo-related fields that are no longer needed

## Benefits

1. **Improved Separation of Concerns**:
   - Cargo operations are now isolated in a dedicated service
   - ProsimController is simplified and more focused

2. **Enhanced Testability**:
   - Cargo operations can be tested independently
   - Mock implementations can be used for testing

3. **Better Maintainability**:
   - Changes to cargo handling won't affect other parts of the system
   - Cargo-related bugs can be isolated and fixed more easily

4. **Clearer API**:
   - Well-defined interface for cargo operations
   - Event-based notification for cargo state changes

## Testing Considerations

While unit tests are not implemented at this stage, the following testing approach is recommended:

1. **Manual Testing**:
   - Test cargo loading during boarding process
   - Test cargo unloading during deboarding process
   - Verify cargo distribution between forward and aft compartments
   - Test with different cargo amounts
   - Test with zero cargo

2. **Integration Testing**:
   - Verify interaction with ProsimPassengerService
   - Test cargo synchronization with GSX
   - Verify cargo state is preserved correctly during flight phases

## Next Steps

1. Proceed to Phase 2.7: ProsimFuelService
2. Implement unit tests for ProsimCargoService (see unit-testing-strategy.md)
