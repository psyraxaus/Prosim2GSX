namespace Prosim2GSX.Services.GSX.Models
{
    /// <summary>
    /// Status of ground services
    /// </summary>
    public class GroundServiceStatus
    {
        /// <summary>
        /// Whether chocks are set
        /// </summary>
        public bool ChocksSet { get; set; }

        /// <summary>
        /// Whether GPU is connected
        /// </summary>
        public bool GPUConnected { get; set; }

        /// <summary>
        /// Whether PCA is connected
        /// </summary>
        public bool PCAConnected { get; set; }
    }
}