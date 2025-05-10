namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for managing GSX catering
    /// </summary>
    public interface IGsxCateringService
    {
        /// <summary>
        /// Gets whether catering service has been requested
        /// </summary>
        bool IsCateringRequested { get; }

        /// <summary>
        /// Gets whether catering service is active
        /// </summary>
        bool IsCateringActive { get; }

        /// <summary>
        /// Gets whether catering is complete
        /// </summary>
        bool IsCateringComplete { get; }

        /// <summary>
        /// Gets the current catering state (0-6 matching GSX states)
        /// </summary>
        int CateringState { get; }

        /// <summary>
        /// Request catering service through GSX menu
        /// </summary>
        void RequestCateringService();

        /// <summary>
        /// Process catering operations
        /// </summary>
        /// <returns>True if catering is complete</returns>
        bool ProcessCatering();

        /// <summary>
        /// Handle toggle for front passenger door
        /// </summary>
        void ToggleFrontDoor();

        /// <summary>
        /// Handle toggle for aft passenger door
        /// </summary>
        void ToggleAftDoor();

        /// <summary>
        /// Subscribe to service toggle LVARs
        /// </summary>
        void SubscribeToServiceToggles();
    }
}