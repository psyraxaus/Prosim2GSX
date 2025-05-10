using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prosim2GSX.Events;

namespace Prosim2GSX.Services.GSX.Events
{
    /// <summary>
    /// Event raised when the fuel hose connection state changes
    /// </summary>
    public class FuelHoseStateChangedEvent : EventBase
    {
        /// <summary>
        /// Gets whether the fuel hose is connected
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Creates a new fuel hose state changed event
        /// </summary>
        /// <param name="isConnected">Whether the fuel hose is connected</param>
        public FuelHoseStateChangedEvent(bool isConnected)
        {
            IsConnected = isConnected;
        }
    }
}
