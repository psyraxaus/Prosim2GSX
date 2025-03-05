using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for managing hydraulic fluid operations in ProSim
    /// </summary>
    public interface IProsimFluidService
    {
        /// <summary>
        /// Event raised when fluid state changes
        /// </summary>
        event EventHandler<FluidStateChangedEventArgs> FluidStateChanged;
        
        /// <summary>
        /// Gets the current blue hydraulic fluid amount
        /// </summary>
        double BlueFluidAmount { get; }
        
        /// <summary>
        /// Gets the current green hydraulic fluid amount
        /// </summary>
        double GreenFluidAmount { get; }
        
        /// <summary>
        /// Gets the current yellow hydraulic fluid amount
        /// </summary>
        double YellowFluidAmount { get; }
        
        /// <summary>
        /// Sets the initial hydraulic fluid values based on configuration settings
        /// </summary>
        void SetInitialFluids();
        
        /// <summary>
        /// Gets the current hydraulic fluid values and updates the model
        /// </summary>
        /// <returns>A tuple containing the blue, green, and yellow hydraulic fluid amounts</returns>
        (double BlueAmount, double GreenAmount, double YellowAmount) GetHydraulicFluidValues();
    }
    
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
        /// Gets the current blue hydraulic fluid amount
        /// </summary>
        public double BlueAmount { get; }
        
        /// <summary>
        /// Gets the current green hydraulic fluid amount
        /// </summary>
        public double GreenAmount { get; }
        
        /// <summary>
        /// Gets the current yellow hydraulic fluid amount
        /// </summary>
        public double YellowAmount { get; }
        
        /// <summary>
        /// Creates a new instance of FluidStateChangedEventArgs
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="blueAmount">The current blue hydraulic fluid amount</param>
        /// <param name="greenAmount">The current green hydraulic fluid amount</param>
        /// <param name="yellowAmount">The current yellow hydraulic fluid amount</param>
        public FluidStateChangedEventArgs(string operationType, double blueAmount, double greenAmount, double yellowAmount)
        {
            OperationType = operationType;
            BlueAmount = blueAmount;
            GreenAmount = greenAmount;
            YellowAmount = yellowAmount;
        }
    }
}
