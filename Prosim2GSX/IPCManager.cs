using System.Diagnostics;
using System.Linq;
using System.Threading;
using Prosim2GSX.Events;
using Prosim2GSX.Models;

namespace Prosim2GSX
{
    public static class IPCManager
    {
        public static readonly int waitDuration = 10000;

        public static MobiSimConnect SimConnect { get; set; } = null;
        public static GsxController GsxController { get; set; } = null;

        public static bool WaitForSimulator(ServiceModel model)
        {
            bool simRunning = IsSimRunning();

            // Publish event immediately when simulator is detected
            if (simRunning)
            {
                model.IsSimRunning = true;
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", simRunning));
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator started {simRunning}");
            }

            if (!simRunning && model.WaitForConnect)
            {
                do
                {
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator not started - waiting {waitDuration / 1000}s for Sim");
                    Thread.Sleep(waitDuration);
                }
                while (!IsSimRunning() && !model.CancellationRequested);

                Thread.Sleep(waitDuration);
                return true;
            }
            else if (simRunning)
            {
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", simRunning));
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator started {simRunning}");
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForSimulator", $"Simulator not started - aborting");
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
            catch
            {
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
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"Connection not established - waiting {waitDuration / 1000}s for Retry");
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
                Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"SimConnect is opened");
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
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSessionReady", $"Session not ready - waiting {waitDuration / 1000}s for Retry");
                Thread.Sleep(waitDuration);
                isReady = IsCamReady();
            }

            if (!isReady)
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForSessionReady", $"SimConnect or Simulator not available - aborting");
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
                    SimConnect.Disconnect();
                    SimConnect = null;
                }
            }
            catch { }
        }
    }
}
