using System;

namespace Prosim2GSX.Services.Prosim.Models
{
    /// <summary>
    /// Event arguments for when a dataref changes
    /// </summary>
    public class DataRefChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The dataref that changed
        /// </summary>
        public string DataRef { get; }

        /// <summary>
        /// The old value
        /// </summary>
        public dynamic OldValue { get; }

        /// <summary>
        /// The new value
        /// </summary>
        public dynamic NewValue { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DataRefChangedEventArgs(string dataRef, dynamic oldValue, dynamic newValue)
        {
            DataRef = dataRef;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}