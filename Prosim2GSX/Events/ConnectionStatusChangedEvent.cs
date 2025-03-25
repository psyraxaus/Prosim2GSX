namespace Prosim2GSX.Events
{
    public class ConnectionStatusChangedEvent : EventBase
    {
        public string ConnectionName { get; }
        public bool IsConnected { get; }

        public ConnectionStatusChangedEvent(string connectionName, bool isConnected)
        {
            ConnectionName = connectionName;
            IsConnected = isConnected;
        }
    }
}
