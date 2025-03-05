using System;
using Prosim2GSX.Models;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for managing hydraulic fluid operations in ProSim
    /// </summary>
    public class ProsimFluidService : IProsimFluidService
    {
        private readonly IProsimService _prosimService;
        private readonly ServiceModel _model;
        
        /// <summary>
        /// Gets the current blue hydraulic fluid amount
        /// </summary>
        public double BlueFluidAmount => _model.HydaulicsBlueAmount;
        
        /// <summary>
        /// Gets the current green hydraulic fluid amount
        /// </summary>
        public double GreenFluidAmount => _model.HydaulicsGreenAmount;
        
        /// <summary>
        /// Gets the current yellow hydraulic fluid amount
        /// </summary>
        public double YellowFluidAmount => _model.HydaulicsYellowAmount;
        
        /// <summary>
        /// Event raised when fluid state changes
        /// </summary>
        public event EventHandler<FluidStateChangedEventArgs> FluidStateChanged;
        
        /// <summary>
        /// Creates a new instance of ProsimFluidService
        /// </summary>
        /// <param name="prosimService">The ProSim service to use for communication with ProSim</param>
        /// <param name="model">The service model containing configuration settings</param>
        /// <exception cref="ArgumentNullException">Thrown if prosimService or model is null</exception>
        public ProsimFluidService(IProsimService prosimService, ServiceModel model)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }
        
        /// <summary>
        /// Sets the initial hydraulic fluid values based on configuration settings
        /// </summary>
        public void SetInitialFluids()
        {
            try
            {
                _prosimService.SetVariable("aircraft.hydraulics.blue.quantity", _model.HydaulicsBlueAmount);
                _prosimService.SetVariable("aircraft.hydraulics.green.quantity", _model.HydaulicsGreenAmount);
                _prosimService.SetVariable("aircraft.hydraulics.yellow.quantity", _model.HydaulicsYellowAmount);
                
                OnFluidStateChanged("SetInitialFluids", _model.HydaulicsBlueAmount, _model.HydaulicsGreenAmount, _model.HydaulicsYellowAmount);
                
                Logger.Log(LogLevel.Information, "ProsimFluidService:SetInitialFluids", 
                    $"Set initial hydraulic fluid values - Blue: {_model.HydaulicsBlueAmount}, Green: {_model.HydaulicsGreenAmount}, Yellow: {_model.HydaulicsYellowAmount}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFluidService:SetInitialFluids", 
                    $"Error setting initial hydraulic fluid values: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the current hydraulic fluid values and updates the model
        /// </summary>
        /// <returns>A tuple containing the blue, green, and yellow hydraulic fluid amounts</returns>
        public (double BlueAmount, double GreenAmount, double YellowAmount) GetHydraulicFluidValues()
        {
            try
            {
                // Read current values from ProSim and update the model
                _model.HydaulicsBlueAmount = _prosimService.ReadDataRef("aircraft.hydraulics.blue.quantity");
                _model.HydaulicsGreenAmount = _prosimService.ReadDataRef("aircraft.hydraulics.green.quantity");
                _model.HydaulicsYellowAmount = _prosimService.ReadDataRef("aircraft.hydraulics.yellow.quantity");
                
                OnFluidStateChanged("GetHydraulicFluidValues", _model.HydaulicsBlueAmount, _model.HydaulicsGreenAmount, _model.HydaulicsYellowAmount);
                
                return (_model.HydaulicsBlueAmount, _model.HydaulicsGreenAmount, _model.HydaulicsYellowAmount);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimFluidService:GetHydraulicFluidValues", 
                    $"Error getting hydraulic fluid values: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Raises the FluidStateChanged event
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="blueAmount">The current blue hydraulic fluid amount</param>
        /// <param name="greenAmount">The current green hydraulic fluid amount</param>
        /// <param name="yellowAmount">The current yellow hydraulic fluid amount</param>
        protected virtual void OnFluidStateChanged(string operationType, double blueAmount, double greenAmount, double yellowAmount)
        {
            FluidStateChanged?.Invoke(this, new FluidStateChangedEventArgs(operationType, blueAmount, greenAmount, yellowAmount));
        }
    }
}
