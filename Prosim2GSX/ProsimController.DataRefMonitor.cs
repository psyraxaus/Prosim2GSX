using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX
{
    // Delegate for handling dataref change events
    public delegate void DataRefChangedHandler(string dataRef, dynamic oldValue, dynamic newValue);

    public partial class ProsimController
    {
        // Dictionary to store monitored datarefs and their callbacks
        private Dictionary<string, DataRefMonitor> _monitoredDataRefs = new Dictionary<string, DataRefMonitor>();
        private CancellationTokenSource _monitorCts;
        private Task _monitorTask;
        private int _monitoringInterval = 100; // Milliseconds between checks
        private bool _isMonitoringActive = false;

        // Class to track a monitored dataref
        private class DataRefMonitor
        {
            public string DataRef { get; }
            public dynamic LastValue { get; set; }
            public List<DataRefChangedHandler> Handlers { get; } = new List<DataRefChangedHandler>();

            public DataRefMonitor(string dataRef, dynamic initialValue)
            {
                DataRef = dataRef;
                LastValue = initialValue;
            }
        }

        /// <summary>
        /// Starts the dataref monitoring system
        /// </summary>
        public void StartDataRefMonitoring()
        {
            if (_isMonitoringActive)
                return;

            _isMonitoringActive = true;
            _monitorCts = new CancellationTokenSource();

            Logger.Log(LogLevel.Information, "ProsimController:StartDataRefMonitoring",
                "DataRef monitoring system started");

            _monitorTask = Task.Run(async () =>
            {
                while (!_monitorCts.Token.IsCancellationRequested)
                {
                    CheckMonitoredDataRefs();
                    await Task.Delay(_monitoringInterval, _monitorCts.Token);
                }
            }, _monitorCts.Token);
        }

        /// <summary>
        /// Stops the dataref monitoring system
        /// </summary>
        public void StopDataRefMonitoring()
        {
            if (!_isMonitoringActive)
                return;

            _monitorCts.Cancel();
            try
            {
                _monitorTask.Wait(1000); // Give it a second to shut down cleanly
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ProsimController:StopDataRefMonitoring",
                    $"Exception during monitor shutdown: {ex.Message}");
            }

            _isMonitoringActive = false;
            _monitorCts.Dispose();
            _monitorTask = null;

            Logger.Log(LogLevel.Information, "ProsimController:StopDataRefMonitoring",
                "DataRef monitoring system stopped");
        }

        /// <summary>
        /// Sets the interval for checking dataref changes (in milliseconds)
        /// </summary>
        public void SetMonitoringInterval(int milliseconds)
        {
            if (milliseconds < 10)
                milliseconds = 10; // Enforce minimum to prevent excessive CPU usage

            _monitoringInterval = milliseconds;
            Logger.Log(LogLevel.Information, "ProsimController:SetMonitoringInterval",
                $"Monitoring interval set to {milliseconds}ms");
        }

        /// <summary>
        /// Subscribe to changes in a specific ProSim dataref
        /// </summary>
        /// <param name="dataRef">The ProSim dataref to monitor</param>
        /// <param name="handler">Callback function to invoke when the dataref changes</param>
        public void SubscribeToDataRef(string dataRef, DataRefChangedHandler handler)
        {
            Logger.Log(LogLevel.Information, "ProsimController:SubscribeToDataRef", $"Attempting to subscribe to dataRef: '{dataRef}'");

            // Start the monitoring system if it's not already running
            if (!_isMonitoringActive)
            {
                Logger.Log(LogLevel.Information, "ProsimController:SubscribeToDataRef",
                    $"Starting monitoring system for first subscription");
                StartDataRefMonitoring();
            }

            // Get the current value
            dynamic currentValue;
            try
            {
                currentValue = Interface.ReadDataRef(dataRef);
                Logger.Log(LogLevel.Debug, "ProsimController:SubscribeToDataRef",
                    $"Successfully read initial value for '{dataRef}': {currentValue}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimController:SubscribeToDataRef",
                    $"Error reading dataref '{dataRef}': {ex.Message}");
                return;
            }

            // Start the monitoring system if it's not already running
            if (!_isMonitoringActive)
                StartDataRefMonitoring();

            try
            {
                currentValue = Interface.ReadDataRef(dataRef);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimController:SubscribeToDataRef",
                    $"Error reading dataref '{dataRef}': {ex.Message}");
                return;
            }

            // Add or update the monitor
            lock (_monitoredDataRefs)
            {
                if (_monitoredDataRefs.TryGetValue(dataRef, out var monitor))
                {
                    // DataRef already being monitored, add this handler
                    monitor.Handlers.Add(handler);
                    Logger.Log(LogLevel.Debug, "ProsimController:SubscribeToDataRef",
                        $"Added handler to existing monitor for '{dataRef}'");
                }
                else
                {
                    // Create a new monitor for this dataref
                    monitor = new DataRefMonitor(dataRef, currentValue);
                    monitor.Handlers.Add(handler);
                    _monitoredDataRefs.Add(dataRef, monitor);
                    Logger.Log(LogLevel.Information, "ProsimController:SubscribeToDataRef",
                        $"Created new monitor for '{dataRef}', initial value: {currentValue}");
                }
            }
        }

        /// <summary>
        /// Unsubscribe from changes to a specific ProSim dataref
        /// </summary>
        /// <param name="dataRef">The ProSim dataref</param>
        /// <param name="handler">The handler to remove (null to remove all handlers)</param>
        public void UnsubscribeFromDataRef(string dataRef, DataRefChangedHandler handler = null)
        {
            lock (_monitoredDataRefs)
            {
                if (!_monitoredDataRefs.TryGetValue(dataRef, out var monitor))
                    return;

                if (handler == null)
                {
                    // Remove all subscriptions for this dataref
                    _monitoredDataRefs.Remove(dataRef);
                    Logger.Log(LogLevel.Information, "ProsimController:UnsubscribeFromDataRef",
                        $"Removed all handlers for '{dataRef}'");
                }
                else
                {
                    // Remove specific handler
                    monitor.Handlers.Remove(handler);

                    // If no handlers left, remove the entire monitor
                    if (monitor.Handlers.Count == 0)
                    {
                        _monitoredDataRefs.Remove(dataRef);
                    }

                    Logger.Log(LogLevel.Debug, "ProsimController:UnsubscribeFromDataRef",
                        $"Removed handler for '{dataRef}'");
                }

                // If no more datarefs are being monitored, stop the system
                if (_monitoredDataRefs.Count == 0)
                {
                    StopDataRefMonitoring();
                }
            }
        }

        /// <summary>
        /// Check all monitored datarefs for changes
        /// </summary>
        private void CheckMonitoredDataRefs()
        {
            // Debug output to confirm the monitoring is running
            if (_monitoredDataRefs.Count > 0)
            {
                Logger.Log(LogLevel.Debug, "ProsimController:CheckMonitoredDataRefs",
                    $"Checking {_monitoredDataRefs.Count} monitored datarefs");
            }

            // Create a local copy of keys to avoid collection modification issues
            List<string> dataRefsToCheck;
            lock (_monitoredDataRefs)
            {
                dataRefsToCheck = new List<string>(_monitoredDataRefs.Keys);
            }

            foreach (var dataRef in dataRefsToCheck)
            {
                try
                {
                    dynamic currentValue = Interface.ReadDataRef(dataRef);

                    DataRefMonitor monitor;
                    lock (_monitoredDataRefs)
                    {
                        if (!_monitoredDataRefs.TryGetValue(dataRef, out monitor))
                            continue; // Monitor was removed
                    }

                    // Check if value has changed
                    if (!Equals(currentValue, monitor.LastValue))
                    {
                        dynamic oldValue = monitor.LastValue;
                        monitor.LastValue = currentValue;

                        // Call all registered handlers
                        List<DataRefChangedHandler> handlers;
                        lock (_monitoredDataRefs)
                        {
                            handlers = new List<DataRefChangedHandler>(monitor.Handlers);
                        }

                        foreach (var handler in handlers)
                        {
                            try
                            {
                                handler(dataRef, oldValue, currentValue);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogLevel.Error, "ProsimController:CheckMonitoredDataRefs",
                                    $"Error in handler for '{dataRef}': {ex.Message}");
                            }
                        }

                        Logger.Log(LogLevel.Debug, "ProsimController:CheckMonitoredDataRefs",
                            $"DataRef '{dataRef}' changed from {oldValue} to {currentValue}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "ProsimController:CheckMonitoredDataRefs",
                        $"Error reading dataref '{dataRef}': {ex.Message}");
                }
            }
        }
    }
}
