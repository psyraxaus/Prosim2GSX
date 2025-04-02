using Prosim2GSX.Events;

public class FlightPlanChangedEvent : EventBase
{
    public string FlightNumber { get; }

    public FlightPlanChangedEvent(string flightNumber)
    {
        FlightNumber = flightNumber;
    }
}
