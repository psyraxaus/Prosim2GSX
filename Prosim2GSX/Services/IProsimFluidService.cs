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
}
