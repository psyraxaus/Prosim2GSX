using System;
using System.Windows;
using System.Windows.Controls;
using Prosim2GSX.UI.EFB.ViewModels.Aircraft;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Views.Aircraft
{
    /// <summary>
    /// Interaction logic for AircraftPage.xaml
    /// </summary>
    public partial class AircraftPage : Page, IEFBPage
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

        #region IEFBPage Implementation

        public string PageTitle => "Aircraft";

        public void OnNavigatedTo(object parameter)
        {
            // Update the view model when navigated to
            _viewModel.InitializeState();
        }

        public void OnNavigatedFrom()
        {
            // Clean up when navigated away from
            _viewModel.Cleanup();
        }

        #endregion

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
