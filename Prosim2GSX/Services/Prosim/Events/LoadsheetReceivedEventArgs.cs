using System;

namespace Prosim2GSX.Services.Prosim.Events
{
    /// <summary>
    /// Event arguments for loadsheet received events
    /// </summary>
    public class LoadsheetReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Type of loadsheet (Preliminary or Final)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Loadsheet data
        /// </summary>
        public dynamic Data { get; set; }
    }
}