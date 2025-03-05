# Prosim2GSX Modularization Implementation - Phase 2.9: ProsimFluidService

## Overview

This document outlines the implementation plan for Phase 2.9 of the Prosim2GSX modularization strategy, which focuses on extracting hydraulic fluid-related functionality from the ProsimController class into a dedicated ProsimFluidService.

## Implementation Steps

### 1. Create Interface

Create a new interface file `IProsimFluidService.cs` in the `Services` folder with the following content:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for managing hydraulic fluid operations in ProSim
    /// </summary>
    public interface IProsimFluidService
    {
        /// <summary>
        /// Event raised when fluid state changes
        /// </summary>
        event EventHandler<FluidStateChangedEventArgs> FluidStateChanged;
        
        /// <summary>
        /// Gets the current blue hydraulic fluid amount
        /// </summary>
        double BlueFluidAmount { get; }
        
        /// <summary>
        /// Gets the current green hydraulic fluid amount
        /// </summary>
        double GreenFluidAmount { get; }
        
        /// <summary>
        /// Gets the current yellow hydraulic fluid amount
        /// </summary>
        double YellowFluidAmount { get; }
        
        /// <summary>
        /// Sets the initial hydraulic fluid values based on configuration settings
        /// </summary>
        void SetInitialFluids();
        
        /// <summary>
        /// Gets the current hydraulic fluid values and updates the model
        /// </summary>
        /// <returns>A tuple containing the blue, green, and yellow hydraulic fluid amounts</returns>
        (double BlueAmount, double GreenAmount, double YellowAmount) GetHydraulicFluidValues();
    }
    
    /// <summary>
    /// Event arguments for fluid state changes
    /// </summary>
    public class FluidStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current blue hydraulic fluid amount
        /// </summary>
        public double BlueAmount { get; }
        
        /// <summary>
        /// Gets the current green hydraulic fluid amount
        /// </summary>
        public double GreenAmount { get; }
        
        /// <summary>
        /// Gets the current yellow hydraulic fluid amount
        /// </summary>
        public double YellowAmount { get; }
        
        /// <summary>
        /// Creates a new instance of FluidStateChangedEventArgs
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="blueAmount">The current blue hydraulic fluid amount</param>
        /// <param name="greenAmount">The current green hydraulic fluid amount</param>
        /// <param name="yellowAmount">The current yellow hydraulic fluid amount</param>
        public FluidStateChangedEventArgs(string operationType, double blueAmount, double greenAmount, double yellowAmount)
        {
            OperationType = operationType;
            BlueAmount = blueAmount;
            GreenAmount = greenAmount;
            YellowAmount = yellowAmount;
        }
    }
}
```

### 2. Create Implementation

Create a new implementation file `ProsimFluidService.cs` in the `Services` folder with the following content:

```csharp
using System;
using Prosim2GSX.Models;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for managing hydraulic fluid operations in ProSim
    /// </summary>
    public class ProsimFluidService : IProsimFluidService
    {
        private readonly IProsimService _prosimService;
        private readonly ServiceModel _model;
        
        /// <summary>
        /// Gets the current blue hydraulic fluid amount
        /// </summary>
        public double BlueFluidAmount => _model.HydaulicsBlueAmount;
        
        /// <summary>
        /// Gets the current green hydraulic fluid amount
        /// </summary>
        public double GreenFluidAmount => _model.HydaulicsGreenAmount;
        
        /// <summary>
        /// Gets the current yellow hydraulic fluid amount
        /// </summary>
        public double YellowFluidAmount => _model.HydaulicsYellowAmount;
        
        /// <summary>
        /// Event raised when fluid state changes
        /// </summary>
        public event EventHandler<FluidStateChangedEventArgs> FluidStateChanged;
        
        /// <summary>
        /// Creates a new instance of ProsimFluidService
        /// </summary>
        /// <param name="prosimService">The ProSim service to use for communication with ProSim</param>
        /// <param name="model">The service model containing configuration settings</param>
        /// <exception cref="ArgumentNullException">Thrown if prosimService or model is null</exception>
        public ProsimFluidService(IProsimService prosimService, ServiceModel model)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }
        
        /// <summary>
        /// Sets the initial hydraulic fluid values based on configuration settings
        /// </summary>
        public void SetInitialFluids()
        {
            try
            {
                _prosimService.SetVariable("aircraft.hydraulics.blue.quantity", _model.HydaulicsBlueAmount);
                _prosimService.SetVariable("aircraft.hydraulics.green.quantity", _model.HydaulicsGreenAmount);
                _prosimService.SetVariable("aircraft.hydraulics.yellow.quantity", _model.HydaulicsYellowAmount);
                
                OnFluidStateChanged("SetInitialFluids", _model.HydaulicsBlueAmount, _model.HydaulicsGreenAmount, _model.HydaulicsYellowAmount);
                
                Logger.Log(LogLevel.Information, "ProsimFluidService:SetInitialFluids", 
                    $"Set initial hydraulic fluid values - Blue: {_model.HydaulicsBlueAmount}, Green: {_model.HydaulicsGreenAmount}, Yellow: {_model.HydaulicsYellowAmount}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFluidService:SetInitialFluids", 
                    $"Error setting initial hydraulic fluid values: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the current hydraulic fluid values and updates the model
        /// </summary>
        /// <returns>A tuple containing the blue, green, and yellow hydraulic fluid amounts</returns>
        public (double BlueAmount, double GreenAmount, double YellowAmount) GetHydraulicFluidValues()
        {
            try
            {
                // Read current values from ProSim and update the model
                _model.HydaulicsBlueAmount = _prosimService.ReadDataRef("aircraft.hydraulics.blue.quantity");
                _model.HydaulicsGreenAmount = _prosimService.ReadDataRef("aircraft.hydraulics.green.quantity");
                _model.HydaulicsYellowAmount = _prosimService.ReadDataRef("aircraft.hydraulics.yellow.quantity");
                
                OnFluidStateChanged("GetHydraulicFluidValues", _model.HydaulicsBlueAmount, _model.HydaulicsGreenAmount, _model.HydaulicsYellowAmount);
                
                return (_model.HydaulicsBlueAmount, _model.HydaulicsGreenAmount, _model.HydaulicsYellowAmount);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFluidService:GetHydraulicFluidValues", 
                    $"Error getting hydraulic fluid values: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Raises the FluidStateChanged event
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="blueAmount">The current blue hydraulic fluid amount</param>
        /// <param name="greenAmount">The current green hydraulic fluid amount</param>
        /// <param name="yellowAmount">The current yellow hydraulic fluid amount</param>
        protected virtual void OnFluidStateChanged(string operationType, double blueAmount, double greenAmount, double yellowAmount)
        {
            FluidStateChanged?.Invoke(this, new FluidStateChangedEventArgs(operationType, blueAmount, greenAmount, yellowAmount));
        }
    }
}
```

### 3. Update ProsimController

Update the `ProsimController.cs` file to use the new ProsimFluidService:

1. Add a private field for the fluid service:
```csharp
private readonly IProsimFluidService _fluidService;
```

2. Initialize the fluid service in the constructor:
```csharp
// Initialize fluid service with the ProsimService from Interface and the model
_fluidService = new ProsimFluidService(Interface.ProsimService, Model);

// Optionally subscribe to fluid state change events
_fluidService.FluidStateChanged += (sender, args) => {
    // Handle fluid state changes if needed
    Logger.Log(LogLevel.Debug, "ProsimController:FluidStateChanged", 
        $"{args.OperationType}: Blue: {args.BlueAmount}, Green: {args.GreenAmount}, Yellow: {args.YellowAmount}");
};
```

3. Replace the `SetInitialFluids` method with a call to the fluid service:
```csharp
public void SetInitialFluids()
{
    _fluidService.SetInitialFluids();
}
```

4. Replace the `GetHydraulicFluidValues` method with a call to the fluid service:
```csharp
public (double, double, double) GetHydraulicFluidValues()
{
    return _fluidService.GetHydraulicFluidValues();
}
```

## Testing Strategy

After implementing the changes, the following tests should be performed:

1. **Unit Tests**
   - Test the ProsimFluidService constructor with valid and invalid parameters
   - Test the SetInitialFluids method
   - Test the GetHydraulicFluidValues method
   - Test the event raising mechanism

2. **Integration Tests**
   - Test the interaction between ProsimController and ProsimFluidService
   - Verify that hydraulic fluid values are correctly set and retrieved
   - Verify that events are properly raised and handled

3. **Manual Tests**
   - Test the application with ProsimA320 and MSFS2020
   - Verify that hydraulic fluid values are correctly displayed and updated
   - Verify that saved hydraulic fluid values are correctly restored

## Benefits

1. **Improved Separation of Concerns**
   - Hydraulic fluid-related functionality is now isolated in a dedicated service
   - ProsimController is simplified and focused on coordination

2. **Enhanced Testability**
   - ProsimFluidService can be tested in isolation
   - Dependencies are explicit and can be mocked for testing

3. **Better Maintainability**
   - Changes to hydraulic fluid handling only affect ProsimFluidService
   - Code is more organized and easier to understand

4. **Consistent Architecture**
   - Follows the same pattern as other services
   - Maintains the modular architecture of the application

## Conclusion

The implementation of ProsimFluidService completes another step in the modularization of the Prosim2GSX application. This change improves the code organization, maintainability, and testability by extracting hydraulic fluid-related functionality into a dedicated service with a clear interface.
