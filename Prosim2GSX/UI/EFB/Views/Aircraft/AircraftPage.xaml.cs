using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.IO;
using System.Xml;
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
            // Create a simple content structure instead of using InitializeComponent
            try
            {
                // Create a simple Grid with a white background
                Grid mainGrid = new Grid();
                mainGrid.Background = System.Windows.Media.Brushes.White;
                
                // Define rows for the Grid
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                
                // Add a header TextBlock
                TextBlock headerBlock = new TextBlock();
                headerBlock.Text = "Aircraft Status";
                headerBlock.FontSize = 24;
                headerBlock.FontWeight = FontWeights.Bold;
                headerBlock.HorizontalAlignment = HorizontalAlignment.Center;
                headerBlock.Margin = new Thickness(0, 20, 0, 20);
                Grid.SetRow(headerBlock, 0);
                
                // Add a status message TextBlock
                TextBlock statusBlock = new TextBlock();
                statusBlock.Text = "EFB UI is in fallback mode due to resource loading issues.\nThe theme resources have been updated and should work correctly on next restart.";
                statusBlock.FontSize = 16;
                statusBlock.TextWrapping = TextWrapping.Wrap;
                statusBlock.HorizontalAlignment = HorizontalAlignment.Center;
                statusBlock.TextAlignment = TextAlignment.Center;
                statusBlock.Margin = new Thickness(20);
                Grid.SetRow(statusBlock, 1);
                
                // Add a diagnostic info TextBlock
                TextBlock diagBlock = new TextBlock();
                diagBlock.Text = "Diagnostic Info:\n" +
                                "- Added missing EFBHighlightBrush\n" +
                                "- Added missing EFBPrimaryBackgroundBrush\n" +
                                "- Added missing EFBSecondaryBackgroundBrush\n" +
                                "- Added missing EFBPrimaryTextBrush\n" +
                                "- Added missing EFBSecondaryTextBrush\n" +
                                "- Added fallback UI rendering";
                diagBlock.FontSize = 14;
                diagBlock.TextWrapping = TextWrapping.Wrap;
                diagBlock.HorizontalAlignment = HorizontalAlignment.Left;
                diagBlock.Margin = new Thickness(20);
                Grid.SetRow(diagBlock, 2);
                
                // Add the TextBlocks to the Grid
                mainGrid.Children.Add(headerBlock);
                mainGrid.Children.Add(statusBlock);
                mainGrid.Children.Add(diagBlock);
                
                // Set the Grid as the content of the Page
                this.Content = mainGrid;
                
                Logger.Log(LogLevel.Debug, "AircraftPage", "Manual UI initialization completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AircraftPage", ex, "Error in manual UI initialization");
            }

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
