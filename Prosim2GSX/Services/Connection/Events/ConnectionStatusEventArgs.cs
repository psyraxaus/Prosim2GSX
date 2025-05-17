using System;
using Prosim2GSX.Services.Connection.Enums;

namespace Prosim2GSX.Services.Connection.Events
{
    /// <summary>
    /// Event arguments for connection status changes
    /// </summary>
    public class ConnectionStatusEventArgs : EventArgs
    {
        /// <summary>
        /// The type of connection that changed
        /// </summary>
        public ConnectionType ConnectionType { get; }

        /// <summary>
        /// Whether the connection is established
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Creates a new instance of ConnectionStatusEventArgs
        /// </summary>
        /// <param name="connectionType">The type of connection</param>
        /// <param name="isConnected">Whether the connection is established</param>
        public ConnectionStatusEventArgs(ConnectionType connectionType, bool isConnected)
        {
            ConnectionType = connectionType;
            IsConnected = isConnected;
        }
    }
}