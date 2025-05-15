using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.ViewModels.Base;
using System;
using System.Windows;
using System.Windows.Media;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for managing connection status indicators
    /// </summary>
    public class ConnectionStatusViewModel : ViewModelBase
    {
        private readonly ServiceModel _serviceModel;

        private Brush _msfsStatusBrush = new SolidColorBrush(Colors.Red);
        private Brush _simConnectStatusBrush = new SolidColorBrush(Colors.Red);
        private Brush _prosimStatusBrush = new SolidColorBrush(Colors.Red);
        private Brush _sessionStatusBrush = new SolidColorBrush(Colors.Red);

        /// <summary>
        /// Gets or sets the status indicator color for MSFS connection
        /// </summary>
        public Brush MsfsStatusBrush
        {
            get => _msfsStatusBrush;
            set => SetProperty(ref _msfsStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for SimConnect connection
        /// </summary>
        public Brush SimConnectStatusBrush
        {
            get => _simConnectStatusBrush;
            set => SetProperty(ref _simConnectStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Prosim connection
        /// </summary>
        public Brush ProsimStatusBrush
        {
            get => _prosimStatusBrush;
            set => SetProperty(ref _prosimStatusBrush, value);
        }

        /// <summary>
        /// Gets or sets the status indicator color for Session status
        /// </summary>
        public Brush SessionStatusBrush
        {
            get => _sessionStatusBrush;
            set => SetProperty(ref _sessionStatusBrush, value);
        }

        /// <summary>
        /// Subscription token for connection status events
        /// </summary>
        private SubscriptionToken _connectionStatusSubscription;

        /// <summary>
        /// Creates a new instance of the ConnectionStatusViewModel
        /// </summary>
        /// <param name="serviceModel">Service model containing connection state</param>
        public ConnectionStatusViewModel(ServiceModel serviceModel)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));

            // Subscribe to connection status events
            _connectionStatusSubscription = EventAggregator.Instance.Subscribe<ConnectionStatusChangedEvent>(OnConnectionStatusChanged);

            // Initialize status indicators
            UpdateConnectionStatus();
        }

        /// <summary>
        /// Updates all connection status indicators based on the current state
        /// </summary>
        public void UpdateConnectionStatus()
        {
            MsfsStatusBrush = _serviceModel.IsSimRunning ?
                new SolidColorBrush(Colors.Green) :
                new SolidColorBrush(Colors.Red);

            ProsimStatusBrush = _serviceModel.IsProsimRunning ?
                new SolidColorBrush(Colors.Green) :
                new SolidColorBrush(Colors.Red);

            SimConnectStatusBrush = IPCManager.SimConnect?.IsConnected == true ?
                new SolidColorBrush(Colors.Green) :
                new SolidColorBrush(Colors.Red);

            SessionStatusBrush = _serviceModel.IsSessionRunning ?
                new SolidColorBrush(Colors.Green) :
                new SolidColorBrush(Colors.Red);
        }

        /// <summary>
        /// Handles connection status changed events from the EventAggregator
        /// </summary>
        private void OnConnectionStatusChanged(ConnectionStatusChangedEvent evt)
        {
            ExecuteOnUIThread(() =>
            {
                switch (evt.ConnectionName)
                {
                    case "MSFS":
                        MsfsStatusBrush = evt.IsConnected ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red);
                        break;
                    case "SimConnect":
                        SimConnectStatusBrush = evt.IsConnected ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red);
                        break;
                    case "Prosim":
                        ProsimStatusBrush = evt.IsConnected ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red);
                        break;
                    case "Session":
                        SessionStatusBrush = evt.IsConnected ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red);
                        break;
                }
            });
        }

        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            // Unsubscribe from events
            if (_connectionStatusSubscription != null)
            {
                EventAggregator.Instance.Unsubscribe<ConnectionStatusChangedEvent>(_connectionStatusSubscription);
                _connectionStatusSubscription = null;
            }
        }
    }
}
