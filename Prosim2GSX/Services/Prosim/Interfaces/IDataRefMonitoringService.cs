using Prosim2GSX.Events;
using Prosim2GSX.Services.Prosim.Models;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for monitoring ProSim datarefs
    /// </summary>
    public interface IDataRefMonitoringService
    {
        /// <summary>
        /// Whether monitoring is currently active
        /// </summary>
        bool IsMonitoringActive { get; }

        /// <summary>
        /// Start dataref monitoring
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stop dataref monitoring
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Set the interval for dataref checks
        /// </summary>
        /// <param name="milliseconds">Interval in milliseconds</param>
        void SetMonitoringInterval(int milliseconds);

        /// <summary>
        /// Subscribe to changes in a specific dataref
        /// </summary>
        /// <param name="dataRef">The dataref to monitor</param>
        /// <param name="handler">Callback function when dataref changes</param>
        void SubscribeToDataRef(string dataRef, DataRefChangedHandler handler);

        /// <summary>
        /// Unsubscribe from changes to a specific dataref
        /// </summary>
        /// <param name="dataRef">The dataref</param>
        /// <param name="handler">The handler to remove (null to remove all)</param>
        void UnsubscribeFromDataRef(string dataRef, DataRefChangedHandler handler = null);

        /// <summary>
        /// Get a list of all currently monitored datarefs
        /// </summary>
        /// <returns>List of currently monitored datarefs</returns>
        IEnumerable<string> GetMonitoredDataRefs();
    }
}