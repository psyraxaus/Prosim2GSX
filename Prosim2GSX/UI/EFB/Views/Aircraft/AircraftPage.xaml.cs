using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.IO;
using System.Xml;
using System.Diagnostics;
using Prosim2GSX.UI.EFB.ViewModels.Aircraft;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Views.Aircraft
{
    /// <summary>
    /// Interaction logic for AircraftPage.xaml
    /// </summary>
    public partial class AircraftPage : Page
    {
        private readonly AircraftViewModel _viewModel;
        private readonly IEventAggregator _eventAggregator;

        public AircraftPage(
            IProsimDoorService doorService,
            IProsimEquipmentService equipmentService,
            IGSXFuelCoordinator fuelCoordinator,
            IGSXServiceOrchestrator serviceOrchestrator,
            IEventAggregator eventAggregator)
        {
            // Show a simple loading UI immediately
            var loadingGrid = new Grid { Background = Brushes.White };
            var loadingText = new TextBlock { 
                Text = "Loading EFB...", 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24
            };
            loadingGrid.Children.Add(loadingText);
            this.Content = loadingGrid;
            
            // Store dependencies
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // Create the view model
            _viewModel = new AircraftViewModel(
                doorService,
                equipmentService,
                fuelCoordinator,
                serviceOrchestrator,
                eventAggregator);

            // Set the data context
            DataContext = _viewModel;

            // Subscribe to events
            _eventAggregator.Subscribe<DoorStateChangedEventArgs>(OnDoorStateChanged);
            _eventAggregator.Subscribe<FuelStateChangedEventArgs>(OnFuelStateChanged);
            
            Logger.Log(LogLevel.Debug, "AircraftPage", "Constructor completed successfully");
            
            // Load the full UI asynchronously
            Dispatcher.InvokeAsync(async () => {
                await Task.Delay(100); // Give the loading UI time to render
                await InitializeFullUIAsync();
            });
        }
        
        /// <summary>
        /// Initializes the full UI asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InitializeFullUIAsync()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "AircraftPage", "Initializing full UI");
                
                // Instead of trying to load XAML directly, we'll create the UI programmatically
                // This avoids XML parsing issues and is more reliable
                CreateManualUI();
                
                Logger.Log(LogLevel.Debug, "AircraftPage", "UI initialized successfully");
                
                // Allow UI to render before returning
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AircraftPage", ex, "Error initializing UI");
            }
        }

        /// <summary>
        /// Creates the UI manually.
        /// </summary>
        private void CreateManualUI()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "AircraftPage", "Creating manual UI");
                
                // Create a Grid with the same structure as the XAML
                Grid mainGrid = new Grid();
                
                // Explicitly set the background to ensure visibility
                mainGrid.Background = Brushes.White;
                
                // Log the grid creation
                Logger.Log(LogLevel.Debug, "AircraftPage", $"Created main grid with background: {mainGrid.Background}");
                
                // Define rows for the Grid
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
                
                // Create resources dictionary for styles
                mainGrid.Resources = new ResourceDictionary();
                
                // Create and add header border style
                Style headerBorderStyle = new Style(typeof(Border));
                headerBorderStyle.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(60, 60, 60))));
                headerBorderStyle.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(69, 69, 69))));
                headerBorderStyle.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(10)));
                mainGrid.Resources.Add("HeaderBorderStyle", headerBorderStyle);
                
                // Create and add primary text style
                Style primaryTextStyle = new Style(typeof(TextBlock));
                primaryTextStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.White));
                mainGrid.Resources.Add("PrimaryTextStyle", primaryTextStyle);
                
                // Create and add secondary text style
                Style secondaryTextStyle = new Style(typeof(TextBlock));
                secondaryTextStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(204, 204, 204))));
                mainGrid.Resources.Add("SecondaryTextStyle", secondaryTextStyle);
                
                // Create and add panel border style
                Style panelBorderStyle = new Style(typeof(Border));
                panelBorderStyle.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 30, 30))));
                panelBorderStyle.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(69, 69, 69))));
                panelBorderStyle.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1)));
                panelBorderStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(5)));
                panelBorderStyle.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(10)));
                mainGrid.Resources.Add("PanelBorderStyle", panelBorderStyle);
                
                // Create header
                Border headerBorder = new Border();
                headerBorder.Style = headerBorderStyle;
                headerBorder.BorderThickness = new Thickness(0, 0, 0, 1);
                Grid.SetRow(headerBorder, 0);
                
                Grid headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                
                TextBlock headerText = new TextBlock();
                headerText.Text = "Aircraft Status";
                headerText.FontSize = 20;
                headerText.FontWeight = FontWeights.Bold;
                headerText.Style = primaryTextStyle;
                headerText.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(headerText, 0);
                
                TextBlock phaseText = new TextBlock();
                phaseText.Text = "PREFLIGHT"; // Default value, will be bound to ViewModel
                phaseText.FontSize = 16;
                phaseText.FontWeight = FontWeights.Bold;
                phaseText.Style = primaryTextStyle;
                phaseText.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(phaseText, 1);
                
                headerGrid.Children.Add(headerText);
                headerGrid.Children.Add(phaseText);
                headerBorder.Child = headerGrid;
                mainGrid.Children.Add(headerBorder);
                
                // Create status bar
                Border statusBorder = new Border();
                statusBorder.Style = headerBorderStyle;
                statusBorder.BorderThickness = new Thickness(0, 1, 0, 0);
                Grid.SetRow(statusBorder, 2);
                
                Grid statusGrid = new Grid();
                statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                
                TextBlock statusText = new TextBlock();
                statusText.Text = "Ready"; // Default value, will be bound to ViewModel
                statusText.Style = primaryTextStyle;
                statusText.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(statusText, 0);
                
                TextBlock connectionText = new TextBlock();
                connectionText.Text = "Connected"; // Default value, will be bound to ViewModel
                connectionText.Style = primaryTextStyle;
                connectionText.VerticalAlignment = VerticalAlignment.Center;
                connectionText.Margin = new Thickness(10, 0, 0, 0);
                Grid.SetColumn(connectionText, 2);
                
                statusGrid.Children.Add(statusText);
                statusGrid.Children.Add(connectionText);
                statusBorder.Child = statusGrid;
                mainGrid.Children.Add(statusBorder);
                
                // Create service controls panel
                Border serviceControlsBorder = new Border();
                serviceControlsBorder.Style = panelBorderStyle;
                serviceControlsBorder.HorizontalAlignment = HorizontalAlignment.Right;
                serviceControlsBorder.VerticalAlignment = VerticalAlignment.Top;
                serviceControlsBorder.Margin = new Thickness(0, 10, 10, 0);
                Grid.SetRow(serviceControlsBorder, 1);
                
                StackPanel serviceControlsPanel = new StackPanel();
                
                TextBlock servicesTitle = new TextBlock();
                servicesTitle.Text = "Services";
                servicesTitle.FontSize = 16;
                servicesTitle.FontWeight = FontWeights.Bold;
                servicesTitle.Style = primaryTextStyle;
                servicesTitle.Margin = new Thickness(0, 0, 0, 10);
                
                serviceControlsPanel.Children.Add(servicesTitle);
                
                // Add service buttons
                string[] serviceTypes = new[] { "Refueling", "Catering", "Boarding", "Deboarding", "Cargo Loading", "Cargo Unloading" };
                foreach (var serviceType in serviceTypes)
                {
                    Button requestButton = new Button();
                    requestButton.Content = $"Request {serviceType}";
                    requestButton.Margin = new Thickness(0, 5, 0, 0);
                    serviceControlsPanel.Children.Add(requestButton);
                    
                    Button cancelButton = new Button();
                    cancelButton.Content = $"Cancel {serviceType}";
                    cancelButton.Margin = new Thickness(0, 5, 0, 0);
                    serviceControlsPanel.Children.Add(cancelButton);
                }
                
                serviceControlsBorder.Child = serviceControlsPanel;
                mainGrid.Children.Add(serviceControlsBorder);
                
                // Create progress panel
                Border progressBorder = new Border();
                progressBorder.Style = panelBorderStyle;
                progressBorder.HorizontalAlignment = HorizontalAlignment.Left;
                progressBorder.VerticalAlignment = VerticalAlignment.Top;
                progressBorder.Margin = new Thickness(10, 10, 0, 0);
                Grid.SetRow(progressBorder, 1);
                
                StackPanel progressPanel = new StackPanel();
                progressPanel.Width = 200;
                
                TextBlock progressTitle = new TextBlock();
                progressTitle.Text = "Service Progress";
                progressTitle.FontSize = 16;
                progressTitle.FontWeight = FontWeights.Bold;
                progressTitle.Style = primaryTextStyle;
                progressTitle.Margin = new Thickness(0, 0, 0, 10);
                
                progressPanel.Children.Add(progressTitle);
                
                // Add a "no services" message
                TextBlock noServicesText = new TextBlock();
                noServicesText.Text = "No services in progress";
                noServicesText.Style = secondaryTextStyle;
                noServicesText.TextAlignment = TextAlignment.Center;
                noServicesText.Margin = new Thickness(0, 10, 0, 0);
                
                progressPanel.Children.Add(noServicesText);
                
                progressBorder.Child = progressPanel;
                mainGrid.Children.Add(progressBorder);
                
                // Set the Grid as the content of the Page
                this.Content = mainGrid;
                
                // Log the content setting
                Logger.Log(LogLevel.Debug, "AircraftPage", $"Set page content to mainGrid, Content type: {this.Content?.GetType().Name ?? "null"}");
                
                // Ensure the page is visible
                this.Visibility = Visibility.Visible;
                
                // Force layout update
                this.UpdateLayout();
                
                // Log the page visibility
                Logger.Log(LogLevel.Debug, "AircraftPage", $"Page visibility: {this.Visibility}, ActualWidth: {this.ActualWidth}, ActualHeight: {this.ActualHeight}");
                
                Logger.Log(LogLevel.Debug, "AircraftPage", "Manual UI creation completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AircraftPage", ex, "Error in manual UI creation");
                
                // Create a very simple fallback UI in case the main UI creation fails
                Grid fallbackGrid = new Grid();
                fallbackGrid.Background = Brushes.White;
                
                TextBlock fallbackText = new TextBlock();
                fallbackText.Text = "EFB UI is in emergency fallback mode.\nPlease restart the application.";
                fallbackText.FontSize = 18;
                fallbackText.HorizontalAlignment = HorizontalAlignment.Center;
                fallbackText.VerticalAlignment = VerticalAlignment.Center;
                fallbackText.TextAlignment = TextAlignment.Center;
                
                fallbackGrid.Children.Add(fallbackText);
                this.Content = fallbackGrid;
            }
        }

        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        public void OnNavigatedTo()
        {
            // Update the view model when navigated to
            _viewModel.InitializeState();
        }

        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public void OnNavigatedFrom()
        {
            // Clean up when navigated away from
            _viewModel.Cleanup();
        }
        
        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public void OnActivated()
        {
            // Handle activation
            _viewModel.InitializeState();
        }
        
        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public void OnDeactivated()
        {
            // Handle deactivation
            _viewModel.Cleanup();
        }
        
        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public void OnRefresh()
        {
            // Refresh the page
            _viewModel.InitializeState();
        }

        #region Event Handlers

        private void OnDoorStateChanged(DoorStateChangedEventArgs args)
        {
            // Log the door state change
            Logger.Log(LogLevel.Debug, "AircraftPage", $"Door state changed: {args.DoorType} - IsOpen: {args.IsOpen}");
        }

        private void OnFuelStateChanged(FuelStateChangedEventArgs args)
        {
            // Log the fuel state change
            Logger.Log(LogLevel.Debug, "AircraftPage", $"Fuel state changed: IsRefueling: {args.IsRefueling}");
        }

        #endregion
    }
}
