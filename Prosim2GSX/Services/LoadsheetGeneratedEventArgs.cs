using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for loadsheet generation events
    /// </summary>
    public class LoadsheetGeneratedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of loadsheet that was generated
        /// </summary>
        public string LoadsheetType { get; }
        
        /// <summary>
        /// Gets the flight number for the loadsheet
        /// </summary>
        public string FlightNumber { get; }
        
        /// <summary>
        /// Gets the timestamp when the loadsheet was generated
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Gets whether the loadsheet generation was successful
        /// </summary>
        public bool Success { get; }
        
        /// <summary>
        /// Initializes a new instance of the LoadsheetGeneratedEventArgs class
        /// </summary>
        /// <param name="loadsheetType">The type of loadsheet that was generated</param>
        /// <param name="flightNumber">The flight number for the loadsheet</param>
        /// <param name="success">Whether the loadsheet generation was successful</param>
        public LoadsheetGeneratedEventArgs(string loadsheetType, string flightNumber, bool success)
        {
            LoadsheetType = loadsheetType;
            FlightNumber = flightNumber;
            Timestamp = DateTime.Now;
            Success = success;
        }
    }
}
