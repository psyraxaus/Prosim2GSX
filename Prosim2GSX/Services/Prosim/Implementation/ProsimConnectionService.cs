using Microsoft.Extensions.Logging;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Threading;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class ProsimConnectionService : IProsimConnectionService
    {
        private readonly IProsimInterface _prosimService;
        private readonly ServiceModel _model;
        private readonly ILogger<ProsimConnectionService> _logger;
        private static readonly int _waitDuration = 30000; // 30 seconds

        public bool IsConnected => _prosimService.IsProsimReady();

        public ProsimConnectionService(
            ILogger<ProsimConnectionService> logger,
            IProsimInterface prosimInterface,
            ServiceModel model)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prosimService = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public bool Initialize()
        {
            try
            {
                // Any initialization logic goes here
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing");
                return false;
            }
        }

        public bool Connect()
        {
            try
            {
                _prosimService.ConnectProsimSDK();
                bool isConnected = _prosimService.IsProsimReady();

                // Publish connection status
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Prosim", isConnected));

                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting");
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Prosim", false));
                return false;
            }
        }

        public void Disconnect()
        {
            // ProSimSDK doesn't have a specific disconnect method,
            // but we can clear out any resources if needed
            EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Prosim", false));
        }

        public bool WaitForAvailability(Func<bool> cancellationRequested = null)
        {
            Thread.Sleep(250);
            Connect();
            Thread.Sleep(5000);

            bool isProsimReady = IsConnected;
            _logger.LogDebug("Prosim Available: {IsProsimReady}", isProsimReady);

            // If already connected or not running, return immediately
            if (isProsimReady || !_model.IsSimRunning)
            {
                return isProsimReady;
            }

            // Function to check if cancellation is requested
            Func<bool> checkCancellation = cancellationRequested ?? (() => _model.CancellationRequested);

            // Wait for connection (polling)
            while (_model.IsSimRunning && !isProsimReady && !checkCancellation())
            {
                _logger.LogInformation("Is Prosim available? {IsProsimReady} - waiting {WaitDuration}s for Retry",
                    isProsimReady, _waitDuration / 1000);

                Connect();
                Thread.Sleep(_waitDuration);
                isProsimReady = IsConnected;
            }

            if (!isProsimReady || !_model.IsSimRunning)
            {
                _logger.LogError("Prosim not available - aborting");
                return false;
            }

            return true;
        }
    }
}
