using Prosim2GSX.Services.GSX.Enums;

namespace Prosim2GSX.Events
{
    public class FlightPhaseChangedEvent : EventBase
    {
        public FlightState OldState { get; }
        public FlightState NewState { get; }

        public FlightPhaseChangedEvent(FlightState oldState, FlightState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
}
