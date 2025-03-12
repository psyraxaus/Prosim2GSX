using System;
using System.Windows;
using System.Windows.Controls;
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
            InitializeComponent();

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
            // Highlight the door that changed state
            switch (args.DoorType)
            {
                case DoorType.ForwardLeft:
                    AircraftDiagramControl.HighlightDoor("ForwardLeft");
                    break;
                case DoorType.ForwardRight:
                    AircraftDiagramControl.HighlightDoor("ForwardRight");
                    break;
                case DoorType.AftLeft:
                    AircraftDiagramControl.HighlightDoor("AftLeft");
                    break;
                case DoorType.AftRight:
                    AircraftDiagramControl.HighlightDoor("AftRight");
                    break;
                case DoorType.ForwardCargo:
                    AircraftDiagramControl.HighlightDoor("ForwardCargo");
                    break;
                case DoorType.AftCargo:
                    AircraftDiagramControl.HighlightDoor("AftCargo");
                    break;
            }

            // Clear the highlight after a delay
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                AircraftDiagramControl.ResetDoorHighlights();
                timer.Stop();
            };
            timer.Start();
        }

        private void OnFuelStateChanged(FuelStateChangedEventArgs args)
        {
            // Highlight the fuel service point when the fuel state changes
            if (args.IsRefueling)
            {
                AircraftDiagramControl.HighlightServicePoint("Refueling");

                // Clear the highlight after a delay
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, e) =>
                {
                    AircraftDiagramControl.ResetServicePointHighlights();
                    timer.Stop();
                };
                timer.Start();
            }
        }

        #endregion
    }
}
