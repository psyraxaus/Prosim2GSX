using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Base class for all event arguments in the Prosim2GSX application
    /// </summary>
    public abstract class BaseEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the timestamp of the event
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseEventArgs"/> class
        /// </summary>
        protected BaseEventArgs()
        {
            Timestamp = DateTime.Now;
        }
    }
}
