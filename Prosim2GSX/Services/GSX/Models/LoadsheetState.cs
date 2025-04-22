namespace Prosim2GSX.Services.GSX.Models
{
    /// <summary>
    /// Represents the state of a loadsheet generation process
    /// </summary>
    public enum LoadsheetState
    {
        /// <summary>
        /// Loadsheet generation has not been started
        /// </summary>
        NotStarted,

        /// <summary>
        /// Waiting for the right conditions to generate a loadsheet
        /// </summary>
        Waiting,

        /// <summary>
        /// Loadsheet is currently being generated
        /// </summary>
        Generating,

        /// <summary>
        /// Loadsheet was successfully generated
        /// </summary>
        Completed,

        /// <summary>
        /// Loadsheet generation failed
        /// </summary>
        Failed
    }
}