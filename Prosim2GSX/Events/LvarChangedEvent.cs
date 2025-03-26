namespace Prosim2GSX.Events
{
    public class LvarChangedEvent : EventBase
    {
        public string LvarName { get; }
        public float OldValue { get; }
        public float NewValue { get; }

        public LvarChangedEvent(string lvarName, float oldValue, float newValue)
        {
            LvarName = lvarName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
