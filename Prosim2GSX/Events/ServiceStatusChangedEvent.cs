namespace Prosim2GSX.Events
{
    public class ServiceStatusChangedEvent : EventBase
    {
        public string ServiceName { get; }
        public ServiceStatus Status { get; }

        public ServiceStatusChangedEvent(string serviceName, ServiceStatus status)
        {
            ServiceName = serviceName;
            Status = status;
        }
    }

    public enum ServiceStatus
    {
        Inactive,
        Requested,
        Disconnected,
        Waiting,
        Active,
        Completed
    }
}
