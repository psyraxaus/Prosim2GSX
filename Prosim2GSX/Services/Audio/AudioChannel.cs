namespace Prosim2GSX.Services.Audio
{
    /// <summary>
    /// Enum representing the available audio channels
    /// </summary>
    public enum AudioChannel
    {
        /// <summary>
        /// GSX intercom channel
        /// </summary>
        INT,

        /// <summary>
        /// VHF1 radio channel
        /// </summary>
        VHF1,

        /// <summary>
        /// VHF2 radio channel
        /// </summary>
        VHF2,

        /// <summary>
        /// VHF3 radio channel
        /// </summary>
        VHF3,

        /// <summary>
        /// Cabin intercom channel
        /// </summary>
        CAB,

        /// <summary>
        /// Passenger announcement channel
        /// </summary>
        PA
    }

    /// <summary>
    /// Enum representing selection of Core Audio or VoiceMeeter
    /// </summary>
    public enum AudioApiType
    {
        CoreAudio,
        VoiceMeeter
    }
}
