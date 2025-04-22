using Prosim2GSX.Events;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.Prosim.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class DataRefMonitoringService : IDataRefMonitoringService
    {
        private readonly IProsimInterface _prosimService;

        // Dictionary to store monitored datarefs and their callbacks
        private readonly Dictionary<string, DataRefMonitor> _monitoredDataRefs = new Dictionary<string, DataRefMonitor>();
        private CancellationTokenSource _monitorCts;
        private Task _monitorTask;
        private int _monitoringInterval = 100; // Milliseconds between checks

        public bool IsMonitoringActive { get; private set; }

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

        public DataRefMonitoringService(IProsimInterface prosimInterface)
        {
            _prosimService = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
        }

        public void StartMonitoring()
        {
            if (IsMonitoringActive)
                return;

            IsMonitoringActive = true;
            _monitorCts = new CancellationTokenSource();

            Logger.Log(LogLevel.Information, nameof(DataRefMonitoringService),
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

        public void StopMonitoring()
        {
            if (!IsMonitoringActive)
                return;

            _monitorCts.Cancel();
            try
            {
                _monitorTask.Wait(1000); // Give it a second to shut down cleanly
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, nameof(DataRefMonitoringService),
                    $"Exception during monitor shutdown: {ex.Message}");
            }

            IsMonitoringActive = false;
            _monitorCts.Dispose();
            _monitorTask = null;

            Logger.Log(LogLevel.Information, nameof(DataRefMonitoringService),
                "DataRef monitoring system stopped");
        }

        public void SetMonitoringInterval(int milliseconds)
        {
            if (milliseconds < 10)
                milliseconds = 10; // Enforce minimum to prevent excessive CPU usage

            _monitoringInterval = milliseconds;
            Logger.Log(LogLevel.Information, nameof(DataRefMonitoringService),
                $"Monitoring interval set to {milliseconds}ms");
        }

        public void SubscribeToDataRef(string dataRef, DataRefChangedHandler handler)
        {
            if (string.IsNullOrEmpty(dataRef))
                throw new ArgumentNullException(nameof(dataRef));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Logger.Log(LogLevel.Information, nameof(DataRefMonitoringService),
                $"Attempting to subscribe to dataRef: '{dataRef}'");

            // Start the monitoring system if it's not already running
            if (!IsMonitoringActive)
            {
                Logger.Log(LogLevel.Information, nameof(DataRefMonitoringService),
                    $"Starting monitoring system for first subscription");
                StartMonitoring();
            }

            // Get the current value
            dynamic currentValue;
            try
            {
                currentValue = _prosimService.GetProsimVariable(dataRef);
                Logger.Log(LogLevel.Debug, nameof(DataRefMonitoringService),
                    $"Successfully read initial value for '{dataRef}': {currentValue}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(DataRefMonitoringService),
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
                    Logger.Log(LogLevel.Debug, nameof(DataRefMonitoringService),
                        $"Added handler to existing monitor for '{dataRef}'");
                }
                else
                {
                    // Create a new monitor for this dataref
                    monitor = new DataRefMonitor(dataRef, currentValue);
                    monitor.Handlers.Add(handler);
                    _monitoredDataRefs.Add(dataRef, monitor);
                    Logger.Log(LogLevel.Information, nameof(DataRefMonitoringService),
                        $"Created new monitor for '{dataRef}', initial value: {currentValue}");
                }
            }
        }

        public void UnsubscribeFromDataRef(string dataRef, DataRefChangedHandler handler = null)
        {
            if (string.IsNullOrEmpty(dataRef))
                return;

            lock (_monitoredDataRefs)
            {
                if (!_monitoredDataRefs.TryGetValue(dataRef, out var monitor))
                    return;

                if (handler == null)
                {
                    // Remove all subscriptions for this dataref
                    _monitoredDataRefs.Remove(dataRef);
                    Logger.Log(LogLevel.Information, nameof(DataRefMonitoringService),
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

                    Logger.Log(LogLevel.Debug, nameof(DataRefMonitoringService),
                        $"Removed handler for '{dataRef}'");
                }

                // If no more datarefs are being monitored, stop the system
                if (_monitoredDataRefs.Count == 0)
                {
                    StopMonitoring();
                }
            }
        }

        public IEnumerable<string> GetMonitoredDataRefs()
        {
            lock (_monitoredDataRefs)
            {
                return _monitoredDataRefs.Keys.ToList();
            }
        }

        private void CheckMonitoredDataRefs()
        {
            // Debug output to confirm the monitoring is running
            if (_monitoredDataRefs.Count > 0)
            {
                Logger.Log(LogLevel.Debug, nameof(DataRefMonitoringService),
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
                    dynamic currentValue = _prosimService.GetProsimVariable(dataRef);

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

                        // Publish event through the aggregator
                        EventAggregator.Instance.Publish(new DataRefChangedEvent(dataRef, oldValue, currentValue));

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
                                Logger.Log(LogLevel.Error, nameof(DataRefMonitoringService),
                                    $"Error in handler for '{dataRef}': {ex.Message}");
                            }
                        }

                        Logger.Log(LogLevel.Debug, nameof(DataRefMonitoringService),
                            $"DataRef '{dataRef}' changed from {oldValue} to {currentValue}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, nameof(DataRefMonitoringService),
                        $"Error reading dataref '{dataRef}': {ex.Message}");
                }
            }
        }
    }
}