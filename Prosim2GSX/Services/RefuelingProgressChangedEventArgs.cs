using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for refueling progress changes
    /// </summary>
public class RefuelingProgressChangedEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; }
        
        /// <summary>
        /// Gets the progress (0-100) - Alias for ProgressPercentage
        /// </summary>
        public int Progress => ProgressPercentage;
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        public double CurrentAmount { get; }
        
        /// <summary>
        /// Gets the target fuel amount
        /// </summary>
        public double TargetAmount { get; }
        
        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        public string FuelUnits { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RefuelingProgressChangedEventArgs"/> class
        /// </summary>
        /// <param name="progressPercentage">The progress percentage (0-100)</param>
        /// <param name="currentAmount">The current fuel amount</param>
        /// <param name="targetAmount">The target fuel amount</param>
        /// <param name="fuelUnits">The fuel units (KG or LBS)</param>
        public RefuelingProgressChangedEventArgs(int progressPercentage, double currentAmount, double targetAmount, string fuelUnits)
        {
            ProgressPercentage = progressPercentage;
            CurrentAmount = currentAmount;
            TargetAmount = targetAmount;
            FuelUnits = fuelUnits;
        }
    }
}
