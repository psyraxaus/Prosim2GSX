using Prosim2GSX.Services.PTT.Enums;
using Prosim2GSX.Services.PTT.Events;
using System;

namespace Prosim2GSX.Services.PTT.Interface
{
    /// <summary>
    /// Interface for input devices that can be used for PTT activation
    /// </summary>
    public interface IInputDevice
    {
        /// <summary>
        /// Gets the type of input device
        /// </summary>
        PttInputDeviceType DeviceType { get; }

        /// <summary>
        /// Event raised when a button is detected during detection mode
        /// </summary>
        event EventHandler<InputButtonEvents> ButtonDetected;

        /// <summary>
        /// Start listening for button presses in detection mode
        /// </summary>
        void StartDetection();

        /// <summary>
        /// Stop listening for button presses
        /// </summary>
        void StopDetection();

        /// <summary>
        /// Check if a specific button is currently pressed
        /// </summary>
        /// <param name="buttonIdentifier">The identifier of the button to check</param>
        /// <returns>True if the button is pressed, false otherwise</returns>
        bool IsButtonPressed(string buttonIdentifier);
    }
}
