namespace Prosim2GSX.Events
{
    public class DataRefChangedEvent : EventBase
    {
        public string DataRef { get; }
        public dynamic OldValue { get; }
        public dynamic NewValue { get; }

        public DataRefChangedEvent(string dataRef, dynamic oldValue, dynamic newValue)
        {
            DataRef = dataRef;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
