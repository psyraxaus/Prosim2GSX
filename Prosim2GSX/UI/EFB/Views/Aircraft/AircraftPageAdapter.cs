using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Views.Aircraft
{
    /// <summary>
    /// Adapter class for AircraftPage that implements IEFBPage.
    /// </summary>
    public class AircraftPageAdapter : UserControl, IEFBPage
    {
        private readonly AircraftPage _page;

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AircraftPageAdapter"/> class.
        /// </summary>
        /// <param name="doorService">The door service.</param>
        /// <param name="equipmentService">The equipment service.</param>
        /// <param name="fuelCoordinator">The fuel coordinator.</param>
        /// <param name="serviceOrchestrator">The service orchestrator.</param>
        /// <param name="eventAggregator">The event aggregator.</param>
        /// <param name="logger">The logger.</param>
        public AircraftPageAdapter(
            IProsimDoorService doorService,
            IProsimEquipmentService equipmentService,
            IGSXFuelCoordinator fuelCoordinator,
            IGSXServiceOrchestrator serviceOrchestrator,
            IEventAggregator eventAggregator,
            ILogger logger = null)
        {
            _logger = logger;
            _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "Creating AircraftPageAdapter");
            
            try
            {
                // Create the AircraftPage
                _page = new AircraftPage(
                    doorService,
                    equipmentService,
                    fuelCoordinator,
                    serviceOrchestrator,
                    eventAggregator);
                
                _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "AircraftPage created successfully");
                
                // Set the content of this UserControl to the AircraftPage
                Content = _page;
                
                _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "Content set to AircraftPage");
                
                // Add loaded event handler to check visibility after rendering
                this.Loaded += AircraftPageAdapter_Loaded;
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "AircraftPageAdapter", ex, "Error creating AircraftPage");
                
                // Create a fallback UI
                CreateFallbackUI();
            }
        }
        
        /// <summary>
        /// Creates a fallback UI when the AircraftPage cannot be created.
        /// </summary>
        private void CreateFallbackUI()
        {
            _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "Creating fallback UI");
            
            try
            {
                // Create a simple Grid with a white background
                Grid fallbackGrid = new Grid();
                fallbackGrid.Background = Brushes.White;
                
                // Add a TextBlock with an error message
                TextBlock errorText = new TextBlock();
                errorText.Text = "Error loading Aircraft page.\nPlease restart the application.";
                errorText.FontSize = 18;
                errorText.HorizontalAlignment = HorizontalAlignment.Center;
                errorText.VerticalAlignment = VerticalAlignment.Center;
                errorText.TextAlignment = TextAlignment.Center;
                
                fallbackGrid.Children.Add(errorText);
                
                // Set the content of this UserControl to the fallback Grid
                Content = fallbackGrid;
                
                _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "Fallback UI created successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "AircraftPageAdapter", ex, "Error creating fallback UI");
            }
        }
        
        /// <summary>
        /// Handles the Loaded event of the AircraftPageAdapter.
        /// </summary>
        private void AircraftPageAdapter_Loaded(object sender, RoutedEventArgs e)
        {
            _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "AircraftPageAdapter loaded");
            
            // Check if the page is visible
            if (_page != null)
            {
                bool isVisible = IsElementVisible(_page);
                _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", $"AircraftPage visibility: {isVisible}");
                
                // Log the visual tree for debugging
                LogVisualTree(_page);
                
                // Check if critical resources are available
                CheckCriticalResources();
            }
        }
        
        /// <summary>
        /// Checks if critical resources are available.
        /// </summary>
        private void CheckCriticalResources()
        {
            _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "Checking critical resources");
            
            var criticalResources = new[] {
                "EFBPrimaryBackgroundBrush",
                "EFBSecondaryBackgroundBrush",
                "EFBPrimaryTextBrush",
                "EFBSecondaryTextBrush",
                "EFBHighlightBrush",
                "EFBPrimaryBorderBrush",
                "EFBAccentBrush"
            };
            
            foreach (var resource in criticalResources)
            {
                try
                {
                    bool exists = Application.Current.Resources.Contains(resource);
                    _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", $"Resource '{resource}' exists: {exists}");
                    
                    if (exists)
                    {
                        var resourceValue = Application.Current.Resources[resource];
                        _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", $"Resource '{resource}' type: {resourceValue?.GetType().Name ?? "null"}");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, "AircraftPageAdapter", ex, $"Error checking resource '{resource}'");
                }
            }
        }
        
        /// <summary>
        /// Determines whether an element is visible.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <returns>True if the element is visible, false otherwise.</returns>
        private bool IsElementVisible(UIElement element)
        {
            if (element == null)
                return false;
            
            try
            {
                return element.Visibility == Visibility.Visible && 
                       element.IsVisible && 
                       element.Opacity > 0;
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "AircraftPageAdapter", ex, "Error checking element visibility");
                return false;
            }
        }
        
        /// <summary>
        /// Logs the visual tree of an element.
        /// </summary>
        /// <param name="element">The root element.</param>
        private void LogVisualTree(DependencyObject element, int depth = 0)
        {
            if (element == null || depth > 10) // Limit depth to avoid infinite recursion
                return;
            
            try
            {
                string indent = new string(' ', depth * 2);
                string typeName = element.GetType().Name;
                
                // Get additional properties based on element type
                string additionalInfo = "";
                
                if (element is FrameworkElement fe)
                {
                    additionalInfo = $"Name='{fe.Name}', Visibility={fe.Visibility}, " +
                                    $"Width={fe.Width}, Height={fe.Height}, " +
                                    $"ActualWidth={fe.ActualWidth}, ActualHeight={fe.ActualHeight}";
                }
                
                if (element is Control control)
                {
                    additionalInfo += $", Background={control.Background}";
                }
                
                if (element is Panel panel)
                {
                    additionalInfo += $", Background={panel.Background}, Children={panel.Children.Count}";
                }
                
                _logger?.Log(LogLevel.Debug, "VisualTree", $"{indent}{typeName} {additionalInfo}");
                
                // Recursively log children
                int childCount = VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < childCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(element, i);
                    LogVisualTree(child, depth + 1);
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "AircraftPageAdapter", ex, "Error logging visual tree");
            }
        }

        /// <summary>
        /// Gets the title of the page.
        /// </summary>
        public string Title => "Aircraft";

        /// <summary>
        /// Gets the icon of the page.
        /// </summary>
        public string Icon => "AircraftIcon";

        /// <summary>
        /// Gets the page content.
        /// </summary>
        UserControl IEFBPage.Content => this;

        /// <summary>
        /// Gets a value indicating whether the page is visible in the navigation menu.
        /// </summary>
        public bool IsVisibleInMenu => true;

        /// <summary>
        /// Gets a value indicating whether the page can be navigated to.
        /// </summary>
        public bool CanNavigateTo => true;

        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        public void OnNavigatedTo()
        {
            _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "OnNavigatedTo called");
            
            // Forward to the wrapped page
            if (_page != null)
            {
                _page.OnNavigatedTo();
                
                // Check visibility after navigation
                bool isVisible = IsElementVisible(_page);
                _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", $"AircraftPage visibility after navigation: {isVisible}");
            }
            else
            {
                _logger?.Log(LogLevel.Warning, "AircraftPageAdapter", "Cannot forward OnNavigatedTo: _page is null");
            }
        }

        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public void OnNavigatedFrom()
        {
            _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "OnNavigatedFrom called");
            
            // Forward to the wrapped page
            if (_page != null)
            {
                _page.OnNavigatedFrom();
            }
            else
            {
                _logger?.Log(LogLevel.Warning, "AircraftPageAdapter", "Cannot forward OnNavigatedFrom: _page is null");
            }
        }

        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public void OnActivated()
        {
            _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "OnActivated called");
            
            // Forward to the wrapped page
            if (_page != null)
            {
                _page.OnActivated();
            }
            else
            {
                _logger?.Log(LogLevel.Warning, "AircraftPageAdapter", "Cannot forward OnActivated: _page is null");
            }
        }

        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public void OnDeactivated()
        {
            _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "OnDeactivated called");
            
            // Forward to the wrapped page
            if (_page != null)
            {
                _page.OnDeactivated();
            }
            else
            {
                _logger?.Log(LogLevel.Warning, "AircraftPageAdapter", "Cannot forward OnDeactivated: _page is null");
            }
        }

        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public void OnRefresh()
        {
            _logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "OnRefresh called");
            
            // Forward to the wrapped page
            if (_page != null)
            {
                _page.OnRefresh();
            }
            else
            {
                _logger?.Log(LogLevel.Warning, "AircraftPageAdapter", "Cannot forward OnRefresh: _page is null");
            }
        }
    }
}
