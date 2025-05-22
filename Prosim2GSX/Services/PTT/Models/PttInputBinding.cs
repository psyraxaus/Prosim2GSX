namespace Prosim2GSX.Services.PTT.Models
{
    /// <summary>
    /// Represents a binding for a PTT input device
    /// </summary>
    public class PttInputBinding
    {
        /// <summary>
        /// Gets or sets the type of input device
        /// </summary>
        public string DeviceType { get; set; }

        /// <summary>
        /// Gets or sets the button identifier
        /// </summary>
        public string ButtonIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        public string DisplayName { get; set; }
    }
}
