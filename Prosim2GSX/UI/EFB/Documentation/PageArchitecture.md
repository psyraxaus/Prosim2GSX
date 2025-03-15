# EFB UI Page Architecture

This document describes the standardized page architecture for the EFB UI. This architecture ensures that all pages follow the same pattern and can be properly hosted in the navigation system.

## Overview

The EFB UI uses a hybrid approach for page navigation that combines WPF's built-in navigation system with our custom navigation requirements. This approach allows us to:

1. Use WPF's `Page` class for content
2. Properly host pages in a `Frame` as required by WPF
3. Maintain compatibility with our existing `IEFBPage` interface
4. Standardize the page lifecycle

## Architecture Components

### 1. IEFBPageBehavior Interface

This interface defines the behavior that all pages should implement. It includes:

- Title and icon properties for navigation
- Visibility and navigation flags
- Lifecycle methods (OnNavigatedTo, OnNavigatedFrom, etc.)

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

### 2. Page Classes

All page content should be implemented as a WPF `Page` that implements `IEFBPageBehavior`:

```csharp
public partial class MyPage : Page, IEFBPageBehavior
{
    // Page implementation
    
    // IEFBPageBehavior implementation
    public string Title => "My Page";
    public string Icon => "\uE123"; // Icon code
    public bool IsVisibleInMenu => true;
    public bool CanNavigateTo => true;
    
    public void OnNavigatedTo() { /* ... */ }
    public void OnNavigatedFrom() { /* ... */ }
    public void OnActivated() { /* ... */ }
    public void OnDeactivated() { /* ... */ }
    public void OnRefresh() { /* ... */ }
}
```

### 3. PageAdapterBase Class

This base class adapts a WPF `Page` to work with our navigation system:

- Hosts the page in a `Frame` (required by WPF)
- Implements `IEFBPage` interface
- Forwards lifecycle methods to the page

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

### 4. Page Adapters

Each page should have a corresponding adapter that inherits from `PageAdapterBase`:

```csharp
public class MyPageAdapter : PageAdapterBase
{
    public MyPageAdapter(/* dependencies */) 
        : base(new MyPage(/* dependencies */))
    {
    }
}
```

## Implementation Pattern

### Creating a New Page

1. Create a new XAML Page (e.g., `MyPage.xaml` and `MyPage.xaml.cs`)
2. Make the page implement `IEFBPageBehavior`
3. Create an adapter class that inherits from `PageAdapterBase`
4. Register the adapter with the navigation system

### Example

```csharp
// MyPage.xaml.cs
public partial class MyPage : Page, IEFBPageBehavior
{
    private readonly MyViewModel _viewModel;
    
    public MyPage(IMyService myService)
    {
        InitializeComponent();
        _viewModel = new MyViewModel(myService);
        DataContext = _viewModel;
    }
    
    // IEFBPageBehavior implementation
    public string Title => "My Page";
    public string Icon => "\uE123";
    public bool IsVisibleInMenu => true;
    public bool CanNavigateTo => true;
    
    public void OnNavigatedTo()
    {
        _viewModel.Initialize();
    }
    
    public void OnNavigatedFrom()
    {
        _viewModel.Cleanup();
    }
    
    public void OnActivated()
    {
        _viewModel.Initialize();
    }
    
    public void OnDeactivated()
    {
        _viewModel.Cleanup();
    }
    
    public void OnRefresh()
    {
        _viewModel.Initialize();
    }
}

// MyPageAdapter.cs
public class MyPageAdapter : PageAdapterBase
{
    public MyPageAdapter(IMyService myService, ILogger logger = null)
        : base(new MyPage(myService), logger)
    {
    }
}
```

## Registration with Navigation System

Register the page adapter with the navigation system in `EFBApplication.cs`:

```csharp
_windowManager.RegisterPage(
    "MyPage",
    () => new MyPageAdapter(
        _serviceModel.GetService<IMyService>(),
        _logger
    ),
    "My Page",
    "\uE123");
```

## Benefits of This Architecture

1. **WPF Compatibility**: Properly uses WPF's navigation system with Frames
2. **Standardization**: All pages follow the same pattern
3. **Separation of Concerns**: Pages focus on UI, adapters handle navigation
4. **Testability**: Pages can be tested independently of navigation
5. **Maintainability**: Consistent pattern makes code easier to understand

## Migration Strategy

For existing pages that don't follow this pattern:

1. Keep them as they are for now
2. When updating them, migrate them to the new pattern
3. For new pages, always use the new pattern

## Troubleshooting

### Common Issues

1. **"Page can have only Window or Frame as parent"**
   - This error occurs when a `Page` is not properly hosted in a `Frame`
   - Make sure the page is wrapped in a `PageAdapterBase` that uses a `Frame`

2. **"Cannot find resource"**
   - This can occur if resources are not properly loaded
   - The `PageAdapterBase` includes fallback UI creation for this case

3. **"Page not visible"**
   - Check that the page is properly initialized
   - Ensure the `Frame` is navigating to the page
   - Use the diagnostic logging in `PageAdapterBase` to troubleshoot
