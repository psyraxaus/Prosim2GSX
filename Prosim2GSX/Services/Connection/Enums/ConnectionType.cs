namespace Prosim2GSX.Services.Connection.Enum
{
    /// <summary>
    /// Represents the types of connections the application manages
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// Connection to Microsoft Flight Simulator process
        /// </summary>
        FlightSimulator,

        /// <summary>
        /// Connection to SimConnect API
        /// </summary>
        SimConnect,

        /// <summary>
        /// Connection to Prosim737 application
        /// </summary>
        Prosim,

        /// <summary>
        /// Connection to active simulator session
        /// </summary>
        Session
    }
}