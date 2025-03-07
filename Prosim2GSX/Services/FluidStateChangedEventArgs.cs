using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for fluid state changes
    /// </summary>
    public class FluidStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current fluid amount
        /// </summary>
        public double CurrentAmount { get; }
        
        /// <summary>
        /// Gets the maximum fluid amount
        /// </summary>
        public double MaximumAmount { get; }
        
        /// <summary>
        /// Gets the fluid type
        /// </summary>
        public string FluidType { get; }
        
        /// <summary>
        /// Gets the blue hydraulic fluid amount
        /// </summary>
        public double BlueAmount { get; }
        
        /// <summary>
        /// Gets the green hydraulic fluid amount
        /// </summary>
        public double GreenAmount { get; }
        
        /// <summary>
        /// Gets the yellow hydraulic fluid amount
        /// </summary>
        public double YellowAmount { get; }
        
        /// <summary>
        /// Gets the timestamp of the state change
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FluidStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentAmount">The current fluid amount</param>
        /// <param name="maximumAmount">The maximum fluid amount</param>
        /// <param name="fluidType">The fluid type</param>
        public FluidStateChangedEventArgs(string operationType, double currentAmount, double maximumAmount, string fluidType)
        {
            OperationType = operationType;
            CurrentAmount = currentAmount;
            MaximumAmount = maximumAmount;
            FluidType = fluidType;
            BlueAmount = 0;
            GreenAmount = 0;
            YellowAmount = 0;
            Timestamp = DateTime.Now;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FluidStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="blueAmount">The blue hydraulic fluid amount</param>
        /// <param name="greenAmount">The green hydraulic fluid amount</param>
        /// <param name="yellowAmount">The yellow hydraulic fluid amount</param>
        public FluidStateChangedEventArgs(string operationType, double blueAmount, double greenAmount, double yellowAmount)
        {
            OperationType = operationType;
            CurrentAmount = 0;
            MaximumAmount = 0;
            FluidType = "Hydraulic";
            BlueAmount = blueAmount;
            GreenAmount = greenAmount;
            YellowAmount = yellowAmount;
            Timestamp = DateTime.Now;
        }
    }
}
