namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for managing GSX passenger boarding and deboarding
    /// </summary>
    public interface IGsxBoardingService
    {
        /// <summary>
        /// Whether boarding is active
        /// </summary>
        bool IsBoardingActive { get; }

        /// <summary>
        /// Whether boarding is complete
        /// </summary>
        bool IsBoardingComplete { get; }

        /// <summary>
        /// Whether boarding is requested
        /// </summary>
        bool IsBoardingRequested { get; }

        /// <summary>
        /// Whether deboarding is active
        /// </summary>
        bool IsDeboarding { get; }

        /// <summary>
        /// Whether deboarding is complete
        /// </summary>
        bool IsDeboardingComplete { get; }

        /// <summary>
        /// Start boarding process
        /// </summary>
        void StartBoarding();

        /// <summary>
        /// Stop boarding process
        /// </summary>
        void StopBoarding();

        /// <summary>
        /// Start deboarding process
        /// </summary>
        void StartDeboarding();

        /// <summary>
        /// Stop deboarding process
        /// </summary>
        void StopDeboarding();

        /// <summary>
        /// Process boarding
        /// </summary>
        /// <param name="paxCurrent">Current passenger count</param>
        /// <param name="cargoPercent">Cargo loading percentage</param>
        /// <returns>True if boarding complete</returns>
        bool ProcessBoarding(int paxCurrent, int cargoPercent);

        /// <summary>
        /// Process deboarding
        /// </summary>
        /// <param name="paxCurrent">Current passenger count</param>
        /// <param name="cargoPercent">Cargo loading percentage</param>
        /// <returns>True if deboarding complete</returns>
        bool ProcessDeboarding(int paxCurrent, int cargoPercent);

        /// <summary>
        /// Set passenger count
        /// </summary>
        /// <param name="numPax">Number of passengers</param>
        void SetPassengers(int numPax);

        /// <summary>
        /// Request boarding service from GSX
        /// </summary>
        void RequestBoardingService();

        /// <summary>
        /// Request deboarding service from GSX
        /// </summary>
        void RequestDeboardingService();

        /// <summary>
        /// Get current passenger count
        /// </summary>
        /// <returns>Current passenger count</returns>
        int GetCurrentPassengers();

        /// <summary>
        /// Get planned passenger count
        /// </summary>
        /// <returns>Planned passenger count</returns>
        int GetPlannedPassengers();
    }
}