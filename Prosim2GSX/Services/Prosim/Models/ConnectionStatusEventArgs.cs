using System;

namespace Prosim2GSX.Services.Prosim.Models
{
    /// <summary>
    /// Event arguments for connection status changes
    /// </summary>
    public class ConnectionStatusEventArgs : EventArgs
    {
        /// <summary>
        /// The connection name
        /// </summary>
        public string ConnectionName { get; }

        /// <summary>
        /// Whether the connection is available
        /// </summary>
        public bool IsAvailable { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectionStatusEventArgs(string connectionName, bool isAvailable)
        {
            ConnectionName = connectionName;
            IsAvailable = isAvailable;
        }
    }
}