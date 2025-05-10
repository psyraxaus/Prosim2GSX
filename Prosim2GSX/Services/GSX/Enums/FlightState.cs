namespace Prosim2GSX.Services.GSX.Enums
{
    /// <summary>
    /// Represents the current phase of flight
    /// </summary>
    public enum FlightState
    {
        /// <summary>
        /// Aircraft is on the ground before departure, engines not running
        /// </summary>
        PREFLIGHT = 0,

        /// <summary>
        /// Aircraft is preparing for departure with ground services and boarding
        /// </summary>
        DEPARTURE,

        /// <summary>
        /// Aircraft is taxiing to the runway
        /// </summary>
        TAXIOUT,

        /// <summary>
        /// Aircraft is in flight
        /// </summary>
        FLIGHT,

        /// <summary>
        /// Aircraft is taxiing to the gate after landing
        /// </summary>
        TAXIIN,

        /// <summary>
        /// Aircraft has arrived at the gate with engines off
        /// </summary>
        ARRIVAL,

        /// <summary>
        /// Aircraft is between flights, preparing for the next departure
        /// </summary>
        TURNAROUND
    }
}