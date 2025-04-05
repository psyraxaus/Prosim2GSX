using System;

namespace Prosim2GSX.Events
{
    public class SimbriefIdRequiredEvent : EventBase
    {
        public bool AutoSwitchToSettings { get; set; } = true;
    }
}
