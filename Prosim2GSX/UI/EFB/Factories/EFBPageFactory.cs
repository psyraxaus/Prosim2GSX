using System;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.UI.EFB.Views.Aircraft;

namespace Prosim2GSX.UI.EFB.Factories
{
    /// <summary>
    /// Interface for a factory that creates EFB page instances
    /// </summary>
    public interface IEFBPageFactory
    {
        /// <summary>
        /// Creates a page instance of the specified type
        /// </summary>
        /// <param name="pageType">The type of page to create</param>
        /// <returns>The created page instance</returns>
        IEFBPage CreatePage(Type pageType);
    }

    /// <summary>
    /// Factory for creating EFB page instances
    /// </summary>
    public class EFBPageFactory : IEFBPageFactory
    {
        private readonly ServiceModel _serviceModel;
        private readonly IProsimDoorService _doorService;
        private readonly IProsimEquipmentService _equipmentService;
        private readonly IGSXFuelCoordinator _fuelCoordinator;
        private readonly IGSXServiceOrchestrator _serviceOrchestrator;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBPageFactory"/> class
        /// </summary>
        /// <param name="serviceModel">The service model</param>
        /// <param name="doorService">The door service</param>
        /// <param name="equipmentService">The equipment service</param>
        /// <param name="fuelCoordinator">The fuel coordinator</param>
        /// <param name="serviceOrchestrator">The service orchestrator</param>
        /// <param name="eventAggregator">The event aggregator</param>
        /// <param name="logger">The logger</param>
        public EFBPageFactory(
            ServiceModel serviceModel,
            IProsimDoorService doorService,
            IProsimEquipmentService equipmentService,
            IGSXFuelCoordinator fuelCoordinator,
            IGSXServiceOrchestrator serviceOrchestrator,
            IEventAggregator eventAggregator,
            ILogger logger = null)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _doorService = doorService ?? throw new ArgumentNullException(nameof(doorService));
            _equipmentService = equipmentService ?? throw new ArgumentNullException(nameof(equipmentService));
            _fuelCoordinator = fuelCoordinator ?? throw new ArgumentNullException(nameof(fuelCoordinator));
            _serviceOrchestrator = serviceOrchestrator ?? throw new ArgumentNullException(nameof(serviceOrchestrator));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger;
        }

        /// <summary>
        /// Creates a page instance of the specified type
        /// </summary>
        /// <param name="pageType">The type of page to create</param>
        /// <returns>The created page instance</returns>
        public IEFBPage CreatePage(Type pageType)
        {
            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType));
            }

            _logger?.Log(LogLevel.Debug, "EFBPageFactory:CreatePage", $"Creating page of type {pageType.Name}");

            try
            {
                // Handle special cases for pages that require dependencies
                if (pageType == typeof(AircraftPageAdapter))
                {
                    return new AircraftPageAdapter(
                        _doorService,
                        _equipmentService,
                        _fuelCoordinator,
                        _serviceOrchestrator,
                        _eventAggregator);
                }

                // For pages with a parameterless constructor, use Activator.CreateInstance
                if (pageType.GetConstructor(Type.EmptyTypes) != null)
                {
                    return (IEFBPage)Activator.CreateInstance(pageType);
                }

                // If we get here, we don't know how to create the page
                throw new InvalidOperationException($"No factory method available for page type {pageType.Name}");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBPageFactory:CreatePage", ex, $"Error creating page of type {pageType.Name}");
                throw;
            }
        }
    }
}
