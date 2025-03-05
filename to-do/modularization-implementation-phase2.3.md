# Modularization Implementation - Phase 2.3: ProsimDoorService

## Overview

This document details the implementation of Phase 2.3 of the Prosim2GSX modularization strategy, which focuses on extracting door-related functionality from the ProsimController into a dedicated ProsimDoorService.

## Implementation Details

### 1. Created IProsimDoorService Interface

Created a new interface file `IProsimDoorService.cs` in the Services folder that defines the contract for door-related operations:

```csharp
public interface IProsimDoorService
{
    void SetAftRightDoor(bool open);
    void SetForwardRightDoor(bool open);
    void SetForwardCargoDoor(bool open);
    void SetAftCargoDoor(bool open);
    event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
}

public class DoorStateChangedEventArgs : EventArgs
{
    public string DoorName { get; }
    public bool IsOpen { get; }
    
    public DoorStateChangedEventArgs(string doorName, bool isOpen)
    {
        DoorName = doorName;
        IsOpen = isOpen;
    }
}
```

The interface defines methods for controlling the aircraft doors and includes an event for notifying subscribers when a door state changes.

### 2. Created ProsimDoorService Implementation

Created a new implementation file `ProsimDoorService.cs` in the Services folder:

```csharp
public class ProsimDoorService : IProsimDoorService
{
    private readonly IProsimService _prosimService;
    
    public event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
    
    public ProsimDoorService(IProsimService prosimService)
    {
        _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
    }
    
    public void SetAftRightDoor(bool open)
    {
        _prosimService.SetVariable("doors.entry.right.aft", open);
        Logger.Log(LogLevel.Information, "ProsimDoorService:SetAftRightDoor", $"Aft right door {(open ? "opened" : "closed")}");
        OnDoorStateChanged("AftRightDoor", open);
    }
    
    public void SetForwardRightDoor(bool open)
    {
        _prosimService.SetVariable("doors.entry.right.fwd", open);
        Logger.Log(LogLevel.Information, "ProsimDoorService:SetForwardRightDoor", $"Forward right door {(open ? "opened" : "closed")}");
        OnDoorStateChanged("ForwardRightDoor", open);
    }
    
    public void SetForwardCargoDoor(bool open)
    {
        _prosimService.SetVariable("doors.cargo.forward", open);
        Logger.Log(LogLevel.Information, "ProsimDoorService:SetForwardCargoDoor", $"Forward cargo door {(open ? "opened" : "closed")}");
        OnDoorStateChanged("ForwardCargoDoor", open);
    }
    
    public void SetAftCargoDoor(bool open)
    {
        _prosimService.SetVariable("doors.cargo.aft", open);
        Logger.Log(LogLevel.Information, "ProsimDoorService:SetAftCargoDoor", $"Aft cargo door {(open ? "opened" : "closed")}");
        OnDoorStateChanged("AftCargoDoor", open);
    }
    
    protected virtual void OnDoorStateChanged(string doorName, bool isOpen)
    {
        DoorStateChanged?.Invoke(this, new DoorStateChangedEventArgs(doorName, isOpen));
    }
}
```

The implementation uses the IProsimService to interact with the ProSim SDK and raises events when door states change.

### 3. Updated ProsimController

Modified the ProsimController to use the new ProsimDoorService:

1. Added a field for the ProsimDoorService:
```csharp
private readonly IProsimDoorService _doorService;
```

2. Initialized the ProsimDoorService in the constructor:
```csharp
_doorService = new ProsimDoorService(Interface.ProsimService);

// Optionally subscribe to door state change events
_doorService.DoorStateChanged += (sender, args) => {
    // Handle door state changes if needed
    Logger.Log(LogLevel.Debug, "ProsimController:DoorStateChanged", 
        $"{args.DoorName} is now {(args.IsOpen ? "open" : "closed")}");
};
```

3. Updated the door-related methods to delegate to the ProsimDoorService:
```csharp
public void SetAftRightDoor(bool open)
{
    _doorService.SetAftRightDoor(open);
}

public void SetForwardRightDoor(bool open)
{
    _doorService.SetForwardRightDoor(open);
}

public void SetForwardCargoDoor(bool open)
{
    _doorService.SetForwardCargoDoor(open);
}

public void SetAftCargoDoor(bool open)
{
    _doorService.SetAftCargoDoor(open);
}
```

## Benefits

1. **Improved Separation of Concerns**:
   - Door-related functionality is now isolated in a dedicated service
   - ProsimController is simplified and more focused

2. **Enhanced Testability**:
   - Door operations can be tested in isolation
   - Mock implementations can be used for testing

3. **Better Maintainability**:
   - Changes to door operations only affect the ProsimDoorService
   - New door-related features can be added without modifying the ProsimController

4. **Consistent Service Pattern**:
   - Follows the same pattern as other services in the application
   - Maintains a clean and consistent architecture

## Next Steps

After completing Phase 2.3, we should proceed with Phase 2.4 to implement the ProsimEquipmentService, following a similar approach.
