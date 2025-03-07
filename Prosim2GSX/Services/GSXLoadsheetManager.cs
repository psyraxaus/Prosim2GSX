using System;
using System.Text;
using System.Threading.Tasks;
using Prosim2GSX.Models;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX loadsheet management
    /// </summary>
    public class GSXLoadsheetManager : IGSXLoadsheetManager
    {
        private readonly IAcarsService _acarsService;
        private readonly IProsimFlightDataService _flightDataService;
        private readonly FlightPlan _flightPlan;
        private readonly ServiceModel _model;
        
        private bool _preliminaryLoadsheetSent = false;
        private bool _finalLoadsheetSent = false;
        
        // Store preliminary data for comparison with final data
        private double _prelimZfw = 0.0d;
        private double _prelimTow = 0.0d;
        private int _prelimPax = 0;
        private double _prelimMacZfw = 0.0d;
        private double _prelimMacTow = 0.0d;
        private double _prelimFuel = 0.0d;
        
        /// <summary>
        /// Event raised when a loadsheet is generated
        /// </summary>
        public event EventHandler<LoadsheetGeneratedEventArgs> LoadsheetGenerated;
        
        /// <summary>
        /// Initializes a new instance of the GSXLoadsheetManager class
        /// </summary>
        /// <param name="acarsService">The ACARS service</param>
        /// <param name="flightDataService">The flight data service</param>
        /// <param name="flightPlan">The flight plan</param>
        /// <param name="model">The service model</param>
        public GSXLoadsheetManager(IAcarsService acarsService, IProsimFlightDataService flightDataService, FlightPlan flightPlan, ServiceModel model)
        {
            _acarsService = acarsService ?? throw new ArgumentNullException(nameof(acarsService));
            _flightDataService = flightDataService ?? throw new ArgumentNullException(nameof(flightDataService));
            _flightPlan = flightPlan ?? throw new ArgumentNullException(nameof(flightPlan));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }
        
        /// <summary>
        /// Initializes the loadsheet manager
        /// </summary>
        public void Initialize()
        {
            Reset();
            Logger.Log(LogLevel.Information, "GSXLoadsheetManager:Initialize", "Loadsheet manager initialized");
        }
        
        /// <summary>
        /// Generates and sends a preliminary loadsheet
        /// </summary>
        /// <param name="flightNumber">The flight number</param>
        /// <returns>True if the loadsheet was generated and sent successfully</returns>
        public async Task<bool> GeneratePreliminaryLoadsheetAsync(string flightNumber)
        {
            if (!_model.UseAcars)
            {
                Logger.Log(LogLevel.Information, "GSXLoadsheetManager:GeneratePreliminaryLoadsheetAsync", "ACARS is disabled, skipping preliminary loadsheet");
                _preliminaryLoadsheetSent = true;
                OnLoadsheetGenerated("prelim", flightNumber, true);
                return true;
            }
            
            try
            {
                var loadsheetData = _flightDataService.GetLoadedData("prelim");
                await _acarsService.SendPreliminaryLoadsheetAsync(flightNumber, loadsheetData);
                
                // Store preliminary data for later comparison
                _prelimZfw = loadsheetData.EstZfw;
                _prelimTow = loadsheetData.EstTow;
                _prelimPax = loadsheetData.PaxAdults;
                _prelimMacZfw = loadsheetData.MacZfw;
                _prelimMacTow = loadsheetData.MacTow;
                _prelimFuel = Math.Round(loadsheetData.FuelInTanks);
                
                _preliminaryLoadsheetSent = true;
                
                Logger.Log(LogLevel.Information, "GSXLoadsheetManager:GeneratePreliminaryLoadsheetAsync", "Preliminary loadsheet sent successfully");
                OnLoadsheetGenerated("prelim", flightNumber, true);
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXLoadsheetManager:GeneratePreliminaryLoadsheetAsync", $"Error sending preliminary loadsheet: {ex.Message}");
                OnLoadsheetGenerated("prelim", flightNumber, false);
                return false;
            }
        }
        
        /// <summary>
        /// Generates and sends a final loadsheet
        /// </summary>
        /// <param name="flightNumber">The flight number</param>
        /// <returns>True if the loadsheet was generated and sent successfully</returns>
        public async Task<bool> GenerateFinalLoadsheetAsync(string flightNumber)
        {
            if (!_model.UseAcars)
            {
                Logger.Log(LogLevel.Information, "GSXLoadsheetManager:GenerateFinalLoadsheetAsync", "ACARS is disabled, skipping final loadsheet");
                _finalLoadsheetSent = true;
                OnLoadsheetGenerated("final", flightNumber, true);
                return true;
            }
            
            try
            {
                var loadsheetData = _flightDataService.GetLoadedData("final");
                var prelimData = (_prelimZfw, _prelimTow, _prelimPax, _prelimMacZfw, _prelimMacTow, _prelimFuel);
                
                await _acarsService.SendFinalLoadsheetAsync(flightNumber, loadsheetData, prelimData);
                
                _finalLoadsheetSent = true;
                
                Logger.Log(LogLevel.Information, "GSXLoadsheetManager:GenerateFinalLoadsheetAsync", "Final loadsheet sent successfully");
                OnLoadsheetGenerated("final", flightNumber, true);
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXLoadsheetManager:GenerateFinalLoadsheetAsync", $"Error sending final loadsheet: {ex.Message}");
                OnLoadsheetGenerated("final", flightNumber, false);
                return false;
            }
        }
        
        /// <summary>
        /// Checks if a preliminary loadsheet has been sent
        /// </summary>
        /// <returns>True if a preliminary loadsheet has been sent</returns>
        public bool IsPreliminaryLoadsheetSent()
        {
            return _preliminaryLoadsheetSent;
        }
        
        /// <summary>
        /// Checks if a final loadsheet has been sent
        /// </summary>
        /// <returns>True if a final loadsheet has been sent</returns>
        public bool IsFinalLoadsheetSent()
        {
            return _finalLoadsheetSent;
        }
        
        /// <summary>
        /// Resets the loadsheet manager state
        /// </summary>
        public void Reset()
        {
            _preliminaryLoadsheetSent = false;
            _finalLoadsheetSent = false;
            _prelimZfw = 0.0d;
            _prelimTow = 0.0d;
            _prelimPax = 0;
            _prelimMacZfw = 0.0d;
            _prelimMacTow = 0.0d;
            _prelimFuel = 0.0d;
            
            Logger.Log(LogLevel.Information, "GSXLoadsheetManager:Reset", "Loadsheet manager reset");
        }
        
        /// <summary>
        /// Raises the LoadsheetGenerated event
        /// </summary>
        /// <param name="loadsheetType">The type of loadsheet that was generated</param>
        /// <param name="flightNumber">The flight number for the loadsheet</param>
        /// <param name="success">Whether the loadsheet generation was successful</param>
        protected virtual void OnLoadsheetGenerated(string loadsheetType, string flightNumber, bool success)
        {
            LoadsheetGenerated?.Invoke(this, new LoadsheetGeneratedEventArgs(loadsheetType, flightNumber, success));
        }
    }
}
