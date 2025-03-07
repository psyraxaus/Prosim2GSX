using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for the GSX controller facade that coordinates GSX integration with ProsimA320
    /// </summary>
    public interface IGSXControllerFacade : IDisposable
    {
        /// <summary>
        /// Gets the current flight state
        /// </summary>
        FlightState CurrentFlightState { get; }
        
        /// <summary>
        /// Gets or sets the interval between service runs in milliseconds
        /// </summary>
        int Interval { get; set; }
        
        /// <summary>
        /// Runs GSX services based on the current flight state
        /// </summary>
        void RunServices();
        
        /// <summary>
        /// Resets audio settings to default
        /// </summary>
        void ResetAudio();
        
        /// <summary>
        /// Controls audio based on cockpit controls
        /// </summary>
        void ControlAudio();
        
        /// <summary>
        /// Event raised when the flight state changes
        /// </summary>
        event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// Event raised when a service status changes
        /// </summary>
        event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;
    }
}
