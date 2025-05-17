using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DataRefMonitoringService> _logger;

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

        public DataRefMonitoringService(ILogger<DataRefMonitoringService> logger, IProsimInterface prosimInterface)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prosimService = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
        }

        public void StartMonitoring()
        {
            if (IsMonitoringActive)
                return;

            IsMonitoringActive = true;
            _monitorCts = new CancellationTokenSource();

            _logger.LogInformation("DataRef monitoring system started");

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
                _logger.LogWarning(ex, "Exception during monitor shutdown");
            }

            IsMonitoringActive = false;
            _monitorCts.Dispose();
            _monitorTask = null;

            _logger.LogInformation("DataRef monitoring system stopped");
        }

        public void SetMonitoringInterval(int milliseconds)
        {
            if (milliseconds < 10)
                milliseconds = 10; // Enforce minimum to prevent excessive CPU usage

            _monitoringInterval = milliseconds;
            _logger.LogInformation("Monitoring interval set to {Interval}ms", milliseconds);
        }

        public void SubscribeToDataRef(string dataRef, DataRefChangedHandler handler)
        {
            if (string.IsNullOrEmpty(dataRef))
                throw new ArgumentNullException(nameof(dataRef));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _logger.LogInformation("Attempting to subscribe to dataRef: '{DataRef}'", dataRef);

            // Start the monitoring system if it's not already running
            if (!IsMonitoringActive)
            {
                _logger.LogInformation("Starting monitoring system for first subscription");
                StartMonitoring();
            }

            // Get the current value
            dynamic currentValue;
            try
            {
                currentValue = _prosimService.GetProsimVariable(dataRef);
                string currentValueStr = currentValue.ToString();
                _logger.LogDebug("Successfully read initial value for '{DataRef}': {Value}", dataRef, currentValueStr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading dataref '{DataRef}'", dataRef);
                return;
            }

            // Add or update the monitor
            lock (_monitoredDataRefs)
            {
                if (_monitoredDataRefs.TryGetValue(dataRef, out var monitor))
                {
                    // DataRef already being monitored, add this handler
                    monitor.Handlers.Add(handler);
                    _logger.LogDebug("Added handler to existing monitor for '{DataRef}'", dataRef);
                }
                else
                {
                    // Create a new monitor for this dataref
                    monitor = new DataRefMonitor(dataRef, currentValue);
                    monitor.Handlers.Add(handler);
                    _monitoredDataRefs.Add(dataRef, monitor);
                    string currentValueStr = currentValue.ToString();
                    _logger.LogInformation("Created new monitor for '{DataRef}', initial value: {Value}", dataRef, currentValueStr);
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
                    _logger.LogInformation("Removed all handlers for '{DataRef}'", dataRef);
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

                    _logger.LogDebug("Removed handler for '{DataRef}'", dataRef);
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
                _logger.LogDebug("Checking {Count} monitored datarefs", _monitoredDataRefs.Count);
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
                                _logger.LogError(ex, "Error in handler for '{DataRef}'", dataRef);
                            }
                        }

                        string oldValueStr = oldValue.ToString();
                        string currentValueStr = currentValue.ToString();
                        _logger.LogDebug("DataRef '{DataRef}' changed from {OldValue} to {NewValue}",
                            dataRef, oldValueStr, currentValueStr);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading dataref '{DataRef}'", dataRef);
                }
            }
        }
    }
}
