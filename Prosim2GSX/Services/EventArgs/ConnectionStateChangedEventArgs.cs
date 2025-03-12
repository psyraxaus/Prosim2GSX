using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for connection state changes
    /// </summary>
    public class ConnectionStateChangedEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets a value indicating whether the connection is established
        /// </summary>
        public bool IsConnected { get; }
        
        /// <summary>
        /// Gets the connection type
        /// </summary>
        public string ConnectionType { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="isConnected">Whether the connection is established</param>
        /// <param name="connectionType">The connection type</param>
        public ConnectionStateChangedEventArgs(bool isConnected, string connectionType = "ProSim")
        {
            IsConnected = isConnected;
            ConnectionType = connectionType;
        }
    }
}
