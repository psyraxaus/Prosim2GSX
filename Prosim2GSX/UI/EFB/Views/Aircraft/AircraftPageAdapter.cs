using System;
using System.Windows.Controls;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AircraftPageAdapter"/> class.
        /// </summary>
        /// <param name="doorService">The door service.</param>
        /// <param name="equipmentService">The equipment service.</param>
        /// <param name="fuelCoordinator">The fuel coordinator.</param>
        /// <param name="serviceOrchestrator">The service orchestrator.</param>
        /// <param name="eventAggregator">The event aggregator.</param>
        public AircraftPageAdapter(
            IProsimDoorService doorService,
            IProsimEquipmentService equipmentService,
            IGSXFuelCoordinator fuelCoordinator,
            IGSXServiceOrchestrator serviceOrchestrator,
            IEventAggregator eventAggregator)
        {
            _page = new AircraftPage(
                doorService,
                equipmentService,
                fuelCoordinator,
                serviceOrchestrator,
                eventAggregator);

            // Set the content of this UserControl to the AircraftPage
            Content = _page;
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
            // Forward to the wrapped page
            _page.OnNavigatedTo();
        }

        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public void OnNavigatedFrom()
        {
            // Forward to the wrapped page
            _page.OnNavigatedFrom();
        }

        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public void OnActivated()
        {
            // Forward to the wrapped page
            _page.OnActivated();
        }

        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public void OnDeactivated()
        {
            // Forward to the wrapped page
            _page.OnDeactivated();
        }

        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public void OnRefresh()
        {
            // Forward to the wrapped page
            _page.OnRefresh();
        }
    }
}
