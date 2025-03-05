# Prosim2GSX Modularization Implementation - Phase 2.4: ProsimEquipmentService

## Overview

This document details the implementation of Phase 2.4 of the Prosim2GSX modularization strategy, which involves extracting equipment-related functionality from the ProsimController into a dedicated ProsimEquipmentService.

## Implementation Details

### 1. Created IProsimEquipmentService Interface

Created a new interface `IProsimEquipmentService` in the Services folder that defines the contract for equipment-related operations:

```csharp
public interface IProsimEquipmentService
{
    event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
    void SetServicePCA(bool enable);
    void SetServiceChocks(bool enable);
    void SetServiceGPU(bool enable);
}
```

The interface includes an event for equipment state changes, allowing subscribers to be notified when equipment states are modified.

### 2. Created ProsimEquipmentService Implementation

Implemented the `ProsimEquipmentService` class that provides the actual functionality:

```csharp
public class ProsimEquipmentService : IProsimEquipmentService
{
    private readonly IProsimService _prosimService;
    
    public event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;
    
    public ProsimEquipmentService(IProsimService prosimService)
    {
        _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
    }
    
    public void SetServicePCA(bool enable)
    {
        _prosimService.SetVariable("groundservice.preconditionedAir", enable);
        OnEquipmentStateChanged("PCA", enable);
    }
    
    public void SetServiceChocks(bool enable)
    {
        _prosimService.SetVariable("efb.chocks", enable);
        OnEquipmentStateChanged("Chocks", enable);
    }
    
    public void SetServiceGPU(bool enable)
    {
        _prosimService.SetVariable("groundservice.groundpower", enable);
        OnEquipmentStateChanged("GPU", enable);
    }
    
    protected virtual void OnEquipmentStateChanged(string equipmentName, bool isEnabled)
    {
        EquipmentStateChanged?.Invoke(this, new EquipmentStateChangedEventArgs(equipmentName, isEnabled));
    }
}
```

The service uses the IProsimService to interact with ProSim and raises events when equipment states change.

### 3. Updated ProsimController

Modified the ProsimController to use the new ProsimEquipmentService:

1. Added a private field for the equipment service:
```csharp
private readonly IProsimEquipmentService _equipmentService;
```

2. Initialized the equipment service in the constructor:
```csharp
_equipmentService = new ProsimEquipmentService(Interface.ProsimService);

// Optionally subscribe to equipment state change events
_equipmentService.EquipmentStateChanged += (sender, args) => {
    // Handle equipment state changes if needed
    Logger.Log(LogLevel.Debug, "ProsimController:EquipmentStateChanged", 
        $"{args.EquipmentName} is now {(args.IsEnabled ? "enabled" : "disabled")}");
};
```

3. Updated the equipment-related methods to use the service:
```csharp
public void SetServicePCA(bool enable)
{
    _equipmentService.SetServicePCA(enable);
}

public void SetServiceChocks(bool enable)
{
    _equipmentService.SetServiceChocks(enable);
}

public void SetServiceGPU(bool enable)
{
    _equipmentService.SetServiceGPU(enable);
}
```

## Benefits

1. **Improved Separation of Concerns**: Equipment-related functionality is now isolated in a dedicated service.
2. **Enhanced Testability**: The service can be tested independently of the controller.
3. **Better Maintainability**: Changes to equipment functionality only require modifications to the service.
4. **Consistent Pattern**: Follows the same pattern as other services in the application.
5. **Event-Based Communication**: Provides events for equipment state changes, enabling loose coupling.

## Next Steps

1. Add unit tests for ProsimEquipmentService (deferred for now)
2. Proceed with Phase 2.5: ProsimPassengerService implementation

## Completed Tasks

- [x] Create `IProsimEquipmentService.cs` interface file
- [x] Create `ProsimEquipmentService.cs` implementation file
- [x] Move equipment-related methods from ProsimController to ProsimEquipmentService
  - [x] Move `SetServicePCA` method
  - [x] Move `SetServiceChocks` method
  - [x] Move `SetServiceGPU` method
- [x] Update ProsimController to use ProsimEquipmentService
- [ ] Add unit tests for ProsimEquipmentService (deferred)
- [x] Test the implementation to ensure it works correctly
