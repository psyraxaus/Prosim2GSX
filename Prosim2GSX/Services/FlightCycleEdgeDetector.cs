namespace Prosim2GSX.Services
{
    // Tracks on-ground / engines-running transitions used as flight-cycle reset
    // triggers, replacing the duplicated _wasOnGround/_wasEnginesRunning fields
    // that several services each maintained by hand (and which drifted — see the
    // M8 Reset/ResetFlight fix). Call Update() once per tick with the current
    // readings; the edge properties then reflect the transition since the
    // previous Update(). Each owner keeps its own instance.
    public sealed class FlightCycleEdgeDetector
    {
        // _wasOnGround starts true to match the services' original field init
        // (they assume on-ground at construction so the first airborne tick is a
        // genuine liftoff edge, not a spurious one).
        private bool _wasOnGround = true;
        private bool _wasEnginesRunning;

        // True on the tick engines stop while on the ground — the common
        // flight-cycle-end / arrival reset trigger. (was-engines-running and
        // now on-ground & engines-off.)
        public bool EngineShutdownOnGround { get; private set; }

        // Stricter variant that additionally requires having been on the ground
        // the previous tick (won't fire if touchdown + shutdown land in the same
        // tick). DeiceHoldoverService uses this deliberately.
        public bool EngineShutdownOnGroundStable { get; private set; }

        // True on the tick the aircraft lifts off (was on ground, now airborne).
        public bool Liftoff { get; private set; }

        public void Update(bool onGround, bool enginesRunning)
        {
            EngineShutdownOnGround = onGround && _wasEnginesRunning && !enginesRunning;
            EngineShutdownOnGroundStable = _wasOnGround && _wasEnginesRunning && onGround && !enginesRunning;
            Liftoff = _wasOnGround && !onGround;

            _wasOnGround = onGround;
            _wasEnginesRunning = enginesRunning;
        }
    }
}
