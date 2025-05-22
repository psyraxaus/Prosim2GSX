namespace Prosim2GSX.Services.PTT.Enums
{
    /// <summary>
    /// Defines the types of Audio Control Panel (ACP) channels
    /// </summary>
    public enum AcpChannelType
    {
        /// <summary>
        /// No channel selected
        /// </summary>
        None = 0,

        /// <summary>
        /// VHF 1 radio channel
        /// </summary>
        VHF1 = 1,

        /// <summary>
        /// VHF 2 radio channel
        /// </summary>
        VHF2 = 2,

        /// <summary>
        /// VHF 3 radio channel
        /// </summary>
        VHF3 = 3,

        /// <summary>
        /// HF 1 radio channel
        /// </summary>
        HF1 = 4,

        /// <summary>
        /// HF 2 radio channel
        /// </summary>
        HF2 = 5,

        /// <summary>
        /// Interphone (Cockpit intercom)
        /// </summary>
        INT = 6,

        /// <summary>
        /// Cabin intercom
        /// </summary>
        CAB = 7,

        /// <summary>
        /// Passenger address system
        /// </summary>
        PA = 8
    }
}
