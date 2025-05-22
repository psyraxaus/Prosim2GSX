using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services.PTT.Enums;
using System;

namespace Prosim2GSX.Services.PTT.Events
{
    /// <summary>
    /// Event arguments for PTT input detection events
    /// </summary>
    public class PttInputDetectedEvents : EventArgs
    {
        /// <summary>
        /// The type of input device
        /// </summary>
        public PttInputDeviceType DeviceType { get; set; }

        /// <summary>
        /// Identifier for the detected button
        /// </summary>
        public string ButtonIdentifier { get; set; }

        /// <summary>
        /// Display name for the detected button
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The channel being configured (if applicable)
        /// </summary>
        public AudioChannel? Channel { get; set; }
    }
}
