# EFB UI Page Architecture

## Overview

This document describes the standardized page architecture implemented for the EFB UI to resolve the "Page can have only Window or Frame as parent" error and establish a consistent pattern for all pages.

## Problem

When navigating to the Aircraft page, the following error occurred:

```
System.InvalidOperationException
  HResult=0x80131509
  Message=Page can have only Window or Frame as parent.
  Source=PresentationFramework
```

This error occurred because:
1. `AircraftPage` inherits from WPF's `Page` class
2. `AircraftPageAdapter` is a `UserControl` that implements `IEFBPage`
3. The adapter was trying to directly host the `Page` as its content
4. WPF requires that a `Page` can only be hosted in a `Window` or `Frame`

## Solution

A hybrid approach was implemented that:
1. Maintains compatibility with the existing navigation system
2. Properly hosts `Page` objects in a `Frame` as required by WPF
3. Provides a standardized pattern for all pages

### Components

1. **IEFBPageBehavior Interface**
   - Defines the behavior that all pages should implement
   - Includes title, icon, visibility, and navigation properties
   - Includes lifecycle methods (OnNavigatedTo, OnNavigatedFrom, etc.)

2. **PageAdapterBase Class**
   - Base class for all page adapters
   - Implements `IEFBPage` interface
   - Uses a `Frame` to host a `Page`
   - Forwards lifecycle methods to the page

3. **Page Implementation**
   - Pages inherit from WPF's `Page` class
   - Pages implement `IEFBPageBehavior` interface
   - Pages focus on UI and behavior

4. **Page Adapter Implementation**
   - Adapters inherit from `PageAdapterBase`
   - Adapters create and initialize the page
   - Adapters handle navigation and lifecycle

### Implementation Details

1. Created `IEFBPageBehavior` interface:
   ```csharp
   public interface IEFBPageBehavior
   {
       string Title { get; }
       string Icon { get; }
       bool IsVisibleInMenu { get; }
       bool CanNavigateTo { get; }
       
       void OnNavigatedTo();
       void OnNavigatedFrom();
       void OnActivated();
       void OnDeactivated();
       void OnRefresh();
   }
   ```

2. Created `PageAdapterBase` class:
   ```csharp
   public class PageAdapterBase : UserControl, IEFBPage
   {
       protected Frame _frame;
       protected Page _page;
       
       public PageAdapterBase(Page page, ILogger logger = null)
       {
           _frame = new Frame();
           _page = page;
           Content = _frame;
           _frame.Navigate(_page);
       }
       
       // IEFBPage implementation that forwards to the page
       public string Title => (_page as IEFBPageBehavior)?.Title ?? "Page";
       // ... other properties and methods
   }
   ```

3. Updated `AircraftPage` to implement `IEFBPageBehavior`:
   ```csharp
   public partial class AircraftPage : Page, IEFBPageBehavior
   {
       // Implementation
   }
   ```

4. Updated `AircraftPageAdapter` to inherit from `PageAdapterBase`:
   ```csharp
   public class AircraftPageAdapter : PageAdapterBase
   {
       public AircraftPageAdapter(
           IProsimDoorService doorService,
           IProsimEquipmentService equipmentService,
           IGSXFuelCoordinator fuelCoordinator,
           IGSXServiceOrchestrator serviceOrchestrator,
           IEventAggregator eventAggregator,
           ILogger logger = null)
           : base(new AircraftPage(
               doorService,
               equipmentService,
               fuelCoordinator,
               serviceOrchestrator,
               eventAggregator), 
               logger)
       {
       }
   }
   ```

5. Created documentation for the new page architecture pattern:
   - `Prosim2GSX/UI/EFB/Documentation/PageArchitecture.md`

## Migration Strategy

For existing pages that don't follow this pattern:
1. Keep them as they are for now
2. When updating them, migrate them to the new pattern
3. For new pages, always use the new pattern

## Benefits

1. **WPF Compatibility**: Properly uses WPF's navigation system with Frames
2. **Standardization**: All pages follow the same pattern
3. **Separation of Concerns**: Pages focus on UI, adapters handle navigation
4. **Testability**: Pages can be tested independently of navigation
5. **Maintainability**: Consistent pattern makes code easier to understand

## Future Work

1. Migrate existing pages (HomePage, LogsPage) to the new pattern
2. Create a base page class that implements common `IEFBPageBehavior` functionality
3. Add unit tests for the new components
4. Update the EFB UI documentation to reflect the new architecture
