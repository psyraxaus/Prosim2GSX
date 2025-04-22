namespace Prosim2GSX.Services.GSX.Models
{
    /// <summary>
    /// Represents the state of a GSX service
    /// </summary>
    public static class GsxServiceState
    {
        /// <summary>
        /// Service is available
        /// </summary>
        public const float Available = 1;

        /// <summary>
        /// Service is unavailable
        /// </summary>
        public const float Unavailable = 2;

        /// <summary>
        /// Service was bypassed
        /// </summary>
        public const float Bypassed = 3;

        /// <summary>
        /// Service was requested
        /// </summary>
        public const float Requested = 4;

        /// <summary>
        /// Service is active
        /// </summary>
        public const float Active = 5;

        /// <summary>
        /// Service is completed
        /// </summary>
        public const float Completed = 6;

        /// <summary>
        /// Toggle service on
        /// </summary>
        public const float ToggleOn = 1;

        /// <summary>
        /// Toggle service off
        /// </summary>
        public const float ToggleOff = 0;
    }
}