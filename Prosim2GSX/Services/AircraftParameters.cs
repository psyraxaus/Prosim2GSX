namespace Prosim2GSX.Services
{
    /// <summary>
    /// Parameters describing the current aircraft state
    /// </summary>
    public class AircraftParameters
    {
        /// <summary>
        /// Gets or sets whether the aircraft is on the ground
        /// </summary>
        public bool OnGround { get; set; }
        
        /// <summary>
        /// Gets or sets whether the engines are running
        /// </summary>
        public bool EnginesRunning { get; set; }
        
        /// <summary>
        /// Gets or sets whether the parking brake is set
        /// </summary>
        public bool ParkingBrakeSet { get; set; }
        
        /// <summary>
        /// Gets or sets whether the beacon is on
        /// </summary>
        public bool BeaconOn { get; set; }
        
        /// <summary>
        /// Gets or sets the ground speed in knots
        /// </summary>
        public float GroundSpeed { get; set; }
        
        /// <summary>
        /// Gets or sets the altitude in feet
        /// </summary>
        public float Altitude { get; set; }
        
        /// <summary>
        /// Gets or sets whether ground equipment is connected
        /// </summary>
        public bool GroundEquipmentConnected { get; set; }
        
        /// <summary>
        /// Gets or sets whether a flight plan is loaded
        /// </summary>
        public bool FlightPlanLoaded { get; set; }
    }
}
