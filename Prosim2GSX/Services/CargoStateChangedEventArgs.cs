using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for cargo state changes
    /// </summary>
    public class CargoStateChangedEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current cargo percentage
        /// </summary>
        public int CurrentPercentage { get; }
        
        /// <summary>
        /// Gets the planned cargo amount
        /// </summary>
        public int PlannedAmount { get; }
        
        /// <summary>
        /// Gets a value indicating whether cargo loading is in progress
        /// </summary>
        public bool IsLoadingInProgress { get; }
        
        /// <summary>
        /// Gets the loading progress percentage (0-100)
        /// </summary>
        public int LoadingProgress { get; }
        
        /// <summary>
        /// Gets a value indicating whether cargo unloading is in progress
        /// </summary>
        public bool IsUnloadingInProgress { get; }
        
        /// <summary>
        /// Gets the unloading progress percentage (0-100)
        /// </summary>
        public int UnloadingProgress { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CargoStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentPercentage">The current cargo percentage</param>
        /// <param name="plannedAmount">The planned cargo amount</param>
        /// <param name="isLoadingInProgress">Whether cargo loading is in progress</param>
        /// <param name="loadingProgress">The loading progress percentage</param>
        /// <param name="isUnloadingInProgress">Whether cargo unloading is in progress</param>
        /// <param name="unloadingProgress">The unloading progress percentage</param>
        public CargoStateChangedEventArgs(
            string operationType, 
            int currentPercentage, 
            int plannedAmount, 
            bool isLoadingInProgress = false, 
            int loadingProgress = 0, 
            bool isUnloadingInProgress = false, 
            int unloadingProgress = 0)
        {
            OperationType = operationType;
            CurrentPercentage = currentPercentage;
            PlannedAmount = plannedAmount;
            IsLoadingInProgress = isLoadingInProgress;
            LoadingProgress = loadingProgress;
            IsUnloadingInProgress = isUnloadingInProgress;
            UnloadingProgress = unloadingProgress;
        }
    }
}
