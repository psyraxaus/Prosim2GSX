using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX service coordination
    /// </summary>
    public interface IGSXServiceCoordinator
    {
        /// <summary>
        /// Event raised when a service status changes
        /// </summary>
        event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;
        
        /// <summary>
        /// Initializes the service coordinator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Runs loading services (refueling, catering, boarding)
        /// </summary>
        /// <param name="refuelState">The current refuel state</param>
        /// <param name="cateringState">The current catering state</param>
        void RunLoadingServices(int refuelState, int cateringState);
        
        /// <summary>
        /// Runs departure services (loadsheet, equipment removal, pushback)
        /// </summary>
        /// <param name="departureState">The current departure state</param>
        void RunDepartureServices(int departureState);
        
        /// <summary>
        /// Runs arrival services (jetway/stairs, PCA, GPU, chocks)
        /// </summary>
        /// <param name="deboardState">The current deboard state</param>
        void RunArrivalServices(int deboardState);
        
        /// <summary>
        /// Runs deboarding service
        /// </summary>
        /// <param name="deboardState">The current deboard state</param>
        void RunDeboardingService(int deboardState);
        
        /// <summary>
        /// Checks if refueling is complete
        /// </summary>
        /// <returns>True if refueling is complete, false otherwise</returns>
        bool IsRefuelingComplete();
        
        /// <summary>
        /// Checks if boarding is complete
        /// </summary>
        /// <returns>True if boarding is complete, false otherwise</returns>
        bool IsBoardingComplete();
        
        /// <summary>
        /// Checks if catering is complete
        /// </summary>
        /// <returns>True if catering is complete, false otherwise</returns>
        bool IsCateringComplete();
        
        /// <summary>
        /// Checks if the loadsheet has been sent
        /// </summary>
        /// <returns>True if the loadsheet has been sent, false otherwise</returns>
        bool IsFinalLoadsheetSent();
        
        /// <summary>
        /// Checks if the preliminary loadsheet has been sent
        /// </summary>
        /// <returns>True if the preliminary loadsheet has been sent, false otherwise</returns>
        bool IsPreliminaryLoadsheetSent();
        
        /// <summary>
        /// Checks if equipment has been removed
        /// </summary>
        /// <returns>True if equipment has been removed, false otherwise</returns>
        bool IsEquipmentRemoved();
        
        /// <summary>
        /// Checks if pushback is complete
        /// </summary>
        /// <returns>True if pushback is complete, false otherwise</returns>
        bool IsPushbackComplete();
        
        /// <summary>
        /// Checks if deboarding is complete
        /// </summary>
        /// <returns>True if deboarding is complete, false otherwise</returns>
        bool IsDeboardingComplete();
        
        /// <summary>
        /// Sets the number of passengers
        /// </summary>
        /// <param name="numPax">The number of passengers</param>
        void SetPassengers(int numPax);
        
        /// <summary>
        /// Calls jetway and/or stairs
        /// </summary>
        void CallJetwayStairs();
        
        /// <summary>
        /// Resets the service status
        /// </summary>
        void ResetServiceStatus();
    }
}
