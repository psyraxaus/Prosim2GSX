using System;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Navigation;

namespace Prosim2GSX.UI.EFB.Views.Aircraft
{
    /// <summary>
    /// Adapter class for AircraftPage that inherits from PageAdapterBase.
    /// </summary>
    public class AircraftPageAdapter : PageAdapterBase
    {
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
            : base(new AircraftPage(
                doorService,
                equipmentService,
                fuelCoordinator,
                serviceOrchestrator,
                eventAggregator), 
                logger)
        {
            logger?.Log(LogLevel.Debug, "AircraftPageAdapter", "AircraftPageAdapter created successfully");
        }
    }
}
