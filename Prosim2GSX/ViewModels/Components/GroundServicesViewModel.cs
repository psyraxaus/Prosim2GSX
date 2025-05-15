using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for displaying and managing ground service statuses
    /// </summary>
    public class GroundServicesViewModel : ViewModelBase
    {
        #region Fields

        private SubscriptionToken _serviceStatusChangedSubscription;
        private readonly Dictionary<string, Brush> _serviceStatusBrushes = new Dictionary<string, Brush>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the status indicator brush for the Jetway service
        /// </summary>
        public Brush JetwayStatusBrush => GetServiceStatusBrush("Jetway");

        /// <summary>
        /// Gets the status indicator brush for the Stairs service
        /// </summary>
        public Brush StairsStatusBrush => GetServiceStatusBrush("Stairs");

        /// <summary>
        /// Gets the status indicator brush for the Refueling service
        /// </summary>
        public Brush RefuelStatusBrush => GetServiceStatusBrush("Refuel");

        /// <summary>
        /// Gets the status indicator brush for the Catering service
        /// </summary>
        public Brush CateringStatusBrush => GetServiceStatusBrush("Catering");

        /// <summary>
        /// Gets the status indicator brush for the Boarding service
        /// </summary>
        public Brush BoardingStatusBrush => GetServiceStatusBrush("Boarding");

        /// <summary>
        /// Gets the status indicator brush for the Deboarding service
        /// </summary>
        public Brush DeboardingStatusBrush => GetServiceStatusBrush("Deboarding");

        /// <summary>
        /// Gets the status indicator brush for the GPU service
        /// </summary>
        public Brush GPUStatusBrush => GetServiceStatusBrush("GPU");

        /// <summary>
        /// Gets the status indicator brush for the PCA service
        /// </summary>
        public Brush PCAStatusBrush => GetServiceStatusBrush("PCA");

        /// <summary>
        /// Gets the status indicator brush for the Pushback service
        /// </summary>
        public Brush PushbackStatusBrush => GetServiceStatusBrush("Pushback");

        /// <summary>
        /// Gets the status indicator brush for the Chocks service
        /// </summary>
        public Brush ChocksStatusBrush => GetServiceStatusBrush("Chocks");

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the GroundServicesViewModel
        /// </summary>
        /// <param name="serviceModel">The service model</param>
        public GroundServicesViewModel(ServiceModel serviceModel = null)
        {
            // Initialize all services with gray status
            InitializeServiceStatuses();

            // Subscribe to service status change events
            _serviceStatusChangedSubscription = EventAggregator.Instance.Subscribe<ServiceStatusChangedEvent>(OnServiceStatusChanged);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes all service statuses to gray (disconnected/inactive)
        /// </summary>
        private void InitializeServiceStatuses()
        {
            // Set default colors for all services
            _serviceStatusBrushes["Jetway"] = new SolidColorBrush(Colors.LightGray);
            _serviceStatusBrushes["Stairs"] = new SolidColorBrush(Colors.LightGray);
            _serviceStatusBrushes["Refuel"] = new SolidColorBrush(Colors.LightGray);
            _serviceStatusBrushes["Catering"] = new SolidColorBrush(Colors.LightGray);
            _serviceStatusBrushes["Boarding"] = new SolidColorBrush(Colors.LightGray);
            _serviceStatusBrushes["Deboarding"] = new SolidColorBrush(Colors.LightGray);
            _serviceStatusBrushes["GPU"] = new SolidColorBrush(Colors.LightGray);
            _serviceStatusBrushes["PCA"] = new SolidColorBrush(Colors.LightGray);
            _serviceStatusBrushes["Pushback"] = new SolidColorBrush(Colors.LightGray);
            _serviceStatusBrushes["Chocks"] = new SolidColorBrush(Colors.LightGray);
        }

        /// <summary>
        /// Gets the status brush for a specific service
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <returns>Brush representing the service status</returns>
        private Brush GetServiceStatusBrush(string serviceName)
        {
            if (_serviceStatusBrushes.TryGetValue(serviceName, out var brush))
            {
                return brush;
            }

            return new SolidColorBrush(Colors.LightGray);
        }

        /// <summary>
        /// Updates the brush color for a service based on its status
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <param name="status">Service status</param>
        private void UpdateServiceStatusBrush(string serviceName, ServiceStatus status)
        {
            var brush = status switch
            {
                ServiceStatus.Completed => new SolidColorBrush(Colors.Green),
                ServiceStatus.Active => new SolidColorBrush(Colors.Gold),
                ServiceStatus.Waiting => new SolidColorBrush(Colors.Blue),
                ServiceStatus.Requested => new SolidColorBrush(Colors.Blue),
                ServiceStatus.Disconnected => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.LightGray)
            };

            _serviceStatusBrushes[serviceName] = brush;

            // Notify property changed for the appropriate property
            OnPropertyChanged($"{serviceName}StatusBrush");
        }

        /// <summary>
        /// Handles service status changed events
        /// </summary>
        /// <param name="evt">The service status changed event</param>
        private void OnServiceStatusChanged(ServiceStatusChangedEvent evt)
        {
            UpdateServiceStatusBrush(evt.ServiceName, evt.Status);
        }

        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            // Unsubscribe from events
            if (_serviceStatusChangedSubscription != null)
            {
                EventAggregator.Instance.Unsubscribe<ServiceStatusChangedEvent>(_serviceStatusChangedSubscription);
                _serviceStatusChangedSubscription = null;
            }
        }

        #endregion
    }
}
