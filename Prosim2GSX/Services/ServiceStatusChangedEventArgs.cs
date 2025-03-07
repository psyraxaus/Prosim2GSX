using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for service status changes
    /// </summary>
    public class ServiceStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of service
        /// </summary>
        public string ServiceType { get; }
        
        /// <summary>
        /// Gets the current status of the service
        /// </summary>
        public string Status { get; }
        
        /// <summary>
        /// Gets whether the service is completed
        /// </summary>
        public bool IsCompleted { get; }
        
        /// <summary>
        /// Gets the timestamp of the status change
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Creates a new instance of ServiceStatusChangedEventArgs
        /// </summary>
        /// <param name="serviceType">The type of service</param>
        /// <param name="status">The current status of the service</param>
        /// <param name="isCompleted">Whether the service is completed</param>
        public ServiceStatusChangedEventArgs(string serviceType, string status, bool isCompleted)
        {
            ServiceType = serviceType;
            Status = status;
            IsCompleted = isCompleted;
            Timestamp = DateTime.Now;
        }
    }
}
