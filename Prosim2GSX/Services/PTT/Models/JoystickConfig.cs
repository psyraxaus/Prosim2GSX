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
        /// Gets the original joystick name from the system
        /// </summary>
        public string JoystickName { get; }

        /// <summary>
        /// Gets the resolved joystick name (actual device name if available)
        /// </summary>
        public string ResolvedName { get; }

        /// <summary>
        /// Gets the display name (resolved name if available, otherwise original name)
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(ResolvedName) ? ResolvedName : JoystickName;

        /// <summary>
        /// Creates a new instance of JoystickConfig
        /// </summary>
        /// <param name="joystickId">The joystick ID</param>
        /// <param name="buttonId">The button ID</param>
        /// <param name="joystickName">The original joystick name</param>
        /// <param name="resolvedName">The resolved joystick name (optional)</param>
        public JoystickConfig(int joystickId, int buttonId, string joystickName, string resolvedName = null)
        {
            JoystickId = joystickId;
            ButtonId = buttonId;
            JoystickName = joystickName;
            ResolvedName = resolvedName;
        }

        /// <summary>
        /// Returns a string representation of this joystick configuration
        /// </summary>
        public override string ToString()
        {
            return $"{DisplayName} (Button {ButtonId + 1})";
        }
    }
}
