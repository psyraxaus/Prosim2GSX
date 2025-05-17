using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.GSX;

namespace Prosim2GSX
{
    public static class IPCManager
    {
        private static ILogger _logger;

        public static readonly int waitDuration = 10000;

        public static MobiSimConnect SimConnect { get; set; } = null;
        /// <summary>
        /// Get or set the GSX controller instance
        /// </summary>
        public static GsxController GsxController { get; set; }
        public static ServiceController ServiceController { get; set; } = null;

        /// <summary>
        /// Initializes the IPCManager with a logger factory
        /// </summary>
        /// <param name="loggerFactory">The logger factory to create loggers</param>
        public static void Initialize(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger("IPCManager") ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public static bool WaitForSimulator(ServiceModel model)
        {
            bool simRunning = IsSimRunning();

            // Publish event immediately when simulator is detected
            if (simRunning)
            {
                model.IsSimRunning = true;
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", simRunning));
                _logger?.LogInformation("Simulator started {SimRunning}", simRunning);
            }

            if (!simRunning && model.WaitForConnect)
            {
                do
                {
                    _logger?.LogInformation("Simulator not started - waiting {Seconds}s for Sim", waitDuration / 1000);
                    Thread.Sleep(waitDuration);
                }
                while (!IsSimRunning() && !model.CancellationRequested);

                Thread.Sleep(waitDuration);
                return true;
            }
            else if (simRunning)
            {
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", simRunning));
                _logger?.LogInformation("Simulator started {SimRunning}", simRunning);
                return true;
            }
            else
            {
                _logger?.LogError("Simulator not started - aborting");
                return false;
            }
        }

        public static bool IsProcessRunning(string processName)
        {
            try
            {
                // For a single process name, use the original implementation
                Process[] processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if process {ProcessName} is running", processName);
                return false;
            }
        }

        public static bool IsSimRunning()
        {
            return IsProcessRunning("FlightSimulator");
        }

        public static bool WaitForConnection(ServiceModel model)
        {
            if (!IsSimRunning())
                return false;

            SimConnect = new MobiSimConnect();
            bool mobiRequested = SimConnect.Connect();
            EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("SimConnect", SimConnect.IsConnected));

            if (!SimConnect.IsConnected)
            {
                do
                {
                    _logger?.LogInformation("Connection not established - waiting {Seconds}s for Retry", waitDuration / 2000);
                    Thread.Sleep(waitDuration / 2);
                    if (!mobiRequested)
                        mobiRequested = SimConnect.Connect();
                }
                while (!SimConnect.IsConnected && IsSimRunning() && !model.CancellationRequested);

                return SimConnect.IsConnected;
            }
            else
            {
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("SimConnect", SimConnect.IsConnected));
                _logger?.LogInformation("SimConnect is opened");
                return true;
            }
        }

        public static bool WaitForSessionReady(ServiceModel model)
        {
            int waitDuration = 5000;
            SimConnect.SubscribeSimVar("CAMERA STATE", "Enum");
            Thread.Sleep(250);
            bool isReady = IsCamReady();
            while (IsSimRunning() && !isReady && !model.CancellationRequested)
            {
                _logger?.LogInformation("Session not ready - waiting {Seconds}s for Retry", waitDuration / 1000);
                Thread.Sleep(waitDuration);
                isReady = IsCamReady();
            }

            if (!isReady)
            {
                _logger?.LogError("SimConnect or Simulator not available - aborting");
                return false;
            }
            EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Session", isReady));
            return true;
        }

        public static bool IsCamReady()
        {
            float value = SimConnect.ReadSimVar("CAMERA STATE", "Enum");

            return value >= 2 && value <= 5;
        }

        public static void CloseSafe()
        {
            try
            {
                if (SimConnect != null)
                {
                    _logger?.LogDebug("Closing SimConnect connection");
                    SimConnect.Disconnect();
                    SimConnect = null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error closing SimConnect connection");
            }
        }
    }
}
