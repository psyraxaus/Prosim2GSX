namespace Prosim2GSX.Services.Logging
{
    /// <summary>
    /// Defines standard logging categories for the application.
    /// These categories replace the custom LogCategory enum
    /// and provide structured filtering capabilities.
    /// </summary>
    public static class LogCategories
    {
        /// <summary>
        /// Base namespace for all Prosim2GSX logs
        /// </summary>
        public const string Base = "Prosim2GSX";

        /// <summary>
        /// GSX Controller logs
        /// </summary>
        public const string GsxController = Base + ".GsxController";

        /// <summary>
        /// Refueling service logs
        /// </summary>
        public const string Refueling = Base + ".Refueling";

        /// <summary>
        /// Boarding service logs
        /// </summary>
        public const string Boarding = Base + ".Boarding";

        /// <summary>
        /// Catering service logs
        /// </summary>
        public const string Catering = Base + ".Catering";

        /// <summary>
        /// Ground services logs
        /// </summary>
        public const string GroundServices = Base + ".GroundServices";

        /// <summary>
        /// SimConnect interface logs
        /// </summary>
        public const string SimConnect = Base + ".SimConnect";

        /// <summary>
        /// Prosim interface logs
        /// </summary>
        public const string Prosim = Base + ".Prosim";

        /// <summary>
        /// Event system logs
        /// </summary>
        public const string Events = Base + ".Events";

        /// <summary>
        /// Menu system logs
        /// </summary>
        public const string Menu = Base + ".Menu";

        /// <summary>
        /// Audio system logs
        /// </summary>
        public const string Audio = Base + ".Audio";

        /// <summary>
        /// Configuration logs
        /// </summary>
        public const string Configuration = Base + ".Configuration";

        /// <summary>
        /// Door operations logs
        /// </summary>
        public const string Doors = Base + ".Doors";

        /// <summary>
        /// Cargo operations logs
        /// </summary>
        public const string Cargo = Base + ".Cargo";

        /// <summary>
        /// Loadsheet operations logs
        /// </summary>
        public const string Loadsheet = Base + ".Loadsheet";
    }
}
