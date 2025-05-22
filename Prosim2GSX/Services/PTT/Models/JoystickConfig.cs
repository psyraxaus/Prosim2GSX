namespace Prosim2GSX.Services.PTT.Models
{
    /// <summary>
    /// Configuration for a joystick button used for PTT
    /// </summary>
    public class JoystickConfig
    {
        /// <summary>
        /// Gets the joystick ID
        /// </summary>
        public int JoystickId { get; }

        /// <summary>
        /// Gets the button ID
        /// </summary>
        public int ButtonId { get; }

        /// <summary>
        /// Gets the joystick name
        /// </summary>
        public string JoystickName { get; }

        /// <summary>
        /// Creates a new instance of JoystickConfig
        /// </summary>
        /// <param name="joystickId">The joystick ID</param>
        /// <param name="buttonId">The button ID</param>
        /// <param name="joystickName">The joystick name</param>
        public JoystickConfig(int joystickId, int buttonId, string joystickName)
        {
            JoystickId = joystickId;
            ButtonId = buttonId;
            JoystickName = joystickName;
        }

        /// <summary>
        /// Returns a string representation of this joystick configuration
        /// </summary>
        public override string ToString()
        {
            return $"{JoystickName} (Button {ButtonId + 1})";
        }
    }
}
