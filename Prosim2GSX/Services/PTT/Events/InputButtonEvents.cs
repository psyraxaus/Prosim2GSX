using Prosim2GSX.Services.PTT.Enums;
using System;

namespace Prosim2GSX.Services.PTT.Events
{
    /// <summary>
    /// Event arguments for button detection events
    /// </summary>
    public class InputButtonEvents : EventArgs
    {
        /// <summary>
        /// The type of device that detected the button press
        /// </summary>
        public PttInputDeviceType DeviceType { get; set; }

        /// <summary>
        /// A unique identifier for the button that was pressed
        /// </summary>
        public string ButtonIdentifier { get; set; }

        /// <summary>
        /// A user-friendly display name for the button that was pressed
        /// </summary>
        public string DisplayName { get; set; }
    }
}
